using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using ThreadFlow.WinForms.ApiClient;
using ThreadFlow.WinForms.Theme.Colors;

namespace ThreadFlow.WinForms;

/// <summary>
/// Sales clerk workspace. It uses the same window shell as the Tenant Admin workspace
/// (blue top bar with search and account card, sidebar navigation, borderless window),
/// with the point-of-sale screen as its first page.
/// </summary>
public sealed class SalesForm : Form
{
    // All sizes below are written for 96 DPI (100% scaling).
    // AutoScaleMode.Dpi scales them up on higher-DPI screens, like the Tenant Admin screen.
    private const int FormWidth = 1120;
    private const int FormHeight = 680;
    private const int SidebarWidth = 232;
    private const int SideMargin = 32;
    private const int HeaderHeight = 104;
    private const int FooterHeight = 44;
    // The sale body scrolls instead of squashing the payments list once it gets shorter than this.
    private const int MinBodyWidth = 680;
    private const int MinBodyHeight = 440;
    private const int ButtonHeight = 42;
    private const int ContentLeft = SidebarWidth + 1 + SideMargin;
    private const int TopBarHeight = 56;
    private const int NavItemHeight = 40;
    private const int NavInset = 16;
    private const int AvatarSize = 40;
    private const int AvatarGap = 8;

    // The role shown in the header pill and in the account card. This screen is the Sales Clerk workspace.
    private const string AccountRoleLabel = "SALES CLERK";

    private static readonly Pallets Palette = new();
    private static readonly Shades BrandShades = new();
    private static readonly Tints BrandTints = new();
    private static readonly Color NavIconColor = Color.FromArgb(75, 85, 99);

    // Same Docker Desktop-style sidebar colors as the Tenant Admin screen.
    private static readonly Color SidebarBackColor = Color.FromArgb(246, 247, 250);
    private static readonly Color SidebarActiveColor = Color.FromArgb(209, 221, 248);
    private static readonly Color SidebarHoverColor = Color.FromArgb(232, 237, 247);
    private static readonly Color SidebarDividerColor = Color.FromArgb(218, 222, 230);
    private static readonly Color SearchFillColor = Lighten(Palette.Primary.Color, 0.16f);
    private static readonly Color AvatarFillColor = Color.FromArgb(30, 64, 175);
    private static readonly Color ProfileRolePillColor = Color.FromArgb(107, 114, 128);

    // Point-of-sale surfaces: white cards with a soft border and white fields inside them.
    private static readonly Color CardFillColor = Color.White;
    private static readonly Color CardBorderColor = Color.FromArgb(225, 231, 243);
    private static readonly Color RowHoverColor = Color.White;
    private static readonly Color RowSelectedColor = Color.White;
    private static readonly Color RowDividerColor = Color.FromArgb(234, 238, 246);
    private static readonly Color MutedIconColor = Color.FromArgb(156, 170, 196);
    private static readonly Color OkColor = Color.FromArgb(21, 128, 61);
    private static readonly Color WarnColor = Color.FromArgb(180, 83, 9);
    private static readonly Color DangerColor = Color.FromArgb(185, 28, 28);

    private static readonly Font TitleFont = new("Segoe UI", 18F, FontStyle.Bold);
    private static readonly Font SubtitleFont = new("Segoe UI", 9.5F);
    private static readonly Font SectionFont = new("Segoe UI Semibold", 11F);
    private static readonly Font LabelFont = new("Segoe UI Semibold", 9F);
    private static readonly Font FieldFont = new("Segoe UI", 10F);
    private static readonly Font ValueFont = new("Segoe UI Semibold", 10F);
    private static readonly Font PrimaryButtonFont = new("Segoe UI", 10F, FontStyle.Bold);
    private static readonly Font SecondaryButtonFont = new("Segoe UI", 10F);
    private static readonly Font LinkFont = new("Segoe UI", 9F, FontStyle.Underline);
    private static readonly Font TopBarBrandFont = new("Segoe UI Semibold", 13F);
    private static readonly Font PillFont = new("Segoe UI Semibold", 7.5F);
    private static readonly Font NavFont = new("Segoe UI Semibold", 9.5F);
    private static readonly Font SearchFont = new("Segoe UI", 10F);
    private static readonly Font AvatarFont = new("Segoe UI Semibold", 11F);
    private static readonly Font ProfileInitialFont = new("Segoe UI Semibold", 24F);
    private static readonly Font ProfileRoleFont = new("Segoe UI Semibold", 8F);
    private static readonly Font ProfileEmailFont = new("Segoe UI", 11F);
    private static readonly Font CaptionFont = new("Segoe UI Semibold", 8F);
    private static readonly Font TotalFont = new("Segoe UI Semibold", 20F);
    private static readonly Font RemainingFont = new("Segoe UI Semibold", 12F);
    private static readonly Font RowTitleFont = new("Segoe UI Semibold", 10F);
    private static readonly Font RowSubFont = new("Segoe UI", 8.5F);
    private static readonly Font QuantityFont = new("Segoe UI Semibold", 11F);
    private static readonly Font StepButtonFont = new("Segoe UI", 13F);
    private static readonly Font CompleteButtonFont = new("Segoe UI", 11F, FontStyle.Bold);

    private readonly SalesApiClient _apiClient = new();

    // Sale page controls.
    private readonly ComboBox _branchPicker = new SelectBox();
    private readonly TableLayoutPanel _branchPickerRow = new BufferedTable();
    private readonly BranchChip _branchChip = new();
    private readonly SelectBox _productPicker = new();
    private readonly NumericUpDown _quantityPicker = new();
    private readonly RoundedButton _qtyMinusButton = MakeSecondaryButton("\u2212");
    private readonly RoundedButton _qtyPlusButton = MakeSecondaryButton("+");
    private readonly RoundedButton _addButton = MakeSecondaryButton("Add to sale");
    private readonly Label _stockHintLabel = new();
    private readonly Label _lineTotalLabel = new();
    private readonly RowListBox _cartList = new();
    private readonly Label _cartCountLabel = new();
    private readonly LinkButton _clearSaleButton = new("Clear all");
    private readonly EmptyState _cartEmpty = new(EmptyIcon.Cart, "No items yet", "Pick a product above and add it to the sale.");
    private readonly SegmentedPicker _paymentMethodPicker = new();
    private readonly NumericUpDown _paymentAmountPicker = new();
    private readonly RoundedButton _addPaymentButton = MakeSecondaryButton("Add payment");
    private readonly RowListBox _paymentsList = new();
    private readonly EmptyState _paymentsEmpty = new(EmptyIcon.Card, "No payments yet", "Add a payment to cover the total.");
    private readonly Label _totalLabel = new();
    private readonly Label _paidLabel = new();
    private readonly Label _remainingCaption = new();
    private readonly Label _remainingLabel = new();
    private readonly RoundedButton _completeSaleButton = MakePrimaryButton("Complete Sale");
    private readonly StatusLabel _statusLabel = new();

    // Window shell (same pieces as the Tenant Admin screen).
    private readonly Panel _topBar = new ResizeEdgePanel();
    private readonly Panel _content = new ResizeEdgePanel();
    private readonly Panel _sidebar = new ResizeEdgePanel();
    private readonly Panel _sidebarEdge = new();
    private readonly SidebarNavButton _navSales = new("Sales", NavIcon.Sales);
    private readonly SidebarNavButton _navLogout = new("Log out", NavIcon.LogOut);
    private readonly HeaderSearchBox _search = new("Search products");
    private readonly CaptionButton _captionMinimize = new(CaptionGlyph.Minimize);
    private readonly CaptionButton _captionMaximize = new(CaptionGlyph.Maximize);
    private readonly CaptionButton _captionClose = new(CaptionGlyph.Close);
    private readonly AvatarButton _avatar = new();
    private readonly Label _title = new();
    private readonly Label _subtitle = new();

    // Each page is a group of controls that are shown or hidden together.
    private readonly Panel _salesBody = new();
    private Control[] _salesPageControls = [];

    // The header search looks products up on the server, so it waits for a short pause in typing.
    private readonly System.Windows.Forms.Timer _searchDebounce = new() { Interval = 350 };
    private int _loadRequestId;

    private readonly List<CartLine> _cart = [];
    private readonly List<PaymentEntry> _payments = [];
    private bool _isLoadingBranches;
    private Guid? _loadedBranchId;

    // The account card that drops down from the avatar in the header.
    private string _accountEmail = string.Empty;
    private ProfilePopup? _profilePopup;
    private long _profileClosedTick;
    private bool _ignoreAvatarClick;

    private enum NavIcon
    {
        Sales,
        LogOut
    }

