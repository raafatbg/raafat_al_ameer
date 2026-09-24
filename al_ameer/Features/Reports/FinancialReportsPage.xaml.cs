using al_ameer.Services;
using System.Windows;
using System.Windows.Controls;

namespace al_ameer.Features.Reports;

public partial class FinancialReportsPage : Page
{
    private readonly FinancialReportService _reports = new();
    public FinancialReportsPage()
    {
        InitializeComponent();
        dpFrom.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        dpThrough.SelectedDate = DateTime.Today;
        LoadReports();
    }
    private void Refresh_Click(object sender, RoutedEventArgs e) => LoadReports();
    private void LoadReports()
    {
        if (dpFrom.SelectedDate is not DateTime from || dpThrough.SelectedDate is not DateTime through)
        { MessageBox.Show("Choose both dates."); return; }
        try
        {
            var cash = _reports.GetCashFlow(from, through);
            dgCash.ItemsSource = cash;
            txtCashSummary.Text = AppSettings.Current.Language == "ar"
                ? $"المقبوضات: {cash.Sum(x => x.Inflow):N0} ل.ل.  ·  صافي النقد التشغيلي: {cash.Sum(x => x.Net):N0} ل.ل."
                : $"Receipts: {cash.Sum(x => x.Inflow):N0} LBP  ·  Net operating cash: {cash.Sum(x => x.Net):N0} LBP";
            dgAging.ItemsSource = _reports.GetCustomerAging(DateTime.Today);
            dgSalary.ItemsSource = _reports.GetSalaryBalances();
        }
        catch (Exception ex) { MessageBox.Show("Reports could not load: " + ex.Message); }
    }
}
