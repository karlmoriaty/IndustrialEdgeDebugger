using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Text;
using System.Text.Json;
using System.Windows.Input;
using System.Windows.Media;
using IndustrialEdgeDebugger.App.Models;
using IndustrialEdgeDebugger.Comm;
using IndustrialEdgeDebugger.Comm.Services;
using IndustrialEdgeDebugger.Comm.Models;
using CommDeviceInfo = IndustrialEdgeDebugger.Comm.DeviceInfo;
using IndustrialEdgeDebugger.App.Services;

namespace IndustrialEdgeDebugger.App.ViewModels;

public class MainWindowViewModel : ViewModelBase, IDisposable
{
    private IDeviceComm _deviceComm;
    private readonly ILogStorage _logStorage;
    private CommDeviceInfo? _selectedDevice;
    private DeviceType _selectedDeviceType;
    private string? _selectedPort;
    private int _selectedBaudRate;
    private Parity _selectedParity;
    private int _selectedDataBits;
    private StopBits _selectedStopBits;
    private bool _isConnected;
    private string _receivedData = string.Empty;
    private string _connectionStatus = "未连接";
    private bool _isSimulating = false;
    private CancellationTokenSource? _simulationCts;
    private CancellationTokenSource? _processingCts;
    private Task? _processingTask;
    private readonly BlockingCollection<byte[]> _dataQueue = new();
    private bool _isRecording;
    private string _recordingStatus = "未记录";
    private long _dataPointCount;
    private string _dataPointCountText = "0";
    private double _dataXOffset;
    private readonly object _dataXLock = new();
    private bool _disposed;

    public ObservableCollection<DeviceType> DeviceTypes { get; } = new();
    public ObservableCollection<CommDeviceInfo> AvailableDevices { get; } = new();
    public ObservableCollection<string> AvailablePorts { get; } = new();
    public ObservableCollection<int> BaudRates { get; } = new();
    public ObservableCollection<Parity> Parities { get; } = new();
    public ObservableCollection<int> DataBitsOptions { get; } = new();
    public ObservableCollection<StopBits> StopBitsOptions { get; } = new();
    public ObservableCollection<WaveformChannelViewModel> WaveformChannels { get; } = new();

    public DeviceType SelectedDeviceType
    {
        get => _selectedDeviceType;
        set
        {
            if (SetProperty(ref _selectedDeviceType, value))
            {
                RefreshDevices();
                SaveConfiguration();
            }
        }
    }

    public CommDeviceInfo? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (SetProperty(ref _selectedDevice, value))
            {
                SaveConfiguration();
                ((RelayCommand)ConnectCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string? SelectedPort
    {
        get => _selectedPort;
        set
        {
            if (SetProperty(ref _selectedPort, value))
            {
                SaveConfiguration();
            }
        }
    }

    public int SelectedBaudRate
    {
        get => _selectedBaudRate;
        set
        {
            if (SetProperty(ref _selectedBaudRate, value))
            {
                SaveConfiguration();
            }
        }
    }

    public Parity SelectedParity
    {
        get => _selectedParity;
        set
        {
            if (SetProperty(ref _selectedParity, value))
            {
                SaveConfiguration();
            }
        }
    }

    public int SelectedDataBits
    {
        get => _selectedDataBits;
        set
        {
            if (SetProperty(ref _selectedDataBits, value))
            {
                SaveConfiguration();
            }
        }
    }

    public StopBits SelectedStopBits
    {
        get => _selectedStopBits;
        set
        {
            if (SetProperty(ref _selectedStopBits, value))
            {
                SaveConfiguration();
            }
        }
    }

    private void SaveConfiguration()
    {
        App.ConfigurationService.Settings.SerialPort.LastPortName = SelectedPort;
        App.ConfigurationService.Settings.SerialPort.BaudRate = SelectedBaudRate;
        App.ConfigurationService.Settings.SerialPort.Parity = SelectedParity;
        App.ConfigurationService.Settings.SerialPort.DataBits = SelectedDataBits;
        App.ConfigurationService.Settings.SerialPort.StopBits = SelectedStopBits;
    }

    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            SetProperty(ref _isConnected, value);
            ConnectionStatus = value ? "已连接" : "未连接";
            ((RelayCommand)ConnectCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DisconnectCommand).RaiseCanExecuteChanged();
        }
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }

