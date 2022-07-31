using Bot.BuildOrders;
using Bot.Micro;
using Bot.Modules;
using Bot.Queries;
using SC2APIProtocol;
using Action = SC2APIProtocol.Action;

namespace Bot;

internal class RaxBot : Bot
{
    private readonly AntiChangelingModule _antiChangelingModule = new();
    private readonly ArmyMovementModule _armyMovementModule = new();
    private readonly ArmyModuleV2 _armyMovementModule2 = new();

    private readonly IList<IUnitMicro> _unitMicros = new List<IUnitMicro>()
    {
        new MarauderMicro(),
        new MarineMicro(),
        new MedicavMicro(),
        new TankMicro(),
        new VikingsMicro(),
        new RavenMicro(),
    };
        
    private readonly BuildingModule _buildingModule = new();
    private readonly CatFactModule _catFactModule = new();

    private readonly MuleModule _muleModule = new();
    private readonly ResearchModule _researchModule = new();

    private readonly ScanModule _scanModule = new();
    private readonly BunkerModule _bunkerModule = new();
    private readonly SpawnerModule _spawnerModule = new();
    private readonly ScoutingModule _scoutingModule = new();
    private readonly RepairUnitModule _repairUnitModule = new();
    private readonly ScvDefenseModule _scvDefenseModule = new();


    //the following will be called every frame
    //you can increase the amount of frames that get processed for each step at once in Wrapper/GameConnection.cs: stepSize  
    public async Task<(IEnumerable<Action>, IEnumerable<DebugCommand>)> OnFrame()
    {
        Controller.OpenFrame();

        if (Controller.Frame == 0)
        {
            Logger.Info("RaxBot");
            Logger.Info("--------------------------------------");
            Logger.Info("Map: {0}", Controller.GameInfo.MapName);
            Logger.Info("--------------------------------------");
        }

        var structures = Controller.GetUnits(Units.Structures);
        if (structures.Count == 1)
            //last building                
        {
            if (structures[0].Integrity < 0.4) //being attacked or burning down                 
            {
                if (!Controller.ChatLog.Contains("gg"))
                {
                    Controller.Chat("gg");
                }
            }
        }

        // TODO This should be moved into spawner?
        var resourceCenters = Controller.GetUnits(Units.ResourceCenters);
        var nextBuildOrder = BuildOrderQueries.GetNextStep() as BuildingStep;
        var canBuildOrbital = Controller.GetUnits(Units.BARRACKS, onlyCompleted: true).Any();
        var stopScvProduction = nextBuildOrder != null && nextBuildOrder.BuildingType == Units.ORBITAL_COMMAND && canBuildOrbital;

        //var totalAssign
        var totalAssigned = resourceCenters.Sum(rc => rc.AssignedWorkers);
        var totalIdeal = resourceCenters.Sum(rc => rc.IdealWorkers);

        if (totalIdeal > totalAssigned && Controller.CanConstruct(Units.SCV) && !stopScvProduction)
        {
            foreach (var rc in resourceCenters)
            {
                rc.Train(Units.SCV);
            }
        }

        //if (Controller.Frame % 5 == 0)
        {
            await _buildingModule.OnFrame();
        }

        //_armyMovementModule.OnFrame();
        _armyMovementModule2.OnFrame();

        _bunkerModule.OnFrame();
        _scanModule.OnFrame();
        _scoutingModule.OnFrame();
        _repairUnitModule.OnFrame();
        _scvDefenseModule.OnFrame();

        if (Controller.Frame % 10 == 0)
        {
            Controller.DistributeWorkers();
            _researchModule.OnFrame();
            _spawnerModule.OnFrame();
            _muleModule.OnFrame();

            _antiChangelingModule.OnFrame();
        }

        if (Controller.Frame % 4 == 0)
        {
            foreach (var unitMicro in _unitMicros)
            {
                unitMicro.OnFrame();
            }
        }

        _catFactModule.OnFrame();

        return Controller.CloseFrame();
    }
}