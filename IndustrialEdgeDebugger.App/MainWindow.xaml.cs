﻿﻿﻿using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using IndustrialEdgeDebugger.App.ViewModels;
using IndustrialEdgeDebugger.App.Views;

namespace IndustrialEdgeDebugger.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SettingsButton.Click += SettingsButton_Click;
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(App.ConfigurationService)
        {
            Owner = this
        };
        settingsWindow.ShowDialog();
        
        // 配置变更后重新应用
        if (DataContext is MainWindowViewModel viewModel)
        {
            var config = App.ConfigurationService.Settings;
            viewModel.SelectedBaudRate = config.SerialPort.BaudRate;
            viewModel.SelectedParity = config.SerialPort.Parity;
            viewModel.SelectedDataBits = config.SerialPort.DataBits;
            viewModel.SelectedStopBits = config.SerialPort.StopBits;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private void LogQueryButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            var logQueryViewModel = new LogQueryViewModel(viewModel.LogStorage);
            var logQueryWindow = new LogQueryWindow(logQueryViewModel)
            {
                Owner = this
            };
            logQueryWindow.ShowDialog();
        }
    }
}