    public string ReceivedData
    {
        get => _receivedData;
        set => SetProperty(ref _receivedData, value);
    }

    public bool IsSimulating
    {
        get => _isSimulating;
        set
        {
            SetProperty(ref _isSimulating, value);
            ((RelayCommand)ToggleSimulationCommand).RaiseCanExecuteChanged();
        }
    }

    public bool IsRecording
    {
        get => _isRecording;
        set
        {
            SetProperty(ref _isRecording, value);
            RecordingStatus = value ? "记录中" : "未记录";
            ((RelayCommand)StartRecordingCommand).RaiseCanExecuteChanged();
            ((RelayCommand)StopRecordingCommand).RaiseCanExecuteChanged();
        }
    }

    public string RecordingStatus
    {
        get => _recordingStatus;
        set => SetProperty(ref _recordingStatus, value);
    }

    public long DataPointCount
    {
        get => _dataPointCount;
        set
        {
            SetProperty(ref _dataPointCount, value);
            DataPointCountText = value.ToString("N0");
        }
    }

    public string DataPointCountText
    {
        get => _dataPointCountText;
        set => SetProperty(ref _dataPointCountText, value);
    }

    public ICommand RefreshDevicesCommand { get; }
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand ToggleSimulationCommand { get; }
    public ICommand ClearWaveformsCommand { get; }
    public ICommand StartRecordingCommand { get; }
    public ICommand StopRecordingCommand { get; }
    
    public ILogStorage LogStorage => _logStorage;

    public MainWindowViewModel()
    {
        _deviceComm = DeviceCommFactory.Create(DeviceType.SerialPort);
        _deviceComm.DataReceived += OnDataReceived;
        _deviceComm.ErrorOccurred += OnErrorOccurred;

        _logStorage = new SqliteLogStorage("device_logs.db");
        _logStorage.Initialize();

        InitializeCollections();
        InitializeWaveformChannels();
        ApplyConfiguration();
        RefreshDevices();

        RefreshDevicesCommand = new RelayCommand(_ => RefreshDevices());
        ConnectCommand = new RelayCommand(_ => Connect(), _ => !IsConnected && SelectedDevice != null);
        DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => IsConnected);
        ToggleSimulationCommand = new RelayCommand(_ => ToggleSimulation());
        ClearWaveformsCommand = new RelayCommand(_ => ClearWaveforms());
        StartRecordingCommand = new RelayCommand(_ => StartRecording(), _ => !IsRecording);
        StopRecordingCommand = new RelayCommand(_ => StopRecording(), _ => IsRecording);

