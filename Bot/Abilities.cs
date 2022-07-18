namespace Bot;

internal static class Abilities
{
    //you can get all these values from the stableid.json file (just search for it on your PC)

    public static int RESEARCH_BANSHEE_CLOAK = 790;
    public static int RESEARCH_INFERNAL_PREIGNITER = 761;
    public static int RESEARCH_UPGRADE_MECH_AIR = 3699;
    public static int RESEARCH_UPGRADE_MECH_ARMOR = 3700;
    public static int RESEARCH_UPGRADE_MECH_GROUND = 3701;

    public static int RESEARCH_UPGRADE_INFANTRY_ARMOR = 3697; //656; //;  
    public static int RESEARCH_UPGRADE_INFANTRY_WEAPON = 3698; // 652; //;  
    
    public static int RESEARCH_STIMPACK = 730; // 652; //;  
    public static int RESEARCH_COMBAT_SHIELD = 731; // 652; //;  
    public static int RESEARCH_CONCUSSIVE_SHELL = 733; // 652; //;  
    
    public static int COMMAND_CENTER_ORBITAL_UPGRADE = 1516;

    public static int CANCEL_CONSTRUCTION = 314;
    public static int CANCEL = 3659;
    public static int CANCEL_LAST = 3671;
    public static int LIFT = 3679;
    public static int LAND = 3678;

    public static int SMART = 1;
    public static int STOP = 4;
    public static int ATTACK = 23;
    public static int MOVE = 16;
    public static int PATROL = 17;
    public static int RALLY = 3673;
    public static int REPAIR = 316;

    // Thor
    public static int THOR_SWITCH_AP = 2362;
    public static int THOR_SWITCH_NORMAL = 2364;
    
    // Marine
    public static int GENERAL_STIMPACK = 3675;

    public static int SCANNER_SWEEP = 399;
    public static int YAMATO = 401;
    public static int CALL_DOWN_MULE = 171;
    public static int CLOAK = 3676;
    public static int REAPER_GRENADE = 2588;
    public static int DEPOT_RAISE = 558;
    public static int DEPOT_LOWER = 556;
    public static int SIEGE_TANK = 388;
    public static int UNSIEGE_TANK = 390;
    public static int TRANSFORM_TO_HELLBAT = 1998;
    public static int TRANSFORM_TO_HELLION = 1978;
    public static int UNLOAD_BUNKER = 408;
    public static int SALVAGE_BUNKER = 32;

    //gathering/returning minerals
    public static int GATHER_RESOURCES = 295;
    public static int RETURN_RESOURCES = 296;


    //gathering/returning minerals
    public static int GATHER_MINERALS = 295;
    public static int RETURN_MINERALS = 296;
    
    
    
    public static int BurrowBanelingDown = 1374;
    public static int BurrowDroneDown = 1378;
    public static int BurrowHydraliskDown = 1382;
    public static int BurrowRoachDown = 1386;
    public static int BurrowZerglingDown = 1390;
    public static int BurrowInfestorTerranDown = 1394;
    public static int BurrowQueenDown = 1433;
    public static int BurrowInfestorDown = 1444;
    public static int BurrowUltraliskDown = 1512;
    public static int BurrowCreepTumorDown = 1662;
    public static int BurrowRavagerDown = 2340;
    public static int BurrowLurkerDown = 2754;
    public static int BurrowDown = 3661;
    public static int WidowMineBurrow = 2095;

    public static List<int> AllBurrowActions = new List<int>()
    {
        BurrowBanelingDown,
        BurrowDroneDown,
        BurrowHydraliskDown,
        BurrowRoachDown,
        BurrowZerglingDown,
        BurrowInfestorTerranDown,
        BurrowQueenDown,
        BurrowInfestorDown,
        BurrowUltraliskDown,
        BurrowCreepTumorDown,
        BurrowRavagerDown,
        BurrowLurkerDown,
        BurrowDown,
        WidowMineBurrow,
    };


    public static int GetID(uint unit)
    {
        return (int)Controller.gameData.Units[(int)unit].AbilityId;
    }
}