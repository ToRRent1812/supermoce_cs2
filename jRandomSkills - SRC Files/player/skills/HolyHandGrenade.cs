using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class HolyHandGrenade : ISkill
    {
        private const Skills skillName = Skills.HolyHandGrenade;
        private static readonly float damageMultiplier = Config.GetValue<float>(skillName, "damageMultiplier");
        private static readonly float damageRadiusMultiplier = Config.GetValue<float>(skillName, "damageRadiusMultiplier");

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void OnEntitySpawned(CEntityInstance @event)
        {
            var name = @event.DesignerName;
            if (!name.EndsWith("hegrenade_projectile"))
                return;

            Server.NextFrame(() =>
            {
                var hegrenade = @event.As<CHEGrenadeProjectile>();
                if (hegrenade == null || !hegrenade.IsValid) return;

                var playerPawn = hegrenade.Thrower.Value;
                if (playerPawn == null || !playerPawn.IsValid) return;

                var player = Utilities.GetPlayers().FirstOrDefault(p => p.PlayerPawn.Index == playerPawn.Index);
                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player?.SteamID);
                if (playerInfo?.Skill != skillName) return;

                hegrenade.Damage *= damageMultiplier;
                hegrenade.DmgRadius *= damageRadiusMultiplier;
            });
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillUtils.TryGiveWeapon(player, CsItem.HEGrenade);
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#ffdd00", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, float damageMultiplier = 2f, float damageRadiusMultiplier = 2f) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float DamageMultiplier { get; set; } = damageMultiplier;
            public float DamageRadiusMultiplier { get; set; } = damageRadiusMultiplier;
        }
    }
}