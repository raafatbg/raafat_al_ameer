using System.Data;
using Microsoft.Data.SqlClient;
using System.Windows;

namespace al_ameer.Features.Customers
{
    public partial class SelectCustomerWindow : Window
    {
        private string conn = "Server=DESKTOP-TVOR3BK;Database=al_ameer;Trusted_Connection=True;TrustServerCertificate=True;";
        public int SelectedID { get; set; }
        public string SelectedName { get; set; }

        public SelectCustomerWindow()
        {
            InitializeComponent();
            LoadCustomers();
        }

        private void LoadCustomers(string filter = "")
        {
            using SqlConnection c = new SqlConnection(conn);
            string sql = "SELECT CustomerID, FullName FROM Customers WHERE FullName LIKE @f";
            SqlDataAdapter da = new SqlDataAdapter(sql, c);
            da.SelectCommand.Parameters.AddWithValue("@f", $"%{filter}%");
            DataTable dt = new DataTable();
            da.Fill(dt);
            lstCustomers.ItemsSource = dt.DefaultView;
        }

        private void txtSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => LoadCustomers(txtSearch.Text);
        private void Cancel_Click(object sender, RoutedEventArgs e) => this.Close();
        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (lstCustomers.SelectedItem is DataRowView row)
            {
                SelectedID = (int)row["CustomerID"];
                SelectedName = row["FullName"].ToString();
                this.DialogResult = true;
            }
        }
        private void lstCustomers_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => Select_Click(null, null);
    }
}