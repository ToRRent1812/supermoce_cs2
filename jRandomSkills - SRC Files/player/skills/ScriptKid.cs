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
            SkillUtils.RegisterSkill(skillName,
            "E-sportowiec",
            "Zmniejszony recoil",
            "#429ef5");
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
                var playerInfo = SkillUtils.GetPlayerInfo(player);

                if (playerInfo?.Skill == skillName)
                {
                    var pawn = player.PlayerPawn.Value;
                    if (pawn == null || !pawn.IsValid || pawn.CameraServices == null) continue;
                    pawn.CameraServices.CsViewPunchAngleTick = 0;
                    pawn.CameraServices.CsViewPunchAngleTickRatio = 0f;
                }
            }
        }

        public static void WeaponFire(EventWeaponFire @event)
        {
            var player = @event.Userid;
            if(player == null) return;
            if (Instance?.IsPlayerValid(player) == false) return;

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo?.Skill != skillName) return;

            ApplyNoRecoil(player);

            var pawn = player.PlayerPawn?.Value;
            if (pawn?.WeaponServices?.ActiveWeapon?.Value == null) return;

            var activeWeapon = pawn.WeaponServices.ActiveWeapon.Value;
            var weaponBase = activeWeapon.As<CCSWeaponBase>();
            if (weaponBase == null) return;

            weaponBase.FlRecoilIndex = 0;
            weaponBase.AccuracyPenalty = 0;
        }

        private static void ApplyNoRecoil(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null) return;

            player.ReplicateConVar("weapon_accuracy_nospread", "1");

            var pawn = player.PlayerPawn.Value;
            if (!pawn.IsValid) return;
            if (pawn.AimPunchServices == null) return;

            pawn.AimPunchServices.PredictableBaseAngle.X = 0;
            pawn.AimPunchServices.PredictableBaseAngle.Y = 0;
            pawn.AimPunchServices.PredictableBaseAngle.Z = 0;

            pawn.AimPunchServices.PredictableBaseAngleVel.X = 0;
            pawn.AimPunchServices.PredictableBaseAngleVel.Y = 0;
            pawn.AimPunchServices.PredictableBaseAngleVel.Z = 0;

            pawn.AimPunchServices.UnpredictableBaseAngle.X = 0;
            pawn.AimPunchServices.UnpredictableBaseAngle.Y = 0;
            pawn.AimPunchServices.UnpredictableBaseAngle.Z = 0;

            pawn.AimPunchServices.PredictableBaseTick = -1;
            pawn.AimPunchServices.UnpredictableBaseTick = -1;
        }
    }
}
