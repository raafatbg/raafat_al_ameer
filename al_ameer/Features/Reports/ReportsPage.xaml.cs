using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Linq;

namespace al_ameer.Features.Reports
{
    public partial class ReportsPage : Page
    {
        // Database connection string
        private readonly string connString = al_ameer.Data.DatabaseConfig.ConnectionString;

        public ReportsPage()
        {
            InitializeComponent();
            dpFilter.SelectedDate = DateTime.Now;
            LoadDailyReportData();
        }

        /// <summary>
        /// Logic for the Refresh Button to reload data and reset filter
        /// </summary>
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            dpFilter.SelectedDate = DateTime.Now;
            LoadDailyReportData();
        }

        private void DateChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only reload if the Page is fully initialized to prevent null reference crashes
            if (this.IsLoaded)
            {
                LoadDailyReportData();
            }
        }

        /// <summary>
        /// Fetches sales and expenses from DB and calculates totals
        /// </summary>
        private void LoadDailyReportData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    // SQL logic: Full Outer Join ensures we see dates with ONLY sales or ONLY expenses
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
                            (SELECT Date, SUM(Amount) as TotalExp FROM
                                (SELECT CAST(ExpenseDate as DATE) as Date, Amount FROM Expenses
                                 UNION ALL
                                 SELECT EntryDate as Date, Amount FROM EmployeeSalaryEntries WHERE EntryType = 'Payment') outflows
                             GROUP BY Date) e
                        ON s.Date = e.Date
                        ORDER BY ReportDate DESC";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    // Handle Date Filtering
                    DataView dv = dt.DefaultView;
                    if (dpFilter.SelectedDate.HasValue)
                    {
                        DateTime filterDate = dpFilter.SelectedDate.Value.Date;
                        // Filters the view to show only the selected day
                        dv.RowFilter = $"ReportDate = '{filterDate:yyyy-MM-dd}'";
                    }

                    // Update the Dark UI Grid
                    dgProfitReport.ItemsSource = dv;

                    // Recalculate KPI Summaries based on the filtered results
                    decimal totalRev = 0;
                    decimal totalExp = 0;

                    foreach (DataRowView row in dv)
                    {
                        totalRev += Convert.ToDecimal(row["DailySales"]);
                        totalExp += Convert.ToDecimal(row["DailyExpenses"]);
                    }

                    lblTotalRevenue.Text = $"{totalRev:N0} LBP";
                    lblTotalExpenses.Text = $"{totalExp:N0} LBP";
                    lblNetProfit.Text = $"{(totalRev - totalExp):N0} LBP";

                    // Highlight Net Profit color if negative
                    lblNetProfit.Foreground = (totalRev - totalExp) >= 0 ? Brushes.White : new SolidColorBrush(Color.FromRgb(239, 68, 68));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Report Error: " + ex.Message, "Database Connectivity Issue");
            }
        }

        /// <summary>
        /// Generates a FlowDocument for printing the current analytics view
        /// </summary>
        private void PrintReport_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                FlowDocument doc = new FlowDocument();
                doc.PagePadding = new Thickness(50);
                doc.Background = Brushes.White;
                doc.FontFamily = new FontFamily("Segoe UI");

                Paragraph title = new Paragraph(new Run("AL AMEER TIRES - PERFORMANCE REPORT"))
                {
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 30)
                };
                doc.Blocks.Add(title);

                doc.Blocks.Add(new Paragraph(new Run($"Report Date: {dpFilter.SelectedDate:dd/MM/yyyy}")));
                doc.Blocks.Add(new Paragraph(new Run($"Generated On: {DateTime.Now:dd/MM/yyyy HH:mm}")));

                // Professional Summary Box
                Section summary = new Section()
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(0, 1, 0, 1),
                    Padding = new Thickness(0, 20, 0, 20),
                    Margin = new Thickness(0, 20, 0, 40)
                };
                summary.Blocks.Add(new Paragraph(new Run($"TOTAL REVENUE: {lblTotalRevenue.Text}")));
                summary.Blocks.Add(new Paragraph(new Run($"TOTAL EXPENSES: {lblTotalExpenses.Text}")));
                summary.Blocks.Add(new Paragraph(new Run($"NET PROFIT: {lblNetProfit.Text}")) { FontSize = 18, FontWeight = FontWeights.Bold });
                doc.Blocks.Add(summary);

                doc.Blocks.Add(new Paragraph(new Run("Thank you for using Al Ameer POS System.")) { FontStyle = FontStyles.Italic, TextAlignment = TextAlignment.Center });

                pd.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Financial Report Print");
            }
        }
    }
}
