using Bot.BuildOrders;

namespace Bot.Queries;

public static class BuildOrderQueries
{
    //public static ICollection<BuildOrderDefinition.IBuildStep> buildOrder 

    public static BuildOrderDefinition currentBuild = new MarineMedivacIntoTankBuild();


// FAST MARINE RUSH
    // public static ICollection<IBuildStep> buildOrder = new List<IBuildStep>
    // {
    //     new BuildingStep(Units.SUPPLY_DEPOT),
    //     new BuildingStep(Units.BARRACKS),
    //     new BuildingStep(Units.REFINERY),
    //     new BuildingStep(Units.BARRACKS),
    //     new BuildingStep(Units.SUPPLY_DEPOT),
    //     new BuildingStep(Units.BARRACKS_REACTOR),
    //     new BuildingStep(Units.BARRACKS),
    //     new BuildingStep(Units.ENGINEERING_BAY),
    //     new BuildingStep(Units.SUPPLY_DEPOT),
    //     new BuildingStep(Units.REFINERY),
    //     new BuildingStep(Units.FACTORY),
    //     new BuildingStep(Units.BARRACKS_TECHLAB),
    //     new BuildingStep(Units.SUPPLY_DEPOT),// Added by myself
    //     new BuildingStep(Units.STARPORT),
    //     new BuildingStep(Units.FACTORY_TECHLAB), // Should be reactor now to switch with starport but not implemented yet
    // };
    
    public static bool IsBuildOrderCompleted()
    {
        var groups = currentBuild.buildOrder.GroupBy(x => ((BuildOrderDefinition.BuildingStep)x).BuildingType);
        foreach (var buildOrderGrouping in groups)
        {
            HashSet<uint> unitTypes = new HashSet<uint> { buildOrderGrouping.Cast<BuildOrderDefinition.BuildingStep>().First().BuildingType };

            if (unitTypes.First() == Units.COMMAND_CENTER)
            {
                unitTypes = Units.ResourceCenters;
            }
            
            // TODO MC Update this with ResearchStep also
            if (Controller.GetUnits(unitTypes).Count < buildOrderGrouping.Count())
            {
                return false;
            }
        }

        return true;
    }
    
    public static uint? GetNextBuildOrderUnit()
    {
        var countDic = new Dictionary<uint, int>();
        countDic[Units.COMMAND_CENTER] = 1; // We already start with a command center, so its not included in the build order
        
        foreach (var step in currentBuild.buildOrder)
        {
            // TODO Fix with UpgradeStep
            var u = ((BuildOrderDefinition.BuildingStep)step).BuildingType;
            if (!countDic.TryGetValue(u, out var targetCount))
            {
                targetCount = 1;
                countDic.Add(u, targetCount);
            }
            else
            {
                targetCount++;
                countDic[u] = targetCount;
            }

            HashSet<uint> unitsToCount = new HashSet<uint>() { u };
            if (u == Units.COMMAND_CENTER)
            {
                unitsToCount = Units.ResourceCenters;
            }
            if (u == Units.SUPPLY_DEPOT)
            {
                unitsToCount = Units.SupplyDepots;
            }
            
            // An upgrading orbital is still considered a command center so we need to do this check
            if (u == Units.ORBITAL_COMMAND 
                && Controller.GetUnits(Units.COMMAND_CENTER).Any(x => x.order.AbilityId == Abilities.COMMAND_CENTER_ORBITAL_UPGRADE))
            {
                continue;
            }
            
            // TODO MC Warning: This count does not include the unit that has been planned, but not yet started to construct. See if we have this info in the observer
            
            if (Controller.GetUnits(unitsToCount).Count < targetCount)
            {
                return u;
            }
        }

        return null;
    }

}