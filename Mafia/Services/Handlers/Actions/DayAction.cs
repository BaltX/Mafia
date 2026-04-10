using Mafia.Models;

namespace Mafia.Services.Handlers;

public class DayAction : ActionBase
{
    public override GameStage RequiredStage => GameStage.DayVoting;
    public override GameRole? RequiredRole => null;

    public override bool CanExecute(LobbyState lobby, PlayerState player) =>
        (lobby.Stage == GameStage.DayVoting || lobby.Stage == GameStage.DayVoting2) && !player.IsDead() && !player.IsHost();

    public override string? Validate(LobbyState lobby, PlayerState player, Guid targetId)
    {
        if (!CanExecute(lobby, player))
            return GameConstants.Errors.CannotVoteNow;

        if (lobby.Stage == GameStage.DayVoting2 && lobby.Day1TopVotedPlayerIds.Count > 0)
        {
            if (!lobby.Day1TopVotedPlayerIds.Contains(targetId))
                return GameConstants.Errors.SecondDayVoteOnlyForLeaders;
        }

        var target = lobby.GetPlayer(targetId);
        if (target is null || !target.IsAlive || target.IsHost() || targetId == player.Id)
            return GameConstants.Errors.InvalidTarget;

        return null;
    }

    public override void Execute(LobbyState lobby, PlayerState player, Guid targetId) =>
        lobby.DayVotes[player.Id] = targetId;

    public override void Cancel(LobbyState lobby, PlayerState player) =>
        lobby.DayVotes.Remove(player.Id);
}