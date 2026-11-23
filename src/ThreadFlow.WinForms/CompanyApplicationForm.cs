using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using ThreadFlow.WinForms.ApiClient;
using ThreadFlow.WinForms.Theme.Colors;

namespace ThreadFlow.WinForms;

public sealed class CompanyApplicationForm : Form
{
    // All sizes below are written for 96 DPI (100% scaling).
    // AutoScaleMode.Dpi scales them up on higher-DPI screens, so this form keeps the
    // same proportions as the welcome and sign-in screens.
    private const int FormWidth = 440;
    private const int FormHeight = 588;
    private const int ContentWidth = 380;
    private const int ContentLeft = (FormWidth - ContentWidth) / 2;
    private const int ColumnGap = 16;
    private const int ColumnWidth = (ContentWidth - ColumnGap) / 2;
    private const int FieldHeight = 40;
    private const int ButtonHeight = 42;

    private static readonly Pallets Palette = new();
    private static readonly Shades BrandShades = new();
    private static readonly Tints BrandTints = new();

    // The color palette has no error color yet, so this red is defined here (same as SignInForm).
    private static readonly Color ErrorColor = Color.FromArgb(185, 28, 28);

    private static readonly Font TitleFont = new("Segoe UI", 18F, FontStyle.Bold);
    private static readonly Font SubtitleFont = new("Segoe UI", 9.5F);
    private static readonly Font LabelFont = new("Segoe UI Semibold", 9F);
    private static readonly Font FieldFont = new("Segoe UI", 10F);
    private static readonly Font HelperFont = new("Segoe UI", 8.5F);
    private static readonly Font PrimaryButtonFont = new("Segoe UI", 10F, FontStyle.Bold);
    private static readonly Font SecondaryButtonFont = new("Segoe UI", 10F);
    private static readonly Font StatusFont = new("Segoe UI", 9F);

    private readonly OnboardingApiClient _api = new();
    private readonly TextBox _company = new();
    private readonly TextBox _contact = new();
    private readonly TextBox _email = new();
    private readonly TextBox _phone = new();
    private readonly ComboBox _plan = new();
    private readonly RoundedButton _submit = MakePrimaryButton("Submit application");
    private readonly RoundedButton _cancel = MakeSecondaryButton("Cancel");
    private readonly Label _status = new();

    public CompanyApplicationForm()
    {
        // Apply the shared palette first, then style our own controls the same way the
        // welcome and sign-in screens do. (The theme would repaint every button solid
        // blue, which would undo the primary / outlined button difference.)
        Theme.ThreadFlowTheme.Apply(this);

        SuspendLayout();

        BuildForm();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Text = "ThreadFlow";
        BackColor = Palette.BackgroundSecondary.Color;
        ClientSize = new Size(FormWidth, FormHeight);

        // Opens centered on the welcome window, like the sign-in screen.
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        DoubleBuffered = true;

        AcceptButton = _submit;
        CancelButton = _cancel;

        ResumeLayout(false);
        PerformLayout();
    }

