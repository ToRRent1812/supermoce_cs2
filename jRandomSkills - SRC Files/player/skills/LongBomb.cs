using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class LongBomb : ISkill
    {
        private const Skills skillName = Skills.LongBomb;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Długa paka", "Czas detonacji C4 wroga wyniesie 90 sek. o ile będziesz żywy podczas plantu.", "#c22727", 2, 1);
        }

        public static void BombPlanted(EventBombPlanted @event)
        {
            var planter = @event.Userid;
            if (Instance?.IsPlayerValid(planter) == false) return;
            var aliveCTPlayers = Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && p.Team == CsTeam.CounterTerrorist && p.PawnIsAlive && p.PawnHealth > 0).ToArray();
            if (aliveCTPlayers.Length <= 0) return;
            foreach (var alivePlayer in aliveCTPlayers)
            {
                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == alivePlayer.SteamID);
                if (playerInfo?.Skill == skillName)
                {
                    var plantedBomb = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
                    if (plantedBomb != null)
                    {
                        Server.NextFrame(() => {
                            if (plantedBomb != null && plantedBomb.IsValid)
                                plantedBomb.C4Blow = Server.CurrentTime + 90;
                        });
                        foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid))
                            p.PrintToCenterAlert($"Bomba została podłożona!\n90 s do detonacji");
                    }
                }
            }

            
        }
    }
}
