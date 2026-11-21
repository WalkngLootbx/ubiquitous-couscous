using TMPro;
using UnityEngine;

/// <summary>
/// Shows lifetime stats from StatsManager. Put this on the Statistics page root (or any child)
/// and assign the three text fields. It refreshes whenever the page is enabled.
/// </summary>
public class StatisticsPage : MonoBehaviour
{
    [SerializeField] private TMP_Text buddsEarnedText;
    [SerializeField] private TMP_Text screentimeText;
    [SerializeField] private TMP_Text messagesSentText;

    [Header("Formats ({0} = value)")]
    [SerializeField] private string buddsFormat = "{0}";
    [SerializeField] private string screentimeFormat = "{0}";
    [SerializeField] private string messagesFormat = "{0}";

    private long _lastShownSecond = -1;

    private void OnEnable()
    {
        StatsManager.OnStatsChanged += RefreshCounts;
        RefreshCounts();
        RefreshScreentime(force: true);
    }

    private void OnDisable()
    {
        StatsManager.OnStatsChanged -= RefreshCounts;
    }

    // Screentime keeps ticking while the page is open.
    private void Update() => RefreshScreentime(force: false);

    private void RefreshCounts()
    {
        var stats = StatsManager.Instance;
        if (buddsEarnedText) buddsEarnedText.text = string.Format(buddsFormat, stats.TotalBuddsEarned);
        if (messagesSentText) messagesSentText.text = string.Format(messagesFormat, stats.TotalMessagesSent);
    }

    private void RefreshScreentime(bool force)
    {
        if (!screentimeText) return;
        double seconds = StatsManager.Instance.TotalScreenSeconds;
        long whole = (long)seconds;
        if (!force && whole == _lastShownSecond) return; // only rebuild the string once per second
        _lastShownSecond = whole;
        screentimeText.text = string.Format(screentimeFormat, StatsManager.FormatDuration(seconds));
    }
}
