using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using jRandomSkills.src.utils;
using static CounterStrikeSharp.API.Core.Listeners;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Fov : ISkill
    {
        private const Skills skillName = Skills.Fov;
        private static readonly Dictionary<ulong, PlayerSkillInfo> SkillPlayerInfo = [];
        private static int cd = 10;
        
        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));

            Instance.RegisterEventHandler<EventRoundFreezeEnd>((@event, info) =>
            {
                Instance.AddTimer(0.2f, () =>
                {
                    foreach (var player in Utilities.GetPlayers())
                    {
                        var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                        if (playerInfo?.Skill == skillName)
                        {
                            cd = Instance.Random.Next(4, 10) * 5;
                            EnableSkill(player);
                        }
                    }
                });

                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventRoundEnd>((@event, info) =>
            {
                SkillPlayerInfo.Clear();
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventPlayerDeath>((@event, info) =>
            {
                var player = @event.Userid;
                if (player == null) return HookResult.Continue;

                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill == skillName)
                    SkillPlayerInfo.Remove(player.SteamID);

                return HookResult.Continue;
            });

            Instance.RegisterListener<OnTick>(() =>
            {
                foreach (var player in Utilities.GetPlayers())
                {
                    var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                    if (playerInfo?.Skill == skillName)
                        if (SkillPlayerInfo.TryGetValue(player.SteamID, out var skillInfo))
                            if (skillInfo.LastClick.AddSeconds(4) >= DateTime.Now)
                                UpdateHUD(player, skillInfo, true);
                            else
                                UpdateHUD(player, skillInfo, false);
                }
            });

            /*Instance.AddCommand("css_useSkill", "Use Skill", (player, _) =>
            {
                if (player == null) return;
                var playerInfo = Instance.skillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill == skillName)
                    UseSkill(player);
            });*/
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillPlayerInfo[player.SteamID] = new PlayerSkillInfo
            {
                SteamID = player.SteamID,
                CanUse = true,
                Cooldown = DateTime.MinValue,
                LastClick = DateTime.MinValue,
                FindedEnemy = false,
            };
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            SkillPlayerInfo.Remove(player.SteamID);
        }

        private static void UpdateHUD(CCSPlayerController player, PlayerSkillInfo skillInfo, bool showInfo)
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
            string remainingLine = "";

            if (showInfo)
                remainingLine = cooldown != 0 ? $"<font class='fontSize-m' color='#FFFFFF'>{Localization.GetTranslation("hud_info", $"<font color='#FF0000'>{cooldown}</font>")}</font>"
                                : !skillInfo.FindedEnemy ? $"<font class='fontSize-m' color='#FF0000'>{Localization.GetTranslation("hud_info_no_enemy")}</font>"
                                : $"<font class='fontSize-s' class='fontWeight-Bold' color='#FFFFFF'>{skillData.Description}</font>";
            else
                remainingLine = cooldown != 0 ? $"<font class='fontSize-m' color='#FFFFFF'>{Localization.GetTranslation("hud_info", $"<font color='#FF0000'>{cooldown}</font>")}</font>" : $"<font class='fontSize-s' class='fontWeight-Bold' color='#FFFFFF'>{skillData.Description}</font>";

            var hudContent = skillLine + remainingLine;
            player.PrintToCenterHtml(hudContent);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;

            if (SkillPlayerInfo.TryGetValue(player.SteamID, out var skillInfo))
            {
                List<CCSPlayerController> enemy = Utilities.GetPlayers().FindAll(p => Instance.IsPlayerValid(p) && p.Team != player.Team && p.PawnIsAlive);
                if (enemy.Count == 0)
                {
                    skillInfo.FindedEnemy = false;
                    skillInfo.LastClick = DateTime.Now;
                    return;
                }

                CCSPlayerController randomEnemy = enemy[Instance.Random.Next(0, enemy.Count)];
                if (randomEnemy == null || !player.IsValid || !player.PawnIsAlive || !randomEnemy.IsValid || !randomEnemy.PawnIsAlive) return;
                if (skillInfo.CanUse)
                {
                    skillInfo.FindedEnemy = true;
                    skillInfo.CanUse = false;
                    skillInfo.Cooldown = DateTime.Now;
                    ChangeFOV(randomEnemy);
                }
                else
                    skillInfo.LastClick = DateTime.Now;
            }
        }

        private static void ChangeFOV(CCSPlayerController player)
        {
            if (player != null && player.IsValid && player.PawnIsAlive)
            {
                var randomfov = Instance.Random.Next(1, 2);

                switch (randomfov)
                {
                    case 1:
                        player.DesiredFOV = 20;
                        break;
                    case 2:
                        player.DesiredFOV = 140;
                        break;
                    default:
                        player.DesiredFOV = 55;
                        break;
                }
                Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");
                player.PrintToChat($" {ChatColors.LightRed}Przeciwnik Cię upił! Zaraz wytrzeźwiejesz.");

                Instance.AddTimer(1.5f, () =>
                {
                    player.DesiredFOV = player.DesiredFOV < 100 ? player.DesiredFOV += 10 : player.DesiredFOV -= 10;
                });

                Instance.AddTimer(3.0f, () =>
                {
                    player.DesiredFOV = player.DesiredFOV < 100 ? player.DesiredFOV += 10 : player.DesiredFOV -= 10;
                });

                Instance.AddTimer(4.5f, () =>
                {
                    player.DesiredFOV = player.DesiredFOV < 100 ? player.DesiredFOV += 10 : player.DesiredFOV -= 10;
                });
                
                Instance.AddTimer(6.0f, () =>
                {
                    player.DesiredFOV = 90;
                });
            }
        }

        public class PlayerSkillInfo
        {
            public ulong SteamID { get; set; }
            public bool CanUse { get; set; }
            public DateTime Cooldown { get; set; }
            public DateTime LastClick { get; set; }
            public bool FindedEnemy { get; set; }
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#1466F5", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}
