using ThreadFlow.WinForms.ApiClient;

namespace ThreadFlow.WinForms;

public sealed class SalesForm : Form
{
    private readonly SalesApiClient _apiClient = new();

    private readonly ComboBox _branchPicker = new();
    private readonly ComboBox _productPicker = new();
    private readonly NumericUpDown _quantityPicker = new();
    private readonly Button _addButton = new();
    private readonly ListBox _cartList = new();
    private readonly Label _totalLabel = new();
    private readonly Button _completeSaleButton = new();
    private readonly Label _statusLabel = new();

    private readonly List<CartLine> _cart = [];

    public SalesForm()
    {
        Text = "ThreadFlow | Sales";
        ClientSize = new Size(860, 640);
        StartPosition = FormStartPosition.CenterParent;

        BuildForm();
        Load += SalesForm_Load;
    }

    private void BuildForm()
    {
        var branchLabel = new Label
        {
            Text = "Branch",
            Location = new Point(30, 25),
            AutoSize = true
        };

        _branchPicker.DropDownStyle = ComboBoxStyle.DropDownList;
        _branchPicker.Location = new Point(30, 50);
        _branchPicker.Width = 790;

        var productLabel = new Label
        {
            Text = "Product",
            Location = new Point(30, 95),
            AutoSize = true
        };

        _productPicker.DropDownStyle = ComboBoxStyle.DropDownList;
        _productPicker.FormattingEnabled = true;
        _productPicker.DisplayMember = nameof(SalesProductDto.Name);
        _productPicker.Format += (_, e) =>
        {
            if (e.ListItem is SalesProductDto product)
            {
                e.Value =
                    $"{product.Sku} - {product.Name} ({product.UnitPrice:C2})";
            }
        };
        _productPicker.Location = new Point(30, 120);
        _productPicker.Width = 550;

        var quantityLabel = new Label
        {
            Text = "Quantity",
            Location = new Point(600, 95),
            AutoSize = true
        };

        _quantityPicker.Minimum = 1;
        _quantityPicker.Maximum = 1000;
        _quantityPicker.Value = 1;
        _quantityPicker.Location = new Point(600, 120);
        _quantityPicker.Width = 90;

        _addButton.Text = "Add";
        _addButton.Location = new Point(710, 119);
        _addButton.Width = 110;
        _addButton.Enabled = false;
        _addButton.Click += AddButton_Click;

        var cartLabel = new Label
        {
            Text = "Sale items",
            Location = new Point(30, 175),
            AutoSize = true
        };

        _cartList.Location = new Point(30, 200);
        _cartList.Width = 790;
        _cartList.Height = 260;

        _totalLabel.Text = "Total: 0.00";
        _totalLabel.Location = new Point(30, 480);
        _totalLabel.AutoSize = true;

        _completeSaleButton.Text = "Complete Sale";
        _completeSaleButton.Location = new Point(30, 520);
        _completeSaleButton.Width = 160;
        _completeSaleButton.Height = 40;
        _completeSaleButton.Enabled = false;
        _completeSaleButton.Click += CompleteSaleButton_Click;

        _statusLabel.Location = new Point(30, 580);
        _statusLabel.AutoSize = true;

        Controls.AddRange(new Control[]
        {
            branchLabel, _branchPicker,
            productLabel, _productPicker,
            quantityLabel, _quantityPicker,
            _addButton, cartLabel, _cartList,
            _totalLabel, _completeSaleButton, _statusLabel
        });
    }

    private async void SalesForm_Load(object? sender, EventArgs e)
    {
        _statusLabel.Text = "Loading branch and product catalog...";

        try
        {
            var catalog = await _apiClient.GetCatalogAsync();

            _branchPicker.DataSource = catalog.Branches.ToList();
            _branchPicker.DisplayMember = nameof(SalesBranchDto.Name);

            _productPicker.DataSource = catalog.Products.ToList();

            _addButton.Enabled =
                catalog.Branches.Count > 0 &&
                catalog.Products.Count > 0;

            _statusLabel.Text = _addButton.Enabled
                ? "Catalog loaded."
                : "No active branch or products are available.";
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
    }

    private void AddButton_Click(object? sender, EventArgs e)
    {
        if (_productPicker.SelectedItem is not SalesProductDto product)
            return;

        var quantity = (int)_quantityPicker.Value;
        var existingLine = _cart.FirstOrDefault(
            line => line.Product.Id == product.Id);

        if (existingLine is null)
            _cart.Add(new CartLine(product, quantity));
        else
            existingLine.Quantity += quantity;

        RefreshCart();
    }

    private async void CompleteSaleButton_Click(object? sender, EventArgs e)
    {
        if (_branchPicker.SelectedItem is not SalesBranchDto branch ||
            _cart.Count == 0)
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
                    .ToList());

            var sale = await _apiClient.CreateAsync(request);

            _cart.Clear();
            RefreshCart();

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
            _completeSaleButton.Enabled = _cart.Count > 0;
        }
    }

    private void RefreshCart()
    {
        _cartList.Items.Clear();

        foreach (var line in _cart)
        {
            var lineTotal = line.Product.UnitPrice * line.Quantity;
            _cartList.Items.Add(
                $"{line.Product.Name} x {line.Quantity} " +
                $"@ {line.Product.UnitPrice:C2} = {lineTotal:C2}");
        }

        var total = _cart.Sum(
            line => line.Product.UnitPrice * line.Quantity);

        _totalLabel.Text = $"Total: {total:C2}";
        _completeSaleButton.Enabled = _cart.Count > 0;
    }

    private sealed class CartLine(SalesProductDto product, int quantity)
    {
        public SalesProductDto Product { get; } = product;
        public int Quantity { get; set; } = quantity;
    }
}