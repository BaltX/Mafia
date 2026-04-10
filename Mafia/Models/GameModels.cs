namespace Mafia.Models;

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

public class LobbyState
{
    public string Code { get; init; } = string.Empty;
    public int Round { get; set; } = 0;
    public GameStage Stage { get; set; } = GameStage.Lobby;
    public int DiscussionSeconds { get; set; } = 90;
    public int DayVoteSeconds { get; set; } = 60;
    public int NightVoteSeconds { get; set; } = 300;
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

public class StageResultEntry
{
    public int Round { get; init; }
    public required string StageText { get; init; }
    public required List<string> ResultText { get; init; }
}

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

public class LobbyListItemViewModel
{
    public required string Code { get; init; }
    public required string StageText { get; init; }
    public int TotalPlayers { get; init; }
}

public class GameHomeViewModel
{
    public List<LobbyListItemViewModel> Lobbies { get; init; } = [];
}

public static class GameStageExtensions
{
    public static int GetSeconds(this GameStage stage, LobbyState lobby) => stage switch
    {
        GameStage.Discussion => lobby.DiscussionSeconds,
        GameStage.DayVoting => lobby.DayVoteSeconds,
        GameStage.DiscussionBeforeSecondVote => lobby.DiscussionSeconds,
        GameStage.DayVoting2 => lobby.DayVoteSeconds,
        GameStage.BeautyTurn => lobby.NightVoteSeconds,
        GameStage.DoctorTurn => lobby.NightVoteSeconds,
        GameStage.CommissionerTurn => lobby.NightVoteSeconds,
        GameStage.MafiaTurn => lobby.NightVoteSeconds,
        GameStage.KillerTurn => lobby.NightVoteSeconds,
        GameStage.NecromancerTurn => lobby.NightVoteSeconds,
        _ => 30
    };
}

public static class PlayerStateExtensions
{
    public static bool IsDead(this PlayerState p) => !p.IsAlive;
    public static bool IsHost(this PlayerState p) => p.Role == GameRole.Host;
    public static bool IsKiller(this PlayerState p) => p.Role == GameRole.Killer;
    public static bool IsMafia(this PlayerState p) => p.Role == GameRole.Mafia || p.Role == GameRole.Don;
    public static bool IsBeauty(this PlayerState p) => p.Role == GameRole.Beauty;
    public static bool IsDoctor(this PlayerState p) => p.Role == GameRole.Doctor;
    public static bool IsCommissioner(this PlayerState p) => p.Role == GameRole.Commissioner;
    public static bool IsNecromancer(this PlayerState p) => p.Role == GameRole.Necromancer;
}

public static class LobbyStateExtensions
{
    public static PlayerState? GetPlayer(this LobbyState lobby, Guid id) => 
        lobby.Players.FirstOrDefault(p => p.Id == id);
    
    public static PlayerState? GetAlivePlayer(this LobbyState lobby, GameRole role) => 
        lobby.Players.FirstOrDefault(p => p.IsAlive && p.Role == role);
}
