using Mafia.Models;

namespace Mafia.Services;

public static class NightActionService
{
    public static void ApplyNightActions(LobbyState lobby)
    {
        lobby.LastNightResultTexts.Clear();
        lobby.LastKilledPlayers.Clear();
        
        if (lobby.BeautyVote.HasValue)
        {
            var victim = lobby.Players.FirstOrDefault(p => p.Id == lobby.BeautyVote.Value);
            if (victim is not null)
                lobby.LastNightResultTexts.Add($"💅 Красотка выбрала: {victim.Name}");
        }

        if (lobby.DoctorVote.HasValue)
        {
            var doctorTarget = lobby.Players.FirstOrDefault(p => p.Id == lobby.DoctorVote.Value);
            if (doctorTarget is not null)
                lobby.LastNightResultTexts.Add($"⚕️ Доктор выбрал: {doctorTarget.Name}");
        }

        if (lobby.CommissionerVote.HasValue)
        {
            var commissionerTarget = lobby.Players.FirstOrDefault(p => p.Id == lobby.CommissionerVote.Value);
            if (commissionerTarget is not null)
            {
                if (lobby.CommissionerIsKill == true)
                {
                    lobby.LastNightResultTexts.Add($"🔪 Комиссар выбрал: {commissionerTarget.Name}");
                }
                else
                {
                    var isMafia = commissionerTarget.IsMafia();
                    lobby.CommissionerChecks[commissionerTarget.Id] = isMafia;
                    commissionerTarget.IsMafiaChecked = isMafia;
                    var result = isMafia ? "Мафия" : "Мирный";
                    lobby.LastNightResultTexts.Add($"🔍 Комиссар проверил: {commissionerTarget.Name} - {result}");
                }
            }
        }

        var mafiaVoters = lobby.MafiaVotes.Count;
        if (mafiaVoters > 0)
        {
            var mafiaTarget = lobby.MafiaVictimId.HasValue 
                ? lobby.Players.FirstOrDefault(p => p.Id == lobby.MafiaVictimId.Value)?.Name 
                : "не выбрали";
            lobby.LastNightResultTexts.Add($"🎭 Мафия (@{mafiaVoters} чел.): {mafiaTarget}");
        }

        if (lobby.KillerVote.HasValue)
        {
            var killerTarget = lobby.Players.FirstOrDefault(p => p.Id == lobby.KillerVote.Value);
            if (killerTarget is not null)
                lobby.LastNightResultTexts.Add($"🔪 Убийца выбрал: {killerTarget.Name}");
        }

        if (lobby.NecromancerVote.HasValue)
        {
            var necromancerTarget = lobby.Players.FirstOrDefault(p => p.Id == lobby.NecromancerVote.Value);
            if (necromancerTarget is not null)
                lobby.LastNightResultTexts.Add($"🧟 Некромант выбрал: {necromancerTarget.Name}");
        }

        var beautyTargetId = lobby.BeautyVote;
        var doctorTargetId = beautyTargetId == lobby.Doctor?.Id ? null : lobby.DoctorVote;
        var commissionerVictimId =
            lobby.CommissionerIsKill != true ||
            beautyTargetId == lobby.Commissioner?.Id ||
            beautyTargetId == lobby.CommissionerVote ||
            doctorTargetId == lobby.CommissionerVote
                ? null
                : lobby.CommissionerVote;

        if (commissionerVictimId.HasValue)
        {
            KillPlayer(lobby, commissionerVictimId.Value);
        }
        
        var mafiaAlive = lobby.Players.Count(p =>
            p.IsAlive && (p.Role == GameRole.Mafia || p.Role == GameRole.Don) &&
            p.Id != beautyTargetId);
        
        var mafiaVictimId =
            mafiaAlive == 0 ||
            beautyTargetId == lobby.MafiaVictimId ||
            doctorTargetId == lobby.MafiaVictimId
                ? null
                : lobby.MafiaVictimId;
        
        if (mafiaVictimId.HasValue)
        {
            KillPlayer(lobby, mafiaVictimId.Value);
        }
        
        var killerVictimId  =
            !(lobby.Killer?.IsAlive ?? false) ||
            beautyTargetId == lobby.Killer?.Id ||
            beautyTargetId == lobby.KillerVote ||
            doctorTargetId == lobby.KillerVote
                ? null
                : lobby.KillerVote;

        if (killerVictimId.HasValue)
        {
            KillPlayer(lobby, killerVictimId.Value);
        }

        var necromancerTargetId =
            !(lobby.Necromancer?.IsAlive ?? false) ||
            beautyTargetId == lobby.Necromancer?.Id
                ? null
                : lobby.NecromancerVote;
        
        if (necromancerTargetId.HasValue)
        {
            var zombieCandidate = lobby.LastKilledPlayers.FirstOrDefault(p => p == necromancerTargetId.Value);
            if (zombieCandidate is not null)
            {
                MakeZombie(lobby, zombieCandidate.Value);
            }
        }

        if (lobby.LastKilledPlayers.Count == 0 && lobby.LastResurrectedPlayers.Count == 0)
        {
            lobby.LastNightResultTexts.Add("🌙 Никто не пострадал.");
        }
        else
        {
            if (lobby.LastKilledPlayers.Count > 0)
            {
                var killedNames = lobby.LastKilledPlayers
                    .Select(id => lobby.Players.FirstOrDefault(p => p.Id == id)?.Name)
                    .Where(n => n is not null)
                    .ToList();
                if (killedNames.Count > 0)
                {
                    lobby.LastNightResultTexts.Add($"💀 Убит{(killedNames.Count == 1 ? "" : "ы")}: {string.Join(", ", killedNames)}");
                }
            }
            
            if (lobby.LastResurrectedPlayers.Count > 0)
            {
                var resurrectedNames = lobby.LastResurrectedPlayers
                    .Select(id => lobby.Players.FirstOrDefault(p => p.Id == id)?.Name)
                    .Where(n => n is not null)
                    .ToList();
                if (resurrectedNames.Count > 0)
                {
                    lobby.LastNightResultTexts.Add($"✨ Воскрес{(resurrectedNames.Count == 1 ? "" : "и")}: {string.Join(", ", resurrectedNames)}");
                }
            }
        }
        
        lobby.StageHistory.Add(new StageResultEntry
        {
            Round = lobby.Round,
            StageText = "Ночные действия",
            ResultText = lobby.LastNightResultTexts.ToList()
        });
    }

    public static void MakeZombie(LobbyState lobby, Guid id)
    {
        var player = lobby.Players.FirstOrDefault(p => p.Id == id);
        if (player is not null && player is { IsAlive: false, IsZombie: false })
        {
            player.IsAlive = true;
            player.IsZombie = true;
            lobby.LastKilledPlayers.Remove(player.Id);
            lobby.LastResurrectedPlayers.Add(player.Id);
        }
    }

    public static void KillPlayer(LobbyState lobby, Guid id)
    {
        var player = lobby.Players.FirstOrDefault(p => p.Id == id);
        if (player is not null && player.IsAlive)
        {
            player.IsAlive = false;
            lobby.LastKilledPlayers.Add(player.Id);
            if (player.Role == GameRole.Necromancer)
            {
                foreach (var zombie in lobby.Players.Where(x=>x.IsAlive && x.IsZombie))
                {
                    zombie.IsAlive = false;
                    lobby.LastKilledPlayers.Add(zombie.Id);
                }
            }
        }
    }
}