using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using ThreadFlow.WinForms.ApiClient;
using ThreadFlow.WinForms.Theme.Colors;

namespace ThreadFlow.WinForms;

public sealed class CompleteInvitationForm : Form
{
    // All sizes below are written for 96 DPI (100% scaling).
    // AutoScaleMode.Dpi scales them up on higher-DPI screens, so the fields and text keep
    // the same proportions as the welcome and sign-in screens.
    private const int FormWidth = 960;
    private const int FormHeight = 680;
    private const int BrandPanelWidth = 280;
    private const int ContentPadding = 36;
    private const int ColumnGap = 20;
    private const int FieldHeight = 40;
    private const int ContentHeight = 950;

    private static readonly Pallets Palette = new();
    private static readonly Shades BrandShades = new();
    private static readonly Tints BrandTints = new();

    // The color palette has no error color yet, so this red is defined here (same as SignInForm).
    private static readonly Color ErrorColor = Color.FromArgb(185, 28, 28);

    // Colors for the artwork on the brand panel. They are only used for that decoration,
    // so they live here instead of in the shared palette.
    private static readonly Color ArtOrange = Color.FromArgb(245, 146, 50);
    private static readonly Color ArtYellow = Color.FromArgb(255, 217, 124);
    private static readonly Color ArtSky = Color.FromArgb(53, 176, 230);
    private static readonly Color ArtTeal = Color.FromArgb(64, 184, 200);
    private static readonly Color ArtLightTeal = Color.FromArgb(130, 220, 228);
    private static readonly Color ArtPurple = Color.FromArgb(142, 47, 208);
    private static readonly Color ArtLilac = Color.FromArgb(176, 108, 228);

    private static readonly Font BrandTitleFont = new("Segoe UI", 18F, FontStyle.Bold);
    private static readonly Font BrandSubtitleFont = new("Segoe UI", 9.5F);
    private static readonly Font SectionFont = new("Segoe UI", 11F, FontStyle.Bold);
    private static readonly Font LabelFont = new("Segoe UI Semibold", 9F);
    private static readonly Font FieldFont = new("Segoe UI", 10F);
    private static readonly Font HelperFont = new("Segoe UI", 8.5F);
    private static readonly Font PrimaryButtonFont = new("Segoe UI", 10F, FontStyle.Bold);
    private static readonly Font SecondaryButtonFont = new("Segoe UI", 10F);
    private static readonly Font StatusFont = new("Segoe UI", 9F);

    private readonly AuthApiClient _api = new();
    private readonly BrandArtPanel _brandPanel = new();
    private readonly Panel _scrollPanel = new();
    private readonly Panel _contentPanel = new();
    private readonly Label _status = new();

    private readonly TextBox _email = new();
    private readonly TextBox _tenantCode = new();
    private readonly TextBox _token = new();
    private readonly TextBox _password = new();
    private readonly TextBox _legalName = new();
    private readonly TextBox _contactEmail = new();
    private readonly TextBox _address = new();
    private readonly TextBox _taxId = new();
    private readonly TextBox _phone = new();
    private readonly NumericUpDown _taxRate = new();
    private readonly TextBox _receiptHeader = new();
    private readonly TextBox _receiptFooter = new();
    private readonly ComboBox _currency = new();
    private readonly RoundedButton _complete = MakePrimaryButton("Complete setup");
    private readonly RoundedButton _back = MakeSecondaryButton("Back to start");

    private readonly Dictionary<TextBox, FieldParts> _textFields = new();
    private readonly Dictionary<Control, (Label Label, Panel Frame)> _otherFields = new();
    private readonly List<(Label Label, int Y)> _sectionHeadings = [];
    private readonly List<Panel> _sectionDividers = [];
    private readonly Label _passwordHint = new();
    private readonly Label _brandTitle = new();
    private readonly Label _brandSubtitle = new();
    private readonly Theme.ThreadFlowLogo _logo = new(56);

    public CompleteInvitationForm()
    {
        // Apply the shared palette first, then style our own controls the same way the
        // welcome and sign-in screens do. (The theme would repaint every button solid
        // blue, which would undo the primary / outlined button difference.)
        Theme.ThreadFlowTheme.Apply(this);
        SuspendLayout();

        BuildLayout();
        LayoutBrandPanel();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Text = "ThreadFlow";
        ClientSize = new Size(FormWidth, FormHeight);
        MinimumSize = new Size(860, 600);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Palette.BackgroundSecondary.Color;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        DoubleBuffered = true;

        ResumeLayout(true);
    }

