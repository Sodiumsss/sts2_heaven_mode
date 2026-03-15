using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Logging;

namespace HeavenMode;

internal static class HeavenConfig
{
    private const string ModFolderName = "HeavenMode";
    private const string ConfigFileName = "config.json";

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
    };

    private sealed class ConfigData
    {
        [JsonPropertyName("unlock")]
        public bool Unlock { get; set; }
    }

    private static ConfigData? _config;

    public static bool UnlockAll
    {
        get
        {
            EnsureLoaded();
            return _config?.Unlock ?? false;
        }
    }

    public static void Initialize() => EnsureLoaded();

    private static void EnsureLoaded()
    {
        if (_config != null)
            return;

        _config = LoadOrCreateConfig();
    }

    private static ConfigData LoadOrCreateConfig()
    {
        string modDirectory = ResolveModDirectory();
        Directory.CreateDirectory(modDirectory);
        string configPath = Path.Combine(modDirectory, ConfigFileName);

        if (!File.Exists(configPath))
        {
            ConfigData defaults = new() { Unlock = false };
            WriteConfig(configPath, defaults);
            return defaults;
        }

        try
        {
            using JsonDocument jsonDocument = JsonDocument.Parse(File.ReadAllText(configPath));
            bool unlock = false;
            if (TryGetUnlockProperty(jsonDocument.RootElement, out JsonElement value) &&
                (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False))
            {
                unlock = value.GetBoolean();
                if (!jsonDocument.RootElement.TryGetProperty("unlock", out _))
                    WriteConfig(configPath, new ConfigData { Unlock = unlock });
            }
            else
            {
                WriteConfig(configPath, new ConfigData { Unlock = false });
            }

            return new ConfigData { Unlock = unlock };
        }
        catch (Exception ex)
        {
            Log.Warn($"[HeavenMode] Failed to parse config at {configPath}: {ex.Message}");
            BackupCorruptedConfig(configPath);
            ConfigData defaults = new() { Unlock = false };
            WriteConfig(configPath, defaults);
            return defaults;
        }
    }

    private static string ResolveModDirectory()
    {
        string? assemblyLocation = Assembly.GetExecutingAssembly().Location;
        string? assemblyDirectory = string.IsNullOrWhiteSpace(assemblyLocation) ? null : Path.GetDirectoryName(assemblyLocation);
        if (!string.IsNullOrWhiteSpace(assemblyDirectory) && Directory.Exists(assemblyDirectory))
            return assemblyDirectory;

        string fallbackModDirectory = Path.Combine(AppContext.BaseDirectory, "mods", ModFolderName);
        if (Directory.Exists(fallbackModDirectory))
            return fallbackModDirectory;

        string appDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataRoot, "StS2Mods", ModFolderName);
    }

    private static void WriteConfig(string configPath, ConfigData config)
    {
        File.WriteAllText(configPath, JsonSerializer.Serialize(config, WriteOptions));
    }

    private static bool TryGetUnlockProperty(JsonElement rootElement, out JsonElement value)
    {
        if (rootElement.TryGetProperty("unlock", out value))
            return true;

        if (rootElement.TryGetProperty(nameof(ConfigData.Unlock), out value))
            return true;

        value = default;
        return false;
    }

    private static void BackupCorruptedConfig(string configPath)
    {
        if (!File.Exists(configPath))
            return;

        string backupPath = $"{configPath}.bak";
        if (File.Exists(backupPath))
            backupPath = $"{configPath}.{DateTime.Now:yyyyMMddHHmmss}.bak";

        File.Move(configPath, backupPath);
    }
}
