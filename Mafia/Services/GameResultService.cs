using Mafia.Models;

namespace Mafia.Services;

public static class GameResultService
{
    public static Guid? ResolveDayVote(LobbyState lobby) => ResolveVote(lobby, lobby.DayVotes);

    public static Guid? ResolveMafiaVote(LobbyState lobby) => ResolveMafiaVoteCore(lobby, lobby.MafiaVotes);

    public static Guid? ResolveVote(LobbyState lobby, Dictionary<Guid, Guid> votes)
    {
        var necromancer = lobby.Necromancer;
        if (necromancer?.IsAlive == true && lobby.NecromancerVote.HasValue)
        {
            foreach (var voter in lobby.Players.Where(p => p.IsAlive && p.IsZombie))
            {
                if (votes.ContainsKey(voter.Id))
                {
                    votes[voter.Id] = lobby.NecromancerVote.Value;
                }
            }
        }

        var tallies = votes.Values
            .GroupBy(x => x)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        if (tallies.Count == 0) return null;
        if (tallies.Count > 1 && tallies[0].Count == tallies[1].Count) return null;

        return lobby.Players.Any(p => p.Id == tallies[0].Id && p.IsAlive) ? tallies[0].Id : null;
    }

    private static Guid? ResolveMafiaVoteCore(LobbyState lobby, Dictionary<Guid, Guid> votes)
    {
        var tallies = votes.Values
            .GroupBy(x => x)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        if (tallies.Count == 0) return null;

        if (tallies.Count > 1 && tallies[0].Count == tallies[1].Count)
        {
            var donVote = votes.FirstOrDefault(v =>
                lobby.Players.Any(p => p.Id == v.Key && p.IsAlive && p.Role == GameRole.Don));
            if (donVote.Value != Guid.Empty)
            {
                return lobby.Players.Any(p => p.Id == donVote.Value && p.IsAlive) ? donVote.Value : null;
            }
            return null;
        }

        return lobby.Players.Any(p => p.Id == tallies[0].Id && p.IsAlive) ? tallies[0].Id : null;
    }

    public static List<(Guid Id, int Count)> GetDayVoteTallies(LobbyState lobby) =>
        lobby.DayVotes
            .GroupBy(v => v.Value)
            .Select(g => (g.Key, g.Count()))
            .OrderByDescending(x => x.Item2)
            .ToList();

    public static List<Guid> GetDay1TopVotedIds(LobbyState lobby, int maxVotes)
    {
        var tallies = lobby.DayVotes.Values
            .GroupBy(x => x)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        if (tallies.Count == 0 || tallies[0].Count == 0) return [];

        var max = tallies[0].Count;
        return tallies.Where(x => x.Count == max).Select(x => x.Id).ToList();
    }
}