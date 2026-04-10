namespace Mafia.Models;

public static class GameConstants
{
    public static class Errors
    {
        public const string EnterHostName = "Введите имя ведущего.";
        public const string InvalidHostName = "Недопустимое имя ведущего.";
        public const string EnterLobbyCodeAndName = "Введите код лобби и имя игрока.";
        public const string JoinFailed = "Не удалось присоединиться. Проверьте код или статус игры.";
        public const string LobbyOrPlayerNotFound = "Лобби или игрок не найдены.";
        public const string BotsOnlyInLobby = "Ботов можно добавлять только в лобби.";
        public const string LobbyNotFound = "Лобби не найдено.";
        public const string OnlyHostCanDoThis = "Только ведущий может выполнить это действие.";
        public const string PlayerNotFoundInLobby = "Игрок не найден в лобби.";
        public const string CannotAdvanceStage = "Сейчас нельзя перейти к следующей стадии.";
        public const string CannotVoteNow = "Сейчас нельзя голосовать.";
        public const string SecondDayVoteOnlyForLeaders = "Во второй день можно голосовать только за лидеров первого дня.";
        public const string CannotCancelNow = "Сейчас нельзя отменить.";
        public const string MafiaTurnNotAvailable = "Сейчас ход мафии недоступен.";
        public const string ManiacTurnNotAvailable = "Сейчас ход маньяка недоступен.";
        public const string CommissionerTurnNotAvailable = "Сейчас ход комиссара недоступен.";
        public const string BeautyTurnNotAvailable = "Сейчас ход красотки недоступен.";
        public const string DoctorTurnNotAvailable = "Сейчас ход доктора недоступен.";
        public const string NecromancerTurnNotAvailable = "Сейчас ход некроманта недоступен.";
        public const string CannotVoteSelf = "Нельзя выбрать себя.";
        public const string CannotVoteThisPlayer = "Нельзя выбрать этого игрока.";
        public const string CannotVoteMafiaToMafia = "Мафия может голосовать только против живых не-мафиози.";
        public const string CannotSelectSamePlayerTwice = "Нельзя выбирать одного игрока 2 ночи подряд.";
        public const string MinPlayersRequired = "Для старта игры нужно минимум 3 игрока, не считая ведущего.";
        public const string InvalidTarget = "Неверная цель.";
        public const string CannotVote = "Нельзя голосовать.";
    }

    public static class WinMessages
    {
        public const string MafiaWins = "Победа мафии!";
        public const string ManiacWins = "Победа маньяков!";
        public const string CiviliansWins = "Победа мирных жителей!";
        public const string Draw = "Ничья";
    }

    public static class StageText
    {
        public const string Lobby = "Ожидание";
        public const string Discussion = "Обсуждение";
        public const string DayVoting = "Дневное голосование";
        public const string DiscussionBeforeSecondVote = "Обсуждение перед вторым голосованием";
        public const string DayVoting2 = "Второе дневное голосование";
        public const string NightStart = "Начало ночи";
        public const string BeautyTurn = "Ход красотки";
        public const string DoctorTurn = "Ход доктора";
        public const string CommissionerTurn = "Ход комиссара";
        public const string MafiaTurn = "Ход мафии";
        public const string KillerTurn = "Ход маньяка";
        public const string NecromancerTurn = "Ход некроманта";
        public const string NightResult = "Результат ночи";
        public const string GameOver = "Завершена";
    }

    public static class ActionMessages
    {
        public const string NightActions = "Ночные действия";
        public const string NoOneKilled = "🌙 Никто не пострадал.";
    }

    public static class GameSettings
    {
        public const int DefaultDiscussionSeconds = 300;
        public const int DefaultDayVoteSeconds = 60;
        public const int DefaultNightVoteSeconds = 30;
        public const int MinPlayersToStart = 3;
        public const int LobbyCodeLength = 6;
    }
}

public static class GameStageExtensions
{
    public static string GetDisplayText(this GameStage stage) => stage switch
    {
        GameStage.Lobby => GameConstants.StageText.Lobby,
        GameStage.Discussion => GameConstants.StageText.Discussion,
        GameStage.DayVoting => GameConstants.StageText.DayVoting,
        GameStage.DiscussionBeforeSecondVote => GameConstants.StageText.DiscussionBeforeSecondVote,
        GameStage.DayVoting2 => GameConstants.StageText.DayVoting2,
        GameStage.NightStart => GameConstants.StageText.NightStart,
        GameStage.BeautyTurn => GameConstants.StageText.BeautyTurn,
        GameStage.DoctorTurn => GameConstants.StageText.DoctorTurn,
        GameStage.CommissionerTurn => GameConstants.StageText.CommissionerTurn,
        GameStage.MafiaTurn => GameConstants.StageText.MafiaTurn,
        GameStage.KillerTurn => GameConstants.StageText.KillerTurn,
        GameStage.NecromancerTurn => GameConstants.StageText.NecromancerTurn,
        GameStage.NightResult => GameConstants.StageText.NightResult,
        GameStage.GameOver => GameConstants.StageText.GameOver,
        _ => stage.ToString()
    };

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
        _ => GameConstants.GameSettings.DefaultNightVoteSeconds
    };
}