using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using ThreadFlow.WinForms.Theme.Colors;

namespace ThreadFlow.WinForms.Theme;

/// <summary>
/// The shared look of every popup in the app: a rounded white card floating over a dimmed
/// backdrop, with the heading at the top, hairline dividers and right-aligned actions.
/// Tables keep their own design; this only dresses the window around them.
/// </summary>
internal static class DialogChrome
{
    /// Page margin inside a card.
    internal const int Pad = 24;

    /// Height of the heading block: title, subtitle and the divider underneath them.
    internal const int HeaderHeight = 92;

    /// Height of the action buttons in a card footer.
    internal const int ButtonHeight = 42;

    /// Corner radius of the card itself.
    internal const int Radius = 12;

    /// Width and height of the close glyph in a card corner.
    internal const int CloseSize = 28;

    private static readonly Pallets Palette = new();

    internal static readonly Color SurfaceColor = Color.White;
    internal static readonly Color DividerColor = Color.FromArgb(233, 236, 243);
    internal static readonly Color ScrimColor = Color.FromArgb(15, 23, 42);
    internal static readonly Color TitleColor = Palette.TextPrimary.Color;
    internal static readonly Color MutedColor = Palette.TextSecondary.Color;
    internal static readonly Color AccentColor = Palette.Primary.Color;

    internal static readonly Font TitleFont = new("Segoe UI Semibold", 16F);
    internal static readonly Font SubtitleFont = new("Segoe UI", 10F);
    internal static readonly Font ButtonFont = new("Segoe UI Semibold", 9.5F);

    /// <summary>
    /// One veil per page, reused by every popup on it. Building a fresh window for each popup
    /// put a new surface in front of the user mid-open, which is what made the cards flash.
    /// </summary>
    private static readonly Dictionary<Form, ModalBackdrop> Veils = new();

    /// <summary>The page each open card is dimming, so the veil comes down when it closes.</summary>
    private static readonly Dictionary<Form, Form> DimmedPages = new();

    /// <summary>
    /// Turns a form into a card: square-free corners, a white surface, the dimmed backdrop
    /// underneath while it is open, and no title bar of its own.
    /// </summary>
    internal static void Prepare(Form dialog)
    {
        dialog.BackColor = SurfaceColor;
        dialog.FormBorderStyle = FormBorderStyle.None;
        dialog.StartPosition = FormStartPosition.CenterParent;
        dialog.ShowInTaskbar = false;

        RoundCorners(dialog);
        dialog.HandleCreated += (_, _) =>
        {
            RoundCorners(dialog);

            // Set before the window is ever shown, so the very first frame is already composed.
            ComposeCard(dialog);
        };
        dialog.Resize += (_, _) => RoundCorners(dialog);

        // Lift the veil while the card is still loading. Load runs once the dialog knows the page
        // that owns it and before its window is shown, so the page dims in the same frame the
        // popup appears. Shown and every later activation only re-assert it, which does nothing
        // while it is already up.
        dialog.Load += (_, _) =>
        {
            // Re-assert the composite while the window is still hidden: this is the last point
            // before it is shown, so the first frame the user sees is a finished card.
            ComposeCard(dialog);
            AttachBackdrop(dialog);
        };
        dialog.Shown += (_, _) => AttachBackdrop(dialog);
        dialog.Activated += (_, _) => AttachBackdrop(dialog);

        // Take the veil down only once the card is gone. Lowering it while the card is still on
        // screen would flash the page back to full brightness around the closing popup.
        dialog.FormClosed += (_, _) => DetachBackdrop(dialog);
        dialog.Disposed += (_, _) => DetachBackdrop(dialog);
    }

