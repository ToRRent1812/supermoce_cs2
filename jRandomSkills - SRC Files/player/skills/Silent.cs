using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.UserMessages;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Silent : ISkill
    {
        private const Skills skillName = Skills.Silent;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Cichociemny", "Twoje kroki i skoki są niesłyszalne dla INNYCH graczy", "#414141");
        }

        public static void PlayerMakeSound(UserMessage um)
        {
            var soundevent = um.ReadUInt("soundevent_hash");
            var userIndex = um.ReadUInt("source_entity_index");
            if (userIndex == 0) return;

            if (Instance?.footstepSoundEvents.Contains(soundevent) == false && Instance?.silentSoundEvents.Contains(soundevent) == false)
                return;

            var player = Utilities.GetPlayers().FirstOrDefault(p => p.Pawn?.Value != null && p.Pawn.Value.IsValid && p.Pawn.Value.Index == userIndex);
            if (Instance?.IsPlayerValid(player) == false) return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player?.SteamID);
            if (playerInfo?.Skill != skillName) return;

            um.Recipients.Clear();
        }
    }
}