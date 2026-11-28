using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using ThreadFlow.WinForms.ApiClient;
using ThreadFlow.WinForms.Theme.Colors;

namespace ThreadFlow.WinForms;

public sealed class CompleteStaffInvitationForm : Form
{
    // All sizes below are written for 96 DPI (100% scaling).
    // AutoScaleMode.Dpi scales them up on higher-DPI screens, so this form keeps the
    // same proportions as the welcome, sign-in, and invitation screens.
    private const int FormWidth = 360;
    private const int FormHeight = 560;
    private const int ContentWidth = 300;
    private const int ContentLeft = (FormWidth - ContentWidth) / 2;
    private const int FieldHeight = 40;
    private const int ButtonHeight = 42;

    private static readonly Pallets Palette = new();
    private static readonly Shades BrandShades = new();
    private static readonly Tints BrandTints = new();

    private static readonly Font TitleFont = new("Segoe UI", 18F, FontStyle.Bold);
    private static readonly Font SubtitleFont = new("Segoe UI", 9.5F);
    private static readonly Font LabelFont = new("Segoe UI Semibold", 9F);
    private static readonly Font FieldFont = new("Segoe UI", 10F);
    private static readonly Font HintFont = new("Segoe UI", 8.5F);
    private static readonly Font PrimaryButtonFont = new("Segoe UI", 10F, FontStyle.Bold);

    private readonly AuthApiClient _api = new();
    private readonly TextBox _email = new();
    private readonly TextBox _tenantCode = new();
    private readonly TextBox _token = new();
    private readonly TextBox _password = new();
    private readonly RoundedButton _complete = MakePrimaryButton("Set password and activate account");

    public CompleteStaffInvitationForm()
    {
        // Apply the shared palette first, then style our own controls the same way the
        // welcome, sign-in, and complete-invitation screens do. (The theme would repaint
        // the button solid blue, which would undo the rounded button styling.)
        Theme.ThreadFlowTheme.Apply(this);

        SuspendLayout();

        BuildForm();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Text = "ThreadFlow";
        BackColor = Palette.BackgroundSecondary.Color;
        ClientSize = new Size(FormWidth, FormHeight);

        // Opens centered on the welcome window, like the other auth screens.
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        DoubleBuffered = true;

        ResumeLayout(false);
        PerformLayout();
    }

    private void BuildForm()
    {
        var logo = new Theme.ThreadFlowLogo(48)
        {
            Location = new Point((FormWidth - 48) / 2, 24)
        };

        var title = MakeCenteredLabel(
            "Join your company", TitleFont, Palette.TextPrimary.Color, y: 80, height: 34);

        var subtitle = MakeCenteredLabel(
            "Activate your employee account", SubtitleFont, Palette.TextSecondary.Color, y: 114, height: 24);

        AddField("Employee email", _email, 160);
        _email.PlaceholderText = "name@company.com";

        AddField("Tenant code", _tenantCode, 234);
        _tenantCode.PlaceholderText = "Provided with your invitation";

        AddField("Invitation setup token", _token, 308);
        _token.PlaceholderText = "Paste your setup token";

        AddField("Password", _password, 382);
        _password.UseSystemPasswordChar = true;
        _password.PlaceholderText = "Create a password";

        var passwordHint = new Label
        {
            Text = "Use 10+ characters with upper and lower case, a number, and a symbol.",
            Font = HintFont,
            ForeColor = Palette.TextSecondary.Color,
            BackColor = Color.Transparent,
            AutoSize = false,
            Location = new Point(ContentLeft, 448),
            Size = new Size(ContentWidth, 32)
        };

        _complete.Location = new Point(ContentLeft, 492);
        _complete.Size = new Size(ContentWidth, ButtonHeight);
        _complete.Click += async (_, _) => await CompleteAsync();

        Controls.AddRange([logo, title, subtitle, passwordHint, _complete]);
    }

    private void AddField(string labelText, TextBox field, int top)
    {
        Controls.Add(MakeFieldLabel(labelText, top));

        // The frame draws the rounded border around the normal TextBox.
        var frame = new RoundedFieldFrame(field)
        {
            Location = new Point(ContentLeft, top + 22),
            Size = new Size(ContentWidth, FieldHeight)
        };

        Controls.Add(frame);
    }

    private static Label MakeFieldLabel(string text, int top) => new()
    {
        Text = text,
        Font = LabelFont,
        ForeColor = Palette.TextPrimary.Color,
        BackColor = Color.Transparent,
        AutoSize = false,
        TextAlign = ContentAlignment.MiddleLeft,
        Location = new Point(ContentLeft, top),
        Size = new Size(ContentWidth, 20)
    };

    private static Label MakeCenteredLabel(string text, Font font, Color color, int y, int height) => new()
    {
        Text = text,
        Font = font,
        ForeColor = color,
        BackColor = Color.Transparent,
        AutoSize = false,
        TextAlign = ContentAlignment.MiddleCenter,
        Location = new Point(ContentLeft, y),
        Size = new Size(ContentWidth, height)
    };

    private async Task CompleteAsync()
    {
        _complete.Enabled = false;
        try
        {
            await _api.CompleteInvitationAsync(new CompleteTenantInvitationDto(
                _email.Text.Trim(), _tenantCode.Text.Trim(), _token.Text.Trim(), _password.Text, null));
            MessageBox.Show("Employee account activated. They can now sign in.", "Invitation complete",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Could not complete invitation",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _complete.Enabled = true;
        }
    }

    // ---- Rounded window corners (Windows 11) ----

    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwcpRound = 2;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        // Windows 11 (build 22000+) supports rounded window corners. Older versions ignore this.
        if (Environment.OSVersion.Version.Build >= 22000)
        {
            var preference = DwmwcpRound;
            _ = DwmSetWindowAttribute(Handle, DwmwaWindowCornerPreference, ref preference, sizeof(int));
        }
    }

