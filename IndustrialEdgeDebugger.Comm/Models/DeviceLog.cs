namespace IndustrialEdgeDebugger.Comm.Models;

public class DeviceLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
}
