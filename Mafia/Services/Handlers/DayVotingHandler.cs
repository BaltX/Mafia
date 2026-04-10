using Mafia.Models;

namespace Mafia.Services.Handlers;

public class DayVotingHandler : IVotingHandler
{
    public GameStage RequiredStage => GameStage.DayVoting;

    public bool CanVote(LobbyState lobby, PlayerState player) =>
        (lobby.Stage == GameStage.DayVoting || lobby.Stage == GameStage.DayVoting2) && 
        !player.IsDead() && !player.IsHost();

    public string? GetNotValidError(LobbyState lobby, PlayerState player, Guid targetId)
    {
        if (!CanVote(lobby, player))
            return GameConstants.Errors.CannotVoteNow;

        if (lobby.Stage == GameStage.DayVoting2 && lobby.Day1TopVotedPlayerIds.Count > 0)
        {
            if (!lobby.Day1TopVotedPlayerIds.Contains(targetId))
                return GameConstants.Errors.SecondDayVoteOnlyForLeaders;
        }

        if (!ValidateTarget(lobby, player, targetId))
            return GameConstants.Errors.InvalidTarget;

        return null;
    }

    public void SetVote(LobbyState lobby, PlayerState player, Guid targetId) =>
        lobby.DayVotes[player.Id] = targetId;

    public void ClearVote(LobbyState lobby, PlayerState player) =>
        lobby.DayVotes.Remove(player.Id);

    private static bool ValidateTarget(LobbyState lobby, PlayerState player, Guid targetId)
    {
        var target = lobby.GetPlayer(targetId);
        return target is not null && target.IsAlive && !target.IsHost() && targetId != player.Id;
    }
}