namespace IndustrialEdgeDebugger.Comm;

public class DeviceInfo
{
    public DeviceType Type { get; set; }
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Manufacturer { get; set; }
    public string? ProductId { get; set; }
    public string? VendorId { get; set; }
}
