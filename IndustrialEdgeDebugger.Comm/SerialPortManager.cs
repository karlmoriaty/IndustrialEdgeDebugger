using System.IO.Ports;
using System.Text;

namespace IndustrialEdgeDebugger.Comm;

public class SerialPortManager : IDeviceComm
{
    private SerialPort? _serialPort;
    private bool _disposed;
    
    public event EventHandler<byte[]>? DataReceived;
    public event EventHandler<string>? ErrorOccurred;
    
    public bool IsOpen => _serialPort?.IsOpen ?? false;
    
    public DeviceInfo[] GetAvailableDevices()
    {
        var ports = SerialPort.GetPortNames();
        var devices = new List<DeviceInfo>();
        
        foreach (var port in ports)
        {
            devices.Add(new DeviceInfo
            {
                Type = DeviceType.SerialPort,
                Id = port,
                Name = port,
                Description = $"串口 {port}"
            });
        }
        
        return devices.ToArray();
    }
    
    public bool Open(DeviceInfo device, object? config = null)
    {
        try
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                Close();
            }
            
            var portConfig = config as SerialPortConfig ?? new SerialPortConfig { PortName = device.Id };
            
            _serialPort = new SerialPort
            {
                PortName = portConfig.PortName,
                BaudRate = portConfig.BaudRate,
                Parity = portConfig.Parity,
                DataBits = portConfig.DataBits,
                StopBits = portConfig.StopBits,
                Handshake = portConfig.Handshake,
                ReadTimeout = portConfig.ReadTimeout,
                WriteTimeout = portConfig.WriteTimeout
            };
            
            _serialPort.DataReceived += OnDataReceived;
            _serialPort.ErrorReceived += OnErrorReceived;
            _serialPort.Open();
            
            return true;
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex.Message);
            return false;
        }
    }
    
    public bool Close()
    {
        try
        {
            if (_serialPort != null)
            {
                _serialPort.DataReceived -= OnDataReceived;
                _serialPort.ErrorReceived -= OnErrorReceived;
                
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
                
                _serialPort.Dispose();
                _serialPort = null;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex.Message);
            return false;
        }
    }
    
    public bool SendData(byte[] data)
    {
        try
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                OnErrorOccurred("串口未打开");
                return false;
            }
            
            _serialPort.Write(data, 0, data.Length);
            return true;
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex.Message);
            return false;
        }
    }
    
    public bool SendData(string data)
    {
        return SendData(Encoding.UTF8.GetBytes(data));
    }
    
    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            if (_serialPort == null) return;
            
            var bytesToRead = _serialPort.BytesToRead;
            var buffer = new byte[bytesToRead];
            _serialPort.Read(buffer, 0, bytesToRead);
            
            DataReceived?.Invoke(this, buffer);
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex.Message);
        }
    }
    
    private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e)
    {
        OnErrorOccurred($"串口错误: {e.EventType}");
    }
    
    private void OnErrorOccurred(string error)
    {
        ErrorOccurred?.Invoke(this, error);
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
            Close();
        }
        
        _disposed = true;
    }
    
    ~SerialPortManager()
    {
        Dispose(false);
    }
}
