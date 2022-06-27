namespace Bot.Queries;

public static class BuildOrderQueries
{
    public static ICollection<uint> buildOrder = new List<uint>
    {
        Units.SUPPLY_DEPOT,
        Units.BARRACKS,
        Units.REFINERY,
// Scout
        Units.ORBITAL_COMMAND,
// Reaper scout/harrass
        Units.COMMAND_CENTER,
        Units.BARRACKS,
        Units.BARRACKS_REACTOR,
        Units.SUPPLY_DEPOT,
        Units.REFINERY,
        Units.FACTORY,
        Units.BARRACKS_TECHLAB,
// Research stim
        Units.SUPPLY_DEPOT,// Added by myself
        Units.STARPORT,
        Units.FACTORY_TECHLAB, // Should be reactor now to switch with starport but not implemented yet
    };

    public static bool IsBuildOrderCompleted()
    {
        var groups = buildOrder.GroupBy(x => x);
        foreach (var buildOrderGrouping in groups)
        {
            if (Controller.GetUnits(buildOrderGrouping.First()).Count < buildOrderGrouping.Count())
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
        
        foreach (var u in buildOrder)
        {
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