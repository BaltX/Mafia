using Mafia.Models;

namespace Mafia.Services.Handlers;

public class CommissionerAction : ActionBase
{
    public override GameStage RequiredStage => GameStage.CommissionerTurn;
    public override GameRole? RequiredRole => GameRole.Commissioner;

    public override bool CanExecute(LobbyState lobby, PlayerState player) =>
        lobby.Stage == GameStage.CommissionerTurn && !player.IsDead() && player.Role == GameRole.Commissioner;

    public override string? Validate(LobbyState lobby, PlayerState player, Guid targetId)
    {
        if (!CanExecute(lobby, player))
            return GameConstants.Errors.CommissionerTurnNotAvailable;

        var target = lobby.GetPlayer(targetId);
        if (target is null || !target.IsAlive || target.IsHost() || target.Role == GameRole.Commissioner)
            return GameConstants.Errors.InvalidTarget;

        return null;
    }

    public override void Execute(LobbyState lobby, PlayerState player, Guid targetId)
    {
        if (lobby.CommissionerIsKill)
        {
            lobby.CommissionerVote = targetId;
        }
        else
        {
            lobby.CommissionerVote = targetId;
        }
    }

    public override void Cancel(LobbyState lobby, PlayerState player)
    {
        lobby.CommissionerVote = null;
    }

    public bool CommissionerCheck(LobbyState lobby, PlayerState player, Guid targetId, out string? error)
    {
        error = null;
        if (!CanExecute(lobby, player))
        {
            error = GameConstants.Errors.CommissionerTurnNotAvailable;
            return false;
        }
        
        var target = lobby.GetPlayer(targetId);
        if (target is null || !target.IsAlive || target.IsHost() || target.Role == GameRole.Commissioner)
        {
            error = GameConstants.Errors.InvalidTarget;
            return false;
        }

        lobby.CommissionerVote = targetId;
        return true;
    }

    public bool CommissionerKill(LobbyState lobby, PlayerState player, Guid targetId, out string? error)
    {
        error = null;
        if (!CanExecute(lobby, player))
        {
            error = GameConstants.Errors.CommissionerTurnNotAvailable;
            return false;
        }

        var target = lobby.GetPlayer(targetId);
        if (target is null || !target.IsAlive || target.IsHost() || target.Role == GameRole.Commissioner)
        {
            error = GameConstants.Errors.InvalidTarget;
            return false;
        }

        lobby.CommissionerIsKill = true;
        lobby.CommissionerVote = targetId;
        return true;
    }

    public bool SetCommissionerIsKill(LobbyState lobby, PlayerState player, bool isKill, out string? error)
    {
        error = null;
        if (!CanExecute(lobby, player))
        {
            error = GameConstants.Errors.CommissionerTurnNotAvailable;
            return false;
        }

        lobby.CommissionerIsKill = isKill;
        if (!isKill)
            lobby.CommissionerVote = null;
        return true;
    }
}