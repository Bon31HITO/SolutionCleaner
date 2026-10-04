using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SolutionCleaner;

internal sealed class SettingsManager
{
    private readonly string settingsFilePath;

    public SettingsManager()
    {
        string localApplicationDataDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string applicationDirectoryPath = Path.Combine(localApplicationDataDirectoryPath, "SolutionCleaner");
        settingsFilePath = Path.Combine(applicationDirectoryPath, "settings.json");
    }

    public Settings LoadSettings()
    {
        try
        {
            if (!File.Exists(settingsFilePath))
            {
                return new Settings(
                    string.Empty,
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            }

            string jsonContent = File.ReadAllText(settingsFilePath);
            Settings loadedSettings = JsonSerializer.Deserialize<Settings>(jsonContent);

            string destinationDirectoryPath = string.IsNullOrWhiteSpace(loadedSettings.DestinationDirectoryPath)
                ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                : loadedSettings.DestinationDirectoryPath;

            return new Settings(
                loadedSettings.SourceDirectoryPath ?? string.Empty,
                destinationDirectoryPath);
        }
        catch
        {
            return new Settings(
                string.Empty,
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
        }
    }

    public void SaveSettings(Settings settings)
    {
        try
        {
            string? directoryPath = Path.GetDirectoryName(settingsFilePath);
            if (directoryPath is not null && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            JsonSerializerOptions serializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string jsonContent = JsonSerializer.Serialize(settings, serializerOptions);
            File.WriteAllText(settingsFilePath, jsonContent);
        }
        catch
        {
            // 設定保存の例外はアプリケーションの主処理継続のため握り潰します
        }
    }
}
