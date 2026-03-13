using System.Collections.Generic;

namespace HeavenMode;

/// <summary>
/// Shared runtime state for the Heaven selection layered on top of official ascension 0.
/// SelectedOption: 0 = Off, 1 = Heaven 1, 2 = Heaven 2
/// </summary>
internal static class HeavenState
{
    public static int SelectedOption { get; set; } = 0;

    public static int GetEffectiveAscension(int officialAscension)
    {
        if (officialAscension == 0 && SelectedOption >= 1)
            return 10;

        return officialAscension;
    }

    // Per-player flag: set just before the Neow heal, consumed when HP would exceed 10.
    // This lets us clamp the Neow heal to 10 without affecting subsequent in-run heals.
    private static readonly HashSet<ulong> _pendingHpPlayers = new();

    public static void MarkPendingHp(ulong playerId) => _pendingHpPlayers.Add(playerId);
    public static bool HasPendingHp(ulong playerId) => _pendingHpPlayers.Contains(playerId);
    public static void ClearPendingHp(ulong playerId) => _pendingHpPlayers.Remove(playerId);
}
