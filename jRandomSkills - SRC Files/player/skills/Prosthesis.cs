﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Prosthesis : ISkill
    {
        private const Skills skillName = Skills.Prosthesis;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;
            int damage = @event.DmgHealth;
            var hitgroup = (HitGroup_t)@event.Hitgroup;

            if (!Instance.IsPlayerValid(attacker) || !Instance.IsPlayerValid(victim)) return;
            var victimInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == victim?.SteamID);
            if (victimInfo == null || victimInfo.Skill != skillName) return;

            HitGroup_t[] disabledHitbox = [HitGroup_t.HITGROUP_LEFTARM, HitGroup_t.HITGROUP_RIGHTARM, HitGroup_t.HITGROUP_LEFTLEG, HitGroup_t.HITGROUP_RIGHTLEG];
            if (disabledHitbox.Contains(hitgroup))
                RestoreHealth(victim!, damage);
        }

        private static void RestoreHealth(CCSPlayerController victim, float damage)
        {
            var playerPawn = victim.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid) return;
            var newHealth = playerPawn.Health + damage;

            if (newHealth > 100)
                newHealth = 100;

            playerPawn.Health = (int)newHealth;
            Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_iHealth");
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#9c9c9c", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}