using System.IO.Ports;
using System.Windows.Input;
using IndustrialEdgeDebugger.App.Models;
using IndustrialEdgeDebugger.App.Services;

namespace IndustrialEdgeDebugger.App.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly IConfigurationService _configService;
    private AppSettings _workingSettings;

    public string? SelectedPort
    {
        get => _workingSettings.SerialPort.LastPortName;
        set
        {
            _workingSettings.SerialPort.LastPortName = value;
            OnPropertyChanged();
        }
    }

    public int BaudRate
    {
        get => _workingSettings.SerialPort.BaudRate;
        set
        {
            _workingSettings.SerialPort.BaudRate = value;
            OnPropertyChanged();
        }
    }

    public Parity Parity
    {
        get => _workingSettings.SerialPort.Parity;
        set
        {
            _workingSettings.SerialPort.Parity = value;
            OnPropertyChanged();
        }
    }

    public int DataBits
    {
        get => _workingSettings.SerialPort.DataBits;
        set
        {
            _workingSettings.SerialPort.DataBits = value;
            OnPropertyChanged();
        }
    }

    public StopBits StopBits
    {
        get => _workingSettings.SerialPort.StopBits;
        set
        {
            _workingSettings.SerialPort.StopBits = value;
            OnPropertyChanged();
        }
    }

    public Handshake Handshake
    {
        get => _workingSettings.SerialPort.Handshake;
        set
        {
            _workingSettings.SerialPort.Handshake = value;
            OnPropertyChanged();
        }
    }

    public string Channel1Color
    {
        get => _workingSettings.Waveform.Channel1Color;
        set
        {
            _workingSettings.Waveform.Channel1Color = value;
            OnPropertyChanged();
        }
    }

    public string Channel2Color
    {
        get => _workingSettings.Waveform.Channel2Color;
        set
        {
            _workingSettings.Waveform.Channel2Color = value;
            OnPropertyChanged();
        }
    }

    public string Channel3Color
    {
        get => _workingSettings.Waveform.Channel3Color;
        set
        {
            _workingSettings.Waveform.Channel3Color = value;
            OnPropertyChanged();
        }
    }

    public int BufferSize
    {
        get => _workingSettings.Waveform.BufferSize;
        set
        {
            _workingSettings.Waveform.BufferSize = value;
            OnPropertyChanged();
        }
    }

    public double DefaultZoomX
    {
        get => _workingSettings.Waveform.DefaultZoomX;
        set
        {
            _workingSettings.Waveform.DefaultZoomX = value;
            OnPropertyChanged();
        }
    }

    public double DefaultZoomY
    {
        get => _workingSettings.Waveform.DefaultZoomY;
        set
        {
            _workingSettings.Waveform.DefaultZoomY = value;
            OnPropertyChanged();
        }
    }

    public bool AutoConnectOnStartup
    {
        get => _workingSettings.Preferences.AutoConnectOnStartup;
        set
        {
            _workingSettings.Preferences.AutoConnectOnStartup = value;
            OnPropertyChanged();
        }
    }

    public bool SaveLogAutomatically
    {
        get => _workingSettings.Preferences.SaveLogAutomatically;
        set
        {
            _workingSettings.Preferences.SaveLogAutomatically = value;
            OnPropertyChanged();
        }
    }

    public string Theme
    {
        get => _workingSettings.Preferences.Theme;
        set
        {
            _workingSettings.Preferences.Theme = value;
            OnPropertyChanged();
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ResetCommand { get; }

    public event EventHandler? RequestClose;

    public SettingsViewModel(IConfigurationService configService)
    {
        _configService = configService;
        _workingSettings = CloneSettings(_configService.Settings);

        SaveCommand = new RelayCommand(_ => Save());
        CancelCommand = new RelayCommand(_ => Cancel());
        ResetCommand = new RelayCommand(_ => Reset());
    }

    private AppSettings CloneSettings(AppSettings original)
    {
        return new AppSettings
        {
            SerialPort = new SerialPortSettings
            {
                LastPortName = original.SerialPort.LastPortName,
                BaudRate = original.SerialPort.BaudRate,
                Parity = original.SerialPort.Parity,
                DataBits = original.SerialPort.DataBits,
                StopBits = original.SerialPort.StopBits,
                Handshake = original.SerialPort.Handshake
            },
            Waveform = new WaveformSettings
            {
                Channel1Color = original.Waveform.Channel1Color,
                Channel2Color = original.Waveform.Channel2Color,
                Channel3Color = original.Waveform.Channel3Color,
                BufferSize = original.Waveform.BufferSize,
                DefaultZoomX = original.Waveform.DefaultZoomX,
                DefaultZoomY = original.Waveform.DefaultZoomY
            },
            Window = new WindowSettings
            {
                Width = original.Window.Width,
                Height = original.Window.Height,
                Left = original.Window.Left,
                Top = original.Window.Top,
                WindowState = original.Window.WindowState
            },
            Preferences = new UserPreferences
            {
                AutoConnectOnStartup = original.Preferences.AutoConnectOnStartup,
                SaveLogAutomatically = original.Preferences.SaveLogAutomatically,
                Theme = original.Preferences.Theme
            }
        };
    }

    private void Save()
    {
        _configService.Settings.SerialPort = _workingSettings.SerialPort;
        _configService.Settings.Waveform = _workingSettings.Waveform;
        _configService.Settings.Preferences = _workingSettings.Preferences;
        _configService.Save();
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private void Cancel()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private void Reset()
    {
        _workingSettings = new AppSettings();
        OnPropertyChanged(string.Empty);
    }
}