    private void BuildLayout()
    {
        _brandPanel.Dock = DockStyle.Left;
        _brandPanel.Width = BrandPanelWidth;
        _brandPanel.BackColor = Palette.BackgroundSecondary.Color;
        _brandPanel.Resize += (_, _) => LayoutBrandPanel();

        _scrollPanel.Dock = DockStyle.Fill;
        _scrollPanel.AutoScroll = true;
        _scrollPanel.BackColor = Palette.BackgroundSecondary.Color;
        _scrollPanel.Resize += (_, _) => LayoutContent();

        _contentPanel.Location = Point.Empty;
        _contentPanel.Height = ContentHeight;
        _contentPanel.BackColor = Palette.BackgroundSecondary.Color;
        _scrollPanel.Controls.Add(_contentPanel);

        BuildBrandPanel();
        BuildFormContent();
        Controls.Add(_scrollPanel);
        Controls.Add(_brandPanel);

        LayoutContent();
    }

    private void BuildBrandPanel()
    {
        _logo.BackColor = _brandPanel.BackColor;

        _brandTitle.Text = "Set up your\ncompany";
        _brandTitle.Font = BrandTitleFont;
        _brandTitle.ForeColor = Palette.TextPrimary.Color;
        _brandTitle.BackColor = Color.Transparent;
        _brandTitle.TextAlign = ContentAlignment.MiddleCenter;
        _brandTitle.AutoSize = false;
        _brandTitle.Size = new Size(240, 62);

        _brandSubtitle.Text = "Finish your Tenant Admin\naccount to start using\nThreadFlow.";
        _brandSubtitle.Font = BrandSubtitleFont;
        _brandSubtitle.ForeColor = Palette.TextSecondary.Color;
        _brandSubtitle.BackColor = Color.Transparent;
        _brandSubtitle.TextAlign = ContentAlignment.TopCenter;
        _brandSubtitle.AutoSize = false;
        _brandSubtitle.Size = new Size(240, 66);

        _brandPanel.Controls.AddRange([_logo, _brandTitle, _brandSubtitle]);
    }

    private void LayoutBrandPanel()
    {
        var center = _brandPanel.ClientSize.Width / 2;
        var gapAfterLogo = Scale(28);
        var gapAfterTitle = Scale(14);
        var groupHeight = _logo.Height + gapAfterLogo + _brandTitle.Height + gapAfterTitle + _brandSubtitle.Height;
        var top = Math.Max(Scale(40), (_brandPanel.ClientSize.Height - groupHeight) / 2);

        _logo.Location = new Point(center - _logo.Width / 2, top);
        _brandTitle.Location = new Point(center - _brandTitle.Width / 2, _logo.Bottom + gapAfterLogo);
        _brandSubtitle.Location = new Point(center - _brandSubtitle.Width / 2, _brandTitle.Bottom + gapAfterTitle);

        // The colorful banners only use the space above the logo and below the subtitle,
        // so they never run behind the text and the middle of the panel stays white.
        _brandPanel.SetClearance(_logo.Top, _brandPanel.ClientSize.Height - _brandSubtitle.Bottom);
    }

