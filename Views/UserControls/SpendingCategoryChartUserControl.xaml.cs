using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BudgetManagement.Models;
using OxyPlot;
using OxyPlot.Series;

namespace BudgetManagement.Views.UserControls
{
    /// <summary>
    /// Chart control for displaying spending breakdown by category
    /// </summary>
    public partial class SpendingCategoryChartUserControl : UserControl
    {
        public SpendingCategoryChartUserControl()
        {
            InitializeComponent();
            InitializeChart();
        }

        #region Dependency Properties

        // Spending entries property
        public static readonly DependencyProperty SpendingEntriesProperty =
            DependencyProperty.Register("SpendingEntries", typeof(ObservableCollection<SpendingWithCategory>), 
                typeof(SpendingCategoryChartUserControl), new PropertyMetadata(new ObservableCollection<SpendingWithCategory>(), OnSpendingEntriesChanged));

        public ObservableCollection<SpendingWithCategory> SpendingEntries
        {
            get { return (ObservableCollection<SpendingWithCategory>)GetValue(SpendingEntriesProperty); }
            set { SetValue(SpendingEntriesProperty, value); }
        }

        // Pie chart model property
        public static readonly DependencyProperty PieChartModelProperty =
            DependencyProperty.Register("PieChartModel", typeof(PlotModel), typeof(SpendingCategoryChartUserControl), new PropertyMetadata(null));

        public PlotModel PieChartModel
        {
            get { return (PlotModel)GetValue(PieChartModelProperty); }
            set { SetValue(PieChartModelProperty, value); }
        }

        // Category summaries property
        public static readonly DependencyProperty CategorySummariesProperty =
            DependencyProperty.Register("CategorySummaries", typeof(ObservableCollection<CategorySummary>), 
                typeof(SpendingCategoryChartUserControl), new PropertyMetadata(new ObservableCollection<CategorySummary>()));

        public ObservableCollection<CategorySummary> CategorySummaries
        {
            get { return (ObservableCollection<CategorySummary>)GetValue(CategorySummariesProperty); }
            set { SetValue(CategorySummariesProperty, value); }
        }

        #endregion

        private void InitializeChart()
        {
            PieChartModel = new PlotModel
            {
                Background = OxyColors.Transparent,
                PlotAreaBorderColor = OxyColors.Transparent
            };
        }

        private static void OnSpendingEntriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SpendingCategoryChartUserControl control)
            {
                control.UpdateChart();
            }
        }

        private void UpdateChart()
        {
            if (SpendingEntries == null || !SpendingEntries.Any())
            {
                PieChartModel?.Series.Clear();
                CategorySummaries?.Clear();
                PieChartModel?.InvalidatePlot(true);
                return;
            }

            // Group by category and calculate totals
            var categoryTotals = SpendingEntries
                .GroupBy(s => new { s.CategoryId, s.CategoryName })
                .Select(g => new
                {
                    CategoryName = g.Key.CategoryName,
                    Amount = g.Sum(s => s.Amount),
                    Count = g.Count()
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            var totalAmount = categoryTotals.Sum(c => c.Amount);

            // Create pie series
            var pieSeries = new PieSeries
            {
                StrokeThickness = 1.0,
                InsideLabelPosition = 0.8,
                AngleSpan = 360,
                StartAngle = 0,
                Stroke = OxyColors.White,
                FontSize = 10
            };

            // Color palette for categories
            var colors = new[]
            {
                OxyColor.FromRgb(76, 175, 80),   // Green
                OxyColor.FromRgb(33, 150, 243),  // Blue
                OxyColor.FromRgb(255, 152, 0),   // Orange
                OxyColor.FromRgb(233, 30, 99),   // Pink
                OxyColor.FromRgb(156, 39, 176),  // Purple
                OxyColor.FromRgb(255, 193, 7),   // Amber
                OxyColor.FromRgb(96, 125, 139),  // Blue Grey
                OxyColor.FromRgb(121, 85, 72),   // Brown
                OxyColor.FromRgb(158, 158, 158), // Grey
                OxyColor.FromRgb(255, 87, 34)    // Deep Orange
            };

            CategorySummaries?.Clear();
            if (CategorySummaries == null)
                CategorySummaries = new ObservableCollection<CategorySummary>();

            for (int i = 0; i < categoryTotals.Count && i < 10; i++)
            {
                var category = categoryTotals[i];
                var color = colors[i % colors.Length];
                var percentage = totalAmount > 0 ? (double)(category.Amount / totalAmount) : 0;

                // Add to pie chart
                pieSeries.Slices.Add(new PieSlice(category.CategoryName, (double)category.Amount)
                {
                    IsExploded = false,
                    Fill = color
                });

                // Add to summary list
                CategorySummaries.Add(new CategorySummary
                {
                    CategoryName = category.CategoryName,
                    Amount = category.Amount,
                    Percentage = percentage,
                    Color = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B))
                });
            }

            // Update chart
            PieChartModel?.Series.Clear();
            PieChartModel?.Series.Add(pieSeries);
            PieChartModel?.InvalidatePlot(true);
        }
    }

    /// <summary>
    /// Summary data for category display
    /// </summary>
    public class CategorySummary
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public double Percentage { get; set; }
        public SolidColorBrush Color { get; set; } = new SolidColorBrush(Colors.Gray);
    }
}