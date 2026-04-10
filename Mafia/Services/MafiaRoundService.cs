using Mafia.Models;

namespace Mafia.Services;

public class MafiaRoundService
{
    private static readonly Random SharedRandom = Random.Shared;

    private (bool IsValid, string? Error) ValidateTarget(LobbyState lobby, PlayerState player, Guid targetId, Func<PlayerState, bool>? excludeRole = null, bool excludeSelf = false)
    {
        if (player.IsDead() || player.IsHost()) return (false, "Нельзя голосовать.");
        
        var target = lobby.GetPlayer(targetId);
        if (target is null || !target.IsAlive || target.IsHost()) return (false, "Неверная цель.");
        if (excludeSelf && targetId == player.Id) return (false, "Нельзя выбрать себя.");
        if (excludeRole is not null && excludeRole(target)) return (false, "Нельзя выбрать этого игрока.");
        
        return (true, null);
    }

    public bool StartGame(LobbyState lobby, out string? error)
    {
        error = null;
        var activePlayers = lobby.Players.Where(p => p.Role != GameRole.Host).ToList();
        if (activePlayers.Count < 3)
        {
            error = "Для старта игры нужно минимум 3 игрока, не считая ведущего.";
            return false;
        }

        foreach (var p in activePlayers)
        {
            p.Role = GameRole.Civilian;
            p.IsAlive = true;
        }

        var mafiaCount = Math.Max(1, activePlayers.Count / 3);
        var shuffled = activePlayers.OrderBy(_ => SharedRandom.Next()).ToList();
        for (int i = 0; i < mafiaCount; i++)
        {
            shuffled[i].Role = i == 0 ? GameRole.Don : GameRole.Mafia;
        }
        var maniacCandidate = shuffled.Skip(mafiaCount).FirstOrDefault();
        if (maniacCandidate is not null)
        {
            maniacCandidate.Role = GameRole.Killer;
        }
        
        var civilianPlayers = shuffled.Where(p => p.Role == GameRole.Civilian).ToList();
        if (civilianPlayers.Count > 0)
        {
            var commissioner = civilianPlayers[SharedRandom.Next(civilianPlayers.Count)];
            commissioner.Role = GameRole.Commissioner;
            civilianPlayers.Remove(commissioner);
        }

        if (civilianPlayers.Count > 0)
        {
            var beauty = civilianPlayers[SharedRandom.Next(civilianPlayers.Count)];
            beauty.Role = GameRole.Beauty;
            civilianPlayers.Remove(beauty);
        }

        if (civilianPlayers.Count > 0)
        {
            var doctor = civilianPlayers[SharedRandom.Next(civilianPlayers.Count)];
            doctor.Role = GameRole.Doctor;
            civilianPlayers.Remove(doctor);
        }

        if (civilianPlayers.Count > 0)
        {
            var necromancer = civilianPlayers[SharedRandom.Next(civilianPlayers.Count)];
            necromancer.Role = GameRole.Necromancer;
        }

        lobby.Round = 1;
        lobby.Stage = GameStage.Discussion;
        lobby.StageEndsAtUtc = DateTimeOffset.UtcNow.AddSeconds(lobby.DiscussionSeconds);
        ClearAllVotes(lobby);
        return true;
    }

    public bool DayVote(LobbyState lobby, PlayerState player, Guid targetId, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.DayVoting && lobby.Stage != GameStage.DayVoting2 || player.IsDead() || player.IsHost()) { error = "Сейчас нельзя голосовать."; return false; }

        if (lobby.Stage == GameStage.DayVoting2 && lobby.Day1TopVotedPlayerIds.Count > 0)
        {
            if (!lobby.Day1TopVotedPlayerIds.Contains(targetId))
            {
                error = "Во второй день можно голосовать только за лидеров первого дня.";
                return false;
            }
        }

        var (isValid, validationError) = ValidateTarget(lobby, player, targetId);
        if (!isValid) { error = validationError ?? "Неверная цель."; return false; }

