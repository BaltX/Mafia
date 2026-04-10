using Mafia.Models;

namespace Mafia.Services.Handlers;

public abstract class ActionBase : IAction
{
    public abstract GameStage RequiredStage { get; }
    public abstract GameRole? RequiredRole { get; }

    public virtual bool CanExecute(LobbyState lobby, PlayerState player) =>
        lobby.Stage == RequiredStage && !player.IsDead() && (RequiredRole is null || player.Role == RequiredRole);

    public abstract string? Validate(LobbyState lobby, PlayerState player, Guid targetId);
    public abstract void Execute(LobbyState lobby, PlayerState player, Guid targetId);
    public abstract void Cancel(LobbyState lobby, PlayerState player);
}