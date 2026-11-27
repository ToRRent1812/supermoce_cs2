using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Supermoce
{
    public static class Bomb
    {
        private const string PlantedDesignerName = "planted_c4";
        private const string CarriedDesignerName = "weapon_c4";

        private static CPlantedC4? _planted;
        private static CC4? _carried;

        public static CPlantedC4? Planted
        {
            get
            {
                if (_planted != null && _planted.IsValid) return _planted;
                _planted = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>(PlantedDesignerName).FirstOrDefault();
                return _planted;
            }
        }

        public static CC4? Carried
        {
            get
            {
                if (_carried != null && _carried.IsValid) return _carried;
                _carried = Utilities.FindAllEntitiesByDesignerName<CC4>(CarriedDesignerName).FirstOrDefault();
                return _carried;
            }
        }

        public static void Refresh()
        {
            if (_planted != null && !_planted.IsValid) _planted = null;
            if (_carried != null && !_carried.IsValid) _carried = null;
        }

        public static void Reset()
        {
            _planted = null;
            _carried = null;
        }

        public static bool IsCarryingBomb(CCSPlayerController? player)
        {
            var weapons = player?.PlayerPawn?.Value?.WeaponServices?.MyWeapons;
            if (weapons == null) return false;
            return weapons.Any(w => w?.Value?.DesignerName == CarriedDesignerName);
        }

        public static void SetDetonationIn(float seconds)
        {
            Server.NextFrame(() =>
            {
                if (Planted is { } bomb && bomb.IsValid)
                    bomb.C4Blow = Server.CurrentTime + seconds;
            });
        }

        public static void RemovePlanted()
        {
            if (Planted is { } bomb && bomb.IsValid)
            {
                bomb.AcceptInput("Kill");
                _planted = null;
            }
        }

        public static void ArmInstantly()
        {
            if (Carried is { } bomb && bomb.IsValid)
            {
                bomb.BombPlacedAnimation = false;
                bomb.ArmedTime = 0.0f;
            }
        }

        public static void ForceDefuse()
        {
            Server.NextFrame(() =>
            {
                if (Planted is { } bomb && bomb.IsValid)
                    bomb.DefuseCountDown = 0;
            });
        }

        public static void SetInBombZone(CCSPlayerPawn? pawn, bool value)
        {
            if (pawn == null || !pawn.IsValid) return;
            pawn.InBombZone = value;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_bInBombZone");
        }
    }
}
