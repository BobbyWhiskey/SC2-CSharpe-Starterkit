namespace Bot.BuildOrders;

public class MarineMedivacIntoTankBuild : BuildOrderDefinition
{
    public MarineMedivacIntoTankBuild()
    {
        buildOrder = new List<IBuildStep>
        {
            new BuildingStep(Units.SUPPLY_DEPOT),
            new BuildingStep(Units.BARRACKS),
            new BuildingStep(Units.REFINERY),
// TODO Scout
            new BuildingStep(Units.ORBITAL_COMMAND),
// TODO Reaper scout/harrass
            new BuildingStep(Units.COMMAND_CENTER),
            new BuildingStep(Units.BARRACKS),
            new BuildingStep(Units.BARRACKS_REACTOR),
            new BuildingStep(Units.SUPPLY_DEPOT),
            new BuildingStep(Units.REFINERY),
            new BuildingStep(Units.FACTORY),
            new BuildingStep(Units.BARRACKS_TECHLAB),
// TODO  Research stim
            new BuildingStep(Units.SUPPLY_DEPOT), // Added by myself
            new BuildingStep(Units.STARPORT),
            new BuildingStep(Units.FACTORY_TECHLAB), // TODO Should be reactor now to switch with starport but not implemented yet
        };

        idealUnitRatio = new Dictionary<uint, double>()
        {
            { Units.MARINE, 8 },
            { Units.MARAUDER, 2 },
            { Units.MEDIVAC, 1 },
            { Units.SIEGE_TANK, 1 },
        };
    }
}