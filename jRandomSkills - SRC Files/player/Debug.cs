using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public static class Debug
    {
        private static string sessionId = "00000";
        private static readonly string debugFolder = Path.Combine(Instance?.ModuleDirectory ?? ".", "logs");

        public static void Load()
        {
            sessionId = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
            Instance?.RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
            {
                var player = @event.Userid;
                if (player == null || !player.IsValid) return HookResult.Continue;
                WriteToDebug($"{(player.IsBot ? "Bot" : "Gracz")} {player.PlayerName} wbił na serwer.");
                return HookResult.Continue;
            });

            Instance?.RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
            {
                var player = @event.Userid;
                if (player == null || !player.IsValid) return HookResult.Continue;
                WriteToDebug($"{(player.IsBot ? "Bot" : "Gracz")} {player.PlayerName} wyszedł z serwera.");
                return HookResult.Continue;
            });

            Instance?.RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                var teams = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager").Where(t => t != null).ToList();
                var tTeam = teams.FirstOrDefault(t => t.TeamNum == (int)CsTeam.Terrorist);
                var ctTeam = teams.FirstOrDefault(t => t.TeamNum == (int)CsTeam.CounterTerrorist);
                WriteToDebug($"Runda #{tTeam?.Score + ctTeam?.Score + 1} (CT {ctTeam?.Score} : {tTeam?.Score} TT) rozpoczęta.");
                return HookResult.Continue;
            });

            Instance?.RegisterEventHandler<EventRoundFreezeEnd>((@event, info) =>
            {
                WriteToDebug($"Skończyła się prerunda.");
                return HookResult.Continue;
            });

            Instance?.RegisterEventHandler<EventRoundEnd>((@event, info) =>
            {
                var teams = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager").Where(t => t != null).ToList();
                var tTeam = teams.FirstOrDefault(t => t.TeamNum == (int)CsTeam.Terrorist);
                var ctTeam = teams.FirstOrDefault(t => t.TeamNum == (int)CsTeam.CounterTerrorist);
                WriteToDebug($"Runda #{tTeam?.Score + ctTeam?.Score} (CT {ctTeam?.Score} : {tTeam?.Score} TT) zakończona.");
                return HookResult.Continue;
            });

            Instance?.RegisterEventHandler<EventPlayerDeath>((@event, info) =>
            {
                var victim = @event.Userid;
                var attacker = @event.Attacker;
                if (victim != null)
                {
                    if (attacker != null)
                        WriteToDebug($"{(victim.IsBot ? "Bot" : "Gracz")} {victim.PlayerName} zmarł od {(attacker.IsBot ? "bota" : "gracza")} {attacker.PlayerName}.");
                    else
                        WriteToDebug($"{(victim.IsBot ? "Bot" : "Gracz")} {victim.PlayerName} zmarł.");
                }
                return HookResult.Continue;
            });

            Instance?.RegisterEventHandler<EventBombPlanted>((@event, info) =>
            {
                WriteToDebug($"Bomba podłożona.");
                return HookResult.Continue;
            });

            Instance?.RegisterEventHandler<EventBombDefused>((@event, info) =>
            {
                WriteToDebug($"Bomba rozbrojona.");
                return HookResult.Continue;
            });

            Instance?.RegisterListener<OnMapStart>((string mapName) =>
            {
                WriteToDebug($"Mapa zmieniona: {mapName}.");
            });

            Instance?.RegisterEventHandler<EventPlayerShoot>((@event, info) =>
            {
                var player = @event.Userid;
                if (player == null || !player.IsValid) return HookResult.Continue;
                WriteToDebug($"{(player.IsBot ? "Bot" : "Gracz")} {player.PlayerName} oddał strzał.");
                return HookResult.Continue;
            });

            VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
        }

        private static HookResult OnTakeDamage(DynamicHook h)
        {
            CEntityInstance param = h.GetParam<CEntityInstance>(0);
            CTakeDamageInfo param2 = h.GetParam<CTakeDamageInfo>(1);

            if (param == null || param.Entity == null || param2 == null || param2.Attacker == null || param2.Attacker.Value == null)
                return HookResult.Continue;

            CCSPlayerPawn attackerPawn = new(param2.Attacker.Value.Handle);
            CCSPlayerPawn victimPawn = new(param.Handle);

            if (attackerPawn.DesignerName != "player" || victimPawn.DesignerName != "player")
                return HookResult.Continue;

            if (attackerPawn == null || attackerPawn.Controller?.Value == null || victimPawn == null || victimPawn.Controller?.Value == null)
                return HookResult.Continue;

            CCSPlayerController attacker = attackerPawn.Controller.Value.As<CCSPlayerController>();
            CCSPlayerController victim = victimPawn.Controller.Value.As<CCSPlayerController>();

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker.SteamID);
            if (playerInfo == null) return HookResult.Continue;

            WriteToDebug($"{(victim.IsBot ? "Bot" : "Gracz")} {victim.PlayerName} otrzymał obrażenia od {(attacker.IsBot ? "bota" : "gracza")} {attacker.PlayerName}.");
            return HookResult.Continue;
        }

        public static void WriteToDebug(string message)
        {
            string filename = $"debug_{sessionId}.txt";
            string path = Path.Combine(debugFolder, filename);

            Directory.CreateDirectory(debugFolder);
            File.AppendAllText(path, $"[{DateTime.Now:dd.MM.yyyy HH:mm:ss}] {message}{Environment.NewLine}", System.Text.Encoding.UTF8);
        }
    }
}