    public SalesForm()
    {
        var user = ApiSession.Current?.User
            ?? throw new InvalidOperationException("Sign in before opening the workspace.");

        // Apply the shared palette first, then style our own controls the same way the
        // Tenant Admin, platform admin, welcome and sign-in screens do.
        Theme.ThreadFlowTheme.Apply(this);

        SuspendLayout();

        _accountEmail = user.Email;

        BuildForm();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Text = $"ThreadFlow | Sales | {user.Email}";
        BackColor = Palette.BackgroundSecondary.Color;

        // Borderless window with our own blue header bar (like the Tenant Admin screen).
        FormBorderStyle = FormBorderStyle.None;
        ClientSize = new Size(FormWidth, FormHeight + TopBarHeight);
        StartPosition = FormStartPosition.CenterScreen;
        UpdateMaximizedBounds(Screen.PrimaryScreen ?? Screen.AllScreens[0]);
        WindowState = FormWindowState.Maximized;
        DoubleBuffered = true;

        ResumeLayout(false);
        PerformLayout();

        Load += SalesForm_Load;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _searchDebounce.Dispose();

        base.Dispose(disposing);
    }

    private void BuildForm()
    {
        _content.SuspendLayout();
        _content.Size = new Size(FormWidth, FormHeight);
        _content.BackColor = Palette.BackgroundSecondary.Color;

        BuildSidebar();

        _title.Text = "Sales";
        _title.Font = TitleFont;
        _title.ForeColor = Palette.TextPrimary.Color;
        _title.BackColor = Color.Transparent;
        _title.AutoSize = false;
        _title.TextAlign = ContentAlignment.MiddleLeft;
        _title.Location = new Point(ContentLeft, 24);
        _title.Size = new Size(520, 34);

        _subtitle.Text = "Create a sale for the selected branch.";
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

        BuildSalesPage(contentWidth);

        _content.Controls.AddRange([
            _sidebar, _sidebarEdge, _title, _subtitle, headerDivider,
            .. _salesPageControls
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

        var pill = new RolePill(AccountRoleLabel)
        {
            Location = new Point(164, (TopBarHeight - 22) / 2),
            Size = new Size(104, 22)
        };

        _search.Size = new Size(400, 36);
        _search.SearchChanged += (_, _) =>
        {
            // The search box looks up products for the selected branch. Typing restarts the
            // timer, so the server is only asked once the person pauses.
            _searchDebounce.Stop();
            _searchDebounce.Start();
        };
        _searchDebounce.Tick += async (_, _) =>
        {
            _searchDebounce.Stop();
            await LoadProductsForSelectedBranchAsync();
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

        _navSales.Location = new Point(0, 16);
        _navSales.Size = new Size(SidebarWidth, NavItemHeight);
        _navSales.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _navSales.Active = true;
        _navSales.Click += (_, _) => ShowSalesView();

        // Adding a navigation item later: add a NavIcon value and its drawing in DrawIcon, create a
        // SidebarNavButton below this one (next Location is 16 + (NavItemHeight + 4) * index),
        // build the new page's controls into their own array like _salesPageControls, and switch
        // between pages in a ShowXxxView method, like ShowSalesView.

        // A divider and Log out pinned to the bottom, like the Tenant Admin screen.
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

        _sidebar.Controls.AddRange([_navSales, navDivider, _navLogout]);

        _sidebarEdge.Location = new Point(SidebarWidth, 0);
        _sidebarEdge.Size = new Size(1, FormHeight);
        _sidebarEdge.BackColor = SidebarBackColor; // no visible edge, like the Tenant Admin screen
        _sidebarEdge.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
    }

    private void BuildSalesPage(int contentWidth)
    {
        // The page body sits in a scroll panel (like the Tenant Admin settings page). It keeps its
        // layout down to AutoScrollMinSize and scrolls on shorter windows instead of overlapping.
        var bodyHeight = FormHeight - HeaderHeight - FooterHeight;

        _salesBody.Location = new Point(ContentLeft, HeaderHeight);
        // A few spare pixels on the right: at display scaling above 100% each control's position and
        // width are rounded separately, and the last pixel of a right-aligned control (its border)
        // could otherwise fall outside the panel and be clipped. Controls are still laid out to contentWidth.
        const int edgeSlack = 4;
        _salesBody.Size = new Size(contentWidth + edgeSlack, bodyHeight);
        _salesBody.BackColor = Palette.BackgroundSecondary.Color;
        _salesBody.AutoScroll = true;
        // AutoScrollMinSize is set in OnLoad, once the window has been scaled to the screen's DPI.
        _salesBody.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        // Two columns: the sale being built (left) and checkout (right). The tables size
        // themselves, so the layout follows the window instead of hand-placed coordinates.
        var root = MakeTable(_salesBody.BackColor, [0, 16, 360], [0]);
        root.Padding = new Padding(0, 0, edgeSlack, 4);
        root.Controls.Add(BuildOrderColumn(), 0, 0);
        root.Controls.Add(BuildCheckoutCard(), 2, 0);
        _salesBody.Controls.Add(root);

        // ---- Footer: just a quiet status line. The main action now lives in the checkout card. ----

        var footerDivider = new Panel
        {
            BackColor = BrandTints.iceBlue.Color,
            Location = new Point(ContentLeft, FormHeight - FooterHeight + 4),
            Size = new Size(contentWidth, 1),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        StyleHint(
            _statusLabel,
            new Point(ContentLeft + 4, FormHeight - FooterHeight + 10),
            new Size(contentWidth - 4, 28));
        _statusLabel.AutoEllipsis = true;

        _salesPageControls = [_salesBody, footerDivider, _statusLabel];

        _cartCountLabel.Text = "No items yet";
        UpdateAddButtonState();
        UpdatePaymentSummary();
    }

    // Left column: branch, the "add an item" card and the sale items.
    private Control BuildOrderColumn()
    {
        var column = MakeTable(_salesBody.BackColor, [0], [ButtonHeight, 12, 168, 16, 0]);

        // Branch: a drop-down when the clerk can sell for several branches, otherwise a small chip.
        var branchRow = new Panel { BackColor = _salesBody.BackColor };

        ConfigureTable(_branchPickerRow, _salesBody.BackColor, [58, 0], [0]);
        _branchPickerRow.Dock = DockStyle.Left;
        _branchPickerRow.Width = 340;

        _branchPicker.SelectedIndexChanged += BranchPicker_SelectedIndexChanged;
        Place(_branchPickerRow, NewLabel("Branch", LabelFont, Palette.TextPrimary.Color, ContentAlignment.MiddleLeft), 0, 0);
        Place(_branchPickerRow, new FieldFrame(_branchPicker), 1, 0);

        _branchChip.Dock = DockStyle.Fill;
        _branchChip.Visible = false;

        // Docking order: the Fill control is added first, the Left control after it.
        branchRow.Controls.Add(_branchChip);
        branchRow.Controls.Add(_branchPickerRow);

        Place(column, branchRow, 0, 0);
        Place(column, BuildAddCard(), 0, 2);
        Place(column, BuildCartCard(), 0, 4);
        return column;
    }

    private Control BuildAddCard()
    {
        var card = new CardPanel { Padding = new Padding(16, 12, 16, 12) };
        var table = MakeTable(CardFillColor, [148, 0], [20, ButtonHeight, 10, ButtonHeight, 28]);

        // ---- Product: each row of the list shows name, SKU, stock and price in columns ----

        _productPicker.FormattingEnabled = true;
        _productPicker.DisplayMember = nameof(SalesProductDto.Name);
        _productPicker.ItemPainter = PaintProductRow;
        _productPicker.RowHeight = 30;
        _productPicker.PlaceholderText = "Choose a product";
        _productPicker.SelectedIndexChanged += (_, _) => UpdateAddButtonState();
        _productPicker.Format += (_, e) =>
        {
            if (e.ListItem is SalesProductDto product)
            {
                e.Value =
                    $"{product.Sku} - {product.Name} ({product.Color}, {product.Size}) " +
                    $"— {product.QuantityOnHand} available — ({product.UnitPrice:C2})";
            }
        };
        _productPicker.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter && !_productPicker.DroppedDown)
                AddFromKeyboard(e);
        };

        Place(table, NewLabel("Product", LabelFont, Palette.TextPrimary.Color, ContentAlignment.BottomLeft), 0, 0, 2, new Padding(0, 0, 0, 3));
        Place(table, new FieldFrame(_productPicker), 0, 1, 2);

        // ---- Quantity: [-] 1 [+] and the Add button ----

        _quantityPicker.Minimum = 1;
        _quantityPicker.Maximum = 1000;
        _quantityPicker.Value = 1;
        _quantityPicker.TextAlign = HorizontalAlignment.Center;
        _quantityPicker.ValueChanged += (_, _) => UpdateAddButtonState();
        _quantityPicker.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
                AddFromKeyboard(e);
        };

        var quantityFrame = new FieldFrame(_quantityPicker);
        _quantityPicker.Font = QuantityFont;
        HideSpinner(_quantityPicker);

        _qtyMinusButton.Font = StepButtonFont;
        _qtyMinusButton.Click += (_, _) =>
            _quantityPicker.Value = Math.Max(_quantityPicker.Minimum, _quantityPicker.Value - 1);
        _qtyPlusButton.Font = StepButtonFont;
        _qtyPlusButton.Click += (_, _) =>
            _quantityPicker.Value = Math.Min(_quantityPicker.Maximum, _quantityPicker.Value + 1);

        var quantityGroup = MakeTable(CardFillColor, [36, 0, 36], [0]);
        Place(quantityGroup, _qtyMinusButton, 0, 0, 1, new Padding(0, 0, 4, 0));
        Place(quantityGroup, quantityFrame, 1, 0);
        Place(quantityGroup, _qtyPlusButton, 2, 0, 1, new Padding(4, 0, 0, 0));
        Place(table, quantityGroup, 0, 3);

        _addButton.Enabled = false;
        _addButton.Click += AddButton_Click;
        Place(table, _addButton, 1, 3, 1, new Padding(10, 0, 0, 0));

        // ---- Stock hint on the left, line total on the right ----

        ConfigureLabel(_stockHintLabel, SubtitleFont, Palette.TextSecondary.Color, ContentAlignment.MiddleLeft);
        ConfigureLabel(_lineTotalLabel, ValueFont, Palette.TextPrimary.Color, ContentAlignment.MiddleRight);

        var hintRow = MakeTable(CardFillColor, [0, 150], [0]);
        Place(hintRow, _stockHintLabel, 0, 0);
        Place(hintRow, _lineTotalLabel, 1, 0);
        Place(table, hintRow, 0, 4, 2);

        card.Controls.Add(table);
        return card;
    }

    private Control BuildCartCard()
    {
        var card = new CardPanel { Padding = new Padding(16, 12, 16, 16) };
        var table = MakeTable(CardFillColor, [0], [32, 0]);

        var header = MakeTable(CardFillColor, [100, 0, 90], [0]);
        Place(header, NewLabel("Sale items", SectionFont, Palette.TextPrimary.Color, ContentAlignment.MiddleLeft), 0, 0);
        ConfigureLabel(_cartCountLabel, SubtitleFont, Palette.TextSecondary.Color, ContentAlignment.MiddleLeft);
        Place(header, _cartCountLabel, 1, 0);
        _clearSaleButton.Enabled = false;
        _clearSaleButton.Click += ClearSaleButton_Click;
        Place(header, _clearSaleButton, 2, 0);
        Place(table, header, 0, 0);

        _cartList.RowHeight = 64;
        _cartList.ShowStepper = true;
        _cartList.Content = item => item is CartLine line
            ? new RowContent(
                line.Product.Name,
                $"{line.Product.Sku}  \u00b7  {line.Product.Color}, {line.Product.Size}  \u00b7  {line.Product.UnitPrice:C2} each",
                (line.Product.UnitPrice * line.Quantity).ToString("C2"),
                line.Quantity)
            : default;
        _cartList.RowAction += CartList_RowAction;

        var cartFrame = new FieldFrame(_cartList) { Overlay = _cartEmpty };
        Place(table, cartFrame, 0, 1, 1, new Padding(0, 6, 0, 0));

        card.Controls.Add(table);
        return card;
    }

    // Right column: the big total, how it is being paid, and the one main action.
    private Control BuildCheckoutCard()
    {
        var card = new CardPanel { Padding = new Padding(18, 12, 18, 14) };

        // Rows: title, total, divider, method label, method, gap, amount label, amount row, gap,
        // payments label, payments list (takes the leftover space), paid/remaining, button.
        var table = MakeTable(
            CardFillColor,
            [0],
            [24, 52, 11, 20, 36, 6, 20, 36, 6, 20, 0, 52, 44]);

        Place(table, NewLabel("Payment", SectionFont, Palette.TextPrimary.Color, ContentAlignment.MiddleLeft), 0, 0);

        // ---- Total ----

        ConfigureLabel(_totalLabel, TotalFont, Palette.TextPrimary.Color, ContentAlignment.MiddleLeft);
        _totalLabel.Dock = DockStyle.Fill;

        var totalCaption = NewLabel("TOTAL DUE", CaptionFont, Palette.TextSecondary.Color, ContentAlignment.BottomLeft);
        totalCaption.Dock = DockStyle.Top;
        totalCaption.Height = 16;

        var totalBlock = new Panel { BackColor = CardFillColor };
        totalBlock.Controls.Add(_totalLabel);   // Fill first, Top after it
        totalBlock.Controls.Add(totalCaption);
        Place(table, totalBlock, 0, 1);

        var divider = new Panel
        {
            BackColor = CardBorderColor,
            Height = 1,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = Padding.Empty
        };
        table.Controls.Add(divider, 0, 2);

        // ---- Method: one tap instead of a drop-down ----

        Place(table, NewLabel("Method", LabelFont, Palette.TextPrimary.Color, ContentAlignment.BottomLeft), 0, 3, 1, new Padding(0, 0, 0, 3));

        _paymentMethodPicker.SetItems("Cash", "Card", "EWallet");
        _paymentMethodPicker.DisplayText = DisplayPaymentMethod;
        _paymentMethodPicker.SelectedIndex = 0;
        Place(table, _paymentMethodPicker, 0, 4);

        // ---- Amount + Add payment ----

        Place(table, NewLabel("Amount", LabelFont, Palette.TextPrimary.Color, ContentAlignment.BottomLeft), 0, 6, 1, new Padding(0, 0, 0, 3));

        _paymentAmountPicker.DecimalPlaces = 2;
        _paymentAmountPicker.Minimum = 0.01m;
        _paymentAmountPicker.Maximum = 999999999m;
        _paymentAmountPicker.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter)
                return;

            e.SuppressKeyPress = true;
            e.Handled = true;
            if (_addPaymentButton.Enabled)
                AddPaymentButton_Click(this, EventArgs.Empty);
        };

