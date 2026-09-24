using System.Data;
using Microsoft.Data.SqlClient;
using System.Windows;

namespace al_ameer.Features.Customers
{
    public partial class SelectCustomerWindow : Window
    {
        private readonly string conn = al_ameer.Data.DatabaseConfig.ConnectionString;
        public int SelectedID { get; set; }
        public string SelectedName { get; set; } = string.Empty;

        public SelectCustomerWindow()
        {
            InitializeComponent();
            LoadCustomers();
        }

        private void LoadCustomers(string filter = "")
        {
            int? selectedId = (lstCustomers.SelectedItem as DataRowView)?["CustomerID"] as int?;
            using SqlConnection c = new SqlConnection(conn);
            string sql = "SELECT CustomerID, FullName FROM Customers WHERE FullName LIKE @f OR Phone LIKE @f ORDER BY FullName";
            SqlDataAdapter da = new SqlDataAdapter(sql, c);
            da.SelectCommand.Parameters.AddWithValue("@f", $"%{filter}%");
            DataTable dt = new DataTable();
            da.Fill(dt);
            lstCustomers.ItemsSource = dt.DefaultView;
            lstCustomers.SelectedItem = dt.DefaultView.Cast<DataRowView>().FirstOrDefault(row =>
                selectedId.HasValue && (int)row["CustomerID"] == selectedId.Value)
                ?? (dt.DefaultView.Count == 1 ? dt.DefaultView[0] : null);
        }

        private void txtSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => LoadCustomers(txtSearch.Text);
        private void Cancel_Click(object sender, RoutedEventArgs e) => this.Close();
        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (lstCustomers.SelectedItem is DataRowView row)
            {
                SelectedID = (int)row["CustomerID"];
                SelectedName = row["FullName"].ToString() ?? string.Empty;
                this.DialogResult = true;
            }
            else MessageBox.Show("Select a customer first.");
        }
        private void lstCustomers_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => Select_Click(sender, e);
    }
}
