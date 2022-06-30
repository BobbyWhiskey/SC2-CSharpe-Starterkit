using Bot.BuildOrders;

namespace Bot.Queries;

public static class BuildOrderQueries
{
    //public static ICollection<BuildOrderDefinition.IBuildStep> buildOrder 

    public static BuildOrderDefinition currentBuild = new MarineMedivacIntoTankBuild();
    //public static BuildOrderDefinition currentBuild = new MassMarineRush();
    
    public static bool IsBuildOrderCompleted()
    {
        var groups = currentBuild.buildOrder.OfType<BuildingStep>().GroupBy(x => ((BuildingStep)x).BuildingType);
        foreach (var buildOrderGrouping in groups)
        {
            HashSet<uint> unitTypes = new HashSet<uint> { buildOrderGrouping.First().BuildingType };

            // TODO Those 2 ifs are duplicated in GetNextBuildOrderUnit, fix this
            if (unitTypes.First() == Units.COMMAND_CENTER)
            {
                unitTypes = Units.ResourceCenters;
            }
            if (unitTypes.First() == Units.SUPPLY_DEPOT)
            {
                unitTypes = Units.SupplyDepots;
            }
            
            // TODO MC Update this with ResearchStep also
            if (Controller.GetUnits(unitTypes).Count < buildOrderGrouping.Count())
            {
                return false;
            }
        }

        var lastBuildOrder = GetNextStep();
        if (lastBuildOrder is WaitStep waitStep)
        {
            if (Controller.frame < (ulong)(waitStep.StartedFrame + waitStep.Delay))
            {
                return false;
            }
        }

        return true;
    }
    
    public static IBuildStep? GetNextStep()
    {
        var countDic = new Dictionary<uint, int>();
        countDic[Units.COMMAND_CENTER] = 1; // We already start with a command center, so its not included in the build order
        
        foreach (var step in currentBuild.buildOrder)
        {
            // TODO add with UpgradeStep
            if (step is WaitStep waitStep)
            {
                if (waitStep.StartedFrame == 0)
                {
                    waitStep.StartedFrame = Controller.frame;
                }
                if (Controller.frame < waitStep.Delay + waitStep.StartedFrame)
                {
                    return step;
                }
            }
            if (step is BuildingStep buildingStep)
            {
                var u = ((BuildingStep)step).BuildingType;
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
                    return step;
                }
            }
            
        }

        return null;
    }

}