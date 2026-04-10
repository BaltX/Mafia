using Mafia.Models;

namespace Mafia.Services.Handlers;

public interface IVotingHandler
{
    GameStage RequiredStage { get; }
    bool CanVote(LobbyState lobby, PlayerState player);
    string? GetNotValidError(LobbyState lobby, PlayerState player, Guid targetId);
    void SetVote(LobbyState lobby, PlayerState player, Guid targetId);
    void ClearVote(LobbyState lobby, PlayerState player);
}