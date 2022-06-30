namespace Bot.Queries;

public static class IsTimeForExpandQuery
{
    public static bool Get()
    {
        var nextBuildOrder = BuildOrderQueries.GetNextBuildOrderUnit();
        if (nextBuildOrder.HasValue && nextBuildOrder == Units.COMMAND_CENTER)
        {
            return true;
        }
        
        var rcs = Controller.GetUnits(Units.ResourceCenters);
        var scvCount = Controller.GetUnits(Units.SCV).Count();
        var idealWorkerTotal = rcs.Sum(rc => rc.idealWorkers);

        return rcs.All(rc => rc.assignedWorkers >= rc.idealWorkers)
               && !rcs.Any(rc => rc.buildProgress < 1)
               && idealWorkerTotal < 35;
    }
}