    // ---- Control factories ----

    // Filled button: the one main action on the screen.
    private static RoundedButton MakePrimaryButton(string text) => new(
        back: Palette.Primary.Color,
        hoverBack: BrandShades.royalBlue.Color,
        pressedBack: BrandShades.deepBlue.Color,
        textColor: Palette.BackgroundSecondary.Color)
    {
        Text = text,
        Font = PrimaryButtonFont
    };

    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        // Keep the corners from being larger than the shape itself.
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

    // ---- Custom controls ----

    /// <summary>
    /// A normal Button that paints itself with smooth rounded corners.
    /// Works with Click, Enabled, and keyboard focus like any other Button.
    /// </summary>
    private sealed class RoundedButton : Button
    {
        private readonly Color _back;
        private readonly Color _hoverBack;
        private readonly Color _pressedBack;
        private readonly Color _text;

        private bool _hover;
        private bool _pressed;

        public RoundedButton(Color back, Color hoverBack, Color pressedBack, Color textColor)
        {
            _back = back;
            _hoverBack = hoverBack;
            _pressedBack = pressedBack;
            _text = textColor;

            BackColor = back;
            ForeColor = textColor;
            FlatStyle = FlatStyle.Flat;
            Cursor = Cursors.Hand;

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
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
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            if (mevent.Button == MouseButtons.Left)
            {
                _pressed = true;
                Invalidate();
            }

            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var g = pevent.Graphics;
            var parentBack = Parent?.BackColor ?? BackColor;
            g.Clear(parentBack);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var radius = (int)Math.Round(8 * DeviceDpi / 96f);
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

            // A disabled button (while the invitation is being completed) is shown faded.
            var fillColor = !Enabled ? Blend(_back, parentBack, 0.5f)
                : _pressed ? _pressedBack
                : _hover ? _hoverBack
                : _back;
            var textColor = Enabled ? _text : Blend(_text, parentBack, 0.5f);

            using (var path = RoundedRectangle(bounds, radius))
            using (var fill = new SolidBrush(fillColor))
            {
                g.FillPath(fill, path);
            }

            // Keyboard focus cue (shown when the user navigates with Tab).
            if (Focused && ShowFocusCues && Enabled)
            {
                var inset = (int)Math.Round(4 * DeviceDpi / 96f);
                var ringBounds = Rectangle.Inflate(bounds, -inset, -inset);
                using var ring = RoundedRectangle(ringBounds, Math.Max(2, radius - inset));
                using var ringPen = new Pen(Color.FromArgb(150, _text), 1f);
                g.DrawPath(ringPen, ring);
            }

            TextRenderer.DrawText(
                g,
                Text,
                Font,
                ClientRectangle,
                textColor,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.NoPrefix |
                TextFormatFlags.EndEllipsis);
        }

        private static Color Blend(Color from, Color to, float amount) => Color.FromArgb(
            (int)Math.Round(from.R + (to.R - from.R) * amount),
            (int)Math.Round(from.G + (to.G - from.G) * amount),
            (int)Math.Round(from.B + (to.B - from.B) * amount));
    }

    /// <summary>
    /// Wraps a normal TextBox in a rounded border that turns brand blue while the box has focus.
    /// The TextBox keeps working exactly as before (Text, PlaceholderText,
    /// UseSystemPasswordChar...), so existing code that reads the TextBox does not need to change.
    /// </summary>
    private sealed class RoundedFieldFrame : Panel
    {
        private readonly TextBox _inner;
        private bool _focused;

        public RoundedFieldFrame(TextBox inner)
        {
            _inner = inner;

            BackColor = Palette.BackgroundSecondary.Color;
            TabStop = false;

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            inner.BorderStyle = BorderStyle.None;
            // A single-line box is placed by font height, so its line gets room for the tails.
            inner.AutoSize = false;
            inner.BackColor = Palette.BackgroundSecondary.Color;
            inner.ForeColor = Palette.TextPrimary.Color;
            inner.Font = FieldFont;
            inner.GotFocus += (_, _) => SetFocused(true);
            inner.LostFocus += (_, _) => SetFocused(false);

            // Clicking the padding around the text should still put the cursor in the box.
            Click += (_, _) => inner.Focus();

            Controls.Add(inner);
        }

        private void SetFocused(bool focused)
        {
            _focused = focused;
            Invalidate();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            var padding = (int)Math.Round(12 * DeviceDpi / 96f);
            var box = Theme.InputMetrics.SingleLineBox(Width, Height, _inner.Font, DeviceDpi, padding, padding);
            _inner.SetBounds(box.X, box.Y, box.Width, box.Height);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var radius = (int)Math.Round(8 * DeviceDpi / 96f);
            var bounds = new Rectangle(1, 1, Width - 3, Height - 3);

            using var path = RoundedRectangle(bounds, radius);
            using (var fill = new SolidBrush(BackColor))
            {
                g.FillPath(fill, path);
            }

            var borderColor = _focused ? Palette.Primary.Color : BrandTints.powderBlue.Color;
            using var pen = new Pen(borderColor, _focused ? 2f : 1f);
            g.DrawPath(pen, path);
        }
    }
}
