using Mafia.Models;

namespace Mafia.Services;

/// <summary>
/// Сервис для обработки всех голосований в игре.
/// </summary>
public class MafiaVotingService
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

    #region Day Vote

    /// <summary>Дневное голосование.</summary>
    public bool DayVote(LobbyState lobby, PlayerState player, Guid targetId, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.DayVoting && lobby.Stage != GameStage.DayVoting2 || player.IsDead() || player.IsHost()) 
        { 
            error = "Сейчас нельзя голосовать."; 
            return false; 
        }

        if (lobby.Stage == GameStage.DayVoting2 && lobby.Day1TopVotedPlayerIds.Count > 0)
        {
            if (!lobby.Day1TopVotedPlayerIds.Contains(targetId))
            {
                error = "Во второй день можно голосовать только за лидеров первого дня.";
                return false;
            }
        }

        var (isValid, validationError) = ValidateTarget(lobby, player, targetId);
        if (!isValid) 
        { 
            error = validationError ?? "Неверная цель."; 
            return false; 
        }

        lobby.DayVotes[player.Id] = targetId;
        return true;
    }

    /// <summary>Отмена дневного голосования.</summary>
    public bool CancelDayVote(LobbyState lobby, PlayerState player, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.DayVoting && lobby.Stage != GameStage.DayVoting2 || player.IsDead() || player.IsHost()) 
        { 
            error = "Сейчас нельзя отменить."; 
            return false; 
        }
        lobby.DayVotes.Remove(player.Id);
        return true;
    }

    #endregion

    #region Mafia Vote

    /// <summary>Голосование мафии.</summary>
    public bool MafiaVote(LobbyState lobby, PlayerState player, Guid targetId, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.MafiaTurn || player.IsDead() || !player.IsMafia()) 
        { 
            error = "Сейчас ход мафии недоступен."; 
            return false; 
        }

        var (isValid, validationError) = ValidateTarget(lobby, player, targetId, p => p.IsMafia());
        if (!isValid) 
        { 
            error = validationError ?? "Мафия может голосовать только против живых не-мафиози."; 
            return false; 
        }

        lobby.MafiaVotes[player.Id] = targetId;
        return true;
    }

    /// <summary>Отмена голосования мафии.</summary>
    public bool CancelMafiaVote(LobbyState lobby, PlayerState player, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.MafiaTurn || player.IsDead() || !player.IsMafia()) 
        { 
            error = "Сейчас нельзя отменить."; 
            return false; 
        }
        lobby.MafiaVotes.Remove(player.Id);
        return true;
    }

    #endregion

    #region Maniac/Killer Vote

    /// <summary>Голосование маньяка.</summary>
    public bool ManiacVote(LobbyState lobby, PlayerState player, Guid targetId, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.KillerTurn || player.IsDead() || !player.IsKiller()) 
        { 
            error = "Сейчас ход маньяка недоступен."; 
            return false; 
        }

        var (isValid, validationError) = ValidateTarget(lobby, player, targetId, p => p.IsKiller(), excludeSelf: true);
        if (!isValid) 
        { 
            error = validationError ?? "Неверная цель."; 
            return false; 
        }

        lobby.KillerVote = targetId;
        return true;
    }

    /// <summary>Отмена голосования маньяка.</summary>
    public bool CancelManiacVote(LobbyState lobby, PlayerState player, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.KillerTurn || player.IsDead() || !player.IsKiller()) 
        { 
            error = "Сейчас нельзя отменить."; 
            return false; 
        }
        lobby.KillerVote = null;
        return true;
    }

    #endregion

    #region Commissioner Vote

    /// <summary>Действие комиссара (проверка или убийство).</summary>
    public bool CommissionerAction(LobbyState lobby, PlayerState player, Guid targetId, bool isKill, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.CommissionerTurn || player.IsDead() || !player.IsCommissioner()) 
        { 
            error = "Сейчас ход комиссара недоступен."; 
            return false; 
        }

        var (isValid, validationError) = ValidateTarget(lobby, player, targetId, excludeSelf: true);
        if (!isValid) 
        { 
            error = validationError ?? "Неверная цель."; 
            return false; 
        }

        lobby.CommissionerVote = targetId;
        lobby.CommissionerIsKill = isKill;
        lobby.PendingCommissionerCheckResult = null;
        return true;
    }

    /// <summary>Установить режим действия комиссара (проверка/убийство).</summary>
    public bool SetCommissionerIsKill(LobbyState lobby, PlayerState player, bool isKill, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.CommissionerTurn || player.IsDead() || !player.IsCommissioner()) 
        { 
            error = "Сейчас ход комиссара недоступен."; 
            return false; 
        }

        lobby.CommissionerIsKill = isKill;
        lobby.PendingCommissionerCheckResult = null;
        return true;
    }

    /// <summary>Голосование комиссара.</summary>
    public bool CommissionerVote(LobbyState lobby, PlayerState player, Guid targetId, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.CommissionerTurn || player.IsDead() || !player.IsCommissioner()) 
        { 
            error = "Сейчас ход комиссара недоступен."; 
            return false; 
        }

        lobby.CommissionerVote = targetId;
        lobby.PendingCommissionerCheckResult = null;
        return true;
    }

    /// <summary>Отмена голосования комиссара.</summary>
    public bool CancelCommissionerVote(LobbyState lobby, PlayerState player, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.CommissionerTurn || player.IsDead() || !player.IsCommissioner()) 
        { 
            error = "Сейчас нельзя отменить."; 
            return false; 
        }
        lobby.CommissionerVote = null;
        lobby.CommissionerIsKill = false;
        lobby.PendingCommissionerCheckResult = null;
        return true;
    }

    #endregion

    #region Beauty Vote

    /// <summary>Действие красотки.</summary>
    public bool BeautyAction(LobbyState lobby, PlayerState player, Guid targetId, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.BeautyTurn || player.IsDead() || !player.IsBeauty()) 
        { 
            error = "Сейчас ход красотки недоступен."; 
            return false; 
        }

        var (isValid, validationError) = ValidateTarget(lobby, player, targetId, excludeSelf: true);
        if (!isValid) 
        { 
            error = validationError ?? "Неверная цель."; 
            return false; 
        }
        if (lobby.LastNightBeautyVote == targetId) 
        { 
            error = "Нельзя выбирать одного игрока 2 ночи подряд."; 
            return false; 
        }

        lobby.BeautyVote = targetId;
        return true;
    }

    /// <summary>Отмена голосования красотки.</summary>
    public bool CancelBeautyVote(LobbyState lobby, PlayerState player, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.BeautyTurn || player.IsDead() || !player.IsBeauty()) 
        { 
            error = "Сейчас нельзя отменить."; 
            return false; 
        }
        lobby.BeautyVote = null;
        return true;
    }

    #endregion

    #region Doctor Vote

    /// <summary>Действие доктора.</summary>
    public bool DoctorAction(LobbyState lobby, PlayerState player, Guid targetId, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.DoctorTurn || player.IsDead() || !player.IsDoctor()) 
        { 
            error = "Сейчас ход доктора недоступен."; 
            return false; 
        }

        var (isValid, validationError) = ValidateTarget(lobby, player, targetId);
        if (!isValid) 
        { 
            error = validationError ?? "Неверная цель."; 
            return false; 
        }
        if (lobby.LastNightDoctorVote == targetId) 
        { 
            error = "Нельзя выбирать одного игрока 2 ночи подряд."; 
            return false; 
        }

        lobby.DoctorVote = targetId;
        return true;
    }

    /// <summary>Отмена действия доктора.</summary>
    public bool CancelDoctorVote(LobbyState lobby, PlayerState player, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.DoctorTurn || player.IsDead() || !player.IsDoctor()) 
        { 
            error = "Сейчас нельзя отменить."; 
            return false; 
        }
        lobby.DoctorVote = null;
        return true;
    }

    #endregion

    #region Necromancer Vote

    /// <summary>Действие некроманта.</summary>
    public bool NecromancerAction(LobbyState lobby, PlayerState player, Guid targetId, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.NecromancerTurn || player.IsDead() || !player.IsNecromancer()) 
        { 
            error = "Сейчас ход некроманта недоступен."; 
            return false; 
        }

        var (isValid, validationError) = ValidateTarget(lobby, player, targetId, excludeSelf: true);
        if (!isValid) 
        { 
            error = validationError ?? "Неверная цель."; 
            return false; 
        }

        lobby.NecromancerVote = targetId;
        return true;
    }

    /// <summary>Отмена действия некроманта.</summary>
    public bool CancelNecromancerVote(LobbyState lobby, PlayerState player, out string? error)
    {
        error = null;
        if (lobby.Stage != GameStage.NecromancerTurn || player.IsDead() || !player.IsNecromancer()) 
        { 
            error = "Сейчас нельзя отменить."; 
            return false; 
        }
        lobby.NecromancerVote = null;
        return true;
    }

    #endregion

    #region Bot Votes

    /// <summary>Обработка голосований ботов для указанной стадии.</summary>
    public void ProcessBotVotes(LobbyState lobby, GameStage stage)
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

    private void ProcessBotDayVotes(LobbyState lobby)
    {
        foreach (var bot in lobby.Players.Where(p => p.IsBot && p.IsAlive && p.Role != GameRole.Host))
        {
            if (!lobby.DayVotes.ContainsKey(bot.Id))
            {
                TrySetRandomVote(lobby, bot, p => p.IsAlive && p.Role != GameRole.Host && p.Id != bot.Id, t => lobby.DayVotes[bot.Id] = t.Id);
            }
        }
    }

    private void ProcessBotDayVoting2Votes(LobbyState lobby)
    {
        foreach (var bot in lobby.Players.Where(p => p.IsBot && p.IsAlive && p.Role != GameRole.Host))
        {
            if (!lobby.DayVotes.ContainsKey(bot.Id))
            {
                TrySetRandomVote(lobby, bot, p => p.IsAlive && p.Role != GameRole.Host && p.Id != bot.Id && lobby.Day1TopVotedPlayerIds.Contains(p.Id), t => lobby.DayVotes[bot.Id] = t.Id);
            }
        }
    }

    private void ProcessBotCommissionerVotes(LobbyState lobby)
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

    private void ProcessBotMafiaVotes(LobbyState lobby)
    {
        foreach (var bot in lobby.Players.Where(p => p.IsBot && p.IsAlive && (p.Role == GameRole.Mafia || p.Role == GameRole.Don)))
        {
            if (!lobby.MafiaVotes.ContainsKey(bot.Id))
            {
                TrySetRandomVote(lobby, bot, p => p.IsAlive && p.Role != GameRole.Host && p.Role != GameRole.Mafia && p.Role != GameRole.Don, t => lobby.MafiaVotes[bot.Id] = t.Id);
            }
        }
    }

    private void ProcessBotKillerVote(LobbyState lobby)
    {
        if (lobby.KillerVote is null)
            TrySetRandomVote(lobby, null, p => p.IsAlive && p.Role != GameRole.Host && p.Role != GameRole.Killer, t => lobby.KillerVote = t.Id);
    }

    private void ProcessBotBeautyVote(LobbyState lobby)
    {
        if (lobby.BeautyVote is null)
        {
            TrySetRandomVote(lobby, null, p => p.IsAlive && p.Role != GameRole.Host && p.Role != GameRole.Beauty && p.Id != lobby.LastNightBeautyVote, t => lobby.BeautyVote = t.Id);
        }
    }

    private void ProcessBotDoctorVote(LobbyState lobby)
    {
        if (lobby.DoctorVote is null)
        {
            TrySetRandomVote(lobby, null, p => p.IsAlive && p.Role != GameRole.Host && p.Role != GameRole.Doctor && p.Id != lobby.LastNightDoctorVote, t => lobby.DoctorVote = t.Id);
        }
    }

    private void ProcessBotNecromancerVote(LobbyState lobby)
    {
        if (lobby.NecromancerVote is null)
            TrySetRandomVote(lobby, null, p => p.IsAlive && !p.IsZombie && p.Role != GameRole.Host && p.Role != GameRole.Necromancer && p.Role != GameRole.Killer, t => lobby.NecromancerVote = t.Id);
    }

    private void TrySetRandomVote(LobbyState lobby, PlayerState? bot, Func<PlayerState, bool> filter, Action<PlayerState> setVote)
    {
        var targets = lobby.Players.Where(filter).ToList();
        if (targets.Count > 0)
        {
            setVote(targets[SharedRandom.Next(targets.Count)]);
        }
    }

    #endregion

    #region Vote Resolution

    /// <summary>Подсчёт результатов голосования.</summary>
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

    /// <summary>Подсчёт результатов голосования мафии (с учётом дона).</summary>
    public static Guid? ResolveMafiaVote(LobbyState lobby, Dictionary<Guid, Guid> votes)
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

    #endregion
}