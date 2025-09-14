using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class FireBullets : ISkill
    {
        private const Skills skillName = Skills.FireBullets;
        private static readonly Dictionary<ulong, PlayerSkillInfo> SkillPlayerInfo = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void NewRound()
        {
            SkillPlayerInfo.Clear();
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return;

            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill == skillName)
                SkillPlayerInfo.Remove(player.SteamID);
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var victim = @event.Userid;
            var attacker = @event.Attacker;

            if (victim == null || attacker == null || victim.IsBot || !victim.IsValid || !victim.PawnIsAlive || !Instance.IsPlayerValid(attacker) || !Instance.IsPlayerValid(victim))
                return;
            if (attacker == victim || attacker.TeamNum == victim.TeamNum)
                return;

            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker.SteamID);
            if (playerInfo?.Skill != skillName) return;

            if (SkillPlayerInfo.ContainsKey(victim.SteamID))
                SkillPlayerInfo[victim.SteamID].HitTick = Server.TickCount;
            else
            {
                SkillPlayerInfo.Add(victim.SteamID, new PlayerSkillInfo
                {
                    SteamID = victim.SteamID,
                    HitTick = Server.TickCount,
                });
            }
        }

        public static void OnTick()
        {
            if(Server.TickCount % 64 != 0) return;
            foreach (var player in Utilities.GetPlayers().FindAll(p => p != null && p.PawnIsAlive))
            {
                if (SkillPlayerInfo.TryGetValue(player.SteamID, out var skillInfo))
                {
                    if (player == null || !player.IsValid || !player.PawnIsAlive) continue;

                    if (Server.TickCount - skillInfo.HitTick >= 64 * 5)
                    {
                        SkillPlayerInfo.Remove(player.SteamID);
                        continue;
                    }
                    else
                    {
                        var playerPawn = player.PlayerPawn.Value;
                        if (playerPawn == null || !playerPawn.IsValid) continue;
                        SkillUtils.TakeHealth(playerPawn, Instance.Random.Next(2, 6));
                        playerPawn.EmitSound("Player.DamageBody.Onlooker", volume: 0.25f);
                        if (playerPawn.Health <= 0)
                        {
                            SkillPlayerInfo.Remove(player.SteamID);
                            continue;
                        }
                    }
                }
            }
        }

        public class PlayerSkillInfo
        {
            public ulong SteamID { get; set; }
            public nint HitTick { get; set; }
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#ffff00", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}
