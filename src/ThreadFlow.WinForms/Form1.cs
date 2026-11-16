using ThreadFlow.WinForms.ApiClient;

namespace ThreadFlow.WinForms
{
    public partial class Form1 : Form
    {
        private readonly CustomerInteractionsApiClient _apiClient = new();

        private readonly ComboBox _interactionType = new();
        private readonly TextBox _subject = new();
        private readonly TextBox _details = new();
        private readonly Button _submitButton = new();
        private readonly Label _status = new();
        private readonly Button _openSalesButton = new();
        private readonly Button _openReportsButton = new();

        public Form1()
        {
            InitializeComponent();
            BuildForm();
            Theme.ThreadFlowTheme.Apply(this);
        }

        private void BuildForm()
        {
            Text = "ThreadFlow | Customer Service";
            ClientSize = new Size(900, 620);
            StartPosition = FormStartPosition.CenterScreen;

            var typeLabel = new Label
            {
                Text = "Interaction type",
                Location = new Point(32, 28),
                AutoSize = true
            };

            _interactionType.Items.AddRange(
                new object[] { "Inquiry", "Complaint", "Feedback" });
            _interactionType.DropDownStyle = ComboBoxStyle.DropDownList;
            _interactionType.SelectedIndex = 0;
            _interactionType.Location = new Point(32, 54);
            _interactionType.Width = 800;

            var subjectLabel = new Label
            {
                Text = "Subject",
                Location = new Point(32, 105),
                AutoSize = true
            };

            _subject.Location = new Point(32, 131);
            _subject.Width = 800;
            _subject.MaxLength = 160;

            var detailsLabel = new Label
            {
                Text = "Details",
                Location = new Point(32, 182),
                AutoSize = true
            };

            _details.Location = new Point(32, 208);
            _details.Width = 800;
            _details.Height = 180;
            _details.Multiline = true;
            _details.MaxLength = 4000;
            _details.ScrollBars = ScrollBars.Vertical;

            _submitButton.Text = "Submit";
            _submitButton.Location = new Point(32, 414);
            _submitButton.Width = 140;
            _submitButton.Height = 40;
            _submitButton.Click += SubmitButton_Click;

            _status.Location = new Point(32, 472);
            _status.AutoSize = true;

            _openSalesButton.Text = "Open Sales";
            _openSalesButton.Location = new Point(190, 414);
            _openSalesButton.Width = 140;
            _openSalesButton.Height = 40;
            _openSalesButton.Click += OpenSalesButton_Click;

            _openReportsButton.Text = "Open Reports";
            _openReportsButton.Location = new Point(350, 414);
            _openReportsButton.Width = 140;
            _openReportsButton.Height = 40;
            _openReportsButton.Click += OpenReportsButton_Click;

            Controls.AddRange(new Control[]
            {
                typeLabel, _interactionType,
                subjectLabel, _subject,
                detailsLabel, _details,
                _submitButton, _openSalesButton, _openReportsButton, _status
            });
        }

        private void OpenSalesButton_Click(object? sender, EventArgs e)
        {
            using var salesForm = new SalesForm();
            salesForm.ShowDialog(this);
        }

        private void OpenReportsButton_Click(object? sender, EventArgs e)
        {
            using var reportsForm = new ReportsForm();
            reportsForm.ShowDialog(this);
        }

        private async void SubmitButton_Click(object? sender, EventArgs e)
        {
            _submitButton.Enabled = false;
            _status.Text = "Submitting...";

            try
            {
                var request = new CreateCustomerInteractionRequest(
                    CustomerId: null,
                    Type: _interactionType.SelectedItem?.ToString() ?? "Inquiry",
                    Subject: _subject.Text,
                    Details: _details.Text);

                var created = await _apiClient.CreateAsync(request);

                _status.Text = $"Saved as {created.Type} ({created.Status}).";
                _subject.Clear();
                _details.Clear();
            }
            catch (HttpRequestException exception)
            {
                _status.Text = "Could not submit the interaction.";
                MessageBox.Show(
                    exception.Message,
                    "API request failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _submitButton.Enabled = true;
            }
        }
    }
}
