using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Linq;
using WpfSize = System.Windows.Size;
using WpfPoint = System.Windows.Point;
using WpfColor = System.Windows.Media.Color;
using WpfBrushes = System.Windows.Media.Brushes;

namespace NxDataManager.Controls;

/// <summary>
/// 简单的柱状图控件
/// </summary>
public class SimpleBarChart : System.Windows.Controls.Control
{
    static SimpleBarChart()
    {
        System.Windows.FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(
            typeof(SimpleBarChart),
            new FrameworkPropertyMetadata(typeof(SimpleBarChart)));
    }

    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(List<ChartDataPoint>), typeof(SimpleBarChart),
            new PropertyMetadata(null, OnDataChanged));

    public List<ChartDataPoint> Data
    {
        get => (List<ChartDataPoint>)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var chart = d as SimpleBarChart;
        chart?.DrawChart();
    }

    private Canvas? _canvas;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _canvas = GetTemplateChild("PART_Canvas") as Canvas;
        DrawChart();
    }

    private void DrawChart()
    {
        if (_canvas == null || Data == null || Data.Count == 0)
            return;

        _canvas.Children.Clear();

        var maxValue = Data.Max(d => d.Value);
        var barWidth = _canvas.ActualWidth / Data.Count;
        var padding = 10.0;
        var availableWidth = barWidth - padding;

        for (int i = 0; i < Data.Count; i++)
        {
            var dataPoint = Data[i];
            var barHeight = (dataPoint.Value / maxValue) * (_canvas.ActualHeight - 40);

            // 绘制柱子
            var bar = new System.Windows.Shapes.Rectangle
            {
                Width = availableWidth,
                Height = barHeight,
                Fill = new SolidColorBrush(dataPoint.Color),
                RadiusX = 4,
                RadiusY = 4
            };

            Canvas.SetLeft(bar, i * barWidth + padding / 2);
            Canvas.SetBottom(bar, 20);
            _canvas.Children.Add(bar);

            // 添加值标签
            var valueText = new TextBlock
            {
                Text = dataPoint.Value.ToString("N0"),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = WpfBrushes.White
            };

            valueText.Measure(new WpfSize(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(valueText, i * barWidth + (barWidth - valueText.DesiredSize.Width) / 2);
            Canvas.SetBottom(valueText, barHeight + 22);
            _canvas.Children.Add(valueText);

            // 添加标签
            var labelText = new TextBlock
            {
                Text = dataPoint.Label,
                FontSize = 10,
                Foreground = WpfBrushes.Gray,
                TextAlignment = TextAlignment.Center,
                Width = barWidth
            };

            Canvas.SetLeft(labelText, i * barWidth);
            Canvas.SetBottom(labelText, 2);
            _canvas.Children.Add(labelText);
        }
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        DrawChart();
    }
}

/// <summary>
/// 简单的饼图控件
/// </summary>
public class SimplePieChart : System.Windows.Controls.Control
{
    static SimplePieChart()
    {
        System.Windows.FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(
            typeof(SimplePieChart),
            new FrameworkPropertyMetadata(typeof(SimplePieChart)));
    }

    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(List<ChartDataPoint>), typeof(SimplePieChart),
            new PropertyMetadata(null, OnDataChanged));

    public List<ChartDataPoint> Data
    {
        get => (List<ChartDataPoint>)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var chart = d as SimplePieChart;
        chart?.DrawChart();
    }

    private Canvas? _canvas;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _canvas = GetTemplateChild("PART_Canvas") as Canvas;
        DrawChart();
    }

    private void DrawChart()
    {
        if (_canvas == null || Data == null || Data.Count == 0)
            return;

        _canvas.Children.Clear();

        var total = Data.Sum(d => d.Value);
        var centerX = _canvas.ActualWidth / 2;
        var centerY = _canvas.ActualHeight / 2;
        var radius = Math.Min(centerX, centerY) - 20;

        double currentAngle = -90;

        foreach (var dataPoint in Data)
        {
            var percentage = dataPoint.Value / total;
            var sweepAngle = 360 * percentage;

            var path = new Path
            {
                Fill = new SolidColorBrush(dataPoint.Color),
                Stroke = WpfBrushes.White,
                StrokeThickness = 2
            };

            var figure = new PathFigure { StartPoint = new WpfPoint(centerX, centerY) };

            var startAngleRad = currentAngle * Math.PI / 180;
            var endAngleRad = (currentAngle + sweepAngle) * Math.PI / 180;

            var startPoint = new WpfPoint(
                centerX + radius * Math.Cos(startAngleRad),
                centerY + radius * Math.Sin(startAngleRad)
            );

            var endPoint = new WpfPoint(
                centerX + radius * Math.Cos(endAngleRad),
                centerY + radius * Math.Sin(endAngleRad)
            );

            figure.Segments.Add(new LineSegment(startPoint, true));
            figure.Segments.Add(new ArcSegment(
                endPoint,
                new WpfSize(radius, radius),
                0,
                sweepAngle > 180,
                SweepDirection.Clockwise,
                true
            ));
            figure.Segments.Add(new LineSegment(new WpfPoint(centerX, centerY), true));

            path.Data = new PathGeometry { Figures = { figure } };
            _canvas.Children.Add(path);

            // 添加百分比标签
            var labelAngle = currentAngle + sweepAngle / 2;
            var labelAngleRad = labelAngle * Math.PI / 180;
            var labelDistance = radius * 0.7;

            var label = new TextBlock
            {
                Text = $"{percentage:P0}",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = WpfBrushes.White
            };

            label.Measure(new WpfSize(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(label, centerX + labelDistance * Math.Cos(labelAngleRad) - label.DesiredSize.Width / 2);
            Canvas.SetTop(label, centerY + labelDistance * Math.Sin(labelAngleRad) - label.DesiredSize.Height / 2);
            _canvas.Children.Add(label);

            currentAngle += sweepAngle;
        }
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        DrawChart();
    }
}

/// <summary>
/// 图表数据点
/// </summary>
public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public WpfColor Color { get; set; } = Colors.Blue;
}