    /// <summary>
    /// The top of a card: bold heading, muted subtitle, a close glyph in the corner and the
    /// divider that separates the heading from the content. Dragging it moves the dialog.
    /// </summary>
    internal static Panel CreateHeader(Form dialog, string title, string subtitle, int width)
    {
        var header = new Panel
        {
            BackColor = SurfaceColor,
            Location = Point.Empty,
            Size = new Size(width, HeaderHeight)
        };

        var titleLabel = new Label
        {
            Text = title,
            Font = TitleFont,
            ForeColor = TitleColor,
            BackColor = Color.Transparent,
            AutoSize = false,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(Pad, 20),
            Size = new Size(width - Pad * 2 - 44, 30)
        };

        var subtitleLabel = new Label
        {
            Text = subtitle,
            Font = SubtitleFont,
            ForeColor = MutedColor,
            BackColor = Color.Transparent,
            AutoSize = false,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(Pad, 54),
            Size = new Size(width - Pad * 2, 24)
        };

        var divider = new Panel
        {
            BackColor = DividerColor,
            Location = new Point(0, HeaderHeight - 1),
            Size = new Size(width, 1)
        };

        var close = new DialogCloseButton
        {
            // Kept a full margin away from the card edge, level with the title.
            Location = new Point(width - Pad - CloseSize, 20)
        };
        close.Click += (_, _) => dialog.Close();

        header.Controls.AddRange([titleLabel, subtitleLabel, close, divider]);
        EnableDrag(dialog, header, titleLabel, subtitleLabel);

        return header;
    }

    /// <summary>A full-width hairline used above card footers.</summary>
    internal static Panel CreateDivider(int width, int top) => new()
    {
        BackColor = DividerColor,
        Location = new Point(0, top),
        Size = new Size(width, 1)
    };

    internal static int MeasureWrappedHeight(string text, Font font, int width)
    {
        if (string.IsNullOrEmpty(text))
            return 22;

        var measured = TextRenderer.MeasureText(
            text,
            font,
            new Size(Math.Max(40, width), 4000),
            TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);

        return Math.Max(22, measured.Height + 2);
    }

    internal static int MeasureButtonWidth(string text) =>
        Math.Max(96, TextRenderer.MeasureText(text, ButtonFont).Width + 40);

