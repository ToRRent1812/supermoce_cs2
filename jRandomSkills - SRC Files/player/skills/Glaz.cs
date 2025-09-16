using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Glaz : ISkill
    {
        private const Skills skillName = Skills.Glaz;
        private readonly static List<int> smokes = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void NewRound()
        {
            smokes.Clear();
        }

        public static void SmokegrenadeDetonate(EventSmokegrenadeDetonate @event)
        {
            smokes.Add(@event.Entityid);
        }

        public static void SmokegrenadeExpired(EventSmokegrenadeExpired @event)
        {
            smokes.Remove(@event.Entityid);
        }

        public static void CheckTransmit([CastFrom(typeof(nint))] CCheckTransmitInfoList infoList)
        {
            foreach (var (info, player) in infoList)
            {
                if (player == null) continue;
                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);

                var observedPlayer = Utilities.GetPlayers().FirstOrDefault(p => p?.Pawn?.Value?.Handle == player?.Pawn?.Value?.ObserverServices?.ObserverTarget?.Value?.Handle);
                var observerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == observedPlayer?.SteamID);

                if (playerInfo?.Skill != skillName && observerInfo?.Skill != skillName) continue;
                foreach (var smoke in smokes)
                    info.TransmitEntities.Remove(smoke);
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillUtils.EnableTransmit();
            SkillUtils.TryGiveWeapon(player, CsItem.SmokeGrenade);
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#5d00ff", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}