using al_ameer.Services;
using System.Windows;
using System.Windows.Controls;

namespace al_ameer.Features.Customers;

public partial class CustomerLedgerPage : Page
{
    private readonly CustomerLedgerService _ledger = new();
    private IReadOnlyList<CustomerBalance> _customers = [];
    private int? _selectedCustomerId;
    private int? _selectedSaleId;
    private bool _updatingSelection;

    public CustomerLedgerPage()
    {
        InitializeComponent();
        dpPaymentDate.SelectedDate = DateTime.Today;
        LoadCustomers();
    }

    private void LoadCustomers()
    {
        try
        {
            _customers = _ledger.GetBalances();
            FilterCustomers();
        }
        catch (Exception ex) { MessageBox.Show("Customer accounts could not load: " + ex.Message); }
    }

    private void FilterCustomers()
    {
        string search = txtCustomerSearch.Text.Trim();
        var visible = _customers.Where(c =>
            c.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            c.Phone.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        CustomerBalance? selected = visible.FirstOrDefault(c => c.CustomerId == _selectedCustomerId);
        if (selected is null && visible.Count == 1) selected = visible[0];
        _updatingSelection = true;
        dgCustomers.ItemsSource = visible;
        dgCustomers.SelectedItem = selected;
        _updatingSelection = false;
        if (selected is not null) ShowCustomer(selected);
        else ClearCustomerSelection();
    }

    private void CustomerSearch_Changed(object sender, TextChangedEventArgs e)
    {
        if (dgCustomers != null) FilterCustomers();
    }

    private void CustomerSelection_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingSelection) return;
        if (dgCustomers.SelectedItem is not CustomerBalance customer)
        { ClearCustomerSelection(); return; }
        ShowCustomer(customer);
    }

    private void ShowCustomer(CustomerBalance customer)
    {
        if (_selectedCustomerId != customer.CustomerId) _selectedSaleId = null;
        _selectedCustomerId = customer.CustomerId;
        txtSelectedCustomer.Text = customer.FullName;
        txtBalance.Text = AppSettings.Current.Language == "ar"
            ? $"الرصيد المستحق: {customer.Balance:N2} ل.ل." : $"Outstanding: {customer.Balance:N2} LBP";
        try
        {
            var invoices = _ledger.GetInvoices(customer.CustomerId);
            _updatingSelection = true;
            dgInvoices.ItemsSource = invoices;
            dgInvoices.SelectedItem = invoices.FirstOrDefault(i => i.SaleId == _selectedSaleId && i.RemainingBalance > 0)
                ?? invoices.FirstOrDefault(i => i.RemainingBalance > 0);
            _updatingSelection = false;
            UpdateInvoiceSelection();
            dgPayments.ItemsSource = _ledger.GetPayments(customer.CustomerId);
        }
        catch (Exception ex) { _updatingSelection = false; MessageBox.Show("Customer ledger could not load: " + ex.Message); }
    }

    private void ClearCustomerSelection()
    {
        _selectedCustomerId = null;
        _selectedSaleId = null;
        txtSelectedCustomer.Text = UiLanguage.Translate("Select a customer");
        txtBalance.Text = AppSettings.Current.Language == "ar" ? "الرصيد المستحق: ٠ ل.ل." : "Outstanding: 0 LBP";
        dgInvoices.ItemsSource = null;
        dgPayments.ItemsSource = null;
        UpdateInvoiceSelection();
    }

    private void UpdateInvoiceSelection()
    {
        if (dgInvoices.SelectedItem is CustomerInvoice invoice)
        {
            _selectedSaleId = invoice.SaleId;
            txtInvoiceSelection.Text = AppSettings.Current.Language == "ar"
                ? $"الفاتورة المحددة: {invoice.InvoiceNumber} · المتبقي {invoice.RemainingBalance:N0} ل.ل."
                : $"Selected invoice: {invoice.InvoiceNumber} · {invoice.RemainingBalance:N0} LBP due";
            btnRecordPayment.IsEnabled = invoice.RemainingBalance > 0;
        }
        else
        {
            _selectedSaleId = null;
            txtInvoiceSelection.Text = AppSettings.Current.Language == "ar"
                ? "اختر فاتورة لها رصيد مستحق لتسجيل دفعة."
                : "Select an invoice with an outstanding balance to record a payment.";
            btnRecordPayment.IsEnabled = false;
        }
    }

    private void InvoiceSelection_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_updatingSelection) UpdateInvoiceSelection();
    }

    private void RecordPayment_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCustomerId is not int customerId || dgInvoices.SelectedItem is not CustomerInvoice invoice)
        {
            MessageBox.Show("Select a customer and an invoice first.");
            return;
        }
        if (!InputParser.TryNonNegativeMoney(txtPaymentAmount.Text, out decimal amount) || amount <= 0 ||
            dpPaymentDate.SelectedDate is not DateTime paymentDate)
        {
            MessageBox.Show("Enter a payment amount greater than zero and a payment date.");
            return;
        }
        string method = (cbMethod.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Cash";
        try
        {
            _ledger.RecordPayment(customerId, invoice.SaleId, amount, paymentDate, method, txtPaymentNotes.Text);
            txtPaymentAmount.Clear();
            txtPaymentNotes.Clear();
            LoadCustomers();
        }
        catch (Exception ex) { MessageBox.Show("Payment was not recorded: " + ex.Message); }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => LoadCustomers();

    private void ReversePayment_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCustomerId is not int customerId || dgPayments.SelectedItem is not CustomerPayment payment)
        { MessageBox.Show("Select a payment first."); return; }
        if (payment.CustomerPaymentId <= 0 || payment.ReversedAt is not null)
        { MessageBox.Show("Only active, separately recorded payments can be reversed."); return; }
        string? reason = ReversalPrompt.Ask(Window.GetWindow(this), "Reverse customer payment");
        if (reason is null) return;
        if (MessageBox.Show("Reverse this payment and restore the invoice balance?", "Confirm",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { _ledger.ReversePayment(customerId, payment.CustomerPaymentId, reason); LoadCustomers(); }
        catch (Exception ex) { MessageBox.Show("Reversal failed: " + ex.Message); }
    }
}