    private void BuildForm()
    {
        var logo = new Theme.ThreadFlowLogo(48)
        {
            Location = new Point((FormWidth - 48) / 2, 26)
        };

        var title = MakeCenteredLabel(
            "Apply for a company plan", TitleFont, Palette.TextPrimary.Color, y: 86, height: 34);

        var subtitle = MakeCenteredLabel(
            "Tell us about your company to get started", SubtitleFont, Palette.TextSecondary.Color, y: 120, height: 22);

        // Two tidy columns: company details on the left, contact details on the right.
        var rightColumnLeft = ContentLeft + ColumnWidth + ColumnGap;

        AddField("Company name", _company, ContentLeft, 156, ColumnWidth, "Acme Apparel Co.");
        AddField("Contact person", _contact, rightColumnLeft, 156, ColumnWidth, "Maria Santos");
        AddField("Contact email", _email, ContentLeft, 230, ColumnWidth, "name@company.com");
        AddField("Phone (optional)", _phone, rightColumnLeft, 230, ColumnWidth, "+63 912 345 6789");

        // The hint sits under the two fields and can use the full width, so it never
        // gets squeezed into the narrow left column.
        var emailHint = new Label
        {
            Text = "If approved, this will be the Tenant Admin sign-in email.",
            Font = HelperFont,
            ForeColor = Palette.TextSecondary.Color,
            BackColor = Color.Transparent,
            AutoSize = false,
            Location = new Point(ContentLeft, 296),
            Size = new Size(ContentWidth, 34)
        };

        var planLabel = MakeFieldLabel("Requested plan", ContentLeft, 342, ContentWidth);

        // Same drop-down treatment as the currency field on the complete-invitation screen.
        _plan.DropDownStyle = ComboBoxStyle.DropDownList;
        _plan.FlatStyle = FlatStyle.Flat;
        _plan.Items.AddRange(["Micro", "Small", "Medium"]);
        _plan.SelectedIndex = 0;
        _plan.AccessibleName = "Requested plan";

        var planFrame = new RoundedFieldFrame(_plan)
        {
            Location = new Point(ContentLeft, 364),
            Size = new Size(ContentWidth, FieldHeight)
        };

        _submit.Location = new Point(ContentLeft, 428);
        _submit.Size = new Size(ContentWidth, ButtonHeight);
        _submit.Click += Submit_Click;

        _cancel.Location = new Point(ContentLeft, 480);
        _cancel.Size = new Size(ContentWidth, ButtonHeight);
        _cancel.DialogResult = DialogResult.Cancel;

        _status.Location = new Point(ContentLeft, 532);
        _status.Size = new Size(ContentWidth, 44);
        _status.Font = StatusFont;
        _status.ForeColor = Palette.TextSecondary.Color;
        _status.BackColor = Color.Transparent;
        _status.TextAlign = ContentAlignment.TopCenter;

        Controls.AddRange([
            logo, title, subtitle, emailHint, planLabel, planFrame,
            _submit, _cancel, _status
        ]);

        UpdateSubmitState();
    }

    private void AddField(string labelText, TextBox field, int x, int top, int width, string placeholder)
    {
        Controls.Add(MakeFieldLabel(labelText, x, top, width));
        AddTextBoxFrame(field, placeholder, x, top + 22, width);
        field.TextChanged += (_, _) => UpdateSubmitState();
    }

    private void AddTextBoxFrame(TextBox field, string placeholder, int x, int top, int width)
    {
        field.PlaceholderText = placeholder;
        var frame = new RoundedFieldFrame(field)
        {
            Location = new Point(x, top),
            Size = new Size(width, FieldHeight)
        };
        Controls.Add(frame);
    }

