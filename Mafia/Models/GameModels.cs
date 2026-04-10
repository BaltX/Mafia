namespace Mafia.Models;

/// <summary>
/// Роли игроков в игре.
/// </summary>
public enum GameRole
{
    Unassigned,
    Host,
    Civilian,
    Mafia,
    Don,
    Commissioner,
    Killer,
    Beauty,
    Doctor,
    Necromancer
}

/// <summary>
/// Стадии игрового раунда.
/// </summary>
public enum GameStage
{
    Lobby,
    Discussion,
    DayVoting,
    DiscussionBeforeSecondVote,
    DayVoting2,
    NightStart,
    BeautyTurn,
    DoctorTurn,
    CommissionerTurn,
    MafiaTurn,
    KillerTurn,
    NecromancerTurn,
    NightResult,
    GameOver
}

/// <summary>
/// Состояние игрока в лобби.
/// </summary>
public class PlayerState
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public GameRole Role { get; set; } = GameRole.Unassigned;
    public bool IsAlive { get; set; } = true;
    public bool IsBot { get; set; } = false;
    public bool IsZombie { get; set; } = false;
    public bool? IsMafiaChecked { get; set; }
}

/// <summary>
/// Состояние лобби (игровой комнаты).
/// </summary>
public class LobbyState
{
    public string Code { get; init; } = string.Empty;
    public int Round { get; set; } = 0;
    public GameStage Stage { get; set; } = GameStage.Lobby;
    public int DiscussionSeconds { get; set; } = 300;
    public int DayVoteSeconds { get; set; } = 60;
    public int NightVoteSeconds { get; set; } = 30;
    public DateTimeOffset? StageEndsAtUtc { get; set; }
    public List<PlayerState> Players { get; init; } = [];
    
    public PlayerState? Commissioner => Players.FirstOrDefault(p => p.IsAlive && p.Role == GameRole.Commissioner);
    public PlayerState? Killer => Players.FirstOrDefault(p => p.IsAlive && p.Role == GameRole.Killer);
    public PlayerState? Beauty => Players.FirstOrDefault(p => p.IsAlive && p.Role == GameRole.Beauty);
    public PlayerState? Doctor => Players.FirstOrDefault(p => p.IsAlive && p.Role == GameRole.Doctor);
    public PlayerState? Necromancer =>  Players.FirstOrDefault(p => p.IsAlive && p.Role == GameRole.Necromancer);
    
    public Dictionary<Guid, Guid> DayVotes { get; init; } = [];
    public Guid? CommissionerVote { get; set; }
    public bool CommissionerIsKill { get; set; }
    public Guid? KillerVote { get; set; }
    public Dictionary<Guid, Guid> MafiaVotes { get; init; } = [];
    public Guid? MafiaVictimId { get; set; }
    public Guid? BeautyVote { get; set; }
    public Guid? LastNightBeautyVote { get; set; }
    public Guid? DoctorVote { get; set; }
    public Guid? LastNightDoctorVote { get; set; }
    public Guid? NecromancerVote { get; set; }
    public List<Guid?> LastKilledPlayers { get; init; } = [];
    public List<Guid?> LastResurrectedPlayers { get; init; } = [];
    public List<Guid> Day1TopVotedPlayerIds { get; set; } = [];

    public List<string> LastNightResultTexts { get; init; } = [];
    public Guid? LastDayVictimId { get; set; }
    public Dictionary<Guid, bool> CommissionerChecks { get; init; } = [];
    public bool? PendingCommissionerCheckResult { get; set; }
    public string? WinnerText { get; set; }
    public List<StageResultEntry> StageHistory { get; init; } = [];
}

/// <summary>
/// Запись в истории стадий игры.
/// </summary>
public class StageResultEntry
{
    public int Round { get; init; }
    public required string StageText { get; init; }
    public required List<string> ResultText { get; init; }
}

/// <summary>
/// ViewModel для страницы игры.
/// </summary>
public class GamePageViewModel
{
    public required LobbyState Lobby { get; init; }
    public required PlayerState CurrentPlayer { get; init; }
    public bool IsHost => CurrentPlayer.Role == GameRole.Host;
    public bool IsMafia => CurrentPlayer.Role == GameRole.Mafia;
    public bool IsDon => CurrentPlayer.Role == GameRole.Don;
    public bool IsBot => CurrentPlayer.IsBot;
    public bool IsBeauty => CurrentPlayer.Role == GameRole.Beauty;
    public bool IsDoctor => CurrentPlayer.Role == GameRole.Doctor;
    public bool IsNecromancer => CurrentPlayer.Role == GameRole.Necromancer;
    public bool IsZombie => CurrentPlayer.IsZombie;
}

/// <summary>
/// ViewModel для элемента списка лобби.
/// </summary>
public class LobbyListItemViewModel
{
    public required string Code { get; init; }
    public required string StageText { get; init; }
    public int TotalPlayers { get; init; }
}

/// <summary>
/// ViewModel для домашней страницы игры.
/// </summary>
public class GameHomeViewModel
{
    public List<LobbyListItemViewModel> Lobbies { get; init; } = [];
}



/// <summary>
/// Расширения для PlayerState.
/// </summary>
public static class PlayerStateExtensions
{
    /// <summary>Проверяет, мёртв ли игрок.</summary>
    public static bool IsDead(this PlayerState p) => !p.IsAlive;
    /// <summary>Проверяет, является ли игрок ведущим.</summary>
    public static bool IsHost(this PlayerState p) => p.Role == GameRole.Host;
    /// <summary>Проверяет, является ли игрок маньяком.</summary>
    public static bool IsKiller(this PlayerState p) => p.Role == GameRole.Killer;
    /// <summary>Проверяет, является ли игрок мафией (дон или мафиози).</summary>
    public static bool IsMafia(this PlayerState p) => p.Role == GameRole.Mafia || p.Role == GameRole.Don;
    /// <summary>Проверяет, является ли игрок красоткой.</summary>
    public static bool IsBeauty(this PlayerState p) => p.Role == GameRole.Beauty;
    /// <summary>Проверяет, является ли игрок доктором.</summary>
    public static bool IsDoctor(this PlayerState p) => p.Role == GameRole.Doctor;
    /// <summary>Проверяет, является ли игрок комиссаром.</summary>
    public static bool IsCommissioner(this PlayerState p) => p.Role == GameRole.Commissioner;
    /// <summary>Проверяет, является ли игрок некромантом.</summary>
    public static bool IsNecromancer(this PlayerState p) => p.Role == GameRole.Necromancer;
}

/// <summary>
/// Расширения для LobbyState.
/// </summary>
public static class LobbyStateExtensions
{
    /// <summary>Получить игрока по ID.</summary>
    public static PlayerState? GetPlayer(this LobbyState lobby, Guid id) => 
        lobby.Players.FirstOrDefault(p => p.Id == id);
    
    /// <summary>Получить живого игрока с указанной ролью.</summary>
    public static PlayerState? GetAlivePlayer(this LobbyState lobby, GameRole role) => 
        lobby.Players.FirstOrDefault(p => p.IsAlive && p.Role == role);
}
