using Mafia.Models;

namespace Mafia.Services.Handlers;

public class BeautyVotingHandler : IVotingHandler
{
    public GameStage RequiredStage => GameStage.BeautyTurn;

    public bool CanVote(LobbyState lobby, PlayerState player) =>
        lobby.Stage == GameStage.BeautyTurn && !player.IsDead() && player.IsBeauty();

    public string? GetNotValidError(LobbyState lobby, PlayerState player, Guid targetId)
    {
        if (!CanVote(lobby, player))
            return GameConstants.Errors.BeautyTurnNotAvailable;

        if (!ValidateTarget(lobby, player, targetId))
            return GameConstants.Errors.InvalidTarget;

        if (lobby.LastNightBeautyVote == targetId)
            return GameConstants.Errors.CannotSelectSamePlayerTwice;

        return null;
    }

    public void SetVote(LobbyState lobby, PlayerState player, Guid targetId) =>
        lobby.BeautyVote = targetId;

    public void ClearVote(LobbyState lobby, PlayerState player) =>
        lobby.BeautyVote = null;

    private static bool ValidateTarget(LobbyState lobby, PlayerState player, Guid targetId)
    {
        var target = lobby.GetPlayer(targetId);
        return target is not null && target.IsAlive && !target.IsHost() && targetId != player.Id;
    }
}