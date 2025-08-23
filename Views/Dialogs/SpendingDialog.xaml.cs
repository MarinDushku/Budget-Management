using System;
using System.Collections.Generic;
using System.Windows;
using BudgetManagement.Models;

namespace BudgetManagement.Views.Dialogs
{
    public partial class SpendingDialog : Window
    {
        public Spending Spending { get; private set; }
        private readonly List<Category> _categories;

        public SpendingDialog(List<Category> categories)
        {
            InitializeComponent();
            _categories = categories ?? throw new ArgumentNullException(nameof(categories));
            DatePicker.SelectedDate = DateTime.Today;
            CategoryComboBox.ItemsSource = _categories;
        }

        public SpendingDialog(Spending spending, List<Category> categories) : this(categories)
        {
            Spending = spending;
            if (spending != null)
            {
                DatePicker.SelectedDate = spending.Date;
                DescriptionTextBox.Text = spending.Description;
                AmountTextBox.Text = spending.Amount.ToString("F2");
                CategoryComboBox.SelectedValue = spending.CategoryId;
                Title = "Edit Spending Entry";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Simple validation
            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
                MessageBox.Show("Please enter a description.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                DescriptionTextBox.Focus();
                return;
            }

            if (!decimal.TryParse(AmountTextBox.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Please enter a valid amount greater than 0.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                AmountTextBox.Focus();
                return;
            }

            if (DatePicker.SelectedDate == null)
            {
                MessageBox.Show("Please select a date.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                DatePicker.Focus();
                return;
            }

            if (CategoryComboBox.SelectedValue == null)
            {
                MessageBox.Show("Please select a category.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                CategoryComboBox.Focus();
                return;
            }

            // Create or update spending
            if (Spending == null)
            {
                Spending = new Spending();
            }

            Spending.Date = DatePicker.SelectedDate.Value;
            Spending.Description = DescriptionTextBox.Text.Trim();
            Spending.Amount = amount;
            Spending.CategoryId = (int)CategoryComboBox.SelectedValue;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}