using Mafia.Models;
using Mafia.Services.Handlers;

namespace Mafia.Services;

public static class BotActionService
{
    private static readonly Random SharedRandom = Random.Shared;

    public static void ProcessBotVotes(LobbyState lobby, GameStage stage)
    {
        switch (stage)
        {
            case GameStage.DayVoting:
                ProcessBotDayVotes(lobby);
                break;
            case GameStage.DayVoting2:
                ProcessBotDayVoting2Votes(lobby);
                break;
            case GameStage.CommissionerTurn:
                ProcessBotCommissionerVotes(lobby);
                break;
            case GameStage.MafiaTurn:
                ProcessBotMafiaVotes(lobby);
                break;
            case GameStage.KillerTurn:
                ProcessBotKillerVote(lobby);
                break;
            case GameStage.BeautyTurn:
                ProcessBotBeautyVote(lobby);
                break;
            case GameStage.DoctorTurn:
                ProcessBotDoctorVote(lobby);
                break;
            case GameStage.NecromancerTurn:
                ProcessBotNecromancerVote(lobby);
                break;
        }
    }

    private static void ProcessBotDayVotes(LobbyState lobby)
    {
        foreach (var bot in lobby.Players.Where(p => p.IsBot && p.IsAlive && p.Role != GameRole.Host))
        {
            if (!lobby.DayVotes.ContainsKey(bot.Id))
            {
                TrySetRandomVote(lobby, bot, p => p.IsAlive && p.Role != GameRole.Host && p.Id != bot.Id, t => lobby.DayVotes[bot.Id] = t.Id);
            }
        }
    }

    private static void ProcessBotDayVoting2Votes(LobbyState lobby)
    {
        foreach (var bot in lobby.Players.Where(p => p.IsBot && p.IsAlive && p.Role != GameRole.Host))
        {
            if (!lobby.DayVotes.ContainsKey(bot.Id))
            {
                TrySetRandomVote(lobby, bot, p => p.IsAlive && p.Role != GameRole.Host && p.Id != bot.Id && lobby.Day1TopVotedPlayerIds.Contains(p.Id), t => lobby.DayVotes[bot.Id] = t.Id);
            }
        }
    }

    private static void ProcessBotCommissionerVotes(LobbyState lobby)
    {
        if (!lobby.PendingCommissionerCheckResult.HasValue && lobby.CommissionerVote is null)
        {
            var targets = lobby.Players.Where(p => p.IsAlive && p.Role != GameRole.Host && p.Role != GameRole.Commissioner).ToList();
            if (targets.Count > 0)
            {
                var target = targets[SharedRandom.Next(targets.Count)];
                if (SharedRandom.Next(2) == 0)
                    lobby.PendingCommissionerCheckResult = target.Role != GameRole.Mafia && target.Role != GameRole.Don;
                else
                    lobby.CommissionerVote = target.Id;
            }
        }
    }

    private static void ProcessBotMafiaVotes(LobbyState lobby)
    {
        foreach (var bot in lobby.Players.Where(p => p.IsBot && p.IsAlive && (p.Role == GameRole.Mafia || p.Role == GameRole.Don)))
        {
            if (!lobby.MafiaVotes.ContainsKey(bot.Id))
            {
                TrySetRandomVote(lobby, bot, p => p.IsAlive && p.Role != GameRole.Host && p.Role != GameRole.Mafia && p.Role != GameRole.Don, t => lobby.MafiaVotes[bot.Id] = t.Id);
            }
        }
    }

    private static void ProcessBotKillerVote(LobbyState lobby)
    {
        if (lobby.KillerVote is null)
            TrySetRandomVote(lobby, null, p => p.IsAlive && p.Role != GameRole.Host && p.Role != GameRole.Killer, t => lobby.KillerVote = t.Id);
    }

    private static void ProcessBotBeautyVote(LobbyState lobby)
    {
        if (lobby.BeautyVote is null)
        {
            TrySetRandomVote(lobby, null, p => p.IsAlive && p.Role != GameRole.Host && p.Role != GameRole.Beauty && p.Id != lobby.LastNightBeautyVote, t => lobby.BeautyVote = t.Id);
        }
    }

    private static void ProcessBotDoctorVote(LobbyState lobby)
    {
        if (lobby.DoctorVote is null)
        {
            TrySetRandomVote(lobby, null, p => p.IsAlive && p.Role != GameRole.Host && p.Role != GameRole.Doctor && p.Id != lobby.LastNightDoctorVote, t => lobby.DoctorVote = t.Id);
        }
    }

    private static void ProcessBotNecromancerVote(LobbyState lobby)
    {
        if (lobby.NecromancerVote is null)
            TrySetRandomVote(lobby, null, p => p.IsAlive && !p.IsZombie && p.Role != GameRole.Host && p.Role != GameRole.Necromancer && p.Role != GameRole.Killer, t => lobby.NecromancerVote = t.Id);
    }

    private static void TrySetRandomVote(LobbyState lobby, PlayerState? bot, Func<PlayerState, bool> filter, Action<PlayerState> setVote)
    {
        var targets = lobby.Players.Where(filter).ToList();
        if (targets.Count > 0)
        {
            setVote(targets[SharedRandom.Next(targets.Count)]);
        }
    }
}