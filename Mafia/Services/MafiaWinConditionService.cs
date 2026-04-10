using Mafia.Models;

namespace Mafia.Services;

/// <summary>
/// Сервис для определения победителя в игре.
/// </summary>
public static class MafiaWinConditionService
{
    /// <summary>
    /// Проверяет, есть ли победитель, и обновляет состояние лобби.
    /// </summary>
    /// <returns>True если определён победитель.</returns>
    public static bool TrySetWinner(LobbyState lobby)
    {
        var mafiaAlive = lobby.Players.Count(p => p.IsAlive && (p.Role == GameRole.Mafia || p.Role == GameRole.Don));
        var maniacAlive = lobby.Players.Count(p => p.IsAlive && (p.Role == GameRole.Killer || p.Role == GameRole.Necromancer));
        var civilianAlive = lobby.Players.Count(p => p.IsAlive && p.Role != GameRole.Mafia && p.Role != GameRole.Don && p.Role != GameRole.Killer && p.Role != GameRole.Necromancer && p.Role != GameRole.Host);

        if (mafiaAlive > 0 && mafiaAlive >= civilianAlive + maniacAlive)
        {
            lobby.Stage = GameStage.GameOver;
            lobby.WinnerText = "Победа мафии!";
            return true;
        }

        if (maniacAlive > 0 && civilianAlive == 0 && mafiaAlive == 0)
        {
            lobby.Stage = GameStage.GameOver;
            lobby.WinnerText = "Победа маньяков!";
            return true;
        }

        if (civilianAlive > 0 && mafiaAlive == 0 && maniacAlive == 0)
        {
            lobby.Stage = GameStage.GameOver;
            lobby.WinnerText = "Победа мирных жителей!";
            return true;
        }

        if (civilianAlive == 0 && mafiaAlive == 0 && maniacAlive == 0)
        {
            lobby.Stage = GameStage.GameOver;
            lobby.WinnerText = "Ничья";
            return true;
        }

        return false;
    }

    /// <summary>Количество живых игроков мафии.</summary>
    public static int GetMafiaAliveCount(LobbyState lobby) =>
        lobby.Players.Count(p => p.IsAlive && (p.Role == GameRole.Mafia || p.Role == GameRole.Don));

    /// <summary>Количество живых маньяков.</summary>
    public static int GetManiacAliveCount(LobbyState lobby) =>
        lobby.Players.Count(p => p.IsAlive && (p.Role == GameRole.Killer || p.Role == GameRole.Necromancer));

    /// <summary>Количество живых мирных.</summary>
    public static int GetCivilianAliveCount(LobbyState lobby) =>
        lobby.Players.Count(p => p.IsAlive && p.Role != GameRole.Mafia && p.Role != GameRole.Don && p.Role != GameRole.Killer && p.Role != GameRole.Necromancer && p.Role != GameRole.Host);

    /// <summary>Проверяет, выиграла ли мафия.</summary>
    public static bool IsMafiaWin(LobbyState lobby) =>
        lobby.Stage == GameStage.GameOver && lobby.WinnerText == "Победа мафии!";

    /// <summary>Проверяет, выиграли ли маньяки.</summary>
    public static bool IsManiacWin(LobbyState lobby) =>
        lobby.Stage == GameStage.GameOver && lobby.WinnerText == "Победа маньяков!";

    /// <summary>Проверяет, выиграли ли мирные.</summary>
    public static bool IsCivilianWin(LobbyState lobby) =>
        lobby.Stage == GameStage.GameOver && lobby.WinnerText == "Победа мирных жителей!";

    /// <summary>Проверяет, закончилась ли игра вничью.</summary>
    public static bool IsDraw(LobbyState lobby) =>
        lobby.Stage == GameStage.GameOver && lobby.WinnerText == "Ничья";

    /// <summary>Проверяет, завершена ли игра.</summary>
    public static bool IsGameOver(LobbyState lobby) =>
        lobby.Stage == GameStage.GameOver;
}