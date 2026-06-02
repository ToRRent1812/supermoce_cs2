using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class MegaMoly : ISkill
    {
        private const Skills skillName = Skills.MegaMoly;
        private static readonly ConcurrentDictionary<Vector, ulong> fires = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Mega-Tov", 
            "Twoje mołotowy mocniej rozprzestrzeniają ogień", 
            "#ff7b00", 
            teamnum:1);
        }

        public static void NewRound()
        {
            fires.Clear();
        }

        public static void MolotovDetonate(EventMolotovDetonate @event)
        {
            var player = @event.Userid;
            if(player == null) return;
            if (Instance?.IsPlayerValid(player) == false) return;
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill != skillName) return;
            fires.TryAdd(new Vector(@event.X, @event.Y, @event.Z), player.SteamID);
            Instance?.AddTimer(20.0f, () =>
            {
                SkillUtils.TryGiveWeapon(player, CsItem.Molotov);
            });
        }

        public static void OnEntitySpawned(CEntityInstance entity)
        {
            if (entity.DesignerName != "inferno")
                return;

            var inferno = entity.As<CInferno>();
            if (inferno == null || !inferno.IsValid)
                return;

            if (inferno.OwnerEntity == null || !inferno.OwnerEntity.IsValid || inferno.OwnerEntity.Value == null || !inferno.OwnerEntity.Value.IsValid)
                return;

            var pawn = inferno.OwnerEntity.Value.As<CCSPlayerPawn>();
            if (pawn == null || !pawn.IsValid || pawn.Controller == null || !pawn.Controller.IsValid || pawn.Controller.Value == null || !pawn.Controller.Value.IsValid)
                return;

            var player = pawn.Controller.Value.As<CCSPlayerController>();
            if (player == null || !player.IsValid) return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill != skillName)
                return;

            Server.NextFrame(() =>
            {
                if (inferno.IsValid)
                {
                    Server.PrintToConsole($"[BEFORE] FireCount: {inferno.MaxFlames}, FireEffectTickBegin: {inferno.FireEffectTickBegin}, FireLifetime: {inferno.FireLifetime}, FireSpawnOffset: {inferno.FireSpawnOffset}, InPostEffectTime: {inferno.InPostEffectTime}, InfernoType: {inferno.InfernoType}, MaxFlames: {inferno.MaxFlames}, SpreadCount: {inferno.SpreadCount}");
                    inferno.MaxFlames = 80;
                    inferno.FireLifetime = 22f;
                    inferno.SpreadCount = 80;
                    Server.PrintToConsole($"[AFTER] FireCount: {inferno.MaxFlames}, FireEffectTickBegin: {inferno.FireEffectTickBegin}, FireLifetime: {inferno.FireLifetime}, FireSpawnOffset: {inferno.FireSpawnOffset}, InPostEffectTime: {inferno.InPostEffectTime}, InfernoType: {inferno.InfernoType}, MaxFlames: {inferno.MaxFlames}, SpreadCount: {inferno.SpreadCount}");
                }
            });
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillUtils.TryGiveWeapon(player, CsItem.Molotov);
        }
    }
}