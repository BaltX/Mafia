using Mafia.Models;

namespace Mafia.Services.Handlers;

public static class ActionFactory
{
    private static readonly DayAction DayAction = new();
    private static readonly MafiaAction MafiaAction = new();
    private static readonly KillerAction KillerAction = new();
    private static readonly BeautyAction BeautyAction = new();
    private static readonly DoctorAction DoctorAction = new();
    private static readonly CommissionerAction CommissionerAction = new();
    private static readonly NecromancerAction NecromancerAction = new();

    public static IAction? Get(GameStage stage, GameRole? role = null) => (stage, role) switch
    {
        (GameStage.DayVoting, _) or (GameStage.DayVoting2, _) => DayAction,
        (GameStage.MafiaTurn, GameRole.Mafia) or (GameStage.MafiaTurn, GameRole.Don) => MafiaAction,
        (GameStage.KillerTurn, GameRole.Killer) => KillerAction,
        (GameStage.BeautyTurn, GameRole.Beauty) => BeautyAction,
        (GameStage.DoctorTurn, GameRole.Doctor) => DoctorAction,
        (GameStage.CommissionerTurn, GameRole.Commissioner) => CommissionerAction,
        (GameStage.NecromancerTurn, GameRole.Necromancer) => NecromancerAction,
        _ => null
    };

    public static IAction? GetByStage(GameStage stage) => stage switch
    {
        GameStage.DayVoting or GameStage.DayVoting2 => DayAction,
        GameStage.MafiaTurn => MafiaAction,
        GameStage.KillerTurn => KillerAction,
        GameStage.BeautyTurn => BeautyAction,
        GameStage.DoctorTurn => DoctorAction,
        GameStage.CommissionerTurn => CommissionerAction,
        GameStage.NecromancerTurn => NecromancerAction,
        _ => null
    };

    public static CommissionerAction GetCommissioner() => CommissionerAction;
}