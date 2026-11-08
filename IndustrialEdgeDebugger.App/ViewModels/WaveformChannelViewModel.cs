using System.Collections.Concurrent;
using System.Windows.Media;
using IndustrialEdgeDebugger.App.Models;

namespace IndustrialEdgeDebugger.App.ViewModels;

public class WaveformChannelViewModel : ViewModelBase
{
    private readonly ConcurrentQueue<WaveformDataPoint> _dataQueue = new();
    private readonly object _bufferLock = new();
    private WaveformDataPoint[] _buffer;
    private int _bufferSize;
    private int _writeIndex = 0;
    private string _name = string.Empty;
    private Color _color;
    private bool _isVisible = true;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public Color Color
    {
        get => _color;
        set => SetProperty(ref _color, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public int BufferSize => _bufferSize;

    public WaveformChannelViewModel(string name, Color color, int bufferSize = 1000)
    {
        _name = name;
        _color = color;
        _bufferSize = bufferSize;
        _buffer = new WaveformDataPoint[bufferSize];
    }

    public void AddDataPoint(WaveformDataPoint point)
    {
        lock (_bufferLock)
        {
            _buffer[_writeIndex] = point;
            _writeIndex = (_writeIndex + 1) % _bufferSize;
        }
    }

    public WaveformDataPoint[] GetBufferSnapshot()
    {
        lock (_bufferLock)
        {
            var snapshot = new WaveformDataPoint[_bufferSize];
            Array.Copy(_buffer, snapshot, _bufferSize);
            return snapshot;
        }
    }

    public void Clear()
    {
        lock (_bufferLock)
        {
            _buffer = new WaveformDataPoint[_bufferSize];
            _writeIndex = 0;
        }
    }
}
