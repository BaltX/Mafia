using Mafia.Models;

namespace Mafia.Services;

/// <summary>
/// Сервис для управления логикой начала игры и распределения ролей.
/// </summary>
public class MafiaGameLogicService
{
    private static readonly Random SharedRandom = Random.Shared;

    /// <summary>
    /// Начинает игру и распределяет роли между игроками.
    /// </summary>
    /// <param name="lobby">Лобби для старта игры.</param>
    /// <param name="error">Сообщение об ошибке, если старт невозможен.</param>
    /// <returns>True если игра успешно началась.</returns>
    public bool StartGame(LobbyState lobby, out string? error)
    {
        error = null;
        var activePlayers = lobby.Players.Where(p => p.Role != GameRole.Host).ToList();
        
        if (activePlayers.Count < 3)
        {
            error = "Для старта игры нужно минимум 3 игрока, не считая ведущего.";
            return false;
        }

        ResetPlayerRoles(activePlayers);
        AssignMafiaRoles(activePlayers);
        AssignSpecialRoles(activePlayers);

        lobby.Round = 1;
        lobby.Stage = GameStage.Discussion;
        lobby.StageEndsAtUtc = DateTimeOffset.UtcNow.AddSeconds(lobby.DiscussionSeconds);
        ClearAllVotes(lobby);
        
        return true;
    }

    private static void ResetPlayerRoles(List<PlayerState> players)
    {
        foreach (var p in players)
        {
            if (p.Role == GameRole.Unassigned)
                p.Role = GameRole.Civilian;
            p.IsAlive = true;
        }
    }

    private void AssignMafiaRoles(List<PlayerState> players)
    {
        var unassigned = players.Where(p => p.Role == GameRole.Civilian).ToList();
        if (unassigned.Count == 0) return;

        var hasMafia = players.Any(p => p.Role == GameRole.Mafia || p.Role == GameRole.Don);
        if (!hasMafia)
        {
            var mafiaCount = Math.Max(1, players.Count / 3);
            var shuffled = unassigned.OrderBy(_ => SharedRandom.Next()).ToList();

            for (int i = 0; i < mafiaCount && i < shuffled.Count; i++)
            {
                shuffled[i].Role = i == 0 ? GameRole.Don : GameRole.Mafia;
            }

            unassigned = shuffled.Skip(mafiaCount).ToList();
        }

        if (unassigned.Count > 0 && !players.Any(p => p.Role == GameRole.Killer))
        {
            var maniacCandidate = unassigned[SharedRandom.Next(unassigned.Count)];
            maniacCandidate.Role = GameRole.Killer;
        }
    }

    private void AssignSpecialRoles(List<PlayerState> players)
    {
        var unassigned = players.Where(p => p.Role == GameRole.Civilian).ToList();
        if (unassigned.Count == 0) return;

        if (!players.Any(p => p.Role == GameRole.Commissioner))
        {
            var commissioner = unassigned[SharedRandom.Next(unassigned.Count)];
            commissioner.Role = GameRole.Commissioner;
            unassigned.Remove(commissioner);
        }

        if (unassigned.Count > 0 && !players.Any(p => p.Role == GameRole.Beauty))
        {
            var beauty = unassigned[SharedRandom.Next(unassigned.Count)];
            beauty.Role = GameRole.Beauty;
            unassigned.Remove(beauty);
        }

        if (unassigned.Count > 0 && !players.Any(p => p.Role == GameRole.Doctor))
        {
            var doctor = unassigned[SharedRandom.Next(unassigned.Count)];
            doctor.Role = GameRole.Doctor;
            unassigned.Remove(doctor);
        }

        if (unassigned.Count > 0 && !players.Any(p => p.Role == GameRole.Necromancer))
        {
            var necromancer = unassigned[SharedRandom.Next(unassigned.Count)];
            necromancer.Role = GameRole.Necromancer;
        }
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
}