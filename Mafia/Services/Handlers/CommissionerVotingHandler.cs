using Mafia.Models;

namespace Mafia.Services.Handlers;

public class CommissionerVotingHandler : IVotingHandler
{
    public GameStage RequiredStage => GameStage.CommissionerTurn;

    public bool CanVote(LobbyState lobby, PlayerState player) =>
        lobby.Stage == GameStage.CommissionerTurn && !player.IsDead() && player.IsCommissioner();

    public string? GetNotValidError(LobbyState lobby, PlayerState player, Guid targetId)
    {
        if (!CanVote(lobby, player))
            return GameConstants.Errors.CommissionerTurnNotAvailable;

        if (!ValidateTarget(lobby, player, targetId))
            return GameConstants.Errors.InvalidTarget;

        if (lobby.LastNightBeautyVote == targetId || lobby.LastNightDoctorVote == targetId)
            return GameConstants.Errors.CannotSelectSamePlayerTwice;

        return null;
    }

    public void SetVote(LobbyState lobby, PlayerState player, Guid targetId)
    {
        lobby.CommissionerVote = targetId;
        lobby.PendingCommissionerCheckResult = null;
    }

    public void ClearVote(LobbyState lobby, PlayerState player)
    {
        lobby.CommissionerVote = null;
        lobby.CommissionerIsKill = false;
        lobby.PendingCommissionerCheckResult = null;
    }

    public bool CommissionerCheck(LobbyState lobby, PlayerState player, Guid targetId, out string? error)
    {
        error = GetNotValidError(lobby, player, targetId);
        if (error is not null) return false;

        lobby.CommissionerVote = targetId;
        lobby.CommissionerIsKill = false;
        lobby.PendingCommissionerCheckResult = null;
        return true;
    }

    public bool CommissionerKill(LobbyState lobby, PlayerState player, Guid targetId, out string? error)
    {
        error = GetNotValidError(lobby, player, targetId);
        if (error is not null) return false;

        lobby.CommissionerVote = targetId;
        lobby.CommissionerIsKill = true;
        lobby.PendingCommissionerCheckResult = null;
        return true;
    }

    public bool SetCommissionerIsKill(LobbyState lobby, PlayerState player, bool isKill, out string? error)
    {
        error = null;
        if (!CanVote(lobby, player))
        {
            error = GameConstants.Errors.CommissionerTurnNotAvailable;
            return false;
        }

        lobby.CommissionerIsKill = isKill;
        lobby.PendingCommissionerCheckResult = null;
        return true;
    }

    public bool CommissionerVote(LobbyState lobby, PlayerState player, Guid targetId, out string? error)
    {
        error = GetNotValidError(lobby, player, targetId);
        if (error is not null) return false;

        lobby.CommissionerVote = targetId;
        lobby.PendingCommissionerCheckResult = null;
        return true;
    }

    public bool CancelCommissionerVote(LobbyState lobby, PlayerState player, out string? error)
    {
        error = null;
        if (!CanVote(lobby, player))
        {
            error = GameConstants.Errors.CannotCancelNow;
            return false;
        }

        lobby.CommissionerVote = null;
        lobby.CommissionerIsKill = false;
        lobby.PendingCommissionerCheckResult = null;
        return true;
    }

    private static bool ValidateTarget(LobbyState lobby, PlayerState player, Guid targetId)
    {
        var target = lobby.GetPlayer(targetId);
        return target is not null && target.IsAlive && !target.IsHost() && targetId != player.Id;
    }
}