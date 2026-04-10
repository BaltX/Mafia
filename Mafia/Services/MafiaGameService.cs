using Mafia.Models;

namespace Mafia.Services;

public class MafiaGameService
{
    private readonly object _sync = new();
    private readonly Dictionary<string, LobbyState> _lobbies = new(StringComparer.OrdinalIgnoreCase);
    private readonly MafiaRoundService _roundService = new();
    private static readonly Random SharedRandom = Random.Shared;

    public (LobbyState Lobby, PlayerState Host)? CreateLobby(string hostName)
    {
        if (string.IsNullOrWhiteSpace(hostName))
        {
            return null;
        }

        var trimmedName = hostName.Trim();
        if (trimmedName.Length > 20 || trimmedName.Any(c => !char.IsLetterOrDigit(c) && c != ' '))
        {
            return null;
        }

        lock (_sync)
        {
            var code = GenerateCode();
            var host = new PlayerState
            {
                Name = trimmedName,
                Role = GameRole.Host,
                IsAlive = true
            };

            var lobby = new LobbyState
            {
                Code = code
            };
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
                    StageText = StageText(lobby.Stage),
                    TotalPlayers = lobby.Players.Count
                })
                .ToList();
        }
    }

    public (LobbyState Lobby, PlayerState Player)? JoinLobby(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var trimmedName = name.Trim();
        if (trimmedName.Length > 20 || trimmedName.Any(c => !char.IsLetterOrDigit(c) && c != ' '))
        {
            return null;
        }

        lock (_sync)
        {
            if (!_lobbies.TryGetValue(code.Trim(), out var lobby) || lobby.Stage != GameStage.Lobby)
            {
                return null;
            }

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
            {
                return null;
            }

            if (lobby!.Stage != GameStage.Lobby)
            {
                error = "Ботов можно добавлять только в лобби.";
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
            {
                return null;
            }

            var now = DateTimeOffset.UtcNow;
            while (lobby.StageEndsAtUtc.HasValue && now >= lobby.StageEndsAtUtc.Value && lobby.Stage != GameStage.Lobby && lobby.Stage != GameStage.GameOver)
            {
                _roundService.ProcessBotVotes(lobby, lobby.Stage);
                _roundService.NextStage(lobby, out _);
            }

            var player = lobby.Players.FirstOrDefault(p => p.Id == playerId);
            return player is null ? null : (lobby, player);
        }
    }

    public bool StartGame(string code, Guid hostId, out string? error)
    {
        lock (_sync)
        {
            error = null;
            if (!TryGetHostLobby(code, hostId, out var lobby, out error))
            {
                return false;
            }

            return _roundService.StartGame(lobby!, out error);
        }
    }

    public bool DayVote(string code, Guid playerId, Guid targetId, out string? error)
    {
        lock (_sync)
        {
            error = null;
            if (!TryGetPlayerLobby(code, playerId, out var lobby, out var player, out error))
            {
                return false;
            }

            return _roundService.DayVote(lobby!, player!, targetId, out error);
        }
    }

    public bool CancelDayVote(string code, Guid playerId, out string? error)
    {
        lock (_sync)
        {
            error = null;
            if (!TryGetPlayerLobby(code, playerId, out var lobby, out var player, out error))
            {
                return false;
            }

            return _roundService.CancelDayVote(lobby!, player!, out error);
        }
    }

    public bool MafiaVote(string code, Guid playerId, Guid targetId, out string? error)
    {
        lock (_sync)
        {
            error = null;
            if (!TryGetPlayerLobby(code, playerId, out var lobby, out var player, out error))
            {
                return false;
            }

            return _roundService.MafiaVote(lobby!, player!, targetId, out error);
        }
    }

    public bool CancelMafiaVote(string code, Guid playerId, out string? error)
    {
        lock (_sync)
        {
            error = null;
            if (!TryGetPlayerLobby(code, playerId, out var lobby, out var player, out error))
            {
                return false;
            }

            return _roundService.CancelMafiaVote(lobby!, player!, out error);
        }
    }

    public bool ManiacVote(string code, Guid playerId, Guid targetId, out string? error)
    {
        lock (_sync)
        {
            error = null;
            if (!TryGetPlayerLobby(code, playerId, out var lobby, out var player, out error))
            {
                return false;
            }

            return _roundService.ManiacVote(lobby!, player!, targetId, out error);
        }
    }

    public bool CancelManiacVote(string code, Guid playerId, out string? error)
    {
        lock (_sync)
        {
            error = null;
            if (!TryGetPlayerLobby(code, playerId, out var lobby, out var player, out error))
            {
                return false;
            }

            return _roundService.CancelManiacVote(lobby!, player!, out error);
        }
    }

    public bool CommissionerAction(string code, Guid playerId, Guid targetId, bool isKill, out string? error)
    {
        lock (_sync)
        {
            error = null;
            if (!TryGetPlayerLobby(code, playerId, out var lobby, out var player, out error))
            {
                return false;
            }

            return _roundService.CommissionerAction(lobby!, player!, targetId, isKill, out error);
        }
    }

    public bool CommissionerCheck(string code, Guid playerId, Guid targetId, out string? error)
    {
        lock (_sync)
        {
            error = null;
            if (!TryGetPlayerLobby(code, playerId, out var lobby, out var player, out error))
            {
                return false;
            }

            return _roundService.CommissionerAction(lobby!, player!, targetId, false, out error);
        }
    }

    public bool CommissionerKill(string code, Guid playerId, Guid targetId, out string? error)
    {
        lock (_sync)
        {
            error = null;
            if (!TryGetPlayerLobby(code, playerId, out var lobby, out var player, out error))
            {
                return false;
            }

            return _roundService.CommissionerAction(lobby!, player!, targetId, true, out error);
        }
    }

    public bool SetCommissionerIsKill(string code, Guid playerId, bool isKill, out string? error)
    {
        lock (_sync)
        {
            error = null;
            if (!TryGetPlayerLobby(code, playerId, out var lobby, out var player, out error))
            {
                return false;
            }

            return _roundService.SetCommissionerIsKill(lobby!, player!, isKill, out error);
        }
    }

    public bool CommissionerVote(string code, Guid playerId, Guid targetId, out string? error)
    {
        lock (_sync)
        {
            error = null;
            if (!TryGetPlayerLobby(code, playerId, out var lobby, out var player, out error))
            {
                return false;
            }

            return _roundService.CommissionerVote(lobby!, player!, targetId, out error);
        }
    }

    public bool CancelCommissionerVote(string code, Guid playerId, out string? error)
    {
        lock (_sync)
        {
            error = null;
            if (!TryGetPlayerLobby(code, playerId, out var lobby, out var player, out error))
            {
                return false;
            }

            return _roundService.CancelCommissionerVote(lobby!, player!, out error);
        }
    }

    public bool BeautyAction(string code, Guid playerId, Guid targetId, out string? error)
    {
        lock (_sync)
        {
            error = null;
            if (!TryGetPlayerLobby(code, playerId, out var lobby, out var player, out error))
            {
                return false;
            }

            return _roundService.BeautyAction(lobby!, player!, targetId, out error);
        }
    }

    public bool CancelBeautyVote(string code, Guid playerId, out string? error)
    {
        lock (_sync)
        {
            error = null;
            if (!TryGetPlayerLobby(code, playerId, out var lobby, out var player, out error))
            {
                return false;
            }

            return _roundService.CancelBeautyVote(lobby!, player!, out error);
        }
    }

    public bool DoctorAction(string code, Guid playerId, Guid targetId, out string? error)
    {
        lock (_sync)
        {
            error = null;
            if (!TryGetPlayerLobby(code, playerId, out var lobby, out var player, out error))
            {
                return false;
            }

            return _roundService.DoctorAction(lobby!, player!, targetId, out error);
        }
    }

    public bool CancelDoctorVote(string code, Guid playerId, out string? error)
    {
        lock (_sync)
        {
            error = null;
            if (!TryGetPlayerLobby(code, playerId, out var lobby, out var player, out error))
            {
                return false;
            }

            return _roundService.CancelDoctorVote(lobby!, player!, out error);
        }
    }

    public bool NecromancerAction(string code, Guid playerId, Guid targetId, out string? error)
    {
        lock (_sync)
        {
            error = null;
            if (!TryGetPlayerLobby(code, playerId, out var lobby, out var player, out error))
            {
                return false;
            }

            return _roundService.NecromancerAction(lobby!, player!, targetId, out error);
        }
    }

    public bool CancelNecromancerVote(string code, Guid playerId, out string? error)
    {
        lock (_sync)
        {
            error = null;
            if (!TryGetPlayerLobby(code, playerId, out var lobby, out var player, out error))
            {
                return false;
            }

            return _roundService.CancelNecromancerVote(lobby!, player!, out error);
        }
    }

    public bool NextStage(string code, Guid hostId, out string? error)
    {
        lock (_sync)
        {
            error = null;
            if (!TryGetHostLobby(code, hostId, out var lobby, out error))
            {
                return false;
            }

            return _roundService.NextStage(lobby!, out error);
        }
    }

    private bool TryGetHostLobby(string code, Guid hostId, out LobbyState? lobby, out string? error)
    {
        error = null;
        lobby = null;
        if (!_lobbies.TryGetValue(code.Trim(), out lobby))
        {
            error = "Лобби не найдено.";
            return false;
        }

        var host = lobby.Players.FirstOrDefault(p => p.Id == hostId);
        if (host is null || host.Role != GameRole.Host)
        {
            error = "Только ведущий может выполнить это действие.";
            return false;
        }

        return true;
    }

    private bool TryGetPlayerLobby(string code, Guid playerId, out LobbyState? lobby, out PlayerState? player, out string? error)
    {
        error = null;
        player = null;
        lobby = null;
        if (!_lobbies.TryGetValue(code.Trim(), out lobby))
        {
            error = "Лобби не найдено.";
            return false;
        }

        player = lobby.Players.FirstOrDefault(p => p.Id == playerId);
        if (player is null)
        {
            error = "Игрок не найден в лобби.";
            return false;
        }

        return true;
    }

    private string GenerateCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        while (true)
        {
            var code = new string(Enumerable.Range(0, 6).Select(_ => alphabet[SharedRandom.Next(alphabet.Length)]).ToArray());
            if (!_lobbies.ContainsKey(code))
            {
                return code;
            }
        }
    }

    private void CleanupOldLobbies()
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

    private static string StageText(GameStage stage) => stage switch
    {
        GameStage.Lobby => "Ожидание",
        GameStage.Discussion => "Обсуждение",
        GameStage.DayVoting => "Дневное голосование",
        GameStage.NightStart => "Начало ночи",
        GameStage.BeautyTurn => "Ход красотки",
        GameStage.DoctorTurn => "Ход доктора",
        GameStage.CommissionerTurn => "Ход комиссара",
        GameStage.MafiaTurn => "Ход мафии",
        GameStage.KillerTurn => "Ход маньяка",
        GameStage.NecromancerTurn => "Ход некроманта",
        GameStage.NightResult => "Результат ночи",
        GameStage.GameOver => "Завершена",
        _ => stage.ToString()
    };
}
