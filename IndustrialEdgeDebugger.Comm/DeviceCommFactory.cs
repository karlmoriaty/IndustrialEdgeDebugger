namespace IndustrialEdgeDebugger.Comm;

public static class DeviceCommFactory
{
    public static IDeviceComm Create(DeviceType deviceType)
    {
        return deviceType switch
        {
            DeviceType.SerialPort => new SerialPortManager(),
            DeviceType.USB => new UsbDeviceManager(),
            DeviceType.Bluetooth => new BluetoothDeviceManager(),
            _ => throw new NotSupportedException($"不支持的设备类型: {deviceType}")
        };
    }
    
    public static DeviceInfo[] GetAllAvailableDevices()
    {
        var allDevices = new List<DeviceInfo>();
        
        try
        {
            using var serialManager = new SerialPortManager();
            allDevices.AddRange(serialManager.GetAvailableDevices());
        }
        catch { }
        
        try
        {
            using var usbManager = new UsbDeviceManager();
            allDevices.AddRange(usbManager.GetAvailableDevices());
        }
        catch { }
        
        try
        {
            using var btManager = new BluetoothDeviceManager();
            allDevices.AddRange(btManager.GetAvailableDevices());
        }
        catch { }
        
        return allDevices.ToArray();
    }
    
    public static DeviceInfo[] GetAvailableDevicesByType(DeviceType deviceType)
    {
        try
        {
            using var manager = Create(deviceType);
            return manager.GetAvailableDevices();
        }
        catch
        {
            return Array.Empty<DeviceInfo>();
        }
    }
}
