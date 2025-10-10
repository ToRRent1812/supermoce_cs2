using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class ScriptKid : ISkill
    {
        private const Skills skillName = Skills.ScriptKid;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "E-sportowiec", "Znacznie zmniejszony recoil", "#429ef5");
        }

        public static void NewRound()
        {
            Server.ExecuteCommand("weapon_accuracy_nospread 0");
        }

        public static void EnableSkill(CCSPlayerController _)
        {
            Server.ExecuteCommand("weapon_accuracy_nospread 1");
        }

        public static void DisableSkill(CCSPlayerController _)
        {
            Server.ExecuteCommand("weapon_accuracy_nospread 0");
        }

        public static void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (Instance?.IsPlayerValid(player) == false) continue;
                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);

                if (playerInfo?.Skill == skillName)
                {
                    var pawn = player.PlayerPawn.Value;
                    if (pawn == null || !pawn.IsValid || pawn.CameraServices == null) continue;
                    pawn.AimPunchTickBase = 0;
                    pawn.AimPunchTickFraction = 0f;
                    pawn.CameraServices.CsViewPunchAngleTick = 0;
                    pawn.CameraServices.CsViewPunchAngleTickRatio = 0f;
                }
            }
        }
    }
}