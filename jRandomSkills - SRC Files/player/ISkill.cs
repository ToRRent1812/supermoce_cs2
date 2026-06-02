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
    public static void HostageFollows(EventHostageFollows _) { }

    public static void DecoyStarted(EventDecoyStarted _) { }
    public static void DecoyDetonate(EventDecoyDetonate _) { }
    public static void MolotovDetonate(EventMolotovDetonate _) { }

    public static void SmokegrenadeDetonate(EventSmokegrenadeDetonate _) { }
    public static void SmokegrenadeExpired(EventSmokegrenadeExpired _) { }

    public class SkillConfig { }
}

public interface IPassiveSkill
{
    public static abstract void LoadSkill();
    public static abstract void EnableSkill(CCSPlayerController player);
    public static void DisableSkill(CCSPlayerController player) { }
    public static void NewRound() { }
}

public interface IActiveSkill
{
    public static abstract void LoadSkill();
    public static abstract void EnableSkill(CCSPlayerController player);
    public static abstract void UseSkill(CCSPlayerController player);
    public static void DisableSkill(CCSPlayerController player) { }
    public static void NewRound() { }
}

public interface IMenuSkill
{
    public static abstract void LoadSkill();
    public static abstract void EnableSkill(CCSPlayerController player);
    public static abstract void TypeSkill(CCSPlayerController player, string[] commands);
    public static void DisableSkill(CCSPlayerController player) { }
    public static void NewRound() { }
}

public enum Skills
{
    Aimbot,
    Airstrike,
    Gambler,
    Anomaly,
    AntyFlash,
    AntyHead,
    Armored,
    Assassin,
    BrokenKnee,
    Astronaut,
    Enemyfire,
    //LongFlame,
    //MegaMoly,
    Giant,
    LongBomb,
    FastEscape,
    InstantEscape,
    SlowHost,
    SpawnSwap,
    Baseball,
    Illusionist,
    Behind,
    Catapult,
    Pallet,
    Chicken,
    Commando,
    Cutter,
    Darkness,
    Deactivator,
    Dash,
    Disarmament,
    Dopamine,
    Watchmaker,
    Dracula,
    NanoKevlar,
    Inflation,
    Duplicator,
    Dwarf,
    EnemySpawn,
    Evade,
    ExplosiveShot,
    FalconEye,
    FireBullets,
    Fireball,
    Flash,
    Fortnite,
    Fov,
    HalfMoney,
    FrozenDecoy,
    Ghost,
    Glaz,
    Glitch,
    Glue,
    GodMode,
    HomingNades,
    Thorns,
    ReZombie,
    HealingSmoke,
    HolyHandGrenade,
    Impostor,
    Psycho,
    InfiniteAmmo,
    //Jackal,
    Jammer,
    Phoenix,
    Halflife,
    Robinhood,
    JumpBan,
    JumpingJack,
    KillerFlash,
    Medic,
    Muhammed,
    Magneto,
    Miner,
    Noclip,
    NoMoney,
    NoNades,
    None,
    OneShot,
    PawelJumper,
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
    Sandstorm,
    ScriptKid,
    SecondLife,
    Smoker,
    ShortBomb,
    Silent,
    Soldier,
    SwapPosition,
    TeleKiller,
    RandomGuns,
    RoundToxin,
    Thief,
    ToxicSmoke,
    Wallhack,
    WeaponsSwap,
    Zeus,
}
