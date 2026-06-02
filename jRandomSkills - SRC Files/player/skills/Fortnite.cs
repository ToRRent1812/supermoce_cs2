using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static CounterStrikeSharp.API.Core.Listeners;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Fortnite : ISkill, IActiveSkill
    {
        private const Skills skillName = Skills.Fortnite;
        private static readonly string propModel = "models/props/de_aztec/hr_aztec/aztec_scaffolding/aztec_scaffold_wall_support_128.vmdl";
        private static readonly ConcurrentDictionary<int, int> barricades = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterActiveSkill(
                skillName,
                "Bambik",
                "Stawiasz barykadę na żądanie",
                "#1b04cc",
                minCooldown: 10,
                maxCooldown: 40,
                cooldownStep: 5);
            Instance?.RegisterListener<OnServerPrecacheResources>((ResourceManifest manifest) => manifest.AddResource(propModel));
        }

        public static void NewRound()
        {
            barricades.Clear();
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
            if (Instance?.GameRules?.FreezePeriod == true) return;
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;
            if (!player.IsValid || !player.PawnIsAlive) return;

            if (!ActiveSkillFramework.CanUseSkill(skillName, player))
                return;

            ActiveSkillFramework.MarkSkillUsed(skillName, player);
            CreateBox(player);
        }

        private static void CreateBox(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            var box = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
            if (box == null || playerPawn == null || !playerPawn.IsValid || playerPawn.AbsOrigin == null || playerPawn.AbsRotation == null) return;

            float distance = 50;
            Vector pos = playerPawn.AbsOrigin + SkillUtils.GetForwardVector(playerPawn.AbsRotation) * distance;
            QAngle angle = new(playerPawn.AbsRotation.X, playerPawn.AbsRotation.Y + 90, playerPawn.AbsRotation.Z);

            box.Entity!.Name = box.Globalname = $"FortniteWall_{Server.TickCount}";
            box.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
            box.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(box.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));
            box.DispatchSpawn();
            int hp = Instance?.Random.Next(180, 351) ?? 180;
            barricades.TryAdd((int)box.Index, hp);
            Server.NextFrame(() =>
            {
                box.SetModel(propModel);
                box.Teleport(pos, angle, null);
            });
        }

        public static HookResult OnTakeDamage(CEntityInstance entity, CTakeDamageInfo info)
        {
            if (entity == null || entity.Entity == null || info == null || info.Attacker == null || info.Attacker.Value == null)
                return HookResult.Continue;

            CCSPlayerPawn attackerPawn = new(info.Attacker.Value.Handle);
            if (attackerPawn.DesignerName != "player")
                return HookResult.Continue;

            if (attackerPawn == null || attackerPawn.Controller?.Value == null)
                return HookResult.Continue;
            if (string.IsNullOrEmpty(entity.Entity?.Name)) return HookResult.Continue;
            if (!entity.Entity.Name.StartsWith("FortniteWall")) return HookResult.Continue;

            var box = entity.As<CDynamicProp>();
            if (box == null || !box.IsValid) return HookResult.Continue;
            box.EmitSound("Wood_Plank.BulletImpact", volume: 1f);

            if (barricades.TryGetValue((int)box.Index, out int health))
            {
                health -= (int)info.Damage;
                barricades.AddOrUpdate((int)box.Index, health, (k, v) => health);
                if (health <= 0) box.AcceptInput("Kill");
            }
            else box.AcceptInput("Kill");

            return HookResult.Continue;
        }
    }
}