using System.Collections.ObjectModel;
using System.Windows.Input;
using IndustrialEdgeDebugger.Comm.Models;
using IndustrialEdgeDebugger.Comm.Services;

namespace IndustrialEdgeDebugger.App.ViewModels;

public class LogQueryViewModel : ViewModelBase
{
    private readonly ILogStorage _logStorage;
    private DateTime? _startTime;
    private DateTime? _endTime;
    private string? _selectedDeviceId;
    private string? _selectedDataType;
    private string? _keyword;
    private int _currentPage = 1;
    private int _pageSize = 50;
    private int _totalCount;
    private bool _isLoading;
    private bool _isExporting;
    private DeviceLog? _selectedLog;

    public ObservableCollection<DeviceLog> Logs { get; } = new();
    public ObservableCollection<string> AvailableDeviceIds { get; } = new();
    public ObservableCollection<string> AvailableDataTypes { get; } = new();

    public DateTime? StartTime
    {
        get => _startTime;
        set => SetProperty(ref _startTime, value);
    }

    public DateTime? EndTime
    {
        get => _endTime;
        set => SetProperty(ref _endTime, value);
    }

    public string? SelectedDeviceId
    {
        get => _selectedDeviceId;
        set => SetProperty(ref _selectedDeviceId, value);
    }

    public string? SelectedDataType
    {
        get => _selectedDataType;
        set => SetProperty(ref _selectedDataType, value);
    }

    public string? Keyword
    {
        get => _keyword;
        set => SetProperty(ref _keyword, value);
    }

    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
                ((RelayCommand)PreviousPageCommand).RaiseCanExecuteChanged();
                ((RelayCommand)NextPageCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public int PageSize
    {
        get => _pageSize;
        set => SetProperty(ref _pageSize, value);
    }

    public int TotalCount
    {
        get => _totalCount;
        set => SetProperty(ref _totalCount, value);
    }

    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                ((RelayCommand)QueryCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ExportCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsExporting
    {
        get => _isExporting;
        set
        {
            if (SetProperty(ref _isExporting, value))
            {
                ((RelayCommand)ExportCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public DeviceLog? SelectedLog
    {
        get => _selectedLog;
        set => SetProperty(ref _selectedLog, value);
    }

    public ICommand QueryCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand ShowLogDetailCommand { get; }

    public event EventHandler<DeviceLog>? LogDetailRequested;
    public event EventHandler<string>? ExportRequested;

    public LogQueryViewModel(ILogStorage logStorage)
    {
        _logStorage = logStorage;

        QueryCommand = new RelayCommand(async _ => await ExecuteQueryAsync(), _ => !IsLoading);
        ClearCommand = new RelayCommand(_ => ExecuteClear());
        ExportCommand = new RelayCommand(_ => OnExportRequested(), _ => !IsLoading && !IsExporting && Logs.Count > 0);
        PreviousPageCommand = new RelayCommand(async _ => await GoToPreviousPageAsync(), _ => CurrentPage > 1);
        NextPageCommand = new RelayCommand(async _ => await GoToNextPageAsync(), _ => CurrentPage < TotalPages);
        ShowLogDetailCommand = new RelayCommand(_ => OnShowLogDetail(), _ => SelectedLog != null);

        StartTime = DateTime.Today.AddDays(-7);
        EndTime = DateTime.Now;
    }

    public void Initialize()
    {
        LoadAvailableFilters();
    }

    public void LoadAvailableFilters()
    {
        AvailableDeviceIds.Clear();
        AvailableDataTypes.Clear();

        try
        {
            var deviceIds = _logStorage.GetDistinctDeviceIds();
            var dataTypes = _logStorage.GetDistinctDataTypes();

            AvailableDeviceIds.Add("");
            foreach (var id in deviceIds)
            {
                AvailableDeviceIds.Add(id);
            }

            AvailableDataTypes.Add("");
            foreach (var type in dataTypes)
            {
                AvailableDataTypes.Add(type);
            }
        }
        catch
        {
        }
    }

    private async Task ExecuteQueryAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        CurrentPage = 1;
        Logs.Clear();

        try
        {
            await Task.Run(() =>
            {
                var deviceId = string.IsNullOrEmpty(SelectedDeviceId) ? null : SelectedDeviceId;
                var dataType = string.IsNullOrEmpty(SelectedDataType) ? null : SelectedDataType;
                var keyword = string.IsNullOrEmpty(Keyword) ? null : Keyword;

                var (logs, totalCount) = _logStorage.GetLogsPaged(
                    StartTime,
                    EndTime,
                    deviceId,
                    dataType,
                    keyword,
                    CurrentPage,
                    PageSize
                );

                App.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var log in logs)
                    {
                        Logs.Add(log);
                    }
                    TotalCount = totalCount;
                    OnPropertyChanged(nameof(TotalPages));
                });
            });
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task GoToPreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadPageAsync();
        }
    }

    private async Task GoToNextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadPageAsync();
        }
    }

    private async Task LoadPageAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        Logs.Clear();

        try
        {
            await Task.Run(() =>
            {
                var deviceId = string.IsNullOrEmpty(SelectedDeviceId) ? null : SelectedDeviceId;
                var dataType = string.IsNullOrEmpty(SelectedDataType) ? null : SelectedDataType;
                var keyword = string.IsNullOrEmpty(Keyword) ? null : Keyword;

                var (logs, totalCount) = _logStorage.GetLogsPaged(
                    StartTime,
                    EndTime,
                    deviceId,
                    dataType,
                    keyword,
                    CurrentPage,
                    PageSize
                );

                App.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var log in logs)
                    {
                        Logs.Add(log);
                    }
                });
            });
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ExecuteClear()
    {
        StartTime = DateTime.Today.AddDays(-7);
        EndTime = DateTime.Now;
        SelectedDeviceId = "";
        SelectedDataType = "";
        Keyword = "";
        CurrentPage = 1;
        TotalCount = 0;
        Logs.Clear();
        LoadAvailableFilters();
    }

    private void OnExportRequested()
    {
        ExportRequested?.Invoke(this, "");
    }

    private void OnShowLogDetail()
    {
        if (SelectedLog != null)
        {
            LogDetailRequested?.Invoke(this, SelectedLog);
        }
    }

    public async Task ExportLogsAsync(string filePath)
    {
        if (IsExporting) return;

        IsExporting = true;

        try
        {
            await Task.Run(() =>
            {
                var deviceId = string.IsNullOrEmpty(SelectedDeviceId) ? null : SelectedDeviceId;
                var dataType = string.IsNullOrEmpty(SelectedDataType) ? null : SelectedDataType;
                var keyword = string.IsNullOrEmpty(Keyword) ? null : Keyword;

                var (logs, _) = _logStorage.GetLogsPaged(
                    StartTime,
                    EndTime,
                    deviceId,
                    dataType,
                    keyword,
                    1,
                    int.MaxValue
                );

                _logStorage.ExportToCsv(logs, filePath);
            });
        }
        finally
        {
            IsExporting = false;
        }
    }
}
