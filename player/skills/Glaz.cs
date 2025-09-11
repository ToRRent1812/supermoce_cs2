using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Glaz : ISkill
    {
        private const Skills skillName = Skills.Glaz;
        private static bool exists = false;
        private readonly static List<int> smokes = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));

            Instance.RegisterEventHandler<EventRoundFreezeEnd>((@event, @info) =>
            {
                smokes.Clear();
                Instance.AddTimer(2.0f, () =>
                {
                    foreach (var player in Utilities.GetPlayers())
                    {
                        if (!Instance.IsPlayerValid(player)) continue;

                        var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                        if (playerInfo?.Skill == skillName)
                            EnableSkill(player);
                    }

                    if (exists)
                        Instance.RegisterListener<Listeners.CheckTransmit>(CheckTransmit);
                });
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventRoundEnd>((@event, @info) =>
            {
                smokes.Clear();
                Instance.RemoveListener<Listeners.CheckTransmit>(CheckTransmit);
                exists = false;
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventSmokegrenadeDetonate>((@event, @info) =>
            {
                smokes.Add(@event.Entityid);
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventSmokegrenadeExpired>((@event, @info) =>
            {
                smokes.Remove(@event.Entityid);
                return HookResult.Continue;
            });
        }

        public static void CheckTransmit([CastFrom(typeof(nint))] CCheckTransmitInfoList infoList)
        {
            foreach (var (info, player) in infoList)
            {
                if (player == null) continue;
                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);

                var observedPlayer = Utilities.GetPlayers().FirstOrDefault(p => p?.Pawn?.Value?.Handle == player?.Pawn?.Value?.ObserverServices?.ObserverTarget?.Value?.Handle);
                var observerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == observedPlayer?.SteamID);

                if (playerInfo?.Skill != skillName && observerInfo?.Skill != skillName) continue;

                foreach (var smoke in smokes)
                    info.TransmitEntities.Remove(smoke);
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (!exists)
                Instance.RegisterListener<Listeners.CheckTransmit>(CheckTransmit);
            exists = true;
            SkillUtils.TryGiveWeapon(player, CsItem.SmokeGrenade);
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#5d00ff", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}