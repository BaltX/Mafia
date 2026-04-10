using Mafia.Models;

namespace Mafia.Services.Handlers;

public class MafiaAction : ActionBase
{
    public override GameStage RequiredStage => GameStage.MafiaTurn;
    public override GameRole? RequiredRole => null;

    public override bool CanExecute(LobbyState lobby, PlayerState player) =>
        lobby.Stage == GameStage.MafiaTurn && !player.IsDead() && player.IsMafia();

    public override string? Validate(LobbyState lobby, PlayerState player, Guid targetId)
    {
        if (!CanExecute(lobby, player))
            return GameConstants.Errors.MafiaTurnNotAvailable;

        var target = lobby.GetPlayer(targetId);
        if (target is null || !target.IsAlive || target.IsHost() || target.IsMafia())
            return GameConstants.Errors.CannotVoteMafiaToMafia;

        return null;
    }

    public override void Execute(LobbyState lobby, PlayerState player, Guid targetId) =>
        lobby.MafiaVotes[player.Id] = targetId;

    public override void Cancel(LobbyState lobby, PlayerState player) =>
        lobby.MafiaVotes.Remove(player.Id);
}