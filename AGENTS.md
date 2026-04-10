# Руководство для ИИ-ассистентов

## Архитектура проекта

Проект "Мафия" — веб-приложение на ASP.NET Core с игровой логикой в реальном времени.

### Структура

```
Mafia/
├── Models/
│   ├── GameModels.cs       # Основные модели (PlayerState, LobbyState, GameRole, GameStage)
│   ├── GameConstants.cs   # Константы, ошибки, тексты
│   └── ErrorViewModel.cs
├── Services/
│   ├── MafiaGameService.cs           # Фасад — входные точки API
│   ├── MafiaGameLogicService.cs      # Логика старта игры, распределение ролей
│   ├── MafiaNightService.cs          # Управление стадиями игры
│   ├── MafiaWinConditionService.cs   # Определение победителя
│   ├── LobbyService.cs               # Управление лобби
│   ├── VotingCoordinator.cs           # Координация голосований
│   ├── BotActionService.cs           # Логика голосований ботов
│   ├── GameResultService.cs          # Подсчёт результатов голосований
│   ├── NightActionService.cs          # Ночные действия (убийства/воскрешения)
│   └── Handlers/
│       ├── IAction.cs               # Интерфейс действий игроков
│       ├── ActionBase.cs            # Базовый класс для действий
│       ├── ActionFactory.cs         # Фабрика действий (НОВЫЙ)
│       └── Actions/
│           ├── DayAction.cs         # Дневное голосование
│           ├── MafiaAction.cs      # Голосование мафии
│           ├── KillerAction.cs     # Голосование маньяка
│           ├── BeautyAction.cs     # Действие красотки
│           ├── DoctorAction.cs     # Действие доктора
│           ├── CommissionerAction.cs # Действия комиссара (check/kill)
│           └── NecromancerAction.cs  # Действие некроманта
├── Controllers/
│   └── GameController.cs
└── Views/
```

## Модели данных

### GameRole (роли)
- `Host` — ведущий
- `Civilian` — мирный
- `Mafia` — мафиози
- `Don` — дон
- `Commissioner` — комиссар
- `Killer` — маньяк
- `Beauty` — красотка
- `Doctor` — доктор
- `Necromancer` — некромант

### GameStage (стадии)
```
Lobby → Discussion → DayVoting → DiscussionBeforeSecondVote → DayVoting2 → NightStart
→ BeautyTurn → DoctorTurn → CommissionerTurn → MafiaTurn → KillerTurn → NecromancerTurn → NightResult
→ Discussion (следующий раунд)
```

### LobbyState
- `Players` — список игроков
- `Stage` — текущая стадия
- `Round` — номер раунда
- `DayVotes`, `MafiaVotes`, `KillerVote`, `CommissionerVote`, `BeautyVote`, `DoctorVote`, `NecromancerVote` — голосования
- `StageEndsAtUtc` — когда истекает текущая стадия

### PlayerState
- `Id`, `Name`, `Role`, `IsAlive`, `IsBot`, `IsZombie`, `IsMafiaChecked`

## Services

### MafiaGameService
Фасад — точка входа для всех операций:
- Лобби: `CreateLobby`, `JoinLobby`, `AddBot`, `GetState`, `GetLobbies`
- Игра: `StartGame`, `NextStage`
- Голосования: `DayVote`, `MafiaVote`, `ManiacVote`, `CommissionerCheck`, `CommissionerKill`, `BeautyAction`, `DoctorAction`, `NecromancerAction`
- Отмена: `CancelDayVote`, `CancelMafiaVote`, etc.

### IAction / ActionFactory (НОВЫЙ паттерн)
Каждый тип действия — отдельный класс с своей логикой:

```csharp
// Получение обработчика
var action = ActionFactory.Get(stage, role);

// Интерфейс IAction
bool CanExecute(LobbyState lobby, PlayerState player)
string? Validate(LobbyState lobby, PlayerState player, Guid targetId)
void Execute(LobbyState lobby, PlayerState player, Guid targetId)
void Cancel(LobbyState lobby, PlayerState player)
```

**Список действий:**
- `DayAction` — дневное голосование
- `MafiaAction` — голосование мафии
- `KillerAction` — голосование маньяка
- `BeautyAction` — действие красотки
- `DoctorAction` — действие доктора
- `CommissionerAction` — имеет дополнительные методы: `CommissionerCheck`, `CommissionerKill`, `SetCommissionerIsKill`
- `NecromancerAction` — действие некроманта

### VotingCoordinator
Координация голосований — использует ActionFactory:
- `GetState(code, playerId)` — получить состояние с автопереходом стадий
- `DayVote`, `MafiaVote`, etc. — методы голосования
- `CommissionerCheck`, `CommissionerKill`, `SetCommissionerIsKill` — специальные методы комиссара

### BotActionService
Логика голосований ботов:
- `ProcessBotVotes(lobby, stage)` — обработать голоса ботов для стадии

### GameResultService
Подсчёт результатов голосований:
- `ResolveDayVote(lobby)` — результат дневного голосования
- `ResolveMafiaVote(lobby)` — результат голосования мафии

### NightActionService
Ночные действия:
- `ApplyNightActions(lobby)` — применить ночные действия
- `KillPlayer(lobby, id)` — убить игрока
- `ZombiePlayer(lobby, id)` — воскресить игрока как зомби

### MafiaNightService
Управление стадиями игры:
- `NextStage(lobby)` — переход к следующей стадии
- `GetNextStage(stage)` — следующая стадия
- `GetRolesForStage(stage)` — роли для стадии
- `GetPlayersForStage(lobby, stage)` — игроки для стадии

### MafiaWinConditionService
Определение победителя:
- `TrySetWinner(lobby)` — проверить и установить победителя
- `IsMafiaWin(lobby)`, `IsManiacWin(lobby)`, `IsCivilianWin(lobby)`

## Паттерны

### Result pattern
Методы сервисов возвращают `bool success` + `out string? error`

### Extension methods
- `PlayerStateExtensions`: `IsDead()`, `IsHost()`, `IsMafia()`, `IsKiller()`, etc.
- `LobbyStateExtensions`: `GetPlayer()`, `GetAlivePlayer()`

## Важные константы в GameConstants

- `GameConstants.Errors.*` — тексты ошибок
- `GameConstants.WinMessages.*` — тексты победы
- `GameConstants.StageText.*` — названия стадий

## Как добавить новую роль

1. Добавить в `GameRole` enum
2. Создать новый класс в `Handlers/Actions/` (наследник ActionBase)
3. Добавить стадию в `GameStage` если нужен отдельный ход
4. Добавить действие в `ActionFactory.Get()`
5. Добавить в `MafiaNightService.GetRolesForStage()`
6. Обновить `MafiaWinConditionService`
7. Добавить обработку ботов в `BotActionService`
8. Добавить ночное действие в `NightActionService` если нужно

## Тестирование

```bash
dotnet build
dotnet run
```

Игра доступна на `http://localhost:5000`