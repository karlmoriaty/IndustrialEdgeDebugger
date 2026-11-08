using System.IO;
using System.Text.Json;
using IndustrialEdgeDebugger.App.Models;

namespace IndustrialEdgeDebugger.App.Services;

public class JsonConfigurationService : IConfigurationService
{
    private readonly string _configFilePath;
    private AppSettings _settings = new();

    public AppSettings Settings => _settings;

    public JsonConfigurationService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "IndustrialEdgeDebugger");
        Directory.CreateDirectory(appFolder);
        _configFilePath = Path.Combine(appFolder, "appsettings.json");
    }

    public void Load()
    {
        if (File.Exists(_configFilePath))
        {
            try
            {
                var json = File.ReadAllText(_configFilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (loaded != null)
                {
                    _settings = loaded;
                }
            }
            catch
            {
                _settings = new AppSettings();
            }
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_configFilePath, json);
        }
        catch
        {
        }
    }

    public void ResetToDefaults()
    {
        _settings = new AppSettings();
        Save();
    }
}
