using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Supermoce.src.player;
using System.Collections.Concurrent;

namespace Supermoce
{
    public class CollectiveBomb : ISkill
    {
        private const Skills skillName = Skills.CollectiveBomb;
        private static readonly ConcurrentDictionary<ulong, byte> bombCarriers = [];
        private static int activeSkillCount = 0;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName,
            "Dostawa Bomb",
            "Wszyscy terroryści mogą podłożyć C4. Po podłożeniu reszta bomb znika.",
            "#ffca1c",
            teamnum: 1,
            objective: 1);
        }

        public static void NewRound()
        {
            activeSkillCount = 0;
            bombCarriers.Clear();
            Server.NextFrame(() =>
            {
                foreach (var player in Utilities.GetPlayers()
                    .Where(p => p.IsValid && !p.IsBot && p.Team == CsTeam.Terrorist && p.PawnIsAlive))
                {
                    var pawn = player.PlayerPawn?.Value;
                    if (pawn?.WeaponServices == null) continue;

                    bool hasC4 = pawn.WeaponServices.MyWeapons
                        .Any(w => w?.Value?.DesignerName == "weapon_c4");

                    if (!hasC4)
                    {
                        player.GiveNamedItem("weapon_c4");
                        bombCarriers.TryAdd(player.SteamID, 0);
                    }
                }
            });
        }

        public static void BombPlanted(EventBombPlanted @event)
        {
            Server.NextFrame(() =>
            {
                foreach (var player in Utilities.GetPlayers()
                    .Where(p => p.IsValid && !p.IsBot && p.Team == CsTeam.Terrorist))
                {
                    player.RemoveItemByDesignerName("weapon_c4");
                }
                bombCarriers.Clear();
            });
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            var victim = @event.Userid;
            if (victim == null || !victim.IsValid) return;
            victim.RemoveItemByDesignerName("weapon_c4");
            bombCarriers.TryRemove(victim.SteamID, out _);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            activeSkillCount += 1;
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            activeSkillCount -= 1;
            if (activeSkillCount <= 0)
            {
                foreach (var steamId in bombCarriers.Keys)
                {
                    var carrier = Utilities.GetPlayers()
                        .FirstOrDefault(p => p.IsValid && p.SteamID == steamId);
                    if (carrier != null)
                        carrier.RemoveItemByDesignerName("weapon_c4");
                }
                bombCarriers.Clear();
            }
        }
    }
}
