using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using ThreadFlow.WinForms.ApiClient;
using ThreadFlow.WinForms.Theme.Colors;

namespace ThreadFlow.WinForms;

public sealed class SignInForm : Form
{
    // All sizes below are written for 96 DPI (100% scaling).
    // AutoScaleMode.Dpi scales them up on higher-DPI screens.
    private const int FormWidth = 360;
    private const int FormHeight = 420;
    private const int ContentWidth = 300;
    private const int ContentLeft = (FormWidth - ContentWidth) / 2;
    private const int FieldHeight = 40;
    private const int ButtonHeight = 42;

    private static readonly Pallets Palette = new();
    private static readonly Shades BrandShades = new();
    private static readonly Tints BrandTints = new();

    // The color palette has no error color yet, so this red is defined here.
    private static readonly Color ErrorColor = Color.FromArgb(185, 28, 28);

    private static readonly Font TitleFont = new("Segoe UI", 18F, FontStyle.Bold);
    private static readonly Font SubtitleFont = new("Segoe UI", 9.5F);
    private static readonly Font LabelFont = new("Segoe UI Semibold", 9F);
    private static readonly Font StatusFont = new("Segoe UI", 9F);
    private static readonly Font FieldFont = new("Segoe UI", 10F);
    private static readonly Font PrimaryButtonFont = new("Segoe UI", 10F, FontStyle.Bold);

    private readonly AuthApiClient _apiClient = new();
    private readonly TextBox _email = new();
    private readonly TextBox _password = new();
    private readonly RoundedButton _signIn = MakePrimaryButton("Sign in");
    private readonly Label _status = new();

    public SignInForm()
    {
        // Apply the shared theme to the form first, then style our own controls.
        // (The theme repaints every button solid blue and resizes fonts, which would
        // undo the rounded button and field styling this screen relies on.)
        Theme.ThreadFlowTheme.Apply(this);

        SuspendLayout();

        BuildForm();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Text = "ThreadFlow";
        BackColor = Palette.BackgroundSecondary.Color;
        ClientSize = new Size(FormWidth, FormHeight);

        // Opens centered on the welcome window, so the screens swap in the same spot.
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

        var title = new Label
        {
            Text = "Welcome back",
            Font = TitleFont,
            ForeColor = Palette.TextPrimary.Color,
            BackColor = Color.Transparent,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(ContentLeft, 80),
            Size = new Size(ContentWidth, 34)
        };

        var subtitle = new Label
        {
            Text = "Sign in to your ThreadFlow account",
            Font = SubtitleFont,
            ForeColor = Palette.TextSecondary.Color,
            BackColor = Color.Transparent,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(ContentLeft, 114),
            Size = new Size(ContentWidth, 24)
        };

        AddField("Email", _email, 160);
        _email.PlaceholderText = "name@company.com";

        AddField("Password", _password, 234);
        _password.UseSystemPasswordChar = true;
        _password.PlaceholderText = "Enter your password";

        _signIn.Location = new Point(ContentLeft, 316);
        _signIn.Size = new Size(ContentWidth, ButtonHeight);
        _signIn.Click += SignIn_Click;

        _status.Location = new Point(ContentLeft, 366);
        _status.Size = new Size(ContentWidth, 36);
        _status.Font = StatusFont;
        _status.ForeColor = Palette.TextSecondary.Color;
        _status.BackColor = Color.Transparent;
        _status.TextAlign = ContentAlignment.TopCenter;

        Controls.AddRange([logo, title, subtitle, _signIn, _status]);
        AcceptButton = _signIn;
    }

    private void AddField(string labelText, TextBox field, int top)
    {
        var label = new Label
        {
            Text = labelText,
            Font = LabelFont,
            ForeColor = Palette.TextPrimary.Color,
            BackColor = Color.Transparent,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(ContentLeft, top),
            Size = new Size(ContentWidth, 20)
        };

        // The frame draws the rounded border around the normal TextBox.
        var frame = new RoundedFieldFrame(field)
        {
            Location = new Point(ContentLeft, top + 22),
            Size = new Size(ContentWidth, FieldHeight)
        };

        Controls.AddRange([label, frame]);
    }

    private async void SignIn_Click(object? sender, EventArgs e)
    {
        _signIn.Enabled = false;
        _status.ForeColor = Palette.TextSecondary.Color;
        _status.Text = "Signing in...";

        try
        {
            var login = await _apiClient.LoginAsync(
                _email.Text.Trim(),
                _password.Text,
                tenantCode: null);

            ApiSession.Start(login);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (HttpRequestException exception)
        {
            _status.ForeColor = ErrorColor;
            _status.Text = exception.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? "Email, password, or tenant code is incorrect."
                : "Could not sign in.";

            MessageBox.Show(
                exception.Message,
                "Sign-in failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception exception)
        {
            _status.ForeColor = ErrorColor;
            _status.Text = "Could not connect to the API.";
            MessageBox.Show(
                exception.Message,
                "Sign-in failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _signIn.Enabled = true;
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
    /// Works with Click, AcceptButton, Enabled, and keyboard focus like any other Button.
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

            // A disabled button (for example while signing in) is shown faded.
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
    /// Wraps a normal TextBox in a rounded border that turns brand blue while the box has focus.
    /// The TextBox keeps working exactly as before (Text, PlaceholderText, UseSystemPasswordChar...),
    /// so existing code that reads the TextBox does not need to change.
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