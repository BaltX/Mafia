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
}