using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;
using System.Collections.Concurrent;

namespace jRandomSkills
{
    public class ChaoticDamage : ISkill
    {
        private const Skills skillName = Skills.ChaoticDamage;
        private static readonly ConcurrentDictionary<ulong, PlayerSkillInfo> SkillPlayerInfo = [];
        private static int cd = 25;
        private static bool Randomness = false;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Podejrzane naboje", "Możesz na 5 sek. wywołać na serwerze losowe obrażenia pocisków", "#a8720c");
        }

        public static void NewRound()
        {
            cd = ((Instance?.Random.Next(5, 9)) ?? 5) * 5;
            Randomness = false;
            SkillPlayerInfo.Clear();
        }

        public static void OnTick()
        {
            if (SkillUtils.IsFreezetime()) return;
            foreach (var player in Utilities.GetPlayers())
            {
                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill == skillName && SkillPlayerInfo.TryGetValue(player.SteamID, out var skillInfo))
                    UpdateHUD(player, skillInfo);
            }
        }

        public static void EnableSkill(CCSPlayerController player) =>
            SkillPlayerInfo.TryAdd(player.SteamID, new PlayerSkillInfo
            {
                SteamID = player.SteamID,
                CanUse = true,
                Cooldown = DateTime.MinValue,
            });

        public static void DisableSkill(CCSPlayerController player) =>
            SkillPlayerInfo.TryRemove(player.SteamID, out _);

        private static void UpdateHUD(CCSPlayerController player, PlayerSkillInfo skillInfo)
        {
            float cooldown = 0;
            if (skillInfo != null)
            {
                float time = (int)(skillInfo.Cooldown.AddSeconds(cd) - DateTime.Now).TotalSeconds;
                cooldown = Math.Max(time, 0);

                if (cooldown == 0 && skillInfo.CanUse == false)
                    skillInfo.CanUse = true;
            }

            var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == skillName);
            if (skillData == null) return;

            string skillLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='{skillData.Color}'>{skillData.Name}</font> <br>";
            string remainingLine = cooldown != 0
                ? $"<font class='fontSize-m' color='#FFFFFF'>Poczekaj <font color='#FF0000'>{cooldown}</font> sek.</font>"
                : $"<font class='fontSize-s' class='fontWeight-Bold' color='#FFFFFF'>{skillData.Description}</font><br><font class='fontSize-s' class='fontWeight-Bold' color='#ffffff'>Wciśnij INSPEKT by użyć</font>";

            player.PrintToCenterHtml(skillLine + remainingLine);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            if (player?.IsValid != true || Instance?.GameRules?.FreezePeriod == true) return;
            var playerPawn = player.PlayerPawn?.Value;
            if (playerPawn?.CBodyComponent == null) return;

            if (SkillPlayerInfo.TryGetValue(player.SteamID, out var skillInfo))
            {
                if (!player.PawnIsAlive) return;
                if (skillInfo.CanUse)
                {
                    skillInfo.CanUse = false;
                    skillInfo.Cooldown = DateTime.Now;

                    Randomness = true;
                    SkillUtils.PrintToChatAll($"UWAGA! {ChatColors.LightRed}włączono losowy damage na 5 sek!");

                    Instance?.AddTimer(5.0f, () => Randomness = false);
                }
            }
        }

        public class PlayerSkillInfo
        {
            public ulong SteamID { get; set; }
            public bool CanUse { get; set; }
            public DateTime Cooldown { get; set; }
        }

        public static void OnTakeDamage(DynamicHook h)
        {
            if (!Randomness) return;

            var param = h.GetParam<CEntityInstance>(0);
            var param2 = h.GetParam<CTakeDamageInfo>(1);

            if (param?.Entity == null || param2?.Attacker?.Value == null)
                return;

            var attackerPawn = new CCSPlayerPawn(param2.Attacker.Value.Handle);
            var victimPawn = new CCSPlayerPawn(param.Handle);

            if (attackerPawn.DesignerName != "player" || victimPawn.DesignerName != "player")
                return;

            var attackerController = attackerPawn.Controller?.Value?.As<CCSPlayerController>();
            if (attackerController == null || victimPawn.Controller?.Value == null)
                return;

            var activeWeapon = attackerPawn.WeaponServices?.ActiveWeapon.Value;
            if (activeWeapon != null && attackerController.PawnIsAlive)
            {
                param2.Damage = Instance?.Random.Next(1, 150) ?? 1;
            }
        }
    }
}
