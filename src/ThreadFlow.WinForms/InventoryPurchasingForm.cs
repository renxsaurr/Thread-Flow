using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using ThreadFlow.WinForms.ApiClient;
using ThreadFlow.WinForms.Theme.Colors;

namespace ThreadFlow.WinForms
{

    /// <summary>
    /// Inventory/Purchasing Staff workspace for the product catalog, suppliers and stock receipts.
    /// Same shell as the Platform Admin screen: blue header bar with search, sidebar navigation and
    /// Docker Desktop-style tables. Creating and editing records happens in focused dialogs instead of
    /// forms squeezed under the table.
    /// </summary>
    public sealed class InventoryPurchasingForm : Form
    {
        // All sizes below are written for 96 DPI (100% scaling).
        // AutoScaleMode.Dpi scales them up on higher-DPI screens, like the auth screens.
        private const int FormWidth = 1120;
        private const int FormHeight = 680;
        private const int SidebarWidth = 232;
        private const int SideMargin = 32;
        private const int HeaderHeight = 104;
        private const int FooterHeight = 88;
        private const int ButtonHeight = 42;
        private const int ContentLeft = SidebarWidth + 1 + SideMargin;
        private const int TopBarHeight = 56;
        private const int NavItemHeight = 40;
        private const int NavInset = 16;
        private const int FieldRowHeight = 70;
        private const int AvatarSize = 40;
        private const int AvatarGap = 8;

        // The role shown in the header pill and in the account card.
        private const string AccountRoleLabel = "INVENTORY / PURCHASING";

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
        private static readonly Color AvatarFillColor = Color.FromArgb(30, 64, 175);
        private static readonly Color ProfileRolePillColor = Color.FromArgb(107, 114, 128);
        private static readonly Color TableRowHighlightColor = Color.FromArgb(224, 224, 224);
        private static readonly Color TableLineColor = Color.FromArgb(228, 231, 236);
        private static readonly Color TableHeaderLineColor = Color.FromArgb(208, 213, 221);
        private static readonly Color ErrorTextColor = Color.FromArgb(185, 28, 28);
        private static readonly Color ActivePillBack = Color.FromArgb(220, 244, 230);
        private static readonly Color ActivePillText = Color.FromArgb(22, 101, 52);
        private static readonly Color InactivePillBack = Color.FromArgb(235, 236, 239);
        private static readonly Color InactivePillText = Color.FromArgb(95, 105, 120);

        private static readonly Font TitleFont = new("Segoe UI", 18F, FontStyle.Bold);
        private static readonly Font SubtitleFont = new("Segoe UI", 9.5F);
        private static readonly Font GridFont = new("Segoe UI", 9F);
        private static readonly Font GridHeaderFont = new("Segoe UI Semibold", 9F);
        private static readonly Font PrimaryButtonFont = new("Segoe UI", 10F, FontStyle.Bold);
        private static readonly Font SecondaryButtonFont = new("Segoe UI", 10F);
        private static readonly Font TopBarBrandFont = new("Segoe UI Semibold", 13F);
        private static readonly Font SearchFont = new("Segoe UI", 10F);
        private static readonly Font PillFont = new("Segoe UI Semibold", 7.5F);
        private static readonly Font AvatarFont = new("Segoe UI Semibold", 11F);
        private static readonly Font ProfileInitialFont = new("Segoe UI Semibold", 24F);
        private static readonly Font ProfileRoleFont = new("Segoe UI Semibold", 8F);
        private static readonly Font ProfileEmailFont = new("Segoe UI", 11F);
        private static readonly Font NavFont = new("Segoe UI Semibold", 9.5F);
        private static readonly Font FieldFont = new("Segoe UI", 10F);
        private static readonly Font FieldLabelFont = new("Segoe UI Semibold", 9F);

        private const string StatusColumn = "Status";

        private readonly InventoryCatalogApiClient _api = new();

        private readonly DataGridView _productsGrid = new();
        private readonly DataGridView _suppliersGrid = new();
        private readonly DataGridView _receiptsGrid = new();
        private readonly BranchBadge _branchBadge = new();

        private readonly RoundedButton _refresh = MakeSecondaryButton("Refresh");
        private readonly RoundedButton _newProduct = MakePrimaryButton("New product");
        private readonly RoundedButton _editProduct = MakeSecondaryButton("Edit");
        private readonly RoundedButton _receiveFromProducts = MakeSecondaryButton("Receive stock");
        private readonly RoundedButton _toggleProduct = MakeSecondaryButton("Deactivate");
        private readonly RoundedButton _newSupplier = MakePrimaryButton("New supplier");
        private readonly RoundedButton _editSupplier = MakeSecondaryButton("Edit");
        private readonly RoundedButton _toggleSupplier = MakeSecondaryButton("Deactivate");
        private readonly RoundedButton _receiveFromReceipts = MakePrimaryButton("Receive stock");

        private readonly SidebarNavButton _navProducts = new("Products", NavIcon.Products);
        private readonly SidebarNavButton _navSuppliers = new("Suppliers", NavIcon.Suppliers);
        private readonly SidebarNavButton _navReceipts = new("Stock Receipts", NavIcon.Receipts);
        private readonly SidebarNavButton _navLogout = new("Log out", NavIcon.LogOut);

        private readonly Panel _topBar = new ResizeEdgePanel();
        private readonly Panel _content = new ResizeEdgePanel();
        private readonly Panel _sidebar = new ResizeEdgePanel();
        private readonly Panel _sidebarEdge = new();
        private readonly HeaderSearchBox _search = new("Search products");
        private readonly CaptionButton _captionMinimize = new(CaptionGlyph.Minimize);
        private readonly CaptionButton _captionMaximize = new(CaptionGlyph.Maximize);
        private readonly CaptionButton _captionClose = new(CaptionGlyph.Close);
        private readonly AvatarButton _avatar = new();

        // The account card that drops down from the avatar in the header.
        private string _accountEmail = string.Empty;
        private ProfilePopup? _profilePopup;
        private long _profileClosedTick;
        private bool _ignoreAvatarClick;

        private readonly Label _title = new();
        private readonly Label _subtitle = new();
        private readonly Panel _pageHost = new();
        private readonly Panel _productsPage = new();
        private readonly Panel _suppliersPage = new();
        private readonly Panel _receiptsPage = new();
        private readonly Label _productsHint = new();
        private readonly Label _suppliersHint = new();
        private readonly Label _receiptsHint = new();

        private List<InventoryProductDto> _products = [];
        private List<InventorySupplierDto> _suppliers = [];
        private List<InventoryBranchDto> _branches = [];
        private List<InventoryStockReceiptDto> _receipts = [];

        private string _productSearchText = string.Empty;
        private string _supplierSearchText = string.Empty;
        private string _receiptSearchText = string.Empty;
        private string _productsHintText = string.Empty;
        private string _suppliersHintText = string.Empty;
        private string _receiptsHintText = string.Empty;

        private InventoryPage _activePage = InventoryPage.Products;
        // Staff are assigned to their own branch, so it is shown (not picked) in the page header.
        private InventoryBranchChoice? _selectedBranch;

        private enum InventoryPage
        {
            Products,
            Suppliers,
            Receipts
        }

        private enum NavIcon
        {
            Products,
            Suppliers,
            Receipts,
            LogOut
        }

        public InventoryPurchasingForm()
        {
            var user = ApiSession.Current?.User
                ?? throw new InvalidOperationException("Sign in before opening the workspace.");

            // Apply the shared palette first, then style our own controls the same way the
            // welcome and sign-in screens do.
            Theme.ThreadFlowTheme.Apply(this);

            SuspendLayout();

            _accountEmail = user.Email;
            BuildForm();

            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;

            Text = $"ThreadFlow | Inventory and Purchasing | {user.Email}";
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

            Load += async (_, _) => await RefreshAllAsync();
        }

