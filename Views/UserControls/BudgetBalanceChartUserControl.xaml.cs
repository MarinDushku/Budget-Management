using System;
using System.Collections.ObjectModel;
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
    /// Chart control for displaying daily budget balance over time
    /// </summary>
    public partial class BudgetBalanceChartUserControl : UserControl
    {
        private IThemeService? _themeService;

        public BudgetBalanceChartUserControl()
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

        // Budget balance data property
        public static readonly DependencyProperty BudgetBalanceDataProperty =
            DependencyProperty.Register("BudgetBalanceData", typeof(ObservableCollection<DailyBudgetBalance>), 
                typeof(BudgetBalanceChartUserControl), new PropertyMetadata(new ObservableCollection<DailyBudgetBalance>(), OnBudgetBalanceDataChanged));

        public ObservableCollection<DailyBudgetBalance> BudgetBalanceData
        {
            get { return (ObservableCollection<DailyBudgetBalance>)GetValue(BudgetBalanceDataProperty); }
            set { SetValue(BudgetBalanceDataProperty, value); }
        }

        // Line chart model property
        public static readonly DependencyProperty LineChartModelProperty =
            DependencyProperty.Register("LineChartModel", typeof(PlotModel), typeof(BudgetBalanceChartUserControl), new PropertyMetadata(null));

        public PlotModel LineChartModel
        {
            get { return (PlotModel)GetValue(LineChartModelProperty); }
            set { SetValue(LineChartModelProperty, value); }
        }

        // Best day balance property
        public static readonly DependencyProperty BestDayBalanceProperty =
            DependencyProperty.Register("BestDayBalance", typeof(decimal), typeof(BudgetBalanceChartUserControl), new PropertyMetadata(0m));

        public decimal BestDayBalance
        {
            get { return (decimal)GetValue(BestDayBalanceProperty); }
            set { SetValue(BestDayBalanceProperty, value); }
        }

        // Worst day balance property
        public static readonly DependencyProperty WorstDayBalanceProperty =
            DependencyProperty.Register("WorstDayBalance", typeof(decimal), typeof(BudgetBalanceChartUserControl), new PropertyMetadata(0m));

        public decimal WorstDayBalance
        {
            get { return (decimal)GetValue(WorstDayBalanceProperty); }
            set { SetValue(WorstDayBalanceProperty, value); }
        }

        // Current balance property
        public static readonly DependencyProperty CurrentBalanceProperty =
            DependencyProperty.Register("CurrentBalance", typeof(decimal), typeof(BudgetBalanceChartUserControl), new PropertyMetadata(0m));

        public decimal CurrentBalance
        {
            get { return (decimal)GetValue(CurrentBalanceProperty); }
            set { SetValue(CurrentBalanceProperty, value); }
        }

        // Current balance color property
        public static readonly DependencyProperty CurrentBalanceColorProperty =
            DependencyProperty.Register("CurrentBalanceColor", typeof(SolidColorBrush), typeof(BudgetBalanceChartUserControl), new PropertyMetadata(new SolidColorBrush(Colors.Blue)));

        public SolidColorBrush CurrentBalanceColor
        {
            get { return (SolidColorBrush)GetValue(CurrentBalanceColorProperty); }
            set { SetValue(CurrentBalanceColorProperty, value); }
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

            // Add zero line for reference
            LineChartModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Key = "ZeroLine",
                IsAxisVisible = false,
                Minimum = 0,
                Maximum = 0
            });

            var zeroLine = new LineSeries
            {
                Color = themeColors.ZeroLineColor,
                StrokeThickness = 1,
                LineStyle = LineStyle.Dash,
                Title = "Break Even"
            };
            LineChartModel.Series.Add(zeroLine);
        }

        private static void OnBudgetBalanceDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BudgetBalanceChartUserControl control)
            {
                // Unsubscribe from old collection
                if (e.OldValue is ObservableCollection<DailyBudgetBalance> oldCollection)
                {
                    oldCollection.CollectionChanged -= control.OnDataCollectionChanged;
                }
                
                // Subscribe to new collection
                if (e.NewValue is ObservableCollection<DailyBudgetBalance> newCollection)
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
            System.Diagnostics.Debug.WriteLine($"BudgetBalanceChartUserControl.UpdateChart: Starting with {BudgetBalanceData?.Count ?? 0} data points");
            
            if (BudgetBalanceData == null || !BudgetBalanceData.Any())
            {
                System.Diagnostics.Debug.WriteLine("BudgetBalanceChartUserControl.UpdateChart: No data available, clearing chart");
                LineChartModel?.Series.Clear();
                LineChartModel?.InvalidatePlot(true);
                return;
            }

            var sortedData = BudgetBalanceData.OrderBy(d => d.Date).ToList();

            // Clear existing series (except zero line)
            LineChartModel?.Series.Clear();

            // Add zero line for reference  
            var themeColors = GetThemeAwareChartColors();
            var zeroLine = new LineSeries
            {
                Color = themeColors.ZeroLineColor,
                StrokeThickness = 1,
                LineStyle = LineStyle.Dash
            };

            // Create cumulative balance line series
            var cumulativeLineSeries = new LineSeries
            {
                Color = themeColors.CumulativeLineColor,
                StrokeThickness = 3,
                MarkerType = MarkerType.Circle,
                MarkerSize = 4,
                MarkerStroke = themeColors.CumulativeLineColor,
                MarkerFill = themeColors.MarkerFillColor,
                Title = "Cumulative Balance"
            };

            // Create daily balance line series (positive/negative)
            var positiveSeries = new LineSeries
            {
                Color = themeColors.PositiveColor,
                StrokeThickness = 2,
                MarkerType = MarkerType.Circle,
                MarkerSize = 3,
                MarkerStroke = themeColors.PositiveColor,
                MarkerFill = themeColors.PositiveColor,
                Title = "Positive Days"
            };

            var negativeSeries = new LineSeries
            {
                Color = themeColors.NegativeColor,
                StrokeThickness = 2,
                MarkerType = MarkerType.Circle,
                MarkerSize = 3,
                MarkerStroke = themeColors.NegativeColor,
                MarkerFill = themeColors.NegativeColor,
                Title = "Negative Days"
            };

            foreach (var data in sortedData)
            {
                var dateTime = DateTime.SpecifyKind(data.Date, DateTimeKind.Unspecified);
                var dateValue = DateTimeAxis.ToDouble(dateTime);

                // Add cumulative balance line point
                cumulativeLineSeries.Points.Add(new DataPoint(dateValue, (double)data.CumulativeBalance));

                // Add daily balance points (positive or negative)
                if (data.DailyBalance >= 0)
                {
                    positiveSeries.Points.Add(new DataPoint(dateValue, (double)data.DailyBalance));
                }
                else
                {
                    negativeSeries.Points.Add(new DataPoint(dateValue, (double)data.DailyBalance));
                }
            }

            // Calculate statistics
            var dailyBalances = sortedData.Select(d => d.DailyBalance).ToList();
            BestDayBalance = dailyBalances.Any() ? dailyBalances.Max() : 0m;
            WorstDayBalance = dailyBalances.Any() ? dailyBalances.Min() : 0m;
            CurrentBalance = sortedData.LastOrDefault()?.CumulativeBalance ?? 0m;

            // Set current balance color using theme-aware colors
            if (CurrentBalance >= 0)
            {
                CurrentBalanceColor = new SolidColorBrush(Color.FromRgb(themeColors.PositiveColor.R, themeColors.PositiveColor.G, themeColors.PositiveColor.B));
            }
            else
            {
                CurrentBalanceColor = new SolidColorBrush(Color.FromRgb(themeColors.NegativeColor.R, themeColors.NegativeColor.G, themeColors.NegativeColor.B));
            }

            // Add zero line first (background)
            LineChartModel?.Series.Add(zeroLine);
            
            // Add daily balance series (background)
            if (positiveSeries.Points.Any())
                LineChartModel?.Series.Add(positiveSeries);
            if (negativeSeries.Points.Any())
                LineChartModel?.Series.Add(negativeSeries);
                
            // Add cumulative line series (foreground)
            LineChartModel?.Series.Add(cumulativeLineSeries);

            LineChartModel?.InvalidatePlot(true);
        }

        private BudgetBalanceChartColors GetThemeAwareChartColors()
        {
            var isDarkTheme = _themeService?.IsDarkTheme ?? false;

            if (isDarkTheme)
            {
                return new BudgetBalanceChartColors
                {
                    BorderColor = OxyColor.FromRgb(96, 96, 96),      // Lighter border for dark theme
                    AxisTextColor = OxyColor.FromRgb(200, 200, 200), // Light text for dark theme
                    GridColor = OxyColor.FromRgb(64, 64, 64),        // Subtle grid for dark theme
                    CumulativeLineColor = OxyColor.FromRgb(66, 165, 245), // Light blue for dark theme
                    PositiveColor = OxyColor.FromRgb(102, 187, 106),  // Light green for dark theme
                    NegativeColor = OxyColor.FromRgb(239, 83, 80),    // Light red for dark theme
                    ZeroLineColor = OxyColor.FromRgb(158, 158, 158),  // Light gray for dark theme
                    MarkerFillColor = OxyColor.FromRgb(30, 30, 30)    // Dark marker fill
                };
            }
            else
            {
                return new BudgetBalanceChartColors
                {
                    BorderColor = OxyColors.LightGray,               // Standard border for light theme
                    AxisTextColor = OxyColor.FromRgb(102, 102, 102), // Standard text color
                    GridColor = OxyColor.FromRgb(220, 220, 220),     // Light grid color
                    CumulativeLineColor = OxyColor.FromRgb(33, 150, 243), // Standard blue
                    PositiveColor = OxyColor.FromRgb(76, 175, 80),    // Standard green
                    NegativeColor = OxyColor.FromRgb(244, 67, 54),    // Standard red
                    ZeroLineColor = OxyColors.Gray,                   // Standard gray
                    MarkerFillColor = OxyColors.White                 // White marker fill
                };
            }
        }
    }

    /// <summary>
    /// Theme-aware color configuration for budget balance charts
    /// </summary>
    public class BudgetBalanceChartColors
    {
        public OxyColor BorderColor { get; set; }
        public OxyColor AxisTextColor { get; set; }
        public OxyColor GridColor { get; set; }
        public OxyColor CumulativeLineColor { get; set; }
        public OxyColor PositiveColor { get; set; }
        public OxyColor NegativeColor { get; set; }
        public OxyColor ZeroLineColor { get; set; }
        public OxyColor MarkerFillColor { get; set; }
    }
}