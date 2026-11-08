using System.IO.Ports;
using System.Windows;
using System.Windows.Media;

namespace IndustrialEdgeDebugger.App.Models;

public class AppSettings
{
    public SerialPortSettings SerialPort { get; set; } = new();
    public WaveformSettings Waveform { get; set; } = new();
    public WindowSettings Window { get; set; } = new();
    public UserPreferences Preferences { get; set; } = new();
}

public class SerialPortSettings
{
    public string? LastPortName { get; set; }
    public int BaudRate { get; set; } = 9600;
    public Parity Parity { get; set; } = Parity.None;
    public int DataBits { get; set; } = 8;
    public StopBits StopBits { get; set; } = StopBits.One;
    public Handshake Handshake { get; set; } = Handshake.None;
}

public class WaveformSettings
{
    public string Channel1Color { get; set; } = "#32CD32";
    public string Channel2Color { get; set; } = "#00FFFF";
    public string Channel3Color { get; set; } = "#FFA500";
    public int BufferSize { get; set; } = 1000;
    public double DefaultZoomX { get; set; } = 1.0;
    public double DefaultZoomY { get; set; } = 1.0;
}

public class WindowSettings
{
    public double Width { get; set; } = 1200;
    public double Height { get; set; } = 800;
    public double Left { get; set; } = double.NaN;
    public double Top { get; set; } = double.NaN;
    public WindowState WindowState { get; set; } = WindowState.Normal;
}

public class UserPreferences
{
    public bool AutoConnectOnStartup { get; set; } = false;
    public bool SaveLogAutomatically { get; set; } = false;
    public string Theme { get; set; } = "Light";
}