    private void BuildFormContent()
    {
        AddSectionHeading("Your account", 40);
        AddTextField("Tenant Admin email", _email, "name@company.com", 78);
        AddTextField("Tenant code", _tenantCode, "Provided with your invitation", 78, secondColumn: true);
        AddTextField("Invitation setup token", _token, "Paste your setup token", 152);
        AddTextField("Password", _password, "Create a password", 152, secondColumn: true, password: true);

        _passwordHint.Text = "Use 10+ characters with upper and lower case, a number, and a symbol.";
        _passwordHint.Font = HelperFont;
        _passwordHint.ForeColor = Palette.TextSecondary.Color;
        _passwordHint.BackColor = Color.Transparent;
        _passwordHint.AutoSize = false;
        _passwordHint.Height = 36;
        _contentPanel.Controls.Add(_passwordHint);

        AddSectionDivider(270);
        AddSectionHeading("Company details", 286);
        AddTextField("Legal company name", _legalName, "Registered company name", 324);
        AddTextField("Company contact email", _contactEmail, "hello@company.com", 324, secondColumn: true);
        AddTextField("Business address", _address, "Street, city, province", 398, fullWidth: true);
        AddTextField("Tax ID (optional)", _taxId, "Tax identification number", 472);
        AddTextField("Phone (optional)", _phone, "+63 912 345 6789", 472, secondColumn: true);

        AddSectionDivider(560);
        AddSectionHeading("Tax and receipts", 576);
        AddOtherField("Tax rate", _taxRate, 614);
        AddOtherField("Currency", _currency, 614, secondColumn: true);
        AddTextField("Receipt header (optional)", _receiptHeader, "Company name shown on receipts", 688);
        AddTextField("Receipt footer (optional)", _receiptFooter, "Thank you message", 688, secondColumn: true);

        _complete.Click += Complete_Click;
        _contentPanel.Controls.Add(_complete);

        _back.Click += (_, _) => Close();
        _contentPanel.Controls.Add(_back);

        _status.Font = StatusFont;
        _status.ForeColor = Palette.TextSecondary.Color;
        _status.BackColor = Color.Transparent;
        _status.TextAlign = ContentAlignment.MiddleCenter;
        _status.AutoSize = false;
        _status.Height = 36;
        _contentPanel.Controls.Add(_status);

        _currency.DropDownStyle = ComboBoxStyle.DropDownList;
        _currency.FlatStyle = FlatStyle.Flat;
        _currency.Items.AddRange(["PHP", "USD"]);
        _currency.SelectedIndex = 0;

        _taxRate.DecimalPlaces = 2;
        _taxRate.Minimum = 0;
        _taxRate.Maximum = 100;
        _taxRate.Value = 12;
        _taxRate.Font = FieldFont;
        _taxRate.BorderStyle = BorderStyle.None;
        _taxRate.TextAlign = HorizontalAlignment.Left;

        AcceptButton = _complete;
        CancelButton = _back;
    }

    private void AddSectionHeading(string text, int y)
    {
        var label = new Label
        {
            Text = text,
            Font = SectionFont,
            ForeColor = Palette.Primary.Color,
            BackColor = Color.Transparent,
            AutoSize = false,
            Height = 24
        };
        _sectionHeadings.Add((label, y));
        _contentPanel.Controls.Add(label);
    }

    private void AddSectionDivider(int y)
    {
        var divider = new Panel { Height = 1, BackColor = BrandTints.iceBlue.Color };
        _sectionDividers.Add(divider);
        _contentPanel.Controls.Add(divider);
        divider.Tag = y;
    }

    private void AddTextField(
        string labelText,
        TextBox field,
        string placeholder,
        int y,
        bool readOnly = false,
        bool secondColumn = false,
        bool fullWidth = false,
        bool password = false)
    {
        field.PlaceholderText = placeholder;
        field.ReadOnly = readOnly;
        field.UseSystemPasswordChar = password;

        var background = readOnly
            ? BrandTints.veryPaleBlue.Color
            : Palette.BackgroundSecondary.Color;

        var frame = new RoundedFieldFrame(field, background)
        {
            Height = FieldHeight
        };

        var label = MakeFieldLabel(labelText);
        _textFields[field] = new FieldParts(label, frame, y, secondColumn, fullWidth);
        _contentPanel.Controls.Add(label);
        _contentPanel.Controls.Add(frame);
    }

    private void AddOtherField(string labelText, Control control, int y, bool secondColumn = false)
    {
        var label = MakeFieldLabel(labelText);
        var frame = new RoundedFieldFrame(control) { Height = FieldHeight };

        _otherFields[control] = (label, frame);
        control.Tag = new FieldLayout(y, secondColumn);
        _contentPanel.Controls.Add(label);
        _contentPanel.Controls.Add(frame);
    }

