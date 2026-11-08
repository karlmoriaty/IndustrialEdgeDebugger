namespace IndustrialEdgeDebugger.Comm;

public interface ISerialPort : IDisposable
{
    event EventHandler<byte[]>? DataReceived;
    event EventHandler<string>? ErrorOccurred;
    
    string[] GetAvailablePorts();
    bool IsOpen { get; }
    bool Open(SerialPortConfig config);
    bool Close();
    bool SendData(byte[] data);
    bool SendData(string data);
}
