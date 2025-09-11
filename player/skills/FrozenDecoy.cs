using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static CounterStrikeSharp.API.Core.Listeners;
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

            Instance.RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                decoys.Clear();
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventRoundFreezeEnd>((@event, info) =>
            {
                Instance.AddTimer(0.5f, () =>
                {
                    foreach (var player in Utilities.GetPlayers())
                    {
                        if (!Instance.IsPlayerValid(player)) continue;
                        var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                        if (playerInfo?.Skill != skillName) continue;
                        EnableSkill(player);
                    }
                });

                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventDecoyStarted>((@event, info) =>
            {
                var player = @event.Userid;
                if (player == null || !player.IsValid) return HookResult.Continue;
                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill != skillName) return HookResult.Continue;
                decoys.Add(new Vector(@event.X, @event.Y, @event.Z));
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventDecoyDetonate>((@event, @info) =>
            {
                var player = @event.Userid;
                if (player == null || !player.IsValid) return HookResult.Continue;
                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill != skillName) return HookResult.Continue;
                decoys.RemoveAll(v => v.X == @event.X && v.Y == @event.Y && v.Z == @event.Z);
                return HookResult.Continue;
            });

            Instance.RegisterListener<OnTick>(() =>
            {
                foreach (Vector decoyPos in decoys)
                    foreach (var player in Utilities.GetPlayers())
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
            });
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillUtils.TryGiveWeapon(player, CsItem.DecoyGrenade);
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#00eaff", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, float triggerRadius = 240f, int slownessMultiplier = 10) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float TriggerRadius { get; set; } = triggerRadius;
            public int SlownessMultiplier { get; set; } = slownessMultiplier;
        }
    }
}