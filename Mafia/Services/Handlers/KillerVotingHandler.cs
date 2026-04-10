using Mafia.Models;

namespace Mafia.Services.Handlers;

public class KillerVotingHandler : IVotingHandler
{
    public GameStage RequiredStage => GameStage.KillerTurn;

    public bool CanVote(LobbyState lobby, PlayerState player) =>
        lobby.Stage == GameStage.KillerTurn && !player.IsDead() && player.IsKiller();

    public string? GetNotValidError(LobbyState lobby, PlayerState player, Guid targetId)
    {
        if (!CanVote(lobby, player))
            return GameConstants.Errors.ManiacTurnNotAvailable;

        if (!ValidateTarget(lobby, player, targetId))
            return GameConstants.Errors.InvalidTarget;

        return null;
    }

    public void SetVote(LobbyState lobby, PlayerState player, Guid targetId) =>
        lobby.KillerVote = targetId;

    public void ClearVote(LobbyState lobby, PlayerState player) =>
        lobby.KillerVote = null;

    private static bool ValidateTarget(LobbyState lobby, PlayerState player, Guid targetId)
    {
        var target = lobby.GetPlayer(targetId);
        return target is not null && target.IsAlive && !target.IsHost() && targetId != player.Id;
    }
}