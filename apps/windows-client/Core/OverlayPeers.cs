using System.Text.Json;
namespace ChestTracker.Core;

public record OverlayPeer(string Id, string Name, GameState State, int Count)
{
    public string Label => Name + " | " + (State switch
    {
        GameState.IN_MATCH => $"Truhen: {Count}", GameState.LOBBY => "Lobby", GameState.LOADING => "Lädt",
        GameState.MATCH_END_PENDING => "Rundenende", _ => "Fortnite offen"
    });
    public string Color => State == GameState.IN_MATCH ? "#5E9FF7" : State == GameState.LOBBY ? "#4FC48B" : "#94A2BA";
}

public static class OverlayPeers
{
    public static IReadOnlyList<OverlayPeer> Read(JsonElement dashboard, string ownId)
    {
        if (!dashboard.TryGetProperty("users", out var users) || users.ValueKind != JsonValueKind.Array) return [];
        var peers = new List<OverlayPeer>();
        foreach (var user in users.EnumerateArray())
        {
            if (!user.TryGetProperty("id", out var idValue) || idValue.ValueKind != JsonValueKind.String) continue;
            var id = idValue.GetString(); if (string.IsNullOrEmpty(id) || id == ownId) continue;
            var state = user.TryGetProperty("state", out var stateValue) && stateValue.ValueKind == JsonValueKind.String && Enum.TryParse<GameState>(stateValue.GetString(), out var parsed) ? parsed : GameState.UNKNOWN;
            if (state == GameState.OFFLINE) continue;
            bool running = user.TryGetProperty("fortnite_running", out var window)
                ? window.ValueKind == JsonValueKind.True
                : state is GameState.IN_MATCH or GameState.LOBBY or GameState.LOADING or GameState.MATCH_END_PENDING;
            if (!running) continue;
            var name = user.TryGetProperty("display_name", out var display) && display.ValueKind == JsonValueKind.String ? display.GetString() : null;
            if (string.IsNullOrWhiteSpace(name)) continue;
            int count = user.TryGetProperty("current_chests", out var chests) && chests.ValueKind == JsonValueKind.Number && chests.TryGetInt32(out var n) ? Math.Clamp(n, 0, 200) : 0;
            peers.Add(new(id, name, state, count));
        }
        return peers.DistinctBy(p => p.Id).OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).Take(5).ToArray();
    }
}
