using System.Windows;
using System.Windows.Controls;
using IndustrialEdgeDebugger.App.ViewModels;
using IndustrialEdgeDebugger.Comm.Models;
using Microsoft.Win32;

namespace IndustrialEdgeDebugger.App.Views;

public partial class LogQueryWindow : Window
{
    private readonly LogQueryViewModel _viewModel;

    public LogQueryWindow(LogQueryViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;
        
        _viewModel.LogDetailRequested += OnLogDetailRequested;
        _viewModel.ExportRequested += OnExportRequested;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel.Initialize();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LogQueryViewModel.IsLoading))
        {
            StatusValue.Text = _viewModel.IsLoading ? "查询中..." : "就绪";
        }
        else if (e.PropertyName == nameof(LogQueryViewModel.IsExporting))
        {
            StatusValue.Text = _viewModel.IsExporting ? "导出中..." : "就绪";
        }
    }

    private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_viewModel.SelectedLog != null)
        {
            ShowLogDetail(_viewModel.SelectedLog);
        }
    }

    private void OnLogDetailRequested(object? sender, DeviceLog log)
    {
        ShowLogDetail(log);
    }

    private void ShowLogDetail(DeviceLog log)
    {
        var detailWindow = new Window
        {
            Title = "日志详情",
            Width = 600,
            Height = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        AddDetailRow(grid, 0, "ID:", log.Id.ToString());
        AddDetailRow(grid, 1, "时间:", log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        AddDetailRow(grid, 2, "设备ID:", log.DeviceId);
        AddDetailRow(grid, 3, "数据类型:", log.DataType);
        
        var dataLabel = new TextBlock { Text = "数据:", Margin = new Thickness(10, 10, 10, 5), FontWeight = FontWeights.Bold };
        Grid.SetRow(dataLabel, 4);
        grid.Children.Add(dataLabel);

        var dataTextBox = new TextBox
        {
            Text = log.Data,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(10, 0, 10, 10)
        };
        Grid.SetRow(dataTextBox, 5);
        Grid.SetRowSpan(dataTextBox, 4);
        grid.Children.Add(dataTextBox);

        detailWindow.Content = grid;
        detailWindow.ShowDialog();
    }

    private void AddDetailRow(Grid grid, int row, string label, string value)
    {
        var labelBlock = new TextBlock
        {
            Text = label,
            Margin = new Thickness(10, 10, 10, 5),
            FontWeight = FontWeights.Bold
        };
        Grid.SetRow(labelBlock, row);
        Grid.SetColumn(labelBlock, 0);
        grid.Children.Add(labelBlock);

        var valueBlock = new TextBlock
        {
            Text = value,
            Margin = new Thickness(10, 10, 10, 5)
        };
        Grid.SetRow(valueBlock, row);
        Grid.SetColumn(valueBlock, 1);
        grid.Children.Add(valueBlock);
    }

    private async void OnExportRequested(object? sender, string e)
    {
        var saveDialog = new SaveFileDialog
        {
            Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
            DefaultExt = "csv",
            FileName = $"logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                await _viewModel.ExportLogsAsync(saveDialog.FileName);
                MessageBox.Show(this, "导出成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