        lobby.DayVotes[player.Id] = targetId;
        return true;
    }

    public bool CancelDayVote(LobbyState lobby, PlayerState player, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.DayVoting && lobby.Stage != GameStage.DayVoting2 || player.IsDead() || player.IsHost()) { error = "Сейчас нельзя отменить."; return false; }
        lobby.DayVotes.Remove(player.Id);
        return true;
    }

    public bool MafiaVote(LobbyState lobby, PlayerState player, Guid targetId, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.MafiaTurn || player.IsDead() || !player.IsMafia()) { error = "Сейчас ход мафии недоступен."; return false; }

        var (isValid, validationError) = ValidateTarget(lobby, player, targetId, p => p.IsMafia());
        if (!isValid) { error = validationError ?? "Мафия может голосовать только против живых не-мафиози."; return false; }

        lobby.MafiaVotes[player.Id] = targetId;
        return true;
    }

    public bool CancelMafiaVote(LobbyState lobby, PlayerState player, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.MafiaTurn || player.IsDead() || !player.IsMafia()) { error = "Сейчас нельзя отменить."; return false; }
        lobby.MafiaVotes.Remove(player.Id);
        return true;
    }

    public bool ManiacVote(LobbyState lobby, PlayerState player, Guid targetId, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.KillerTurn || player.IsDead() || !player.IsKiller()) { error = "Сейчас ход маньяка недоступен."; return false; }

        var (isValid, validationError) = ValidateTarget(lobby, player, targetId, p => p.IsKiller(), excludeSelf: true);
        if (!isValid) { error = validationError ?? "Неверная цель."; return false; }

        lobby.KillerVote = targetId;
        return true;
    }

    public bool CancelManiacVote(LobbyState lobby, PlayerState player, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.KillerTurn || player.IsDead() || !player.IsKiller()) { error = "Сейчас нельзя отменить."; return false; }
        lobby.KillerVote = null;
        return true;
    }

    public bool CommissionerAction(LobbyState lobby, PlayerState player, Guid targetId, bool isKill, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.CommissionerTurn || player.IsDead() || !player.IsCommissioner()) { error = "Сейчас ход комиссара недоступен."; return false; }

        var (isValid, validationError) = ValidateTarget(lobby, player, targetId, excludeSelf: true);
        if (!isValid) { error = validationError ?? "Неверная цель."; return false; }

        lobby.CommissionerVote = targetId;
        lobby.CommissionerIsKill = isKill;
        lobby.PendingCommissionerCheckResult = null;
        return true;
    }

    public bool SetCommissionerIsKill(LobbyState lobby, PlayerState player, bool isKill, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.CommissionerTurn || player.IsDead() || !player.IsCommissioner()) { error = "Сейчас ход комиссара недоступен."; return false; }

        lobby.CommissionerIsKill = isKill;
        lobby.PendingCommissionerCheckResult = null;
        return true;
    }

    public bool CommissionerVote(LobbyState lobby, PlayerState player, Guid targetId, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.CommissionerTurn || player.IsDead() || !player.IsCommissioner()) { error = "Сейчас ход комиссара недоступен."; return false; }

        lobby.CommissionerVote = targetId;
        lobby.PendingCommissionerCheckResult = null;
        return true;
    }

    public bool CancelCommissionerVote(LobbyState lobby, PlayerState player, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.CommissionerTurn || player.IsDead() || !player.IsCommissioner()) { error = "Сейчас нельзя отменить."; return false; }
        lobby.CommissionerVote = null;
        lobby.CommissionerIsKill = false;
        lobby.PendingCommissionerCheckResult = null;
        return true;
    }

    public bool BeautyAction(LobbyState lobby, PlayerState player, Guid targetId, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.BeautyTurn || player.IsDead() || !player.IsBeauty()) { error = "Сейчас ход красотки недоступен."; return false; }

        var (isValid, validationError) = ValidateTarget(lobby, player, targetId, excludeSelf: true);
        if (!isValid) { error = validationError ?? "Неверная цель."; return false; }
        if (lobby.LastNightBeautyVote == targetId) { error = "Нельзя выбирать одного игрока 2 ночи подряд."; return false; }

        lobby.BeautyVote = targetId;
        return true;
    }

    public bool CancelBeautyVote(LobbyState lobby, PlayerState player, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.BeautyTurn || player.IsDead() || !player.IsBeauty()) { error = "Сейчас нельзя отменить."; return false; }
        lobby.BeautyVote = null;
        return true;
    }

    public bool DoctorAction(LobbyState lobby, PlayerState player, Guid targetId, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.DoctorTurn || player.IsDead() || !player.IsDoctor()) { error = "Сейчас ход доктора недоступен."; return false; }

        var (isValid, validationError) = ValidateTarget(lobby, player, targetId);
        if (!isValid) { error = validationError ?? "Неверная цель."; return false; }
        if (lobby.LastNightDoctorVote == targetId) { error = "Нельзя выбирать одного игрока 2 ночи подряд."; return false; }

        lobby.DoctorVote = targetId;
        return true;
    }

    public bool CancelDoctorVote(LobbyState lobby, PlayerState player, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.DoctorTurn || player.IsDead() || !player.IsDoctor()) { error = "Сейчас нельзя отменить."; return false; }
        lobby.DoctorVote = null;
        return true;
    }

    public bool NecromancerAction(LobbyState lobby, PlayerState player, Guid targetId, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.NecromancerTurn || player.IsDead() || !player.IsNecromancer()) { error = "Сейчас ход некроманта недоступен."; return false; }

        var (isValid, validationError) = ValidateTarget(lobby, player, targetId, excludeSelf: true);
        if (!isValid) { error = validationError ?? "Неверная цель."; return false; }

        lobby.NecromancerVote = targetId;
        return true;
    }

    public bool CancelNecromancerVote(LobbyState lobby, PlayerState player, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.NecromancerTurn || player.IsDead() || !player.IsNecromancer()) { error = "Сейчас нельзя отменить."; return false; }
        lobby.NecromancerVote = null;
        return true;
    }

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
            lobby.MafiaVictimId = ResolveMafiaVote(lobby, lobby.MafiaVotes);
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

        if (TrySetWinner(lobby))
        {
            return;
        }
        lobby.StageEndsAtUtc = DateTimeOffset.UtcNow.AddSeconds(lobby.Stage.GetSeconds(lobby));
        lobby.Stage = nextStage.Value;
        ProcessBotVotes(lobby, nextStage.Value);
    }

    private static GameStage? GetNextStage(GameStage currentStage) => currentStage switch
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

    private static List<PlayerState> GetPlayersForStage(LobbyState lobby, GameStage stage)
    {
        var roles = GetRolesForStage(stage);
        if (roles.Count == 0) return [];
        return lobby.Players.Where(p => p.IsAlive && roles.Contains(p.Role)).ToList();
    }

    private static List<GameRole> GetRolesForStage(GameStage stage) => stage switch
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

    private void HandleDayResult(LobbyState lobby)
    {
        lobby.LastDayVictimId = ResolveVote(lobby, lobby.DayVotes);
        
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
        if (TrySetWinner(lobby))
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

    private static Guid? ResolveVote(LobbyState lobby, Dictionary<Guid, Guid> votes)
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

        if (tallies.Count == 0)
        {
            return null;
        }

        if (tallies.Count > 1 && tallies[0].Count == tallies[1].Count)
        {
            return null;
        }

        return lobby.Players.Any(p => p.Id == tallies[0].Id && p.IsAlive) ? tallies[0].Id : null;
    }

    private static Guid? ResolveMafiaVote(LobbyState lobby, Dictionary<Guid, Guid> votes)
    {
        var tallies = votes.Values
            .GroupBy(x => x)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        if (tallies.Count == 0)
        {
            return null;
        }

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

    private static bool TrySetWinner(LobbyState lobby)
    {
        var mafiaAlive = lobby.Players.Count(p => p.IsAlive && (p.Role == GameRole.Mafia || p.Role == GameRole.Don));
        var maniacAlive = lobby.Players.Count(p => p.IsAlive && (p.Role == GameRole.Killer || p.Role == GameRole.Necromancer));
        var civilianAlive = lobby.Players.Count(p => p.IsAlive && p.Role != GameRole.Mafia && p.Role != GameRole.Don && p.Role != GameRole.Killer && p.Role != GameRole.Necromancer && p.Role != GameRole.Host);

        if (mafiaAlive > 0 && mafiaAlive >= civilianAlive + maniacAlive)
        {
            lobby.Stage = GameStage.GameOver;
            lobby.WinnerText = "Победа мафии!";
            return true;
        }

        if (maniacAlive > 0 && civilianAlive == 0 && mafiaAlive == 0)
        {
            lobby.Stage = GameStage.GameOver;
            lobby.WinnerText = "Победа маньяков!";
            return true;
        }

        if (civilianAlive > 0 && mafiaAlive == 0 && maniacAlive == 0)
        {
            lobby.Stage = GameStage.GameOver;
            lobby.WinnerText = "Победа мирных жителей!";
            return true;
        }

        if (civilianAlive == 0 && mafiaAlive == 0 && maniacAlive == 0)
        {
            lobby.Stage = GameStage.GameOver;
            lobby.WinnerText = "Ничья";
            return true;
        }

        return false;
    }

    public void ProcessBotVotes(LobbyState lobby, GameStage stage)
    {
        switch (stage)
        {
            case GameStage.DayVoting:
                foreach (var bot in lobby.Players.Where(p => p.IsBot && p.IsAlive && p.Role != GameRole.Host))
                {
                    if (!lobby.DayVotes.ContainsKey(bot.Id))
                    {
                        TrySetRandomVote(lobby, bot, p => p.IsAlive && p.Role != GameRole.Host && p.Id != bot.Id, t => lobby.DayVotes[bot.Id] = t.Id);
                    }
                }
                break;
            
            case GameStage.DayVoting2:
                foreach (var bot in lobby.Players.Where(p => p.IsBot && p.IsAlive && p.Role != GameRole.Host))
                {
                    if (!lobby.DayVotes.ContainsKey(bot.Id))
                    {
                        TrySetRandomVote(lobby, bot, p => p.IsAlive && p.Role != GameRole.Host && p.Id != bot.Id && lobby.Day1TopVotedPlayerIds.Contains(p.Id), t => lobby.DayVotes[bot.Id] = t.Id);
                    }
                }
                break;

            case GameStage.CommissionerTurn:
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
                break;

            case GameStage.MafiaTurn:
                foreach (var bot in lobby.Players.Where(p => p.IsBot && p.IsAlive && (p.Role == GameRole.Mafia || p.Role == GameRole.Don)))
                {
                    if (!lobby.MafiaVotes.ContainsKey(bot.Id))
                    {
                        TrySetRandomVote(lobby, bot, p => p.IsAlive && p.Role != GameRole.Host && p.Role != GameRole.Mafia && p.Role != GameRole.Don, t => lobby.MafiaVotes[bot.Id] = t.Id);
                    }
                }
                break;

            case GameStage.KillerTurn:
                if (lobby.KillerVote is null)
                    TrySetRandomVote(lobby, null, p => p.IsAlive && p.Role != GameRole.Host && p.Role != GameRole.Killer, t => lobby.KillerVote = t.Id);
                break;

            case GameStage.BeautyTurn:
                if (lobby.BeautyVote is null)
                {
                    TrySetRandomVote(lobby, null, p => p.IsAlive && p.Role != GameRole.Host && p.Role != GameRole.Beauty && p.Id != lobby.LastNightBeautyVote, t => lobby.BeautyVote = t.Id);
                }
                break;

            case GameStage.DoctorTurn:
                if (lobby.DoctorVote is null)
                {
                    TrySetRandomVote(lobby, null, p => p.IsAlive && p.Role != GameRole.Host && p.Role != GameRole.Doctor && p.Id != lobby.LastNightDoctorVote, t => lobby.DoctorVote = t.Id);
                }
                break;

            case GameStage.NecromancerTurn:
                if (lobby.NecromancerVote is null)
                    TrySetRandomVote(lobby, null, p => p.IsAlive && !p.IsZombie && p.Role != GameRole.Host && p.Role != GameRole.Necromancer && p.Role != GameRole.Killer, t => lobby.NecromancerVote = t.Id);
                break;
        }
    }

    private void TrySetRandomVote(LobbyState lobby, PlayerState? bot, Func<PlayerState, bool> filter, Action<PlayerState> setVote)
    {
        var targets = lobby.Players.Where(filter).ToList();
        if (targets.Count > 0)
        {
            setVote(targets[SharedRandom.Next(targets.Count)]);
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