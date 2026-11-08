using System.Text;

namespace IndustrialEdgeDebugger.Comm;

public class BluetoothDeviceManager : IDeviceComm
{
    private bool _isOpen;
    private bool _disposed;
    private CancellationTokenSource? _cts;
    private Thread? _readThread;
    
    public event EventHandler<byte[]>? DataReceived;
    public event EventHandler<string>? ErrorOccurred;
    
    public bool IsOpen => _isOpen;
    
    public DeviceInfo[] GetAvailableDevices()
    {
        var devices = new List<DeviceInfo>();
        
        try
        {
            for (int i = 1; i <= 5; i++)
            {
                devices.Add(new DeviceInfo
                {
                    Type = DeviceType.Bluetooth,
                    Id = $"BT_00:11:22:33:44:{i:D2}",
                    Name = $"蓝牙设备 {i}",
                    Description = "蓝牙串行设备",
                    Manufacturer = "Bluetooth Inc.",
                    ProductId = $"BT_PROD_00{i}",
                    VendorId = "BT_VEND_1234"
                });
            }
        }
        catch (Exception)
        {
            devices.Add(new DeviceInfo
            {
                Type = DeviceType.Bluetooth,
                Id = "BT_DEMO",
                Name = "蓝牙演示设备",
                Description = "用于演示的蓝牙设备"
            });
        }
        
        return devices.ToArray();
    }
    
    public bool Open(DeviceInfo device, object? config = null)
    {
        try
        {
            if (_isOpen)
            {
                Close();
            }
            
            _isOpen = true;
            _cts = new CancellationTokenSource();
            _readThread = new Thread(ReadThreadLoop)
            {
                IsBackground = true
            };
            _readThread.Start(_cts.Token);
            
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
            _cts?.Cancel();
            
            if (_readThread != null && !_readThread.Join(1000))
            {
                _readThread.Interrupt();
            }
            
            _isOpen = false;
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
            if (!_isOpen)
            {
                OnErrorOccurred("蓝牙设备未打开");
                return false;
            }
            
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
    
    private void ReadThreadLoop(object? obj)
    {
        if (obj is not CancellationToken token) return;
        
        var random = new Random();
        
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (_isOpen)
                {
                    var buffer = new byte[64];
                    random.NextBytes(buffer);
                    DataReceived?.Invoke(this, buffer);
                }
                
                Thread.Sleep(50);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex.Message);
            }
        }
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
            _cts?.Dispose();
        }
        
        _disposed = true;
    }
    
    ~BluetoothDeviceManager()
    {
        Dispose(false);
    }
}
