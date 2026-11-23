using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using ThreadFlow.WinForms.ApiClient;
using ThreadFlow.WinForms.Theme.Colors;

namespace ThreadFlow.WinForms;

public sealed class PlatformAdminForm : Form
{
    // All sizes below are written for 96 DPI (100% scaling).
    // AutoScaleMode.Dpi scales them up on higher-DPI screens, like the auth screens.
    private const int FormWidth = 1120;
    private const int FormHeight = 680;
    private const int SidebarWidth = 232;
    private const int SideMargin = 32;
    private const int HeaderHeight = 104;
    private const int FooterHeight = 104;
    private const int ButtonHeight = 42;
    private const int ContentLeft = SidebarWidth + 1 + SideMargin;

    private static readonly Pallets Palette = new();
    private static readonly Shades BrandShades = new();
    private static readonly Tints BrandTints = new();
    private static readonly Color NavIconColor = Color.FromArgb(75, 85, 99);

    // Docker Desktop-style sidebar: light gray panel, inset pale-blue selection bar.
    private static readonly Color SidebarBackColor = Color.FromArgb(246, 247, 250);
    private static readonly Color SidebarActiveColor = Color.FromArgb(209, 221, 248);
    private static readonly Color SidebarHoverColor = Color.FromArgb(232, 237, 247);
    private static readonly Color SidebarDividerColor = Color.FromArgb(218, 222, 230);
    private static readonly Color SearchFillColor = Lighten(Palette.Primary.Color, 0.16f);
    private static readonly Color TableRowHighlightColor = Color.FromArgb(224, 224, 224);
    private static readonly Color TableLineColor = Color.FromArgb(228, 231, 236);
    private static readonly Color TableHeaderLineColor = Color.FromArgb(208, 213, 221);
    private const int TopBarHeight = 56;
    private const int NavItemHeight = 40;
    private const int NavInset = 16;

    private static readonly Font TitleFont = new("Segoe UI", 18F, FontStyle.Bold);
    private static readonly Font SubtitleFont = new("Segoe UI", 9.5F);
    private static readonly Font GridFont = new("Segoe UI", 9F);
    private static readonly Font GridHeaderFont = new("Segoe UI Semibold", 9F);
    private static readonly Font PrimaryButtonFont = new("Segoe UI", 10F, FontStyle.Bold);
    private static readonly Font SecondaryButtonFont = new("Segoe UI", 10F);
    private static readonly Font BrandFont = new("Segoe UI", 14F, FontStyle.Bold);
    private static readonly Font TopBarBrandFont = new("Segoe UI Semibold", 13F);
    private static readonly Font SearchFont = new("Segoe UI", 10F);
    private static readonly Font PillFont = new("Segoe UI Semibold", 7.5F);
    private static readonly Font BrandSubtitleFont = new("Segoe UI", 9F);
    private static readonly Font NavFont = new("Segoe UI Semibold", 9.5F);

    private readonly OnboardingApiClient _api = new();
    private readonly DataGridView _grid = new();
    private readonly DataGridView _auditGrid = new();
    private readonly RoundedButton _refresh = MakeSecondaryButton("Refresh");
    private readonly RoundedButton _approve = MakePrimaryButton("Approve and provision");
    private readonly RoundedButton _reject = MakeSecondaryButton("Reject");
    private readonly SegmentedFilter _applicationFilter = new("Needs review", "All applications");
    private readonly SidebarNavButton _navApplications = new("Applications", NavIcon.Applications);
    private readonly SidebarNavButton _navAudit = new("Audit Log", NavIcon.Audit);
    private readonly SidebarNavButton _navLogout = new("Log out", NavIcon.LogOut);
    private readonly Panel _topBar = new ResizeEdgePanel();
    private readonly Panel _content = new ResizeEdgePanel();
    private readonly Panel _sidebar = new ResizeEdgePanel();
    private readonly Panel _sidebarEdge = new();
    private readonly HeaderSearchBox _search = new("Search applications");
    private readonly CaptionButton _captionMinimize = new(CaptionGlyph.Minimize);
    private readonly CaptionButton _captionMaximize = new(CaptionGlyph.Maximize);
    private readonly CaptionButton _captionClose = new(CaptionGlyph.Close);
    private readonly Label _title = new();
    private readonly Label _subtitle = new();
    private readonly Label _applicationHint = new();
    private readonly Panel _gridCard = new();
    private readonly Panel _auditGridCard = new();
    private readonly Panel _footerDivider = new();
    private List<CompanyApplicationDto> _applications = [];
    private List<PlatformAuditLogDto> _auditLogs = [];
    private List<AuditGridRow> _auditRows = [];
    private string _applicationSearchText = string.Empty;
    private string _auditSearchText = string.Empty;
    private string _applicationHintText = string.Empty;
    private string _auditHintText = string.Empty;
    private bool _auditLoaded;
    private bool _auditLoading;
    private bool _auditViewActive;

    private enum NavIcon
    {
        Applications,
        Audit,
        LogOut
    }

