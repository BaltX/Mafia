using Mafia.Models;

namespace Mafia.Services;

public class MafiaGameService
{
    private readonly LobbyService _lobbyService = new();
    private readonly MafiaGameLogicService _gameLogicService = new();
    private readonly MafiaNightService _nightService = new();
    private readonly VotingCoordinator _votingCoordinator;

    public MafiaGameService()
    {
        _votingCoordinator = new VotingCoordinator(_lobbyService, _nightService);
    }

    public (LobbyState Lobby, PlayerState Host)? CreateLobby(string hostName) =>
        _lobbyService.CreateLobby(hostName);

    public List<LobbyListItemViewModel> GetLobbies() =>
        _lobbyService.GetLobbies();

    public (LobbyState Lobby, PlayerState Player)? JoinLobby(string code, string name) =>
        _lobbyService.JoinLobby(code, name);

    public (LobbyState Lobby, PlayerState Player)? AddBot(string code, Guid hostId, out string? error) =>
        _lobbyService.AddBot(code, hostId, out error);

    public (LobbyState Lobby, PlayerState Player)? GetState(string code, Guid playerId) =>
        _votingCoordinator.GetState(code, playerId);

    public bool StartGame(string code, Guid hostId, out string? error)
    {
        error = null;
        if (!_lobbyService.TryGetHostLobby(code, hostId, out var lobby, out error))
            return false;

        return _gameLogicService.StartGame(lobby!, out error);
    }

    public bool DayVote(string code, Guid playerId, Guid targetId, out string? error) =>
        _votingCoordinator.DayVote(code, playerId, targetId, out error);

    public bool CancelDayVote(string code, Guid playerId, out string? error) =>
        _votingCoordinator.CancelDayVote(code, playerId, out error);

    public bool MafiaVote(string code, Guid playerId, Guid targetId, out string? error) =>
        _votingCoordinator.MafiaVote(code, playerId, targetId, out error);

    public bool CancelMafiaVote(string code, Guid playerId, out string? error) =>
        _votingCoordinator.CancelMafiaVote(code, playerId, out error);

    public bool ManiacVote(string code, Guid playerId, Guid targetId, out string? error) =>
        _votingCoordinator.ManiacVote(code, playerId, targetId, out error);

    public bool CancelManiacVote(string code, Guid playerId, out string? error) =>
        _votingCoordinator.CancelManiacVote(code, playerId, out error);

    public bool CommissionerCheck(string code, Guid playerId, Guid targetId, out string? error) =>
        _votingCoordinator.CommissionerCheck(code, playerId, targetId, out error);

    public bool CommissionerKill(string code, Guid playerId, Guid targetId, out string? error) =>
        _votingCoordinator.CommissionerKill(code, playerId, targetId, out error);

    public bool SetCommissionerIsKill(string code, Guid playerId, bool isKill, out string? error) =>
        _votingCoordinator.SetCommissionerIsKill(code, playerId, isKill, out error);

    public bool CommissionerVote(string code, Guid playerId, Guid targetId, out string? error) =>
        _votingCoordinator.CommissionerVote(code, playerId, targetId, out error);

    public bool CancelCommissionerVote(string code, Guid playerId, out string? error) =>
        _votingCoordinator.CancelCommissionerVote(code, playerId, out error);

    public bool BeautyAction(string code, Guid playerId, Guid targetId, out string? error) =>
        _votingCoordinator.BeautyAction(code, playerId, targetId, out error);

    public bool CancelBeautyVote(string code, Guid playerId, out string? error) =>
        _votingCoordinator.CancelBeautyVote(code, playerId, out error);

    public bool DoctorAction(string code, Guid playerId, Guid targetId, out string? error) =>
        _votingCoordinator.DoctorAction(code, playerId, targetId, out error);

    public bool CancelDoctorVote(string code, Guid playerId, out string? error) =>
        _votingCoordinator.CancelDoctorVote(code, playerId, out error);

    public bool NecromancerAction(string code, Guid playerId, Guid targetId, out string? error) =>
        _votingCoordinator.NecromancerAction(code, playerId, targetId, out error);

    public bool CancelNecromancerVote(string code, Guid playerId, out string? error) =>
        _votingCoordinator.CancelNecromancerVote(code, playerId, out error);

    public bool NextStage(string code, Guid hostId, out string? error)
    {
        error = null;
        if (!_lobbyService.TryGetHostLobby(code, hostId, out var lobby, out error))
            return false;

        return _nightService.NextStage(lobby!, out error);
    }
}