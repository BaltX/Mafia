using Mafia.Services;
using Microsoft.AspNetCore.Mvc;
using Mafia.Models;

namespace Mafia.Controllers;

public class GameController(MafiaGameService gameService) : Controller
{
    [HttpGet]
    public IActionResult Index(string? error = null)
    {
        ViewBag.Error = error;
        return View(new GameHomeViewModel
        {
            Lobbies = gameService.GetLobbies()
        });
    }

    [HttpPost]
    public IActionResult CreateLobby(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            return RedirectToAction(nameof(Index), new { error = "Введите имя ведущего." });
        }

        var result = gameService.CreateLobby(playerName);
        if (result is null)
        {
            return RedirectToAction(nameof(Index), new { error = "Недопустимое имя ведущего." });
        }
        
        return RedirectToAction(nameof(Lobby), new { code = result.Value.Lobby.Code, playerId = result.Value.Host.Id });
    }

    [HttpPost]
    public IActionResult JoinLobby(string code, string playerName)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(playerName))
        {
            return RedirectToAction(nameof(Index), new { error = "Введите код лобби и имя игрока." });
        }

        var result = gameService.JoinLobby(code.ToUpperInvariant(), playerName);
        if (result is null)
        {
            return RedirectToAction(nameof(Index), new { error = "Не удалось присоединиться. Проверьте код или статус игры." });
        }

        return RedirectToAction(nameof(Lobby), new { code = result.Value.Lobby.Code, playerId = result.Value.Player.Id });
    }

    [HttpGet]
    public IActionResult Lobby(string code, Guid playerId, string? error = null)
    {
        var state = gameService.GetState(code, playerId);
        if (state is null)
        {
            return RedirectToAction(nameof(Index), new { error = "Лобби или игрок не найдены." });
        }

        ViewBag.Error = error;
        return View(new Mafia.Models.GamePageViewModel
        {
            Lobby = state.Value.Lobby,
            CurrentPlayer = state.Value.Player
        });
    }

    [HttpGet]
    public IActionResult LobbyContent(string code, Guid playerId)
    {
        var state = gameService.GetState(code, playerId);
        if (state is null)
        {
            return NotFound();
        }

        return PartialView("_LobbyContent", new GamePageViewModel
        {
            Lobby = state.Value.Lobby,
            CurrentPlayer = state.Value.Player
        });
    }

    [HttpGet]
    public IActionResult LobbyState(string code, Guid playerId)
    {
        var state = gameService.GetState(code, playerId);
        if (state is null)
        {
            return NotFound();
        }

        var lobby = state.Value.Lobby;
        var player = state.Value.Player;

        var lastDayVictimName = lobby.LastDayVictimId.HasValue
            ? lobby.Players.FirstOrDefault(p => p.Id == lobby.LastDayVictimId)?.Name
            : null;

        var votingTargets = GetVotingTargets(lobby, player);
        var currentPlayerVote = GetCurrentPlayerVote(lobby, player);
        var isHostView = player.IsHost();
        var isNecromancer = player.Role == GameRole.Necromancer;

        var isNightStage = IsNightStage(lobby.Stage);
        var isParticipatingInNight = isNightStage && IsPlayerParticipatingInNightStage(lobby.Stage, player);
        var showNightOverlay = isNightStage && !isParticipatingInNight && !player.IsHost() && player.IsAlive && player.Role != GameRole.Host;

        return Json(new
        {
            code = lobby.Code,
            stage = lobby.Stage.ToString(),
            round = lobby.Round,
            stageEndsAtUtc = lobby.StageEndsAtUtc?.ToUnixTimeMilliseconds(),
            winnerText = lobby.WinnerText,
            isHost = player.IsHost(),
            showRoles = isHostView || player.Role != GameRole.Host,
            showZombie = isNecromancer,
            showNightOverlay = showNightOverlay,
            currentPlayer = new
            {
                id = player.Id,
                name = player.Name,
                role = player.Role.ToString(),
                isAlive = player.IsAlive,
                isZombie = player.IsZombie,
                isBot = player.IsBot
            },
            players = lobby.Players.Select(p => new
            {
                id = p.Id,
                name = p.Name,
                role = isHostView || p.Id == player.Id ? p.Role.ToString() : "???",
                isAlive = p.IsAlive,
                isZombie = p.IsZombie && (isHostView || isNecromancer),
                isBot = p.IsBot,
                isMafiaChecked = p.IsMafiaChecked
            }).ToList(),
            votingTargets = votingTargets,
            currentVote = currentPlayerVote,
            commissionerKillTargets = player.Role == GameRole.Commissioner ? GetCommissionerKillTargets(lobby, player) : [],
            commissionerChecks = (player.Role == GameRole.Commissioner || isHostView) ? GetCommissionerChecks(lobby) : null,
            hasCommissionerCheckPending = player.Role == GameRole.Commissioner && lobby.PendingCommissionerCheckResult.HasValue,
            commissionerIsKill = player.Role == GameRole.Commissioner && lobby.CommissionerIsKill,
            commissionerTargets = player.Role == GameRole.Commissioner && !lobby.PendingCommissionerCheckResult.HasValue 
                ? GetCommissionerKillTargets(lobby, player) 
                : null,
            dayVotes = isHostView ? lobby.DayVotes : null,
            mafiaVotes = isHostView ? lobby.MafiaVotes : null,
            killerVote = isHostView ? lobby.KillerVote : null,
            beautyVote = isHostView ? lobby.BeautyVote : null,
            doctorVote = isHostView ? lobby.DoctorVote : null,
            necromancerVote = isHostView ? lobby.NecromancerVote : null,
            commissionerVote = isHostView ? lobby.CommissionerVote : null,
            lastDayVictimId = lobby.LastDayVictimId,
            lastKilledPlayers = lobby.LastKilledPlayers,
            stageHistory = FilterStageHistory(lobby.StageHistory, isHostView).Select(s => new
            {
                stage = s.StageText.ToString(),
                text = s.ResultText,
                round = s.Round
            }).ToList()
        });
    }

    private static List<StageResultEntry> FilterStageHistory(List<StageResultEntry> history, bool isHost)
    {
        var filtered = new List<StageResultEntry>();
        foreach (var entry in history)
        {
            if (entry.StageText == "Ночные действия" && !isHost)
            {
                var nightTexts = entry.ResultText
                    .Where(t => t.Contains("Убит") || t.Contains("Воскрес") || t == "🌙 Никто не пострадал.")
                    .ToList();
                if (nightTexts.Count > 0)
                {
                    filtered.Add(new StageResultEntry { Round = entry.Round, StageText = "Ночные действия", ResultText = nightTexts });
                }
            }
            else
            {
                filtered.Add(entry);
            }
        }
        return filtered;
    }

    private static List<Guid> GetVotingTargets(LobbyState lobby, PlayerState player)
    {
        if (player.IsDead() || player.IsHost()) return [];

        var stage = lobby.Stage;
        var alivePlayers = lobby.Players.Where(p => p.IsAlive && !p.IsHost() && p.Id != player.Id).ToList();

        return stage switch
        {
            GameStage.DayVoting => alivePlayers.Select(p => p.Id).ToList(),
            GameStage.DayVoting2 when lobby.Day1TopVotedPlayerIds.Count > 0 =>
                alivePlayers.Where(p => lobby.Day1TopVotedPlayerIds.Contains(p.Id)).Select(p => p.Id).ToList(),
            GameStage.MafiaTurn when player.Role is GameRole.Mafia or GameRole.Don =>
                alivePlayers.Where(p => p.Role != GameRole.Mafia && p.Role != GameRole.Don).Select(p => p.Id).ToList(),
            GameStage.KillerTurn when player.Role == GameRole.Killer =>
                alivePlayers.Select(p => p.Id).ToList(),
            GameStage.CommissionerTurn when player.Role == GameRole.Commissioner =>
                alivePlayers.Where(p => p.Role != GameRole.Commissioner).Select(p => p.Id).ToList(),
            GameStage.BeautyTurn when player.Role == GameRole.Beauty =>
                alivePlayers.Select(p => p.Id).ToList(),
            GameStage.DoctorTurn when player.Role == GameRole.Doctor =>
                alivePlayers.Select(p => p.Id).ToList(),
            GameStage.NecromancerTurn when player.Role == GameRole.Necromancer =>
                lobby.Players.Where(p => !p.IsHost() && p.Id != player.Id).Select(p => p.Id).ToList(),
            _ => []
        };
    }

    private static Dictionary<Guid, bool> GetCommissionerChecks(LobbyState lobby)
    {
        return lobby.CommissionerChecks;
    }

    private static List<Guid> GetCommissionerKillTargets(LobbyState lobby, PlayerState player)
    {
        if (player.Role != GameRole.Commissioner || player.IsDead() || player.IsHost()) return [];
        return lobby.Players.Where(p => p.IsAlive && !p.IsHost() && p.Id != player.Id).Select(p => p.Id).ToList();
    }

    private static bool IsNightStage(GameStage stage)
    {
        return stage is GameStage.NightStart or GameStage.BeautyTurn or GameStage.DoctorTurn 
            or GameStage.CommissionerTurn or GameStage.MafiaTurn or GameStage.KillerTurn 
            or GameStage.NecromancerTurn or GameStage.NightResult;
    }

    private static bool IsPlayerParticipatingInNightStage(GameStage stage, PlayerState player)
    {
        if (player.IsDead() || player.IsHost()) return false;
        
        return stage switch
        {
            GameStage.BeautyTurn => player.Role == GameRole.Beauty,
            GameStage.DoctorTurn => player.Role == GameRole.Doctor,
            GameStage.CommissionerTurn => player.Role == GameRole.Commissioner,
            GameStage.MafiaTurn => player.Role is GameRole.Mafia or GameRole.Don,
            GameStage.KillerTurn => player.Role == GameRole.Killer,
            GameStage.NecromancerTurn => player.Role == GameRole.Necromancer,
            _ => false
        };
    }

    private static Guid? GetCurrentPlayerVote(LobbyState lobby, PlayerState player)
    {
        if (player.IsDead() || player.IsHost()) return null;

        var stage = lobby.Stage;
        return stage switch
        {
            GameStage.DayVoting or GameStage.DayVoting2 => lobby.DayVotes.TryGetValue(player.Id, out var dv) ? dv : null,
            GameStage.MafiaTurn when player.Role is GameRole.Mafia or GameRole.Don =>
                lobby.MafiaVotes.TryGetValue(player.Id, out var mv) ? mv : null,
            GameStage.KillerTurn when player.Role == GameRole.Killer => lobby.KillerVote,
            GameStage.CommissionerTurn when player.Role == GameRole.Commissioner => 
                lobby.CommissionerVote ?? lobby.CommissionerChecks.Keys.LastOrDefault(),
            GameStage.BeautyTurn when player.Role == GameRole.Beauty => lobby.BeautyVote,
            GameStage.DoctorTurn when player.Role == GameRole.Doctor => lobby.DoctorVote,
            GameStage.NecromancerTurn when player.Role == GameRole.Necromancer => lobby.NecromancerVote,
            _ => null
        };
    }

