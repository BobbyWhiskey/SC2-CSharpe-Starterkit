using Bot.Modules;
using Bot.Queries;
using SC2APIProtocol;
using Action = SC2APIProtocol.Action;

namespace Bot;

internal class RaxBot : Bot
{
    private readonly SpawnerModule _spawnerModule;
    private readonly ArmyMovementModule _armyMovementModule;

    private readonly BuildingModule _buildingModule;
    private readonly ResearchModule _researchModule;

    public RaxBot()
    {
        _buildingModule = new BuildingModule();
        _spawnerModule = new SpawnerModule();
        _researchModule = new ResearchModule();
        _armyMovementModule = new ArmyMovementModule();
    }


    //the following will be called every frame
    //you can increase the amount of frames that get processed for each step at once in Wrapper/GameConnection.cs: stepSize  
    public async Task<(IEnumerable<Action>, IEnumerable<DebugCommand>)> OnFrame()
    {
        Controller.OpenFrame();

        if (Controller.frame == 0)
        {
            Logger.Info("RaxBot");
            Logger.Info("--------------------------------------");
            Logger.Info("Map: {0}", Controller.gameInfo.MapName);
            Logger.Info("--------------------------------------");
        }

        if (Controller.frame == Controller.SecsToFrames(1))
            Controller.Chat("gl hf");

        var structures = Controller.GetUnits(Units.Structures);
        if (structures.Count == 1)
            //last building                
            if (structures[0].integrity < 0.4) //being attacked or burning down                 
                if (!Controller.chatLog.Contains("gg"))
                    Controller.Chat("gg");

        // TODO This should be moved into spawner?
        var resourceCenters = Controller.GetUnits(Units.ResourceCenters);
        var nextBuildOrder = BuildOrderQueries.GetNextBuildOrderUnit();
        var canBuildOrbital = Controller.GetUnits(Units.BARRACKS, onlyCompleted: true).Any();
        var stopScvProduction = nextBuildOrder.HasValue && nextBuildOrder.Value == Units.ORBITAL_COMMAND && canBuildOrbital;
        
        //var totalAssign
        var totalAssigned = resourceCenters.Sum(rc => rc.assignedWorkers);
        var totalIdeal = resourceCenters.Sum(rc => rc.idealWorkers);

        if (totalIdeal > totalAssigned  && Controller.CanConstruct(Units.SCV)  && !stopScvProduction)
        {
            foreach (var rc in resourceCenters)
            { 
                rc.Train(Units.SCV); 
            }
        }

        if (Controller.frame % 50 == 0)
        {
            await _buildingModule.OnFrame();
        }

        if (Controller.frame % 10 == 0)
        {
            Controller.DistributeWorkers();
            _researchModule.OnFrame();
            _spawnerModule.OnFrame();
            _armyMovementModule.OnFrame();
        }

        return Controller.CloseFrame();
    }
}