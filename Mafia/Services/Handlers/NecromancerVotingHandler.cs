using Mafia.Models;

namespace Mafia.Services.Handlers;

public class NecromancerVotingHandler : IVotingHandler
{
    public GameStage RequiredStage => GameStage.NecromancerTurn;

    public bool CanVote(LobbyState lobby, PlayerState player) =>
        lobby.Stage == GameStage.NecromancerTurn && !player.IsDead() && player.IsNecromancer();

    public string? GetNotValidError(LobbyState lobby, PlayerState player, Guid targetId)
    {
        if (!CanVote(lobby, player))
            return GameConstants.Errors.NecromancerTurnNotAvailable;

        if (!ValidateTarget(lobby, player, targetId))
            return GameConstants.Errors.InvalidTarget;

        return null;
    }

    public void SetVote(LobbyState lobby, PlayerState player, Guid targetId) =>
        lobby.NecromancerVote = targetId;

    public void ClearVote(LobbyState lobby, PlayerState player) =>
        lobby.NecromancerVote = null;

    private static bool ValidateTarget(LobbyState lobby, PlayerState player, Guid targetId)
    {
        var target = lobby.GetPlayer(targetId);
        return target is not null && 
               !target.IsHost() && 
               targetId != player.Id && 
               !target.IsZombie && 
               target.Role != GameRole.Killer &&
               target.Role != GameRole.Necromancer;
    }
}