using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace al_ameer.Features.Expenses
{
    public partial class ExpensesPage : Page
    {
        private readonly string connString = al_ameer.Data.DatabaseConfig.ConnectionString;

        public ExpensesPage()
        {
            InitializeComponent();
            LoadExpenses();
        }

        public void LoadExpenses(string filter = "")
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    // FIXED Query: Includes ExpenseID for the delete button to work
                    string query = "SELECT ExpenseID, ExpenseDate, Category, Description, Amount FROM Expenses WHERE ReversedAt IS NULL";

                    if (!string.IsNullOrEmpty(filter))
                        query += " AND (Category LIKE @f OR Description LIKE @f)";

                    query += " ORDER BY ExpenseDate DESC";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    if (!string.IsNullOrEmpty(filter))
                        cmd.Parameters.AddWithValue("@f", $"%{filter}%");

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
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

        private void AddExpense_Click(object sender, RoutedEventArgs e)
        {
            AddExpenseWindow win = new AddExpenseWindow { Owner = Window.GetWindow(this) };
            if (win.ShowDialog() == true)
            {
                LoadExpenses();
            }
        }

        private void DeleteExpense_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                if (MessageBox.Show("Are you sure you want to delete this expense?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(connString))
                        {
                            string query = "DELETE FROM Expenses WHERE ExpenseID = @id AND SalaryEntryId IS NULL";
                            SqlCommand cmd = new SqlCommand(query, conn);
                            cmd.Parameters.AddWithValue("@id", btn.Tag);
                            conn.Open();
                            if (cmd.ExecuteNonQuery() != 1)
                            {
                                MessageBox.Show("Salary expenses are linked to employee payments. Reverse the salary payment in Employees instead.");
                                return;
                            }
                            LoadExpenses(txtSearch.Text);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Delete Error: " + ex.Message);
                    }
                }
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadExpenses(txtSearch.Text.Trim());
        }
    }
}
