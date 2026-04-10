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

    public (LobbyState Lobby, PlayerState Player)? GetState(string code, Guid playerId)
    {
        var result = _lobbyService.GetState(code, playerId);
        if (result is null)
            return result;

        var now = DateTimeOffset.UtcNow;
        while (result.Value.Lobby.StageEndsAtUtc.HasValue && 
               now >= result.Value.Lobby.StageEndsAtUtc.Value && 
               result.Value.Lobby.Stage != GameStage.Lobby && 
               result.Value.Lobby.Stage != GameStage.GameOver)
        {
            BotActionService.ProcessBotVotes(result.Value.Lobby, result.Value.Lobby.Stage);
            _nightService.NextStage(result.Value.Lobby, out _);
        }

        return result;
    }

    public bool Vote(string code, Guid playerId, Guid targetId, out string? error, Func<IAction?> getHandler)
    {
        error = null;
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out error))
            return false;

        var handler = getHandler();
        if (handler is null)
        {
            error = GameConstants.Errors.CannotVoteNow;
            return false;
        }

        error = handler.Validate(lobby!, player!, targetId);
        if (error is not null) return false;
        
        handler.Execute(lobby!, player!, targetId);
        return true;
    }

    public bool CancelVote(string code, Guid playerId, out string? error, Func<IAction?> getHandler)
    {
        error = null;
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out error))
            return false;

        var handler = getHandler();
        if (handler is null || !handler.CanExecute(lobby!, player!))
        {
            error = GameConstants.Errors.CannotCancelNow;
            return false;
        }

        handler.Cancel(lobby!, player!);
        return true;
    }

    public bool DayVote(string code, Guid playerId, Guid targetId, out string? error) =>
        Vote(code, playerId, targetId, out error, () => ActionFactory.Get(GameStage.DayVoting, null));

    public bool CancelDayVote(string code, Guid playerId, out string? error) =>
        CancelVote(code, playerId, out error, () => ActionFactory.Get(GameStage.DayVoting, null));

    public bool MafiaVote(string code, Guid playerId, Guid targetId, out string? error) =>
        Vote(code, playerId, targetId, out error, () => ActionFactory.Get(GameStage.MafiaTurn, null));

    public bool CancelMafiaVote(string code, Guid playerId, out string? error) =>
        CancelVote(code, playerId, out error, () => ActionFactory.Get(GameStage.MafiaTurn, null));

    public bool ManiacVote(string code, Guid playerId, Guid targetId, out string? error) =>
        Vote(code, playerId, targetId, out error, () => ActionFactory.Get(GameStage.KillerTurn, null));

    public bool CancelManiacVote(string code, Guid playerId, out string? error) =>
        CancelVote(code, playerId, out error, () => ActionFactory.Get(GameStage.KillerTurn, null));

    public bool BeautyAction(string code, Guid playerId, Guid targetId, out string? error) =>
        Vote(code, playerId, targetId, out error, () => ActionFactory.Get(GameStage.BeautyTurn, null));

    public bool CancelBeautyVote(string code, Guid playerId, out string? error) =>
        CancelVote(code, playerId, out error, () => ActionFactory.Get(GameStage.BeautyTurn, null));

    public bool DoctorAction(string code, Guid playerId, Guid targetId, out string? error) =>
        Vote(code, playerId, targetId, out error, () => ActionFactory.Get(GameStage.DoctorTurn, null));

    public bool CancelDoctorVote(string code, Guid playerId, out string? error) =>
        CancelVote(code, playerId, out error, () => ActionFactory.Get(GameStage.DoctorTurn, null));

    public bool NecromancerAction(string code, Guid playerId, Guid targetId, out string? error) =>
        Vote(code, playerId, targetId, out error, () => ActionFactory.Get(GameStage.NecromancerTurn, null));

    public bool CancelNecromancerVote(string code, Guid playerId, out string? error) =>
        CancelVote(code, playerId, out error, () => ActionFactory.Get(GameStage.NecromancerTurn, null));

    public bool CommissionerCheck(string code, Guid playerId, Guid targetId, out string? error)
    {
        error = null;
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out error))
            return false;
        return ActionFactory.GetCommissioner().CommissionerCheck(lobby!, player!, targetId, out error);
    }

    public bool CommissionerKill(string code, Guid playerId, Guid targetId, out string? error)
    {
        error = null;
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out error))
            return false;
        return ActionFactory.GetCommissioner().CommissionerKill(lobby!, player!, targetId, out error);
    }

    public bool SetCommissionerIsKill(string code, Guid playerId, bool isKill, out string? error)
    {
        error = null;
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out error))
            return false;
        return ActionFactory.GetCommissioner().SetCommissionerIsKill(lobby!, player!, isKill, out error);
    }

    public bool CommissionerVote(string code, Guid playerId, Guid targetId, out string? error)
    {
        error = null;
        if (!_lobbyService.TryGetLobbyForPlayer(code, playerId, out var lobby, out var player, out error))
            return false;
        var handler = ActionFactory.GetCommissioner();
        error = handler.Validate(lobby!, player!, targetId);
        if (error is not null) return false;
        handler.Execute(lobby!, player!, targetId);
        return true;
    }

    public bool CancelCommissionerVote(string code, Guid playerId, out string? error) =>
        CancelVote(code, playerId, out error, () => ActionFactory.Get(GameStage.CommissionerTurn, null));
}