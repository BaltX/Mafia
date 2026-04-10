using Mafia.Models;

namespace Mafia.Services.Handlers;

public class NecromancerAction : ActionBase
{
    public override GameStage RequiredStage => GameStage.NecromancerTurn;
    public override GameRole? RequiredRole => GameRole.Necromancer;

    public override string? Validate(LobbyState lobby, PlayerState player, Guid targetId)
    {
        if (!CanExecute(lobby, player))
            return GameConstants.Errors.NecromancerTurnNotAvailable;

        var target = lobby.GetPlayer(targetId);
        if (target is null || !target.IsAlive || target.IsHost() || target.Role == GameRole.Necromancer || target.Role == GameRole.Killer || target.IsZombie)
            return GameConstants.Errors.InvalidTarget;

        return null;
    }

    public override void Execute(LobbyState lobby, PlayerState player, Guid targetId) =>
        lobby.NecromancerVote = targetId;

    public override void Cancel(LobbyState lobby, PlayerState player) =>
        lobby.NecromancerVote = null;
}