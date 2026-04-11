using Mafia.Models;

namespace Mafia.Services;

/// <summary>
/// Сервис для управления стадиями игры и обработкой ночи.
/// </summary>
public class MafiaNightService
{
    /// <summary>
    /// Переход к следующей стадии игры.
    /// </summary>
    public bool NextStage(LobbyState lobby, out string? error)
    {
        error = null;

        switch (lobby.Stage)
        {
            case GameStage.NightResult:
                HandleNightResult(lobby);
                break;
            case GameStage.Discussion:
            case GameStage.DayVoting:
            case GameStage.DiscussionBeforeSecondVote:
            case GameStage.DayVoting2:
            case GameStage.NightStart:
            case GameStage.BeautyTurn:
            case GameStage.DoctorTurn:
            case GameStage.CommissionerTurn:
            case GameStage.MafiaTurn:
            case GameStage.KillerTurn:
            case GameStage.NecromancerTurn:
                AdvanceStage(lobby);
                break;
            default:
                error = "Сейчас нельзя перейти к следующей стадии.";
                return false;
        }
        return true;
    }

    /// <summary>Получить следующую стадию после текущей.</summary>
    public static GameStage? GetNextStage(GameStage currentStage) => currentStage switch
    {
        GameStage.Discussion => GameStage.DayVoting,
        GameStage.DayVoting => GameStage.DiscussionBeforeSecondVote,
        GameStage.DiscussionBeforeSecondVote => GameStage.DayVoting2,
        GameStage.DayVoting2 => GameStage.NightStart,
        GameStage.NightStart => GameStage.BeautyTurn,
        GameStage.BeautyTurn => GameStage.DoctorTurn,
        GameStage.DoctorTurn => GameStage.CommissionerTurn,
        GameStage.CommissionerTurn => GameStage.MafiaTurn,
        GameStage.MafiaTurn => GameStage.KillerTurn,
        GameStage.KillerTurn => GameStage.NecromancerTurn,
        _ => null
    };

    /// <summary>Получить роли, участвующие в указанной стадии.</summary>
    public static List<GameRole> GetRolesForStage(GameStage stage) => stage switch
    {
        GameStage.Discussion => [GameRole.Civilian, GameRole.Mafia, GameRole.Don, GameRole.Commissioner, GameRole.Killer, GameRole.Beauty, GameRole.Doctor, GameRole.Necromancer],
        GameStage.DayVoting => [GameRole.Civilian, GameRole.Mafia, GameRole.Don, GameRole.Commissioner, GameRole.Killer, GameRole.Beauty, GameRole.Doctor, GameRole.Necromancer],
        GameStage.DiscussionBeforeSecondVote => [GameRole.Civilian, GameRole.Mafia, GameRole.Don, GameRole.Commissioner, GameRole.Killer, GameRole.Beauty, GameRole.Doctor, GameRole.Necromancer],
        GameStage.DayVoting2 => [GameRole.Civilian, GameRole.Mafia, GameRole.Don, GameRole.Commissioner, GameRole.Killer, GameRole.Beauty, GameRole.Doctor, GameRole.Necromancer],
        GameStage.NightStart => [GameRole.Civilian, GameRole.Mafia, GameRole.Don, GameRole.Commissioner, GameRole.Killer, GameRole.Beauty, GameRole.Doctor, GameRole.Necromancer],
        GameStage.BeautyTurn => [GameRole.Beauty],
        GameStage.DoctorTurn => [GameRole.Doctor],
        GameStage.CommissionerTurn => [GameRole.Commissioner],
        GameStage.MafiaTurn => [GameRole.Mafia, GameRole.Don],
        GameStage.KillerTurn => [GameRole.Killer],
        GameStage.NecromancerTurn => [GameRole.Necromancer],
        GameStage.NightResult => [GameRole.Civilian, GameRole.Mafia, GameRole.Don, GameRole.Commissioner, GameRole.Killer, GameRole.Beauty, GameRole.Doctor, GameRole.Necromancer],
        _ => []
    };

    /// <summary>Получить игроков для указанной стадии.</summary>
    public static List<PlayerState> GetPlayersForStage(LobbyState lobby, GameStage stage)
    {
        var roles = GetRolesForStage(stage);
        if (roles.Count == 0) return [];
        return lobby.Players.Where(p => p.IsAlive && roles.Contains(p.Role)).ToList();
    }

