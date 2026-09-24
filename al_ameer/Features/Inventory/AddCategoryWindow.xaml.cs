using al_ameer.Data;
using al_ameer.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Input;

namespace al_ameer.Features.Inventory;

public partial class AddCategoryWindow : Window
{
    public AddCategoryWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => txtName.Focus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        string name = txtName.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Enter a category name.", "Category", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            using var db = new AppDbContext();
            if (db.Categories.Any(x => x.CategoryName.ToLower() == name.ToLower()))
            {
                MessageBox.Show("That category already exists.", "Category", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            db.Categories.Add(new Category { CategoryName = name, Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim() });
            db.SaveChanges();
            DialogResult = true;
        }
        catch (DbUpdateException ex)
        {
            Exception root = ex;
            while (root.InnerException != null) root = root.InnerException;
            MessageBox.Show("Could not add category: " + root.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    private void Window_MouseDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
}
