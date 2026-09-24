using al_ameer.Data;
using al_ameer.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace al_ameer.Features.Inventory;

public partial class InventoryPage : Page
{
    private List<Product> _products = [];
    private List<Category> _categories = [];
    private int? _openCategoryId;

    public InventoryPage()
    {
        InitializeComponent();
        LoadInventory();
    }

    public void LoadInventory()
    {
        try
        {
            using var db = new AppDbContext();
            _categories = db.Categories.AsNoTracking().OrderBy(c => c.CategoryName).ToList();
            _products = db.Products.Include(p => p.Category).Include(p => p.Supplier).AsNoTracking().ToList();
            RefreshView();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Database Error: {ex.Message}", "Inventory", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshView()
    {
        string search = txtSearch.Text.Trim();
        if (_openCategoryId is int categoryId)
        {
            Category? category = _categories.FirstOrDefault(c => c.CategoryId == categoryId);
            if (category == null) { _openCategoryId = null; RefreshView(); return; }
            rootHeading.Visibility = Visibility.Collapsed;
            categoryHeading.Visibility = Visibility.Visible;
            folderView.Visibility = Visibility.Collapsed;
            productView.Visibility = Visibility.Visible;
            txtCategoryTitle.Text = category.CategoryName;
            var matches = _products.Where(p => p.CategoryId == categoryId && MatchesProduct(p, search))
                .OrderBy(p => p.ProductName).ToList();
            dgInventory.ItemsSource = matches;
            txtProductCount.Text = $"{matches.Count} product{(matches.Count == 1 ? "" : "s")}";
            emptyCategoryMessage.Visibility = matches.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            rootHeading.Visibility = Visibility.Visible;
            categoryHeading.Visibility = Visibility.Collapsed;
            folderView.Visibility = Visibility.Visible;
            productView.Visibility = Visibility.Collapsed;
            categoryFolders.ItemsSource = _categories.Where(c =>
                c.CategoryName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                _products.Any(p => p.CategoryId == c.CategoryId && MatchesProduct(p, search)))
                .Select(c => new CategoryFolder(c.CategoryId, c.CategoryName,
                    _products.Count(p => p.CategoryId == c.CategoryId))).ToList();
        }
    }

    private static bool MatchesProduct(Product product, string search) =>
        string.IsNullOrWhiteSpace(search) ||
        product.ProductName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
        (product.Barcode?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
        (product.Supplier?.CompanyName.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false);

    private void OpenCategory_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int id })
        {
            _openCategoryId = id;
            txtSearch.Clear();
            RefreshView();
        }
    }

    private void BackToCategories_Click(object sender, RoutedEventArgs e)
    {
        _openCategoryId = null;
        txtSearch.Clear();
        RefreshView();
    }

    private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (categoryFolders != null) RefreshView();
    }

    private void AddProduct_Click(object sender, RoutedEventArgs e)
    {
        if (new AddProductWindow { Owner = Window.GetWindow(this) }.ShowDialog() == true) LoadInventory();
    }

    private void AddCategory_Click(object sender, RoutedEventArgs e)
    {
        if (new AddCategoryWindow { Owner = Window.GetWindow(this) }.ShowDialog() == true) LoadInventory();
    }

    private void EditProduct_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: Product product } &&
            new EditProductWindow(product) { Owner = Window.GetWindow(this) }.ShowDialog() == true)
            LoadInventory();
    }

    private void DeleteProduct_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: Product product }) return;
        if (MessageBox.Show($"Delete {product.ProductName}?", "Confirm", MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        try
        {
            using var db = new AppDbContext();
            bool referenced = db.SaleItems.Any(x => x.ProductId == product.ProductId);
            using (var connection = new SqlConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                using var purchaseCheck = new SqlCommand("SELECT COUNT(*) FROM PurchaseItems WHERE ProductId=@id", connection);
                purchaseCheck.Parameters.AddWithValue("@id", product.ProductId);
                referenced |= Convert.ToInt32(purchaseCheck.ExecuteScalar()) > 0;
            }
            if (referenced)
            {
                var persisted = db.Products.Find(product.ProductId);
                if (persisted != null) persisted.IsActive = false;
                db.SaveChanges();
                MessageBox.Show("This product has sales or purchase history, so it was deactivated instead of deleted.");
            }
            else
            {
                db.Products.Remove(db.Products.Find(product.ProductId) ?? product);
                db.SaveChanges();
            }
            LoadInventory();
        }
        catch (Exception ex) { MessageBox.Show("Delete failed: " + ex.Message); }
    }

    private sealed record CategoryFolder(int CategoryId, string CategoryName, int ProductCount)
    {
        public string ProductSummary => $"{ProductCount} product{(ProductCount == 1 ? "" : "s")}";
    }
}
