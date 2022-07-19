using Bot.BuildOrders;

namespace Bot.Queries;

public static class IsTimeForExpandQuery
{
    public static bool Get()
    {
        var nextStep = BuildOrderQueries.GetNextStep();
        if (nextStep is WaitStep)
        {
            return false;
        }
        var nextBuildOrder = nextStep as BuildingStep;
        if (nextBuildOrder != null && nextBuildOrder.BuildingType == Units.COMMAND_CENTER)
        {
            return true;
        }

        var rcs = Controller.GetUnits(Units.ResourceCenters);
        var scvCount = Controller.GetUnits(Units.SCV).Count();
        var idealWorkerTotal = rcs.Sum(rc => rc.IdealWorkers);

        return rcs.All(rc => rc.AssignedWorkers >= rc.IdealWorkers)
               && !rcs.Any(rc => rc.BuildProgress < 1)
               && idealWorkerTotal < 35;
    }
}