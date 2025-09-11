using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static CounterStrikeSharp.API.Core.Listeners;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class HealingSmoke : ISkill
    {
        private const Skills skillName = Skills.HealingSmoke;
        private static readonly float smokeRadius = Config.GetValue<float>(skillName, "smokeRadius");
        private static readonly List<Vector> smokes = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));

            Instance.RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                smokes.Clear();
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventRoundFreezeEnd>((@event, info) =>
            {
                Instance.AddTimer(0.1f, () =>
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

            Instance.RegisterEventHandler<EventSmokegrenadeDetonate>((@event, info) =>
            {
                var player = @event.Userid;
                if (player == null || !player.IsValid) return HookResult.Continue;
                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill != skillName) return HookResult.Continue;
                smokes.Add(new Vector(@event.X, @event.Y, @event.Z));

                Instance.AddTimer(10.0f, () =>
                {
                    player.GiveNamedItem("weapon_smokegrenade");
                });

                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventSmokegrenadeExpired>((@event, @info) =>
            {
                var player = @event.Userid;
                if (player == null || !player.IsValid) return HookResult.Continue;
                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill != skillName) return HookResult.Continue;
                smokes.RemoveAll(v => v.X == @event.X && v.Y == @event.Y && v.Z == @event.Z);
                return HookResult.Continue;
            });

            Instance.RegisterListener<OnEntitySpawned>(@event =>
            {
                var name = @event.DesignerName;
                if (name != "smokegrenade_projectile") return;

                var grenade = @event.As<CBaseCSGrenadeProjectile>();
                if (grenade == null || !grenade.IsValid || grenade.OwnerEntity == null || !grenade.OwnerEntity.IsValid || grenade.OwnerEntity.Value == null || !grenade.OwnerEntity.Value.IsValid) return;
                var pawn = grenade.OwnerEntity.Value.As<CCSPlayerPawn>();
                if (pawn == null || !pawn.IsValid || pawn.Controller == null || !pawn.Controller.IsValid || pawn.Controller.Value == null || !pawn.Controller.Value.IsValid) return;
                var player = pawn.Controller.Value.As<CCSPlayerController>();
                if (player == null || !player.IsValid) return;

                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill != skillName) return;

                Server.NextFrame(() =>
                {
                    var smoke = @event.As<CSmokeGrenadeProjectile>();
                    smoke.SmokeColor.X = 0;
                    smoke.SmokeColor.Y = 255;
                    smoke.SmokeColor.Z = 30;
                });
            });

            Instance.RegisterListener<OnTick>(() =>
            {
                foreach (Vector smokePos in smokes)
                    foreach (var player in Utilities.GetPlayers())
                        if (Server.TickCount % 32 == 0)
                             if (player.PlayerPawn.Value != null && player.PlayerPawn.Value.IsValid && player.PlayerPawn.Value.AbsOrigin != null)
                                if (SkillUtils.GetDistance(smokePos, player.PlayerPawn.Value.AbsOrigin) <= smokeRadius)
                                    AddHealth(player.PlayerPawn.Value, Instance.Random.Next(3, 9));
            });
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillUtils.TryGiveWeapon(player, CsItem.SmokeGrenade);
        }

        private static void AddHealth(CCSPlayerPawn player, int health)
        {
            if (player.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                return;

            if (player.Health != player.MaxHealth)
                player.EmitSound("Healthshot.Success", volume: 0.3f);

            player.Health = Math.Min(player.Health + health, player.MaxHealth);
            Utilities.SetStateChanged(player, "CBaseEntity", "m_iHealth");
        }
        
        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#1fe070", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, float smokeRadius = 180) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float SmokeRadius { get; set; } = smokeRadius;
        }
    }
}
