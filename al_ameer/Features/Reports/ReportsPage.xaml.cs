using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace al_ameer.Features.Reports
{
    public partial class ReportsPage : Page
    {
        private readonly string connString = "Server=DESKTOP-TVOR3BK;Database=al_ameer;Trusted_Connection=True;TrustServerCertificate=True;";

        public ReportsPage()
        {
            InitializeComponent();
            dpFilter.SelectedDate = DateTime.Now;
            LoadDailyReportData();
        }

        private void DateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpFilter != null)
            {
                LoadDailyReportData();
            }
        }

        private void LoadDailyReportData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    // FIXED QUERY: Uses GrandTotal and groups strictly by Date
                    string query = @"
                        SELECT 
                            ISNULL(s.Date, e.Date) as ReportDate,
                            ISNULL(s.TotalSales, 0) as DailySales,
                            ISNULL(e.TotalExp, 0) as DailyExpenses,
                            (ISNULL(s.TotalSales, 0) - ISNULL(e.TotalExp, 0)) as NetDaily
                        FROM 
                            (SELECT CAST(SaleDate as DATE) as Date, SUM(GrandTotal) as TotalSales 
                             FROM Sales GROUP BY CAST(SaleDate as DATE)) s
                        FULL OUTER JOIN 
                            (SELECT CAST(ExpenseDate as DATE) as Date, SUM(Amount) as TotalExp 
                             FROM Expenses GROUP BY CAST(ExpenseDate as DATE)) e
                        ON s.Date = e.Date
                        ORDER BY ReportDate DESC";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    // Apply Date Filtering if a date is selected
                    DataView dv = dt.DefaultView;
                    if (dpFilter.SelectedDate.HasValue)
                    {
                        DateTime filterDate = dpFilter.SelectedDate.Value.Date;
                        dv.RowFilter = $"ReportDate = '{filterDate:yyyy-MM-dd}'";
                    }

                    dgProfitReport.ItemsSource = dv;

                    // Calculate KPIs based on the filtered view
                    decimal rev = 0, exp = 0;
                    foreach (DataRowView row in dv)
                    {
                        rev += Convert.ToDecimal(row["DailySales"]);
                        exp += Convert.ToDecimal(row["DailyExpenses"]);
                    }

                    lblTotalRevenue.Text = $"{rev:N0} LBP";
                    lblTotalExpenses.Text = $"{exp:N0} LBP";
                    lblNetProfit.Text = $"{(rev - exp):N0} LBP";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading daily tracker: " + ex.Message, "Al Ameer Tires");
            }
        }

        private void PrintReport_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                FlowDocument doc = new FlowDocument();
                doc.PagePadding = new Thickness(50);
                doc.Background = Brushes.White;

                Paragraph title = new Paragraph(new Run("AL AMEER TIRES - DAILY PERFORMANCE REPORT"))
                { FontSize = 22, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 30) };
                doc.Blocks.Add(title);

                doc.Blocks.Add(new Paragraph(new Run($"Printed On: {DateTime.Now:dd/MM/yyyy HH:mm}")));

                Section summary = new Section() { BorderBrush = Brushes.Black, BorderThickness = new Thickness(0, 1, 0, 1), Padding = new Thickness(0, 20, 0, 20), Margin = new Thickness(0, 20, 0, 40) };
                summary.Blocks.Add(new Paragraph(new Run($"TOTAL REVENUE: {lblTotalRevenue.Text}")));
                summary.Blocks.Add(new Paragraph(new Run($"TOTAL EXPENSES: {lblTotalExpenses.Text}")));
                summary.Blocks.Add(new Paragraph(new Run($"NET PROFIT: {lblNetProfit.Text}")) { FontSize = 18, FontWeight = FontWeights.Bold });
                doc.Blocks.Add(summary);

                pd.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Daily Financial Report");
            }
        }
    }
}