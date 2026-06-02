using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;

namespace jRandomSkills
{
    public class Psycho : ISkill
    {
        private const Skills skillName = Skills.Psycho;
        private static bool skillEnabled = false;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Psychopata", 
            "Każdy wystrzelony pocisk Terrorystów zadaje im 1 HP", 
            "#a55ece", 
            teamnum:2);
        }

        public static void NewRound()
        {
            skillEnabled = false;
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            skillEnabled = true;
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            foreach (var p in Utilities.GetPlayers())
            {
                if (p != null && p.IsValid && p.Team == CsTeam.CounterTerrorist)
                {
                    var playerInfo = SkillUtils.GetPlayerInfo(player);
                    if (player.PawnIsAlive && playerInfo?.Skill == skillName) return;
                    skillEnabled = true;
                    return;
                }
            }
            skillEnabled = false;
        }

        public static void WeaponFire(EventWeaponFire @event)
        {
            var player = @event.Userid;
            if(player == null) return;
            if (player.Team == CsTeam.Terrorist && skillEnabled)
            {
                var playerPawn = player.PlayerPawn.Value;
                if (playerPawn == null) return;
                var activeWeapon = playerPawn?.WeaponServices?.ActiveWeapon.Value;
                if(activeWeapon == null) return;
                if (activeWeapon.DesignerName == "weapon_knife" || activeWeapon.DesignerName == "weapon_c4" ||
                activeWeapon.DesignerName == "weapon_flashbang" || activeWeapon.DesignerName == "weapon_smokegrenade" ||
                activeWeapon.DesignerName == "weapon_hegrenade" || activeWeapon.DesignerName == "weapon_decoy" ||
                activeWeapon.DesignerName == "weapon_molotov" || activeWeapon.DesignerName == "weapon_incgrenade") return;
                if (player.Health > 0) SkillUtils.TakeHealth(playerPawn, 1, false);
            }
                
        }
    }
}