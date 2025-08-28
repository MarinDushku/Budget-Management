// Income Search Control - Advanced Income Search Interface
// File: Views/UserControls/IncomeSearchControl.xaml.cs

using System.Windows;
using System.Windows.Controls;

namespace BudgetManagement.Views.UserControls
{
    /// <summary>
    /// User control for advanced income search functionality
    /// </summary>
    public partial class IncomeSearchControl : UserControl
    {
        public IncomeSearchControl()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        /// <summary>
        /// Search ViewModel property
        /// </summary>
        public static readonly DependencyProperty SearchViewModelProperty =
            DependencyProperty.Register("SearchViewModel", typeof(object), typeof(IncomeSearchControl), new PropertyMetadata(null));

        public object SearchViewModel
        {
            get { return GetValue(SearchViewModelProperty); }
            set { SetValue(SearchViewModelProperty, value); }
        }

        #endregion
    }
}