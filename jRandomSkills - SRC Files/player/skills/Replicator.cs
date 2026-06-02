using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Replicator : ISkill, IActiveSkill
    {
        private const Skills skillName = Skills.Replicator;

        public static void LoadSkill()
        {
            SkillUtils.RegisterActiveSkill(
                skillName,
                "Dmuchana Lala",
                "Tworzysz swoją replikę, która odbija obrażenia",
                "#a3000b",
                minCooldown: 15,
                maxCooldown: 50,
                cooldownStep: 5);
        }

        public static void NewRound()
        {
            ActiveSkillFramework.OnNewRound();
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            var config = SkillUtils.GetActiveSkillConfig(skillName);
            if (config != null)
                ActiveSkillFramework.OnSkillEnabled(skillName, player, config);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            ActiveSkillFramework.OnSkillDisabled(skillName, player);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive || Instance?.GameRules?.FreezePeriod == true)
                return;

            if (!ActiveSkillFramework.CanUseSkill(skillName, player))
                return;

            var playerPawn = player.PlayerPawn?.Value;
            if (playerPawn == null || playerPawn.CBodyComponent == null || playerPawn.AbsOrigin == null || playerPawn.AbsRotation == null)
                return;

            if (!((PlayerFlags)playerPawn.Flags).HasFlag(PlayerFlags.FL_ONGROUND))
            {
                SkillUtils.PrintToChat(player, "Musisz stać na ziemi!", true);
                return;
            }

            CreateReplica(player);
            ActiveSkillFramework.MarkSkillUsed(skillName, player);
        }

        private static void CreateReplica(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            var replica = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
            if (replica == null || playerPawn == null || !playerPawn.IsValid || playerPawn.AbsOrigin == null || playerPawn.AbsRotation == null)
                return;

            float distance = 70;
            Vector pos = playerPawn.AbsOrigin + SkillUtils.GetForwardVector(playerPawn.AbsRotation) * distance;

            if (((PlayerFlags)playerPawn.Flags).HasFlag(PlayerFlags.FL_DUCKING))
                pos.Z -= 19;
            
            replica.Flags = playerPawn.Flags;
            replica.Flags |= (uint)Flags_t.FL_DUCKING;
            replica.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
            replica.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(replica.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));
            replica.SetModel(playerPawn!.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName);
            replica.Entity!.Name = replica.Globalname = $"Replica_{Server.TickCount}_{(player.Team == CsTeam.CounterTerrorist ? "CT" : "TT")}";
            replica.Teleport(pos, playerPawn.AbsRotation, null);
            replica.DispatchSpawn();
        }

        public static HookResult OnTakeDamage(CEntityInstance entity, CTakeDamageInfo info)
        {
            if (entity == null || entity.Entity == null || info == null || info.Attacker == null || info.Attacker.Value == null)
                return HookResult.Continue;

            if (string.IsNullOrEmpty(entity.Entity?.Name)) return HookResult.Continue;
            if (!entity.Entity.Name.StartsWith("Replica_")) return HookResult.Continue;

            var replica = entity.As<CPhysicsPropMultiplayer>();
            if (replica == null || !replica.IsValid) return HookResult.Continue;
            replica.EmitSound("GlassBottle.BulletImpact", volume: 1f);
            if(Instance?.Random.Next(1,5) == 1)
                replica.AcceptInput("Kill");

            CCSPlayerPawn attackerPawn = new(info.Attacker.Value.Handle);
            if (attackerPawn.DesignerName != "player")
                return HookResult.Continue;

            var attackerTeam = attackerPawn.TeamNum;
            var replicaTeam = replica.Globalname.EndsWith("CT") ? 3 : 2;
            SkillUtils.TakeHealth(attackerPawn, attackerTeam != replicaTeam ? 15 : 5);

            return HookResult.Continue;
        }

    }
}