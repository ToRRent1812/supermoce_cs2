using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Supermoce.src.player;
using System.Collections.Concurrent;
using System.Drawing;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class FrozenDecoy : ISkill
    {
        private const Skills skillName = Skills.FrozenDecoy;
        private static readonly ConcurrentDictionary<Vector, byte> decoys = [];
        private static readonly ConcurrentDictionary<ulong, byte> frozenPlayers = [];
        private static readonly Color frozenColor = Color.FromArgb(255, 0, 150, 255);

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Zimny Wabik", 
            "Twój wabik zamraża wszystkich pobliskich graczy", 
            "#00eaff");
        }

        public static void NewRound()
        {
            decoys.Clear();
            frozenPlayers.Clear();
        }

        public static void DecoyStarted(EventDecoyStarted @event)
        {
            var player = @event.Userid;
            if (Instance?.IsPlayerValid(player) == false) return;
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo?.Skill != skillName) return;
            decoys.TryAdd(new Vector(@event.X, @event.Y, @event.Z), 0);
        }

        public static void DecoyDetonate(EventDecoyDetonate @event)
        {
            var player = @event.Userid;
            if(player == null) return;
            if (Instance?.IsPlayerValid(player) == false) return;
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo?.Skill != skillName) return;
            foreach (var decoy in decoys.Keys.Where(v => v.X == @event.X && v.Y == @event.Y && v.Z == @event.Z))
                decoys.TryRemove(decoy, out _);
            Instance?.AddTimer(15.0f, () =>
            {
                SkillUtils.TryGiveWeapon(player, CsItem.DecoyGrenade);
            });
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return;

            if (frozenPlayers.TryRemove(player.SteamID, out _))
            {
                var pawn = player.PlayerPawn.Value;
                if (pawn != null && pawn.IsValid)
                {
                    pawn.Render = Color.FromArgb(255, 255, 255, 255);
                    Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
                    pawn.VelocityModifier = 1f;
                }
            }
        }

        public static void OnTick()
        {
            var inRangePlayers = new HashSet<ulong>();

            foreach (Vector decoyPos in decoys.Keys)
                foreach (var player in Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist || p.Team == CsTeam.CounterTerrorist))
                {
                    var pawn = player.PlayerPawn.Value;
                    if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null) continue;
                    double distance = SkillUtils.GetDistance(decoyPos, pawn.AbsOrigin);
                    if (distance <= 180)
                    {
                        inRangePlayers.Add(player.SteamID);
                        double modifier = Math.Clamp(distance / 180, 0f, 1f);
                        pawn.VelocityModifier = (float)Math.Pow(modifier, 5);

                        if (!frozenPlayers.ContainsKey(player.SteamID))
                        {
                            frozenPlayers.TryAdd(player.SteamID, 0);
                            pawn.Render = frozenColor;
                            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
                        }
                    }
                }

            foreach (var steamId in frozenPlayers.Keys)
            {
                if (!inRangePlayers.Contains(steamId))
                {
                    frozenPlayers.TryRemove(steamId, out _);
                    var player = Utilities.GetPlayerFromSteamId(steamId);
                    if (player != null && player.IsValid && player.PlayerPawn.Value != null && player.PlayerPawn.Value.IsValid)
                    {
                        player.PlayerPawn.Value.Render = Color.FromArgb(255, 255, 255, 255);
                        Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
                    }
                }
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            SkillUtils.TryGiveWeapon(player, CsItem.DecoyGrenade);
        }
    }
}