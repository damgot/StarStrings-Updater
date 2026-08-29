using System.Text.Json;
using StarStringsUpdater.Models;

namespace StarStringsUpdater.Services;

/// <summary>
/// Persists the application's state (root folder + installed version per channel)
/// in a JSON file next to the executable.
/// </summary>
public sealed class SettingsStore
{
    private readonly string _filePath;

    public SettingsStore()
    {
        _filePath = Path.Combine(AppContext.BaseDirectory, "state.json");
    }

    public AppState Load()
    {
        if (!File.Exists(_filePath))
        {
            return new AppState();
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<AppState>(json) ?? new AppState();
        }
        catch (Exception)
        {
            return new AppState();
        }
    }

    public void Save(AppState state)
    {
        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }
}
