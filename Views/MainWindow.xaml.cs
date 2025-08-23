using System.Windows;
using System.Windows.Media;
using BudgetManagement.ViewModels;

namespace BudgetManagement.Views
{
    /// <summary>
    /// Main window for the Budget Management application
    /// Designed with senior-friendly interface principles
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Set up senior-friendly window behavior
            InitializeSeniorFriendlyFeatures();
        }

        public MainWindow(MainViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private void InitializeSeniorFriendlyFeatures()
        {
            // Ensure the window is properly sized for seniors
            if (SystemParameters.PrimaryScreenWidth < 1400)
            {
                Width = SystemParameters.PrimaryScreenWidth * 0.9;
            }
            
            if (SystemParameters.PrimaryScreenHeight < 900)
            {
                Height = SystemParameters.PrimaryScreenHeight * 0.9;
            }

            // Center the window
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Enable high DPI awareness for better text clarity
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize the view model when the window loads
            if (DataContext is MainViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Could add confirmation dialog here if needed
            // For seniors, it's often better to prevent accidental closes
        }

        // Keyboard shortcuts for senior accessibility
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (DataContext is not MainViewModel viewModel) return;

            // F5 - Refresh (common and intuitive)
            if (e.Key == System.Windows.Input.Key.F5)
            {
                if (viewModel.RefreshCommand.CanExecute(null))
                    viewModel.RefreshCommand.Execute(null);
                e.Handled = true;
            }
            
            // Ctrl+N - New Income (Add Income)
            if (e.Key == System.Windows.Input.Key.N && 
                (e.KeyboardDevice.Modifiers & System.Windows.Input.ModifierKeys.Control) != 0)
            {
                if (viewModel.AddIncomeCommand.CanExecute(null))
                    viewModel.AddIncomeCommand.Execute(null);
                e.Handled = true;
            }
            
            // Ctrl+E - New Expense (Add Spending)
            if (e.Key == System.Windows.Input.Key.E && 
                (e.KeyboardDevice.Modifiers & System.Windows.Input.ModifierKeys.Control) != 0)
            {
                if (viewModel.AddSpendingCommand.CanExecute(null))
                    viewModel.AddSpendingCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}