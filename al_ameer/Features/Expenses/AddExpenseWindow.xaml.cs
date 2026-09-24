using Microsoft.Data.SqlClient;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace al_ameer.Features.Expenses
{
    public partial class AddExpenseWindow : Window
    {
        private readonly string connString = al_ameer.Data.DatabaseConfig.ConnectionString;

        public AddExpenseWindow()
        {
            InitializeComponent();
        }

        // FIXED: Missing Save Logic
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!al_ameer.Services.InputParser.TryNonNegativeMoney(txtAmount.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Enter an expense amount greater than zero.");
                return;
            }

            using (SqlConnection conn = new SqlConnection(connString))
            {
                string sql = "INSERT INTO Expenses (Category, Amount, Description, ExpenseDate) VALUES (@cat, @amt, @desc, GETDATE())";
                SqlCommand cmd = new SqlCommand(sql, conn);

                // Note: Ensure your XAML ComboBox is named 'cbCategory' and TextBox is 'txtAmount'
                cmd.Parameters.AddWithValue("@cat", (cbCategory.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Other");
                cmd.Parameters.AddWithValue("@amt", amount);
                cmd.Parameters.AddWithValue("@desc", txtDescription.Text);

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    this.DialogResult = true; // Signals the Page to refresh
                    this.Close();
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }

        // FIXED: Missing Cancel Logic
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // FIXED: Missing Drag Logic
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