        var amountFrame = new FieldFrame(_paymentAmountPicker);
        _paymentAmountPicker.Font = QuantityFont;
        HideSpinner(_paymentAmountPicker);

        _addPaymentButton.Enabled = false;
        _addPaymentButton.Click += AddPaymentButton_Click;

        var amountRow = MakeTable(CardFillColor, [0, 136], [0]);
        Place(amountRow, amountFrame, 0, 0, 1, new Padding(0, 0, 10, 0));
        Place(amountRow, _addPaymentButton, 1, 0);
        Place(table, amountRow, 0, 7);

        // ---- Payments taken so far ----

        Place(table, NewLabel("Payments", LabelFont, Palette.TextPrimary.Color, ContentAlignment.BottomLeft), 0, 9, 1, new Padding(0, 0, 0, 3));

        _paymentsList.RowHeight = 40;
        _paymentsList.Content = item => item is PaymentEntry payment
            ? new RowContent(DisplayPaymentMethod(payment.Method), null, payment.Amount.ToString("C2"), 0)
            : default;
        _paymentsList.RowAction += PaymentsList_RowAction;

        Place(table, new FieldFrame(_paymentsList) { Overlay = _paymentsEmpty }, 0, 10);

        // ---- Paid / Remaining ----

        var summary = MakeTable(CardFillColor, [0, 150], [24, 28]);
        Place(summary, NewLabel("Paid", SubtitleFont, Palette.TextSecondary.Color, ContentAlignment.MiddleLeft), 0, 0);
        ConfigureLabel(_paidLabel, ValueFont, Palette.TextPrimary.Color, ContentAlignment.MiddleRight);
        Place(summary, _paidLabel, 1, 0);
        ConfigureLabel(_remainingCaption, SectionFont, Palette.TextPrimary.Color, ContentAlignment.MiddleLeft);
        Place(summary, _remainingCaption, 0, 1);
        ConfigureLabel(_remainingLabel, RemainingFont, Palette.TextPrimary.Color, ContentAlignment.MiddleRight);
        Place(summary, _remainingLabel, 1, 1);
        Place(table, summary, 0, 11);

        // ---- The main action ----

        _completeSaleButton.Font = CompleteButtonFont;
        _completeSaleButton.Enabled = false;
        _completeSaleButton.Click += CompleteSaleButton_Click;
        Place(table, _completeSaleButton, 0, 12);

