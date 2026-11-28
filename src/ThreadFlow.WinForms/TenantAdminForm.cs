using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using ThreadFlow.WinForms.ApiClient;
using ThreadFlow.WinForms.Theme.Colors;

using ConfirmDialog = ThreadFlow.WinForms.Theme.ConfirmDialog;
using MessageBox = ThreadFlow.WinForms.Theme.AppMessageBox;

namespace ThreadFlow.WinForms;

/// <summary>Tenant Admin workspace and its user and company settings screens.</summary>
public sealed class TenantAdminForm : Form
{
    // All sizes below are written for 96 DPI (100% scaling).
    // AutoScaleMode.Dpi scales them up on higher-DPI screens, like the platform admin screen.
    private const int FormWidth = 1120;
    private const int FormHeight = 680;
    private const int SidebarWidth = 232;
    private const int SideMargin = 32;
    private const int HeaderHeight = 104;
    private const int FooterHeight = 104;
    private const int InviteFooterHeight = 152;
    private const int ButtonHeight = 42;
    private const int ContentLeft = SidebarWidth + 1 + SideMargin;
    private const int TopBarHeight = 56;
    private const int NavItemHeight = 40;
    private const int NavInset = 16;
    private const int AvatarSize = 40;
    private const int AvatarGap = 8;

    // The role shown in the header pill and in the account card. This screen is the Tenant Admin workspace.
    private const string AccountRoleLabel = "TENANT ADMIN";

    private static readonly Pallets Palette = new();
    private static readonly Shades BrandShades = new();
    private static readonly Tints BrandTints = new();
    private static readonly Color NavIconColor = Color.FromArgb(75, 85, 99);


    // Same Docker Desktop-style sidebar and table colors as the platform admin screen.
    private static readonly Color SidebarBackColor = Color.FromArgb(246, 247, 250);
    private static readonly Color SidebarActiveColor = Color.FromArgb(209, 221, 248);
    private static readonly Color SidebarHoverColor = Color.FromArgb(232, 237, 247);
    private static readonly Color SidebarDividerColor = Color.FromArgb(218, 222, 230);
    private static readonly Color SearchFillColor = Lighten(Palette.Primary.Color, 0.16f);
    private static readonly Color TableRowHighlightColor = Color.FromArgb(224, 224, 224);
    private static readonly Color TableLineColor = Color.FromArgb(228, 231, 236);
    private static readonly Color TableHeaderLineColor = Color.FromArgb(208, 213, 221);
    private static readonly Color AvatarFillColor = Color.FromArgb(30, 64, 175);
    private static readonly Color ProfileRolePillColor = Color.FromArgb(107, 114, 128);

    private static readonly Font TitleFont = new("Segoe UI", 18F, FontStyle.Bold);
    private static readonly Font SubtitleFont = new("Segoe UI", 9.5F);
    private static readonly Font SectionFont = new("Segoe UI Semibold", 11F);
    private static readonly Font LabelFont = new("Segoe UI Semibold", 9F);
    private static readonly Font FieldFont = new("Segoe UI", 10F);
    private static readonly Font GridFont = new("Segoe UI", 9F);
    private static readonly Font GridHeaderFont = new("Segoe UI Semibold", 9F);
    private static readonly Font PrimaryButtonFont = new("Segoe UI", 10F, FontStyle.Bold);
    private static readonly Font SecondaryButtonFont = new("Segoe UI", 10F);
    private static readonly Font TopBarBrandFont = new("Segoe UI Semibold", 13F);
    private static readonly Font PillFont = new("Segoe UI Semibold", 7.5F);
    private static readonly Font NavFont = new("Segoe UI Semibold", 9.5F);
    private static readonly Font SearchFont = new("Segoe UI", 10F);
    private static readonly Font AvatarFont = new("Segoe UI Semibold", 11F);
    private static readonly Font ProfileInitialFont = new("Segoe UI Semibold", 24F);
    private static readonly Font ProfileRoleFont = new("Segoe UI Semibold", 8F);
    private static readonly Font ProfileEmailFont = new("Segoe UI", 11F);

    private readonly TenantAdministrationApiClient _api = new();
    private readonly DataGridView _usersGrid = new();
    private readonly TextBox _employeeEmail = new();
    private readonly ComboBox _employeeRole = new SelectBox();
    private readonly ComboBox _employeeBranch = new SelectBox();
    private readonly RoundedButton _inviteEmployee = MakePrimaryButton("Invite employee");
    private readonly RoundedButton _assignBranch = MakeSecondaryButton("Assign branch");
    private readonly RoundedButton _refreshUsers = MakeSecondaryButton("Refresh");
    private readonly Label _usersStatus = new();
    private readonly bool _branchAssignmentsEnabled;

    private readonly TextBox _legalName = new();
    private readonly TextBox _address = new();
    private readonly TextBox _taxId = new();
    private readonly TextBox _contactEmail = new();
    private readonly TextBox _phone = new();
    private readonly NumericUpDown _taxRate = new();
    private readonly TextBox _receiptHeader = new();
    private readonly TextBox _receiptFooter = new();
    private readonly ComboBox _currency = new SelectBox();
    private readonly RoundedButton _saveSettings = MakePrimaryButton("Save company settings");
    private readonly Label _settingsStatus = new();

    // Window shell (same pieces as the platform admin screen).
    private readonly Panel _topBar = new ResizeEdgePanel();
    private readonly Panel _content = new ResizeEdgePanel();
    private readonly Panel _sidebar = new ResizeEdgePanel();
    private readonly Panel _sidebarEdge = new();
    private readonly SidebarNavButton _navUsers = new("Users and Roles", NavIcon.Users);
    private readonly SidebarNavButton _navSettings = new("Company Settings", NavIcon.Settings);
    private readonly SidebarNavButton _navLogout = new("Log out", NavIcon.LogOut);
    private readonly HeaderSearchBox _search = new("Search users");
    private readonly CaptionButton _captionMinimize = new(CaptionGlyph.Minimize);
    private readonly CaptionButton _captionMaximize = new(CaptionGlyph.Maximize);
    private readonly CaptionButton _captionClose = new(CaptionGlyph.Close);
    private readonly AvatarButton _avatar = new();
    private readonly Label _title = new();
    private readonly Label _subtitle = new();
    private readonly Panel _usersGridCard = new();
    private readonly Panel _usersFooterDivider = new();
    private readonly Panel _settingsScroll = new();
    private readonly Panel _settingsFooterDivider = new();

    // Each page is a group of controls that are shown or hidden together.
    private Control[] _usersPageControls = [];
    private Control[] _settingsPageControls = [];

    // The users table keeps the full list; the search box decides which rows are shown.
    private List<EmployeeGridRow> _userRows = [];
    private string _userSearchText = string.Empty;
    private bool _settingsViewActive;

    // The account card that drops down from the avatar in the header.
    private string _accountEmail = string.Empty;
    private ProfilePopup? _profilePopup;
    private long _profileClosedTick;
    private bool _ignoreAvatarClick;

    private enum NavIcon
    {
        Users,
        Settings,
        LogOut
    }

    public TenantAdminForm()
    {
        var user = ApiSession.Current?.User
            ?? throw new InvalidOperationException("Sign in before opening the workspace.");

        // Apply the shared palette first, then style our own controls the same way the
        // platform admin, welcome and sign-in screens do.
        Theme.ThreadFlowTheme.Apply(this);

        SuspendLayout();

        _accountEmail = user.Email;
        _branchAssignmentsEnabled = string.Equals(
            user.CompanySize,
            "Medium",
            StringComparison.OrdinalIgnoreCase);
        _employeeRole.SelectedIndexChanged += (_, _) => UpdateBranchPickerState();
        _assignBranch.Click += async (_, _) => await AssignSelectedEmployeeBranchAsync();

        BuildForm();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Text = $"ThreadFlow | Tenant Administration | {user.Email}";
        BackColor = Palette.BackgroundSecondary.Color;

        // Borderless window with our own blue header bar (like the platform admin screen).
        FormBorderStyle = FormBorderStyle.None;
        ClientSize = new Size(FormWidth, FormHeight + TopBarHeight);
        StartPosition = FormStartPosition.CenterScreen;
        UpdateMaximizedBounds(Screen.PrimaryScreen ?? Screen.AllScreens[0]);
        WindowState = FormWindowState.Maximized;
        DoubleBuffered = true;

        ResumeLayout(false);
        PerformLayout();

        Load += async (_, _) =>
        {
            await LoadRolesAndUsersAsync();
            await LoadSettingsAsync();
        };
    }