        private void BuildForm()
        {
            _content.SuspendLayout();
            _content.Size = new Size(FormWidth, FormHeight);
            _content.BackColor = Palette.BackgroundSecondary.Color;

            BuildSidebar();

            _title.Font = TitleFont;
            _title.ForeColor = Palette.TextPrimary.Color;
            _title.BackColor = Color.Transparent;
            _title.AutoSize = false;
            _title.TextAlign = ContentAlignment.MiddleLeft;
            _title.Location = new Point(ContentLeft, 24);
            _title.Size = new Size(320, 34);

            _subtitle.Font = SubtitleFont;
            _subtitle.ForeColor = Palette.TextSecondary.Color;
            _subtitle.BackColor = Color.Transparent;
            _subtitle.AutoSize = false;
            _subtitle.TextAlign = ContentAlignment.MiddleLeft;
            _subtitle.Location = new Point(ContentLeft, 60);
            _subtitle.Size = new Size(320, 20);

            _refresh.Location = new Point(FormWidth - SideMargin - 110, 40);
            _refresh.Size = new Size(110, 36);
            _refresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _refresh.Click += async (_, _) => await RefreshActiveViewAsync();

            // Branch badge: stock levels and receipts belong to the branch this employee is assigned
            // to, so it is shown read-only in the page header (hidden on the Suppliers page, which
            // is not branch specific). The badge keeps its right edge next to Refresh.
            _branchBadge.SetBounds(FormWidth - SideMargin - 110 - 12 - 200, 40, 200, 36);
            _branchBadge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _branchBadge.BranchName = null;

            var contentWidth = FormWidth - ContentLeft - SideMargin;

            var headerDivider = new Panel
            {
                BackColor = BrandTints.iceBlue.Color,
                Location = new Point(ContentLeft, 92),
                Size = new Size(contentWidth, 1),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            StyleGrid(_productsGrid);
            StyleGrid(_suppliersGrid);
            StyleGrid(_receiptsGrid);
            ConfigureProductsGrid();
            ConfigureSuppliersGrid();
            ConfigureReceiptsGrid();

            TrackUserSelection(_productsGrid);
            TrackUserSelection(_suppliersGrid);
            TrackUserSelection(_receiptsGrid);

            _productsGrid.SelectionChanged += (_, _) => UpdateProductActions();
            _productsGrid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0)
                    ShowProductDialog(CurrentProduct());
            };
            _suppliersGrid.SelectionChanged += (_, _) => UpdateSupplierActions();
            _suppliersGrid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0)
                    ShowSupplierDialog(CurrentSupplier());
            };
            _receiptsGrid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0)
                    ShowSelectedReceiptDetails();
            };

            BuildProductsPage();
            BuildSuppliersPage();
            BuildReceiptsPage();

            _pageHost.Location = new Point(ContentLeft, HeaderHeight);
            _pageHost.Size = new Size(contentWidth, FormHeight - HeaderHeight);
            _pageHost.BackColor = Palette.BackgroundSecondary.Color;
            _pageHost.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _pageHost.Controls.AddRange([_productsPage, _suppliersPage, _receiptsPage]);

            _content.Controls.AddRange([
                _sidebar, _sidebarEdge, _title, _subtitle, _refresh, _branchBadge,
            headerDivider, _pageHost
            ]);
            _content.Dock = DockStyle.Fill;
            _content.ResumeLayout(false);

            // Order matters for docking: the Fill panel is added first, the top bar after it.
            Controls.Add(_content);
            BuildTopBar();

            ShowPage(InventoryPage.Products);
            UpdateProductActions();
            UpdateSupplierActions();
            UpdateReceiveActions();
        }

        // ---- Pages ----

        private void BuildProductsPage()
        {
            var card = MakeGridCard(_productsGrid);
            var footer = MakeFooter(
                _productsHint,
                _newProduct, _receiveFromProducts, _editProduct, _toggleProduct);

            _newProduct.Size = new Size(130, ButtonHeight);
            _receiveFromProducts.Size = new Size(130, ButtonHeight);
            _editProduct.Size = new Size(80, ButtonHeight);
            _toggleProduct.Size = new Size(116, ButtonHeight);

            _newProduct.Click += (_, _) => ShowProductDialog(null);
            _editProduct.Click += (_, _) => ShowProductDialog(CurrentProduct());
            _toggleProduct.Click += async (_, _) => await ToggleProductAsync();
            _receiveFromProducts.Click += (_, _) => ShowReceiveDialog(CurrentProduct());

            AddPage(_productsPage, card, footer);
        }

        private void BuildSuppliersPage()
        {
            var card = MakeGridCard(_suppliersGrid);
            var footer = MakeFooter(_suppliersHint, _newSupplier, _editSupplier, _toggleSupplier);

            _newSupplier.Size = new Size(140, ButtonHeight);
            _editSupplier.Size = new Size(80, ButtonHeight);
            _toggleSupplier.Size = new Size(116, ButtonHeight);

            _newSupplier.Click += (_, _) => ShowSupplierDialog(null);
            _editSupplier.Click += (_, _) => ShowSupplierDialog(CurrentSupplier());
            _toggleSupplier.Click += async (_, _) => await ToggleSupplierAsync();

            AddPage(_suppliersPage, card, footer);
        }

        private void BuildReceiptsPage()
        {
            var card = MakeGridCard(_receiptsGrid);
            var footer = MakeFooter(_receiptsHint, _receiveFromReceipts);

            _receiveFromReceipts.Size = new Size(150, ButtonHeight);
            _receiveFromReceipts.Click += (_, _) => ShowReceiveDialog(null);

            AddPage(_receiptsPage, card, footer);
        }

        private static Panel MakeGridCard(DataGridView grid)
        {
            grid.Dock = DockStyle.Fill;
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Palette.BackgroundSecondary.Color,
                Padding = Padding.Empty
            };
            card.Controls.Add(grid);
            return card;
        }

        /// <summary>
        /// The strip under every table: a short message on the left and that page's actions on the
        /// right (the first button passed in ends up right-most, like Approve in Platform Admin).
        /// </summary>
        private static Panel MakeFooter(Label hint, params RoundedButton[] buttons)
        {
            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = FooterHeight,
                BackColor = Palette.BackgroundSecondary.Color
            };

            hint.Font = SubtitleFont;
            hint.ForeColor = Palette.TextSecondary.Color;
            hint.BackColor = Palette.BackgroundSecondary.Color;
            hint.AutoSize = false;
            hint.AutoEllipsis = true;
            hint.Dock = DockStyle.Fill;
            hint.TextAlign = ContentAlignment.MiddleLeft;
            hint.Padding = new Padding(4, 0, 12, 0);
            hint.Margin = Padding.Empty;

            var buttonRow = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Palette.BackgroundSecondary.Color,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                Anchor = AnchorStyles.Right
            };
            foreach (var button in buttons)
            {
                button.Margin = new Padding(12, 0, 0, 0);
                buttonRow.Controls.Add(button);
            }

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Palette.BackgroundSecondary.Color,
                Padding = new Padding(0, 12, 0, 12)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            table.Controls.Add(hint, 0, 0);
            table.Controls.Add(buttonRow, 1, 0);

            var divider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = BrandTints.iceBlue.Color
            };

            // Docking runs from the last control added: the divider takes the top edge first,
            // then the table fills what is left.
            footer.Controls.Add(table);
            footer.Controls.Add(divider);
            return footer;
        }

        private static void AddPage(Panel page, Panel card, Panel footer)
        {
            page.Dock = DockStyle.Fill;
            page.BackColor = Palette.BackgroundSecondary.Color;
            page.Visible = false;

            // The Fill card is added first so the footer docks to the bottom before it fills the rest.
            page.Controls.Add(card);
            page.Controls.Add(footer);
        }

        private void ShowPage(InventoryPage page)
        {
            _activePage = page;
            _navProducts.Active = page == InventoryPage.Products;
            _navSuppliers.Active = page == InventoryPage.Suppliers;
            _navReceipts.Active = page == InventoryPage.Receipts;

            _productsPage.Visible = page == InventoryPage.Products;
            _suppliersPage.Visible = page == InventoryPage.Suppliers;
            _receiptsPage.Visible = page == InventoryPage.Receipts;

            var branchPage = page != InventoryPage.Suppliers;
            _branchBadge.Visible = branchPage;

            switch (page)
            {
                case InventoryPage.Products:
                    _title.Text = "Products";
                    _subtitle.Text = "Product catalog and stock on hand.";
                    _productsGrid.Focus();
                    break;
                case InventoryPage.Suppliers:
                    _title.Text = "Suppliers";
                    _subtitle.Text = "Vendors you purchase stock from.";
                    _suppliersGrid.Focus();
                    break;
                default:
                    _title.Text = "Stock receipts";
                    _subtitle.Text = "Recent deliveries received at a branch.";
                    _receiptsGrid.Focus();
                    break;
            }

            SwitchSearchContext(page);
        }

        private void SwitchSearchContext(InventoryPage page)
        {
            switch (page)
            {
                case InventoryPage.Products:
                    _search.PlaceholderText = "Search products";
                    _search.SetSearchText(_productSearchText);
                    break;
                case InventoryPage.Suppliers:
                    _search.PlaceholderText = "Search suppliers";
                    _search.SetSearchText(_supplierSearchText);
                    break;
                default:
                    _search.PlaceholderText = "Search stock receipts";
                    _search.SetSearchText(_receiptSearchText);
                    break;
            }
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
                Size = new Size(160, 22)
            };

            _search.Size = new Size(400, 36);
            _search.SearchChanged += (_, _) =>
            {
                // The search box only filters the page that is currently open.
                switch (_activePage)
                {
                    case InventoryPage.Products:
                        _productSearchText = _search.SearchText;
                        BindProducts();
                        break;
                    case InventoryPage.Suppliers:
                        _supplierSearchText = _search.SearchText;
                        BindSuppliers();
                        break;
                    default:
                        _receiptSearchText = _search.SearchText;
                        BindReceipts();
                        break;
                }
            };

            // Keep the search box centred in the bar, clear of the brand, the avatar and the window buttons.
            void CenterSearch()
            {
                var scale = DeviceDpi / 96f;
                var margin = (int)Math.Round(16 * scale);
                var minLeft = pill.Right + margin;
                var maxLeft = _topBar.Width - (int)Math.Round((46 * 3 + AvatarGap + AvatarSize) * scale) - margin - _search.Width;
                var left = Math.Clamp((_topBar.Width - _search.Width) / 2, minLeft, Math.Max(minLeft, maxLeft));
                _search.Location = new Point(left, (_topBar.Height - _search.Height) / 2);
            }

            _topBar.Resize += (_, _) => CenterSearch();
            CenterSearch();

            const int captionWidth = 46;

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

            var navItems = new[] { _navProducts, _navSuppliers, _navReceipts };
            for (var i = 0; i < navItems.Length; i++)
            {
                var item = navItems[i];
                item.Location = new Point(0, 16 + i * (NavItemHeight + 4));
                item.Size = new Size(SidebarWidth, NavItemHeight);
                item.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            }

            _navProducts.Click += (_, _) => ShowPage(InventoryPage.Products);
            _navSuppliers.Click += (_, _) => ShowPage(InventoryPage.Suppliers);
            _navReceipts.Click += (_, _) => ShowPage(InventoryPage.Receipts);

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

            _sidebar.Controls.AddRange([_navProducts, _navSuppliers, _navReceipts, navDivider, _navLogout]);

            _sidebarEdge.Location = new Point(SidebarWidth, 0);
            _sidebarEdge.Size = new Size(1, FormHeight);
            _sidebarEdge.BackColor = SidebarBackColor; // no visible edge, like Docker Desktop
            _sidebarEdge.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
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

        // Grids that the person has actually clicked or typed in. Until then, any row the
        // DataGridView highlights by itself (it picks the first row when first shown) is cleared.
        private readonly HashSet<DataGridView> _userSelectedGrids = [];

        private void TrackUserSelection(DataGridView grid)
        {
            grid.MouseDown += (_, e) =>
            {
                if (grid.HitTest(e.X, e.Y).RowIndex >= 0)
                    _userSelectedGrids.Add(grid);
            };
            grid.KeyDown += (_, _) => _userSelectedGrids.Add(grid);

            void ClearIfUntouched()
            {
                if (!_userSelectedGrids.Contains(grid))
                    ClearGridSelection(grid);
            }

            // The automatic selection happens after the grid is shown/laid out, so clear it
            // once the current message has finished processing.
            void ClearIfUntouchedLater()
            {
                if (grid.IsHandleCreated)
                    grid.BeginInvoke(ClearIfUntouched);
            }

            grid.DataBindingComplete += (_, _) => ClearIfUntouched();
            grid.HandleCreated += (_, _) => ClearIfUntouchedLater();
            grid.VisibleChanged += (_, _) =>
            {
                if (grid.Visible)
                    ClearIfUntouchedLater();
            };
        }

        private static void ClearGridSelection(DataGridView grid)
        {
            // Nothing is highlighted until the person clicks a row.
            grid.ClearSelection();
            grid.CurrentCell = null;
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
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && grid.Columns[e.ColumnIndex].Name == StatusColumn)
                {
                    PaintStatusPill(e);
                    return;
                }

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

        // ---- Tables ----

        private void ConfigureProductsGrid()
        {
            // Unbreakable values (SKU, prices, numbers) size to their content so they are never cut off.
            // Names and suppliers share the leftover space and wrap onto more lines.
            const DataGridViewAutoSizeColumnMode fit = DataGridViewAutoSizeColumnMode.AllCells;
            const DataGridViewAutoSizeColumnMode fill = DataGridViewAutoSizeColumnMode.Fill;

            AddGridColumn(_productsGrid, nameof(ProductRow.Sku), "SKU", fit, 100, 80);
            AddGridColumn(_productsGrid, nameof(ProductRow.Name), "Product", fill, 160, 120, wrap: true);
            AddGridColumn(_productsGrid, nameof(ProductRow.Color), "Color", fit, 100, 70);
            AddGridColumn(_productsGrid, nameof(ProductRow.Size), "Size", fit, 100, 60);
            AddGridColumn(_productsGrid, nameof(ProductRow.UnitPrice), "Unit price", fit, 100, 90, format: "N2", alignRight: true);
            AddGridColumn(_productsGrid, nameof(ProductRow.Supplier), "Supplier", fill, 120, 100, wrap: true);
            AddGridColumn(_productsGrid, nameof(ProductRow.OnHand), "On hand", fit, 100, 80, format: "N0", alignRight: true);
            AddGridColumn(_productsGrid, StatusColumn, "Status", fit, 100, 90);
        }

        private void ConfigureSuppliersGrid()
        {
            const DataGridViewAutoSizeColumnMode fit = DataGridViewAutoSizeColumnMode.AllCells;
            const DataGridViewAutoSizeColumnMode fill = DataGridViewAutoSizeColumnMode.Fill;

            AddGridColumn(_suppliersGrid, nameof(SupplierRow.Name), "Supplier", fill, 140, 110, wrap: true);
            AddGridColumn(_suppliersGrid, nameof(SupplierRow.Contact), "Contact", fill, 110, 100, wrap: true);
            AddGridColumn(_suppliersGrid, nameof(SupplierRow.Email), "Email", fit, 100, 120);
            AddGridColumn(_suppliersGrid, nameof(SupplierRow.Phone), "Phone", fit, 100, 90);
            AddGridColumn(_suppliersGrid, nameof(SupplierRow.Address), "Address", fill, 200, 130, wrap: true);
            AddGridColumn(_suppliersGrid, StatusColumn, "Status", fit, 100, 90);
        }

        private void ConfigureReceiptsGrid()
        {
            const DataGridViewAutoSizeColumnMode fit = DataGridViewAutoSizeColumnMode.AllCells;
            const DataGridViewAutoSizeColumnMode fill = DataGridViewAutoSizeColumnMode.Fill;

            AddGridColumn(_receiptsGrid, nameof(ReceiptRow.ReceivedAt), "Received", fill, 90, 95, wrap: true, format: "g");
            AddGridColumn(_receiptsGrid, nameof(ReceiptRow.Sku), "SKU", fit, 100, 80);
            AddGridColumn(_receiptsGrid, nameof(ReceiptRow.Product), "Product", fill, 150, 120, wrap: true);
            AddGridColumn(_receiptsGrid, nameof(ReceiptRow.Branch), "Branch", fill, 100, 90, wrap: true);
            AddGridColumn(_receiptsGrid, nameof(ReceiptRow.Supplier), "Supplier", fill, 110, 100, wrap: true);
            AddGridColumn(_receiptsGrid, nameof(ReceiptRow.Quantity), "Quantity", fit, 100, 80, format: "N0", alignRight: true);
            AddGridColumn(_receiptsGrid, nameof(ReceiptRow.Reference), "Reference", fill, 120, 100, wrap: true);
        }

        private static void AddGridColumn(
            DataGridView grid,
            string property,
            string title,
            DataGridViewAutoSizeColumnMode sizeMode,
            float fillWeight,
            int minimumWidth,
            bool wrap = false,
            string? format = null,
            bool alignRight = false)
        {
            var alignment = alignRight
                ? DataGridViewContentAlignment.MiddleRight
                : DataGridViewContentAlignment.MiddleLeft;

            var column = new DataGridViewTextBoxColumn
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
                WrapMode = wrap ? DataGridViewTriState.True : DataGridViewTriState.False,
                Alignment = alignment
            }
            };
            column.HeaderCell.Style.Alignment = alignment;
            grid.Columns.Add(column);
        }

        // The Active / Inactive cell is drawn as a small rounded pill instead of plain text.
        private void PaintStatusPill(DataGridViewCellPaintingEventArgs e)
        {
            e.Paint(e.ClipBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);
            e.Handled = true;

            var text = Convert.ToString(e.FormattedValue) ?? string.Empty;
            if (text.Length == 0 || e.Graphics is not { } g)
                return;

            var scale = DeviceDpi / 96f;
            var active = text == "Active";
            var textSize = TextRenderer.MeasureText(
                g, text, GridHeaderFont, new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            var width = textSize.Width + (int)Math.Round(20 * scale);
            var height = (int)Math.Round(22 * scale);
            var bounds = new Rectangle(
                e.CellBounds.Left + (int)Math.Round(10 * scale),
                e.CellBounds.Top + (e.CellBounds.Height - height) / 2,
                width,
                height);

            var state = g.Save();
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = RoundedRectangle(bounds, bounds.Height / 2))
            using (var fill = new SolidBrush(active ? ActivePillBack : InactivePillBack))
            {
                g.FillPath(fill, path);
            }

            g.Restore(state);
            TextRenderer.DrawText(
                g,
                text,
                GridHeaderFont,
                bounds,
                active ? ActivePillText : InactivePillText,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.NoPrefix |
                TextFormatFlags.NoPadding);
        }

        private static void SelectRow<T>(DataGridView grid, Func<T, bool> match) where T : class
        {
            var firstVisible = grid.Columns.GetFirstColumn(DataGridViewElementStates.Visible);
            if (firstVisible is not null)
            {
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.DataBoundItem is T item && match(item))
                    {
                        grid.CurrentCell = row.Cells[firstVisible.Index];
                        row.Selected = true;
                        return;
                    }
                }
            }

            ClearGridSelection(grid);
        }

        // ---- Data: loading ----

        private async Task RefreshAllAsync(Guid? selectSupplierId = null)
        {
            await RefreshBranchesAsync();
            await RefreshSuppliersAsync(selectSupplierId);
            await RefreshProductsAsync();
            await RefreshReceiptsAsync();
        }

        private async Task RefreshActiveViewAsync()
        {
            _refresh.Enabled = false;
            try
            {
                switch (_activePage)
                {
                    case InventoryPage.Products:
                        await RefreshProductsAsync();
                        break;
                    case InventoryPage.Suppliers:
                        await RefreshSuppliersAsync();
                        break;
                    default:
                        await RefreshReceiptsAsync();
                        break;
                }
            }
            finally
            {
                _refresh.Enabled = true;
            }
        }

        private async Task RefreshBranchesAsync()
        {
            try
            {
                _branches = await _api.GetBranchesAsync();

                // Keep the branch the person was already looking at when the list is reloaded;
                // otherwise use the first (normally only) branch they are assigned to.
                var previous = _selectedBranch?.Id;
                var choices = _branches.Select(branch => new InventoryBranchChoice(branch.Id, branch.Name)).ToList();
                _selectedBranch = choices.FirstOrDefault(choice => choice.Id == previous) ?? choices.FirstOrDefault();
                _branchBadge.BranchName = _selectedBranch?.Name;
                UpdateReceiveActions();
            }
            catch (Exception exception)
            {
                ShowError(exception, "Could not load branches", InventoryPage.Products);
            }
        }

        private async Task RefreshProductsAsync(Guid? selectId = null)
        {
            try
            {
                if (_selectedBranch is not { } branch)
                {
                    _products = [];
                    BindProducts();
                    return;
                }

                _products = await _api.GetProductsAsync(branch.Id);
                BindProducts(selectId);
            }
            catch (Exception exception)
            {
                ShowError(exception, "Could not load products", InventoryPage.Products);
            }
        }

        private async Task RefreshSuppliersAsync(Guid? selectId = null)
        {
            try
            {
                _suppliers = await _api.GetSuppliersAsync();
                BindSuppliers(selectId);
            }
            catch (Exception exception)
            {
                ShowError(exception, "Could not load suppliers", InventoryPage.Suppliers);
            }
        }

        private async Task RefreshReceiptsAsync()
        {
            try
            {
                if (_selectedBranch is not { } branch)
                {
                    _receipts = [];
                    BindReceipts();
                    return;
                }

                _receipts = await _api.GetReceiptsAsync(branch.Id);
                BindReceipts();
            }
            catch (Exception exception)
            {
                ShowError(exception, "Could not load stock receipts", InventoryPage.Receipts);
            }
        }

        // ---- Data: binding to the tables ----

        private void RefreshHints()
        {
            _productsHint.Text = _productsHintText;
            _suppliersHint.Text = _suppliersHintText;
            _receiptsHint.Text = _receiptsHintText;
        }

        private void BindProducts(Guid? selectId = null)
        {
            if (selectId is not null)
                _userSelectedGrids.Add(_productsGrid);
            var keep = selectId ?? (_userSelectedGrids.Contains(_productsGrid) ? CurrentProduct()?.Id : null);
            var query = _productSearchText.Trim();
            var searching = query.Length > 0;

            var rows = _products
                .Where(product => !searching || MatchesSearch(
                    query,
                    product.Sku,
                    product.Name,
                    product.Color,
                    product.Size,
                    product.UnitPrice.ToString("N2"),
                    product.SupplierName,
                    product.QuantityOnHand,
                    product.IsActive ? "Active" : "Inactive"))
                .Select(ToProductRow)
                .ToList();

            _productsGrid.DataSource = null;
            _productsGrid.DataSource = rows;
            SizeColumnsToContent(_productsGrid);
            if (keep is Guid id)
                SelectRow<ProductRow>(_productsGrid, row => row.Source.Id == id);
            else
                ClearGridSelection(_productsGrid);

            _productsHintText = _selectedBranch is null
                ? "No available branch was found for this account."
                : searching
                    ? rows.Count == 0
                        ? $"No products match \"{query}\"."
                        : $"Showing {rows.Count} matching product(s). Double-click a row to edit."
                    : rows.Count == 0
                        ? "No products yet. Choose New product to add one."
                        : $"{rows.Count} product(s). Double-click a row to edit.";
            RefreshHints();
            UpdateProductActions();
        }

        private void BindSuppliers(Guid? selectId = null)
        {
            if (selectId is not null)
                _userSelectedGrids.Add(_suppliersGrid);
            var keep = selectId ?? (_userSelectedGrids.Contains(_suppliersGrid) ? CurrentSupplier()?.Id : null);
            var query = _supplierSearchText.Trim();
            var searching = query.Length > 0;

            var rows = _suppliers
                .Where(supplier => !searching || MatchesSearch(
                    query,
                    supplier.Name,
                    supplier.ContactName,
                    supplier.Email,
                    supplier.Phone,
                    supplier.Address,
                    supplier.IsActive ? "Active" : "Inactive"))
                .Select(ToSupplierRow)
                .ToList();

            _suppliersGrid.DataSource = null;
            _suppliersGrid.DataSource = rows;
            SizeColumnsToContent(_suppliersGrid);
            if (keep is Guid id)
                SelectRow<SupplierRow>(_suppliersGrid, row => row.Source.Id == id);
            else
                ClearGridSelection(_suppliersGrid);

            _suppliersHintText = searching
                ? rows.Count == 0
                    ? $"No suppliers match \"{query}\"."
                    : $"Showing {rows.Count} matching supplier(s). Double-click a row to edit."
                : rows.Count == 0
                    ? "No suppliers yet. Choose New supplier to add one."
                    : $"{rows.Count} supplier(s). Double-click a row to edit.";
            RefreshHints();
            UpdateSupplierActions();
        }

        private void BindReceipts()
        {
            var query = _receiptSearchText.Trim();
            var searching = query.Length > 0;

            var rows = _receipts
                .Where(receipt => !searching || MatchesSearch(
                    query,
                    receipt.ReceivedAtUtc.ToLocalTime(),
                    receipt.Sku,
                    receipt.ProductName,
                    receipt.BranchName,
                    receipt.SupplierName,
                    receipt.Quantity,
                    receipt.Reference))
                .Select(ToReceiptRow)
                .ToList();

            _receiptsGrid.DataSource = null;
            _receiptsGrid.DataSource = rows;
            SizeColumnsToContent(_receiptsGrid);
            ClearGridSelection(_receiptsGrid);

            var branchName = _selectedBranch?.Name;
            _receiptsHintText = branchName is null
                ? "No available branch was found for this account."
                : searching
                    ? rows.Count == 0
                        ? $"No stock receipts match \"{query}\"."
                        : $"Showing {rows.Count} matching receipt(s) for {branchName}. Double-click a row for details."
                    : rows.Count == 0
                        ? $"No stock receipts yet for {branchName}."
                        : $"Showing {rows.Count} recent stock receipt(s) for {branchName}. Double-click a row for details.";
            RefreshHints();
        }

        private static ProductRow ToProductRow(InventoryProductDto product) => new(
            product.Sku,
            product.Name,
            product.Color ?? string.Empty,
            product.Size ?? string.Empty,
            product.UnitPrice,
            string.IsNullOrWhiteSpace(product.SupplierName) ? "—" : product.SupplierName,
            product.QuantityOnHand,
            product.IsActive ? "Active" : "Inactive",
            product);

        private static SupplierRow ToSupplierRow(InventorySupplierDto supplier) => new(
            supplier.Name,
            supplier.ContactName ?? string.Empty,
            supplier.Email ?? string.Empty,
            supplier.Phone ?? string.Empty,
            supplier.Address ?? string.Empty,
            supplier.IsActive ? "Active" : "Inactive",
            supplier);

        private static ReceiptRow ToReceiptRow(InventoryStockReceiptDto receipt) => new(
            receipt.ReceivedAtUtc.ToLocalTime(),
            receipt.Sku,
            receipt.ProductName,
            receipt.BranchName,
            string.IsNullOrWhiteSpace(receipt.SupplierName) ? "—" : receipt.SupplierName,
            receipt.Quantity,
            receipt.Reference ?? string.Empty,
            receipt);

        // ---- Selection and button state ----

        private InventoryProductDto? CurrentProduct() =>
            (_productsGrid.CurrentRow?.DataBoundItem as ProductRow)?.Source;

        private InventorySupplierDto? CurrentSupplier() =>
            (_suppliersGrid.CurrentRow?.DataBoundItem as SupplierRow)?.Source;

        private void UpdateProductActions()
        {
            var product = CurrentProduct();
            _editProduct.Enabled = product is not null;
            _toggleProduct.Enabled = product is not null;
            _toggleProduct.Text = product is { IsActive: false } ? "Reactivate" : "Deactivate";
            UpdateReceiveActions();
        }

        private void UpdateSupplierActions()
        {
            var supplier = CurrentSupplier();
            _editSupplier.Enabled = supplier is not null;
            _toggleSupplier.Enabled = supplier is not null;
            _toggleSupplier.Text = supplier is { IsActive: false } ? "Reactivate" : "Deactivate";
        }

        // Receiving stock needs a branch to receive into; the product is chosen inside the dialog.
        private void UpdateReceiveActions()
        {
            var hasBranch = _selectedBranch is not null;
            _receiveFromProducts.Enabled = hasBranch;
            _receiveFromReceipts.Enabled = hasBranch;
        }

        // ---- Actions ----

        private async Task ToggleProductAsync()
        {
            if (CurrentProduct() is not { } product)
                return;

            if (product.IsActive)
            {
                var answer = MessageBox.Show(
                    this,
                    $"Deactivate {product.Sku} — {product.Name}?\r\n\r\n" +
                    "Deactivating keeps the product on record, so sales that already reference it stay intact. " +
                    "You can reactivate it at any time.",
                    "Confirm deactivation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (answer != DialogResult.Yes)
                    return;
            }

            _toggleProduct.Enabled = false;
            try
            {
                await _api.SetProductStatusAsync(product.Id, !product.IsActive);
                await RefreshProductsAsync(product.Id);
                _productsHintText = product.IsActive ? "Product deactivated." : "Product reactivated.";
                RefreshHints();
            }
            catch (Exception exception)
            {
                ShowError(exception, "Inventory request failed", InventoryPage.Products);
            }
            finally
            {
                UpdateProductActions();
            }
        }

        private async Task ToggleSupplierAsync()
        {
            if (CurrentSupplier() is not { } supplier)
                return;

            if (supplier.IsActive)
            {
                var answer = MessageBox.Show(
                    this,
                    $"Deactivate {supplier.Name}?\r\n\r\n" +
                    "Deactivating keeps the supplier's links to existing products. " +
                    "You can reactivate it at any time.",
                    "Confirm deactivation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (answer != DialogResult.Yes)
                    return;
            }

            _toggleSupplier.Enabled = false;
            try
            {
                await _api.SetSupplierStatusAsync(supplier.Id, !supplier.IsActive);
                await RefreshAllAsync(supplier.Id);
                _suppliersHintText = supplier.IsActive ? "Supplier deactivated." : "Supplier reactivated.";
                RefreshHints();
            }
            catch (Exception exception)
            {
                ShowError(exception, "Inventory request failed", InventoryPage.Suppliers);
            }
            finally
            {
                UpdateSupplierActions();
            }
        }

        private void ShowSelectedReceiptDetails()
        {
            if (_receiptsGrid.CurrentRow?.DataBoundItem is not ReceiptRow row)
                return;

            var receipt = row.Source;
            MessageBox.Show(this,
                $"Received: {receipt.ReceivedAtUtc.ToLocalTime():f}\r\n" +
                $"SKU: {receipt.Sku}\r\n" +
                $"Product: {receipt.ProductName}\r\n" +
                $"Branch: {receipt.BranchName}\r\n" +
                $"Supplier: {(string.IsNullOrWhiteSpace(receipt.SupplierName) ? "Unspecified" : receipt.SupplierName)}\r\n" +
                $"Quantity: {receipt.Quantity}\r\n" +
                $"Reference: {(string.IsNullOrWhiteSpace(receipt.Reference) ? "None" : receipt.Reference)}\r\n" +
                $"Receipt ID: {receipt.Id}",
                "Stock receipt details", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ---- Dialogs (create / edit / receive) ----

        private void ShowProductDialog(InventoryProductDto? existing)
        {
            var isNew = existing is null;
            var sku = new TextBox { MaxLength = 48, Text = existing?.Sku ?? string.Empty };
            var name = new TextBox { MaxLength = 160, Text = existing?.Name ?? string.Empty };
            var color = new TextBox { MaxLength = 48, Text = existing?.Color ?? string.Empty };
            var size = new TextBox { MaxLength = 32, Text = existing?.Size ?? string.Empty };
            var price = new NumericUpDown
            {
                DecimalPlaces = 2,
                ThousandsSeparator = true,
                Minimum = 0,
                Maximum = 999999999999999999m
            };
            price.Value = existing is null
                ? 0
                : Math.Min(price.Maximum, Math.Max(price.Minimum, existing.UnitPrice));
            var supplier = new SelectBox();
            BindSupplierChoices(supplier, existing?.SupplierId, "— No supplier —");

            var form = MakeFormGrid(4);
            AddField(form, MakeField("SKU", sku, required: true), 0, 0);
            AddField(form, MakeField("Unit price", price), 1, 0);
            AddField(form, MakeField("Product name", name, required: true), 0, 1, span: 2);
            AddField(form, MakeField("Color", color), 0, 2);
            AddField(form, MakeField("Size", size), 1, 2);
            AddField(form, MakeField("Primary supplier", supplier), 0, 3, span: 2);

            async Task<string?> SaveAsync()
            {
                if (string.IsNullOrWhiteSpace(sku.Text) || string.IsNullOrWhiteSpace(name.Text))
                    return "SKU and product name are required.";

                try
                {
                    var request = new SaveInventoryProductRequest(
                        sku.Text.Trim(),
                        name.Text.Trim(),
                        color.Text.Trim(),
                        size.Text.Trim(),
                        price.Value,
                        (supplier.SelectedItem as SupplierChoice)?.Id);
                    var saved = await _api.SaveProductAsync(existing?.Id, request);
                    await RefreshProductsAsync(saved.Id);
                    _productsHintText = $"Saved {saved.Sku} — {saved.Name}.";
                    RefreshHints();
                    return null;
                }
                catch (Exception exception)
                {
                    return DescribeError(exception);
                }
            }

            using var dialog = new EditorDialog(
                isNew ? "New product" : "Edit product",
                isNew ? "New product" : "Edit product",
                isNew ? "Enter the product details and save." : "Update the product details and save.",
                "Save product",
                form,
                4 * FieldRowHeight,
                SaveAsync,
                () => SelectSupplierChoice(supplier, existing?.SupplierId));
            dialog.ShowDialog(this);
        }

        private void ShowSupplierDialog(InventorySupplierDto? existing)
        {
            var isNew = existing is null;
            var name = new TextBox { MaxLength = 160, Text = existing?.Name ?? string.Empty };
            var contact = new TextBox { MaxLength = 160, Text = existing?.ContactName ?? string.Empty };
            var phone = new TextBox { MaxLength = 32, Text = existing?.Phone ?? string.Empty };
            var email = new TextBox { MaxLength = 254, Text = existing?.Email ?? string.Empty };
            var address = new TextBox { MaxLength = 300, Text = existing?.Address ?? string.Empty };

            var form = MakeFormGrid(4);
            AddField(form, MakeField("Supplier name", name, required: true), 0, 0, span: 2);
            AddField(form, MakeField("Contact name", contact), 0, 1);
            AddField(form, MakeField("Phone", phone), 1, 1);
            AddField(form, MakeField("Email", email), 0, 2, span: 2);
            AddField(form, MakeField("Address", address), 0, 3, span: 2);

            async Task<string?> SaveAsync()
            {
                if (string.IsNullOrWhiteSpace(name.Text))
                    return "Supplier name is required.";

                try
                {
                    var request = new SaveInventorySupplierRequest(
                        name.Text.Trim(),
                        contact.Text.Trim(),
                        email.Text.Trim(),
                        phone.Text.Trim(),
                        address.Text.Trim());
                    var saved = await _api.SaveSupplierAsync(existing?.Id, request);

                    // Supplier names also appear on products and receipts, so everything is reloaded.
                    await RefreshAllAsync(saved.Id);
                    _suppliersHintText = $"Saved supplier {saved.Name}.";
                    RefreshHints();
                    return null;
                }
                catch (Exception exception)
                {
                    return DescribeError(exception);
                }
            }

            using var dialog = new EditorDialog(
                isNew ? "New supplier" : "Edit supplier",
                isNew ? "New supplier" : "Edit supplier",
                isNew ? "Enter the supplier details and save." : "Update the supplier details and save.",
                "Save supplier",
                form,
                4 * FieldRowHeight,
                SaveAsync);
            dialog.ShowDialog(this);
        }

        private void ShowReceiveDialog(InventoryProductDto? preselect)
        {
            if (_selectedBranch is not { } branch)
            {
                MessageBox.Show(this, "Choose an available branch first.", "Receive stock",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var choices = _products
                .Where(item => item.IsActive)
                .OrderBy(item => item.Name)
                .Select(item => new ProductChoice(
                    item.Id, $"{item.Sku} — {item.Name}", item.SupplierId))
                .ToList();
            if (choices.Count == 0)
            {
                MessageBox.Show(this,
                    "There are no active products to receive stock for. Add or reactivate a product first.",
                    "Receive stock", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var product = new SelectBox();
            product.DataSource = choices;
            product.DisplayMember = nameof(ProductChoice.Label);
            product.ValueMember = nameof(ProductChoice.Id);
            var startIndex = preselect is null ? -1 : choices.FindIndex(choice => choice.Id == preselect.Id);

            var quantity = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 1000000,
                ThousandsSeparator = true
            };
            quantity.Value = 1;
            var supplier = new SelectBox();
            var reference = new TextBox { MaxLength = 80 };

            // The product's primary supplier is suggested, but it can be changed for this delivery.
            void SyncSupplier() => BindSupplierChoices(
                supplier, (product.SelectedItem as ProductChoice)?.SupplierId, "— Unspecified supplier —");

            var form = MakeFormGrid(3);
            AddField(form, MakeField("Product", product, required: true), 0, 0, span: 2);
            AddField(form, MakeField("Quantity received", quantity, required: true), 0, 1);
            AddField(form, MakeField("Supplier", supplier), 1, 1);
            AddField(form, MakeField("Reference (optional)", reference), 0, 2, span: 2);

            async Task<string?> SaveAsync()
            {
                if (product.SelectedItem is not ProductChoice choice)
                    return "Select an active product first.";

                try
                {
                    var receipt = await _api.ReceiveStockAsync(new ReceiveInventoryStockRequest(
                        branch.Id,
                        choice.Id,
                        decimal.ToInt32(quantity.Value),
                        (supplier.SelectedItem as SupplierChoice)?.Id,
                        reference.Text.Trim()));
                    await RefreshProductsAsync(choice.Id);
                    await RefreshReceiptsAsync();

                    var message = $"Received {receipt.Quantity} unit(s) of {receipt.ProductName} at {receipt.BranchName}.";
                    _productsHintText = message;
                    _receiptsHintText = message;
                    RefreshHints();
                    return null;
                }
                catch (Exception exception)
                {
                    return DescribeError(exception);
                }
            }

            using var dialog = new EditorDialog(
                "Receive stock",
                "Receive stock",
                $"Receiving into {branch.Name}.",
                "Receive stock",
                form,
                3 * FieldRowHeight,
                SaveAsync,
                () =>
                {
                    if (product.Items.Count > 0)
                        product.SelectedIndex = startIndex >= 0 ? startIndex : 0;
                    SyncSupplier();
                });
            product.SelectedIndexChanged += (_, _) =>
            {
                if (supplier.IsHandleCreated)
                    SyncSupplier();
            };
            dialog.ShowDialog(this);
        }

        private void BindSupplierChoices(ComboBox combo, Guid? selectedId, string emptyLabel)
        {
            // Inactive suppliers are hidden unless they are the one already linked.
            var choices = new List<SupplierChoice> { new(null, emptyLabel) };
            choices.AddRange(_suppliers
                .Where(supplier => supplier.IsActive || supplier.Id == selectedId)
                .OrderBy(supplier => supplier.Name)
                .Select(supplier => new SupplierChoice(
                    supplier.Id,
                    supplier.IsActive ? supplier.Name : $"{supplier.Name} (inactive)")));

            combo.DataSource = choices;
            combo.DisplayMember = nameof(SupplierChoice.Label);
            combo.ValueMember = nameof(SupplierChoice.Id);
            // Dialog inputs are initially bound before they have a native handle. Selecting an
            // index at that point can throw because the ComboBox still reports zero native items.
            if (combo.IsHandleCreated)
                SelectSupplierChoice(combo, selectedId);
        }

        private static void SelectSupplierChoice(ComboBox combo, Guid? selectedId)
        {
            if (!combo.IsHandleCreated || combo.Items.Count == 0)
                return;

            var hasSelectedSupplier = selectedId is Guid id &&
                combo.DataSource is IEnumerable<SupplierChoice> choices &&
                choices.Any(choice => choice.Id == id);
            if (hasSelectedSupplier && selectedId is Guid supplierId)
            {
                combo.SelectedValue = supplierId;
            }
            else
                combo.SelectedIndex = 0;
        }

        // ---- Form building helpers ----

        private static TableLayoutPanel MakeFormGrid(int rows)
        {
            var grid = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = rows,
                BackColor = Palette.BackgroundSecondary.Color,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (var row = 0; row < rows; row++)
                grid.RowStyles.Add(new RowStyle(SizeType.Absolute, FieldRowHeight));

            return grid;
        }

        private static void AddField(TableLayoutPanel grid, Control field, int column, int row, int span = 1)
        {
            field.Dock = DockStyle.Fill;
            field.Margin = span > 1
                ? Padding.Empty
                : column == 0 ? new Padding(0, 0, 8, 0) : new Padding(8, 0, 0, 0);
            grid.Controls.Add(field, column, row);
            if (span > 1)
                grid.SetColumnSpan(field, span);
        }

        /// <summary>A caption above a rounded input, the same pattern for every field in every dialog.</summary>
        private static Panel MakeField(string label, Control input, bool required = false)
        {
            var panel = new Panel { BackColor = Palette.BackgroundSecondary.Color };
            var caption = new Label
            {
                Text = required ? $"{label} *" : label,
                Font = FieldLabelFont,
                ForeColor = Palette.TextPrimary.Color,
                BackColor = Color.Transparent,
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var frame = new FieldFrame(input) { Dock = DockStyle.Top, Height = 38 };

            // Docking runs from the last control added: the caption takes the top, the input sits below.
            panel.Controls.Add(frame);
            panel.Controls.Add(caption);
            return panel;
        }

        private static string DescribeError(Exception exception)
        {
            var message = exception.Message;
            if (message.Length > 240)
                message = message[..240] + "…";

            return $"Could not complete the request. {message}";
        }

        private void ShowError(Exception exception, string caption, InventoryPage page)
        {
            switch (page)
            {
                case InventoryPage.Products:
                    _productsHintText = "Could not complete the request.";
                    break;
                case InventoryPage.Suppliers:
                    _suppliersHintText = "Could not complete the request.";
                    break;
                default:
                    _receiptsHintText = "Could not complete the request.";
                    break;
            }

            RefreshHints();
            MessageBox.Show(this, exception.Message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            ApplyGridMetrics(_productsGrid);
            ApplyGridMetrics(_suppliersGrid);
            ApplyGridMetrics(_receiptsGrid);

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
                (int)Math.Round(1040 * scale),
                (int)Math.Round(480 * scale));
        }

        // ---- Row models (what the tables show) and dropdown choices ----

        private sealed record ProductRow(
            string Sku,
            string Name,
            string Color,
            string Size,
            decimal UnitPrice,
            string Supplier,
            int OnHand,
            string Status,
            InventoryProductDto Source);

        private sealed record SupplierRow(
            string Name,
            string Contact,
            string Email,
            string Phone,
            string Address,
            string Status,
            InventorySupplierDto Source);

        private sealed record ReceiptRow(
            DateTime ReceivedAt,
            string Sku,
            string Product,
            string Branch,
            string Supplier,
            int Quantity,
            string Reference,
            InventoryStockReceiptDto Source);

        private sealed record SupplierChoice(Guid? Id, string Label);
        private sealed record ProductChoice(Guid Id, string Label, Guid? SupplierId);
        private sealed record InventoryBranchChoice(Guid Id, string Name);

        /// <summary>
        /// A rounded outline around a plain input (text box, number box, drop-down). The outline turns
        /// blue while the input has focus, like the search box in the header bar.
        /// </summary>
        private sealed class FieldFrame : Panel
        {
            private readonly Control _input;

            public FieldFrame(Control input)
            {
                _input = input;
                BackColor = Palette.BackgroundSecondary.Color;
                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw,
                    true);

                input.Font = FieldFont;
                input.BackColor = BackColor;
                input.ForeColor = Palette.TextPrimary.Color;
                switch (input)
                {
                    case TextBox textBox:
                        textBox.BorderStyle = BorderStyle.None;
                        break;
                    case NumericUpDown numeric:
                        numeric.BorderStyle = BorderStyle.None;
                        break;
                    case ComboBox combo:
                        combo.FlatStyle = FlatStyle.Flat;
                        break;
                }

                input.Enter += (_, _) => Invalidate();
                input.Leave += (_, _) => Invalidate();
                Click += (_, _) => _input.Focus();
                Controls.Add(input);
            }

            protected override void OnLayout(LayoutEventArgs levent)
            {
                base.OnLayout(levent);

                var scale = DeviceDpi / 96f;
                var left = (int)Math.Round(10 * scale);
                var right = (int)Math.Round((_input is ComboBox ? 12 : _input is TextBox ? 10 : 4) * scale);
                var height = _input.Height;
                _input.SetBounds(
                    left,
                    Math.Max(0, (Height - height) / 2),
                    Math.Max(10, Width - left - right),
                    height);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(Parent?.BackColor ?? BackColor);
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var radius = (int)Math.Round(8 * DeviceDpi / 96f);
                var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
                using var path = RoundedRectangle(bounds, radius);
                using var fill = new SolidBrush(BackColor);
                g.FillPath(fill, path);

                var focused = _input.ContainsFocus;
                using var pen = new Pen(focused ? Palette.Primary.Color : BrandTints.powderBlue.Color, 1f);
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

        /// <summary>
        /// A borderless dialog in the same style as the invitation dialog in Platform Admin: blue
        /// header, title, fields, and Cancel / Save buttons. Save runs the supplied action; if it
        /// returns an error message the dialog stays open and shows it, so nothing typed is lost.
        /// </summary>
        private sealed class EditorDialog : Form
        {
            private const int DialogHeaderHeight = 40;
            private const int DialogWidth = 600;
            private const int ErrorHeight = 44;

            private readonly Func<Task<string?>> _saveAsync;
            private readonly Label _error = new();
            private readonly RoundedButton _save;
            private readonly RoundedButton _cancel;

            public EditorDialog(
                string headerText,
                string title,
                string subtitle,
                string saveText,
                Control body,
                int bodyHeight,
                Func<Task<string?>> saveAsync,
                Action? initializeInputs = null)
            {
                _saveAsync = saveAsync;

                Theme.ThreadFlowTheme.Apply(this);

                SuspendLayout();

                const int left = 24;
                var innerWidth = DialogWidth - left * 2;
                var bodyTop = 104 + DialogHeaderHeight;
                var errorTop = bodyTop + bodyHeight + 4;
                var buttonTop = errorTop + ErrorHeight + 4;

                Text = headerText;
                BackColor = Palette.BackgroundSecondary.Color;
                ClientSize = new Size(DialogWidth, buttonTop + ButtonHeight + 24);
                StartPosition = FormStartPosition.CenterParent;
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;

                var header = new Panel
                {
                    BackColor = Palette.Primary.Color,
                    Location = Point.Empty,
                    Size = new Size(DialogWidth, DialogHeaderHeight),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                var headerTitle = new Label
                {
                    Text = headerText,
                    Font = TopBarBrandFont,
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Location = new Point(18, 0),
                    Size = new Size(DialogWidth - 18 - 46, DialogHeaderHeight)
                };
                var closeHeader = new CaptionButton(CaptionGlyph.Close)
                {
                    Location = new Point(DialogWidth - 46, 0),
                    Size = new Size(46, DialogHeaderHeight),
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
                    Font = TitleFont,
                    ForeColor = Palette.TextPrimary.Color,
                    BackColor = Color.Transparent,
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Location = new Point(left, 20 + DialogHeaderHeight),
                    Size = new Size(innerWidth, 34)
                };
                var subtitleLabel = new Label
                {
                    Text = subtitle,
                    Font = SubtitleFont,
                    ForeColor = Palette.TextSecondary.Color,
                    BackColor = Color.Transparent,
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Location = new Point(left, 56 + DialogHeaderHeight),
                    Size = new Size(innerWidth, 20)
                };
                var divider = new Panel
                {
                    BackColor = BrandTints.iceBlue.Color,
                    Location = new Point(left, 88 + DialogHeaderHeight),
                    Size = new Size(innerWidth, 1)
                };

                body.Location = new Point(left, bodyTop);
                body.Size = new Size(innerWidth, bodyHeight);

                _error.Font = SubtitleFont;
                _error.ForeColor = ErrorTextColor;
                _error.BackColor = Color.Transparent;
                _error.AutoSize = false;
                _error.TextAlign = ContentAlignment.TopLeft;
                _error.Location = new Point(left, errorTop);
                _error.Size = new Size(innerWidth, ErrorHeight);

                _save = MakePrimaryButton(saveText);
                _save.Location = new Point(DialogWidth - left - 150, buttonTop);
                _save.Size = new Size(150, ButtonHeight);
                _save.Click += async (_, _) => await SubmitAsync();

                _cancel = MakeSecondaryButton("Cancel");
                _cancel.Location = new Point(DialogWidth - left - 150 - 12 - 110, buttonTop);
                _cancel.Size = new Size(110, ButtonHeight);
                _cancel.DialogResult = DialogResult.Cancel;

                Controls.AddRange([header, titleLabel, subtitleLabel, divider, body, _error, _save, _cancel]);

                AutoScaleDimensions = new SizeF(96F, 96F);
                AutoScaleMode = AutoScaleMode.Dpi;

                ResumeLayout(false);
                PerformLayout();

                AcceptButton = _save;
                CancelButton = _cancel;

                Shown += (_, _) =>
                {
                    initializeInputs?.Invoke();
                    if (FindFirstInput(body) is { } input)
                    {
                        input.Focus();
                        if (input is TextBox textBox)
                            textBox.SelectAll();
                    }
                };
            }

            private static Control? FindFirstInput(Control parent)
            {
                foreach (Control child in parent.Controls)
                {
                    if (child is TextBox or ComboBox or NumericUpDown)
                        return child;

                    if (FindFirstInput(child) is { } found)
                        return found;
                }

                return null;
            }

            private async Task SubmitAsync()
            {
                _error.Text = string.Empty;
                _save.Enabled = false;
                _cancel.Enabled = false;
                try
                {
                    var error = await _saveAsync();
                    if (error is null)
                    {
                        DialogResult = DialogResult.OK;
                        return;
                    }

                    _error.Text = error;
                }
                finally
                {
                    if (!IsDisposed)
                    {
                        _save.Enabled = true;
                        _cancel.Enabled = true;
                    }
                }
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
        /// A sidebar row with a line icon. The active page gets a full-width light-blue
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
                    case NavIcon.Products:
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

                    case NavIcon.Suppliers:
                        {
                            // Delivery truck: cargo box, cab and two wheels.
                            var u = bounds.Width / 18f;
                            var x0 = bounds.X;
                            var y0 = bounds.Y;
                            g.DrawRectangle(pen, x0 + 1f * u, y0 + 3.5f * u, 9f * u, 8f * u);
                            g.DrawLines(pen, [
                                new PointF(x0 + 10f * u, y0 + 6.5f * u),
                            new PointF(x0 + 14f * u, y0 + 6.5f * u),
                            new PointF(x0 + 17f * u, y0 + 9.5f * u),
                            new PointF(x0 + 17f * u, y0 + 11.5f * u),
                            new PointF(x0 + 10f * u, y0 + 11.5f * u)
                            ]);
                            g.DrawEllipse(pen, x0 + 3f * u, y0 + 11f * u, 4f * u, 4f * u);
                            g.DrawEllipse(pen, x0 + 11f * u, y0 + 11f * u, 4f * u, 4f * u);
                            break;
                        }

                    case NavIcon.Receipts:
                        {
                            // Inbox tray with an arrow dropping in (stock coming in).
                            var u = bounds.Width / 18f;
                            var x0 = bounds.X;
                            var y0 = bounds.Y;
                            g.DrawPolygon(pen, [
                                new PointF(x0 + 1f * u, y0 + 10f * u),
                            new PointF(x0 + 1f * u, y0 + 16f * u),
                            new PointF(x0 + 17f * u, y0 + 16f * u),
                            new PointF(x0 + 17f * u, y0 + 10f * u),
                            new PointF(x0 + 12f * u, y0 + 10f * u),
                            new PointF(x0 + 11f * u, y0 + 12.5f * u),
                            new PointF(x0 + 7f * u, y0 + 12.5f * u),
                            new PointF(x0 + 6f * u, y0 + 10f * u)
                            ]);
                            g.DrawLine(pen, x0 + 9f * u, y0 + 1.5f * u, x0 + 9f * u, y0 + 8.5f * u);
                            g.DrawLines(pen, [
                                new PointF(x0 + 6f * u, y0 + 5.5f * u),
                            new PointF(x0 + 9f * u, y0 + 8.5f * u),
                            new PointF(x0 + 12f * u, y0 + 5.5f * u)
                            ]);
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

        /// <summary>
        /// Read-only branch badge for the page header: a rounded outline holding a small grey
        /// "BRANCH" label, a thin divider and the branch name. Its right edge stays put when the
        /// name changes (it grows towards the left), so it never drifts into the Refresh button.
        /// </summary>
        private sealed class BranchBadge : Control
        {
            private static readonly Font CaptionFont = new("Segoe UI", 8.5F);
            private static readonly Font NameFont = new("Segoe UI Semibold", 11F);

            private string? _branchName;

            public BranchBadge()
            {
                TabStop = false;
                AccessibleRole = AccessibleRole.StaticText;
                AccessibleName = "Branch";

                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw,
                    true);
            }

            public string? BranchName
            {
                get => _branchName;
                set
                {
                    _branchName = value;
                    AccessibleName = string.IsNullOrWhiteSpace(value) ? "Branch" : $"Branch: {value}";
                    FitToText();
                    Invalidate();
                }
            }

            private string DisplayName => string.IsNullOrWhiteSpace(_branchName) ? "No branch available" : _branchName;

            private const TextFormatFlags MeasureFlags =
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;

            private void FitToText()
            {
                var scale = DeviceDpi / 96f;
                var width = Px(18, scale) * 2 + Px(12, scale) * 2 + 1
                    + TextRenderer.MeasureText("BRANCH", CaptionFont, new Size(int.MaxValue, int.MaxValue), MeasureFlags).Width
                    + TextRenderer.MeasureText(DisplayName, NameFont, new Size(int.MaxValue, int.MaxValue), MeasureFlags).Width;

                // Keep the right edge where it is; the badge grows or shrinks towards the left.
                SetBounds(Right - width, Top, width, Height);
            }

            private static int Px(float designUnits, float scale) => (int)Math.Round(designUnits * scale);

            protected override void OnDpiChangedAfterParent(EventArgs e)
            {
                base.OnDpiChangedAfterParent(e);
                FitToText();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(Parent?.BackColor ?? BackColor);
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var scale = DeviceDpi / 96f;
                var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var path = RoundedRectangle(bounds, bounds.Height / 2))
                using (var fill = new SolidBrush(Palette.BackgroundSecondary.Color))
                using (var border = new Pen(BrandTints.powderBlue.Color, 1f))
                {
                    g.FillPath(fill, path);
                    g.DrawPath(border, path);
                }

                var padding = Px(18, scale);
                var gap = Px(12, scale);
                var captionWidth = TextRenderer.MeasureText(
                    "BRANCH", CaptionFont, new Size(int.MaxValue, int.MaxValue), MeasureFlags).Width;

                const TextFormatFlags flags =
                    TextFormatFlags.Left |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.SingleLine |
                    TextFormatFlags.NoPrefix |
                    TextFormatFlags.NoPadding;

                var captionBounds = new Rectangle(padding, 0, captionWidth, Height);
                TextRenderer.DrawText(g, "BRANCH", CaptionFont, captionBounds, Palette.TextSecondary.Color, flags);

                var dividerX = captionBounds.Right + gap;
                var dividerInset = Height / 4;
                using (var divider = new Pen(BrandTints.powderBlue.Color, 1f))
                {
                    g.DrawLine(divider, dividerX, dividerInset, dividerX, Height - dividerInset - 1);
                }

                var nameBounds = new Rectangle(
                    dividerX + 1 + gap, 0, Math.Max(1, Width - (dividerX + 1 + gap) - padding), Height);
                TextRenderer.DrawText(
                    g, DisplayName, NameFont, nameBounds, Palette.TextPrimary.Color, flags | TextFormatFlags.EndEllipsis);
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
}

namespace ThreadFlow.WinForms.ApiClient
{
    public sealed class InventoryCatalogApiClient
    {
        private static readonly HttpClient Http = new() { BaseAddress = new Uri("http://localhost:5086/") };

        public async Task<List<InventoryBranchDto>> GetBranchesAsync(CancellationToken cancellationToken = default)
        {
            ApiSession.ApplyAuthorization(Http);
            return await Http.GetFromJsonAsync<List<InventoryBranchDto>>(
                "api/inventory/branches", cancellationToken) ?? [];
        }

        public async Task<List<InventoryProductDto>> GetProductsAsync(Guid branchId, CancellationToken cancellationToken = default)
        {
            ApiSession.ApplyAuthorization(Http);
            return await Http.GetFromJsonAsync<List<InventoryProductDto>>(
                $"api/inventory/products?branchId={branchId}", cancellationToken) ?? [];
        }

        public async Task<List<InventoryStockReceiptDto>> GetReceiptsAsync(Guid branchId, CancellationToken cancellationToken = default)
        {
            ApiSession.ApplyAuthorization(Http);
            return await Http.GetFromJsonAsync<List<InventoryStockReceiptDto>>(
                $"api/inventory/receipts?branchId={branchId}", cancellationToken) ?? [];
        }

        public Task<InventoryStockReceiptDto> ReceiveStockAsync(
            ReceiveInventoryStockRequest request, CancellationToken cancellationToken = default) =>
            SendAsync<InventoryStockReceiptDto>(
                () => Http.PostAsJsonAsync("api/inventory/receipts", request, cancellationToken),
                cancellationToken);

        public async Task<List<InventorySupplierDto>> GetSuppliersAsync(CancellationToken cancellationToken = default)
        {
            ApiSession.ApplyAuthorization(Http);
            return await Http.GetFromJsonAsync<List<InventorySupplierDto>>(
                "api/inventory/suppliers", cancellationToken) ?? [];
        }

        public Task<InventoryProductDto> SaveProductAsync(
            Guid? id, SaveInventoryProductRequest request, CancellationToken cancellationToken = default) =>
            SendAsync<InventoryProductDto>(
                () => id is Guid productId
                    ? Http.PutAsJsonAsync($"api/inventory/products/{productId}", request, cancellationToken)
                    : Http.PostAsJsonAsync("api/inventory/products", request, cancellationToken),
                cancellationToken);

        public Task<InventorySupplierDto> SaveSupplierAsync(
            Guid? id, SaveInventorySupplierRequest request, CancellationToken cancellationToken = default) =>
            SendAsync<InventorySupplierDto>(
                () => id is Guid supplierId
                    ? Http.PutAsJsonAsync($"api/inventory/suppliers/{supplierId}", request, cancellationToken)
                    : Http.PostAsJsonAsync("api/inventory/suppliers", request, cancellationToken),
                cancellationToken);

        public Task SetProductStatusAsync(Guid id, bool isActive, CancellationToken cancellationToken = default) =>
            SendNoContentAsync(() => Http.PatchAsJsonAsync(
                $"api/inventory/products/{id}/status", new SetCatalogStatusRequest(isActive), cancellationToken),
                cancellationToken);

        public Task SetSupplierStatusAsync(Guid id, bool isActive, CancellationToken cancellationToken = default) =>
            SendNoContentAsync(() => Http.PatchAsJsonAsync(
                $"api/inventory/suppliers/{id}/status", new SetCatalogStatusRequest(isActive), cancellationToken),
                cancellationToken);

        private static async Task<T> SendAsync<T>(Func<Task<HttpResponseMessage>> send, CancellationToken cancellationToken)
        {
            ApiSession.ApplyAuthorization(Http);
            using var response = await send();
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"The API returned {(int)response.StatusCode}: {body}", null, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("The API returned an empty response.");
        }

        private static async Task SendNoContentAsync(Func<Task<HttpResponseMessage>> send, CancellationToken cancellationToken)
        {
            ApiSession.ApplyAuthorization(Http);
            using var response = await send();
            if (response.IsSuccessStatusCode)
                return;
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"The API returned {(int)response.StatusCode}: {body}", null, response.StatusCode);
        }
    }

    public sealed record InventoryProductDto(
        Guid Id, string Sku, string Name, string Color, string Size,
        decimal UnitPrice, Guid? SupplierId, string? SupplierName, int QuantityOnHand, bool IsActive);

    public sealed record InventoryBranchDto(Guid Id, string Code, string Name);

    public sealed record ReceiveInventoryStockRequest(
        Guid BranchId, Guid ProductId, int Quantity, Guid? SupplierId, string? Reference);

    public sealed record InventoryStockReceiptDto(
        Guid Id, string Sku, string ProductName, string BranchName,
        string? SupplierName, int Quantity, string Reference, DateTime ReceivedAtUtc);

    public sealed record SaveInventoryProductRequest(
        string Sku, string Name, string? Color, string? Size, decimal UnitPrice, Guid? SupplierId);

    public sealed record InventorySupplierDto(
        Guid Id, string Name, string ContactName, string Email, string Phone, string Address, bool IsActive);

    public sealed record SaveInventorySupplierRequest(
        string Name, string? ContactName, string? Email, string? Phone, string? Address);

    public sealed record SetCatalogStatusRequest(bool IsActive);
}