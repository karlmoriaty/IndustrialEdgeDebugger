namespace IndustrialEdgeDebugger.Comm;

public interface IDeviceComm : IDisposable
{
    event EventHandler<byte[]>? DataReceived;
    event EventHandler<string>? ErrorOccurred;
    
    DeviceInfo[] GetAvailableDevices();
    bool IsOpen { get; }
    bool Open(DeviceInfo device, object? config = null);
    bool Close();
    bool SendData(byte[] data);
    bool SendData(string data);
}
