using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Aimbot : ISkill
    {
        private const Skills skillName = Skills.Aimbot;
        private static readonly ConcurrentDictionary<nint, int> hitGroups = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Aimbot", "Każdy twój trafiony pocisk to headshot", "#ff0000");
        }

        public static HookResult OnTakeDamage(CEntityInstance entity, CTakeDamageInfo info)
        {
            if (entity == null || entity.Entity == null || info == null || info.Attacker == null || info.Attacker.Value == null)
                return HookResult.Continue;

            CCSPlayerPawn attackerPawn = new(info.Attacker.Value.Handle);
            CCSPlayerPawn victimPawn = new(entity.Handle);

            if (attackerPawn.DesignerName != "player" || victimPawn.DesignerName != "player")
                return HookResult.Continue;

            if (attackerPawn == null || attackerPawn.Controller?.Value == null || victimPawn == null || victimPawn.Controller?.Value == null)
                return HookResult.Continue;

            CCSPlayerController attacker = attackerPawn.Controller.Value.As<CCSPlayerController>();
            CCSPlayerController victim = victimPawn.Controller.Value.As<CCSPlayerController>();

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker.SteamID);
            if (playerInfo == null) return HookResult.Continue;

            if (attacker.PawnIsAlive)
            {   
                nint hitGroupPointer = Marshal.ReadIntPtr(info.Handle, GameData.GetOffset("CTakeDamageInfo_HitGroup"));
                if (hitGroupPointer != nint.Zero)
                {
                    nint hitGroupOffset = Marshal.ReadIntPtr(hitGroupPointer, 16);
                    if (hitGroupOffset != nint.Zero)
                    {
                        if (playerInfo.Skill == skillName)
                        {
                            int oldValue = Marshal.ReadInt32(hitGroupOffset, 56);
                            hitGroups.TryAdd(hitGroupOffset, Marshal.ReadInt32(hitGroupOffset, 56));
                            Marshal.WriteInt32(hitGroupOffset, 56, (int)HitGroup_t.HITGROUP_HEAD);
                        } else if (hitGroups.TryGetValue(hitGroupOffset, out var hitGroup))
                                Marshal.WriteInt32(hitGroupOffset, 56, hitGroup);
                    }
                }
            }

            return HookResult.Continue;
        }

        public static void DisableSkill(CCSPlayerController _)
        {
            foreach (var hit in hitGroups)
                Marshal.WriteInt32(hit.Key, 56, hit.Value);
        }
    }
}