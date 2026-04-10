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
│   ├── MafiaGameService.cs      # Главный сервис — координация игры
│   ├── MafiaGameLogicService.cs # Логика старта игры, распределение ролей
│   ├── MafiaVotingService.cs    # Все голосования
│   ├── MafiaNightService.cs     # Управление стадиями ночи
│   └── MafiaWinConditionService.cs # Определение победителя
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
- Создание/вход в лобби
- Запуск игры
- Делегирование голосований в MafiaVotingService
- Делегирование перехода стадий в MafiaNightService

### MafiaVotingService
- Валидация голосов
- Подсчёт результатов через `ResolveVote`, `ResolveMafiaVote`
- Бот-голосования через `ProcessBotVotes`

### MafiaNightService
- Переход между стадиями через `NextStage`
- Применение ночных действий
- Определение убитых/воскрешённых

### MafiaWinConditionService
- Статический класс
- `TrySetWinner(lobby)` — проверяет условия победы

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
2. Добавить голосование в `MafiaVotingService`
3. Добавить стадию в `GameStage` если нужен отдельный ход
4. Обновить `MafiaNightService.GetRolesForStage()`
5. Обновить `MafiaWinConditionService`

## Тестирование

```bash
dotnet build
dotnet run
```

Игра доступна на `http://localhost:5000`