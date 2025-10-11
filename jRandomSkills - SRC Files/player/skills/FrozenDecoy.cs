using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class FrozenDecoy : ISkill
    {
        private const Skills skillName = Skills.FrozenDecoy;
        private static readonly ConcurrentDictionary<Vector, byte> decoys = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Zamrażający Wabik", "Twój wabik zamraża wszystkich pobliskich graczy", "#00eaff");
        }

        public static void NewRound()
        {
            decoys.Clear();
        }

        public static void DecoyStarted(EventDecoyStarted @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return;
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill != skillName) return;
            decoys.TryAdd(new Vector(@event.X, @event.Y, @event.Z), 0);
        }

        public static void DecoyDetonate(EventDecoyDetonate @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return;
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill != skillName) return;
            foreach (var decoy in decoys.Keys.Where(v => v.X == @event.X && v.Y == @event.Y && v.Z == @event.Z))
                decoys.TryRemove(decoy, out _);
            Instance?.AddTimer(20.0f, () =>
            {
                SkillUtils.TryGiveWeapon(player, CsItem.DecoyGrenade);
            });
        }

        public static void OnTick()
        {
            foreach (Vector decoyPos in decoys.Keys)
                foreach (var player in Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist || p.Team == CsTeam.CounterTerrorist))
                {
                    var pawn = player.PlayerPawn.Value;
                    if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null) return;
                    double distance = SkillUtils.GetDistance(decoyPos, pawn.AbsOrigin);
                    if (distance <= 180)
                    {
                        double modifier = Math.Clamp(distance / 180, 0f, 1f);
                        pawn.VelocityModifier = (float)Math.Pow(modifier, 5);
                        
                    }
                }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillUtils.TryGiveWeapon(player, CsItem.DecoyGrenade);
        }
    }
}