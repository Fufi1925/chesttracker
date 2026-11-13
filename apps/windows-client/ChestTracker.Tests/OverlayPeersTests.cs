using System.Text.Json;
using ChestTracker.Core;
using Xunit;
namespace ChestTracker.Tests;
public sealed class OverlayPeersTests
{
    [Fact] public void OnlyOtherOnlineUsersWithOpenFortniteAppear()
    {
        using var json = JsonDocument.Parse("""
        {"users":[
          {"id":"self","display_name":"Janik","state":"IN_MATCH","fortnite_running":true,"current_chests":5},
          {"id":"a","display_name":"Lio","state":"IN_MATCH","fortnite_running":true,"current_chests":3},
          {"id":"b","display_name":"Johan","state":"UNKNOWN","fortnite_running":true},
          {"id":"c","display_name":"Offline","state":"OFFLINE","fortnite_running":true},
          {"id":"d","display_name":"Nur Client","state":"UNKNOWN","fortnite_running":false}
        ]}
        """);
        var peers = OverlayPeers.Read(json.RootElement, "self");
        Assert.Equal(new[] { "Johan", "Lio" }, peers.Select(p => p.Name));
        Assert.Equal("Lio | Truhen: 3", peers[1].Label); Assert.Equal("Johan | Fortnite offen", peers[0].Label);
    }
    [Fact] public void OlderServerResponsesRemainCompatibleAndDoNotDuplicateUsers()
    {
        using var json = JsonDocument.Parse("""{"users":[{"id":"a","display_name":"Lio","state":"LOBBY"},{"id":"a","display_name":"Lio","state":"LOBBY"},{"id":"b","display_name":"Other","state":"UNKNOWN"}]}""");
        var peer = Assert.Single(OverlayPeers.Read(json.RootElement, "self")); Assert.Equal("Lio | Lobby", peer.Label);
    }
    [Fact] public void ClosedWindowDisappearsEvenIfOldMatchStateRemains()
    {
        using var json = JsonDocument.Parse("""{"users":[{"id":"a","display_name":"Lio","state":"IN_MATCH","fortnite_running":false,"current_chests":4}]}""");
        Assert.Empty(OverlayPeers.Read(json.RootElement, "self"));
    }
}
