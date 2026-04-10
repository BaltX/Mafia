using Mafia.Models;

namespace Mafia.Services.Handlers;

public class DoctorAction : ActionBase
{
    public override GameStage RequiredStage => GameStage.DoctorTurn;
    public override GameRole? RequiredRole => GameRole.Doctor;

    public override string? Validate(LobbyState lobby, PlayerState player, Guid targetId)
    {
        if (!CanExecute(lobby, player))
            return GameConstants.Errors.DoctorTurnNotAvailable;

        var target = lobby.GetPlayer(targetId);
        if (target is null || !target.IsAlive || target.IsHost() || target.Role == GameRole.Doctor || targetId == lobby.LastNightDoctorVote)
            return GameConstants.Errors.CannotSelectSamePlayerTwice;

        return null;
    }

    public override void Execute(LobbyState lobby, PlayerState player, Guid targetId) =>
        lobby.DoctorVote = targetId;

    public override void Cancel(LobbyState lobby, PlayerState player) =>
        lobby.DoctorVote = null;
}