using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Glaz : ISkill
    {
        private const Skills skillName = Skills.Glaz;
        private readonly static ConcurrentDictionary<int, byte> smokes = [];
        private static readonly object setLock = new();

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Oczy kobry", "Nie widzisz granatów dymnych", "#5d00ff");
        }

        public static void NewRound()
        {
            lock (setLock)
                smokes.Clear();
        }

        public static void SmokegrenadeDetonate(EventSmokegrenadeDetonate @event)
        {
            smokes.TryAdd(@event.Entityid, 0);
        }

        public static void SmokegrenadeExpired(EventSmokegrenadeExpired @event)
        {
            smokes.TryRemove(@event.Entityid, out _);
        }

        public static void CheckTransmit([CastFrom(typeof(nint))] CCheckTransmitInfoList infoList)
        {
            foreach (var (info, player) in infoList)
            {
                if (player == null) continue;
                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);

                var observedPlayer = Utilities.GetPlayers().FirstOrDefault(p => p?.Pawn?.Value?.Handle == player?.Pawn?.Value?.ObserverServices?.ObserverTarget?.Value?.Handle);
                var observerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == observedPlayer?.SteamID);

                if (playerInfo?.Skill != skillName && observerInfo?.Skill != skillName) continue;
                foreach (var smoke in smokes.Keys)
                    info.TransmitEntities.Remove(smoke);
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            Event.EnableTransmit();
            SkillUtils.TryGiveWeapon(player, CsItem.SmokeGrenade);
        }
    }
}