using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.UserMessages;

namespace jRandomSkills.src.player;

public interface ISkill
{
    public static void LoadSkill() { }
    public static void EnableSkill(CCSPlayerController _) { }
    public static void DisableSkill(CCSPlayerController _) { }
    public static void UseSkill(CCSPlayerController _) { }
    public static void TypeSkill(CCSPlayerController _, string[] __) { }

    public static void OnTakeDamage(DynamicHook _) { }
    public static void OnEntitySpawned(CEntityInstance _) { }
    public static void OnTick() { }
    public static void CheckTransmit([CastFrom(typeof(nint))] CCheckTransmitInfoList _) { }

    public static void NewRound() { }
    public static void PlayerMakeSound(UserMessage _) { }
    public static void PlayerBlind(EventPlayerBlind _) { }
    public static void PlayerHurt(EventPlayerHurt _) { }
    public static void PlayerDeath(EventPlayerDeath _) { }
    public static void PlayerJump(EventPlayerJump _) { }

    public static void WeaponFire(EventWeaponFire _) { }
    public static void WeaponEquip(EventItemEquip _) { }
    public static void WeaponPickup(EventItemPickup _) { }
    public static void WeaponReload(EventWeaponReload _) { }
    public static void GrenadeThrown(EventGrenadeThrown _) { }

    public static void BombBeginplant(EventBombBeginplant _) { }
    public static void BombPlanted(EventBombPlanted _) { }
    public static void BombBegindefuse(EventBombBegindefuse _) { }

    public static void DecoyStarted(EventDecoyStarted _) { }
    public static void DecoyDetonate(EventDecoyDetonate _) { }

    public static void SmokegrenadeDetonate(EventSmokegrenadeDetonate _) { }
    public static void SmokegrenadeExpired(EventSmokegrenadeExpired _) { }

    public class SkillConfig { }
}

public enum Skills
{
    Aimbot,
    Gambler,
    Anomaly,
    AntyFlash,
    AntyHead,
    Armored,
    Assassin,
    Astronaut,
    Behind,
    Catapult,
    Chicken,
    Commando,
    Cutter,
    Darkness,
    Deactivator,
    Deaf,
    Dash,
    Disarmament,
    Dopamine,
    Watchmaker,
    Dracula,
    Duplicator,
    Dwarf,
    EnemySpawn,
    Evade,
    ExplosiveShot,
    FalconEye,
    FireBullets,
    Flash,
    Fortnite,
    Fov,
    FriendlyFire,
    FrozenDecoy,
    Ghost,
    Glaz,
    Glitch,
    Glue,
    GodMode,
    Thorns,
    ReZombie,
    HealingSmoke,
    HolyHandGrenade,
    Impostor,
    InfiniteAmmo,
    Jackal,
    Jammer,
    JumpBan,
    JumpingJack,
    KillerFlash,
    LongZeus,
    Medic,
    Muhammed,
    Noclip,
    NoMoney,
    NoNades,
    None,
    OneShot,
    PawelJumper,
    Phoenix,
    Pilot,
    Planter,
    Prosthesis,
    PsychicDefusing,
    ChaoticDamage,
    QuickShot,
    Rapper,
    RadarHack,
    Rambo,
    RandomWeapon,
    RefillOnKill,
    Regeneration,
    Replicator,
    Retreat,
    ReturnToSender,
    TimeManipulator,
    RichBoy,
    Rubber,
    Push,
    Saper,
    ScriptKid,
    SecondLife,
    Shade,
    ShortBomb,
    Silent,
    Soldier,
    SoundMaker,
    Spectator,
    SwapPosition,
    TeleKiller,
    RandomGuns,
    Thief,
    ThirdEye,
    ToxicSmoke,
    Wallhack,
    WeaponsSwap,
    Zeus,
}
