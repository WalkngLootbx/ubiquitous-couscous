using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Lifetime player statistics (Budds earned, screentime, messages sent) plus the saved Budds balance.
/// Creates itself before the scene loads, so it does not need to be placed in the scene,
/// and StatsManager.Instance is ready before any other script's Awake.
/// Data is saved as JSON in Application.persistentDataPath.
/// </summary>
public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance { get; private set; }

    /// <summary>Fired whenever Budds or message stats change (not every screentime tick).</summary>
    public static event Action OnStatsChanged;

    private const string FileName = "stats.json";
    private const float AutoSaveInterval = 30f;

    private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    [Serializable]
    private class StatsData
    {
        public long TotalBuddsEarned;
        public double TotalScreenSeconds;
        public long TotalMessagesSent;
        public int BuddsBalance;
    }

    private StatsData _data = new StatsData();
    private float _autoSaveTimer;

    public long TotalBuddsEarned => _data.TotalBuddsEarned;
    public double TotalScreenSeconds => _data.TotalScreenSeconds;
    public long TotalMessagesSent => _data.TotalMessagesSent;
    public int BuddsBalance => _data.BuddsBalance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        var go = new GameObject(nameof(StatsManager));
        DontDestroyOnLoad(go);
        go.AddComponent<StatsManager>();
    }

    private void Awake()
    {
        Instance = this;
        Load();
    }

    private void Update()
    {
        // Only count time while the app window is focused.
        if (!Application.isFocused) return;

        _data.TotalScreenSeconds += Time.unscaledDeltaTime;

        _autoSaveTimer += Time.unscaledDeltaTime;
        if (_autoSaveTimer >= AutoSaveInterval) Save();
    }

    // --- Recording ---

    public void RecordBuddsEarned(int amount)
    {
        if (amount <= 0) return;
        _data.TotalBuddsEarned += amount;
        Changed();
    }

    public void RecordMessageSent()
    {
        _data.TotalMessagesSent++;
        Changed();
    }

    public void SetBuddsBalance(int balance)
    {
        _data.BuddsBalance = balance;
        Changed();
    }

    private void Changed()
    {
        Save();
        OnStatsChanged?.Invoke();
    }

    // --- Persistence ---

    private void OnApplicationFocus(bool hasFocus) { if (!hasFocus) Save(); }
    private void OnApplicationPause(bool paused) { if (paused) Save(); }
    private void OnApplicationQuit() => Save();

    private void Load()
    {
        if (!File.Exists(SavePath)) return;
        try
        {
            _data = JsonConvert.DeserializeObject<StatsData>(File.ReadAllText(SavePath)) ?? new StatsData();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[StatsManager] Could not read {SavePath}, starting fresh. {e.Message}");
            _data = new StatsData();
        }
    }

    public void Save()
    {
        _autoSaveTimer = 0f;
        try
        {
            // Write to a temp file first so a crash mid-write can't destroy the old stats.
            string tempPath = SavePath + ".tmp";
            File.WriteAllText(tempPath, JsonConvert.SerializeObject(_data, Formatting.Indented));
            if (File.Exists(SavePath)) File.Delete(SavePath);
            File.Move(tempPath, SavePath);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[StatsManager] Could not save stats. {e.Message}");
        }
    }

    // --- Formatting ---

    /// <summary>Formats seconds as hh:mm:ss (hours keep growing past 24).</summary>
    public static string FormatDuration(double seconds)
    {
        long total = (long)seconds;
        return $"{total / 3600:00}:{total % 3600 / 60:00}:{total % 60:00}";
    }
}
