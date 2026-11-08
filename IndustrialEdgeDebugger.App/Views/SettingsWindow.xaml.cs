using System.Windows;
using IndustrialEdgeDebugger.App.ViewModels;
using IndustrialEdgeDebugger.App.Services;

namespace IndustrialEdgeDebugger.App.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(IConfigurationService configService)
    {
        InitializeComponent();
        var viewModel = new SettingsViewModel(configService);
        viewModel.RequestClose += (s, e) => Close();
        DataContext = viewModel;
    }
}
