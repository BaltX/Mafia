using Mafia.Models;

namespace Mafia.Services.Handlers;

public static class VotingHandlerFactory
{
    private static readonly DayVotingHandler DayHandler = new();
    private static readonly MafiaVotingHandler MafiaHandler = new();
    private static readonly KillerVotingHandler KillerHandler = new();
    private static readonly BeautyVotingHandler BeautyHandler = new();
    private static readonly DoctorVotingHandler DoctorHandler = new();
    private static readonly CommissionerVotingHandler CommissionerHandler = new();
    private static readonly NecromancerVotingHandler NecromancerHandler = new();

    public static IVotingHandler? GetHandler(GameStage stage, GameRole role)
    {
        return (stage, role) switch
        {
            (GameStage.DayVoting, _) or (GameStage.DayVoting2, _) => DayHandler,
            (GameStage.MafiaTurn, GameRole.Mafia) => MafiaHandler,
            (GameStage.MafiaTurn, GameRole.Don) => MafiaHandler,
            (GameStage.KillerTurn, GameRole.Killer) => KillerHandler,
            (GameStage.BeautyTurn, GameRole.Beauty) => BeautyHandler,
            (GameStage.DoctorTurn, GameRole.Doctor) => DoctorHandler,
            (GameStage.CommissionerTurn, GameRole.Commissioner) => CommissionerHandler,
            (GameStage.NecromancerTurn, GameRole.Necromancer) => NecromancerHandler,
            _ => null
        };
    }

    public static IVotingHandler? GetHandlerByStage(GameStage stage)
    {
        return stage switch
        {
            GameStage.DayVoting or GameStage.DayVoting2 => DayHandler,
            GameStage.MafiaTurn => MafiaHandler,
            GameStage.KillerTurn => KillerHandler,
            GameStage.BeautyTurn => BeautyHandler,
            GameStage.DoctorTurn => DoctorHandler,
            GameStage.CommissionerTurn => CommissionerHandler,
            GameStage.NecromancerTurn => NecromancerHandler,
            _ => null
        };
    }

    public static CommissionerVotingHandler GetCommissionerHandler() => CommissionerHandler;
}