    public PlatformAdminForm()
    {
        // Apply the shared palette first, then style our own controls the same way the
        // welcome and sign-in screens do.
        Theme.ThreadFlowTheme.Apply(this);

        SuspendLayout();

        BuildForm();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Text = "ThreadFlow | Platform Administration";
        BackColor = Palette.BackgroundSecondary.Color;

        // Borderless window with our own blue header bar (like Docker Desktop).
        FormBorderStyle = FormBorderStyle.None;
        ClientSize = new Size(FormWidth, FormHeight + TopBarHeight);
        StartPosition = FormStartPosition.CenterScreen;
        UpdateMaximizedBounds(Screen.PrimaryScreen ?? Screen.AllScreens[0]);
        WindowState = FormWindowState.Maximized;
        DoubleBuffered = true;

        ResumeLayout(false);
        PerformLayout();

        Load += async (_, _) => await RefreshApplicationsAsync();
    }

    private void BuildForm()
    {
        _content.SuspendLayout();
        _content.Size = new Size(FormWidth, FormHeight);
        _content.BackColor = Palette.BackgroundSecondary.Color;

        BuildSidebar();

        _title.Text = "Company plan applications";
        _title.Font = TitleFont;
        _title.ForeColor = Palette.TextPrimary.Color;
        _title.BackColor = Color.Transparent;
        _title.AutoSize = false;
        _title.TextAlign = ContentAlignment.MiddleLeft;
        _title.Location = new Point(ContentLeft, 24);
        _title.Size = new Size(360, 34);

        _subtitle.Text = "Review, approve, or reject incoming applications.";
        _subtitle.Font = SubtitleFont;
        _subtitle.ForeColor = Palette.TextSecondary.Color;
        _subtitle.BackColor = Color.Transparent;
        _subtitle.AutoSize = false;
        _subtitle.TextAlign = ContentAlignment.MiddleLeft;
        _subtitle.Location = new Point(ContentLeft, 60);
        _subtitle.Size = new Size(360, 20);

        _refresh.Location = new Point(FormWidth - SideMargin - 110, 40);
        _refresh.Size = new Size(110, 36);
        _refresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _refresh.Click += async (_, _) => await RefreshActiveViewAsync();

        _applicationFilter.AccessibleName = "Filter applications";
        _applicationFilter.SelectedIndex = 0;
        _applicationFilter.Location = new Point(FormWidth - SideMargin - 110 - 12 - 260, 40);
        _applicationFilter.Size = new Size(260, 36);
        _applicationFilter.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _applicationFilter.SelectedIndexChanged += (_, _) => BindApplications();

        var contentWidth = FormWidth - ContentLeft - SideMargin;

        var headerDivider = new Panel
        {
            BackColor = BrandTints.iceBlue.Color,
            Location = new Point(ContentLeft, 92),
            Size = new Size(contentWidth, 1),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        StyleGrid(_grid);
        StyleGrid(_auditGrid);
        ConfigureApplicationGrid();
        ConfigureAuditGrid();

        _grid.Dock = DockStyle.Fill;
        _gridCard.Location = new Point(ContentLeft, HeaderHeight);
        _gridCard.Size = new Size(contentWidth, FormHeight - HeaderHeight - FooterHeight);
        _gridCard.BackColor = Palette.BackgroundSecondary.Color;
        _gridCard.Padding = Padding.Empty;
        _gridCard.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _gridCard.Controls.Add(_grid);

        _auditGrid.Dock = DockStyle.Fill;
        _auditGridCard.Location = new Point(ContentLeft, HeaderHeight);
        // Same size as the applications table; the audit log has its own message underneath.
        _auditGridCard.Size = new Size(contentWidth, FormHeight - HeaderHeight - FooterHeight);
        _auditGridCard.BackColor = Palette.BackgroundSecondary.Color;
        _auditGridCard.Padding = Padding.Empty;
        _auditGridCard.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _auditGridCard.Visible = false;
        _auditGridCard.Controls.Add(_auditGrid);

        _footerDivider.BackColor = BrandTints.iceBlue.Color;
        _footerDivider.Location = new Point(ContentLeft, FormHeight - 88);
        _footerDivider.Size = new Size(contentWidth, 1);
        _footerDivider.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        _applicationHint.Font = SubtitleFont;
        _applicationHint.ForeColor = Palette.TextSecondary.Color;
        _applicationHint.Location = new Point(ContentLeft + 4, FormHeight - 72);

        // The hint stops before the Reject button and wraps onto a second line if it is long.
        _applicationHint.AutoSize = false;
        _applicationHint.Size = new Size(
            FormWidth - SideMargin - 190 - 12 - 110 - 16 - (ContentLeft + 4), ButtonHeight);
        _applicationHint.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _applicationHint.TextAlign = ContentAlignment.MiddleLeft;

        _approve.Location = new Point(FormWidth - SideMargin - 190, FormHeight - 72);
        _approve.Size = new Size(190, ButtonHeight);
        _approve.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _approve.Click += async (_, _) => await ApproveSelectedAsync();

        _reject.Location = new Point(FormWidth - SideMargin - 190 - 12 - 110, FormHeight - 72);
        _reject.Size = new Size(110, ButtonHeight);
        _reject.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _reject.Click += async (_, _) => await RejectSelectedAsync();
        _grid.SelectionChanged += (_, _) => UpdateApplicationActions();
        _grid.CellDoubleClick += (_, _) => ShowSelectedApplicationDetails();
        _auditGrid.CellDoubleClick += (_, _) => ShowSelectedAuditDetails();

        _content.Controls.AddRange([
            _sidebar, _sidebarEdge, _title, _subtitle, _refresh, _applicationFilter, headerDivider,
            _gridCard, _auditGridCard, _footerDivider, _applicationHint, _reject, _approve
        ]);
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

        var pill = new RolePill("PLATFORM ADMIN")
        {
            Location = new Point(164, (TopBarHeight - 22) / 2),
            Size = new Size(116, 22)
        };

        _search.Size = new Size(400, 36);
        _search.SearchChanged += (_, _) =>
        {
            // The search box only filters the page that is currently open.
            if (_auditViewActive)
            {
                _auditSearchText = _search.SearchText;
                BindAuditLogs();
            }
            else
            {
                _applicationSearchText = _search.SearchText;
                BindApplications();
            }
        };

        // Keep the search box centred in the bar, clear of the brand and the window buttons.
        void CenterSearch()
        {
            var scale = DeviceDpi / 96f;
            var margin = (int)Math.Round(16 * scale);
            var minLeft = pill.Right + margin;
            var maxLeft = _topBar.Width - (int)Math.Round(46 * 3 * scale) - margin - _search.Width;
            var left = Math.Clamp((_topBar.Width - _search.Width) / 2, minLeft, Math.Max(minLeft, maxLeft));
            _search.Location = new Point(left, (_topBar.Height - _search.Height) / 2);
        }

        _topBar.Resize += (_, _) => CenterSearch();
        CenterSearch();

        const int captionWidth = 46;
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

        _topBar.Controls.AddRange([logo, brand, pill, _search, .. captionButtons]);
        _topBar.Dock = DockStyle.Top;
        _topBar.ResumeLayout(false);
        Controls.Add(_topBar);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.K))
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

