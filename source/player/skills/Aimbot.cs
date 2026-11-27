using CounterStrikeSharp.API.Core;
using Supermoce.src.player;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;

namespace Supermoce
{
    public class Aimbot : ISkill
    {
        private const Skills skillName = Skills.Aimbot;
        private static readonly ConcurrentDictionary<nint, int> hitGroups = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(
                skillName,
                 "Aimbot",
                 "Każdy twój trafiony pocisk to headshot", 
                 "#ff0000");
        }

        public static HookResult OnPlayerTakeDamage(CCSPlayerPawn victim, CTakeDamageInfo info)
        {
            if (victim == null || !victim.IsValid || info == null || info.Attacker == null || info.Attacker.Value == null)
                return HookResult.Continue;

            CCSPlayerPawn attackerPawn = new(info.Attacker.Value.Handle);

            if (attackerPawn.DesignerName != "player")
                return HookResult.Continue;

            if (attackerPawn.Controller?.Value == null || victim.Controller?.Value == null)
                return HookResult.Continue;

            CCSPlayerController attacker = attackerPawn.Controller.Value.As<CCSPlayerController>();
            var playerInfo = SkillUtils.GetPlayerInfo(attacker);
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
                            hitGroups.TryAdd(hitGroupOffset, Marshal.ReadInt32(hitGroupOffset, 56));
                            Marshal.WriteInt32(hitGroupOffset, 56, (int)HitGroup_t.HITGROUP_HEAD);
                        } 
                        else if (hitGroups.TryGetValue(hitGroupOffset, out var hitGroup))
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