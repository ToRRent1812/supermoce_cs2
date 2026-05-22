using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static CounterStrikeSharp.API.Core.Listeners;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Pallet : ISkill
    {
        private const Skills skillName = Skills.Pallet;
        private static int cd = 15;
        private static string propModel1 = "models/props/cs_italy/italy_wine_pallet.vmdl";
        private static string propModel2 = "models/props/de_dust/dust_aid_crate_74.vmdl";
        private static string propModel3 = "models/props/de_vertigo/pallet_cinderblock01.vmdl";
        private static string propModel4 = "models/props/de_vertigo/pallet_stack01.vmdl";
        private static string propModel5 = "models/props/de_dust/pallet01.vmdl";
        private static readonly ConcurrentDictionary<ulong, PlayerSkillInfo> SkillPlayerInfo = [];
        private static readonly ConcurrentDictionary<ulong, int> barricades = [];
        private static readonly object setLock = new();

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Magazynier", "Stawiasz niezniszczalną paletę na żądanie", "#1b04cc");
            Instance?.RegisterListener<OnServerPrecacheResources>((ResourceManifest manifest) => {
                manifest.AddResource(propModel1);
                manifest.AddResource(propModel2);
                manifest.AddResource(propModel3);
                manifest.AddResource(propModel4);
                manifest.AddResource(propModel5);
            });
        }

        public static void NewRound()
        {
            cd = ((Instance?.Random.Next(3, 9)) ?? 2) * 5;
            lock (setLock)
            {
                SkillPlayerInfo.Clear();
                barricades.Clear();
            }
        }

        public static void OnTick()
        {
            if (SkillUtils.IsFreezetime()) return;
            foreach (var player in Utilities.GetPlayers())
            {
                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill == skillName)
                    if (SkillPlayerInfo.TryGetValue(player.SteamID, out var skillInfo))
                        UpdateHUD(player, skillInfo);
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillPlayerInfo.TryAdd(player.SteamID, new PlayerSkillInfo
            {
                SteamID = player.SteamID,
                CanUse = true,
                Cooldown = DateTime.MinValue,
            });
        }

        public static void DisableSkill(CCSPlayerController player)
        {
              SkillPlayerInfo.TryRemove(player.SteamID, out _);
        }

        private static void UpdateHUD(CCSPlayerController player, PlayerSkillInfo skillInfo)
        {
            float cooldown = 0;
            if (skillInfo != null)
            {
                float time = (int)(skillInfo.Cooldown.AddSeconds(cd) - DateTime.Now).TotalSeconds;
                cooldown = Math.Max(time, 0);

                if (cooldown == 0 && skillInfo?.CanUse == false)
                    skillInfo.CanUse = true;
            }

            var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == skillName);
            if (skillData == null) return;

            string skillLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='{skillData.Color}'>{skillData.Name}</font> <br>";
            string remainingLine = cooldown != 0 ? $"<font class='fontSize-m' color='#FFFFFF'>Poczekaj <font color='#FF0000'>{cooldown}</font> sek.</font>" : $"<font class='fontSize-s' class='fontWeight-Bold' color='#FFFFFF'>{skillData.Description}</font><br><font class='fontSize-s' class='fontWeight-Bold' color='#ffffff'>Wciśnij INSPEKT by użyć</font>";

            var hudContent = skillLine + remainingLine;
            player.PrintToCenterHtml(hudContent);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            if(Instance?.GameRules?.FreezePeriod == true) return;
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;

            if (SkillPlayerInfo.TryGetValue(player.SteamID, out var skillInfo))
            {
                if (!player.IsValid || !player.PawnIsAlive) return;
                if (skillInfo.CanUse)
                {
                    skillInfo.CanUse = false;
                    skillInfo.Cooldown = DateTime.Now;
                    CreateBox(player);
                }
            }
        }

        private static void CreateBox(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            var box = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
            if (box == null || playerPawn == null || !playerPawn.IsValid || playerPawn.AbsOrigin == null || playerPawn.AbsRotation == null) return;

            float distance = 100;
            Vector pos = playerPawn.AbsOrigin + SkillUtils.GetForwardVector(playerPawn.AbsRotation) * distance;
            QAngle angle = new(playerPawn.AbsRotation.X, playerPawn.AbsRotation.Y + 90, playerPawn.AbsRotation.Z);

            box.Entity!.Name = box.Globalname = $"Pallet_{Server.TickCount}";
            box.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
            box.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(box.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));
            box.DispatchSpawn();
            barricades.TryAdd(box.Index, 1);
            int rngModel = Instance?.Random.Next(1, 6) ?? 1;
            string selectedModel = rngModel switch
            {
                1 => propModel1,
                2 => propModel2,
                3 => propModel3,
                4 => propModel4,
                5 => propModel5,
                _ => propModel1
            };
            Server.NextFrame(() =>
            {
                box.SetModel(selectedModel);
                box.Teleport(pos, angle, null);
            });
        }

        public class PlayerSkillInfo
        {
            public ulong SteamID { get; set; }
            public bool CanUse { get; set; }
            public DateTime Cooldown { get; set; }
        }
    }
}