        var culture = System.Globalization.CultureInfo.CurrentCulture;
        var haystack = string.Join('\n', fields.Select(field => field switch
        {
            null => string.Empty,
            DateTime date => date.ToString("g", culture),
            DateTimeOffset date => date.ToString("g", culture),
            _ => field.ToString() ?? string.Empty
        }));

        // Every word typed must appear somewhere in the row (in any column).
        return terms.All(term => haystack.Contains(term, StringComparison.CurrentCultureIgnoreCase));
    }

    private static void ClearGridSelection(DataGridView grid)
    {
        // Nothing is highlighted until the person clicks a row.
        grid.ClearSelection();
        grid.CurrentCell = null;
    }

    private void SwitchSearchContext(bool toAudit)
    {
        _search.PlaceholderText = toAudit ? "Search audit log" : "Search applications";
        _search.SetSearchText(toAudit ? _auditSearchText : _applicationSearchText);
    }

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

    // ---- Borderless window plumbing ----

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

    private void BuildSidebar()
    {
        _sidebar.Location = Point.Empty;
        _sidebar.Size = new Size(SidebarWidth, FormHeight);
        _sidebar.BackColor = SidebarBackColor;
        _sidebar.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;

        _navApplications.Location = new Point(0, 16);
        _navApplications.Size = new Size(SidebarWidth, NavItemHeight);
        _navApplications.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _navApplications.Active = true;
        _navApplications.Click += (_, _) => ShowApplicationsView();

        _navAudit.Location = new Point(0, 16 + NavItemHeight + 4);
        _navAudit.Size = new Size(SidebarWidth, NavItemHeight);
        _navAudit.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _navAudit.Click += async (_, _) => await ShowAuditViewAsync();

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

        _sidebar.Controls.AddRange([
            _navApplications, _navAudit, navDivider, _navLogout
        ]);

        _sidebarEdge.Location = new Point(SidebarWidth, 0);
        _sidebarEdge.Size = new Size(1, FormHeight);
        _sidebarEdge.BackColor = SidebarBackColor; // no visible edge, like Docker Desktop
        _sidebarEdge.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
    }

    private void StyleGrid(DataGridView grid)
    {
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

        // Every row is white. Only the selected row gets a very light blue tint,
        // so it is still clear which application Approve / Reject will act on.
        // (No AlternatingRowsDefaultCellStyle: alternating rows reuse this same style.)
        grid.DefaultCellStyle.BackColor = Palette.BackgroundSecondary.Color;
        grid.DefaultCellStyle.ForeColor = Color.Black;
        grid.DefaultCellStyle.SelectionBackColor = TableRowHighlightColor;
        grid.DefaultCellStyle.SelectionForeColor = Color.Black;
        grid.DefaultCellStyle.Font = GridFont;
        grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

        // Columns decide for themselves whether to wrap (see AddGridColumn).
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

        // Docker Desktop-style header: a line under the whole header row and a short divider
        // between header cells.
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

    private void ConfigureApplicationGrid()
    {
        // Unbreakable values (email, phone, codes) size to their content so they are never cut off.
        // Names, dates, and notes share the leftover space and wrap onto more lines.
        // Minimum widths are in 96 DPI units and are scaled for the screen in ApplyGridMetrics.
        const DataGridViewAutoSizeColumnMode fit = DataGridViewAutoSizeColumnMode.AllCells;
        const DataGridViewAutoSizeColumnMode fill = DataGridViewAutoSizeColumnMode.Fill;

        AddGridColumn(_grid, nameof(CompanyApplicationDto.CompanyName), "Company", fill, 100, 90, wrap: true);
        AddGridColumn(_grid, nameof(CompanyApplicationDto.ContactName), "Contact Person", fill, 110, 100, wrap: true);
        AddGridColumn(_grid, nameof(CompanyApplicationDto.ContactEmail), "Contact Email", fit, 100, 120);
        AddGridColumn(_grid, nameof(CompanyApplicationDto.ContactPhone), "Phone", fit, 100, 90);
        AddGridColumn(_grid, nameof(CompanyApplicationDto.RequestedCompanySize), "Plan", fit, 100, 70);
        AddGridColumn(_grid, nameof(CompanyApplicationDto.Status), "Status", fit, 100, 80);
        AddGridColumn(_grid, nameof(CompanyApplicationDto.SubmittedAtUtc), "Submitted", fill, 80, 90,
            wrap: true, format: "g");
        AddGridColumn(_grid, nameof(CompanyApplicationDto.TenantCode), "Tenant Code", fit, 100, 100);
        AddGridColumn(_grid, nameof(CompanyApplicationDto.ReviewNotes), "Review Notes", fill, 200, 110, wrap: true);
    }

    private void ConfigureAuditGrid()
    {
        const DataGridViewAutoSizeColumnMode fit = DataGridViewAutoSizeColumnMode.AllCells;
        const DataGridViewAutoSizeColumnMode fill = DataGridViewAutoSizeColumnMode.Fill;

        AddGridColumn(_auditGrid, nameof(AuditGridRow.OccurredAt), "When", fill, 40, 95, wrap: true, format: "g");
        AddGridColumn(_auditGrid, nameof(AuditGridRow.Actor), "Actor", fit, 100, 120);
        AddGridColumn(_auditGrid, nameof(AuditGridRow.Tenant), "Tenant", fill, 50, 80, wrap: true);
        AddGridColumn(_auditGrid, nameof(AuditGridRow.Action), "Activity", fill, 60, 100, wrap: true);
        AddGridColumn(_auditGrid, nameof(AuditGridRow.RecordType), "Record type", fit, 100, 100);
        AddGridColumn(_auditGrid, nameof(AuditGridRow.Summary), "What happened", fill, 400, 200, wrap: true);
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
    /// content (a whole email / phone / code, or the longest word of a wrapping column). If the
    /// window is too narrow for that, the table scrolls sideways instead of truncating text.
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

            // Wider content gets proportionally more room, but long notes are capped so they wrap.
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

    private void ShowApplicationsView()
    {
        SwitchSearchContext(toAudit: false);
        _auditViewActive = false;
        _navApplications.Active = true;
        _navAudit.Active = false;
        _title.Text = "Company plan applications";
        _subtitle.Text = "Review new applications or view past decisions.";
        _gridCard.Visible = true;
        _auditGridCard.Visible = false;
        _applicationFilter.Visible = true;
        _applicationHint.Visible = true;
        _footerDivider.Visible = true;
        _reject.Visible = true;
        _approve.Visible = true;
        RefreshHint();
        UpdateApplicationActions();
        _grid.Focus();
    }

    private async Task ShowAuditViewAsync()
    {
        SwitchSearchContext(toAudit: true);
        _auditViewActive = true;
        _navApplications.Active = false;
        _navAudit.Active = true;
        _title.Text = "Audit log";
        _subtitle.Text = "Recent platform activity";
        _gridCard.Visible = false;
        _auditGridCard.Visible = true;
        _applicationFilter.Visible = false;
        _applicationHint.Visible = true;
        _footerDivider.Visible = true;
        _reject.Visible = false;
        _approve.Visible = false;
        RefreshHint();
        _auditGrid.Focus();

        if (!_auditLoaded)
            await LoadAuditLogsAsync();
    }

    private async Task RefreshActiveViewAsync()
    {
        if (_auditViewActive)
            await LoadAuditLogsAsync();
        else
            await RefreshApplicationsAsync();
    }

    private async Task LoadAuditLogsAsync()
    {
        if (_auditLoading)
            return;

        _auditLoading = true;
        _refresh.Enabled = false;
        try
        {
            _auditLogs = await _api.GetAuditLogsAsync();
            _auditRows = _auditLogs.Select(entry => new AuditGridRow(
                entry.OccurredAtUtc.ToLocalTime(),
                entry.ActorEmail ?? (entry.ActorUserId is null ? "System" : "Unknown user"),
                entry.Tenant,
                FormatAuditAction(entry.Action),
                entry.EntityType,
                entry.Summary,
                entry)).ToList();
            BindAuditLogs();
            _auditLoaded = true;
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Could not load audit log",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _auditLoading = false;
            _refresh.Enabled = true;
        }
    }

    /// <summary>
    /// A small borderless confirmation prompt: light header with a close button, title, one
    /// line of text, Cancel and confirm buttons. Returns DialogResult.OK when confirmed;
    /// Cancel, the close button or Esc all return Cancel.
    /// </summary>
    private sealed class ConfirmDialog : Form
    {
        private const int DialogWidth = 340;
        private const int DialogHeight = 160;
        private const int DialogHeaderHeight = 32;
        private const int DialogButtonHeight = 36;

        private static readonly Color HeaderBackColor = Color.FromArgb(238, 241, 248);

        public ConfirmDialog(string headerText, string title, string message, string confirmText, string cancelText = "Cancel")
        {
            Theme.ThreadFlowTheme.Apply(this);

            SuspendLayout();

            const int pad = 20;
            var innerWidth = DialogWidth - pad * 2;

            Text = headerText;
            BackColor = Palette.BackgroundSecondary.Color;
            ClientSize = new Size(DialogWidth, DialogHeight);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;

            var header = new Panel
            {
                BackColor = HeaderBackColor,
                Location = Point.Empty,
                Size = new Size(DialogWidth, DialogHeaderHeight),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            var headerTitle = new Label
            {
                Text = headerText,
                Font = SubtitleFont,
                ForeColor = Palette.TextPrimary.Color,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(12, 0),
                Size = new Size(DialogWidth - 12 - 40, DialogHeaderHeight)
            };
            var closeHeader = new CaptionButton(CaptionGlyph.Close)
            {
                GlyphColor = Palette.TextPrimary.Color,
                Location = new Point(DialogWidth - 40, 0),
                Size = new Size(40, DialogHeaderHeight),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            closeHeader.Click += (_, _) => DialogResult = DialogResult.Cancel;

            void BeginHeaderDrag(object? sender, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Left || e.Clicks != 1)
                    return;

                ReleaseCapture();
                _ = SendMessage(Handle, WmNcLButtonDown, HtCaption, 0);
            }

            header.MouseDown += BeginHeaderDrag;
            headerTitle.MouseDown += BeginHeaderDrag;
            header.Controls.Add(headerTitle);
            header.Controls.Add(closeHeader);

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = Palette.TextPrimary.Color,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(pad, DialogHeaderHeight + 14),
                Size = new Size(innerWidth, 26)
            };
            var messageLabel = new Label
            {
                Text = message,
                Font = SubtitleFont,
                ForeColor = Palette.TextSecondary.Color,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.TopLeft,
                Location = new Point(pad, DialogHeaderHeight + 42),
                Size = new Size(innerWidth, 20)
            };

            var buttonTop = DialogHeight - pad + 2 - DialogButtonHeight;

            var confirm = MakePrimaryButton(confirmText);
            confirm.Size = new Size(96, DialogButtonHeight);
            confirm.Location = new Point(DialogWidth - pad - confirm.Width, buttonTop);
            confirm.DialogResult = DialogResult.OK;

            var cancel = MakeSecondaryButton(cancelText);
            cancel.Size = new Size(88, DialogButtonHeight);
            cancel.Location = new Point(confirm.Left - 10 - cancel.Width, buttonTop);
            cancel.DialogResult = DialogResult.Cancel;

            Controls.AddRange([header, titleLabel, messageLabel, confirm, cancel]);

            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;

            ResumeLayout(false);
            PerformLayout();

            AcceptButton = confirm;
            CancelButton = cancel;
        }
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

    // ---- Square window corners (Windows 11 rounds them by default) ----

    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwcpDoNotRound = 1;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        ApplyGridMetrics(_grid);
        ApplyGridMetrics(_auditGrid);

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
            (int)Math.Round(440 * scale));
    }

    private async Task RefreshApplicationsAsync()
    {
        _refresh.Enabled = false;
        try
        {
            _applications = await _api.GetApplicationsAsync();
            BindApplications();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Could not load applications",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _refresh.Enabled = true;
        }
    }

    // The message under the table belongs to whichever page is open.
    private void RefreshHint() =>
        _applicationHint.Text = _auditViewActive ? _auditHintText : _applicationHintText;

    private void BindAuditLogs()
    {
        var query = _auditSearchText;
        var rows = string.IsNullOrWhiteSpace(query)
            ? _auditRows
            : _auditRows.Where(row => MatchesSearch(
                query, row.OccurredAt, row.Actor, row.Tenant, row.Action, row.RecordType, row.Summary)).ToList();

        _auditGrid.DataSource = null;
        _auditGrid.DataSource = rows;
        SizeColumnsToContent(_auditGrid);
        ClearGridSelection(_auditGrid);

        _auditHintText = !string.IsNullOrWhiteSpace(query)
            ? rows.Count == 0
                ? $"No audit logs match \"{query.Trim()}\"."
                : $"Showing {rows.Count} matching audit log(s). Double-click a row for full details."
            : rows.Count == 0
                ? "No audit logs yet."
                : $"Showing {rows.Count} audit log(s). Double-click a row for full details.";
        RefreshHint();
    }

    private void BindApplications()
    {
        var showAll = _applicationFilter.SelectedIndex == 1;
        var visibleApplications = showAll
            ? _applications
            : _applications.Where(application =>
                application.Status is "Pending" or "Provisioning").ToList();

        var query = _applicationSearchText.Trim();
        var searching = query.Length > 0;
        if (searching)
        {
            visibleApplications = visibleApplications.Where(application => MatchesSearch(
                query,
                application.CompanyName,
                application.ContactName,
                application.ContactEmail,
                application.ContactPhone,
                application.RequestedCompanySize,
                application.Status,
                application.SubmittedAtUtc,
                application.TenantCode,
                application.ReviewNotes)).ToList();
        }

        _grid.DataSource = null;
        _grid.DataSource = visibleApplications;
        SizeColumnsToContent(_grid);
        ClearGridSelection(_grid);
        _applicationHintText = searching
            ? visibleApplications.Count == 0
                ? $"No applications match \"{query}\"."
                : $"Showing {visibleApplications.Count} matching application(s). Double-click a row for full details."
            : showAll
                ? $"Showing {visibleApplications.Count} application(s). Double-click a row for full details."
                : visibleApplications.Count == 0
                    ? "No applications need review. Choose All applications to see past decisions."
                    : $"{visibleApplications.Count} application(s) need review. Double-click a row for full details.";
        RefreshHint();
        UpdateApplicationActions();
    }

    private CompanyApplicationDto? GetSelectedApplication() =>
        _grid.CurrentRow?.DataBoundItem as CompanyApplicationDto;

    private void UpdateApplicationActions()
    {
        var application = GetSelectedApplication();
        _approve.Enabled = application?.Status is "Pending" or "Provisioning";
        _reject.Enabled = application?.Status == "Pending";
    }

    private void ShowSelectedApplicationDetails()
    {
        if (GetSelectedApplication() is not { } application)
            return;

        MessageBox.Show(this,
            $"Company: {application.CompanyName}\r\n" +
            $"Contact: {application.ContactName}\r\n" +
            $"Email: {application.ContactEmail}\r\n" +
            $"Phone: {application.ContactPhone ?? "Not provided"}\r\n" +
            $"Requested plan: {application.RequestedCompanySize}\r\n" +
            $"Status: {application.Status}\r\n" +
            $"Submitted: {application.SubmittedAtUtc.ToLocalTime():f}\r\n" +
            $"Tenant code: {application.TenantCode ?? "Not assigned"}\r\n" +
            $"Review notes: {application.ReviewNotes ?? "None"}\r\n" +
            $"Application ID: {application.Id}",
            "Company application details", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ShowSelectedAuditDetails()
    {
        if (_auditGrid.CurrentRow?.DataBoundItem is not AuditGridRow row)
            return;

        var entry = row.Source;
        MessageBox.Show(this,
            $"When: {entry.OccurredAtUtc.ToLocalTime():f}\r\n" +
            $"Actor: {entry.ActorEmail ?? (entry.ActorUserId is null ? "System" : "Unknown user")}\r\n" +
            $"Tenant: {entry.Tenant}\r\n" +
            $"Activity: {FormatAuditAction(entry.Action)} ({entry.Action})\r\n" +
            $"Record type: {entry.EntityType}\r\n" +
            $"Record ID: {entry.EntityId}\r\n" +
            $"Audit entry ID: {entry.Id}\r\n\r\n" +
            entry.Summary,
            "Audit event details", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static string FormatAuditAction(string action) => action switch
    {
        "CompanyApplicationSubmitted" => "Application submitted",
        "CompanyApplicationApproved" => "Application approved",
        "CompanyApplicationRejected" => "Application rejected",
        "InvitationCompleted" => "Invitation completed",
        "TenantEmployeeInvited" => "Employee invited",
        "TenantCompanySettingsUpdated" => "Company settings updated",
        "TenantCompanySettingsCompleted" => "Company setup completed",
        _ => action
    };

    private sealed record AuditGridRow(
        DateTime OccurredAt,
        string Actor,
        string Tenant,
        string Action,
        string RecordType,
        string Summary,
        PlatformAuditLogDto Source);

    private sealed class InvitationHandoffDialog : Form
    {
        private const int DialogHeaderHeight = 40;

        public InvitationHandoffDialog(CompanyApplicationApprovedDto invitation)
        {
            Theme.ThreadFlowTheme.Apply(this);

            SuspendLayout();

            Text = "Invitation created";
            BackColor = Palette.BackgroundSecondary.Color;
            ClientSize = new Size(640, 440 + DialogHeaderHeight);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;

            var header = new Panel
            {
                BackColor = Palette.Primary.Color,
                Location = Point.Empty,
                Size = new Size(640, DialogHeaderHeight),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            var headerTitle = new Label
            {
                Text = Text,
                Font = TopBarBrandFont,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(18, 0),
                Size = new Size(540, DialogHeaderHeight)
            };
            var closeHeader = new CaptionButton(CaptionGlyph.Close)
            {
                Location = new Point(594, 0),
                Size = new Size(46, DialogHeaderHeight),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            closeHeader.Click += (_, _) => Close();

            void BeginHeaderDrag(object? sender, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Left || e.Clicks != 1)
                    return;

                ReleaseCapture();
                _ = SendMessage(Handle, WmNcLButtonDown, HtCaption, 0);
            }

            header.MouseDown += BeginHeaderDrag;
            headerTitle.MouseDown += BeginHeaderDrag;
            header.Controls.Add(headerTitle);
            header.Controls.Add(closeHeader);

            var title = new Label
            {
                Text = "Invitation created",
                Font = TitleFont,
                ForeColor = Palette.TextPrimary.Color,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(24, 20 + DialogHeaderHeight),
                Size = new Size(592, 34)
            };
            var subtitle = new Label
            {
                Text = "Share these details privately with the company's Tenant Admin.",
                Font = SubtitleFont,
                ForeColor = Palette.TextSecondary.Color,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(24, 56 + DialogHeaderHeight),
                Size = new Size(592, 20)
            };
            var divider = new Panel
            {
                BackColor = BrandTints.iceBlue.Color,
                Location = new Point(24, 88 + DialogHeaderHeight),
                Size = new Size(592, 1)
            };
            var detailsBox = new TextBox
            {
                Text = BuildHandoffDetails(invitation),
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Palette.BackgroundSecondary.Color,
                ForeColor = Palette.TextPrimary.Color,
                Font = new Font("Segoe UI", 10F),
                ScrollBars = ScrollBars.Vertical,
                WordWrap = true,
                Dock = DockStyle.Fill
            };
            var detailsFrame = new HandoffDetailsFrame(detailsBox)
            {
                Location = new Point(24, 104 + DialogHeaderHeight),
                Size = new Size(592, 258)
            };

            var copy = MakeSecondaryButton("Copy details");
            copy.Location = new Point(24, 378 + DialogHeaderHeight);
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
                    MessageBox.Show(exception.Message, "Could not copy",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            var done = MakePrimaryButton("Done");
            done.Location = new Point(466, 378 + DialogHeaderHeight);
            done.Size = new Size(150, ButtonHeight);
            done.DialogResult = DialogResult.Cancel;

            Controls.AddRange([header, title, subtitle, divider, detailsFrame, copy, done]);

            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;

            ResumeLayout(false);
            PerformLayout();

            CancelButton = done;
        }

        private static string BuildHandoffDetails(CompanyApplicationApprovedDto invitation) =>
            $"Company: {invitation.CompanyName}\r\n" +
            $"Plan: {invitation.CompanySize}\r\n" +
            $"Tenant code: {invitation.TenantCode}\r\n" +
            $"Tenant Admin: {invitation.TenantAdminEmail}\r\n\r\n" +
            $"Setup token (share privately):\r\n{invitation.TenantAdminSetupToken}\r\n\r\n" +
            "The Tenant Admin opens ThreadFlow, chooses 'Complete Tenant Admin invitation', " +
            "and enters these values plus company settings and a password.";
    }

    private sealed class HandoffDetailsFrame : Panel
    {
        public HandoffDetailsFrame(TextBox details)
        {
            BackColor = Palette.BackgroundSecondary.Color;
            Padding = new Padding(14, 12, 14, 12);
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            Controls.Add(details);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;

            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundedRectangle(bounds, 7);
            using var pen = new Pen(BrandTints.powderBlue.Color, 1f);
            e.Graphics.DrawPath(pen, path);
        }
    }

    private async Task ApproveSelectedAsync()
    {
        var application = GetSelectedApplication();
        if (application is null)
            return;
        if (application.Status != "Pending" && application.Status != "Provisioning")
        {
            MessageBox.Show("Select a Pending application, or retry a Provisioning application.",
                "Application status", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var answer = MessageBox.Show(
            $"Approve {application.CompanyName} for the {application.RequestedCompanySize} plan and provision a separate tenant database?",
            "Confirm approval", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes)
            return;

        _approve.Enabled = false;
        try
        {
            var approval = await _api.ApproveAsync(
                application.Id, application.RequestedCompanySize);
            using var invitation = new InvitationHandoffDialog(approval);
            invitation.ShowDialog(this);
            await RefreshApplicationsAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Approval failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _approve.Enabled = true;
        }
    }

    private async Task RejectSelectedAsync()
    {
        var application = GetSelectedApplication();
        if (application is null || application.Status != "Pending")
            return;

        if (MessageBox.Show(
                $"Reject the application from {application.CompanyName}?",
                "Confirm rejection", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        _reject.Enabled = false;
        try
        {
            await _api.RejectAsync(application.Id, "Rejected by Platform Admin.");
            await RefreshApplicationsAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Rejection failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _reject.Enabled = true;
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
    /// A white sidebar row with a line icon. The active page gets a full-width light-blue
    /// highlight, matching the navigation style used by the platform tools.
    /// </summary>
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
                case NavIcon.Applications:
                    {
                        // Isometric cube (like the Containers icon in Docker Desktop).
                        var top = new PointF(cx, cy - r);
                        var upperRight = new PointF(cx + r * 0.87f, cy - r * 0.5f);
                        var lowerRight = new PointF(cx + r * 0.87f, cy + r * 0.5f);
                        var bottom = new PointF(cx, cy + r);
                        var lowerLeft = new PointF(cx - r * 0.87f, cy + r * 0.5f);
                        var upperLeft = new PointF(cx - r * 0.87f, cy - r * 0.5f);
                        var center = new PointF(cx, cy);

                        g.DrawPolygon(pen, [top, upperRight, lowerRight, bottom, lowerLeft, upperLeft]);
                        g.DrawLine(pen, upperLeft, center);
                        g.DrawLine(pen, upperRight, center);
                        g.DrawLine(pen, center, bottom);
                        break;
                    }

                case NavIcon.Audit:
                    {
                        // Rounded document with short text lines (like the Logs icon).
                        var width = (int)Math.Round(14 * scale);
                        var height = (int)Math.Round(17 * scale);
                        var left = bounds.X + (bounds.Width - width) / 2;
                        var top = bounds.Y + (bounds.Height - height) / 2;
                        using (var path = RoundedRectangle(new Rectangle(left, top, width, height), (int)Math.Round(3 * scale)))
                        {
                            g.DrawPath(pen, path);
                        }

                        var lineLeft = left + (int)Math.Round(3.5f * scale);
                        var lineRight = left + width - (int)Math.Round(3.5f * scale);
                        g.DrawLine(pen, lineLeft, top + (int)Math.Round(5 * scale), lineRight, top + (int)Math.Round(5 * scale));
                        g.DrawLine(pen, lineLeft, top + (int)Math.Round(8.5f * scale), lineRight, top + (int)Math.Round(8.5f * scale));
                        g.DrawLine(pen, lineLeft, top + (int)Math.Round(12 * scale), lineLeft + (int)Math.Round(3 * scale), top + (int)Math.Round(12 * scale));
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

    /// <summary>Small dark-blue pill with white text (the "PERSONAL" badge in Docker Desktop).</summary>
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
    /// A row of rounded options where exactly one is selected. Used instead of a drop-down for the
    /// application filter. It exposes SelectedIndex and SelectedIndexChanged like a ComboBox, so the
    /// filtering code does not change.
    /// </summary>
    private sealed class SegmentedFilter : Control
    {
        private readonly string[] _items;
        private int _selectedIndex = -1;
        private int _hoverIndex = -1;

        public SegmentedFilter(params string[] items)
        {
            _items = items;
            Font = SecondaryButtonFont;
            Cursor = Cursors.Hand;
            TabStop = true;

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
        }

        public event EventHandler? SelectedIndexChanged;

        [DefaultValue(-1)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (value < 0 || value >= _items.Length || value == _selectedIndex)
                    return;

                _selectedIndex = value;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private Rectangle GetSegment(int index)
        {
            var gap = (int)Math.Round(8 * DeviceDpi / 96f);
            var count = Math.Max(1, _items.Length);
            var width = (Width - gap * (count - 1)) / count;
            return new Rectangle(index * (width + gap), 0, width - 1, Height - 1);
        }

        private int IndexAt(Point point)
        {
            for (var index = 0; index < _items.Length; index++)
            {
                if (GetSegment(index).Contains(point))
                    return index;
            }

            return -1;
        }

        protected override bool IsInputKey(Keys keyData) =>
            keyData is Keys.Left or Keys.Right or Keys.Up or Keys.Down || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Left or Keys.Up)
            {
                SelectedIndex = _selectedIndex - 1;
                e.Handled = true;
            }
            else if (e.KeyCode is Keys.Right or Keys.Down)
            {
                SelectedIndex = _selectedIndex + 1;
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var index = IndexAt(e.Location);
            if (index != _hoverIndex)
            {
                _hoverIndex = index;
                Invalidate();
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hoverIndex = -1;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Focus();
                var index = IndexAt(e.Location);
                if (index >= 0)
                    SelectedIndex = index;
            }

            base.OnMouseDown(e);
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

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var radius = (int)Math.Round(8 * DeviceDpi / 96f);

            for (var index = 0; index < _items.Length; index++)
            {
                var bounds = GetSegment(index);
                var selected = index == _selectedIndex;
                var hover = index == _hoverIndex;

                // Selected: light blue with a blue outline. Others: white with a soft outline.
                var fillColor = selected || hover
                    ? Palette.BackgroundPrimary.Color
                    : Palette.BackgroundSecondary.Color;
                var borderColor = selected ? Palette.Primary.Color
                    : hover ? BrandTints.softBlue.Color
                    : BrandTints.powderBlue.Color;
                var textColor = selected ? Palette.Primary.Color
                    : hover ? Palette.TextPrimary.Color
                    : Palette.TextSecondary.Color;

                using (var path = RoundedRectangle(bounds, radius))
                using (var fill = new SolidBrush(fillColor))
                using (var pen = new Pen(borderColor, 1f))
                {
                    g.FillPath(fill, path);
                    g.DrawPath(pen, path);
                }

                TextRenderer.DrawText(
                    g,
                    _items[index],
                    Font,
                    bounds,
                    textColor,
                    TextFormatFlags.HorizontalCenter |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.SingleLine |
                    TextFormatFlags.NoPrefix |
                    TextFormatFlags.EndEllipsis);
            }

            // Keyboard focus cue (shown when the user navigates with Tab).
            if (Focused && ShowFocusCues && _selectedIndex >= 0)
            {
                var inset = (int)Math.Round(4 * DeviceDpi / 96f);
                var ringBounds = Rectangle.Inflate(GetSegment(_selectedIndex), -inset, -inset);
                using var ring = RoundedRectangle(ringBounds, Math.Max(2, radius - inset));
                using var ringPen = new Pen(Color.FromArgb(150, Palette.Primary.Color), 1f);
                g.DrawPath(ringPen, ring);
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
}