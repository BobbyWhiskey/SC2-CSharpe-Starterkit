using Bot.BuildOrders;
using Bot.Micro;
using Bot.Modules;
using Bot.Queries;
using SC2APIProtocol;
using Action = SC2APIProtocol.Action;

namespace Bot;

internal class RaxBot : Bot
{
    private readonly SpawnerModule _spawnerModule = new();
    private readonly ArmyMovementModule _armyMovementModule = new();

    private readonly BuildingModule _buildingModule = new();
    private readonly ResearchModule _researchModule = new();
    private readonly MarineMicro _marineMicro = new();
    private readonly MarauderMicro _marauderMicro = new();
    private readonly TankMicro _tankMicro = new();
    private readonly MuleModule _muleModule = new();
    private readonly CatFactModule _catFactModule = new();
    private readonly AntiChangelingModule _antiChangelingModule = new();

    private readonly ScanModule _scanModule = new();

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

        var structures = Controller.GetUnits(Units.Structures);
        if (structures.Count == 1)
            //last building                
            if (structures[0].integrity < 0.4) //being attacked or burning down                 
                if (!Controller.chatLog.Contains("gg"))
                    Controller.Chat("gg");

        // TODO This should be moved into spawner?
        var resourceCenters = Controller.GetUnits(Units.ResourceCenters);
        var nextBuildOrder = BuildOrderQueries.GetNextStep() as BuildingStep;
        var canBuildOrbital = Controller.GetUnits(Units.BARRACKS, onlyCompleted: true).Any();
        var stopScvProduction = nextBuildOrder != null && nextBuildOrder.BuildingType == Units.ORBITAL_COMMAND && canBuildOrbital;
        
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

        if (Controller.frame % 5 == 0)
        {
            await _buildingModule.OnFrame();
        }

        if (Controller.frame % 10 == 0)
        {
            Controller.DistributeWorkers();
            _researchModule.OnFrame();
            _spawnerModule.OnFrame();
            _armyMovementModule.OnFrame();
            _scanModule.OnFrame();
            _antiChangelingModule.OnFrame();
        }

        if (Controller.frame % 5 == 0)
        {
            _marineMicro.OnFrame();
            _marauderMicro.OnFrame();
            _tankMicro.OnFrame();
            _muleModule.OnFrame();
        }

        _catFactModule.OnFrame();

        return Controller.CloseFrame();
    }
}