    internal static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        radius = Math.Max(1, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2));

        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    /// <summary>
    /// Rounds the card. On Windows 11 the window manager rounds the window itself, with a
    /// smooth antialiased edge; clipping with a Region is a hard one-bit mask, which is why the
    /// corners used to look stepped. Older Windows keeps the Region as the fallback.
    /// </summary>
    private static void RoundCorners(Form dialog)
    {
        if (!dialog.IsHandleCreated)
            return;

        if (RoundWithWindowManager(dialog.Handle))
        {
            // A clipping region would only cut the antialiased corner off again.
            if (dialog.Region is not null)
                dialog.Region = null;

            return;
        }

        var size = dialog.ClientSize;
        if (size.Width <= 0 || size.Height <= 0)
            return;

        var scale = dialog.DeviceDpi > 0 ? dialog.DeviceDpi / 96f : 1f;
        using var path = RoundedRect(new Rectangle(Point.Empty, size), (int)Math.Round(Radius * scale));

        var previous = dialog.Region;
        dialog.Region = new Region(path);
        previous?.Dispose();
    }

    /// <summary>
    /// Asks the window manager for rounded corners and switches off the opening transition, so
    /// the card arrives in a single frame instead of fading and scaling in. False on Windows
    /// versions that do not know the attribute, where the Region takes over.
    /// </summary>
    private static bool RoundWithWindowManager(IntPtr handle)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            return false;

        var transitionsDisabled = 1;
        _ = DwmSetWindowAttribute(handle, DwmTransitionsForcedDisabled, ref transitionsDisabled, sizeof(int));

        var preference = DwmRoundCorners;
        return DwmSetWindowAttribute(handle, DwmWindowCornerPreference, ref preference, sizeof(int)) == 0;
    }

    private static void AttachBackdrop(Form dialog)
    {
        var page = ResolveOwner(dialog);

        if (page is null)
            return;

        if (DimmedPages.TryGetValue(dialog, out var dimmed))
        {
            if (ReferenceEquals(dimmed, page) && !dimmed.IsDisposed)
            {
                // Already dimming this page: only make sure the veil is still up. It is never
                // moved or re-stacked here, because reordering windows that are already on
                // screen is what made the popup flash.
                if (Veils.TryGetValue(page, out var current) && !current.IsDisposed)
                    ShowVeil(current, page, dialog);

                return;
            }

            // The page changed (the owner is only known once the dialog is loading).
            DetachBackdrop(dialog);
        }

        DimmedPages[dialog] = page;
        ShowVeil(GetVeil(page), page, dialog);
    }

    private static Form? ResolveOwner(Form dialog)
    {
        var owner = dialog.Owner ?? dialog.ParentForm ?? Form.ActiveForm;

        if (owner is null || ReferenceEquals(owner, dialog) || owner.IsDisposed || !owner.Visible)
            return null;

        return owner;
    }

    /// <summary>The veil for a page: built the first time a popup needs it, then kept for the next.</summary>
    private static ModalBackdrop GetVeil(Form page)
    {
        if (Veils.TryGetValue(page, out var existing) && !existing.IsDisposed)
            return existing;

        var veil = new ModalBackdrop(page);
        Veils[page] = veil;

        // The veil belongs to the page, so it goes when the page goes.
        page.Disposed += (_, _) => ReleaseVeil(page);
        return veil;
    }

    private static void ShowVeil(ModalBackdrop veil, Form page, Form dialog)
    {
        if (veil.IsDisposed || !page.Visible)
            return;

        if (veil.Bounds != page.Bounds)
            veil.Bounds = page.Bounds;

        // Already dimming: leave it exactly where it is.
        if (veil.Visible)
            return;

        veil.Show(page);

        // A veil coming back from hidden sits above the page's other windows; tuck it directly
        // behind the card so a popup that is already open is never dimmed itself.
        if (dialog.IsHandleCreated && dialog.Visible)
            SetWindowPos(veil.Handle, dialog.Handle, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate);
    }

    private static void DetachBackdrop(Form dialog)
    {
        if (!DimmedPages.Remove(dialog, out var page))
            return;

        if (!Veils.TryGetValue(page, out var veil) || veil.IsDisposed)
            return;

        // Another card on the same page is still open: it keeps the page dimmed.
        foreach (var entry in DimmedPages)
        {
            if (ReferenceEquals(entry.Value, page))
                return;
        }

        veil.Hide();
    }

    private static void ReleaseVeil(Form page)
    {
        if (!Veils.Remove(page, out var veil) || veil.IsDisposed)
            return;

        veil.Dispose();
    }

    /// <summary>
    /// Composites the whole card off screen. Without it the window, its panels and its labels each
    /// paint on their own, which reads as the card flickering the moment the popup opens.
    /// </summary>
    private static void ComposeCard(Form dialog)
    {
        var handle = dialog.Handle;
        if (handle == IntPtr.Zero)
            return;

        var style = GetWindowLong(handle, GwlExStyle);
        if ((style & WsExComposited) == 0)
            SetWindowLong(handle, GwlExStyle, style | WsExComposited);

        // An extended style written after the window exists only takes effect once the frame is
        // recomputed. Without this nudge the card keeps painting its children one at a time,
        // which is the flash seen while it opens.
        SetWindowPos(
            handle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
    }

    private static void EnableDrag(Form dialog, params Control[] controls)
    {
        void BeginDrag(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || e.Clicks != 1)
                return;

            ReleaseCapture();
            _ = SendMessage(dialog.Handle, WmNcLButtonDown, HtCaption, 0);
        }

        foreach (var control in controls)
            control.MouseDown += BeginDrag;
    }

    private const int GwlExStyle = -20;
    private const int WsExComposited = 0x02000000;
    private const int WmNcLButtonDown = 0xA1;
    private const int HtCaption = 2;
    private const int SwpNoSize = 0x0001;
    private const int SwpNoMove = 0x0002;
    private const int SwpNoZOrder = 0x0004;
    private const int SwpNoActivate = 0x0010;
    private const int SwpFrameChanged = 0x0020;
    private const int DwmTransitionsForcedDisabled = 3;
    private const int DwmWindowCornerPreference = 33;
    private const int DwmRoundCorners = 2;

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int flags);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hWnd, int attribute, ref int value, int size);
}

/// <summary>
/// The dark veil behind a popup. It covers the page that opened the dialog, never takes focus
/// and is never hit by the mouse, so the dialog behind it stays the only thing to interact with.
/// </summary>
internal sealed class ModalBackdrop : Form
{
    private readonly Form _owner;

    internal ModalBackdrop(Form owner)
    {
        _owner = owner;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = DialogChrome.ScrimColor;

        // Layered from the very first frame, so the veil never appears solid black for a frame
        // before its alpha is applied.
        AllowTransparency = true;
        Opacity = 0.45;
        Bounds = owner.Bounds;

        owner.LocationChanged += FollowOwner;
        owner.SizeChanged += FollowOwner;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW: stays out of Alt+Tab
            parameters.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE: never steals focus
            return parameters;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _owner.LocationChanged -= FollowOwner;
            _owner.SizeChanged -= FollowOwner;
        }