    private void AdvanceStage(LobbyState lobby)
    {
        var nextStage = GetNextStage(lobby.Stage);
        
        if (nextStage == GameStage.DiscussionBeforeSecondVote && lobby.LastDayVictimId != null)
        {
            nextStage = GameStage.NightStart;
        }
        
        if (nextStage is null)
        {
            EndNight(lobby);
            return;
        }

        var players = GetPlayersForStage(lobby, nextStage.Value);
        if (players.Count == 0)
        {
            lobby.Stage = nextStage.Value;
            AdvanceStage(lobby);
            return;
        }

        if (lobby.Stage == GameStage.MafiaTurn)
        {
            lobby.MafiaVictimId = GameResultService.ResolveMafiaVote(lobby);
        }

        if (lobby.Stage == GameStage.DayVoting)
        {
            HandleDayResult(lobby);
            if(lobby.LastDayVictimId != null)
                nextStage = GameStage.NightStart;
        }
        
        if (lobby.Stage == GameStage.DayVoting2 && lobby.LastDayVictimId == null)
        {
            HandleDayResult(lobby);
        }

        if (lobby.Stage == GameStage.Discussion)
        {
            ClearAllVotes(lobby);
        }
        
        if (lobby.Stage == GameStage.DiscussionBeforeSecondVote)
        {
            lobby.DayVotes.Clear();
        }

        if (MafiaWinConditionService.TrySetWinner(lobby))
        {
            return;
        }
        
        lobby.StageEndsAtUtc = DateTimeOffset.UtcNow.AddSeconds(lobby.Stage.GetSeconds(lobby));
        lobby.Stage = nextStage.Value;
        BotActionService.ProcessBotVotes(lobby, nextStage.Value);
    }

    private static void HandleDayResult(LobbyState lobby)
    {
        lobby.LastDayVictimId = GameResultService.ResolveDayVote(lobby);
        
        if (lobby.Stage == GameStage.DayVoting)
        {
            lobby.Day1TopVotedPlayerIds = GameResultService.GetDay1TopVotedIds(lobby, 1);
        }
        
        if (lobby.LastDayVictimId.HasValue)
        {
            KillPlayer(lobby, lobby.LastDayVictimId.Value);
        }
        
        AddDayResultToHistory(lobby);
    }

    private void HandleNightResult(LobbyState lobby)
    {
        lobby.LastCommissionerCheckRound = 0;
        lobby.Round++;
        lobby.Stage = GameStage.Discussion;
        lobby.StageEndsAtUtc = DateTimeOffset.UtcNow.AddSeconds(lobby.DiscussionSeconds);
    }

    private static void ClearAllVotes(LobbyState lobby)
    {
        lobby.DayVotes.Clear();
        lobby.LastDayVictimId = null;
        lobby.CommissionerVote = null;
        lobby.CommissionerIsKill = false;
        lobby.MafiaVotes.Clear();
        lobby.MafiaVictimId = null;
        lobby.KillerVote = null;
        lobby.BeautyVote = null;
        lobby.DoctorVote = null;
        lobby.NecromancerVote = null;
        lobby.LastNightResultTexts.Clear();
        lobby.LastKilledPlayers.Clear();
        lobby.LastResurrectedPlayers.Clear();
    }

    private void EndNight(LobbyState lobby)
    {
        NightActionService.ApplyNightActions(lobby);
        if (MafiaWinConditionService.TrySetWinner(lobby))
        {
            return;
        }

        lobby.Stage = GameStage.NightResult;
    }

    private static void KillPlayer(LobbyState lobby, Guid id) =>
        NightActionService.KillPlayer(lobby, id);

    private static void AddDayResultToHistory(LobbyState lobby)
    {
        var resultTexts = new List<string>();
        
        var voteCounts = lobby.DayVotes
            .GroupBy(v => v.Value)
            .Select(g => new { PlayerId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        foreach (var vc in voteCounts)
        {
            var target = lobby.Players.FirstOrDefault(p => p.Id == vc.PlayerId);
            if (target is not null)
            {
                resultTexts.Add($"🗳️ {target.Name}: {vc.Count} голос({(vc.Count == 1 ? "" : "ов")})");
            }
        }

        if (lobby.LastDayVictimId.HasValue)
        {
            var victimName = PlayerName(lobby, lobby.LastDayVictimId.Value);
            resultTexts.Add($"\n🚫 Исключён: {victimName}");
        }
        else
        {
            resultTexts.Add("\n🚫 Никого не исключили.");
        }

        var stageText = lobby.Stage == GameStage.DayVoting2 ? "Второе дневное голосование" : "Дневное голосование";
        
        lobby.StageHistory.Add(new StageResultEntry
        {
            Round = lobby.Round,
            StageText = stageText,
            ResultText = resultTexts
        });
    }

    private static string PlayerName(LobbyState lobby, Guid playerId) =>
        lobby.Players.FirstOrDefault(p => p.Id == playerId)?.Name ?? "Неизвестный игрок";
}