namespace IndustrialEdgeDebugger.App.Models;

public struct WaveformDataPoint
{
    public double X { get; set; }
    public double Y { get; set; }
    public DateTime Timestamp { get; set; }

    public WaveformDataPoint(double x, double y)
    {
        X = x;
        Y = y;
        Timestamp = DateTime.Now;
    }

    public WaveformDataPoint(double x, double y, DateTime timestamp)
    {
        X = x;
        Y = y;
        Timestamp = timestamp;
    }
}
