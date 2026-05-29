using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class RadarHack : ISkill
    {
        private const Skills skillName = Skills.RadarHack;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Radarowiec", "Widzisz wrogów na radarze", "#2effcb");
        }

        public static void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (Instance?.IsPlayerValid(player) == false) continue;

                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill == skillName)
                {
                    SetEnemiesVisibleOnRadar(player);
                }
            }
        }
        
        private static void SetEnemiesVisibleOnRadar(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null) return;
            int playerIndex = (int)player.Index - 1;

            foreach (var enemy in Utilities.GetPlayers().FindAll(p => p.Team != player.Team && p.PawnIsAlive))
            {
                var enemyPawn = enemy.PlayerPawn.Value;
                if (enemyPawn == null) continue;
                enemyPawn.EntitySpottedState.SpottedByMask[0] |= 1u << playerIndex % 32;

            }

            var bombEntities = Utilities.FindAllEntitiesByDesignerName<CC4>("weapon_c4").ToList();
            if (bombEntities.Any())
            {
                var bomb = bombEntities.FirstOrDefault();
                if (bomb != null)
                    bomb.EntitySpottedState.SpottedByMask[0] |= 1u << playerIndex % 32;
            }
        }
    }
}