    private void LayoutContent()
    {
        if (_scrollPanel.ClientSize.Width <= 0 || _contentPanel.IsDisposed)
            return;

        var contentWidth = Math.Max(Scale(560), _scrollPanel.ClientSize.Width - 1);
        _contentPanel.Width = contentWidth;
        _contentPanel.Height = Scale(ContentHeight);

        var padding = Scale(ContentPadding);
        var left = padding;
        var available = contentWidth - padding * 2;
        var gap = Scale(ColumnGap);
        var columnWidth = Math.Max(Scale(220), (available - gap) / 2);

        foreach (var (label, y) in _sectionHeadings)
        {
            label.Location = new Point(left, Scale(y));
            label.Width = available;
            label.Height = Scale(24);
        }

        foreach (var divider in _sectionDividers)
        {
            var y = (int)divider.Tag!;
            divider.Location = new Point(left, Scale(y));
            divider.Width = available;
        }

        foreach (var (field, parts) in _textFields)
        {
            var x = parts.FullWidth || !parts.SecondColumn
                ? left
                : left + columnWidth + gap;
            var width = parts.FullWidth ? available : columnWidth;
            parts.Label.Location = new Point(x, Scale(parts.Y));
            parts.Label.Width = width;
            parts.Label.Height = Scale(20);
            parts.Frame.Location = new Point(x, Scale(parts.Y + 22));
            parts.Frame.Width = width;
            parts.Frame.Height = Scale(FieldHeight);
        }

        _passwordHint.Location = new Point(left + columnWidth + gap, Scale(220));
        _passwordHint.Width = columnWidth;
        _passwordHint.Height = Scale(36);

        foreach (var (control, parts) in _otherFields)
        {
            var layout = (FieldLayout)control.Tag!;
            var x = layout.SecondColumn ? left + columnWidth + gap : left;
            parts.Label.Location = new Point(x, Scale(layout.Y));
            parts.Label.Width = columnWidth;
            parts.Label.Height = Scale(20);
            parts.Frame.Location = new Point(x, Scale(layout.Y + 22));
            parts.Frame.Width = columnWidth;
            parts.Frame.Height = Scale(FieldHeight);
        }

        _complete.Location = new Point(left, Scale(786));
        _complete.Size = new Size(available, Scale(42));
        _back.Location = new Point(left, Scale(838));
        _back.Size = new Size(available, Scale(42));
        _status.Location = new Point(left, Scale(888));
        _status.Size = new Size(available, Scale(36));
    }

    private static Label MakeFieldLabel(string text) => new()
    {
        Text = text,
        Font = LabelFont,
        ForeColor = Palette.TextPrimary.Color,
        BackColor = Color.Transparent,
        AutoSize = false,
        Height = 20
    };

    private int Scale(int value) => (int)Math.Round(value * DeviceDpi / 96f);

