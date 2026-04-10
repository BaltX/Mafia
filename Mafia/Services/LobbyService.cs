using Mafia.Models;
using Mafia.Services.Handlers;

namespace Mafia.Services;

public class LobbyService
{
    private readonly object _sync = new();
    private readonly Dictionary<string, LobbyState> _lobbies = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Random SharedRandom = Random.Shared;

    public (LobbyState Lobby, PlayerState Host)? CreateLobby(string hostName)
    {
        if (string.IsNullOrWhiteSpace(hostName))
            return null;

        var trimmedName = hostName.Trim();
        if (trimmedName.Length > 20 || trimmedName.Any(c => !char.IsLetterOrDigit(c) && c != ' '))
            return null;

        lock (_sync)
        {
            var code = GenerateCode();
            var host = new PlayerState
            {
                Name = trimmedName,
                Role = GameRole.Host,
                IsAlive = true
            };

            var lobby = new LobbyState { Code = code };
            lobby.Players.Add(host);
            _lobbies[code] = lobby;
            return (lobby, host);
        }
    }

    public List<LobbyListItemViewModel> GetLobbies()
    {
        lock (_sync)
        {
            return _lobbies.Values
                .OrderBy(x => x.Code)
                .Select(lobby => new LobbyListItemViewModel
                {
                    Code = lobby.Code,
                    StageText = lobby.Stage.GetDisplayText(),
                    TotalPlayers = lobby.Players.Count
                })
                .ToList();
        }
    }

    public (LobbyState Lobby, PlayerState Player)? JoinLobby(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var trimmedName = name.Trim();
        if (trimmedName.Length > 20 || trimmedName.Any(c => !char.IsLetterOrDigit(c) && c != ' '))
            return null;

        lock (_sync)
        {
            if (!_lobbies.TryGetValue(code.Trim(), out var lobby) || lobby.Stage != GameStage.Lobby)
                return null;

            var player = new PlayerState
            {
                Name = trimmedName,
                Role = GameRole.Unassigned,
                IsAlive = true
            };
            lobby.Players.Add(player);
            return (lobby, player);
        }
    }

    public (LobbyState Lobby, PlayerState Player)? AddBot(string code, Guid hostId, out string? error)
    {
        lock (_sync)
        {
            error = null;
            if (!TryGetHostLobby(code, hostId, out var lobby, out error))
                return null;

            if (lobby!.Stage != GameStage.Lobby)
            {
                error = GameConstants.Errors.BotsOnlyInLobby;
                return null;
            }

            var botNumber = lobby.Players.Count(p => p.IsBot) + 1;
            var bot = new PlayerState
            {
                Name = $"Бот {botNumber}",
                Role = GameRole.Unassigned,
                IsAlive = true,
                IsBot = true
            };
            lobby.Players.Add(bot);
            return (lobby, bot);
        }
    }

    public (LobbyState Lobby, PlayerState Player)? GetState(string code, Guid playerId)
    {
        lock (_sync)
        {
            CleanupOldLobbies();

            if (!_lobbies.TryGetValue(code.Trim(), out var lobby))
                return null;

            var player = lobby.Players.FirstOrDefault(p => p.Id == playerId);
            return player is null ? null : (lobby, player);
        }
    }

    public bool TryGetLobby(string code, out LobbyState? lobby)
    {
        lock (_sync)
        {
            return _lobbies.TryGetValue(code.Trim(), out lobby);
        }
    }

    public bool TryGetLobbyForPlayer(string code, Guid playerId, out LobbyState? lobby, out PlayerState? player, out string? error)
    {
        error = null;
        player = null;
        lobby = null;

        lock (_sync)
        {
            if (!_lobbies.TryGetValue(code.Trim(), out lobby))
            {
                error = GameConstants.Errors.LobbyNotFound;
                return false;
            }

            player = lobby.Players.FirstOrDefault(p => p.Id == playerId);
            if (player is null)
            {
                error = GameConstants.Errors.PlayerNotFoundInLobby;
                return false;
            }

            return true;
        }
    }

    public bool TryGetHostLobby(string code, Guid hostId, out LobbyState? lobby, out string? error)
    {
        error = null;
        lobby = null;

        lock (_sync)
        {
            if (!_lobbies.TryGetValue(code.Trim(), out lobby))
            {
                error = GameConstants.Errors.LobbyNotFound;
                return false;
            }

            var host = lobby.Players.FirstOrDefault(p => p.Id == hostId);
            if (host is null || host.Role != GameRole.Host)
            {
                error = GameConstants.Errors.OnlyHostCanDoThis;
                return false;
            }

            return true;
        }
    }

    public void CleanupOldLobbies()
    {
        var threshold = DateTimeOffset.UtcNow.AddHours(-1);
        var toRemove = _lobbies
            .Where(kvp => kvp.Value.Stage == GameStage.GameOver && kvp.Value.StageEndsAtUtc < threshold)
            .Select(kvp => kvp.Key)
            .ToList();
        foreach (var code in toRemove)
        {
            _lobbies.Remove(code);
        }
    }

    private string GenerateCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        while (true)
        {
            var code = new string(Enumerable.Range(0, 6).Select(_ => alphabet[SharedRandom.Next(alphabet.Length)]).ToArray());
            if (!_lobbies.ContainsKey(code))
                return code;
        }
    }
}