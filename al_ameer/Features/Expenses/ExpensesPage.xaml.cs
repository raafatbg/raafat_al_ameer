using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace al_ameer.Features.Expenses
{
    public partial class ExpensesPage : Page
    {
        private readonly string connString = "Server=DESKTOP-TVOR3BK;Database=al_ameer;Trusted_Connection=True;TrustServerCertificate=True;";

        public ExpensesPage()
        {
            InitializeComponent();
            LoadExpenses();
        }

        // Method to load data into the DataGrid
        public void LoadExpenses()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    string query = "SELECT ExpenseDate, Category, Description, Amount FROM Expenses ORDER BY ExpenseDate DESC";
                    SqlDataAdapter da = new SqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgExpenses.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading expenses: " + ex.Message);
            }
        }

        // FIXED: The missing method that your XAML is looking for
        private void AddExpense_Click(object sender, RoutedEventArgs e)
        {
            // We create the window
            AddExpenseWindow win = new AddExpenseWindow();

            // Set the owner so it centers correctly over the app
            win.Owner = Window.GetWindow(this);

            // Show as a popup and refresh the list if saved successfully
            if (win.ShowDialog() == true)
            {
                LoadExpenses();
            }
        }
    }
}