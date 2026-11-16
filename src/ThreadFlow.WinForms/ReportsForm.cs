using ThreadFlow.WinForms.ApiClient;

namespace ThreadFlow.WinForms;

public sealed class ReportsForm : Form
{
    private readonly SalesReportsApiClient _apiClient = new();

    private readonly DateTimePicker _fromDate = new();
    private readonly DateTimePicker _toDate = new();
    private readonly Button _loadButton = new();
    private readonly Label _saleCount = new();
    private readonly Label _unitsSold = new();
    private readonly Label _totalSales = new();
    private readonly Label _status = new();

    public ReportsForm()
    {
        Text = "ThreadFlow | Operational Reports";
        ClientSize = new Size(620, 360);
        StartPosition = FormStartPosition.CenterParent;

        BuildForm();
    }

    private void BuildForm()
    {
        var fromLabel = new Label
        {
            Text = "From date",
            Location = new Point(30, 30),
            AutoSize = true
        };

        _fromDate.Format = DateTimePickerFormat.Short;
        _fromDate.Value = DateTime.Today;
        _fromDate.Location = new Point(120, 25);
        _fromDate.Width = 150;

        var toLabel = new Label
        {
            Text = "To date",
            Location = new Point(310, 30),
            AutoSize = true
        };

        _toDate.Format = DateTimePickerFormat.Short;
        _toDate.Value = DateTime.Today;
        _toDate.Location = new Point(380, 25);
        _toDate.Width = 150;

        _loadButton.Text = "Load Report";
        _loadButton.Location = new Point(30, 80);
        _loadButton.Width = 130;
        _loadButton.Height = 38;
        _loadButton.Click += LoadButton_Click;

        _saleCount.Text = "Sales: -";
        _saleCount.Location = new Point(30, 150);
        _saleCount.AutoSize = true;

        _unitsSold.Text = "Units sold: -";
        _unitsSold.Location = new Point(30, 190);
        _unitsSold.AutoSize = true;

        _totalSales.Text = "Total sales: -";
        _totalSales.Location = new Point(30, 230);
        _totalSales.AutoSize = true;

        _status.Location = new Point(30, 290);
        _status.AutoSize = true;

        Controls.AddRange(new Control[]
        {
            fromLabel, _fromDate,
            toLabel, _toDate,
            _loadButton,
            _saleCount, _unitsSold, _totalSales,
            _status
        });
    }

    private async void LoadButton_Click(object? sender, EventArgs e)
    {
        var fromLocal = DateTime.SpecifyKind(
            _fromDate.Value.Date,
            DateTimeKind.Local);

        var toLocalExclusive = DateTime.SpecifyKind(
            _toDate.Value.Date.AddDays(1),
            DateTimeKind.Local);

        if (fromLocal >= toLocalExclusive)
        {
            _status.Text = "The start date must not be after the end date.";
            return;
        }

        _loadButton.Enabled = false;
        _status.Text = "Loading report...";

        try
        {
            var report = await _apiClient.GetSalesReportAsync(
                new DateTimeOffset(fromLocal),
                new DateTimeOffset(toLocalExclusive));

            _saleCount.Text = $"Sales: {report.SaleCount}";
            _unitsSold.Text = $"Units sold: {report.UnitsSold}";
            _totalSales.Text = $"Total sales: {report.TotalSalesAmount:C2}";
            _status.Text = "Report loaded.";
        }
        catch (HttpRequestException exception)
        {
            _status.Text = "Could not load the report.";
            MessageBox.Show(
                exception.Message,
                "API request failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _loadButton.Enabled = true;
        }
    }
}