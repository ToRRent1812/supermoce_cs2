using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class SmokeMaster : ISkill
    {
        private const Skills skillName = Skills.SmokeMaster;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(
                skillName,
                "Super Smoke",
                "Twoje granaty dymne mają znacznie większą objętość",
                "#ff6600");
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            SkillUtils.TryGiveWeapon(player, CsItem.SmokeGrenade);
        }

        public static void SmokegrenadeDetonate(EventSmokegrenadeDetonate @event)
        {
            var player = @event.Userid;
            if(player == null) return;
            if (Instance?.IsPlayerValid(player) == false) return;
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo?.Skill != skillName) return;

            var smokePos = new Vector(@event.X, @event.Y, @event.Z);

            Instance?.AddTimer(0.5f, () =>
            {
                if (Instance?.IsPlayerValid(player) == false) return;
                if (player.PlayerPawn?.Value == null) return;

                int teamNum = (int)player.Team;

                Vector[] spawnPositions = [
                    new(smokePos.X - 150f, smokePos.Y - 100f, smokePos.Z + 50f),
                    new(smokePos.X + 150f, smokePos.Y - 100f, smokePos.Z + 50f),
                    new(smokePos.X - 150f, smokePos.Y + 100f, smokePos.Z + 50f),
                    new(smokePos.X + 150f, smokePos.Y + 100f, smokePos.Z + 50f),
                ];

                QAngle spawnAngle = new(90, 0, 0);
                Vector spawnVel = new(0, 0, -400);

                foreach (var spawnPos in spawnPositions)
                {
                    SkillUtils.CreateSmokeGrenadeProjectile(spawnPos, spawnAngle, spawnVel, teamNum);
                }
            });
        }
    }
}
