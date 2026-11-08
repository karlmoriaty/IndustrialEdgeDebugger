using IndustrialEdgeDebugger.App.Models;

namespace IndustrialEdgeDebugger.App.Services;

public interface IConfigurationService
{
    AppSettings Settings { get; }
    void Load();
    void Save();
    void ResetToDefaults();
}
