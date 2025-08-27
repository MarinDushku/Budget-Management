using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BudgetManagement.Models;
using BudgetManagement.Shared.Infrastructure;

namespace BudgetManagement.Views.Dialogs
{
    public partial class SpendingDialog : Window
    {
        public Spending? Spending { get; private set; }
        private readonly List<Category> _categories;
        private bool _isPlaceholderActive = true;

        public SpendingDialog(List<Category> categories)
        {
            InitializeComponent();
            _categories = categories ?? throw new ArgumentNullException(nameof(categories));
            DatePicker.SelectedDate = DateTime.Today;
            CategoryComboBox.ItemsSource = _categories;
            SetPlaceholder();
        }

        public SpendingDialog(Spending spending, List<Category> categories) : this(categories)
        {
            Spending = spending;
            if (spending != null)
            {
                DatePicker.SelectedDate = spending.Date;
                DescriptionTextBox.Text = spending.Description;
                SetAmountValue(spending.Amount);
                CategoryComboBox.SelectedValue = spending.CategoryId;
                Title = LocalizationHelper.GetString("EditSpendingTitle", "Edit Spending Entry");
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

            string amountText = _isPlaceholderActive ? "0" : AmountTextBox.Text;
            if (!decimal.TryParse(amountText, out decimal amount) || amount <= 0)
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

        #region Amount Input Helpers

        private void SetPlaceholder()
        {
            _isPlaceholderActive = true;
            AmountTextBox.Text = "0.00";
            AmountTextBox.Foreground = new SolidColorBrush(Color.FromArgb(128, 128, 128, 128)); // Gray placeholder
        }

        private void ClearPlaceholder()
        {
            if (_isPlaceholderActive)
            {
                _isPlaceholderActive = false;
                AmountTextBox.Text = "";
                AmountTextBox.Foreground = new SolidColorBrush(Colors.Black); // Normal text color
            }
        }

        private void SetAmountValue(decimal value)
        {
            _isPlaceholderActive = false;
            AmountTextBox.Text = value.ToString("F2");
            AmountTextBox.Foreground = new SolidColorBrush(Colors.Black);
        }

        private void AmountTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow only digits, decimal point, and ensure only one decimal point
            var regex = new Regex(@"^[0-9]*\.?[0-9]{0,2}$");
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox == null) return;
            var newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);
            
            // Clear placeholder on first character
            if (_isPlaceholderActive)
            {
                ClearPlaceholder();
                newText = e.Text;
            }
            
            e.Handled = !regex.IsMatch(newText) || newText.Count(c => c == '.') > 1;
        }

        private void AmountTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox == null) return;
            
            if (_isPlaceholderActive)
            {
                ClearPlaceholder();
            }
            else
            {
                // Select all text for easy replacement
                textBox.SelectAll();
            }
        }

        private void AmountTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox == null) return;
            
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                SetPlaceholder();
            }
        }

        private void AmountTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Allow backspace, delete, tab, escape, enter
            if (e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Tab || 
                e.Key == Key.Escape || e.Key == Key.Enter)
            {
                return;
            }

            // Allow Ctrl+A, Ctrl+C, Ctrl+V, etc.
            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None)
            {
                return;
            }

            // Block everything else except digits and decimal
            if (!((e.Key >= Key.D0 && e.Key <= Key.D9) || 
                  (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || 
                  e.Key == Key.Decimal || e.Key == Key.OemPeriod))
            {
                e.Handled = true;
            }
        }

        #endregion

        #region Quick Amount Helpers

        private void QuickAmount_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button?.Tag != null && decimal.TryParse(button.Tag.ToString(), out decimal amount))
            {
                SetAmountValue(amount);
            }
        }

        #endregion
    }
}