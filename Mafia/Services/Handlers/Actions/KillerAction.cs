using Mafia.Models;

namespace Mafia.Services.Handlers;

public class KillerAction : ActionBase
{
    public override GameStage RequiredStage => GameStage.KillerTurn;
    public override GameRole? RequiredRole => GameRole.Killer;

    public override string? Validate(LobbyState lobby, PlayerState player, Guid targetId)
    {
        if (!CanExecute(lobby, player))
            return GameConstants.Errors.ManiacTurnNotAvailable;

        var target = lobby.GetPlayer(targetId);
        if (target is null || !target.IsAlive || target.IsHost() || target.Role == GameRole.Killer)
            return GameConstants.Errors.InvalidTarget;

        return null;
    }

    public override void Execute(LobbyState lobby, PlayerState player, Guid targetId) =>
        lobby.KillerVote = targetId;

    public override void Cancel(LobbyState lobby, PlayerState player) =>
        lobby.KillerVote = null;
}