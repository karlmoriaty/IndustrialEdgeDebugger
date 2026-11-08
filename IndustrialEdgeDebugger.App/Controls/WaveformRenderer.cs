using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using IndustrialEdgeDebugger.App.Models;
using IndustrialEdgeDebugger.App.ViewModels;

namespace IndustrialEdgeDebugger.App.Controls;

public class WaveformRenderer : Control
{
    public static readonly DependencyProperty ChannelsProperty =
        DependencyProperty.Register(nameof(Channels), typeof(ObservableCollection<WaveformChannelViewModel>), typeof(WaveformRenderer),
            new PropertyMetadata(null, OnChannelsChanged));

    public ObservableCollection<WaveformChannelViewModel> Channels
    {
        get => (ObservableCollection<WaveformChannelViewModel>)GetValue(ChannelsProperty);
        set => SetValue(ChannelsProperty, value);
    }

    private double _offsetX = 0;
    private double _offsetY = 0;
    private double _scaleX = 1;
    private double _scaleY = 1;
    private bool _isDragging = false;
    private Point _lastMousePos;
    private double _minY = -1;
    private double _maxY = 1;
    private double _minX = 0;
    private double _maxX = 1000;
    private bool _showGrid = true;

    static WaveformRenderer()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(WaveformRenderer),
            new FrameworkPropertyMetadata(typeof(WaveformRenderer)));
    }

    public WaveformRenderer()
    {
        Channels = new ObservableCollection<WaveformChannelViewModel>();
        Background = Brushes.Black;
        ClipToBounds = true;
        Focusable = true;

        CompositionTarget.Rendering += OnRendering;
    }

    private static void OnChannelsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WaveformRenderer renderer)
        {
            if (e.OldValue is ObservableCollection<WaveformChannelViewModel> oldCollection)
            {
                oldCollection.CollectionChanged -= renderer.OnChannelsCollectionChanged;
            }
            if (e.NewValue is ObservableCollection<WaveformChannelViewModel> newCollection)
            {
                newCollection.CollectionChanged += renderer.OnChannelsCollectionChanged;
            }
        }
    }

    private void OnChannelsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        var pos = e.GetPosition(this);
        var delta = e.Delta > 0 ? 1.1 : 0.9;

        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            var centerY = (pos.Y - _offsetY) / _scaleY;
            _scaleY *= delta;
            _offsetY = pos.Y - centerY * _scaleY;
        }
        else
        {
            var centerX = (pos.X - _offsetX) / _scaleX;
            _scaleX *= delta;
            _offsetX = pos.X - centerX * _scaleX;
        }

        e.Handled = true;
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            _isDragging = true;
            _lastMousePos = e.GetPosition(this);
            CaptureMouse();
            e.Handled = true;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
        {
            var currentPos = e.GetPosition(this);
            var delta = currentPos - _lastMousePos;
            _offsetX += delta.X;
            _offsetY += delta.Y;
            _lastMousePos = currentPos;
            e.Handled = true;
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (_isDragging)
        {
            _isDragging = false;
            ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (Channels == null) return;

        var width = ActualWidth;
        var height = ActualHeight;

        drawingContext.DrawRectangle(Background, null, new Rect(0, 0, width, height));

        if (_showGrid)
        {
            DrawGrid(drawingContext, width, height);
        }

        foreach (var channel in Channels)
        {
            if (!channel.IsVisible) continue;
            DrawWaveform(drawingContext, channel, width, height);
        }
    }

    private void DrawGrid(DrawingContext dc, double width, double height)
    {
        var pen = new Pen(Brushes.DarkGray, 0.5);
        pen.Freeze();

        for (double x = 0; x < width; x += 50)
        {
            var screenX = x * _scaleX + _offsetX;
            if (screenX >= 0 && screenX <= width)
            {
                dc.DrawLine(pen, new Point(screenX, 0), new Point(screenX, height));
            }
        }

        for (double y = 0; y < height; y += 50)
        {
            var screenY = y * _scaleY + _offsetY;
            if (screenY >= 0 && screenY <= height)
            {
                dc.DrawLine(pen, new Point(0, screenY), new Point(width, screenY));
            }
        }

        var axisPen = new Pen(Brushes.Gray, 1);
        axisPen.Freeze();

        var zeroY = TransformY(0, height);
        if (zeroY >= 0 && zeroY <= height)
        {
            dc.DrawLine(axisPen, new Point(0, zeroY), new Point(width, zeroY));
        }
    }

    private void DrawWaveform(DrawingContext dc, WaveformChannelViewModel channel, double width, double height)
    {
        var buffer = channel.GetBufferSnapshot();
        if (buffer.Length == 0) return;

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            bool first = true;
            for (int i = 0; i < buffer.Length; i++)
            {
                var point = buffer[i];
                if (point.X == 0 && point.Y == 0 && i > 0) continue;

                var screenPoint = TransformPoint(point, width, height);
                if (first)
                {
                    context.BeginFigure(screenPoint, false, false);
                    first = false;
                }
                else
                {
                    context.LineTo(screenPoint, true, true);
                }
            }
        }
        geometry.Freeze();

        var pen = new Pen(new SolidColorBrush(channel.Color), 1.5);
        pen.Freeze();
        dc.DrawGeometry(null, pen, geometry);
    }

    private Point TransformPoint(WaveformDataPoint point, double width, double height)
    {
        var x = (point.X - _minX) / (_maxX - _minX) * width;
        var y = height - (point.Y - _minY) / (_maxY - _minY) * height;
        return new Point(x * _scaleX + _offsetX, y * _scaleY + _offsetY);
    }

    private double TransformY(double y, double height)
    {
        var normalizedY = (y - _minY) / (_maxY - _minY);
        return (height - normalizedY * height) * _scaleY + _offsetY;
    }

    public void SetYRange(double min, double max)
    {
        _minY = min;
        _maxY = max;
        InvalidateVisual();
    }

    public void SetXRange(double min, double max)
    {
        _minX = min;
        _maxX = max;
        InvalidateVisual();
    }

    public void ResetView()
    {
        _offsetX = 0;
        _offsetY = 0;
        _scaleX = 1;
        _scaleY = 1;
        InvalidateVisual();
    }
}
