using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class FrozenDecoy : ISkill
    {
        private const Skills skillName = Skills.FrozenDecoy;
        private static readonly float decoyRadius = Config.GetValue<float>(skillName, "triggerRadius");
        private static readonly int slownessMultiplier = Config.GetValue<int>(skillName, "slownessMultiplier");
        private static readonly List<Vector> decoys = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void NewRound()
        {
            decoys.Clear();
        }

        public static void DecoyStarted(EventDecoyStarted @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return;
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill != skillName) return;
            decoys.Add(new Vector(@event.X, @event.Y, @event.Z));
        }

        public static void DecoyDetonate(EventDecoyDetonate @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return;
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill != skillName) return;
            decoys.RemoveAll(v => v.X == @event.X && v.Y == @event.Y && v.Z == @event.Z);
            Instance.AddTimer(20.0f, () =>
            {
                SkillUtils.TryGiveWeapon(player, CsItem.DecoyGrenade);
            });
        }

        public static void OnTick()
        {
            foreach (Vector decoyPos in decoys)
                foreach (var player in Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist || p.Team == CsTeam.CounterTerrorist))
                {
                    var pawn = player.PlayerPawn.Value;
                    if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null) return;
                    double distance = SkillUtils.GetDistance(decoyPos, pawn.AbsOrigin);
                    if (distance <= decoyRadius)
                    {
                        double modifier = Math.Clamp(distance / decoyRadius, 0f, 1f);
                        pawn.VelocityModifier = (float)Math.Pow(modifier, slownessMultiplier);
                    }
                }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillUtils.TryGiveWeapon(player, CsItem.DecoyGrenade);
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#00eaff", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, float triggerRadius = 180, int slownessMultiplier = 5) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float TriggerRadius { get; set; } = triggerRadius;
            public int SlownessMultiplier { get; set; } = slownessMultiplier;
        }
    }
}