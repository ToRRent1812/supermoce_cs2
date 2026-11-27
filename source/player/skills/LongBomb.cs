using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class LongBomb : ISkill
    {
        private const Skills skillName = Skills.LongBomb;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Długa paka", 
            "Jeżeli będziesz żywy podczas plantu, czas detonacji wyniesie 90 sek.", 
            "#c22727", 
            teamnum:2, 
            objective:1);
        }

        public static void BombPlanted(EventBombPlanted @event)
        {
            var planter = @event.Userid;
            if (Instance?.IsPlayerValid(planter) == false) return;
            var aliveCTPlayers = SkillUtils.CachedPlayers.Where(p => p.Team == CsTeam.CounterTerrorist && p.PawnIsAlive && p.PawnHealth > 0).ToArray();
            if (aliveCTPlayers.Length <= 0) return;
            foreach (var alivePlayer in aliveCTPlayers)
            {
                var playerInfo = SkillUtils.GetPlayerInfo(alivePlayer);
                if (playerInfo?.Skill == skillName)
                {
                    Bomb.SetDetonationIn(90f);
                }
            }

            
        }
    }
}
