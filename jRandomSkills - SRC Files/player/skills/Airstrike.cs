using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Airstrike : ISkill
    {
        private const Skills skillName = Skills.Airstrike;
        private static readonly ConcurrentDictionary<ulong, bool> airstrikeUsed = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(
                skillName,
                "Nalot powietrzny",
                "Twój pierwszy rzucony wabik wzywa nalot powietrzny",
                "#ff6600");
        }

        public static void NewRound()
        {
            airstrikeUsed.Clear();
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            airstrikeUsed[player.SteamID] = false;
            SkillUtils.TryGiveWeapon(player, CsItem.DecoyGrenade);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            airstrikeUsed.TryRemove(player.SteamID, out _);
        }

        public static void DecoyStarted(EventDecoyStarted @event)
        {
            var player = @event.Userid;
            if(player == null) return;
            if (Instance?.IsPlayerValid(player) == false) return;
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo?.Skill != skillName) return;
            if (airstrikeUsed.TryGetValue(player.SteamID, out bool used) && used) return;

            var decoyPos = new Vector(@event.X, @event.Y, @event.Z);
            airstrikeUsed[player.SteamID] = true;
            SkillUtils.PrintToChat(player, $"Nalot wezwany, ETA 3 sekundy!");

            /*Server.NextFrame(() =>
            {
                var decoy = Utilities.GetEntityFromIndex<CDecoyProjectile>(@event.Entityid);
                if (decoy?.IsValid == true)
                    decoy.EmitSound("nuke.outside.airplane", volume: 0.5f);
            });*/

            for (int wave = 0; wave < 3; wave++)
            {
                float delay = 3.0f + (wave * 2.0f);
                float capturedOffset = wave * 30f;

                Instance?.AddTimer(delay, () =>
                {
                    if (Instance?.IsPlayerValid(player) == false) return;
                    if (player.PlayerPawn?.Value == null) return;

                    int teamNum = (int)player.Team;

                    Vector[] spawnPositions = [
                        new(decoyPos.X - 125f + capturedOffset, decoyPos.Y - 100f - capturedOffset, decoyPos.Z + 700f),
                        new(decoyPos.X + 125f - capturedOffset, decoyPos.Y - 100f + capturedOffset, decoyPos.Z + 700f),
                        new(decoyPos.X - 125f + capturedOffset, decoyPos.Y + 100f + capturedOffset, decoyPos.Z + 700f),
                        new(decoyPos.X + 125f - capturedOffset, decoyPos.Y + 100f - capturedOffset, decoyPos.Z + 700f),
                        new(decoyPos.X, decoyPos.Y, decoyPos.Z + 700f),
                    ];

                    QAngle spawnAngle = new(90, 0, 0);
                    Vector spawnVel = new(0, 0, -400);

                    foreach (var spawnPos in spawnPositions)
                    {
                        SkillUtils.CreateHEGrenadeProjectile(spawnPos, spawnAngle, spawnVel, teamNum);
                    }
                });
            }
        }
    }
}