using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class Smoker : ISkill
    {
        private const Skills skillName = Skills.Smoker;
        private readonly static ConcurrentDictionary<ulong, List<Timer>> playerSmokes = [];
        private static readonly MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, int, CSmokeGrenadeProjectile> SmokeGrenadeProjectile_CreateFunc = new(GameData.GetSignature("SmokeGrenadeProjectile_CreateFunc"));
        private static readonly object setLock = new();

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Zadymiarz", 
            "Twoje granaty dymne działają całą rundę", 
            "#b9b3b3");
        }

        public static void NewRound()
        {
            lock (setLock)
            {
                foreach (var timers in playerSmokes.Values)
                    timers.ForEach(t => t?.Kill());
                playerSmokes.Clear();
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillUtils.TryGiveWeapon(player, CsItem.SmokeGrenade);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (playerSmokes.TryRemove(player.SteamID, out var timers))
                timers.ForEach(t => t?.Kill());
        }

        public static void SmokegrenadeDetonate(EventSmokegrenadeDetonate @event)
        {
            var player = @event.Userid;
            if(player == null) return;
            if (Instance?.IsPlayerValid(player) == false) return;

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo?.Skill != skillName) return;

            ulong steamID = player.SteamID;

            Vector pos = new(@event.X, @event.Y, @event.Z);
            float refillInterval = 15.5f;

            Timer? smokeTimer = null;
            smokeTimer = Instance?.AddTimer(refillInterval, () =>
            {
                var player = Utilities.GetPlayerFromSteamId(steamID);

                if (player == null || !player.IsValid)
                {
                    smokeTimer?.Kill();
                    return;
                }
                SmokeGrenadeProjectile_CreateFunc.Invoke(pos.Handle, QAngle.Zero.Handle, Vector.Zero.Handle, Vector.Zero.Handle, IntPtr.Zero, 45, player.TeamNum);
            }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);

            if (smokeTimer != null)
            {
                playerSmokes.AddOrUpdate(player.SteamID, [smokeTimer], (_, list) => { list.Add(smokeTimer); return list; });
            }
        }
    }
}