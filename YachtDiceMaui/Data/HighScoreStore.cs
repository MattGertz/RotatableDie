using System.Text.Json;
using YachtDiceMaui.Models;

namespace YachtDiceMaui.Data;

public record struct HighScoreEntry(string Name, int Score, DateTime Date);

public static class HighScoreStore
{
    private static readonly string FilePath = Path.Combine(
        FileSystem.AppDataDirectory, "highscores.json");

    private static Dictionary<string, HighScoreEntry> _scores = new();
    private static bool _loaded;

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                _scores = JsonSerializer.Deserialize<Dictionary<string, HighScoreEntry>>(json)
                           ?? new();
            }
        }
        catch
        {
            _scores = new();
        }
    }

    private static void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(_scores, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // Silently fail — non-critical
        }
    }

    private static string Key(GameMode mode) => mode.ToString();

    public static HighScoreEntry? Get(GameMode mode)
    {
        EnsureLoaded();
        return _scores.TryGetValue(Key(mode), out var entry) ? entry : null;
    }

    /// <summary>
    /// Try to add a high score. Only replaces if the new score is higher.
    /// Returns true if this is a new high score.
    /// </summary>
    public static bool TryAdd(GameMode mode, string name, int score)
    {
        EnsureLoaded();
        string key = Key(mode);
        if (_scores.TryGetValue(key, out var existing) && existing.Score >= score)
            return false;

        _scores[key] = new HighScoreEntry(name, score, DateTime.Now);
        Save();
        return true;
    }

    public static void Clear(GameMode mode)
    {
        EnsureLoaded();
        if (_scores.Remove(Key(mode)))
            Save();
    }
}