        StartProcessingThread();
    }

    private void ApplyConfiguration()
    {
        var config = App.ConfigurationService.Settings;
        SelectedBaudRate = config.SerialPort.BaudRate;
        SelectedParity = config.SerialPort.Parity;
        SelectedDataBits = config.SerialPort.DataBits;
        SelectedStopBits = config.SerialPort.StopBits;
        SelectedDeviceType = DeviceType.SerialPort;
        if (!string.IsNullOrEmpty(config.SerialPort.LastPortName))
        {
            SelectedPort = config.SerialPort.LastPortName;
        }
    }

    private void InitializeWaveformChannels()
    {
        WaveformChannels.Add(new WaveformChannelViewModel("通道 1", Colors.LimeGreen, 1000));
        WaveformChannels.Add(new WaveformChannelViewModel("通道 2", Colors.Cyan, 1000));
        WaveformChannels.Add(new WaveformChannelViewModel("通道 3", Colors.Orange, 1000));
    }

    private void InitializeCollections()
    {
        DeviceTypes.AddRange(Enum.GetValues<DeviceType>());
        BaudRates.AddRange(new[] { 300, 600, 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600 });
        Parities.AddRange(Enum.GetValues<Parity>());
        DataBitsOptions.AddRange(new[] { 5, 6, 7, 8 });
        StopBitsOptions.AddRange(new[] { StopBits.One, StopBits.Two, StopBits.OnePointFive });

        SelectedBaudRate = 9600;
        SelectedParity = Parity.None;
        SelectedDataBits = 8;
        SelectedStopBits = StopBits.One;
    }

    private void RefreshDevices()
    {
        AvailableDevices.Clear();
        
        try
        {
            var devices = DeviceCommFactory.GetAvailableDevicesByType(SelectedDeviceType);
            AvailableDevices.AddRange(devices);
        }
        catch
        {
        }

        if (AvailableDevices.Count > 0 && SelectedDevice == null)
        {
            SelectedDevice = AvailableDevices[0];
        }
    }

    private void Connect()
    {
        if (SelectedDevice == null) return;

        try
        {
            _deviceComm.Dispose();
        }
        catch { }

        _deviceComm = DeviceCommFactory.Create(SelectedDevice.Type);
        _deviceComm.DataReceived += OnDataReceived;
        _deviceComm.ErrorOccurred += OnErrorOccurred;

        object? config = null;
        if (SelectedDevice.Type == DeviceType.SerialPort)
        {
            config = new SerialPortConfig
            {
                PortName = SelectedDevice.Id,
                BaudRate = SelectedBaudRate,
                Parity = SelectedParity,
                DataBits = SelectedDataBits,
                StopBits = SelectedStopBits
            };
        }

        IsConnected = _deviceComm.Open(SelectedDevice, config);
        lock (_dataXLock)
        {
            _dataXOffset = 0;
        }
    }

    private void Disconnect()
    {
        IsConnected = !_deviceComm.Close();
    }

    private void StartProcessingThread()
    {
        _processingCts = new CancellationTokenSource();
        _processingTask = Task.Run(() => ProcessingLoop(_processingCts.Token), _processingCts.Token);
    }

    private void StopProcessingThread()
    {
        _processingCts?.Cancel();
        _dataQueue.CompleteAdding();
        _processingTask?.Wait(TimeSpan.FromSeconds(5));
    }

    private void ProcessingLoop(CancellationToken cancellationToken)
    {
        foreach (var data in _dataQueue.GetConsumingEnumerable(cancellationToken))
        {
            try
            {
                ProcessData(data);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理数据时出错: {ex.Message}");
            }
        }
    }

    private void OnDataReceived(object? sender, byte[] data)
    {
        _dataQueue.Add(data);

        var text = Encoding.UTF8.GetString(data);
        App.Current.Dispatcher.Invoke(() =>
        {
            ReceivedData += text;
        });
    }

    private void ProcessData(byte[] data)
    {
        var timestamp = DateTime.Now;
        var text = Encoding.UTF8.GetString(data);

        var parsedPoints = ParseData(text, timestamp);

        if (parsedPoints.Count > 0)
        {
            foreach (var point in parsedPoints)
            {
                AddWaveformDataPoint(point, timestamp);
            }

            if (IsRecording)
            {
                SaveToDatabase(text, timestamp);
            }
        }
    }

    private List<WaveformDataPoint> ParseData(string text, DateTime timestamp)
    {
        var points = new List<WaveformDataPoint>();

        try
        {
            if (text.StartsWith("{") && text.EndsWith("}"))
            {
                var jsonData = JsonSerializer.Deserialize<JsonElement>(text);
                if (jsonData.TryGetProperty("values", out var values) && values.ValueKind == JsonValueKind.Array)
                {
                    double x;
                    lock (_dataXLock)
                    {
                        x = _dataXOffset++;
                    }

                    for (int i = 0; i < values.GetArrayLength(); i++)
                    {
                        if (i < WaveformChannels.Count && values[i].TryGetDouble(out double y))
                        {
                            points.Add(new WaveformDataPoint(x, y, timestamp));
                        }
                    }
                }
                else if (jsonData.TryGetProperty("value", out var singleValue) && singleValue.TryGetDouble(out double singleY))
                {
                    double x;
                    lock (_dataXLock)
                    {
                        x = _dataXOffset++;
                    }
                    points.Add(new WaveformDataPoint(x, singleY, timestamp));
                }
            }
            else
            {
                var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;

                    var parts = trimmed.Split(new[] { ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        double x;
                        lock (_dataXLock)
                        {
                            x = _dataXOffset++;
                        }

                        for (int i = 0; i < parts.Length && i < WaveformChannels.Count; i++)
                        {
                            if (double.TryParse(parts[i], out double y))
                            {
                                points.Add(new WaveformDataPoint(x, y, timestamp));
                            }
                        }
                    }
                }
            }
        }
        catch
        {
        }

        return points;
    }

    private void AddWaveformDataPoint(WaveformDataPoint point, DateTime timestamp)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            for (int i = 0; i < WaveformChannels.Count; i++)
            {
                if (i == 0)
                {
                    WaveformChannels[i].AddDataPoint(point);
                }
                else
                {
                    WaveformChannels[i].AddDataPoint(new WaveformDataPoint(point.X, point.Y * (0.5 + i * 0.2), timestamp));
                }
            }
            DataPointCount++;
        });
    }

    private void SaveToDatabase(string data, DateTime timestamp)
    {
        try
        {
            var log = new DeviceLog
            {
                Timestamp = timestamp,
                DeviceId = SelectedDevice?.Id ?? "Unknown",
                Data = data,
                DataType = "Waveform"
            };
            _logStorage.AddLog(log);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存到数据库时出错: {ex.Message}");
        }
    }

    private void OnErrorOccurred(object? sender, string error)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            ReceivedData += $"[错误] {error}\r\n";
        });
    }

    private void StartRecording()
    {
        IsRecording = true;
    }

    private void StopRecording()
    {
        IsRecording = false;
    }

    private async void ToggleSimulation()
    {
        if (IsSimulating)
        {
            StopSimulation();
        }
        else
        {
            await StartSimulationAsync();
        }
    }

    private async Task StartSimulationAsync()
    {
        IsSimulating = true;
        _simulationCts = new CancellationTokenSource();

        try
        {
            await Task.Run(() => SimulationLoop(_simulationCts.Token), _simulationCts.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void StopSimulation()
    {
        _simulationCts?.Cancel();
        IsSimulating = false;
    }

    private void SimulationLoop(CancellationToken cancellationToken)
    {
        double x = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            var timestamp = DateTime.Now;
            var t = x * 0.1;
            var y1 = Math.Sin(t) * 0.8;
            var y2 = Math.Sin(t * 1.5 + 0.5) * 0.6;
            var y3 = Math.Sin(t * 0.8 + 1.0) * 0.4;

            if (WaveformChannels.Count >= 1)
                WaveformChannels[0].AddDataPoint(new WaveformDataPoint(x, y1, timestamp));
            if (WaveformChannels.Count >= 2)
                WaveformChannels[1].AddDataPoint(new WaveformDataPoint(x, y2, timestamp));
            if (WaveformChannels.Count >= 3)
                WaveformChannels[2].AddDataPoint(new WaveformDataPoint(x, y3, timestamp));

            x++;
            DataPointCount++;
            Thread.Sleep(10);
        }
    }

    private void ClearWaveforms()
    {
        foreach (var channel in WaveformChannels)
        {
            channel.Clear();
        }
        DataPointCount = 0;
        lock (_dataXLock)
        {
            _dataXOffset = 0;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            StopProcessingThread();
            StopSimulation();
            _deviceComm.Dispose();
            _logStorage.Dispose();
            _dataQueue.Dispose();
        }

        _disposed = true;
    }

    ~MainWindowViewModel()
    {
        Dispose(false);
    }
}

public static class ObservableCollectionExtensions
{
    public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
