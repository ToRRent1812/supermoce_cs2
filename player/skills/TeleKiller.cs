using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace jRandomSkills
{
    public class TeleKiller : ISkill
    {
        private const Skills skillName = Skills.TeleKiller;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
            
            Instance.RegisterEventHandler<EventPlayerDeath>((@event, info) =>
            {
                CCSPlayerController? victimPlayer = @event.Userid;
                CCSPlayerController? attackerPlayer = @event.Attacker;

                if (attackerPlayer == victimPlayer) return HookResult.Continue;

                if (attackerPlayer == null || !attackerPlayer.IsValid || victimPlayer == null || !victimPlayer.IsValid) return HookResult.Continue;

                CCSPlayerPawn? attackingPawn = attackerPlayer.PlayerPawn.Value;
                CCSPlayerPawn? victimPawn = victimPlayer.PlayerPawn.Value;

                if (attackingPawn == null || !attackingPawn.IsValid || victimPawn == null || !victimPawn.IsValid) return HookResult.Continue;

                if (victimPawn.AbsOrigin == null) return HookResult.Continue;

                var attackerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == attackerPlayer.SteamID);

                if (attackerInfo?.Skill == skillName)
                {
                    TeleportPlayer(attackerPlayer, victimPawn.AbsOrigin);
                    SkillUtils.AddHealth(attackingPawn, 100);
                }

                return HookResult.Continue;
            });
        }

        public static void TeleportPlayer(CCSPlayerController? player, Vector? position, QAngle? angles = null, Vector? velocity = null)
        {
            if (player == null || !player.IsValid || player.PawnHealth <= 0) return;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;

            pawn.Teleport(position, angles, velocity);

            pawn.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            pawn.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            Utilities.SetStateChanged(player, "CCollisionProperty", "m_CollisionGroup");
            Utilities.SetStateChanged(player, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");

            Server.NextFrame(() =>
            {
                if (!pawn.IsValid || pawn.LifeState != (int)LifeState_t.LIFE_ALIVE) return;

                pawn.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
                pawn.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;

                Utilities.SetStateChanged(player, "CCollisionProperty", "m_CollisionGroup");
                Utilities.SetStateChanged(player, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
            });
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#646464", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}
