using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace HeavenMode;

internal static class HeavenPersistence
{
    private const string SaveFileName = "heaven_mode_current.json";
    private const string PreferenceFileName = "heaven_mode_preferences.json";

    // Legacy single-entry format used before multi-run store was introduced.
    private sealed class LegacyHeavenRunMetadata
    {
        public long StartTime { get; set; }
        public int HeavenLevel { get; set; }
    }

    private sealed class HeavenPreferenceMetadata
    {
        public Dictionary<string, int> CharacterLevels { get; set; } = new();
    }

    public static void SaveCurrentRunSelection()
    {
        try
        {
            long startTime = GetCurrentRunStartTime();
            if (startTime <= 0)
                return;

            SaveSelection(startTime, HeavenState.SelectedOption);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] SaveCurrentRunSelection failed: {ex}");
        }
    }

    public static void SaveSelection(long startTime, int level)
    {
        try
        {
            if (startTime <= 0)
                return;

            Dictionary<long, int> store = LoadStore();
            if (level > 0)
                store[startTime] = level;
            else
                store.Remove(startTime);

            SaveStore(store);
            Log.Info($"[HeavenMode] Saved Heaven level={level} for startTime={startTime}");
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] SaveSelection failed: {ex}");
        }
    }

    public static int LoadSelection(long startTime)
    {
        try
        {
            if (startTime <= 0)
                return 0;

            Dictionary<long, int> store = LoadStore();
            return store.TryGetValue(startTime, out int level) ? level : 0;
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] LoadSelection failed: {ex}");
            return 0;
        }
    }

    public static void RestoreSelection(long startTime)
    {
        int level = LoadSelection(startTime);
        HeavenState.SelectedOption = level;
        Log.Info($"[HeavenMode] Restored Heaven selection for startTime={startTime}: level={level}");
    }

    public static void ClearCurrentRunSelection()
    {
        try
        {
            long startTime = GetCurrentRunStartTime();
            if (startTime <= 0)
                return;

            Dictionary<long, int> store = LoadStore();
            if (!store.Remove(startTime))
                return;

            SaveStore(store);
            Log.Info($"[HeavenMode] Cleared Heaven entry for startTime={startTime}");
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] ClearCurrentRunSelection failed: {ex}");
        }
    }

    public static void SavePreferredSelection(ModelId characterId, int heavenLevel)
    {
        try
        {
            HeavenPreferenceMetadata metadata = LoadPreferences() ?? new HeavenPreferenceMetadata();
            string key = characterId.ToString();
            int clampedLevel = Math.Clamp(heavenLevel, 0, HeavenState.MaxLevel);

            if (clampedLevel <= 0)
                metadata.CharacterLevels.Remove(key);
            else
                metadata.CharacterLevels[key] = clampedLevel;

            string path = GetPreferencePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(metadata));
            Log.Info($"[HeavenMode] Saved preferred Heaven for {characterId}: level={clampedLevel}");
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] SavePreferredSelection failed: {ex}");
        }
    }

    public static int LoadPreferredSelection(ModelId characterId)
    {
        try
        {
            HeavenPreferenceMetadata? metadata = LoadPreferences();
            if (metadata == null)
                return 0;

            return metadata.CharacterLevels.TryGetValue(characterId.ToString(), out int level)
                ? Math.Clamp(level, 0, HeavenState.MaxLevel)
                : 0;
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] LoadPreferredSelection failed: {ex}");
            return 0;
        }
    }

    private static long GetCurrentRunStartTime()
    {
        var field = AccessTools.Field(typeof(RunManager), "_startTime");
        object? value = field?.GetValue(RunManager.Instance);
        return value is long startTime ? startTime : 0L;
    }

    // Loads the multi-run store (Dictionary<startTime, level>).
    // Migrates transparently from the legacy single-entry format on first read.
    private static Dictionary<long, int> LoadStore()
    {
        string path = GetMetadataPath();
        if (!File.Exists(path))
            return new Dictionary<long, int>();

        string json = File.ReadAllText(path);

        // Try new format first.
        try
        {
            var store = JsonSerializer.Deserialize<Dictionary<long, int>>(json);
            if (store != null)
                return store;
        }
        catch { }

        // Fall back to legacy single-entry format and migrate.
        try
        {
            var legacy = JsonSerializer.Deserialize<LegacyHeavenRunMetadata>(json);
            if (legacy != null && legacy.StartTime > 0 && legacy.HeavenLevel > 0)
            {
                var migrated = new Dictionary<long, int> { [legacy.StartTime] = legacy.HeavenLevel };
                SaveStore(migrated);
                Log.Info($"[HeavenMode] Migrated {SaveFileName} to multi-run format (startTime={legacy.StartTime}, level={legacy.HeavenLevel})");
                return migrated;
            }
        }
        catch { }

        return new Dictionary<long, int>();
    }

    private static void SaveStore(Dictionary<long, int> store)
    {
        string path = GetMetadataPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(store));
    }

    private static HeavenPreferenceMetadata? LoadPreferences()
    {
        string path = GetPreferencePath();
        if (!File.Exists(path))
            return null;

        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<HeavenPreferenceMetadata>(json);
    }

    private static string GetMetadataPath()
    {
        string userPath = UserDataPathProvider.GetProfileScopedPath(
            SaveManager.Instance.CurrentProfileId,
            Path.Combine(UserDataPathProvider.SavesDir, SaveFileName));
        return ProjectSettings.GlobalizePath(userPath);
    }

    private static string GetPreferencePath()
    {
        string userPath = UserDataPathProvider.GetProfileScopedPath(
            SaveManager.Instance.CurrentProfileId,
            Path.Combine(UserDataPathProvider.SavesDir, PreferenceFileName));
        return ProjectSettings.GlobalizePath(userPath);
    }
}
