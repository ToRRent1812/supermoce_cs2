using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;
using jRandomSkills.src.player;

namespace jRandomSkills
{
    public class Magneto : ISkill
    {
        private const Skills skillName = Skills.Magneto;
        private readonly static ConcurrentDictionary<uint, byte> nades = [];
        private readonly static ConcurrentDictionary<uint, byte> players = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Magneto", "Odpychasz granaty wroga", "#6ea8ff");
        }

        public static void NewRound()
        {
            nades.Clear();
            players.Clear();
        }

        public static void OnTick()
        {
            if (Server.TickCount % 8 != 0) return;
            float radius = 350f;

            foreach (var nadeIndex in nades.Keys)
            {
                var nade = Utilities.GetEntityFromIndex<CBaseCSGrenadeProjectile>((int)nadeIndex);
                if (nade == null || !nade.IsValid)
                {
                    nades.TryRemove(nadeIndex, out _);
                    continue;
                }

                foreach (var playerIndex in players.Keys)
                {
                    var player = Utilities.GetPlayerFromIndex((int)playerIndex);
                    if (player == null || !player.IsValid || !player.PawnIsAlive || player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid)
                    {
                        players.TryRemove(playerIndex, out _);
                        continue;
                    }

                    var pawn = player.PlayerPawn.Value;
                    double distanceMoved = SkillUtils.GetDistance(nade.AbsOrigin ?? Vector.Zero, pawn.AbsOrigin ?? Vector.Zero);

                    if (distanceMoved < radius && nade.TeamNum != player.TeamNum)
                    {
                        nade.Teleport(null, null, -nade.AbsVelocity);
                        nades.TryRemove(nadeIndex, out _);
                    }
                }
            }
        }

        public static void OnEntitySpawned(CEntityInstance @event)
        {
            var name = @event.DesignerName;
            if (!name.EndsWith("_projectile")) return;

            var grenade = @event.As<CBaseCSGrenadeProjectile>();
            if (grenade == null || !grenade.IsValid) return;

            nades.TryAdd(grenade.Index, 0);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            players.TryAdd(player.Index, 0);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            players.TryRemove(player.Index, out _);
        }
    }
}