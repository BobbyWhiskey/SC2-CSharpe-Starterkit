﻿namespace Bot.BuildOrders;

public class MassMarineRush : BuildOrderDefinition
{
    public MassMarineRush()
    {
        buildOrder = new List<IBuildStep>
        {
            new BuildingStep(Units.SUPPLY_DEPOT),
            new BuildingStep(Units.BARRACKS),
            new BuildingStep(Units.REFINERY),
            new BuildingStep(Units.BARRACKS),
            new BuildingStep(Units.ORBITAL_COMMAND),
            new BuildingStep(Units.BARRACKS_REACTOR),
            new BuildingStep(Units.BARRACKS_REACTOR),
            new WaitStep(60),
            new BuildingStep(Units.BARRACKS),
            new BuildingStep(Units.BARRACKS),
            new BuildingStep(Units.SUPPLY_DEPOT),
            new BuildingStep(Units.BARRACKS_TECHLAB),
            new BuildingStep(Units.REFINERY),
            new BuildingStep(Units.SUPPLY_DEPOT),
            new WaitStep(60),
            new BuildingStep(Units.SUPPLY_DEPOT),
            new BuildingStep(Units.FACTORY),
            new BuildingStep(Units.SUPPLY_DEPOT),
            new BuildingStep(Units.STARPORT),
            new BuildingStep(Units.STARPORT_REACTOR),
            new BuildingStep(Units.SUPPLY_DEPOT),
            new WaitStep(60),
            new BuildingStep(Units.BARRACKS_REACTOR),
            new BuildingStep(Units.BARRACKS_REACTOR),
            new BuildingStep(Units.SUPPLY_DEPOT),
   
            new BuildingStep(Units.SUPPLY_DEPOT),
            new BuildingStep(Units.BARRACKS),
            new BuildingStep(Units.FACTORY_TECHLAB),
            new WaitStep(150)
        };

        idealUnitRatio = new Dictionary<uint, double>
        {
            { Units.MARINE, 8 },
            { Units.MEDIVAC, 1 },
            { Units.HELLION, 2 }
        };
    }
}