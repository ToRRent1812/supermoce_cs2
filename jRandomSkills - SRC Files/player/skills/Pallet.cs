using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static CounterStrikeSharp.API.Core.Listeners;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Pallet : ISkill, IActiveSkill
    {
        private const Skills skillName = Skills.Pallet;
        private static readonly string[] PropModels =
        [
            "models/props/cs_italy/italy_wine_pallet.vmdl",
            "models/props/de_dust/dust_aid_crate_74.vmdl",
            "models/props/de_vertigo/pallet_cinderblock01.vmdl",
            "models/props/de_vertigo/pallet_stack01.vmdl",
            "models/props/de_dust/pallet01.vmdl",
        ];

        public static void LoadSkill()
        {
            SkillUtils.RegisterActiveSkill(
                skillName,
                "Magazynier",
                "Stawiasz niezniszczalną paletę na żądanie",
                "#1b04cc",
                minCooldown: 15,
                maxCooldown: 60,
                cooldownStep: 5);

            Instance?.RegisterListener<OnServerPrecacheResources>(manifest =>
            {
                foreach (var model in PropModels)
                    manifest.AddResource(model);
            });
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
            if (player == null || player.IsValid == false || player.PawnIsAlive == false || Instance?.GameRules?.FreezePeriod == true)
                return;

            if (!ActiveSkillFramework.CanUseSkill(skillName, player))
                return;

            var playerPawn = player.PlayerPawn?.Value;
            if (playerPawn == null || playerPawn.CBodyComponent == null || playerPawn.AbsOrigin == null || playerPawn.AbsRotation == null)
                return;

            if (!((PlayerFlags)playerPawn.Flags).HasFlag(PlayerFlags.FL_ONGROUND))
                return;

            if (!CreateBox(playerPawn))
                return;

            ActiveSkillFramework.MarkSkillUsed(skillName, player);
        }

        private static bool CreateBox(CCSPlayerPawn playerPawn)
        {
            var box = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
            if (box == null || !box.IsValid)
                return false;

            var origin = playerPawn.AbsOrigin;
            var rotation = playerPawn.AbsRotation;
            if (origin == null || rotation == null)
                return false;

            float distance = 100;
            Vector pos = origin! + SkillUtils.GetForwardVector(rotation!) * distance;
            QAngle angle = new(rotation!.X, rotation!.Y + 90, rotation!.Z);

            box.Entity!.Name = box.Globalname = $"Pallet_{Server.TickCount}";
            box.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
            box.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(box.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));
            box.DispatchSpawn();

            string selectedModel = PropModels[Instance!.Random.Next(PropModels.Length)];
            Server.NextFrame(() =>
            {
                box.SetModel(selectedModel);
                box.Teleport(pos, angle, null);
            });

            return true;
        }
    }
}