    private void BuildForm()
    {
        _content.SuspendLayout();
        _content.Size = new Size(FormWidth, FormHeight);
        _content.BackColor = Palette.BackgroundSecondary.Color;

        BuildSidebar();

        _title.Text = "Users and roles";
        _title.Font = TitleFont;
        _title.ForeColor = Palette.TextPrimary.Color;
        _title.BackColor = Color.Transparent;
        _title.AutoSize = false;
        _title.TextAlign = ContentAlignment.MiddleLeft;
        _title.Location = new Point(ContentLeft, 24);
        _title.Size = new Size(520, 34);

        _subtitle.Text = "Invite employees and see who has access to your company.";
        _subtitle.Font = SubtitleFont;
        _subtitle.ForeColor = Palette.TextSecondary.Color;
        _subtitle.BackColor = Color.Transparent;
        _subtitle.AutoSize = false;
        _subtitle.TextAlign = ContentAlignment.MiddleLeft;
        _subtitle.Location = new Point(ContentLeft, 60);
        _subtitle.Size = new Size(520, 20);

        var contentWidth = FormWidth - ContentLeft - SideMargin;

        var headerDivider = new Panel
        {
            BackColor = BrandTints.iceBlue.Color,
            Location = new Point(ContentLeft, 92),
            Size = new Size(contentWidth, 1),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        BuildUsersPage(contentWidth);
        BuildSettingsPage(contentWidth);

        _content.Controls.AddRange([
            _sidebar, _sidebarEdge, _title, _subtitle, headerDivider,
            .. _usersPageControls, .. _settingsPageControls
        ]);

        // The users page is the one that opens first.
        SetPageVisible(_settingsPageControls, false);

        _content.Dock = DockStyle.Fill;
        _content.ResumeLayout(false);

        // Order matters for docking: the Fill panel is added first, the top bar after it.
        Controls.Add(_content);
        BuildTopBar();
    }

    private void BuildTopBar()
    {
        _topBar.SuspendLayout();
        _topBar.Size = new Size(FormWidth, TopBarHeight);
        _topBar.BackColor = Palette.Primary.Color;

        var logo = new Theme.ThreadFlowLogo(32)
        {
            Location = new Point(16, (TopBarHeight - 32) / 2),
            BackColor = _topBar.BackColor
        };

        var brand = new Label
        {
            Text = "ThreadFlow",
            Font = TopBarBrandFont,
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(56, 0),
            Size = new Size(104, TopBarHeight)
        };

        var pill = new RolePill(AccountRoleLabel)
        {
            Location = new Point(164, (TopBarHeight - 22) / 2),
            Size = new Size(104, 22)
        };

        _search.Size = new Size(400, 36);
        _search.SearchChanged += (_, _) =>
        {
            // The search box filters the users table.
            _userSearchText = _search.SearchText;
            BindUsers();
        };

        const int captionWidth = 46;

        // Keep the search box centred in the bar, clear of the brand, the avatar and the window buttons.
        void CenterSearch()
        {
            var scale = DeviceDpi / 96f;
            var margin = (int)Math.Round(16 * scale);
            var minLeft = pill.Right + margin;
            var maxLeft = _topBar.Width - (int)Math.Round((captionWidth * 3 + AvatarGap + AvatarSize) * scale)
                - margin - _search.Width;
            var left = Math.Clamp((_topBar.Width - _search.Width) / 2, minLeft, Math.Max(minLeft, maxLeft));
            _search.Location = new Point(left, (_topBar.Height - _search.Height) / 2);
        }

        _topBar.Resize += (_, _) => CenterSearch();
        CenterSearch();

        // Round avatar with the first letter of the signed-in email, just left of the window buttons.
        _avatar.Text = GetInitial(_accountEmail);
        _avatar.Size = new Size(AvatarSize, AvatarSize);
        _avatar.Location = new Point(
            FormWidth - captionWidth * 3 - AvatarGap - AvatarSize,
            (TopBarHeight - AvatarSize) / 2);
        _avatar.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        // Pressing the avatar while the card is open takes focus away from the card first (which
        // closes it), so remember that here and let the click that follows just close it, not reopen it.
        _avatar.MouseDown += (_, _) =>
            _ignoreAvatarClick = _profilePopup is null && Environment.TickCount64 - _profileClosedTick < 200;
        _avatar.Click += (_, _) =>
        {
            if (_ignoreAvatarClick)
            {
                _ignoreAvatarClick = false;
                return;
            }

            ShowProfileCard();
        };

        var captionButtons = new[] { _captionMinimize, _captionMaximize, _captionClose };
        for (var i = 0; i < captionButtons.Length; i++)
        {
            var button = captionButtons[i];
            button.Size = new Size(captionWidth, TopBarHeight);
            button.Location = new Point(FormWidth - captionWidth * (captionButtons.Length - i), 0);
            button.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        }

        _captionMinimize.Click += (_, _) => WindowState = FormWindowState.Minimized;
        _captionMaximize.Click += (_, _) => ToggleMaximize();
        _captionClose.Click += (_, _) => Close();

        // Dragging the bar moves the window; double-click maximizes / restores.
        foreach (Control draggable in new Control[] { _topBar, logo, brand, pill })
        {
            draggable.MouseDown += TopBarMouseDown;
            draggable.DoubleClick += (_, _) => ToggleMaximize();
        }

        _topBar.Controls.AddRange([logo, brand, pill, _search, _avatar, .. captionButtons]);
        _topBar.Dock = DockStyle.Top;
        _topBar.ResumeLayout(false);
        Controls.Add(_topBar);
    }

    private void BuildSidebar()
    {
        _sidebar.Location = Point.Empty;
        _sidebar.Size = new Size(SidebarWidth, FormHeight);
        _sidebar.BackColor = SidebarBackColor;
        _sidebar.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;

        _navUsers.Location = new Point(0, 16);
        _navUsers.Size = new Size(SidebarWidth, NavItemHeight);
        _navUsers.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _navUsers.Active = true;
        _navUsers.Click += (_, _) => ShowUsersView();

        _navSettings.Location = new Point(0, 16 + NavItemHeight + 4);
        _navSettings.Size = new Size(SidebarWidth, NavItemHeight);
        _navSettings.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _navSettings.Click += (_, _) => ShowSettingsView();

        // A divider and Log out pinned to the bottom, like the platform admin screen.
        var navDivider = new Panel
        {
            BackColor = SidebarDividerColor,
            Location = new Point(16, FormHeight - 80),
            Size = new Size(SidebarWidth - 32, 1),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        _navLogout.Location = new Point(0, FormHeight - 68);
        _navLogout.Size = new Size(SidebarWidth, NavItemHeight);
        _navLogout.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _navLogout.Click += (_, _) => LogOut();

        _sidebar.Controls.AddRange([_navUsers, _navSettings, navDivider, _navLogout]);

        _sidebarEdge.Location = new Point(SidebarWidth, 0);
        _sidebarEdge.Size = new Size(1, FormHeight);
        _sidebarEdge.BackColor = SidebarBackColor; // no visible edge, like the platform admin screen
        _sidebarEdge.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
    }

    private void BuildUsersPage(int contentWidth)
    {
        _refreshUsers.Location = new Point(FormWidth - SideMargin - 110, 40);
        _refreshUsers.Size = new Size(110, 36);
        _refreshUsers.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _refreshUsers.Click += async (_, _) => await LoadUsersAsync();

        StyleGrid(_usersGrid);
        ConfigureUsersGrid();
        _usersGrid.DataBindingComplete += (_, _) => ClearGridSelection(_usersGrid);
        _usersGrid.SelectionChanged += (_, _) =>
        {
            if (_branchAssignmentsEnabled)
                _assignBranch.Enabled = _usersGrid.CurrentRow?.DataBoundItem is EmployeeGridRow row &&
                    row.BranchAssignable;
        };
        _usersGrid.Dock = DockStyle.Fill;

        _usersGridCard.Location = new Point(ContentLeft, HeaderHeight);
        _usersGridCard.Size = new Size(contentWidth, FormHeight - HeaderHeight - InviteFooterHeight);
        _usersGridCard.BackColor = Palette.BackgroundSecondary.Color;
        _usersGridCard.Padding = Padding.Empty;
        _usersGridCard.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _usersGridCard.Controls.Add(_usersGrid);

        _usersFooterDivider.BackColor = BrandTints.iceBlue.Color;
        _usersFooterDivider.Location = new Point(ContentLeft, FormHeight - InviteFooterHeight + 16);
        _usersFooterDivider.Size = new Size(contentWidth, 1);
        _usersFooterDivider.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        var emailLabel = MakeFieldLabel("Employee email", new Point(ContentLeft, FormHeight - 124));
        emailLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

        var emailWidth = _branchAssignmentsEnabled ? 190 : 360;
        var emailField = new FieldFrame(_employeeEmail)
        {
            Location = new Point(ContentLeft, FormHeight - 102),
            Size = new Size(emailWidth, ButtonHeight),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };

        var roleLeft = ContentLeft + emailWidth + 16;
        var roleWidth = _branchAssignmentsEnabled ? 145 : 220;
        var roleLabel = MakeFieldLabel("Role", new Point(roleLeft, FormHeight - 124));
        roleLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

        var roleField = new FieldFrame(_employeeRole)
        {
            Location = new Point(roleLeft, FormHeight - 102),
            Size = new Size(roleWidth, ButtonHeight),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };
        // Keep the closed field compact, but give the popup enough room for full role names.
        _employeeRole.DropDownWidth = Math.Max(roleWidth, 260);
        _employeeRole.MaxDropDownItems = 4;

        var branchControls = new List<Control>();
        if (_branchAssignmentsEnabled)
        {
            var branchLeft = roleLeft + roleWidth + 16;
            var branchLabel = MakeFieldLabel("Branch", new Point(branchLeft, FormHeight - 124));
            branchLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            var branchField = new FieldFrame(_employeeBranch)
            {
                Location = new Point(branchLeft, FormHeight - 102),
                Size = new Size(170, ButtonHeight),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _employeeBranch.DropDownWidth = 210;
            _employeeBranch.Enabled = false;

            _assignBranch.Location = new Point(FormWidth - SideMargin - 278, FormHeight - 102);
            _assignBranch.Size = new Size(128, ButtonHeight);
            _assignBranch.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _assignBranch.Enabled = false;

            _inviteEmployee.Location = new Point(FormWidth - SideMargin - 142, FormHeight - 102);
            _inviteEmployee.Size = new Size(142, ButtonHeight);
            branchControls.AddRange([branchLabel, branchField, _assignBranch]);
        }
        else
        {
            _inviteEmployee.Location = new Point(FormWidth - SideMargin - 190, FormHeight - 102);
            _inviteEmployee.Size = new Size(190, ButtonHeight);
        }
        _inviteEmployee.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _inviteEmployee.Click += async (_, _) => await InviteEmployeeAsync();

        StyleHint(_usersStatus, new Point(ContentLeft + 4, FormHeight - 52), new Size(contentWidth - 8, 24));

        _usersPageControls =
        [
            _refreshUsers, _usersGridCard, _usersFooterDivider,
            emailLabel, emailField, roleLabel, roleField, _inviteEmployee, _usersStatus,
            .. branchControls
        ];
    }

    private void BuildSettingsPage(int contentWidth)
    {
        _settingsScroll.Location = new Point(ContentLeft, HeaderHeight);
        _settingsScroll.Size = new Size(contentWidth, FormHeight - HeaderHeight - FooterHeight);
        _settingsScroll.BackColor = Palette.BackgroundSecondary.Color;
        _settingsScroll.AutoScroll = true;
        _settingsScroll.AutoScrollMargin = new Size(0, 16);
        _settingsScroll.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        // Field widths are written for the 823px content column; three across leaves a 20px gap.
        const int full = 784;
        const int third = 248;
        const int gap = 20;
        const int rowHeight = 82;

        _taxRate.DecimalPlaces = 2;
        _taxRate.Maximum = 100;
        _currency.Items.AddRange(["PHP", "USD"]);

        var top = 0;

        AddSectionTitle("Company profile", ref top);
        AddSettingsField("Legal company name", _legalName, 0, top, full);
        top += rowHeight;
        AddSettingsField("Business address", _address, 0, top, full);
        top += rowHeight;
        AddSettingsField("Tax identification number", _taxId, 0, top, third);
        AddSettingsField("Company contact email", _contactEmail, third + gap, top, third);
        AddSettingsField("Company phone", _phone, (third + gap) * 2, top, third);
        top += rowHeight + 14;

        AddSectionTitle("Tax and currency", ref top);
        AddSettingsField("Tax rate (%)", _taxRate, 0, top, third);
        AddSettingsField("Currency code", _currency, third + gap, top, third);
        top += rowHeight + 14;

        AddSectionTitle("Receipts", ref top);
        AddSettingsField("Receipt header", _receiptHeader, 0, top, full);
        top += rowHeight;
        AddSettingsField("Receipt footer", _receiptFooter, 0, top, full);

        _settingsFooterDivider.BackColor = BrandTints.iceBlue.Color;
        _settingsFooterDivider.Location = new Point(ContentLeft, FormHeight - 88);
        _settingsFooterDivider.Size = new Size(contentWidth, 1);
        _settingsFooterDivider.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        StyleHint(
            _settingsStatus,
            new Point(ContentLeft + 4, FormHeight - 72),
            new Size(FormWidth - SideMargin - 200 - 16 - (ContentLeft + 4), ButtonHeight));

        _saveSettings.Location = new Point(FormWidth - SideMargin - 200, FormHeight - 72);
        _saveSettings.Size = new Size(200, ButtonHeight);
        _saveSettings.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _saveSettings.Click += async (_, _) => await SaveSettingsAsync();

        _settingsPageControls = [_settingsScroll, _settingsFooterDivider, _settingsStatus, _saveSettings];
    }

    private void AddSectionTitle(string text, ref int top)
    {
        _settingsScroll.Controls.Add(new Label
        {
            Text = text,
            Font = SectionFont,
            ForeColor = Palette.TextPrimary.Color,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(0, top)
        });
        top += 34;
    }

    private void AddSettingsField(string label, Control input, int left, int top, int width)
    {
        _settingsScroll.Controls.Add(MakeFieldLabel(label, new Point(left, top)));
        _settingsScroll.Controls.Add(new FieldFrame(input)
        {
            Location = new Point(left, top + 22),
            Size = new Size(width, ButtonHeight)
        });
    }

    private static Label MakeFieldLabel(string text, Point location) => new()
    {
        Text = text,
        Font = LabelFont,
        ForeColor = Palette.TextPrimary.Color,
        BackColor = Color.Transparent,
        AutoSize = true,
        Location = location
    };

    // The quiet message line at the bottom left of a page, like the hint on the platform admin screen.
    private static void StyleHint(Label label, Point location, Size size)
    {
        label.Font = SubtitleFont;
        label.ForeColor = Palette.TextSecondary.Color;
        label.BackColor = Color.Transparent;
        label.AutoSize = false;
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.Location = location;
        label.Size = size;
        label.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
    }

    private static string GetInitial(string email)
    {
        var trimmed = email.Trim();
        return trimmed.Length == 0 ? "?" : char.ToUpperInvariant(trimmed[0]).ToString();
    }

    // Opens the account card under the avatar, lined up with the avatar's right edge.
    private void ShowProfileCard()
    {
        if (_profilePopup is not null)
            return;

        var scale = DeviceDpi / 96f;
        var popup = new ProfilePopup(GetInitial(_accountEmail), _accountEmail, AccountRoleLabel, scale);

        var anchor = _avatar.RectangleToScreen(_avatar.ClientRectangle);
        var area = Screen.FromControl(_avatar).WorkingArea;
        var left = Math.Clamp(anchor.Right - popup.Width, area.Left, Math.Max(area.Left, area.Right - popup.Width));
        var top = anchor.Bottom + (int)Math.Round(4 * scale);
        popup.Location = new Point(left, top);

        popup.FormClosed += (_, _) =>
        {
            _profilePopup = null;
            _profileClosedTick = Environment.TickCount64;
            _avatar.Open = false;
        };

        _profilePopup = popup;
        _avatar.Open = true;
        popup.Show(this);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // Ctrl+K jumps to the search box (it only exists on the users page).
        if (keyData == (Keys.Control | Keys.K) && !_settingsViewActive)
        {
            _search.FocusInput();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private static Color Lighten(Color color, float amount) => Color.FromArgb(
        color.R + (int)((255 - color.R) * amount),
        color.G + (int)((255 - color.G) * amount),
        color.B + (int)((255 - color.B) * amount));

    private static bool MatchesSearch(string query, params object?[] fields)
    {
        var terms = query.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        if (terms.Length == 0)
            return true;

        var haystack = string.Join('\n', fields.Select(field => field?.ToString() ?? string.Empty));

        // Every word typed must appear somewhere in the row (in any column).
        return terms.All(term => haystack.Contains(term, StringComparison.CurrentCultureIgnoreCase));
    }

    private void LogOut()
    {
        using (var confirm = new ConfirmDialog(
            "ThreadFlow",
            "Log out",
            "Are you sure you want to log out?",
            "Log out"))
        {
            if (confirm.ShowDialog(this) != DialogResult.OK)
                return;
        }

        Hide();

        using var welcome = new WelcomeForm();
        if (welcome.ShowDialog() != DialogResult.OK || ApiSession.Current is null)
        {
            Close();
            return;
        }

        // Open the workspace for the account that just signed in, then close this
        // (hidden) workspace once that one closes so the app still exits normally.
        Form workspace = Program.CreateWorkspaceForCurrentUser();
        workspace.FormClosed += (_, _) => Close();
        workspace.Show();
    }

    private static void SetPageVisible(Control[] controls, bool visible)
    {
        foreach (var control in controls)
            control.Visible = visible;
    }

    private void ShowUsersView()
    {
        _navUsers.Active = true;
        _navSettings.Active = false;
        _title.Text = "Users and roles";
        _subtitle.Text = "Invite employees and see who has access to your company.";
        _settingsViewActive = false;
        _search.Visible = true;
        SetPageVisible(_settingsPageControls, false);
        SetPageVisible(_usersPageControls, true);
        _usersGrid.Focus();
    }

    private void ShowSettingsView()
    {
        _navUsers.Active = false;
        _navSettings.Active = true;
        _title.Text = "Company settings";
        _subtitle.Text = "Company profile, tax, and receipt settings.";
        _settingsViewActive = true;
        _search.Visible = false;
        SetPageVisible(_usersPageControls, false);
        SetPageVisible(_settingsPageControls, true);
    }

    private static void ClearGridSelection(DataGridView grid)
    {
        // Nothing is highlighted until the person clicks a row.
        grid.ClearSelection();
        grid.CurrentCell = null;
    }

    private void StyleGrid(DataGridView grid)
    {
        // No row is highlighted until one is clicked, and clicking away drops it.
        Theme.GridSelection.Enable(grid);

        grid.BackgroundColor = Palette.BackgroundSecondary.Color;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = TableLineColor;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Palette.BackgroundSecondary.Color;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Palette.BackgroundSecondary.Color;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.Black;
        grid.ColumnHeadersDefaultCellStyle.Font = GridHeaderFont;
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 0, 12, 0);
        grid.ColumnHeadersHeight = 40;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        // Every row is white. Only the selected row is tinted.
        grid.DefaultCellStyle.BackColor = Palette.BackgroundSecondary.Color;
        grid.DefaultCellStyle.ForeColor = Color.Black;
        grid.DefaultCellStyle.SelectionBackColor = TableRowHighlightColor;
        grid.DefaultCellStyle.SelectionForeColor = Color.Black;
        grid.DefaultCellStyle.Font = GridFont;
        grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
        grid.DefaultCellStyle.Padding = new Padding(12, 8, 12, 8);

        grid.RowTemplate.Height = 40;
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;

        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        grid.Font = GridFont;
        grid.AutoGenerateColumns = false;
        grid.AllowUserToOrderColumns = true;
        grid.ShowCellToolTips = true;

        // Same header as the platform admin table: a line under the whole header row and a
        // short divider between header cells.
        grid.CellPainting += (_, e) =>
        {
            if (e.RowIndex != -1 || e.ColumnIndex < 0)
                return;

            e.Paint(e.ClipBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.Border);

            using var linePen = new Pen(TableHeaderLineColor);
            e.Graphics!.DrawLine(
                linePen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);

            if (e.ColumnIndex < grid.Columns.Count - 1)
            {
                var inset = e.CellBounds.Height / 4;
                using var dividerPen = new Pen(TableLineColor);
                e.Graphics.DrawLine(
                    dividerPen, e.CellBounds.Right - 1, e.CellBounds.Top + inset,
                    e.CellBounds.Right - 1, e.CellBounds.Bottom - inset);
            }

            e.Handled = true;
        };
        grid.CellToolTipTextNeeded += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                e.ToolTipText = Convert.ToString(grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
        };
    }

    private void ConfigureUsersGrid()
    {
        // Same approach as the platform admin tables: each column is sized from its content and
        // its title, so nothing is ever cut off. Minimum widths are in 96 DPI units and are
        // scaled for the screen in ApplyGridMetrics.
        const DataGridViewAutoSizeColumnMode fill = DataGridViewAutoSizeColumnMode.Fill;

        AddGridColumn(_usersGrid, nameof(EmployeeGridRow.Email), "Email", fill, 100, 120);
        AddGridColumn(_usersGrid, nameof(EmployeeGridRow.Role), "Role", fill, 100, 100, wrap: true);
        if (_branchAssignmentsEnabled)
            AddGridColumn(_usersGrid, nameof(EmployeeGridRow.Branch), "Branch", fill, 90, 100);
        AddGridColumn(_usersGrid, nameof(EmployeeGridRow.Status), "Status", fill, 100, 100);
    }

    private static void AddGridColumn(
        DataGridView grid,
        string property,
        string title,
        DataGridViewAutoSizeColumnMode sizeMode,
        float fillWeight,
        int minimumWidth,
        bool wrap = false,
        string? format = null)
    {
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = property,
            HeaderText = title,
            Name = property,
            AutoSizeMode = sizeMode,
            FillWeight = fillWeight,
            MinimumWidth = minimumWidth,
            Tag = minimumWidth,
            SortMode = DataGridViewColumnSortMode.Automatic,
            DefaultCellStyle =
            {
                Format = format ?? string.Empty,
                WrapMode = wrap ? DataGridViewTriState.True : DataGridViewTriState.False
            }
        });
    }

    /// <summary>
    /// Makes sure nothing in the table is ever cut off. Every column fills the available width,
    /// but can never be narrower than its header title and its longest unbreakable piece of
    /// content (a whole email, or the longest word of a wrapping column). If the window is too
    /// narrow for that, the table scrolls sideways instead of truncating text.
    /// </summary>
    private void SizeColumnsToContent(DataGridView grid)
    {
        var scale = DeviceDpi / 96f;
        var headerFont = grid.ColumnHeadersDefaultCellStyle.Font ?? grid.Font;
        var cellFont = grid.DefaultCellStyle.Font ?? grid.Font;
        var cellPadding = grid.DefaultCellStyle.Padding.Horizontal;
        var headerPadding = grid.ColumnHeadersDefaultCellStyle.Padding.Horizontal;
        var sortGlyph = (int)Math.Round(14 * scale);

        // The grid draws text with TextRenderer's default side padding and a border, so measure
        // the same way and keep a safety margin. Too small a margin makes the grid show "...".
        var slack = (int)Math.Round(16 * scale);
        const TextFormatFlags flags = TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
        var unlimited = new Size(int.MaxValue, int.MaxValue);

        foreach (DataGridViewColumn column in grid.Columns)
        {
            var wraps = column.DefaultCellStyle.WrapMode == DataGridViewTriState.True;
            var headerWidth = TextRenderer.MeasureText(column.HeaderText, headerFont, unlimited, flags).Width
                + headerPadding + sortGlyph;

            var longestLine = 0;
            var longestWord = 0;
            foreach (DataGridViewRow row in grid.Rows)
            {
                var text = row.Cells[column.Index].FormattedValue?.ToString() ?? string.Empty;
                if (text.Length == 0)
                    continue;

                longestLine = Math.Max(longestLine, TextRenderer.MeasureText(text, cellFont, unlimited, flags).Width);
                foreach (var word in text.Split([' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
                    longestWord = Math.Max(longestWord, TextRenderer.MeasureText(word, cellFont, unlimited, flags).Width);
            }

            var contentMinimum = (wraps ? longestWord : longestLine) + cellPadding + slack;
            var idealWidth = Math.Max(headerWidth, longestLine + cellPadding + slack);

            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            column.MinimumWidth = Math.Max(headerWidth, contentMinimum);

            // Wider content gets proportionally more room, but long text is capped so it wraps.
            column.FillWeight = Math.Max(1, Math.Min(idealWidth, (int)Math.Round(240 * scale)));
        }
    }

    private void ApplyGridMetrics(DataGridView grid)
    {
        var scale = DeviceDpi / 96f;
        var horizontal = (int)Math.Round(10 * scale);
        var vertical = (int)Math.Round(8 * scale);

        grid.ColumnHeadersHeight = (int)Math.Round(40 * scale);
        grid.RowTemplate.Height = (int)Math.Round(44 * scale);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(horizontal, 0, horizontal, 0);
        grid.DefaultCellStyle.Padding = new Padding(horizontal, vertical, horizontal, vertical);

        // Column minimum widths are not scaled automatically, so scale them for this screen.
        foreach (DataGridViewColumn column in grid.Columns)
        {
            if (column.Tag is int designMinimum)
            {
                var minimum = (int)Math.Round(designMinimum * scale);

                // Never let a column get narrower than its own header title:
                // header text + left/right padding + room for the sort arrow.
                var headerTextWidth = TextRenderer.MeasureText(
                    column.HeaderText,
                    grid.ColumnHeadersDefaultCellStyle.Font ?? grid.Font,
                    new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
                var sortGlyph = (int)Math.Round(14 * scale);
                var headerMinimum = headerTextWidth + (horizontal * 2) + sortGlyph;

                column.MinimumWidth = Math.Max(minimum, headerMinimum);
            }
        }
    }

    // ---- Borderless window plumbing (same as the platform admin screen) ----

    private void TopBarMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || e.Clicks != 1)
            return;

        ReleaseCapture();
        _ = SendMessage(Handle, WmNcLButtonDown, HtCaption, 0);
    }

    private void ToggleMaximize()
    {
        UpdateMaximizedBounds(Screen.FromControl(this));
        WindowState = WindowState == FormWindowState.Maximized
            ? FormWindowState.Normal
            : FormWindowState.Maximized;
    }

    // A borderless window would otherwise cover the taskbar when maximized.
    private void UpdateMaximizedBounds(Screen screen)
    {
        var area = screen.WorkingArea;
        MaximizedBounds = new Rectangle(
            area.X - screen.Bounds.X,
            area.Y - screen.Bounds.Y,
            area.Width,
            area.Height);
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        _captionMaximize.Glyph = WindowState == FormWindowState.Maximized
            ? CaptionGlyph.Restore
            : CaptionGlyph.Maximize;
    }

    private const int WmNcHitTest = 0x84;
    private const int WmNcLButtonDown = 0xA1;
    private const int HtClient = 1;
    private const int HtCaption = 2;
    private const int HtLeft = 10, HtRight = 11, HtTop = 12, HtTopLeft = 13, HtTopRight = 14;
    private const int HtBottom = 15, HtBottomLeft = 16, HtBottomRight = 17;

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

    protected override CreateParams CreateParams
    {
        get
        {
            // WS_MINIMIZEBOX lets the taskbar button minimize and restore a borderless window.
            var parameters = base.CreateParams;
            parameters.Style |= 0x00020000;
            return parameters;
        }
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (m.Msg != WmNcHitTest || (int)m.Result != HtClient || WindowState != FormWindowState.Normal)
            return;

        // Resize from the window edges and corners.
        var screenPoint = new Point(
            (short)(m.LParam.ToInt64() & 0xFFFF),
            (short)((m.LParam.ToInt64() >> 16) & 0xFFFF));
        var point = PointToClient(screenPoint);
        var grip = (int)Math.Round(6 * DeviceDpi / 96f);

        var left = point.X < grip;
        var right = point.X >= ClientSize.Width - grip;
        var top = point.Y < grip;
        var bottom = point.Y >= ClientSize.Height - grip;

        if (top && left) m.Result = HtTopLeft;
        else if (top && right) m.Result = HtTopRight;
        else if (bottom && left) m.Result = HtBottomLeft;
        else if (bottom && right) m.Result = HtBottomRight;
        else if (left) m.Result = HtLeft;
        else if (right) m.Result = HtRight;
        else if (top) m.Result = HtTop;
        else if (bottom) m.Result = HtBottom;
    }

    // ---- Square window corners (Windows 11 rounds them by default) ----

    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwcpDoNotRound = 1;
    private const int DwmwcpRound = 2;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        ApplyGridMetrics(_usersGrid);

        // Windows 11 (build 22000+) rounds window corners by default; ask for square corners.
        // Older versions ignore this.
        if (Environment.OSVersion.Version.Build >= 22000)
        {
            var preference = DwmwcpDoNotRound;
            _ = DwmSetWindowAttribute(Handle, DwmwaWindowCornerPreference, ref preference, sizeof(int));
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        var scale = DeviceDpi / 96f;
        MinimumSize = new Size(
            (int)Math.Round(1000 * scale),
            (int)Math.Round(560 * scale));
    }

    // ---- Data loading and actions (unchanged) ----

    private async Task LoadRolesAndUsersAsync()
    {
        try
        {
            if (_branchAssignmentsEnabled)
            {
                _employeeBranch.Items.Clear();
                _employeeBranch.Items.AddRange((await _api.GetBranchesAsync()).Cast<object>().ToArray());
                if (_employeeBranch.Items.Count > 0)
                    _employeeBranch.SelectedIndex = 0;
            }

            _employeeRole.Items.Clear();
            foreach (var role in await _api.GetRolesAsync())
                _employeeRole.Items.Add(new RoleOption(role, ThreadFlowRoleLabels.Format(role)));
            if (_employeeRole.Items.Count > 0)
                _employeeRole.SelectedIndex = 0;
            UpdateBranchPickerState();
            await LoadUsersAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Could not load tenant users",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            var users = await _api.GetUsersAsync();
            _userRows = users
                .Select(user => new EmployeeGridRow(
                    user.Id,
                    user.Email,
                    FormatRoles(user.Roles),
                    user.BranchName ?? (user.Roles.Contains("TenantAdmin", StringComparer.OrdinalIgnoreCase) ||
                                        user.Roles.Contains("TenantOwnerManager", StringComparer.OrdinalIgnoreCase)
                        ? "Company-wide"
                        : "Unassigned"),
                    user.InvitationPending ? "Invitation pending" : "Setup complete",
                    user.Roles.Any(ThreadFlowRoleLabels.RequiresBranchAssignment)))
                .ToList();
            BindUsers();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Could not load tenant users",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // Shows the users that match the search box (everyone when it is empty).
    private void BindUsers()
    {
        var query = _userSearchText.Trim();
        var searching = query.Length > 0;
        var visibleRows = searching
            ? _userRows.Where(row => MatchesSearch(query, row.Email, row.Role, row.Branch, row.Status)).ToList()
            : _userRows.ToList();

        _usersGrid.DataSource = null;
        _usersGrid.DataSource = visibleRows;
        SizeColumnsToContent(_usersGrid);
        _usersStatus.Text = searching
            ? visibleRows.Count == 0
                ? $"No users match \"{query}\"."
                : $"Showing {visibleRows.Count} matching user(s)."
            : string.Empty;
    }

    private static string FormatRoles(IReadOnlyList<string> roles) =>
        string.Join(", ", roles.Select(ThreadFlowRoleLabels.Format));

    private sealed record EmployeeGridRow(
        string Id, string Email, string Role, string Branch, string Status, bool BranchAssignable);

    private sealed record RoleOption(string Value, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    private async Task InviteEmployeeAsync()
    {
        if (string.IsNullOrWhiteSpace(_employeeEmail.Text) || _employeeRole.SelectedItem is not RoleOption selectedRole)
        {
            MessageBox.Show("Enter an email address and choose a role.", "Invitation required",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        TenantUserBranchDto? selectedBranch = null;
        if (_branchAssignmentsEnabled && ThreadFlowRoleLabels.RequiresBranchAssignment(selectedRole.Value))
        {
            selectedBranch = _employeeBranch.SelectedItem as TenantUserBranchDto;
            if (selectedBranch is null)
            {
                MessageBox.Show("Choose the employee's branch before sending the invitation.",
                    "Branch required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
        }

        _inviteEmployee.Enabled = false;
        try
        {
            var invitation = await _api.InviteUserAsync(
                new CreateTenantUserDto(
                    _employeeEmail.Text.Trim(),
                    selectedRole.Value,
                    selectedBranch?.Id));

            using var dialog = new InvitationDetailsForm(BuildInvitationDetails(invitation));
            dialog.ShowDialog(this);

            _employeeEmail.Clear();
            await LoadUsersAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Could not invite employee",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _inviteEmployee.Enabled = true;
        }
    }

    private static string BuildInvitationDetails(TenantUserInvitationDto invitation) =>
        $"Employee: {invitation.Email}\r\n" +
        $"Tenant: {invitation.TenantCode}\r\n" +
        $"Role: {ThreadFlowRoleLabels.Format(invitation.Role)}\r\n\r\n" +
        (invitation.BranchName is null ? string.Empty : $"Branch: {invitation.BranchName}\r\n") +
        $"Setup token (share privately):\r\n{invitation.SetupToken}\r\n\r\n" +
        "The employee chooses a password using 'Complete staff invitation' on the welcome screen.";

    private void UpdateBranchPickerState()
    {
        if (!_branchAssignmentsEnabled)
            return;

        var role = (_employeeRole.SelectedItem as RoleOption)?.Value;
        var requiresBranch = role is not null && ThreadFlowRoleLabels.RequiresBranchAssignment(role);
        _employeeBranch.Enabled = requiresBranch && _employeeBranch.Items.Count > 0;
        if (_employeeBranch.Enabled && _employeeBranch.SelectedIndex < 0)
            _employeeBranch.SelectedIndex = 0;
    }

    private async Task AssignSelectedEmployeeBranchAsync()
    {
        if (_usersGrid.CurrentRow?.DataBoundItem is not EmployeeGridRow row ||
            !row.BranchAssignable ||
            !Guid.TryParse(row.Id, out var userId))
        {
            MessageBox.Show("Select an employee role that uses branch assignments first.",
                "Employee required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_employeeBranch.SelectedItem is not TenantUserBranchDto branch)
        {
            MessageBox.Show("Choose an active branch first.", "Branch required",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _assignBranch.Enabled = false;
        try
        {
            await _api.AssignBranchAsync(userId, branch.Id);
            await LoadUsersAsync();
            _usersStatus.Text = $"{row.Email} assigned to {branch.Name}.";
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Could not assign branch",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _assignBranch.Enabled = _usersGrid.CurrentRow?.DataBoundItem is EmployeeGridRow currentRow &&
                currentRow.BranchAssignable;
        }
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            var settings = await _api.GetCompanySettingsAsync();
            _legalName.Text = settings.LegalName;
            _address.Text = settings.BusinessAddress;
            _taxId.Text = settings.TaxIdentificationNumber ?? string.Empty;
            _contactEmail.Text = settings.ContactEmail;
            _phone.Text = settings.ContactPhone ?? string.Empty;
            _taxRate.Value = Math.Clamp(settings.TaxRatePercent, _taxRate.Minimum, _taxRate.Maximum);
            _receiptHeader.Text = settings.ReceiptHeader ?? string.Empty;
            _receiptFooter.Text = settings.ReceiptFooter ?? string.Empty;
            _currency.Text = settings.CurrencyCode;
            TenantCurrencyFormatter.SetCurrencyCode(settings.CurrencyCode);
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Could not load company settings",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task SaveSettingsAsync()
    {
        _saveSettings.Enabled = false;
        _settingsStatus.Text = "Saving...";
        try
        {
            await _api.SaveCompanySettingsAsync(new TenantCompanySettingsDto(
                _legalName.Text.Trim(), _address.Text.Trim(), EmptyToNull(_taxId.Text),
                _contactEmail.Text.Trim(), EmptyToNull(_phone.Text), _taxRate.Value,
                EmptyToNull(_receiptHeader.Text), EmptyToNull(_receiptFooter.Text),
                _currency.Text.Trim().ToUpperInvariant()));
            TenantCurrencyFormatter.SetCurrencyCode(_currency.Text);
            _settingsStatus.Text = "Company settings saved.";
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Could not save company settings",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _settingsStatus.Text = "Could not save settings.";
        }
        finally
        {
            _saveSettings.Enabled = true;
        }
    }

    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class InvitationDetailsForm : Form
    {
        private const int DialogWidth = 640;
        private const int DetailsHeight = 258;

        public InvitationDetailsForm(string details)
        {
            Theme.ThreadFlowTheme.Apply(this);
            Theme.DialogChrome.Prepare(this);

            SuspendLayout();

            var pad = Theme.DialogChrome.Pad;
            var detailsTop = Theme.DialogChrome.HeaderHeight + 20;
            var buttonTop = detailsTop + DetailsHeight + 8 + 16;

            Text = "Invitation created";
            ClientSize = new Size(DialogWidth, buttonTop + ButtonHeight + pad);
            MaximizeBox = false;
            MinimizeBox = false;

            var header = Theme.DialogChrome.CreateHeader(
                this,
                "Invitation created",
                "Share these details privately with the new employee.",
                DialogWidth);

            var detailsBox = new TextBox
            {
                Text = details,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = true
            };

            var detailsFrame = new FieldFrame(detailsBox)
            {
                Location = new Point(pad, detailsTop),
                Size = new Size(DialogWidth - pad * 2, DetailsHeight)
            };

            var footerDivider = Theme.DialogChrome.CreateDivider(DialogWidth, detailsTop + DetailsHeight + 8);

            var copy = MakeSecondaryButton("Copy details");
            copy.Location = new Point(pad, buttonTop);
            copy.Size = new Size(150, ButtonHeight);
            copy.Click += (_, _) =>
            {
                try
                {
                    Clipboard.SetText(detailsBox.Text);
                    copy.Text = "Copied";
                }
                catch (Exception exception)
                {
                    Theme.AppMessageBox.Show(
                        this,
                        exception.Message,
                        "Could not copy",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            };

            var done = MakePrimaryButton("Done");
            done.Location = new Point(DialogWidth - pad - 150, buttonTop);
            done.Size = new Size(150, ButtonHeight);
            done.DialogResult = DialogResult.Cancel;

            Controls.AddRange([header, detailsFrame, footerDivider, copy, done]);

            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;

            ResumeLayout(false);
            PerformLayout();

            CancelButton = done;
            Shown += (_, _) => detailsBox.Select(0, 0);
        }
    }

    // ---- Control factories (same as the platform admin screen) ----

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

    private static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
    {
        // Keep the corners from being larger than the shape itself.
        radius = Math.Max(1f, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2f));

        var diameter = radius * 2f;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private sealed class SidebarNavButton : Button
    {
        private readonly NavIcon _icon;
        private bool _active;
        private bool _hover;

        public SidebarNavButton(string text, NavIcon icon)
        {
            Text = text;
            _icon = icon;
            Font = NavFont;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            AccessibleRole = AccessibleRole.PushButton;

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
        }

        [DefaultValue(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Active
        {
            get => _active;
            set
            {
                _active = value;
                Invalidate();
            }
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
            var parentBack = Parent?.BackColor ?? BackColor;
            g.Clear(parentBack);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var scale = DeviceDpi / 96f;
            var inset = (int)Math.Round(NavInset * scale);

            // The highlight is an inset bar (not full width), like Docker Desktop.
            var barBounds = new Rectangle(inset, 0, Math.Max(1, Width - inset * 2), Height);
            var fillColor = _active
                ? SidebarActiveColor
                : _hover ? SidebarHoverColor : parentBack;
            using (var fill = new SolidBrush(fillColor))
            {
                g.FillRectangle(fill, barBounds);
            }

            var iconSize = (int)Math.Round(18 * scale);
            var iconBounds = new Rectangle(
                inset + (int)Math.Round(16 * scale),
                (Height - iconSize) / 2,
                iconSize,
                iconSize);
            var iconColor = NavIconColor; // thin gray line icons, even when selected
            DrawIcon(g, _icon, iconBounds, iconColor, scale);

            var textX = inset + (int)Math.Round(48 * scale);
            var textBounds = new Rectangle(
                textX,
                0,
                Math.Max(10, Width - textX - (int)Math.Round(16 * scale)),
                Height);
            TextRenderer.DrawText(
                g,
                Text,
                Font,
                textBounds,
                Color.Black,
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.NoPrefix |
                TextFormatFlags.EndEllipsis);

            if (Focused && ShowFocusCues)
            {
                using var focusPen = new Pen(Color.FromArgb(120, Palette.Primary.Color), 1f);
                g.DrawRectangle(focusPen, inset + 2, 2, Width - inset * 2 - 5, Height - 5);
            }
        }

        private static void DrawIcon(Graphics g, NavIcon icon, Rectangle bounds, Color color, float scale)
        {
            // Thin outline icons in the style of Docker Desktop.
            using var pen = new Pen(color, Math.Max(1f, 1.25f * scale))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            float cx = bounds.X + bounds.Width / 2f;
            float cy = bounds.Y + bounds.Height / 2f;
            float r = bounds.Width / 2f;

            switch (icon)
            {
                case NavIcon.Users:
                    {
                        // A person: round head over rounded shoulders.
                        var size = bounds.Width;
                        var headRadius = size * 0.24f;
                        g.DrawEllipse(pen, cx - headRadius, bounds.Y + 0.5f * scale, headRadius * 2, headRadius * 2);

                        var shoulderLeft = bounds.X + 1.5f * scale;
                        var shoulderWidth = size - 3f * scale;
                        var shoulderTop = bounds.Y + size * 0.58f;
                        var shoulderHeight = size * 0.8f;
                        g.DrawArc(pen, shoulderLeft, shoulderTop, shoulderWidth, shoulderHeight, 180, 180);
                        var shoulderBase = shoulderTop + shoulderHeight / 2f;
                        g.DrawLine(pen, shoulderLeft, shoulderBase, shoulderLeft + shoulderWidth, shoulderBase);
                        break;
                    }

                case NavIcon.Settings:
                    {
                        // Three sliders, each a line with a small round knob.
                        var left = bounds.X + 0.5f * scale;
                        var right = bounds.Right - 0.5f * scale;
                        var knob = 2.4f * scale;
                        var rows = new (float Y, float KnobX)[]
                        {
                            (bounds.Y + bounds.Height * 0.2f, left + (right - left) * 0.68f),
                            (bounds.Y + bounds.Height * 0.5f, left + (right - left) * 0.32f),
                            (bounds.Y + bounds.Height * 0.8f, left + (right - left) * 0.62f)
                        };

                        foreach (var (y, knobX) in rows)
                        {
                            g.DrawLine(pen, left, y, knobX - knob, y);
                            g.DrawLine(pen, knobX + knob, y, right, y);
                            g.DrawEllipse(pen, knobX - knob, y - knob, knob * 2, knob * 2);
                        }

                        break;
                    }

                case NavIcon.LogOut:
                    {
                        // Open door bracket with an arrow leaving it.
                        var left = bounds.X + (int)Math.Round(1 * scale);
                        var top = bounds.Y + (int)Math.Round(1 * scale);
                        var bottom = bounds.Bottom - (int)Math.Round(1 * scale);
                        var doorRight = bounds.X + (int)Math.Round(10 * scale);

                        g.DrawLines(pen, [
                            new Point(doorRight, top),
                            new Point(left, top),
                            new Point(left, bottom),
                            new Point(doorRight, bottom)
                        ]);

                        var midY = bounds.Y + bounds.Height / 2;
                        var arrowStart = bounds.X + (int)Math.Round(6 * scale);
                        var arrowEnd = bounds.Right - (int)Math.Round(1 * scale);
                        g.DrawLine(pen, arrowStart, midY, arrowEnd, midY);

                        var head = (int)Math.Round(3.5f * scale);
                        g.DrawLine(pen, arrowEnd - head, midY - head, arrowEnd, midY);
                        g.DrawLine(pen, arrowEnd - head, midY + head, arrowEnd, midY);
                        break;
                    }
            }
        }
    }

    /// <summary>
    /// A panel that lets mouse hit-testing fall through at the window edges, so the borderless
    /// form underneath can offer its resize handles even though panels cover its whole surface.
    /// </summary>
    private sealed class ResizeEdgePanel : Panel
    {
        private const int WmNcHitTest = 0x84;
        private const int HtTransparent = -1;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmNcHitTest && FindForm() is { WindowState: FormWindowState.Normal } form)
            {
                var screenPoint = new Point(
                    (short)(m.LParam.ToInt64() & 0xFFFF),
                    (short)((m.LParam.ToInt64() >> 16) & 0xFFFF));
                var point = form.PointToClient(screenPoint);
                var grip = (int)Math.Round(6 * form.DeviceDpi / 96f);

                if (point.X < grip || point.X >= form.ClientSize.Width - grip ||
                    point.Y < grip || point.Y >= form.ClientSize.Height - grip)
                {
                    m.Result = HtTransparent;
                    return;
                }
            }

            base.WndProc(ref m);
        }
    }

    private enum CaptionGlyph
    {
        Minimize,
        Maximize,
        Restore,
        Close
    }

    /// <summary>
    /// White minimize / maximize / close button for the blue header bar. Hover shows a soft
    /// highlight; the close button turns red like a standard Windows title bar.
    /// </summary>
    private sealed class CaptionButton : Control
    {
        private CaptionGlyph _glyph;
        private bool _hover;

        // White by default (for the blue header bars); light headers can set a dark colour.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color GlyphColor { get; set; } = Color.White;

        public CaptionButton(CaptionGlyph glyph)
        {
            _glyph = glyph;
            TabStop = false;
            Cursor = Cursors.Default;
            AccessibleRole = AccessibleRole.PushButton;
            AccessibleName = glyph.ToString();

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.StandardClick,
                true);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CaptionGlyph Glyph
        {
            get => _glyph;
            set
            {
                _glyph = value;
                AccessibleName = value.ToString();
                Invalidate();
            }
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
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_hover)
            {
                var hoverColor = _glyph == CaptionGlyph.Close
                    ? Color.FromArgb(232, 17, 35)
                    : Color.FromArgb(46, Color.White);
                using var hoverBrush = new SolidBrush(hoverColor);
                g.FillRectangle(hoverBrush, ClientRectangle);
            }

            var scale = DeviceDpi / 96f;
            var glyphColor = _hover && _glyph == CaptionGlyph.Close ? Color.White : GlyphColor;
            using var pen = new Pen(glyphColor, Math.Max(1f, 1.2f * scale))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            var half = 5f * scale;
            var cx = Width / 2f;
            var cy = Height / 2f;

            switch (_glyph)
            {
                case CaptionGlyph.Minimize:
                    g.DrawLine(pen, cx - half, cy, cx + half, cy);
                    break;

                case CaptionGlyph.Maximize:
                    g.DrawRectangle(pen, cx - half, cy - half, half * 2, half * 2);
                    break;

                case CaptionGlyph.Restore:
                    {
                        var offset = 2.5f * scale;
                        g.DrawRectangle(pen, cx - half, cy - half + offset, half * 2 - offset, half * 2 - offset);
                        g.DrawLines(pen, [
                            new PointF(cx - half + offset, cy - half + offset),
                            new PointF(cx - half + offset, cy - half),
                            new PointF(cx + half, cy - half),
                            new PointF(cx + half, cy + half - offset),
                            new PointF(cx + half - offset, cy + half - offset)
                        ]);
                        break;
                    }

                case CaptionGlyph.Close:
                    g.DrawLine(pen, cx - half, cy - half, cx + half, cy + half);
                    g.DrawLine(pen, cx - half, cy + half, cx + half, cy - half);
                    break;
            }
        }
    }

    /// <summary>
    /// Round account button for the blue header bar: a dark circle with the first letter of the
    /// signed-in email. A soft light ring shows while the mouse is over it or its card is open.
    /// </summary>
    private sealed class AvatarButton : Control
    {
        private bool _hover;
        private bool _open;

        public AvatarButton()
        {
            TabStop = false;
            Cursor = Cursors.Hand;
            AccessibleRole = AccessibleRole.PushButton;
            AccessibleName = "Account";

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.StandardClick,
                true);
        }

        [DefaultValue(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Open
        {
            get => _open;
            set
            {
                if (_open == value)
                    return;

                _open = value;
                Invalidate();
            }
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
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var scale = DeviceDpi / 96f;
            var diameter = Math.Min(Width, Height) - 1;
            var outer = new Rectangle((Width - diameter) / 2, (Height - diameter) / 2, diameter, diameter);

            if (_open || _hover)
            {
                using var halo = new SolidBrush(Color.FromArgb(_open ? 70 : 40, Color.White));
                g.FillEllipse(halo, outer);
            }

            var inset = (int)Math.Round(4 * scale);
            var inner = Rectangle.Inflate(outer, -inset, -inset);
            using (var fill = new SolidBrush(AvatarFillColor))
            {
                g.FillEllipse(fill, inner);
            }

            TextRenderer.DrawText(
                g,
                Text,
                AvatarFont,
                inner,
                Color.White,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.NoPrefix);
        }
    }

    /// <summary>
    /// The account card that drops down from the header avatar: a colourful banner, a big avatar
    /// with the first letter of the email, the role in an oval and the email underneath.
    /// It closes when the person clicks anywhere else or presses Esc.
    /// </summary>
    private sealed class ProfilePopup : Form
    {
        private const int CsDropShadow = 0x00020000;

        private readonly string _initial;
        private readonly string _email;
        private readonly string _role;
        private readonly float _scale;

        public ProfilePopup(string initial, string email, string role, float scale)
        {
            _initial = initial;
            _email = email;
            _role = role;
            _scale = scale;

            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            BackColor = Palette.BackgroundSecondary.Color;
            DoubleBuffered = true;

            // Wide enough for the whole email, within sensible limits.
            var emailWidth = TextRenderer.MeasureText(email, ProfileEmailFont).Width;
            var width = Math.Clamp(emailWidth + Px(72), Px(280), Px(460));
            ClientSize = new Size(width, Px(196));
        }

        private int Px(float designUnits) => (int)Math.Round(designUnits * _scale);

        protected override CreateParams CreateParams
        {
            get
            {
                // Soft drop shadow around the card.
                var parameters = base.CreateParams;
                parameters.ClassStyle |= CsDropShadow;
                return parameters;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // Rounded corners on Windows 11 (build 22000+); older versions keep square corners.
            if (Environment.OSVersion.Version.Build >= 22000)
            {
                var preference = DwmwcpRound;
                _ = DwmSetWindowAttribute(Handle, DwmwaWindowCornerPreference, ref preference, sizeof(int));
            }
        }

        // Clicking anywhere outside the card (or switching to another window) closes it.
        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Palette.BackgroundSecondary.Color);

            const TextFormatFlags centered =
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.NoPrefix;

            var bannerHeight = Px(68);
            PaintBanner(g, new Rectangle(0, 0, ClientSize.Width, bannerHeight));

            // Big avatar, centred on the lower edge of the banner.
            var avatarSize = Px(72);
            var avatar = new Rectangle(
                (ClientSize.Width - avatarSize) / 2,
                bannerHeight - avatarSize / 2,
                avatarSize,
                avatarSize);
            using (var fill = new SolidBrush(Palette.Primary.Color))
            {
                g.FillEllipse(fill, avatar);
            }

            TextRenderer.DrawText(g, _initial, ProfileInitialFont, avatar, Color.White, centered);

            // Role in an oval.
            var roleTextWidth = TextRenderer.MeasureText(
                _role,
                ProfileRoleFont,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width;
            var pillWidth = roleTextWidth + Px(20);
            var pillHeight = Px(22);
            var pill = new Rectangle(
                (ClientSize.Width - pillWidth) / 2,
                avatar.Bottom + Px(14),
                pillWidth,
                pillHeight);
            using (var path = RoundedRectangle(pill, pill.Height / 2))
            using (var fill = new SolidBrush(ProfileRolePillColor))
            {
                g.FillPath(fill, path);
            }

            TextRenderer.DrawText(g, _role, ProfileRoleFont, pill, Color.White, centered);

            // Email.
            var emailBounds = new Rectangle(Px(16), pill.Bottom + Px(10), ClientSize.Width - Px(32), Px(26));
            TextRenderer.DrawText(
                g,
                _email,
                ProfileEmailFont,
                emailBounds,
                Palette.TextPrimary.Color,
                centered | TextFormatFlags.EndEllipsis);
        }

        // Orange, yellow, sky blue and purple washes with a pale swoosh across them.
        private static void PaintBanner(Graphics g, Rectangle bounds)
        {
            var state = g.Save();
            g.SetClip(bounds);

            using (var sky = new LinearGradientBrush(bounds, Color.White, Color.White, 10f))
            {
                sky.InterpolationColors = new ColorBlend
                {
                    Positions = [0f, 0.28f, 0.5f, 0.75f, 1f],
                    Colors =
                    [
                        Color.FromArgb(235, 125, 25),
                        Color.FromArgb(252, 215, 120),
                        Color.FromArgb(52, 170, 235),
                        Color.FromArgb(96, 72, 232),
                        Color.FromArgb(126, 34, 200)
                    ]
                };
                g.FillRectangle(sky, bounds);
            }

            using (var cyan = new SolidBrush(Color.FromArgb(190, 20, 170, 232)))
            {
                g.FillEllipse(
                    cyan,
                    bounds.Left - bounds.Width * 0.12f,
                    bounds.Top + bounds.Height * 0.45f,
                    bounds.Width * 0.5f,
                    bounds.Height * 1.1f);
            }

            using (var violet = new SolidBrush(Color.FromArgb(150, 150, 40, 210)))
            {
                g.FillEllipse(
                    violet,
                    bounds.Left + bounds.Width * 0.62f,
                    bounds.Top - bounds.Height * 0.4f,
                    bounds.Width * 0.5f,
                    bounds.Height * 1.2f);
            }

            using (var swoosh = new GraphicsPath())
            {
                swoosh.AddBezier(
                    bounds.Left, bounds.Bottom - bounds.Height * 0.1f,
                    bounds.Left + bounds.Width * 0.30f, bounds.Top - bounds.Height * 0.05f,
                    bounds.Left + bounds.Width * 0.60f, bounds.Bottom + bounds.Height * 0.15f,
                    bounds.Right, bounds.Top + bounds.Height * 0.30f);
                swoosh.AddLine(bounds.Right, bounds.Top + bounds.Height * 0.30f, bounds.Right, bounds.Bottom);
                swoosh.AddLine(bounds.Right, bounds.Bottom, bounds.Left, bounds.Bottom);
                swoosh.CloseFigure();

                using var wash = new SolidBrush(Color.FromArgb(48, Color.White));
                g.FillPath(wash, swoosh);
            }

            g.Restore(state);
        }
    }

    /// <summary>Small dark-blue pill with white text that names the kind of admin signed in.</summary>
    private sealed class RolePill : Control
    {
        public RolePill(string text)
        {
            Text = text;
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = RoundedRectangle(bounds, bounds.Height / 2))
            using (var fill = new SolidBrush(Color.FromArgb(80, 0, 20, 90)))
            {
                g.FillPath(fill, path);
            }

            TextRenderer.DrawText(
                g,
                Text,
                PillFont,
                bounds,
                Color.White,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.NoPrefix);
        }
    }

    /// <summary>
    /// Search field for the blue header bar: magnifier icon, white text, a "Ctrl+K" badge while
    /// empty and a clear (x) button once something is typed.
    /// </summary>
    private sealed class HeaderSearchBox : Control
    {
        private readonly TextBox _input = new();
        private readonly Label _hint = new();
        private bool _silent;
        private bool _clearHover;

        public HeaderSearchBox(string placeholder)
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            Cursor = Cursors.IBeam;

            _input.BorderStyle = BorderStyle.None;
            _input.AutoSize = false; // we set the height ourselves so letter tails (g, y, p) are not clipped
            _input.Font = SearchFont;
            _input.ForeColor = Color.White;
            _input.BackColor = SearchFillColor;
            _input.MaxLength = 100;
            _input.AccessibleName = "Search";
            _input.TextChanged += (_, _) =>
            {
                UpdateHint();
                Invalidate();
                if (!_silent)
                    SearchChanged?.Invoke(this, EventArgs.Empty);
            };
            _input.GotFocus += (_, _) => { UpdateHint(); Invalidate(); };
            _input.LostFocus += (_, _) => { UpdateHint(); Invalidate(); };
            _input.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Escape && _input.TextLength > 0)
                {
                    _input.Clear();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };

            _hint.Text = placeholder;
            _hint.Font = SearchFont;
            _hint.ForeColor = Color.FromArgb(214, 227, 252);
            _hint.BackColor = SearchFillColor;
            _hint.AutoSize = false;
            _hint.TextAlign = ContentAlignment.MiddleLeft;
            _hint.Cursor = Cursors.IBeam;
            _hint.Click += (_, _) => _input.Focus();

            Controls.Add(_input);
            Controls.Add(_hint);
            _hint.BringToFront();
        }

        public event EventHandler? SearchChanged;

        public string SearchText => _input.Text;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string PlaceholderText
        {
            get => _hint.Text;
            set => _hint.Text = value;
        }

        /// <summary>Changes the text without raising SearchChanged.</summary>
        public void SetSearchText(string text)
        {
            _silent = true;
            try
            {
                _input.Text = text;
            }
            finally
            {
                _silent = false;
            }
        }

        public void FocusInput()
        {
            _input.Focus();
            _input.SelectAll();
        }

        // The hint hides while the box has focus so the text caret stays visible.
        private void UpdateHint() => _hint.Visible = _input.TextLength == 0 && !_input.Focused;

        private Rectangle ClearBounds()
        {
            var scale = DeviceDpi / 96f;
            var size = (int)Math.Round(22 * scale);
            return new Rectangle(Width - size - (int)Math.Round(10 * scale), (Height - size) / 2, size, size);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            var scale = DeviceDpi / 96f;
            var left = (int)Math.Round(40 * scale);
            var rightArea = (int)Math.Round(74 * scale);
            var width = Math.Max(10, Width - left - rightArea);
            var lineHeight = (int)Math.Ceiling(_input.Font.GetHeight(DeviceDpi));
            var inputHeight = lineHeight + (int)Math.Round(10 * scale);
            var inputTop = Height / 2 - lineHeight / 2 - (int)Math.Round(2 * scale);

            _input.SetBounds(left, inputTop, width, inputHeight);
            _hint.SetBounds(left, 0, width, Height);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var hover = _input.TextLength > 0 && ClearBounds().Contains(e.Location);
            if (hover != _clearHover)
            {
                _clearHover = hover;
                Cursor = hover ? Cursors.Hand : Cursors.IBeam;
                Invalidate();
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_clearHover)
            {
                _clearHover = false;
                Cursor = Cursors.IBeam;
                Invalidate();
            }

            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (_input.TextLength > 0 && ClearBounds().Contains(e.Location))
                _input.Clear();

            _input.Focus();
            base.OnMouseDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var scale = DeviceDpi / 96f;
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            var radius = (int)Math.Round(6 * scale);

            using (var path = RoundedRectangle(bounds, radius))
            using (var fill = new SolidBrush(SearchFillColor))
            {
                g.FillPath(fill, path);
                if (_input.Focused)
                {
                    using var outline = new Pen(Color.FromArgb(170, Color.White), 1f);
                    g.DrawPath(outline, path);
                }
            }

            // Magnifier icon.
            using (var pen = new Pen(Color.White, Math.Max(1f, 1.4f * scale)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                var lens = 11f * scale;
                var lensX = 13f * scale;
                var lensY = (Height - lens) / 2f - 1f * scale;
                g.DrawEllipse(pen, lensX, lensY, lens, lens);
                g.DrawLine(pen, lensX + lens * 0.86f, lensY + lens * 0.86f, lensX + lens + 3f * scale, lensY + lens + 3f * scale);
            }

            if (_input.TextLength > 0)
            {
                var clear = ClearBounds();
                if (_clearHover)
                {
                    using var hoverBrush = new SolidBrush(Color.FromArgb(50, Color.White));
                    g.FillEllipse(hoverBrush, clear);
                }

                using var pen = new Pen(Color.White, Math.Max(1f, 1.3f * scale)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                var inset = 7f * scale;
                g.DrawLine(pen, clear.Left + inset, clear.Top + inset, clear.Right - inset, clear.Bottom - inset);
                g.DrawLine(pen, clear.Left + inset, clear.Bottom - inset, clear.Right - inset, clear.Top + inset);
            }
            else
            {
                var badgeWidth = (int)Math.Round(52 * scale);
                var badgeHeight = (int)Math.Round(22 * scale);
                var badge = new Rectangle(
                    Width - badgeWidth - (int)Math.Round(10 * scale),
                    (Height - badgeHeight) / 2,
                    badgeWidth,
                    badgeHeight);

                using var badgePath = RoundedRectangle(badge, (int)Math.Round(4 * scale));
                using var badgePen = new Pen(Color.FromArgb(200, Color.White), 1f);
                g.DrawPath(badgePen, badgePath);
                TextRenderer.DrawText(
                    g,
                    "Ctrl+K",
                    PillFont,
                    badge,
                    Color.White,
                    TextFormatFlags.HorizontalCenter |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.SingleLine |
                    TextFormatFlags.NoPrefix);
            }
        }
    }

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

            // A disabled button (for example while applications are loading) is shown faded.
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
    /// Wraps a text box, number box, or drop-down in the same rounded outline the platform admin
    /// screen uses for its search box and filter: soft blue border, primary blue when focused.
    /// The wrapped control keeps all of its normal properties and events.
    /// </summary>
    private sealed class FieldFrame : Panel
    {
        private readonly Control _input;
        private bool _focused;
        private bool _laidOut;

        public FieldFrame(Control input)
        {
            _input = input;

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            BackColor = Palette.BackgroundSecondary.Color;

            input.Font = FieldFont;
            input.BackColor = Palette.BackgroundSecondary.Color;
            input.ForeColor = Palette.TextPrimary.Color;

            // The frame draws the border, so the inner control draws none of its own.
            switch (input)
            {
                case TextBox box:
                    box.BorderStyle = BorderStyle.None;

                    // A single-line box is sized in OnLayout, so it can be a little taller than its
                    // text and the tails of g, j, p, q and y are never cut off.
                    if (!box.Multiline)
                        box.AutoSize = false;
                    break;
                case NumericUpDown number:
                    number.BorderStyle = BorderStyle.None;
                    break;
            }

            Controls.Add(input);

            input.Enter += (_, _) => SetFocused(true);
            input.Leave += (_, _) => SetFocused(false);
            input.SizeChanged += (_, _) => PerformLayout();

            // Clicking the padding around the text still puts the cursor in the field.
            MouseDown += (_, _) => input.Focus();
        }

        private void SetFocused(bool focused)
        {
            _focused = focused;
            Invalidate();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            // Moving the inner control can raise its SizeChanged, which asks for another layout.
            if (_laidOut)
                return;

            _laidOut = true;
            try
            {
                var scale = DeviceDpi / 96f;
                var padding = (int)Math.Round(12 * scale);
                var innerWidth = Math.Max(1, Width - padding * 2);

                if (_input is TextBox { Multiline: true })
                {
                    // A multi-line box fills the frame.
                    var vertical = (int)Math.Round(10 * scale);
                    _input.SetBounds(padding, vertical, innerWidth, Math.Max(1, Height - vertical * 2));
                }
                else if (_input is TextBox)
                {
                    // A single-line text box draws its text at the top of the box, so the box is made
                    // taller than one line. The extra room under the text keeps the tails of g, j, p, q
                    // and y from being cut off, and the line of text itself stays centred in the frame.
                    var lineHeight = (int)Math.Ceiling(_input.Font.GetHeight(DeviceDpi));
                    var boxHeight = lineHeight + (int)Math.Round(10 * scale);
                    var boxTop = Math.Max(0, (Height - lineHeight) / 2);
                    _input.SetBounds(padding, boxTop, innerWidth, Math.Min(boxHeight, Math.Max(1, Height - boxTop)));
                }
                else
                {
                    // The number box and drop-downs keep their natural height and sit in the middle.
                    _input.Width = innerWidth;
                    _input.Location = new Point(padding, Math.Max(0, (Height - _input.Height) / 2));
                }
            }
            finally
            {
                _laidOut = false;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Whole numbers are pixel edges here, so a stroke drawn half its width inside the edge
            // lands on whole pixels: crisp, and the same thickness on all four sides.
            g.PixelOffsetMode = PixelOffsetMode.Half;

            var scale = DeviceDpi / 96f;
            var penWidth = _focused ? Math.Max(2f, MathF.Round(1.5f * scale)) : 1f;
            var borderColor = _focused ? Palette.Primary.Color : BrandTints.powderBlue.Color;

            var half = penWidth / 2f;
            var bounds = new RectangleF(half, half, Width - penWidth, Height - penWidth);
            var radius = Math.Max(1f, 7f * scale - half);

            using var path = RoundedRectangle(bounds, radius);
            using var fill = new SolidBrush(Palette.BackgroundSecondary.Color);
            using var pen = new Pen(borderColor, penWidth);
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
        }
    }

    /// <summary>
    /// A drop-down list that paints itself flat, with a thin chevron and the sidebar's pale-blue
    /// highlight for the hovered item. It is still a ComboBox: Items, SelectedItem, Text and
    /// SelectedIndex all work as usual. Put it inside a FieldFrame for the rounded outline.
    /// </summary>
    private sealed class SelectBox : ComboBox
    {
        private const uint SwpNoSize = 0x0001;
        private const uint SwpNoZOrder = 0x0004;
        private const uint SwpNoActivate = 0x0010;

        // The open list is a separate window, so Windows leaves it where it appeared even when the
        // box scrolls away underneath it. While the list is open, this timer moves it along.
        private readonly System.Windows.Forms.Timer _followTimer = new() { Interval = 15 };
        private Point _dropAnchor;

        public SelectBox()
        {
            DropDownStyle = ComboBoxStyle.DropDownList;
            FlatStyle = FlatStyle.Flat;
            DrawMode = DrawMode.OwnerDrawFixed;
            Cursor = Cursors.Hand;

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            _followTimer.Tick += (_, _) => FollowBox();
        }

        protected override void OnDropDown(EventArgs e)
        {
            _dropAnchor = PointToScreen(Point.Empty);
            _followTimer.Start();
            base.OnDropDown(e);
        }

        protected override void OnDropDownClosed(EventArgs e)
        {
            _followTimer.Stop();
            base.OnDropDownClosed(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _followTimer.Dispose();

            base.Dispose(disposing);
        }

        private void FollowBox()
        {
            if (!IsHandleCreated || !DroppedDown)
            {
                _followTimer.Stop();
                return;
            }

            // If the box has been scrolled out of sight, close the list instead of leaving it floating.
            if (!IsMostlyVisible())
            {
                DroppedDown = false;
                return;
            }

            var current = PointToScreen(Point.Empty);
            var dx = current.X - _dropAnchor.X;
            var dy = current.Y - _dropAnchor.Y;
            if (dx == 0 && dy == 0)
                return;

            var info = new ComboBoxInfo { cbSize = Marshal.SizeOf<ComboBoxInfo>() };
            if (GetComboBoxInfo(Handle, ref info) &&
                info.hwndList != IntPtr.Zero &&
                GetWindowRect(info.hwndList, out var rect))
            {
                _ = SetWindowPos(
                    info.hwndList, IntPtr.Zero, rect.Left + dx, rect.Top + dy, 0, 0,
                    SwpNoSize | SwpNoZOrder | SwpNoActivate);
            }

            _dropAnchor = current;
        }

        // True while at least half of the box is inside every panel it sits in (not scrolled away).
        private bool IsMostlyVisible()
        {
            var visible = RectangleToScreen(ClientRectangle);
            for (var parent = Parent; parent is not null && !visible.IsEmpty; parent = parent.Parent)
                visible.Intersect(parent.RectangleToScreen(parent.ClientRectangle));

            return visible.Height >= Height / 2;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ComboBoxInfo
        {
            public int cbSize;
            public NativeRect rcItem;
            public NativeRect rcButton;
            public int stateButton;
            public IntPtr hwndCombo;
            public IntPtr hwndItem;
            public IntPtr hwndList;
        }

        [DllImport("user32.dll")]
        private static extern bool GetComboBoxInfo(IntPtr hwndCombo, ref ComboBoxInfo info);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect rect);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(
            IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // Comfortable row height in the open list.
            ItemHeight = (int)Math.Round(26 * DeviceDpi / 96f);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var scale = DeviceDpi / 96f;
            var chevronArea = (int)Math.Round(26 * scale);

            TextRenderer.DrawText(
                g,
                Text,
                Font,
                new Rectangle(0, 0, Math.Max(1, Width - chevronArea), Height),
                ForeColor,
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.NoPrefix |
                TextFormatFlags.EndEllipsis);

            var cx = Width - chevronArea / 2f - 2f * scale;
            var cy = Height / 2f;
            using var pen = new Pen(Palette.TextSecondary.Color, Math.Max(1f, 1.4f * scale))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            g.DrawLines(pen, [
                new PointF(cx - 4f * scale, cy - 2f * scale),
                new PointF(cx, cy + 2f * scale),
                new PointF(cx + 4f * scale, cy - 2f * scale)
            ]);
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0)
                return;

            var highlighted = (e.State & DrawItemState.Selected) != 0;
            using (var fill = new SolidBrush(highlighted ? SidebarActiveColor : Palette.BackgroundSecondary.Color))
            {
                e.Graphics.FillRectangle(fill, e.Bounds);
            }

            var textBounds = Rectangle.Inflate(e.Bounds, -(int)Math.Round(10 * DeviceDpi / 96f), 0);
            TextRenderer.DrawText(
                e.Graphics,
                GetItemText(Items[e.Index]),
                Font,
                textBounds,
                Palette.TextPrimary.Color,
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.NoPrefix |
                TextFormatFlags.EndEllipsis);
        }
    }
}

internal static class ThreadFlowRoleLabels
{
    public static bool RequiresBranchAssignment(string role) => role is
        "SalesClerk" or "CustomerServiceAgent" or "InventoryPurchasingStaff" or "BranchManager";

    public static string Format(string role) => role switch
    {
        "PlatformAdmin" => "Platform Admin",
        "TenantAdmin" => "Tenant Admin",
        "TenantOwnerManager" => "Tenant Owner/Manager",
        "BranchManager" => "Branch Manager",
        "SalesClerk" => "Sales Clerk",
        "CustomerServiceAgent" => "Customer Service Agent",
        "InventoryPurchasingStaff" => "Inventory/Purchasing Staff",
        _ => role
    };
}
