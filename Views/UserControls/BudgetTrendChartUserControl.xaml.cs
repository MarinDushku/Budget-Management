using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BudgetManagement.Models;
using BudgetManagement.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace BudgetManagement.Views.UserControls
{
    /// <summary>
    /// Chart control for displaying budget trend over time
    /// </summary>
    public partial class BudgetTrendChartUserControl : UserControl
    {
        private IThemeService? _themeService;

        public BudgetTrendChartUserControl()
        {
            InitializeComponent();
            
            // Get theme service from application resources or DI container
            if (Application.Current?.Resources.Contains("ThemeService") == true)
            {
                _themeService = (IThemeService)Application.Current.Resources["ThemeService"];
                _themeService.ThemeChanged += OnThemeChanged;
            }
            
            InitializeChart();
        }

        #region Dependency Properties

        // Budget trend data property
        public static readonly DependencyProperty BudgetTrendDataProperty =
            DependencyProperty.Register("BudgetTrendData", typeof(ObservableCollection<WeeklyBudgetData>), 
                typeof(BudgetTrendChartUserControl), new PropertyMetadata(new ObservableCollection<WeeklyBudgetData>(), OnBudgetTrendDataChanged));

        public ObservableCollection<WeeklyBudgetData> BudgetTrendData
        {
            get { return (ObservableCollection<WeeklyBudgetData>)GetValue(BudgetTrendDataProperty); }
            set { SetValue(BudgetTrendDataProperty, value); }
        }

        // Line chart model property
        public static readonly DependencyProperty LineChartModelProperty =
            DependencyProperty.Register("LineChartModel", typeof(PlotModel), typeof(BudgetTrendChartUserControl), new PropertyMetadata(null));

        public PlotModel LineChartModel
        {
            get { return (PlotModel)GetValue(LineChartModelProperty); }
            set { SetValue(LineChartModelProperty, value); }
        }

        // Best week amount property
        public static readonly DependencyProperty BestWeekAmountProperty =
            DependencyProperty.Register("BestWeekAmount", typeof(decimal), typeof(BudgetTrendChartUserControl), new PropertyMetadata(0m));

        public decimal BestWeekAmount
        {
            get { return (decimal)GetValue(BestWeekAmountProperty); }
            set { SetValue(BestWeekAmountProperty, value); }
        }

        // Worst week amount property
        public static readonly DependencyProperty WorstWeekAmountProperty =
            DependencyProperty.Register("WorstWeekAmount", typeof(decimal), typeof(BudgetTrendChartUserControl), new PropertyMetadata(0m));

        public decimal WorstWeekAmount
        {
            get { return (decimal)GetValue(WorstWeekAmountProperty); }
            set { SetValue(WorstWeekAmountProperty, value); }
        }

        // Trend indicator property
        public static readonly DependencyProperty TrendIndicatorProperty =
            DependencyProperty.Register("TrendIndicator", typeof(string), typeof(BudgetTrendChartUserControl), new PropertyMetadata("⚪"));

        public string TrendIndicator
        {
            get { return (string)GetValue(TrendIndicatorProperty); }
            set { SetValue(TrendIndicatorProperty, value); }
        }

        // Trend percentage property
        public static readonly DependencyProperty TrendPercentageProperty =
            DependencyProperty.Register("TrendPercentage", typeof(double), typeof(BudgetTrendChartUserControl), new PropertyMetadata(0.0));

        public double TrendPercentage
        {
            get { return (double)GetValue(TrendPercentageProperty); }
            set { SetValue(TrendPercentageProperty, value); }
        }

        // Trend color property
        public static readonly DependencyProperty TrendColorProperty =
            DependencyProperty.Register("TrendColor", typeof(SolidColorBrush), typeof(BudgetTrendChartUserControl), new PropertyMetadata(new SolidColorBrush(Colors.Blue)));

        public SolidColorBrush TrendColor
        {
            get { return (SolidColorBrush)GetValue(TrendColorProperty); }
            set { SetValue(TrendColorProperty, value); }
        }

        #endregion

        private void InitializeChart()
        {
            var themeColors = GetThemeAwareChartColors();
            
            LineChartModel = new PlotModel
            {
                Background = OxyColors.Transparent,
                PlotAreaBorderColor = themeColors.BorderColor,
                PlotAreaBorderThickness = new OxyThickness(1),
                Padding = new OxyThickness(10, 5, 10, 5)
            };

            // Add axes with theme-aware colors
            LineChartModel.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "MM/dd",
                Title = "",
                FontSize = 10,
                TextColor = themeColors.AxisTextColor,
                TickStyle = TickStyle.Outside,
                MajorGridlineStyle = LineStyle.None
            });

            LineChartModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "",
                FontSize = 10,
                TextColor = themeColors.AxisTextColor,
                StringFormat = "C0",
                TickStyle = TickStyle.Outside,
                MajorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = themeColors.GridColor
            });
        }

        private static void OnBudgetTrendDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BudgetTrendChartUserControl control)
            {
                // Unsubscribe from old collection
                if (e.OldValue is ObservableCollection<WeeklyBudgetData> oldCollection)
                {
                    oldCollection.CollectionChanged -= control.OnDataCollectionChanged;
                }
                
                // Subscribe to new collection
                if (e.NewValue is ObservableCollection<WeeklyBudgetData> newCollection)
                {
                    newCollection.CollectionChanged += control.OnDataCollectionChanged;
                }
                
                control.UpdateChart();
            }
        }

        private void OnDataCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateChart();
        }

        private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            // Refresh chart with new theme colors
            InitializeChart();
            UpdateChart();
        }

        private void UpdateChart()
        {
            System.Diagnostics.Debug.WriteLine($"BudgetTrendChartUserControl.UpdateChart: Starting with {BudgetTrendData?.Count ?? 0} data points");
            
            if (BudgetTrendData == null || !BudgetTrendData.Any())
            {
                System.Diagnostics.Debug.WriteLine("BudgetTrendChartUserControl.UpdateChart: No data available, clearing chart");
                LineChartModel?.Series.Clear();
                LineChartModel?.InvalidatePlot(true);
                return;
            }

            var sortedData = BudgetTrendData.OrderBy(d => d.WeekStartDate).ToList();

            // Create line series with theme-aware colors
            var themeColors = GetThemeAwareChartColors();
            var lineSeries = new LineSeries
            {
                Color = themeColors.PrimaryLineColor,
                StrokeThickness = 2,
                MarkerType = MarkerType.Circle,
                MarkerSize = 4,
                MarkerStroke = themeColors.PrimaryLineColor,
                MarkerFill = themeColors.MarkerFillColor
            };

            foreach (var data in sortedData)
            {
                var dateTime = DateTime.SpecifyKind(data.WeekStartDate, DateTimeKind.Unspecified);
                lineSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(dateTime), (double)data.RemainingBudget));
            }

            // Calculate statistics
            var budgetValues = sortedData.Select(d => d.RemainingBudget).ToList();
            BestWeekAmount = budgetValues.Any() ? budgetValues.Max() : 0m;
            WorstWeekAmount = budgetValues.Any() ? budgetValues.Min() : 0m;

            // Calculate trend
            CalculateTrend(sortedData);

            // Update chart
            LineChartModel?.Series.Clear();
            LineChartModel?.Series.Add(lineSeries);
            LineChartModel?.InvalidatePlot(true);
        }

        private void CalculateTrend(System.Collections.Generic.List<WeeklyBudgetData> sortedData)
        {
            if (sortedData.Count < 2)
            {
                TrendIndicator = "⚪";
                TrendPercentage = 0.0;
                TrendColor = new SolidColorBrush(Colors.Gray);
                return;
            }

            var firstValue = sortedData.First().RemainingBudget;
            var lastValue = sortedData.Last().RemainingBudget;

            if (firstValue == 0)
            {
                TrendIndicator = "⚪";
                TrendPercentage = 0.0;
                TrendColor = new SolidColorBrush(Colors.Gray);
                return;
            }

            var changePercent = (double)((lastValue - firstValue) / Math.Abs(firstValue));
            TrendPercentage = Math.Abs(changePercent);

            if (changePercent > 0.05) // More than 5% improvement
            {
                TrendIndicator = "📈 ↗️";
                TrendColor = new SolidColorBrush(Colors.Green);
            }
            else if (changePercent < -0.05) // More than 5% decline
            {
                TrendIndicator = "📉 ↘️";
                TrendColor = new SolidColorBrush(Colors.Red);
            }
            else // Relatively stable
            {
                TrendIndicator = "➡️";
                TrendColor = new SolidColorBrush(Colors.Blue);
            }
        }

        private BudgetTrendChartColors GetThemeAwareChartColors()
        {
            var isDarkTheme = _themeService?.IsDarkTheme ?? false;

            if (isDarkTheme)
            {
                return new BudgetTrendChartColors
                {
                    BorderColor = OxyColor.FromRgb(96, 96, 96),      // Lighter border for dark theme
                    AxisTextColor = OxyColor.FromRgb(200, 200, 200), // Light text for dark theme
                    GridColor = OxyColor.FromRgb(64, 64, 64),        // Subtle grid for dark theme
                    PrimaryLineColor = OxyColor.FromRgb(66, 165, 245), // Brighter blue for dark theme
                    MarkerFillColor = OxyColor.FromRgb(30, 30, 30)   // Dark marker fill
                };
            }
            else
            {
                return new BudgetTrendChartColors
                {
                    BorderColor = OxyColors.LightGray,               // Standard border for light theme
                    AxisTextColor = OxyColor.FromRgb(102, 102, 102), // Standard text color
                    GridColor = OxyColor.FromRgb(220, 220, 220),     // Light grid color
                    PrimaryLineColor = OxyColor.FromRgb(33, 150, 243), // Standard blue
                    MarkerFillColor = OxyColors.White                // White marker fill
                };
            }
        }
    }

    /// <summary>
    /// Theme-aware color configuration for budget trend charts
    /// </summary>
    public class BudgetTrendChartColors
    {
        public OxyColor BorderColor { get; set; }
        public OxyColor AxisTextColor { get; set; }
        public OxyColor GridColor { get; set; }
        public OxyColor PrimaryLineColor { get; set; }
        public OxyColor MarkerFillColor { get; set; }
    }
}