using Mafia.Models;

namespace Mafia.Services;

/// <summary>
/// Сервис для управления стадиями игры и обработкой ночи.
/// </summary>
public class MafiaNightService
{
    private readonly MafiaVotingService _votingService = new();

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
            lobby.MafiaVictimId = MafiaVotingService.ResolveMafiaVote(lobby, lobby.MafiaVotes);
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
        _votingService.ProcessBotVotes(lobby, nextStage.Value);
    }

    private static void HandleDayResult(LobbyState lobby)
    {
        lobby.LastDayVictimId = MafiaVotingService.ResolveVote(lobby, lobby.DayVotes);
        
        if (lobby.Stage == GameStage.DayVoting)
        {
            var topVoted = lobby.DayVotes.Values
                .GroupBy(x => x)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();
            
            if (topVoted.Count > 0 && topVoted[0].Count > 0)
            {
                var maxVotes = topVoted[0].Count;
                lobby.Day1TopVotedPlayerIds = topVoted
                    .Where(x => x.Count == maxVotes)
                    .Select(x => x.Id)
                    .ToList();
            }
        }
        
        if (lobby.LastDayVictimId.HasValue)
        {
            KillPlayer(lobby, lobby.LastDayVictimId.Value);
        }
        
        AddDayResultToHistory(lobby);
    }

    private void HandleNightResult(LobbyState lobby)
    {
        lobby.PendingCommissionerCheckResult = null;
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
        ApplyNightActions(lobby);
        if (MafiaWinConditionService.TrySetWinner(lobby))
        {
            return;
        }

        lobby.Stage = GameStage.NightResult;
    }

    private static void ZombiePlayer(LobbyState lobby, Guid id)
    {
        var player = lobby.Players.FirstOrDefault(p => p.Id == id);
        if (player is not null && player is { IsAlive: false, IsZombie: false })
        {
            player.IsAlive = true;
            player.IsZombie = true;
            lobby.LastKilledPlayers.Remove(player.Id);
            lobby.LastResurrectedPlayers.Add(player.Id);
        }
    }

    private static void KillPlayer(LobbyState lobby, Guid id)
    {
        var player = lobby.Players.FirstOrDefault(p => p.Id == id);
        if (player is not null && player.IsAlive)
        {
            player.IsAlive = false;
            lobby.LastKilledPlayers.Add(player.Id);
            if (player.Role == GameRole.Necromancer)
            {
                foreach (var zombie in lobby.Players.Where(x=>x.IsAlive && x.IsZombie))
                {
                    zombie.IsAlive = false;
                    lobby.LastKilledPlayers.Add(zombie.Id);
                }
            }
        }
    }

    private static void ApplyNightActions(LobbyState lobby)
    {
        lobby.LastNightResultTexts.Clear();
        lobby.LastKilledPlayers.Clear();
        
        if (lobby.BeautyVote.HasValue)
        {
            var victim = lobby.Players.FirstOrDefault(p => p.Id == lobby.BeautyVote.Value);
            if (victim is not null)
                lobby.LastNightResultTexts.Add($"💅 Красотка выбрала: {victim.Name}");
        }

        if (lobby.DoctorVote.HasValue)
        {
            var doctorTarget = lobby.Players.FirstOrDefault(p => p.Id == lobby.DoctorVote.Value);
            if (doctorTarget is not null)
                lobby.LastNightResultTexts.Add($"⚕️ Доктор выбрал: {doctorTarget.Name}");
        }

        if (lobby.CommissionerVote.HasValue)
        {
            var commissionerTarget = lobby.Players.FirstOrDefault(p => p.Id == lobby.CommissionerVote.Value);
            if (commissionerTarget is not null)
            {
                if (lobby.CommissionerIsKill == true)
                {
                    lobby.LastNightResultTexts.Add($"🔪 Комиссар выбрал: {commissionerTarget.Name}");
                }
                else
                {
                    var isMafia = commissionerTarget.IsMafia();
                    lobby.CommissionerChecks[commissionerTarget.Id] = isMafia;
                    commissionerTarget.IsMafiaChecked = isMafia;
                    var result = isMafia ? "Мафия" : "Мирный";
                    lobby.LastNightResultTexts.Add($"🔍 Комиссар проверил: {commissionerTarget.Name} - {result}");
                }
            }
        }

        var mafiaVoters = lobby.MafiaVotes.Count;
        if (mafiaVoters > 0)
        {
            var mafiaTarget = lobby.MafiaVictimId.HasValue 
                ? lobby.Players.FirstOrDefault(p => p.Id == lobby.MafiaVictimId.Value)?.Name 
                : "не выбрали";
            lobby.LastNightResultTexts.Add($"🎭 Мафия (@{mafiaVoters} чел.): {mafiaTarget}");
        }

        if (lobby.KillerVote.HasValue)
        {
            var killerTarget = lobby.Players.FirstOrDefault(p => p.Id == lobby.KillerVote.Value);
            if (killerTarget is not null)
                lobby.LastNightResultTexts.Add($"🔪 Убийца выбрал: {killerTarget.Name}");
        }

        if (lobby.NecromancerVote.HasValue)
        {
            var necromancerTarget = lobby.Players.FirstOrDefault(p => p.Id == lobby.NecromancerVote.Value);
            if (necromancerTarget is not null)
                lobby.LastNightResultTexts.Add($"🧟 Некромант выбрал: {necromancerTarget.Name}");
        }

        var beautyTargetId = lobby.BeautyVote;
        var doctorTargetId = beautyTargetId == lobby.Doctor?.Id ? null : lobby.DoctorVote;
        var commissionerVictimId =
            lobby.CommissionerIsKill != true ||
            beautyTargetId == lobby.Commissioner?.Id ||
            beautyTargetId == lobby.CommissionerVote ||
            doctorTargetId == lobby.CommissionerVote
                ? null
                : lobby.CommissionerVote;

        if (commissionerVictimId.HasValue)
        {
            KillPlayer(lobby, commissionerVictimId.Value);
        }
        
        var mafiaAlive = lobby.Players.Count(p =>
            p.IsAlive && (p.Role == GameRole.Mafia || p.Role == GameRole.Don) &&
            p.Id != beautyTargetId);
        
        var mafiaVictimId =
            mafiaAlive == 0 ||
            beautyTargetId == lobby.MafiaVictimId ||
            doctorTargetId == lobby.MafiaVictimId
                ? null
                : lobby.MafiaVictimId;
        
        if (mafiaVictimId.HasValue)
        {
            KillPlayer(lobby, mafiaVictimId.Value);
        }
        
        var killerVictimId  =
            !(lobby.Killer?.IsAlive ?? false) ||
            beautyTargetId == lobby.Killer?.Id ||
            beautyTargetId == lobby.KillerVote ||
            doctorTargetId == lobby.KillerVote
                ? null
                : lobby.KillerVote;

        if (killerVictimId.HasValue)
        {
            KillPlayer(lobby, killerVictimId.Value);
        }

        var necromancerTargetId =
            !(lobby.Necromancer?.IsAlive ?? false) ||
            beautyTargetId == lobby.Necromancer?.Id
                ? null
                : lobby.NecromancerVote;
        
        if (necromancerTargetId.HasValue)
        {
            var zombieCandidate = lobby.LastKilledPlayers.FirstOrDefault(p => p == necromancerTargetId.Value);
            if (zombieCandidate is not null)
            {
                ZombiePlayer(lobby, zombieCandidate.Value);
            }
        }

        if (lobby.LastKilledPlayers.Count == 0 && lobby.LastResurrectedPlayers.Count == 0)
        {
            lobby.LastNightResultTexts.Add("🌙 Никто не пострадал.");
        }
        else
        {
            if (lobby.LastKilledPlayers.Count > 0)
            {
                var killedNames = lobby.LastKilledPlayers
                    .Select(id => lobby.Players.FirstOrDefault(p => p.Id == id)?.Name)
                    .Where(n => n is not null)
                    .ToList();
                if (killedNames.Count > 0)
                {
                    lobby.LastNightResultTexts.Add($"💀 Убит{(killedNames.Count == 1 ? "" : "ы")}: {string.Join(", ", killedNames)}");
                }
            }
            
            if (lobby.LastResurrectedPlayers.Count > 0)
            {
                var resurrectedNames = lobby.LastResurrectedPlayers
                    .Select(id => lobby.Players.FirstOrDefault(p => p.Id == id)?.Name)
                    .Where(n => n is not null)
                    .ToList();
                if (resurrectedNames.Count > 0)
                {
                    lobby.LastNightResultTexts.Add($"✨ Воскрес{(resurrectedNames.Count == 1 ? "" : "и")}: {string.Join(", ", resurrectedNames)}");
                }
            }
        }
        
        lobby.StageHistory.Add(new StageResultEntry
        {
            Round = lobby.Round,
            StageText = "Ночные действия",
            ResultText = lobby.LastNightResultTexts.ToList()
        });
    }

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