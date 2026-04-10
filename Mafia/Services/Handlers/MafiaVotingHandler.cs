using Mafia.Models;

namespace Mafia.Services.Handlers;

public class MafiaVotingHandler : IVotingHandler
{
    public GameStage RequiredStage => GameStage.MafiaTurn;

    public bool CanVote(LobbyState lobby, PlayerState player) =>
        lobby.Stage == GameStage.MafiaTurn && !player.IsDead() && player.IsMafia();

    public string? GetNotValidError(LobbyState lobby, PlayerState player, Guid targetId)
    {
        if (!CanVote(lobby, player))
            return GameConstants.Errors.MafiaTurnNotAvailable;

        if (!ValidateTarget(lobby, player, targetId))
            return GameConstants.Errors.CannotVoteMafiaToMafia;

        return null;
    }

    public void SetVote(LobbyState lobby, PlayerState player, Guid targetId) =>
        lobby.MafiaVotes[player.Id] = targetId;

    public void ClearVote(LobbyState lobby, PlayerState player) =>
        lobby.MafiaVotes.Remove(player.Id);

    private static bool ValidateTarget(LobbyState lobby, PlayerState player, Guid targetId)
    {
        var target = lobby.GetPlayer(targetId);
        return target is not null && target.IsAlive && !target.IsHost() && !target.IsMafia();
    }
}