using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class QuickShot : ISkill
    {
        private const Skills skillName = Skills.QuickShot;

        private static readonly string[] guns =
        [
            "weapon_deagle", "weapon_hegrenade", "weapon_flashbang", "weapon_smokegrenade", "weapon_molotov",
            "weapon_incgrenade", "weapon_decoy", "weapon_taser", "weapon_knife", "weapon_revolver",
            "weapon_glock", "weapon_usp_silencer", "weapon_cz75a", "weapon_fiveseven", "weapon_p250",
            "weapon_tec9", "weapon_elite", "weapon_hkp2000", "weapon_scar20", "weapon_nova",
            "weapon_xm1014", "weapon_mag7", "weapon_sawedoff", "weapon_ssg08", "weapon_awp",
            "weapon_g3sg1"
        ];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!Instance.IsPlayerValid(player)) continue;
                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);

                if (playerInfo?.Skill == skillName)
                {
                    var pawn = player.PlayerPawn.Value!;
                    var weaponServices = pawn.WeaponServices;
                    if (weaponServices == null || weaponServices.ActiveWeapon == null || !weaponServices.ActiveWeapon.IsValid) continue;
                   
                    var weapon = weaponServices.ActiveWeapon.Value;
                    if (weapon == null || !weapon.IsValid || pawn.CameraServices == null) continue;
                    if (!guns.Contains(weapon.DesignerName)) continue;
                    
                    pawn.AimPunchTickBase = 0;
                    pawn.AimPunchTickFraction = 0f;
                    pawn.CameraServices.CsViewPunchAngleTick = 0;
                    pawn.CameraServices.CsViewPunchAngleTickRatio = 0f;

                    Schema.SetSchemaValue<Int32>(weapon.Handle, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick", Server.TickCount);
                    Schema.SetSchemaValue<Int32>(weapon.Handle, "CBasePlayerWeapon", "m_nNextSecondaryAttackTick", Server.TickCount);
                }
            }
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#8a42f5", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}