// Spending Search Control - Advanced Spending Search Interface
// File: Views/UserControls/SpendingSearchControl.xaml.cs

using System.Windows;
using System.Windows.Controls;

namespace BudgetManagement.Views.UserControls
{
    /// <summary>
    /// User control for advanced spending search functionality
    /// </summary>
    public partial class SpendingSearchControl : UserControl
    {
        public SpendingSearchControl()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        /// <summary>
        /// Search ViewModel property
        /// </summary>
        public static readonly DependencyProperty SearchViewModelProperty =
            DependencyProperty.Register("SearchViewModel", typeof(object), typeof(SpendingSearchControl), new PropertyMetadata(null));

        public object SearchViewModel
        {
            get { return GetValue(SearchViewModelProperty); }
            set { SetValue(SearchViewModelProperty, value); }
        }

        #endregion
    }
}