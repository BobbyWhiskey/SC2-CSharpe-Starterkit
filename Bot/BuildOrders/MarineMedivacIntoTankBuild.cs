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
            new BuildingStep(Units.COMMAND_CENTER),
            new BuildingStep(Units.ORBITAL_COMMAND),
            new BuildingStep(Units.BARRACKS),
            new BuildingStep(Units.BARRACKS_REACTOR),
            new BuildingStep(Units.SUPPLY_DEPOT),
            new BuildingStep(Units.REFINERY),
            new BuildingStep(Units.FACTORY),
            new BuildingStep(Units.BARRACKS_TECHLAB),
            new BuildingStep(Units.SUPPLY_DEPOT),
            new BuildingStep(Units.STARPORT),
            new BuildingStep(Units.FACTORY_TECHLAB),
            new BuildingStep(Units.REFINERY),
            new BuildingStep(Units.REFINERY),
            new WaitStep(150),
            new BuildingStep(Units.STARPORT_REACTOR),
        };

        idealUnitRatio = new Dictionary<uint, double>()
        {
            { Units.MARINE, 8 },
            { Units.MARAUDER, 2 },
            { Units.MEDIVAC, 1 },
            { Units.SIEGE_TANK, 1 },
        };
        
        idealUnitFixedNumber =  new Dictionary<uint, double>()
        {
            { Units.RAVEN, 1 },
        };
    }
}