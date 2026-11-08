﻿﻿﻿using System.Windows;
using IndustrialEdgeDebugger.App.Services;

namespace IndustrialEdgeDebugger.App;

public partial class App : Application
{
    public static IConfigurationService ConfigurationService { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ConfigurationService = new JsonConfigurationService();
        ConfigurationService.Load();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ConfigurationService.Save();
        base.OnExit(e);
    }
}