        card.Controls.Add(table);
        return card;
    }

    // ---- Layout helpers ----

    // A grid sized in design units. A size of 0 means "take the space that is left".
    private static TableLayoutPanel MakeTable(Color back, float[] columns, float[] rows)
    {
        var table = new BufferedTable();
        ConfigureTable(table, back, columns, rows);
        return table;
    }

    private static void ConfigureTable(TableLayoutPanel table, Color back, float[] columns, float[] rows)
    {
        table.SuspendLayout();
        table.BackColor = back;
        table.Dock = DockStyle.Fill;
        table.Margin = Padding.Empty;
        table.Padding = Padding.Empty;
        table.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
        table.ColumnCount = columns.Length;
        table.RowCount = rows.Length;

        table.ColumnStyles.Clear();
        foreach (var width in columns)
        {
            table.ColumnStyles.Add(width > 0
                ? new ColumnStyle(SizeType.Absolute, width)
                : new ColumnStyle(SizeType.Percent, 100));
        }

        table.RowStyles.Clear();
        foreach (var height in rows)
        {
            table.RowStyles.Add(height > 0
                ? new RowStyle(SizeType.Absolute, height)
                : new RowStyle(SizeType.Percent, 100));
        }

        table.ResumeLayout(false);
    }

    private static void Place(
        TableLayoutPanel table,
        Control control,
        int column,
        int row,
        int columnSpan = 1,
        Padding? margin = null)
    {
        control.Dock = DockStyle.Fill;
        control.Margin = margin ?? Padding.Empty;
        table.Controls.Add(control, column, row);

        if (columnSpan > 1)
            table.SetColumnSpan(control, columnSpan);
    }

    private static Label NewLabel(string text, Font font, Color color, ContentAlignment alignment)
    {
        var label = new Label { Text = text };
        ConfigureLabel(label, font, color, alignment);
        return label;
    }

    private static void ConfigureLabel(Label label, Font font, Color color, ContentAlignment alignment)
    {
        label.Font = font;
        label.ForeColor = color;
        label.BackColor = Color.Transparent;
        label.AutoSize = false;
        label.AutoEllipsis = true;
        label.TextAlign = alignment;
        label.Margin = Padding.Empty;
    }

    // The number boxes draw their own - and + steppers, so the old up/down arrows are hidden.
    private static void HideSpinner(NumericUpDown number)
    {
        if (number.Controls.Count > 0)
            number.Controls[0].Hide();
    }

    private static string DisplayPaymentMethod(string method) =>
        method == "EWallet" ? "E-Wallet" : method;

    // One row of the product list: name and SKU on the left, stock and price in their own columns.
    private static void PaintProductRow(Graphics g, Rectangle bounds, object item, bool highlighted, float scale)
    {
        if (item is not SalesProductDto product)
            return;

        int S(float value) => (int)Math.Round(value * scale);

        const TextFormatFlags measureFlags =
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
        const TextFormatFlags leftFlags =
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis;
        const TextFormatFlags rightFlags =
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;

        var priceRect = new Rectangle(bounds.Right - S(10) - S(84), bounds.Top, S(84), bounds.Height);
        var stockRect = new Rectangle(priceRect.Left - S(8) - S(88), bounds.Top, S(88), bounds.Height);
        var textRect = new Rectangle(
            bounds.Left + S(10),
            bounds.Top,
            Math.Max(S(20), stockRect.Left - S(12) - (bounds.Left + S(10))),
            bounds.Height);

        TextRenderer.DrawText(g, product.UnitPrice.ToString("C2"), ValueFont, priceRect, Palette.TextPrimary.Color, rightFlags);

        var stockColor = product.QuantityOnHand <= 0 ? DangerColor
            : product.QuantityOnHand <= 3 ? WarnColor
            : Palette.TextSecondary.Color;
        var stockText = product.QuantityOnHand <= 0 ? "Out of stock" : $"{product.QuantityOnHand} in stock";
        TextRenderer.DrawText(g, stockText, RowSubFont, stockRect, stockColor, rightFlags);

        var nameWidth = TextRenderer.MeasureText(
            g, product.Name, RowTitleFont, new Size(int.MaxValue, int.MaxValue), measureFlags).Width;
        var nameRect = new Rectangle(textRect.X, textRect.Y, Math.Min(nameWidth, textRect.Width), textRect.Height);
        TextRenderer.DrawText(g, product.Name, RowTitleFont, nameRect, Palette.TextPrimary.Color, leftFlags);

        var detailLeft = nameRect.Right + S(8);
        if (detailLeft < textRect.Right - S(40))
        {
            TextRenderer.DrawText(
                g,
                $"{product.Sku}  \u00b7  {product.Color}, {product.Size}",
                RowSubFont,
                new Rectangle(detailLeft, textRect.Y, textRect.Right - detailLeft, textRect.Height),
                Palette.TextSecondary.Color,
                leftFlags);
        }
    }

    // The quiet message line at the bottom left of a page, like the hint on the Tenant Admin screen.
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
        // Ctrl+K jumps to the search box.
        if (keyData == (Keys.Control | Keys.K) && _search.Visible)
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

    private static void SetPageVisible(Control[] controls, bool visible)
    {
        foreach (var control in controls)
            control.Visible = visible;
    }

    private void ShowSalesView()
    {
        _navSales.Active = true;
        _title.Text = "Sales";
        _subtitle.Text = "Create a sale for the selected branch.";
        _search.Visible = true;
        SetPageVisible(_salesPageControls, true);
    }

    // ---- Sales logic ----

    private async void SalesForm_Load(object? sender, EventArgs e)
    {
        _statusLabel.Text = "Loading branches...";

        try
        {
            var catalog = await _apiClient.GetCatalogAsync();

            _isLoadingBranches = true;
            _branchPicker.DisplayMember = nameof(SalesBranchDto.Name);
            _branchPicker.DataSource = catalog.Branches.ToList();
            _isLoadingBranches = false;

            if (catalog.Branches.Count == 1)
            {
                // One branch: no drop-down is needed, just show which branch this sale is for.
                _branchPickerRow.Visible = false;
                _branchChip.BranchName = catalog.Branches[0].Name;
                _branchChip.Visible = true;
            }

            if (_branchPicker.SelectedItem is SalesBranchDto)
                await LoadProductsForSelectedBranchAsync();
            else
                _statusLabel.Text = "No active branches are available.";
        }
        catch (HttpRequestException exception)
        {
            _statusLabel.Text = "Could not load the sales catalog.";
            MessageBox.Show(
                exception.Message,
                "API request failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _isLoadingBranches = false;
        }
    }

    private async void BranchPicker_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        if (!_isLoadingBranches)
            await LoadProductsForSelectedBranchAsync();
    }

    private async Task LoadProductsForSelectedBranchAsync()
    {
        if (_branchPicker.SelectedItem is not SalesBranchDto branch)
        {
            _productPicker.DataSource = null;
            UpdateAddButtonState();
            return;
        }

        var branchChanged =
            _loadedBranchId is Guid previousBranchId &&
            previousBranchId != branch.Id;
        var cartWasCleared = branchChanged && _cart.Count > 0;

        if (cartWasCleared)
        {
            _cart.Clear();
            _payments.Clear();
            _paymentsList.Items.Clear();
            RefreshCart();
        }

        _loadedBranchId = branch.Id;

        // The header search can start a new lookup while an older one is still loading.
        // Only the newest lookup is allowed to fill the product list.
        var requestId = ++_loadRequestId;

        _addButton.Enabled = false;
        _statusLabel.Text = "Loading branch products...";

        try
        {
            var catalog = await _apiClient.GetCatalogAsync(
                branch.Id,
                _search.SearchText);

            if (requestId != _loadRequestId)
                return;

            _productPicker.DisplayMember = nameof(SalesProductDto.Name);
            _productPicker.DataSource = catalog.Products.ToList();

            var loadedMessage = catalog.Products.Count == 0
                ? "No matching products found for this branch."
                : $"Loaded {catalog.Products.Count} matching products.";

            _statusLabel.Text = cartWasCleared
                ? $"Branch changed; previous cart cleared. {loadedMessage}"
                : loadedMessage;

            UpdateAddButtonState();
        }
        catch (HttpRequestException exception)
        {
            if (requestId != _loadRequestId)
                return;

            _statusLabel.Text = "Could not load branch products.";
            MessageBox.Show(
                exception.Message,
                "API request failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void AddButton_Click(object? sender, EventArgs e)
    {
        if (_productPicker.SelectedItem is not SalesProductDto product)
            return;

        var quantity = (int)_quantityPicker.Value;
        var existingLine = _cart.FirstOrDefault(
            line => line.Product.Id == product.Id);
        var quantityAlreadyInCart = existingLine?.Quantity ?? 0;

        if (quantityAlreadyInCart + quantity > product.QuantityOnHand)
        {
            _statusLabel.Text =
                $"Only {product.QuantityOnHand} available for {product.Name}; " +
                $"{quantityAlreadyInCart} already in the cart.";
            UpdateAddButtonState();
            return;
        }

        if (existingLine is null)
            _cart.Add(new CartLine(product, quantity));
        else
            existingLine.Quantity += quantity;

        RefreshCart();

        // Show the line that was just added or changed, then get ready for the next item.
        var addedIndex = _cart.FindIndex(line => line.Product.Id == product.Id);
        if (addedIndex >= 0)
            _cartList.SelectedIndex = addedIndex;

        _quantityPicker.Value = 1;
        UpdateAddButtonState();
        _statusLabel.Text = $"Added {quantity} x {product.Name} to the sale.";
        _productPicker.Focus();
    }

    // Enter in the product or quantity box adds the item, so a clerk can work without the mouse.
    private void AddFromKeyboard(KeyEventArgs e)
    {
        e.SuppressKeyPress = true;
        e.Handled = true;

        if (_addButton.Enabled)
            AddButton_Click(this, EventArgs.Empty);
    }

    private void UpdateAddButtonState()
    {
        if (_productPicker.SelectedItem is not SalesProductDto product)
        {
            _addButton.Enabled = false;
            _stockHintLabel.Text = "Choose a product to add it to the sale.";
            _stockHintLabel.ForeColor = Palette.TextSecondary.Color;
            _lineTotalLabel.Text = string.Empty;
            return;
        }

        var quantityAlreadyInCart = _cart
            .FirstOrDefault(line => line.Product.Id == product.Id)
            ?.Quantity ?? 0;
        var quantity = (int)_quantityPicker.Value;
        var stillAvailable = product.QuantityOnHand - quantityAlreadyInCart;

        _addButton.Enabled =
            product.QuantityOnHand > quantityAlreadyInCart &&
            quantityAlreadyInCart + quantity
                <= product.QuantityOnHand;

        if (product.QuantityOnHand <= 0)
        {
            _stockHintLabel.Text = "Out of stock";
            _stockHintLabel.ForeColor = DangerColor;
        }
        else if (stillAvailable <= 0)
        {
            _stockHintLabel.Text = $"All {product.QuantityOnHand} are already in the sale.";
            _stockHintLabel.ForeColor = DangerColor;
        }
        else if (quantity > stillAvailable)
        {
            _stockHintLabel.Text = $"Only {stillAvailable} more available.";
            _stockHintLabel.ForeColor = WarnColor;
        }
        else if (stillAvailable <= 3)
        {
            _stockHintLabel.Text = $"Low stock: {stillAvailable} left.";
            _stockHintLabel.ForeColor = WarnColor;
        }
        else
        {
            _stockHintLabel.Text = $"{stillAvailable} available.";
            _stockHintLabel.ForeColor = Palette.TextSecondary.Color;
        }

        _lineTotalLabel.Text = $"Line total  {product.UnitPrice * quantity:C2}";
    }

    private decimal GetCartTotal() =>
        _cart.Sum(line => line.Product.UnitPrice * line.Quantity);

    // The - / + / x buttons on a sale item row.
    private void CartList_RowAction(int index, RowListBox.RowZone zone)
    {
        if (index < 0 || index >= _cart.Count)
            return;

        var line = _cart[index];

        switch (zone)
        {
            case RowListBox.RowZone.Plus:
                if (line.Quantity + 1 > line.Product.QuantityOnHand)
                {
                    _statusLabel.Text =
                        $"Only {line.Product.QuantityOnHand} available for {line.Product.Name}.";
                    return;
                }

                line.Quantity++;
                break;

            case RowListBox.RowZone.Minus:
                line.Quantity--;
                if (line.Quantity <= 0)
                    _cart.RemoveAt(index);
                break;

            case RowListBox.RowZone.Remove:
                _cart.RemoveAt(index);
                break;

            default:
                return;
        }

        RefreshCart();

        var newIndex = _cart.IndexOf(line);
        if (newIndex >= 0)
            _cartList.SelectedIndex = newIndex;

        UpdateAddButtonState();
    }

    private void ClearSaleButton_Click(object? sender, EventArgs e)
    {
        if (_cart.Count == 0 && _payments.Count == 0)
            return;

        var answer = MessageBox.Show(
            this,
            "Remove all items and payments from this sale?",
            "Clear sale",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (answer != DialogResult.Yes)
            return;

        _cart.Clear();
        _payments.Clear();
        _paymentsList.Items.Clear();
        RefreshCart();
        UpdateAddButtonState();
        _statusLabel.Text = "Sale cleared.";
    }

    private void AddPaymentButton_Click(object? sender, EventArgs e)
    {
        var remaining = GetCartTotal() - _payments.Sum(payment => payment.Amount);

        if (remaining <= 0)
        {
            _statusLabel.Text = "The sale is already fully paid.";
            return;
        }

        if (_paymentAmountPicker.Value > remaining)
        {
            _statusLabel.Text = $"Payment cannot exceed the remaining {remaining:C2}.";
            return;
        }

        var method = _paymentMethodPicker.SelectedItem;

        if (string.IsNullOrWhiteSpace(method))
            return;

        var payment = new PaymentEntry(method, _paymentAmountPicker.Value);
        _payments.Add(payment);
        _paymentsList.Items.Add(payment);

        UpdatePaymentSummary();
        SuggestNextPaymentAmount();
    }

    private void PaymentsList_RowAction(int index, RowListBox.RowZone zone)
    {
        if (zone == RowListBox.RowZone.Remove)
            RemovePaymentAt(index);
    }

    private void RemovePaymentAt(int index)
    {
        if (index < 0 || index >= _payments.Count)
            return;

        _payments.RemoveAt(index);
        _paymentsList.Items.RemoveAt(index);

        UpdatePaymentSummary();
        SuggestNextPaymentAmount();
    }

    /// <summary>
    /// Puts the outstanding balance in the amount box so a split payment is one click
    /// away (for example 500 cash first, then the remainder on card).
    /// </summary>
    private void SuggestNextPaymentAmount()
    {
        var remaining = GetCartTotal() - _payments.Sum(payment => payment.Amount);

        _paymentAmountPicker.Value = remaining > _paymentAmountPicker.Minimum
            ? Math.Min(remaining, _paymentAmountPicker.Maximum)
            : _paymentAmountPicker.Minimum;
    }

    private void UpdatePaymentSummary()
    {
        var total = GetCartTotal();
        var paid = _payments.Sum(payment => payment.Amount);
        var remaining = total - paid;

        _totalLabel.Text = total.ToString("C2");
        _paidLabel.Text = paid.ToString("C2");
        _remainingCaption.Text = remaining >= 0 ? "Remaining" : "Overpaid";
        _remainingLabel.Text = Math.Abs(remaining).ToString("C2");

        // The Remaining amount changes colour with the state of the sale (no message text).
        var stateColor = _cart.Count == 0 ? Palette.TextSecondary.Color
            : remaining > 0 ? WarnColor
            : remaining < 0 ? DangerColor
            : OkColor;

        _remainingLabel.ForeColor = stateColor;

        _completeSaleButton.Enabled = _cart.Count > 0 && remaining == 0;
        _addPaymentButton.Enabled = remaining > 0;
        _clearSaleButton.Enabled = _cart.Count > 0 || _payments.Count > 0;

        // Friendly empty states sit on top of the two lists while they have no rows.
        _cartEmpty.Visible = _cart.Count == 0;
        _paymentsEmpty.Visible = _payments.Count == 0;
    }

    private async void CompleteSaleButton_Click(object? sender, EventArgs e)
    {
        if (_branchPicker.SelectedItem is not SalesBranchDto branch ||
            _cart.Count == 0 ||
            _payments.Sum(payment => payment.Amount) != GetCartTotal())
        {
            return;
        }

        _completeSaleButton.Enabled = false;
        _statusLabel.Text = "Saving sale...";

        try
        {
            var request = new CreateSaleRequestDto(
                branch.Id,
                CustomerId: null,
                Lines: _cart
                    .Select(line => new CreateSaleLineRequestDto(
                        line.Product.Id,
                        line.Quantity))
                    .ToList(),
                Payments: _payments
                    .Select(payment => new CreateSalePaymentRequestDto(
                        payment.Method,
                        payment.Amount))
                    .ToList());

            var sale = await _apiClient.CreateAsync(request);

            _cart.Clear();
            _payments.Clear();
            _paymentsList.Items.Clear();
            RefreshCart();
            await LoadProductsForSelectedBranchAsync();

            _statusLabel.Text =
                $"Sale {sale.SaleNumber} saved. Total: {sale.TotalAmount:C2}.";
        }
        catch (HttpRequestException exception)
        {
            _statusLabel.Text = "Could not save the sale.";
            MessageBox.Show(
                exception.Message,
                "API request failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            UpdatePaymentSummary();
        }
    }

    private void RefreshCart()
    {
        // Keep the scroll position, so tapping + or - on a row far down the list does not jump to the top.
        var topIndex = _cartList.TopIndex;

        _cartList.BeginUpdate();
        _cartList.Items.Clear();

        foreach (var line in _cart)
            _cartList.Items.Add(line);

        _cartList.EndUpdate();

        if (_cartList.Items.Count > 0)
            _cartList.TopIndex = Math.Min(topIndex, _cartList.Items.Count - 1);

        var units = _cart.Sum(line => line.Quantity);
        _cartCountLabel.Text = units == 0
            ? "No items yet"
            : $"{units} {(units == 1 ? "item" : "items")}";

        UpdatePaymentSummary();

        // Always offer the balance as the next payment amount, not only after a first payment.
        SuggestNextPaymentAmount();
    }

    // ---- Borderless window plumbing (same as the Tenant Admin screen) ----

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

        // Below this size the sale body scrolls, so the payments list is never squashed to nothing.
        _salesBody.AutoScrollMinSize = new Size(
            (int)Math.Round(MinBodyWidth * scale),
            (int)Math.Round(MinBodyHeight * scale));
    }

    // ---- Control factories (same as the Tenant Admin screen) ----

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

    // ---- Custom controls (same look as the Tenant Admin screen) ----

    /// <summary>
    /// A sidebar row with a thin line icon. The active page gets an inset light-blue bar,
    /// matching the navigation style of the Tenant Admin screen.
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
                case NavIcon.Sales:
                    {
                        // A shopping cart: handle, basket and two wheels.
                        float X(float u) => bounds.X + bounds.Width * u;
                        float Y(float v) => bounds.Y + bounds.Height * v;

                        g.DrawLines(pen, [
                            new PointF(X(0.02f), Y(0.14f)),
                            new PointF(X(0.18f), Y(0.14f)),
                            new PointF(X(0.34f), Y(0.70f)),
                            new PointF(X(0.88f), Y(0.70f))
                        ]);

                        g.DrawLines(pen, [
                            new PointF(X(0.22f), Y(0.30f)),
                            new PointF(X(0.96f), Y(0.30f)),
                            new PointF(X(0.86f), Y(0.58f)),
                            new PointF(X(0.29f), Y(0.58f))
                        ]);

                        var wheel = 1.3f * scale;
                        g.DrawEllipse(pen, X(0.42f) - wheel, Y(0.88f) - wheel, wheel * 2, wheel * 2);
                        g.DrawEllipse(pen, X(0.80f) - wheel, Y(0.88f) - wheel, wheel * 2, wheel * 2);
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
    /// Wraps a text box, number box, drop-down, or list in the same rounded outline the Tenant Admin
    /// screen uses for its search box and filter: soft blue border, primary blue when focused.
    /// The wrapped control keeps all of its normal properties and events.
    /// </summary>
    private sealed class FieldFrame : Panel
    {
        private readonly Control _input;
        private bool _focused;
        private bool _laidOut;
        private Control? _overlay;

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

        /// <summary>A control (such as an empty-state message) that sits on top of the wrapped control.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Control? Overlay
        {
            get => _overlay;
            set
            {
                if (_overlay is not null)
                    Controls.Remove(_overlay);

                _overlay = value;

                if (value is not null)
                {
                    Controls.Add(value);
                    value.BringToFront();
                }

                PerformLayout();
            }
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

                if (_input is ListBox)
                {
                    // A list fills the frame, with a little room so its rows clear the rounded corners.
                    var inset = (int)Math.Round(6 * scale);
                    _input.SetBounds(
                        inset,
                        inset,
                        Math.Max(1, Width - inset * 2),
                        Math.Max(1, Height - inset * 2));
                }
                else if (_input is TextBox { Multiline: true })
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

                if (_overlay is not null)
                {
                    var overlayInset = (int)Math.Round(6 * scale);
                    _overlay.SetBounds(
                        overlayInset,
                        overlayInset,
                        Math.Max(1, Width - overlayInset * 2),
                        Math.Max(1, Height - overlayInset * 2));
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

        /// <summary>Optional painter for rich rows (several columns). Used for the open list and the closed box.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Action<Graphics, Rectangle, object, bool, float>? ItemPainter { get; set; }

        /// <summary>Row height of the open list, in 96 DPI units.</summary>
        [DefaultValue(26)]
        public int RowHeight { get; set; } = 26;

        /// <summary>Grey text shown while nothing is selected.</summary>
        [DefaultValue("")]
        public string PlaceholderText { get; set; } = string.Empty;

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
            ItemHeight = (int)Math.Round(RowHeight * DeviceDpi / 96f);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var scale = DeviceDpi / 96f;
            var chevronArea = (int)Math.Round(26 * scale);
            var textArea = new Rectangle(0, 0, Math.Max(1, Width - chevronArea), Height);

            if (ItemPainter is not null && SelectedItem is { } selectedItem)
            {
                ItemPainter(g, textArea, selectedItem, false, scale);
            }
            else
            {
                var showPlaceholder = SelectedItem is null && PlaceholderText.Length > 0;
                TextRenderer.DrawText(
                    g,
                    showPlaceholder ? PlaceholderText : Text,
                    Font,
                    textArea,
                    showPlaceholder ? Palette.TextSecondary.Color : ForeColor,
                    TextFormatFlags.Left |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.SingleLine |
                    TextFormatFlags.NoPrefix |
                    TextFormatFlags.EndEllipsis);
            }

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

            if (ItemPainter is not null)
            {
                if ((e.State & DrawItemState.ComboBoxEdit) == 0)
                    ItemPainter(e.Graphics, e.Bounds, Items[e.Index], highlighted, DeviceDpi / 96f);

                return;
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
    /// A quiet text-only button: blue underlined words, no fill and no border. It gets a little
    /// darker when the mouse is over it. It is still a Button: Click, Enabled and focus all work.
    /// </summary>
    private sealed class LinkButton : Button
    {
        private bool _hover;
        private bool _pressed;

        public LinkButton(string text)
        {
            Text = text;
            Font = LinkFont;
            ForeColor = Palette.Primary.Color;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
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

            var textColor = !Enabled ? Color.FromArgb(
                    (Palette.Primary.Color.R + parentBack.R) / 2,
                    (Palette.Primary.Color.G + parentBack.G) / 2,
                    (Palette.Primary.Color.B + parentBack.B) / 2)
                : _pressed ? BrandShades.deepBlue.Color
                : _hover ? BrandShades.royalBlue.Color
                : Palette.Primary.Color;

            // Right-aligned, so the words line up with the right edge of the list below them.
            TextRenderer.DrawText(
                g,
                Text,
                Font,
                ClientRectangle,
                textColor,
                TextFormatFlags.Right |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.NoPrefix |
                TextFormatFlags.EndEllipsis);

            if (Focused && ShowFocusCues && Enabled)
            {
                using var focusPen = new Pen(Color.FromArgb(120, textColor), 1f) { DashStyle = DashStyle.Dot };
                g.DrawRectangle(focusPen, 0, 0, Width - 1, Height - 1);
            }
        }
    }

    /// <summary>A grid that paints without flicker while the window is resized.</summary>
    private sealed class BufferedTable : TableLayoutPanel
    {
        public BufferedTable()
        {
            DoubleBuffered = true;
        }
    }

    /// <summary>A soft rounded surface that groups related controls: the add-item bar, the sale items, checkout.</summary>
    private sealed class CardPanel : Panel
    {
        public CardPanel()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            BackColor = CardFillColor;
            Dock = DockStyle.Fill;
            Margin = Padding.Empty;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Palette.BackgroundSecondary.Color);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            var scale = DeviceDpi / 96f;
            var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);

            using var path = RoundedRectangle(bounds, 10f * scale);
            using var fill = new SolidBrush(CardFillColor);
            using var pen = new Pen(CardBorderColor, 1f);
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
        }
    }

    /// <summary>
    /// The status line at the bottom of the page. It changes colour with the kind of message, so a
    /// problem stands out from a plain "Loaded 3 matching products." without changing any caller.
    /// </summary>
    private sealed class StatusLabel : Label
    {
        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            ForeColor = ColorFor(Text);
        }

        private static Color ColorFor(string text)
        {
            if (text.StartsWith("Could not", StringComparison.Ordinal))
                return DangerColor;

            if (text.StartsWith("Only ", StringComparison.Ordinal) ||
                text.Contains("cannot exceed", StringComparison.Ordinal) ||
                text.Contains("already fully paid", StringComparison.Ordinal))
                return WarnColor;

            if (text.Contains(" saved.", StringComparison.Ordinal))
                return OkColor;

            return Palette.TextSecondary.Color;
        }
    }

    /// <summary>Small pill that says which branch the sale is for, when there is only one to choose from.</summary>
    private sealed class BranchChip : Control
    {
        private string _branchName = string.Empty;

        public BranchChip()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
        }

        [DefaultValue("")]
        public string BranchName
        {
            get => _branchName;
            set
            {
                _branchName = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var scale = DeviceDpi / 96f;
            int S(float value) => (int)Math.Round(value * scale);

            const TextFormatFlags measureFlags =
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
            var unbounded = new Size(int.MaxValue, int.MaxValue);

            var captionWidth = TextRenderer.MeasureText(g, "BRANCH", PillFont, unbounded, measureFlags).Width;
            var nameWidth = TextRenderer.MeasureText(g, _branchName, ValueFont, unbounded, measureFlags).Width;

            var pillHeight = S(32);
            var pillWidth = Math.Min(Width - 1, S(14) + captionWidth + S(10) + S(1) + S(10) + nameWidth + S(14));
            var pill = new Rectangle(0, (Height - pillHeight) / 2, pillWidth, pillHeight);

            using (var path = RoundedRectangle(pill, pill.Height / 2))
            using (var fill = new SolidBrush(Color.White))
            using (var border = new Pen(SidebarActiveColor, 1f))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
            }

            const TextFormatFlags drawFlags =
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis;

            var captionRect = new Rectangle(pill.Left + S(14), pill.Top, captionWidth + S(2), pill.Height);
            TextRenderer.DrawText(g, "BRANCH", PillFont, captionRect, ProfileRolePillColor, drawFlags);

            var dividerX = captionRect.Right + S(8);
            using (var divider = new Pen(SidebarActiveColor, 1f))
            {
                g.DrawLine(divider, dividerX, pill.Top + S(8), dividerX, pill.Bottom - S(8));
            }

            var nameRect = new Rectangle(dividerX + S(10), pill.Top, Math.Max(1, pill.Right - S(10) - (dividerX + S(10))), pill.Height);
            TextRenderer.DrawText(g, _branchName, ValueFont, nameRect, Palette.TextPrimary.Color, drawFlags);
        }
    }

    private enum EmptyIcon
    {
        Cart,
        Card
    }

    /// <summary>The friendly message shown on top of a list while it has no rows.</summary>
    private sealed class EmptyState : Control
    {
        private readonly EmptyIcon _icon;
        private readonly string _title;
        private readonly string _hint;

        public EmptyState(EmptyIcon icon, string title, string hint)
        {
            _icon = icon;
            _title = title;
            _hint = hint;
            BackColor = Palette.BackgroundSecondary.Color;

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
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var scale = DeviceDpi / 96f;
            int S(float value) => (int)Math.Round(value * scale);

            const TextFormatFlags centered =
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;

            // On a short list there is only room for the words.
            var showIcon = Height >= S(112);
            var iconSize = showIcon ? S(40) : 0;
            var iconGap = showIcon ? S(10) : 0;
            var blockHeight = iconSize + iconGap + S(22) + S(20);
            var top = Math.Max(0, (Height - blockHeight) / 2);

            if (showIcon)
                DrawIcon(g, new Rectangle((Width - iconSize) / 2, top, iconSize, iconSize), scale);

            var titleTop = top + iconSize + iconGap;
            TextRenderer.DrawText(g, _title, RowTitleFont, new Rectangle(0, titleTop, Width, S(22)), Palette.TextPrimary.Color, centered);
            TextRenderer.DrawText(g, _hint, SubtitleFont, new Rectangle(S(8), titleTop + S(22), Width - S(16), S(20)), Palette.TextSecondary.Color, centered);
        }

        private void DrawIcon(Graphics g, Rectangle bounds, float scale)
        {
            using var pen = new Pen(MutedIconColor, Math.Max(1f, 1.75f * scale))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            float X(float u) => bounds.X + bounds.Width * u;
            float Y(float v) => bounds.Y + bounds.Height * v;

            if (_icon == EmptyIcon.Cart)
            {
                g.DrawLines(pen, [
                    new PointF(X(0.02f), Y(0.14f)),
                    new PointF(X(0.18f), Y(0.14f)),
                    new PointF(X(0.34f), Y(0.70f)),
                    new PointF(X(0.88f), Y(0.70f))
                ]);

                g.DrawLines(pen, [
                    new PointF(X(0.22f), Y(0.30f)),
                    new PointF(X(0.96f), Y(0.30f)),
                    new PointF(X(0.86f), Y(0.58f)),
                    new PointF(X(0.29f), Y(0.58f))
                ]);

                var wheel = 2f * scale;
                g.DrawEllipse(pen, X(0.42f) - wheel, Y(0.88f) - wheel, wheel * 2, wheel * 2);
                g.DrawEllipse(pen, X(0.80f) - wheel, Y(0.88f) - wheel, wheel * 2, wheel * 2);
                return;
            }

            // A payment card: rounded outline, a stripe and a small chip line.
            var body = new RectangleF(X(0.04f), Y(0.20f), bounds.Width * 0.92f, bounds.Height * 0.60f);
            using (var path = RoundedRectangle(body, 4f * scale))
            {
                g.DrawPath(pen, path);
            }

            g.DrawLine(pen, body.Left, body.Top + body.Height * 0.34f, body.Right, body.Top + body.Height * 0.34f);
            g.DrawLine(pen, body.Left + body.Width * 0.12f, body.Bottom - body.Height * 0.24f, body.Left + body.Width * 0.38f, body.Bottom - body.Height * 0.24f);
        }
    }

    /// <summary>
    /// Segmented buttons for choosing one option (the payment method) with a single tap.
    /// Arrow keys move the selection; Tab moves on.
    /// </summary>
    private sealed class SegmentedPicker : Control
    {
        private readonly List<string> _items = [];
        private int _selectedIndex = -1;
        private int _hoverIndex = -1;

        public SegmentedPicker()
        {
            TabStop = true;
            Cursor = Cursors.Hand;
            Font = PrimaryButtonFont;

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
        }

        public event EventHandler? SelectedIndexChanged;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<string, string>? DisplayText { get; set; }

        [DefaultValue(-1)]
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (value == _selectedIndex || value < -1 || value >= _items.Count)
                    return;

                _selectedIndex = value;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string? SelectedItem =>
            _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null;

        public void SetItems(params string[] items)
        {
            _items.Clear();
            _items.AddRange(items);
            _selectedIndex = -1;
            Invalidate();
        }

        private Rectangle InnerBounds()
        {
            var inset = (int)Math.Round(3 * DeviceDpi / 96f);
            return Rectangle.Inflate(new Rectangle(0, 0, Width - 1, Height - 1), -inset, -inset);
        }

        private Rectangle SegmentBounds(int index)
        {
            var inner = InnerBounds();
            var segmentWidth = inner.Width / Math.Max(1, _items.Count);
            var left = inner.X + index * segmentWidth;
            var right = index == _items.Count - 1 ? inner.Right : left + segmentWidth;
            return new Rectangle(left, inner.Y, right - left, inner.Height);
        }

        private int SegmentAt(Point point)
        {
            for (var i = 0; i < _items.Count; i++)
            {
                if (SegmentBounds(i).Contains(point))
                    return i;
            }

            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var hover = SegmentAt(e.Location);
            if (hover != _hoverIndex)
            {
                _hoverIndex = hover;
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
            Focus();

            if (e.Button == MouseButtons.Left)
            {
                var index = SegmentAt(e.Location);
                if (index >= 0)
                    SelectedIndex = index;
            }

            base.OnMouseDown(e);
        }

        protected override bool IsInputKey(Keys keyData) =>
            keyData is Keys.Left or Keys.Right or Keys.Up or Keys.Down || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_items.Count > 0)
            {
                if (e.KeyCode is Keys.Left or Keys.Up)
                {
                    SelectedIndex = Math.Max(0, _selectedIndex - 1);
                    e.Handled = true;
                }
                else if (e.KeyCode is Keys.Right or Keys.Down)
                {
                    SelectedIndex = Math.Min(_items.Count - 1, _selectedIndex + 1);
                    e.Handled = true;
                }
            }

            base.OnKeyDown(e);
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

            var scale = DeviceDpi / 96f;
            var radius = (int)Math.Round(8 * scale);
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

            using (var path = RoundedRectangle(bounds, radius))
            using (var fill = new SolidBrush(Color.White))
            using (var border = new Pen(Focused && ShowFocusCues ? Palette.Primary.Color : BrandTints.powderBlue.Color, 1f))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
            }

            const TextFormatFlags centered =
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;

            for (var i = 0; i < _items.Count; i++)
            {
                var segment = SegmentBounds(i);
                var selected = i == _selectedIndex;
                var label = DisplayText?.Invoke(_items[i]) ?? _items[i];

                if (selected)
                {
                    using var path = RoundedRectangle(segment, Math.Max(2, radius - (int)Math.Round(2 * scale)));
                    using var fill = new SolidBrush(Palette.Primary.Color);
                    g.FillPath(fill, path);
                }
                else if (i == _hoverIndex)
                {
                    using var path = RoundedRectangle(segment, Math.Max(2, radius - (int)Math.Round(2 * scale)));
                    using var fill = new SolidBrush(Color.FromArgb(240, 244, 252));
                    g.FillPath(fill, path);
                }

                TextRenderer.DrawText(
                    g,
                    label,
                    selected ? PrimaryButtonFont : SecondaryButtonFont,
                    segment,
                    selected ? Color.White : Palette.TextPrimary.Color,
                    centered);
            }
        }
    }

    /// <summary>
    /// A list of two-line rows with a right-aligned amount, built for the sale items and the payments.
    /// Rows can show a - / + quantity stepper and always show a remove (x) button. It is still a
    /// ListBox, so scrolling and the selected row work as usual; Delete removes the selected row.
    /// Put it inside a FieldFrame for the rounded outline.
    /// </summary>
    private sealed class RowListBox : ListBox
    {
        public enum RowZone
        {
            None,
            Minus,
            Plus,
            Remove
        }

        private readonly record struct RowZones(
            Rectangle Text,
            Rectangle Minus,
            Rectangle Qty,
            Rectangle Plus,
            Rectangle Value,
            Rectangle Remove);

        private int _hoverIndex = -1;
        private RowZone _hoverZone;

        public RowListBox()
        {
            BorderStyle = BorderStyle.None;
            DrawMode = DrawMode.OwnerDrawFixed;
            IntegralHeight = false;
            DoubleBuffered = true;
        }

        /// <summary>Raised (after the click has finished) when a - / + / x button on a row is pressed.</summary>
        public event Action<int, RowZone>? RowAction;

        /// <summary>Turns a list item into the text shown on its row.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<object, RowContent>? Content { get; set; }

        /// <summary>Row height in 96 DPI units.</summary>
        [DefaultValue(64)]
        public int RowHeight { get; set; } = 64;

        [DefaultValue(false)]
        public bool ShowStepper { get; set; }

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string? subAppName, string? subIdList);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ItemHeight = (int)Math.Round(RowHeight * DeviceDpi / 96f);

            // The list scrolls (mouse wheel, scrollbar, arrow keys) when there are more rows than fit.
            // The Explorer theme gives it the slim, modern scrollbar instead of the old gray one.
            ScrollAlwaysVisible = false;
            _ = SetWindowTheme(Handle, "Explorer", null);
        }

        private RowZones GetZones(Rectangle row, float scale)
        {
            int S(float value) => (int)Math.Round(value * scale);

            var button = S(26);
            var buttonTop = row.Top + (row.Height - button) / 2;

            var remove = new Rectangle(row.Right - S(10) - button, buttonTop, button, button);
            var valueWidth = S(92);
            var value = new Rectangle(remove.Left - S(8) - valueWidth, row.Top, valueWidth, row.Height);

            var minus = Rectangle.Empty;
            var qty = Rectangle.Empty;
            var plus = Rectangle.Empty;
            var textRight = value.Left - S(12);

            if (ShowStepper)
            {
                var qtyWidth = S(32);
                var stepperLeft = value.Left - S(12) - (button * 2 + qtyWidth);
                minus = new Rectangle(stepperLeft, buttonTop, button, button);
                qty = new Rectangle(minus.Right, row.Top, qtyWidth, row.Height);
                plus = new Rectangle(qty.Right, buttonTop, button, button);
                textRight = stepperLeft - S(12);
            }

            var textLeft = row.Left + S(14);
            var text = new Rectangle(textLeft, row.Top, Math.Max(S(40), textRight - textLeft), row.Height);
            return new RowZones(text, minus, qty, plus, value, remove);
        }

        private RowZone HitZone(int index, Point point)
        {
            var zones = GetZones(GetItemRectangle(index), DeviceDpi / 96f);

            if (zones.Remove.Contains(point))
                return RowZone.Remove;

            if (ShowStepper && zones.Minus.Contains(point))
                return RowZone.Minus;

            if (ShowStepper && zones.Plus.Contains(point))
                return RowZone.Plus;

            return RowZone.None;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            var index = IndexFromPoint(e.Location);
            var zone = index >= 0 ? HitZone(index, e.Location) : RowZone.None;

            if (index != _hoverIndex || zone != _hoverZone)
            {
                _hoverIndex = index;
                _hoverZone = zone;
                Cursor = zone == RowZone.None ? Cursors.Default : Cursors.Hand;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (_hoverIndex != -1 || _hoverZone != RowZone.None)
            {
                _hoverIndex = -1;
                _hoverZone = RowZone.None;
                Cursor = Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button != MouseButtons.Left)
                return;

            var index = IndexFromPoint(e.Location);
            if (index < 0)
                return;

            var zone = HitZone(index, e.Location);
            if (zone == RowZone.None)
                return;

            // Let the click finish first: the form rebuilds the rows in response.
            BeginInvoke(new Action(() => RowAction?.Invoke(index, zone)));
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (SelectedIndex < 0)
                return;

            var zone = e.KeyCode switch
            {
                Keys.Delete => RowZone.Remove,
                Keys.Add or Keys.Oemplus when ShowStepper => RowZone.Plus,
                Keys.Subtract or Keys.OemMinus when ShowStepper => RowZone.Minus,
                _ => RowZone.None
            };

            if (zone == RowZone.None)
                return;

            var index = SelectedIndex;
            e.Handled = true;
            BeginInvoke(new Action(() => RowAction?.Invoke(index, zone)));
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= Items.Count || Content is null)
                return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var scale = DeviceDpi / 96f;
            int S(float value) => (int)Math.Round(value * scale);

            var row = e.Bounds;
            var selected = (e.State & DrawItemState.Selected) != 0;
            var hovered = e.Index == _hoverIndex;

            using (var back = new SolidBrush(
                selected ? RowSelectedColor : hovered ? RowHoverColor : Palette.BackgroundSecondary.Color))
            {
                g.FillRectangle(back, row);
            }

            using (var divider = new Pen(RowDividerColor, 1f))
            {
                g.DrawLine(divider, row.Left + S(12), row.Bottom - 1, row.Right - S(12), row.Bottom - 1);
            }

            // The row is white even when selected, so a thin accent bar marks the selected row
            // (the + / - / Delete keys act on it).
            if (selected)
            {
                using var accent = new SolidBrush(Palette.Primary.Color);
                g.FillRectangle(accent, row.Left + S(2), row.Top + S(10), S(3), row.Height - S(20));
            }

            var content = Content(Items[e.Index]);
            var zones = GetZones(row, scale);

            const TextFormatFlags leftFlags =
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;

            if (!string.IsNullOrEmpty(content.Subtitle))
            {
                TextRenderer.DrawText(
                    g,
                    content.Title,
                    RowTitleFont,
                    new Rectangle(zones.Text.X, row.Top + S(11), zones.Text.Width, S(22)),
                    Palette.TextPrimary.Color,
                    leftFlags);
                TextRenderer.DrawText(
                    g,
                    content.Subtitle,
                    RowSubFont,
                    new Rectangle(zones.Text.X, row.Top + S(33), zones.Text.Width, S(20)),
                    Palette.TextSecondary.Color,
                    leftFlags);
            }
            else
            {
                TextRenderer.DrawText(g, content.Title, RowTitleFont, zones.Text, Palette.TextPrimary.Color, leftFlags);
            }

            TextRenderer.DrawText(
                g,
                content.Value,
                ValueFont,
                zones.Value,
                Palette.TextPrimary.Color,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

            if (ShowStepper)
            {
                DrawStepButton(g, zones.Minus, plus: false, hot: hovered && _hoverZone == RowZone.Minus, scale);
                DrawStepButton(g, zones.Plus, plus: true, hot: hovered && _hoverZone == RowZone.Plus, scale);
                TextRenderer.DrawText(
                    g,
                    content.Quantity.ToString(),
                    ValueFont,
                    zones.Qty,
                    Palette.TextPrimary.Color,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            }

            DrawRemoveButton(g, zones.Remove, hovered && _hoverZone == RowZone.Remove, scale);
        }

        private static void DrawStepButton(Graphics g, Rectangle bounds, bool plus, bool hot, float scale)
        {
            var radius = (int)Math.Round(6 * scale);
            var outline = new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);

            using (var path = RoundedRectangle(outline, radius))
            using (var fill = new SolidBrush(hot ? Color.FromArgb(222, 231, 250) : Palette.BackgroundSecondary.Color))
            using (var border = new Pen(hot ? Palette.Primary.Color : BrandTints.powderBlue.Color, 1f))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
            }

            using var pen = new Pen(Palette.Primary.Color, Math.Max(1f, 1.6f * scale))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };

            var cx = bounds.X + bounds.Width / 2f;
            var cy = bounds.Y + bounds.Height / 2f;
            var arm = 4.5f * scale;

            g.DrawLine(pen, cx - arm, cy, cx + arm, cy);
            if (plus)
                g.DrawLine(pen, cx, cy - arm, cx, cy + arm);
        }

        private static void DrawRemoveButton(Graphics g, Rectangle bounds, bool hot, float scale)
        {
            if (hot)
            {
                using var path = RoundedRectangle(
                    new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1),
                    (int)Math.Round(6 * scale));
                using var fill = new SolidBrush(Color.FromArgb(254, 226, 226));
                g.FillPath(fill, path);
            }

            using var pen = new Pen(hot ? DangerColor : Color.FromArgb(120, 130, 145), Math.Max(1f, 1.5f * scale))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };

            var cx = bounds.X + bounds.Width / 2f;
            var cy = bounds.Y + bounds.Height / 2f;
            var arm = 4f * scale;

            g.DrawLine(pen, cx - arm, cy - arm, cx + arm, cy + arm);
            g.DrawLine(pen, cx - arm, cy + arm, cx + arm, cy - arm);
        }
    }

    private sealed class CartLine(SalesProductDto product, int quantity)
    {
        public SalesProductDto Product { get; } = product;
        public int Quantity { get; set; } = quantity;
    }

    private sealed record PaymentEntry(string Method, decimal Amount);

    private readonly record struct RowContent(string Title, string? Subtitle, string Value, int Quantity);
}