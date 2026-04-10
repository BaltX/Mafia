using Mafia.Models;

namespace Mafia.Services.Handlers;

public class BeautyAction : ActionBase
{
    public override GameStage RequiredStage => GameStage.BeautyTurn;
    public override GameRole? RequiredRole => GameRole.Beauty;

    public override string? Validate(LobbyState lobby, PlayerState player, Guid targetId)
    {
        if (!CanExecute(lobby, player))
            return GameConstants.Errors.BeautyTurnNotAvailable;

        var target = lobby.GetPlayer(targetId);
        if (target is null || !target.IsAlive || target.IsHost() || target.Role == GameRole.Beauty || targetId == lobby.LastNightBeautyVote)
            return GameConstants.Errors.CannotSelectSamePlayerTwice;

        return null;
    }

    public override void Execute(LobbyState lobby, PlayerState player, Guid targetId) =>
        lobby.BeautyVote = targetId;

    public override void Cancel(LobbyState lobby, PlayerState player) =>
        lobby.BeautyVote = null;
}