[HttpPost]
    public IActionResult StartGame(string code, Guid playerId)
    {
        if (!gameService.StartGame(code, playerId, out var error))
        {
            return BadRequest(new { error });
        }

        return Ok();
    }

    [HttpPost]
    public IActionResult DayVote(string code, Guid playerId, Guid targetId)
    {
        if (!gameService.DayVote(code, playerId, targetId, out var error))
        {
            return BadRequest(new { error });
        }

        return Ok();
    }

    [HttpPost]
    public IActionResult MafiaVote(string code, Guid playerId, Guid targetId)
    {
        if (!gameService.MafiaVote(code, playerId, targetId, out var error))
        {
            return BadRequest(new { error });
        }

        return Ok();
    }

    [HttpPost]
    public IActionResult CancelDayVote(string code, Guid playerId)
    {
        if (!gameService.CancelDayVote(code, playerId, out var error))
        {
            return BadRequest(new { error });
        }

        return Ok();
    }

    [HttpPost]
    public IActionResult CancelMafiaVote(string code, Guid playerId)
    {
        if (!gameService.CancelMafiaVote(code, playerId, out var error))
        {
            return BadRequest(new { error });
        }

        return Ok();
    }

    [HttpPost]
    public IActionResult ManiacVote(string code, Guid playerId, Guid targetId)
    {
        if (!gameService.ManiacVote(code, playerId, targetId, out var error))
        {
            return BadRequest(new { error });
        }

        return Ok();
    }

    [HttpPost]
    public IActionResult CancelManiacVote(string code, Guid playerId)
    {
        if (!gameService.CancelManiacVote(code, playerId, out var error))
        {
            return BadRequest(new { error });
        }

        return Ok();
    }

    [HttpPost]
    public IActionResult CommissionerCheck(string code, Guid playerId, Guid targetId)
    {
        if (!gameService.CommissionerCheck(code, playerId, targetId, out var error))
        {
            return BadRequest(new { error });
        }

        return Ok();
    }

    [HttpPost]
    public IActionResult CommissionerKill(string code, Guid playerId, Guid targetId)
    {
        if (!gameService.CommissionerKill(code, playerId, targetId, out var error))
        {
            return BadRequest(new { error });
        }

        return Ok();
    }

    [HttpPost]
    public IActionResult SetCommissionerIsKill(string code, Guid playerId, bool isKill)
    {
        if (!gameService.SetCommissionerIsKill(code, playerId, isKill, out var error))
        {
            return BadRequest(new { error });
        }

        return Ok();
    }

    [HttpPost]
    public IActionResult CommissionerVote(string code, Guid playerId, Guid targetId)
    {
        if (!gameService.CommissionerVote(code, playerId, targetId, out var error))
        {
            return BadRequest(new { error });
        }

        return Ok();
    }

    [HttpPost]
    public IActionResult CancelCommissionerVote(string code, Guid playerId)
    {
        if (!gameService.CancelCommissionerVote(code, playerId, out var error))
        {
            return BadRequest(new { error });
        }

        return Ok();
    }

    [HttpPost]
    public IActionResult BeautyAction(string code, Guid playerId, Guid targetId)
    {
        if (!gameService.BeautyAction(code, playerId, targetId, out var error))
        {
            return BadRequest(new { error });
        }

        return Ok();
    }

    [HttpPost]
    public IActionResult CancelBeautyVote(string code, Guid playerId)
    {
        if (!gameService.CancelBeautyVote(code, playerId, out var error))
        {
            return BadRequest(new { error });
        }

        return Ok();
    }

    [HttpPost]
    public IActionResult DoctorAction(string code, Guid playerId, Guid targetId)
    {
        if (!gameService.DoctorAction(code, playerId, targetId, out var error))
        {
            return BadRequest(new { error });
        }

        return Ok();
    }

    [HttpPost]
    public IActionResult CancelDoctorVote(string code, Guid playerId)
    {
        if (!gameService.CancelDoctorVote(code, playerId, out var error))
        {
            return BadRequest(new { error });
        }

        return Ok();
    }

    [HttpPost]
    public IActionResult NecromancerAction(string code, Guid playerId, Guid targetId)
    {
        if (!gameService.NecromancerAction(code, playerId, targetId, out var error))
        {
            return BadRequest(new { error });
        }

        return Ok();
    }

    [HttpPost]
    public IActionResult CancelNecromancerVote(string code, Guid playerId)
    {
        if (!gameService.CancelNecromancerVote(code, playerId, out var error))
        {
            return BadRequest(new { error });
        }

        return Ok();
    }

    [HttpPost]
    public IActionResult NextStage(string code, Guid playerId)
    {
        if (!gameService.NextStage(code, playerId, out var error))
        {
            return BadRequest(new { error });
        }

        return Ok();
    }

    [HttpPost]
    public IActionResult AddBot(string code, Guid playerId)
    {
        gameService.AddBot(code, playerId, out var error);

        return error==null ? Ok() : BadRequest(error);
    }
}
