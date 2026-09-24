using al_ameer.Services;
using System.Windows;
using System.Windows.Controls;

namespace al_ameer.Features.Inventory;

public partial class StockAdjustmentWindow : Window
{
    private readonly InventoryAdjustmentService _adjustments = new();

    public StockAdjustmentWindow()
    {
        InitializeComponent();
        try { cbProduct.ItemsSource = _adjustments.GetProducts(); }
        catch (Exception ex) { MessageBox.Show("Products could not load: " + ex.Message); }
    }

    private void Product_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (txtCurrentStock != null)
            txtCurrentStock.Text = cbProduct.SelectedItem is StockProduct product
                ? $"Current stock: {product.StockQuantity:N0}" : "Select a product";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (cbProduct.SelectedItem is not StockProduct product)
        { MessageBox.Show("Select a product."); return; }
        if (!int.TryParse(txtQuantity.Text.Trim(), out int quantity) || quantity <= 0)
        { MessageBox.Show("Enter a whole-number quantity greater than zero."); return; }
        if (string.IsNullOrWhiteSpace(txtReason.Text))
        { MessageBox.Show("Enter a reason for this adjustment."); return; }
        int change = rbSubtract.IsChecked == true ? -quantity : quantity;
        string operation = change > 0 ? "Add" : "Remove";
        if (MessageBox.Show($"{operation} {quantity:N0} of {product.ProductName}?\nReason: {txtReason.Text.Trim()}",
            "Confirm stock adjustment", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            StockAdjustmentResult result = _adjustments.Adjust(product.ProductId, change, txtReason.Text);
            MessageBox.Show($"Stock updated: {result.StockBefore:N0} → {result.StockAfter:N0}.\nAdjustment #{result.AdjustmentId} saved.");
            DialogResult = true;
        }
        catch (Exception ex) { MessageBox.Show("Stock adjustment failed: " + ex.Message); }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