        base.Dispose(disposing);
    }

    private void FollowOwner(object? sender, EventArgs e)
    {
        if (_owner.IsDisposed || IsDisposed)
            return;

        Bounds = _owner.Bounds;
    }
}

/// <summary>The small thin X in the top corner of a card.</summary>
internal sealed class DialogCloseButton : Control
{
    private bool _hover;

    internal DialogCloseButton()
    {
        Size = new Size(28, 28);
        Cursor = Cursors.Hand;
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.ResizeRedraw,
            true);

        // Opaque card surface. The rounded shape is painted on top of it, so the antialiased
        // rim never depends on the parent being repainted underneath the control.
        BackColor = DialogChrome.SurfaceColor;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
                    // Lay the card surface down first: a transparent background left the antialiased
            // rim blending with stale pixels, which roughened the shape.
            e.Graphics.Clear(Parent?.BackColor ?? DialogChrome.SurfaceColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (_hover)

        {
            using var hoverBrush = new SolidBrush(Color.FromArgb(241, 243, 247));
            using var hoverPath = DialogChrome.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 6);
            e.Graphics.FillPath(hoverBrush, hoverPath);
        }

        var scale = DeviceDpi > 0 ? DeviceDpi / 96f : 1f;
        var half = 5.5f * scale;
        var centerX = Width / 2f;
        var centerY = Height / 2f;

        using var pen = new Pen(Color.FromArgb(107, 114, 128), Math.Max(1.2f, 1.5f * scale))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        e.Graphics.DrawLine(pen, centerX - half, centerY - half, centerX + half, centerY + half);
        e.Graphics.DrawLine(pen, centerX + half, centerY - half, centerX - half, centerY + half);
    }
}

/// <summary>A footer button: filled blue for the main action, outlined grey for everything else.</summary>
internal sealed class DialogButton : Button
{
    private readonly bool _primary;
    private bool _hover;

    internal DialogButton(string text, bool primary)
    {
        Text = text;
        _primary = primary;
        Font = DialogChrome.ButtonFont;
        Cursor = Cursors.Hand;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        Size = new Size(DialogChrome.MeasureButtonWidth(text), DialogChrome.ButtonHeight);

        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.ResizeRedraw,
            true);

        // Opaque card surface. The rounded shape is painted on top of it, so the antialiased
        // rim never depends on the parent being repainted underneath the control.
        BackColor = DialogChrome.SurfaceColor;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
                    // Paint the card surface behind the shape, exactly as the workspace buttons do.
            // Without it the antialiased rim blends with whatever was left underneath and the
            // footer buttons read as stepped, notched corners.
            e.Graphics.Clear(Parent?.BackColor ?? DialogChrome.SurfaceColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = DialogChrome.RoundedRect(bounds, 9);


        if (_primary)
        {
            var fill = !Enabled
                ? Color.FromArgb(147, 172, 240)
                : _hover ? Color.FromArgb(37, 82, 214) : DialogChrome.AccentColor;
            using var brush = new SolidBrush(fill);
            e.Graphics.FillPath(brush, path);
        }
        else
        {
            using var brush = new SolidBrush(_hover ? Color.FromArgb(246, 248, 252) : DialogChrome.SurfaceColor);
            e.Graphics.FillPath(brush, path);

            using var border = new Pen(Color.FromArgb(208, 213, 221), 1f);
            e.Graphics.DrawPath(border, path);
        }

        var textColor = _primary
            ? Color.White
            : Enabled ? DialogChrome.TitleColor : Color.FromArgb(148, 156, 170);

        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            bounds,
            textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}

/// <summary>
/// A small card prompt for confirmations and messages: heading, one wrapped paragraph and the
/// actions along the bottom. Returns DialogResult.OK when confirmed; Cancel, the close glyph and
/// Esc all return Cancel.
/// </summary>
internal sealed class ConfirmDialog : Form
{
    private const int DialogWidth = 440;