    private static Label MakeFieldLabel(string text, int x, int top, int width) => new()
    {
        Text = text,
        Font = LabelFont,
        ForeColor = Palette.TextPrimary.Color,
        BackColor = Color.Transparent,
        AutoSize = false,
        TextAlign = ContentAlignment.MiddleLeft,
        Location = new Point(x, top),
        Size = new Size(width, 20)
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

    private void UpdateSubmitState()
    {
        _submit.Enabled =
            !string.IsNullOrWhiteSpace(_company.Text) &&
            !string.IsNullOrWhiteSpace(_contact.Text) &&
            !string.IsNullOrWhiteSpace(_email.Text);
    }

    private async void Submit_Click(object? sender, EventArgs e)
    {
        _submit.Enabled = false;
        _status.ForeColor = Palette.TextSecondary.Color;
        _status.Text = "Submitting your application...";

        try
        {
            var result = await _api.SubmitApplicationAsync(new SubmitCompanyApplicationDto(
                _company.Text.Trim(),
                _contact.Text.Trim(),
                _email.Text.Trim(),
                string.IsNullOrWhiteSpace(_phone.Text) ? null : _phone.Text.Trim(),
                _plan.Text));

            _status.ForeColor = Palette.TextSecondary.Color;
            _status.Text = $"Application {result.Id} is pending review.";
            MessageBox.Show(
                $"Application received. Status: {result.Status}.\nThe Platform Admin will review your requested plan.",
                "Application submitted",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception exception)
        {
            _status.ForeColor = ErrorColor;
            _status.Text = "We couldn't submit your application. Please try again.";
            MessageBox.Show(exception.Message, "Application failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (!IsDisposed)
                UpdateSubmitState();
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
        textColor: Palette.BackgroundSecondary.Color,
        border: null)
    {
        Text = text,
        Font = PrimaryButtonFont
    };

    // Outlined button: a real option, but quieter than the main action.
    private static RoundedButton MakeSecondaryButton(string text) => new(
        back: Palette.BackgroundSecondary.Color,
        hoverBack: Palette.BackgroundPrimary.Color,
        pressedBack: BrandTints.iceBlue.Color,
        textColor: Palette.Primary.Color,
        border: Palette.Primary.Color)
    {
        Text = text,
        Font = SecondaryButtonFont
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
    /// Works with Click, AcceptButton, CancelButton, Enabled, and keyboard focus like any other Button.
    /// </summary>
    private sealed class RoundedButton : Button
    {
        private readonly Color _back;
        private readonly Color _hoverBack;
        private readonly Color _pressedBack;
        private readonly Color _text;
        private readonly Color? _border;

        private bool _hover;
        private bool _pressed;

        public RoundedButton(Color back, Color hoverBack, Color pressedBack, Color textColor, Color? border)
        {
            _back = back;
            _hoverBack = hoverBack;
            _pressedBack = pressedBack;
            _text = textColor;
            _border = border;

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

            // A disabled button (for example while the application is submitting) is shown faded.
            var fillColor = !Enabled ? Blend(_back, parentBack, 0.5f)
                : _pressed ? _pressedBack
                : _hover ? _hoverBack
                : _back;
            var textColor = Enabled ? _text : Blend(_text, parentBack, 0.5f);

            using (var path = RoundedRectangle(bounds, radius))
            using (var fill = new SolidBrush(fillColor))
            {
                g.FillPath(fill, path);

                if (_border is { } borderColor)
                {
                    var edge = Enabled ? borderColor : Blend(borderColor, parentBack, 0.5f);
                    using var pen = new Pen(edge, 1f);
                    g.DrawPath(pen, path);
                }
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
    /// Wraps a normal field control (TextBox or ComboBox) in a rounded border that turns
    /// brand blue while the field has focus. The inner control keeps working exactly as
    /// before, so existing code that reads the control does not need to change.
    /// </summary>
    private sealed class RoundedFieldFrame : Panel
    {
        private readonly Control _inner;
        private bool _focused;

        public RoundedFieldFrame(Control inner)
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

            switch (inner)
            {
                case TextBoxBase textBox:
                    textBox.BorderStyle = BorderStyle.None;
                    break;

                // The native combo always draws its own square border. Clipping one pixel
                // off keeps only the rounded frame border visible.
                case ComboBox combo:
                    combo.FlatStyle = FlatStyle.Flat;
                    break;
            }

            inner.BackColor = BackColor;
            inner.ForeColor = Palette.TextPrimary.Color;
            inner.Font = FieldFont;
            inner.GotFocus += (_, _) => SetFocused(true);
            inner.LostFocus += (_, _) => SetFocused(false);

            // Clicking the padding around the text should still put the cursor in the field.
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
            _inner.Width = Math.Max(10, Width - padding * 2);
            _inner.Location = new Point(padding, Math.Max(0, (Height - _inner.Height) / 2));

            if (_inner is ComboBox combo)
            {
                combo.Region?.Dispose();
                combo.Region = new Region(RoundedRectangle(
                    new Rectangle(1, 1, Math.Max(4, combo.Width - 2), Math.Max(4, combo.Height - 2)),
                    (int)Math.Round(4 * DeviceDpi / 96f)));
            }
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
