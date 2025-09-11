/*using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Mute : ISkill
    {
        private const Skills skillName = Skills.Mute;

        public static void LoadSkill()
        {
            if (Config.config.SkillsInfo.FirstOrDefault(s => s.Name == skillName.ToString())?.Active != true)
                return;

            SkillUtils.RegisterSkill(skillName, "#2fc468");

            Instance.RegisterListener<Listeners.OnEntitySpawned>(@event =>
            {
                var name = @event.DesignerName;
                if (!name.EndsWith("_projectile"))
                    return;

                var grenade = @event.As<CBaseCSGrenadeProjectile>();
                var pawn = grenade.OwnerEntity.Value.As<CCSPlayerPawn>();
                var player = pawn.Controller.Value.As<CCSPlayerController>();

                var playerInfo = Instance.skillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill != skillName) return;

                Server.NextFrame(() => {
                    var grenade = @event.As<CBaseCSGrenadeProjectile>();
                    grenade.DetonateTime = float.MaxValue;

                    switch (name)
                    {
                        case "smokegrenade_projectile":
                            var smoke = @event.As<CSmokeGrenadeProjectile>();
                            smoke.SmokeEffectTickBegin = int.MaxValue;
                            break;
                        case "molotov_projectile":
                            var molotov = @event.As<CMolotovProjectile>();
                            molotov.Detonated = true;
                            molotov.StillTimer.Timestamp = float.MaxValue;
                            break;
                        case "decoy_projectile":
                            var decoy = @event.As<CDecoyProjectile>();
                            decoy.ExpireTime = int.MaxValue;
                            decoy.DecoyShotTick = int.MaxValue;
                            decoy.ShotsRemaining = int.MaxValue;
                            break;
                    }
                    // deoy smoke molo/inter
                    Instance.AddTimer(5f, () =>
                    {
                        if (@event != null && @event.IsValid)
                            @event.Remove();
                    });
                });
            });
        }
    }
}*/