    public ConfirmDialog(
        string headerText,
        string title,
        string message,
        string confirmText,
        string? cancelText = "Cancel")
    {
        DialogChrome.Prepare(this);

        SuspendLayout();

        var innerWidth = DialogWidth - DialogChrome.Pad * 2;
        const int titleTop = 22;
        var messageTop = titleTop + 38;
        var messageHeight = DialogChrome.MeasureWrappedHeight(message, DialogChrome.SubtitleFont, innerWidth - 32);
        var buttonTop = messageTop + messageHeight + 24;

        Text = string.IsNullOrWhiteSpace(headerText) ? title : headerText;
        ClientSize = new Size(DialogWidth, buttonTop + DialogChrome.ButtonHeight + DialogChrome.Pad - 4);

        var titleLabel = new Label
        {
            Text = title,
            Font = DialogChrome.TitleFont,
            ForeColor = DialogChrome.TitleColor,
            BackColor = Color.Transparent,
            AutoSize = false,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(DialogChrome.Pad, titleTop),
            Size = new Size(innerWidth - 32, 30)
        };

        var messageLabel = new Label
        {
            Text = message,
            Font = DialogChrome.SubtitleFont,
            ForeColor = DialogChrome.MutedColor,
            BackColor = Color.Transparent,
            AutoSize = false,
            TextAlign = ContentAlignment.TopLeft,
            Location = new Point(DialogChrome.Pad, messageTop),
            Size = new Size(innerWidth, messageHeight)
        };

        var close = new DialogCloseButton
        {
            Location = new Point(DialogWidth - DialogChrome.Pad - DialogChrome.CloseSize, titleTop)
        };
        close.Click += (_, _) => DialogResult = DialogResult.Cancel;

        var confirm = new DialogButton(confirmText, primary: true)
        {
            DialogResult = DialogResult.OK
        };
        confirm.Location = new Point(DialogWidth - DialogChrome.Pad - confirm.Width, buttonTop);

        Controls.AddRange([titleLabel, messageLabel, close, confirm]);

        if (!string.IsNullOrWhiteSpace(cancelText))
        {
            var cancel = new DialogButton(cancelText, primary: false)
            {
                DialogResult = DialogResult.Cancel,
                Location = new Point(confirm.Left - 12 - DialogChrome.MeasureButtonWidth(cancelText), buttonTop)
            };
            cancel.Size = new Size(DialogChrome.MeasureButtonWidth(cancelText), DialogChrome.ButtonHeight);

            // Wider pair of labels (Confirm / Cancel) should not run past the card.
            if (cancel.Left < DialogChrome.Pad)
                cancel.Left = DialogChrome.Pad;

            Controls.Add(cancel);
            CancelButton = cancel;
        }

        AcceptButton = confirm;

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        ResumeLayout(false);
        PerformLayout();
    }
}

/// <summary>
/// Drop-in replacement for the stock message box so every notice, warning and error uses the
/// same card design. Supports the OK, OK/Cancel and Yes/No button sets used by the app.
/// </summary>
internal static class AppMessageBox
{
    internal static DialogResult Show(
        string message,
        string title,
        MessageBoxButtons buttons = MessageBoxButtons.OK,
        MessageBoxIcon icon = MessageBoxIcon.None) =>
        Show(null, message, title, buttons, icon);

    internal static DialogResult Show(
        IWin32Window? owner,
        string message,
        string title,
        MessageBoxButtons buttons = MessageBoxButtons.OK,
        MessageBoxIcon icon = MessageBoxIcon.None)
    {
        _ = icon;

        var asks = buttons is MessageBoxButtons.YesNo
            or MessageBoxButtons.YesNoCancel
            or MessageBoxButtons.OKCancel;

        var confirmText = asks ? "Confirm" : "OK";

        using var dialog = new ConfirmDialog(
            title,
            title,
            message,
            confirmText,
            asks ? "Cancel" : null);

        var parent = owner as Form ?? Form.ActiveForm;
        var result = parent is null || parent.IsDisposed || ReferenceEquals(parent, dialog)
            ? dialog.ShowDialog()
            : dialog.ShowDialog(parent);

        if (!asks)
            return DialogResult.OK;

        if (result != DialogResult.OK)
            return DialogResult.No;

        return buttons == MessageBoxButtons.OKCancel ? DialogResult.OK : DialogResult.Yes;
    }
}
