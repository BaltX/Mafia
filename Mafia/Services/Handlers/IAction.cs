using Mafia.Models;

namespace Mafia.Services.Handlers;

public interface IAction
{
    GameStage RequiredStage { get; }
    GameRole? RequiredRole { get; }
    bool CanExecute(LobbyState lobby, PlayerState player);
    string? Validate(LobbyState lobby, PlayerState player, Guid targetId);
    void Execute(LobbyState lobby, PlayerState player, Guid targetId);
    void Cancel(LobbyState lobby, PlayerState player);
}