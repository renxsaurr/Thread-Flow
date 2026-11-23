using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using ThreadFlow.WinForms.Theme.Colors;

namespace ThreadFlow.WinForms;

public sealed class WelcomeForm : Form
{
    // All sizes below are written for 96 DPI (100% scaling).
    // AutoScaleMode.Dpi scales them up on higher-DPI screens, so the layout
    // keeps its proportions instead of the text outgrowing the controls.
    private const int FormWidth = 360;
    private const int FormHeight = 416;
    private const int ContentWidth = 300;
    private const int ContentLeft = (FormWidth - ContentWidth) / 2;
    private const int ButtonHeight = 42;

    private static readonly Pallets Palette = new();
    private static readonly Shades BrandShades = new();
    private static readonly Tints BrandTints = new();

    private static readonly Font TitleFont = new("Segoe UI", 18F, FontStyle.Bold);
    private static readonly Font TaglineFont = new("Segoe UI", 9.5F);
    private static readonly Font PrimaryButtonFont = new("Segoe UI", 10F, FontStyle.Bold);
    private static readonly Font SecondaryButtonFont = new("Segoe UI", 10F);
    private static readonly Font HintFont = new("Segoe UI", 9F);
    private static readonly Font LinkFont = new("Segoe UI", 9F);

    public WelcomeForm()
    {
        // Apply the shared theme to the form first, then style our own controls.
        // (The theme repaints every button solid blue, which would undo the
        // primary / outlined difference this screen relies on.)
        Theme.ThreadFlowTheme.Apply(this);

        SuspendLayout();

        var logo = new Theme.ThreadFlowLogo(56)
        {
            Location = new Point((FormWidth - 56) / 2, 28)
        };

        var title = MakeCenteredLabel(
            "ThreadFlow", TitleFont, Palette.TextPrimary.Color, y: 92, height: 36);

        var subtitle = MakeCenteredLabel(
            "Run your clothing business in one place", TaglineFont, Palette.TextSecondary.Color, y: 128, height: 24);

        // Each button below makes this window step aside (Opacity = 0) while the next screen
        // is open, and brings it back (Opacity = 1) when the user closes that screen.
        // Opacity is used instead of Hide() because hiding a window that was opened with
        // ShowDialog can make Windows treat it as closed.
        var signIn = MakePrimaryButton("Sign in", 180);
        signIn.Click += (_, _) =>
        {
            using var form = new SignInForm();
            Opacity = 0;
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                Opacity = 1;
            }
        };

        var apply = MakeSecondaryButton("Apply for a company plan", 232);
        apply.Click += (_, _) =>
        {
            using var form = new CompanyApplicationForm();
            Opacity = 0;
            form.ShowDialog(this);
            Opacity = 1;
        };

        var divider = new Panel
        {
            BackColor = BrandTints.iceBlue.Color,
            Location = new Point(ContentLeft, 300),
            Size = new Size(ContentWidth, 1)
        };

        var invitationHint = MakeCenteredLabel(
            "Have an invitation?", HintFont, Palette.TextSecondary.Color, y: 314, height: 20);

        var invitation = MakeLink("I'm setting up my company", 338);
        invitation.LinkClicked += (_, _) =>
        {
            using var form = new CompleteInvitationForm();
            Opacity = 0;
            form.ShowDialog(this);
            Opacity = 1;
        };

        var staffInvitation = MakeLink("I'm joining my company", 362);
        staffInvitation.LinkClicked += (_, _) =>
        {
            using var form = new CompleteStaffInvitationForm();
            Opacity = 0;
            form.ShowDialog(this);
            Opacity = 1;
        };

        Controls.AddRange([logo, title, subtitle, signIn, apply, divider, invitationHint, invitation, staffInvitation]);

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Text = "ThreadFlow";
        BackColor = Palette.BackgroundSecondary.Color;
        ClientSize = new Size(FormWidth, FormHeight);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        DoubleBuffered = true;

        // Pressing Enter triggers Sign in, since it is the main action on this screen.
        AcceptButton = signIn;

        ResumeLayout(false);
        PerformLayout();
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
    private static RoundedButton MakePrimaryButton(string text, int y) => new(
        back: Palette.Primary.Color,
        hoverBack: BrandShades.royalBlue.Color,
        pressedBack: BrandShades.deepBlue.Color,
        textColor: Palette.BackgroundSecondary.Color,
        border: null)
    {
        Text = text,
        Font = PrimaryButtonFont,
        Location = new Point(ContentLeft, y),
        Size = new Size(ContentWidth, ButtonHeight)
    };

    // Outlined button: a real option, but quieter than the main action.
    private static RoundedButton MakeSecondaryButton(string text, int y) => new(
        back: Palette.BackgroundSecondary.Color,
        hoverBack: Palette.BackgroundPrimary.Color,
        pressedBack: BrandTints.iceBlue.Color,
        textColor: Palette.Primary.Color,
        border: Palette.Primary.Color)
    {
        Text = text,
        Font = SecondaryButtonFont,
        Location = new Point(ContentLeft, y),
        Size = new Size(ContentWidth, ButtonHeight)
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

    private static LinkLabel MakeLink(string text, int y) => new()
    {
        Text = text,
        Font = LinkFont,
        BackColor = Color.Transparent,
        AutoSize = false,
        TextAlign = ContentAlignment.MiddleCenter,
        Location = new Point(ContentLeft, y),
        Size = new Size(ContentWidth, 24),
        LinkColor = Palette.Primary.Color,
        ActiveLinkColor = BrandShades.deepBlue.Color,
        VisitedLinkColor = Palette.Primary.Color,
        LinkBehavior = LinkBehavior.HoverUnderline
    };

    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
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
    /// Works with Click, AcceptButton, and keyboard focus like any other Button.
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

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var g = pevent.Graphics;
            g.Clear(Parent?.BackColor ?? BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var radius = (int)Math.Round(8 * DeviceDpi / 96f);
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            var fillColor = _pressed ? _pressedBack : _hover ? _hoverBack : _back;

            using (var path = RoundedRectangle(bounds, radius))
            using (var fill = new SolidBrush(fillColor))
            {
                g.FillPath(fill, path);

                if (_border is { } borderColor)
                {
                    using var pen = new Pen(borderColor, 1f);
                    g.DrawPath(pen, path);
                }
            }

            // Keyboard focus cue (shown when the user navigates with Tab).
            if (Focused && ShowFocusCues)
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
                _text,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.NoPrefix |
                TextFormatFlags.EndEllipsis);
        }
    }
}