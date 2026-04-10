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
            p.Role = GameRole.Civilian;
            p.IsAlive = true;
        }
    }

    private void AssignMafiaRoles(List<PlayerState> players)
    {
        var mafiaCount = Math.Max(1, players.Count / 3);
        var shuffled = players.OrderBy(_ => SharedRandom.Next()).ToList();

        for (int i = 0; i < mafiaCount; i++)
        {
            shuffled[i].Role = i == 0 ? GameRole.Don : GameRole.Mafia;
        }

        var maniacCandidate = shuffled.Skip(mafiaCount).FirstOrDefault();
        if (maniacCandidate is not null)
        {
            maniacCandidate.Role = GameRole.Killer;
        }
    }

    private void AssignSpecialRoles(List<PlayerState> players)
    {
        var civilians = players.Where(p => p.Role == GameRole.Civilian).ToList();

        if (civilians.Count > 0)
        {
            var commissioner = civilians[SharedRandom.Next(civilians.Count)];
            commissioner.Role = GameRole.Commissioner;
            civilians.Remove(commissioner);
        }

        if (civilians.Count > 0)
        {
            var beauty = civilians[SharedRandom.Next(civilians.Count)];
            beauty.Role = GameRole.Beauty;
            civilians.Remove(beauty);
        }

        if (civilians.Count > 0)
        {
            var doctor = civilians[SharedRandom.Next(civilians.Count)];
            doctor.Role = GameRole.Doctor;
            civilians.Remove(doctor);
        }

        if (civilians.Count > 0)
        {
            var necromancer = civilians[SharedRandom.Next(civilians.Count)];
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