    private async void Complete_Click(object? sender, EventArgs e)
    {
        _complete.Enabled = false;
        _status.ForeColor = Palette.TextSecondary.Color;
        _status.Text = "Completing your company setup…";
        try
        {
            await _api.CompleteInvitationAsync(new CompleteTenantInvitationDto(
                _email.Text.Trim(),
                _tenantCode.Text.Trim(),
                _token.Text.Trim(),
                _password.Text,
                new TenantCompanySettingsDto(
                    _legalName.Text.Trim(),
                    _address.Text.Trim(),
                    EmptyToNull(_taxId.Text),
                    _contactEmail.Text.Trim(),
                    EmptyToNull(_phone.Text),
                    _taxRate.Value,
                    EmptyToNull(_receiptHeader.Text),
                    EmptyToNull(_receiptFooter.Text),
                    _currency.Text.Trim().ToUpperInvariant())));
            MessageBox.Show(
                "Account and company setup are complete. The Tenant Admin can now sign in using their email and password.",
                "Setup complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        catch (Exception exception)
        {
            _status.ForeColor = ErrorColor;
            _status.Text = exception.Message;
        }
        finally
        {
            _complete.Enabled = true;
        }
    }

    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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

            // A disabled button (for example while the setup is being submitted) is shown faded.
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
    /// Wraps a normal field control (TextBox, NumericUpDown, ComboBox) in a rounded border
    /// that turns brand blue while the field has focus. The inner control keeps working
    /// exactly as before, so existing code that reads the control does not need to change.
    /// </summary>
    private sealed class RoundedFieldFrame : Panel
    {
        private readonly Control _inner;
        private bool _focused;

        public RoundedFieldFrame(Control inner, Color? fill = null)
        {
            _inner = inner;

            BackColor = fill ?? Palette.BackgroundSecondary.Color;
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

                case NumericUpDown numeric:
                    numeric.BorderStyle = BorderStyle.None;
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

    /// <summary>
    /// The left brand panel. Besides holding the logo and text, it paints a soft, colorful banner
    /// (gradient plus overlapping translucent shapes) along the top and a smaller one along the bottom.
    /// The area in between stays plain white so the logo and text keep a clean background.
    /// </summary>
    private sealed class BrandArtPanel : Panel
    {
        // Tallest the banners are allowed to get (96 DPI units), and how much of the free
        // space above / below the logo group they may take.
        private const int MaxTopBannerHeight = 170;
        private const int MaxBottomBannerHeight = 100;
        private const float TopSpaceShare = 0.80f;
        private const float BottomSpaceShare = 0.70f;
        private const int MinBannerHeight = 24;

        private int _clearTop;
        private int _clearBottom;

        public BrandArtPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        /// <summary>Free pixels above the first control and below the last control.</summary>
        public void SetClearance(int top, int bottom)
        {
            if (top == _clearTop && bottom == _clearBottom)
                return;

            _clearTop = top;
            _clearBottom = bottom;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var scale = DeviceDpi / 96f;
            var topHeight = (int)Math.Min(MaxTopBannerHeight * scale, _clearTop * TopSpaceShare);
            var bottomHeight = (int)Math.Min(MaxBottomBannerHeight * scale, _clearBottom * BottomSpaceShare);

            if (topHeight >= MinBannerHeight)
                DrawBanner(g, Width, topHeight, BackColor);

            if (bottomHeight >= MinBannerHeight)
            {
                // The bottom banner is the top one turned 180 degrees, so the colors and
                // the wave run the other way and the panel feels balanced.
                var state = g.Save();
                g.TranslateTransform(Width, Height);
                g.ScaleTransform(-1f, -1f);
                DrawBanner(g, Width, bottomHeight, BackColor);
                g.Restore(state);
            }
        }

        // Draws one banner into the rectangle (0, 0, width, height) of the current transform.
        private static void DrawBanner(Graphics g, int width, int height, Color cover)
        {
            if (width <= 0 || height <= 0)
                return;

            var area = new Rectangle(0, 0, width, height);
            var clipState = g.Save();
            g.SetClip(area);

            // 1. Orange -> yellow -> sky -> brand blue -> purple gradient.
            using (var gradient = new LinearGradientBrush(area, ArtOrange, ArtPurple, LinearGradientMode.Horizontal))
            {
                gradient.InterpolationColors = new ColorBlend
                {
                    Positions = [0f, 0.28f, 0.52f, 0.76f, 1f],
                    Colors = [ArtOrange, ArtYellow, ArtSky, Palette.Primary.Color, ArtPurple]
                };
                g.FillRectangle(gradient, area);
            }

            // 2. Overlapping translucent shapes (positions are fractions of the banner size).
            void Blob(Color color, int alpha, float x, float y, float w, float h)
            {
                using var brush = new SolidBrush(Color.FromArgb(alpha, color));
                g.FillEllipse(brush, x * width, y * height, w * width, h * height);
            }

            Blob(Color.White, 45, -0.10f, 0.10f, 1.20f, 0.55f);       // soft pale sweep
            Blob(ArtTeal, 225, -0.30f, 0.30f, 1.15f, 1.00f);          // teal, lower left
            Blob(ArtLightTeal, 120, -0.10f, 0.52f, 0.85f, 0.75f);     // lighter teal on top of it
            Blob(ArtPurple, 210, 0.45f, -0.55f, 0.95f, 0.95f);        // purple, upper right
            Blob(ArtLilac, 110, 0.38f, 0.10f, 0.90f, 0.70f);          // lilac, lower right

            // A couple of small bubbles for a little sparkle.
            Blob(Color.White, 80, 0.12f, 0.14f, 0.09f, 0.09f * width / height);
            Blob(Color.White, 60, 0.30f, 0.36f, 0.05f, 0.05f * width / height);

            // 3. Cover the lower part with the panel's own background, following a smooth wave.
            //    Filling a shape with anti-aliasing gives a clean curved edge (a clip would be jagged).
            var yLeft = height * 0.84f;
            var yRight = height * 0.66f;
            using (var wave = new GraphicsPath())
            {
                wave.AddBezier(
                    -2f, yLeft,
                    width * 0.32f, yLeft - 0.55f * height,
                    width * 0.68f, yRight + 0.55f * height,
                    width + 2f, yRight);
                wave.AddLine(width + 2f, yRight, width + 2f, height + 2f);
                wave.AddLine(width + 2f, height + 2f, -2f, height + 2f);
                wave.CloseFigure();

                using var coverBrush = new SolidBrush(cover);
                g.FillPath(coverBrush, wave);
            }

            g.Restore(clipState);
        }
    }

    private sealed record FieldParts(
        Label Label,
        Panel Frame,
        int Y,
        bool SecondColumn,
        bool FullWidth);

    private sealed record FieldLayout(int Y, bool SecondColumn);
}