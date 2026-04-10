using Mafia.Models;
using Mafia.Services.Handlers;

namespace Mafia.Services;

public class VotingCoordinator
{
    private readonly LobbyService _lobbyService;
    private readonly MafiaNightService _nightService;

    public VotingCoordinator(LobbyService lobbyService, MafiaNightService nightService)
    {
        _lobbyService = lobbyService;
        _nightService = nightService;
    }

    public (LobbyState Lobby, PlayerState Player)? GetState(string code, Guid playerId, bool autoAdvance = true)
    {
        var result = _lobbyService.GetState(code, playerId);
        if (result is null || !autoAdvance)
            return result;

        var now = DateTimeOffset.UtcNow;
        while (result.Value.Lobby.StageEndsAtUtc.HasValue && 
               now >= result.Value.Lobby.StageEndsAtUtc.Value && 
               result.Value.Lobby.Stage != GameStage.Lobby && 
               result.Value.Lobby.Stage != GameStage.GameOver)
        {
            MafiaVotingService.ProcessBotVotes(result.Value.Lobby, result.Value.Lobby.Stage);
            _nightService.NextStage(result.Value.Lobby, out _);
        }

        return result;
    }

    public bool ExecuteAction(
        string code,
        Guid playerId,
        Action<LobbyState, PlayerState> action,
        Func<LobbyState, PlayerState, string?>? validate = null)
    {
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out var error))
            return false;

        if (validate is not null)
        {
            var validationError = validate(lobby!, player!);
            if (validationError is not null)
                return false;
        }

        action(lobby!, player!);
        return true;
    }

    public bool ExecuteAction<T>(
        string code,
        Guid playerId,
        T arg,
        Action<LobbyState, PlayerState, T> action,
        Func<LobbyState, PlayerState, T, string?>? validate = null)
    {
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out var error))
            return false;

        if (validate is not null)
        {
            var validationError = validate(lobby!, player!, arg);
            if (validationError is not null)
                return false;
        }

        action(lobby!, player!, arg);
        return true;
    }

    public bool TryVote(string code, Guid playerId, Guid targetId)
    {
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out var error))
            return false;

        var handler = VotingHandlerFactory.GetHandlerByStage(lobby!.Stage);
        if (handler is null)
            return false;

        var validationError = handler.GetNotValidError(lobby, player!, targetId);
        if (validationError is not null)
            return false;

        handler.SetVote(lobby, player!, targetId);
        return true;
    }

    public bool TryCancelVote(string code, Guid playerId)
    {
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out var error))
            return false;

        var handler = VotingHandlerFactory.GetHandlerByStage(lobby!.Stage);
        if (handler is null || !handler.CanVote(lobby, player!))
            return false;

        handler.ClearVote(lobby, player!);
        return true;
    }

    public bool DayVote(string code, Guid playerId, Guid targetId, out string? error)
    {
        error = null;
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out error))
            return false;

        var handler = VotingHandlerFactory.GetHandler(GameStage.DayVoting, player!.Role);
        if (handler is null)
        {
            error = GameConstants.Errors.CannotVoteNow;
            return false;
        }

        error = handler.GetNotValidError(lobby!, player!, targetId);
        if (error is not null) return false;
        
        handler.SetVote(lobby!, player!, targetId);
        return true;
    }

    public bool CancelDayVote(string code, Guid playerId, out string? error)
    {
        error = null;
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out error))
            return false;

        var handler = VotingHandlerFactory.GetHandler(GameStage.DayVoting, player!.Role);
        if (handler is null || !handler.CanVote(lobby!, player!))
        {
            error = GameConstants.Errors.CannotCancelNow;
            return false;
        }

        handler.ClearVote(lobby!, player!);
        return true;
    }

    public bool MafiaVote(string code, Guid playerId, Guid targetId, out string? error)
    {
        error = null;
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out error))
            return false;

        var handler = VotingHandlerFactory.GetHandler(GameStage.MafiaTurn, player!.Role);
        if (handler is null)
        {
            error = GameConstants.Errors.MafiaTurnNotAvailable;
            return false;
        }

        error = handler.GetNotValidError(lobby!, player!, targetId);
        if (error is not null) return false;
        
        handler.SetVote(lobby!, player!, targetId);
        return true;
    }

    public bool CancelMafiaVote(string code, Guid playerId, out string? error)
    {
        error = null;
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out error))
            return false;

        var handler = VotingHandlerFactory.GetHandler(GameStage.MafiaTurn, player!.Role);
        if (handler is null || !handler.CanVote(lobby!, player!))
        {
            error = GameConstants.Errors.CannotCancelNow;
            return false;
        }

        handler.ClearVote(lobby!, player!);
        return true;
    }

    public bool ManiacVote(string code, Guid playerId, Guid targetId, out string? error)
    {
        error = null;
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out error))
            return false;

        var handler = VotingHandlerFactory.GetHandler(GameStage.KillerTurn, player!.Role);
        if (handler is null)
        {
            error = GameConstants.Errors.ManiacTurnNotAvailable;
            return false;
        }

        error = handler.GetNotValidError(lobby!, player!, targetId);
        if (error is not null) return false;
        
        handler.SetVote(lobby!, player!, targetId);
        return true;
    }

    public bool CancelManiacVote(string code, Guid playerId, out string? error)
    {
        error = null;
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out error))
            return false;

        var handler = VotingHandlerFactory.GetHandler(GameStage.KillerTurn, player!.Role);
        if (handler is null || !handler.CanVote(lobby!, player!))
        {
            error = GameConstants.Errors.CannotCancelNow;
            return false;
        }

        handler.ClearVote(lobby!, player!);
        return true;
    }

    public bool BeautyAction(string code, Guid playerId, Guid targetId, out string? error)
    {
        error = null;
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out error))
            return false;

        var handler = VotingHandlerFactory.GetHandler(GameStage.BeautyTurn, player!.Role);
        if (handler is null)
        {
            error = GameConstants.Errors.BeautyTurnNotAvailable;
            return false;
        }

        error = handler.GetNotValidError(lobby!, player!, targetId);
        if (error is not null) return false;
        
        handler.SetVote(lobby!, player!, targetId);
        return true;
    }

    public bool CancelBeautyVote(string code, Guid playerId, out string? error)
    {
        error = null;
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out error))
            return false;

        var handler = VotingHandlerFactory.GetHandler(GameStage.BeautyTurn, player!.Role);
        if (handler is null || !handler.CanVote(lobby!, player!))
        {
            error = GameConstants.Errors.CannotCancelNow;
            return false;
        }

        handler.ClearVote(lobby!, player!);
        return true;
    }

    public bool DoctorAction(string code, Guid playerId, Guid targetId, out string? error)
    {
        error = null;
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out error))
            return false;

        var handler = VotingHandlerFactory.GetHandler(GameStage.DoctorTurn, player!.Role);
        if (handler is null)
        {
            error = GameConstants.Errors.DoctorTurnNotAvailable;
            return false;
        }

        error = handler.GetNotValidError(lobby!, player!, targetId);
        if (error is not null) return false;
        
        handler.SetVote(lobby!, player!, targetId);
        return true;
    }

    public bool CancelDoctorVote(string code, Guid playerId, out string? error)
    {
        error = null;
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out error))
            return false;

        var handler = VotingHandlerFactory.GetHandler(GameStage.DoctorTurn, player!.Role);
        if (handler is null || !handler.CanVote(lobby!, player!))
        {
            error = GameConstants.Errors.CannotCancelNow;
            return false;
        }

        handler.ClearVote(lobby!, player!);
        return true;
    }

    public bool NecromancerAction(string code, Guid playerId, Guid targetId, out string? error)
    {
        error = null;
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out error))
            return false;

        var handler = VotingHandlerFactory.GetHandler(GameStage.NecromancerTurn, player!.Role);
        if (handler is null)
        {
            error = GameConstants.Errors.NecromancerTurnNotAvailable;
            return false;
        }

        error = handler.GetNotValidError(lobby!, player!, targetId);
        if (error is not null) return false;
        
        handler.SetVote(lobby!, player!, targetId);
        return true;
    }

    public bool CancelNecromancerVote(string code, Guid playerId, out string? error)
    {
        error = null;
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out error))
            return false;

        var handler = VotingHandlerFactory.GetHandler(GameStage.NecromancerTurn, player!.Role);
        if (handler is null || !handler.CanVote(lobby!, player!))
        {
            error = GameConstants.Errors.CannotCancelNow;
            return false;
        }

        handler.ClearVote(lobby!, player!);
        return true;
    }

    public bool CommissionerCheck(string code, Guid playerId, Guid targetId, out string? error)
    {
        error = null;
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out error))
            return false;

        var handler = VotingHandlerFactory.GetCommissionerHandler();
        return handler.CommissionerCheck(lobby!, player!, targetId, out error);
    }

    public bool CommissionerKill(string code, Guid playerId, Guid targetId, out string? error)
    {
        error = null;
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out error))
            return false;

        var handler = VotingHandlerFactory.GetCommissionerHandler();
        return handler.CommissionerKill(lobby!, player!, targetId, out error);
    }

    public bool SetCommissionerIsKill(string code, Guid playerId, bool isKill, out string? error)
    {
        error = null;
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out error))
            return false;

        var handler = VotingHandlerFactory.GetCommissionerHandler();
        return handler.SetCommissionerIsKill(lobby!, player!, isKill, out error);
    }

    public bool CommissionerVote(string code, Guid playerId, Guid targetId, out string? error)
    {
        error = null;
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out error))
            return false;

        var handler = VotingHandlerFactory.GetCommissionerHandler();
        return handler.CommissionerVote(lobby!, player!, targetId, out error);
    }

    public bool CancelCommissionerVote(string code, Guid playerId, out string? error)
    {
        error = null;
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out error))
            return false;

        var handler = VotingHandlerFactory.GetCommissionerHandler();
        return handler.CancelCommissionerVote(lobby!, player!, out error);
    }
}