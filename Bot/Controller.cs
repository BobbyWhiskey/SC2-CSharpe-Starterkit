using System.Numerics;
using SC2APIProtocol;
using Action = SC2APIProtocol.Action;

// ReSharper disable MemberCanBePrivate.Global

namespace Bot;

public static class Controller
{
    private const double FRAMES_PER_SECOND = 22.4;

    //editable
    private static readonly int frameDelay = 0; //too fast? increase this to e.g. 20

    //don't edit
    private static readonly List<Action> actions = new();
    private static readonly List<DebugCommand> debugCommands = new();
    private static readonly Random random = new();

    public static ResponseGameInfo gameInfo;
    public static ResponseData gameData;
    public static ResponseObservation obs;
    public static ulong frame;
    public static uint currentSupply;
    public static uint maxSupply;
    public static uint minerals;
    public static uint vespene;

    public static readonly List<Vector3> enemyLocations = new();
    public static Vector3 startingLocation;
    public static readonly List<string> chatLog = new();

    public static void Pause()
    {
        Console.WriteLine("Press any key to continue...");
        while (Console.ReadKey().Key != ConsoleKey.Enter)
        {
            //do nothing
        }
    }

    public static ulong SecsToFrames(int seconds)
    {
        return (ulong)(FRAMES_PER_SECOND * seconds);
    }


    public static (IEnumerable<Action>, IEnumerable<DebugCommand>) CloseFrame()
    {
        return (actions, debugCommands);
    }


    public static void OpenFrame()
    {
        if (gameInfo == null || gameData == null || obs == null)
        {
            if (gameInfo == null)
                Logger.Info("GameInfo is null! The application will terminate.");
            else if (gameData == null)
                Logger.Info("GameData is null! The application will terminate.");
            else
                Logger.Info("ResponseObservation is null! The application will terminate.");
            Pause();
            Environment.Exit(0);
        }

        actions.Clear();
        debugCommands.Clear();

        foreach (var chat in obs.Chat)
            chatLog.Add(chat.Message);

        frame = obs.Observation.GameLoop;
        currentSupply = obs.Observation.PlayerCommon.FoodUsed;
        maxSupply = obs.Observation.PlayerCommon.FoodCap;
        minerals = obs.Observation.PlayerCommon.Minerals;
        vespene = obs.Observation.PlayerCommon.Vespene;

        //initialization
        if (frame == 0)
        {
            var resourceCenters = GetUnits(Units.ResourceCenters);
            if (resourceCenters.Count > 0)
            {
                var rcPosition = resourceCenters[0].position;
                startingLocation = rcPosition;

                foreach (var startLocation in gameInfo.StartRaw.StartLocations)
                {
                    var enemyLocation = new Vector3(startLocation.X, startLocation.Y, 0);
                    var distance = Vector3.Distance(enemyLocation, rcPosition);
                    if (distance > 30)
                        enemyLocations.Add(enemyLocation);
                }
            }
        }

        if (frameDelay > 0)
            Thread.Sleep(frameDelay);
    }


    public static string GetUnitName(uint unitType)
    {
        return gameData.Units[(int)unitType].Name;
    }

    public static void AddAction(Action action)
    {
        actions.Add(action);
    }

    public static void AddDebugCommand(DebugCommand command)
    {
        debugCommands.Add(command);
    }

    public static void Chat(string message, bool team = false)
    {
        var actionChat = new ActionChat();
        actionChat.Channel = team ? ActionChat.Types.Channel.Team : ActionChat.Types.Channel.Broadcast;
        actionChat.Message = message;

        var action = new Action();
        action.ActionChat = actionChat;
        AddAction(action);
    }

    public static bool ConstructGas(uint buildingToConstruct, Unit geyser)
    {
        var worker = GetAvailableWorker(geyser.position);
        var abilityID = GetAbilityID(buildingToConstruct);
        var constructAction = CreateRawUnitCommand(abilityID);
        constructAction.ActionRaw.UnitCommand.UnitTags.Add(worker.tag);
        constructAction.ActionRaw.UnitCommand.TargetUnitTag = geyser.tag;
        AddAction(constructAction);

        Logger.Info("Constructing: {0} @ {1} / {2}", GetUnitName(buildingToConstruct), geyser.position.X,
            geyser.position.Y);
        return true;
    }

    private static int GetAbilityID(uint unit)
    {
        return (int)gameData.Units[(int)unit].AbilityId;
    }

    public static List<Unit> GetGeysers()
    {
        return GetUnits(Units.GasGeysers, Alliance.Neutral);
    }

    public static void Attack(List<Unit> units, Vector3 target)
    {
        var action = CreateRawUnitCommand(Abilities.ATTACK);
        action.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
        action.ActionRaw.UnitCommand.TargetWorldSpacePos.X = target.X;
        action.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = target.Y;
        foreach (var unit in units)
            action.ActionRaw.UnitCommand.UnitTags.Add(unit.tag);
        AddAction(action);
    }


    public static int GetTotalCount(uint unitType)
    {
        var pendingCount = GetPendingCount(unitType, false);
        var constructionCount = GetUnits(unitType).Count;
        return pendingCount + constructionCount;
    }

    public static int GetPendingCount(uint unitType, bool inConstruction = true)
    {
        var workers = GetUnits(Units.Workers);
        var abilityID = Abilities.GetID(unitType);

        var counter = 0;

        //count workers that have been sent to build this structure
        foreach (var worker in workers)
            if (worker.order.AbilityId == abilityID)
                counter += 1;

        //count buildings that are already in construction
        if (inConstruction)
            foreach (var unit in GetUnits(unitType))
                if (unit.buildProgress < 1)
                    counter += 1;

        return counter;
    }

    public static List<Unit> GetUnits(HashSet<uint> hashset, Alliance alliance = Alliance.Self,
        bool onlyCompleted = false, bool onlyVisible = false)
    {
        //ideally this should be cached in the future and cleared at each new frame
        var units = new List<Unit>();
        foreach (var unit in obs.Observation.RawData.Units)
            if (hashset.Contains(unit.UnitType) && unit.Alliance == alliance)
            {
                if (onlyCompleted && unit.BuildProgress < 1)
                    continue;

                if (onlyVisible && unit.DisplayType != DisplayType.Visible)
                    continue;

                units.Add(new Unit(unit));
            }

        return units;
    }

    public static List<Unit> GetUnits(uint unitType, Alliance alliance = Alliance.Self, bool onlyCompleted = false,
        bool onlyVisible = false)
    {
        //ideally this should be cached in the future and cleared at each new frame
        var units = new List<Unit>();
        foreach (var unit in obs.Observation.RawData.Units)
            if (unit.UnitType == unitType && unit.Alliance == alliance)
            {
                if (onlyCompleted && unit.BuildProgress < 1)
                    continue;

                if (onlyVisible && unit.DisplayType != DisplayType.Visible)
                    continue;

                units.Add(new Unit(unit));
            }

        return units;
    }


    public static bool CanAfford(uint unitType)
    {
        var unitData = gameData.Units[(int)unitType];
        return minerals >= unitData.MineralCost && vespene >= unitData.VespeneCost;
    }


    public static bool CanConstruct(uint unitType)
    {
        //is it a structure?
        if (Units.Structures.Contains(unitType))
        {
            //we need worker for every structure
            if (GetUnits(Units.Workers).Count == 0) return false;

            //we need an RC for any structure
            var resourceCenters = GetUnits(Units.ResourceCenters, onlyCompleted: true);
            if (resourceCenters.Count == 0) return false;

            if (unitType == Units.COMMAND_CENTER || unitType == Units.SUPPLY_DEPOT)
                return CanAfford(unitType);

            //we need supply depots for the following structures
            var depots = GetUnits(Units.SupplyDepots, onlyCompleted: true);
            if (depots.Count == 0) return false;
        }

        //it's an actual unit
        else
        {
            //do we have enough supply?
            var requiredSupply = gameData.Units[(int)unitType].FoodRequired;
            if (requiredSupply > maxSupply - currentSupply)
                return false;

            //do we construct the units from barracks? 
            if (Units.FromBarracks.Contains(unitType))
            {
                var barracks = GetUnits(Units.BARRACKS, onlyCompleted: true);
                if (barracks.Count == 0) return false;
            }
        }

        return CanAfford(unitType);
    }

    public static Action CreateRawUnitCommand(int ability)
    {
        var action = new Action();
        action.ActionRaw = new ActionRaw();
        action.ActionRaw.UnitCommand = new ActionRawUnitCommand();
        action.ActionRaw.UnitCommand.AbilityId = ability;
        return action;
    }


    public static bool CanPlace(uint unitType, Vector3 targetPos, bool withExtention = true)
    {
        //Note: this is a blocking call! Use it sparingly, or you will slow down your execution significantly!
        var abilityID = Abilities.GetID(unitType);

        var queryBuildingPlacement = new RequestQueryBuildingPlacement();
        queryBuildingPlacement.AbilityId = abilityID;
        queryBuildingPlacement.TargetPos = new Point2D();
        queryBuildingPlacement.TargetPos.X = targetPos.X;
        queryBuildingPlacement.TargetPos.Y = targetPos.Y;

        var requestQuery = new Request();
        requestQuery.Query = new RequestQuery();
        requestQuery.Query.Placements.Add(queryBuildingPlacement);

        var result = Program.gc.SendQuery(requestQuery.Query);
        if (result.Result.Placements.Count > 0)
            if (result.Result.Placements[0].Result == ActionResult.Success)
            {
                if (withExtention &&
                    (unitType == Units.BARRACKS || unitType == Units.FACTORY || unitType == Units.STARPORT))
                {
                    var extensionAbilityId = Abilities.GetID(Units.FACTORY_REACTOR);
                    queryBuildingPlacement = new RequestQueryBuildingPlacement();
                    queryBuildingPlacement.AbilityId = extensionAbilityId;
                    queryBuildingPlacement.TargetPos = new Point2D();
                    queryBuildingPlacement.TargetPos.X = (float)(targetPos.X + 1.5);
                    queryBuildingPlacement.TargetPos.Y = targetPos.Y;

                    requestQuery = new Request();
                    requestQuery.Query = new RequestQuery();
                    requestQuery.Query.Placements.Add(queryBuildingPlacement);

                    result = Program.gc.SendQuery(requestQuery.Query);

                    if (result.Result.Placements.Count > 0
                        && result.Result.Placements[0].Result == ActionResult.Success)
                        return true;
                    return false;
                }

                return true;
            }

        return false;
    }


    public static void DistributeWorkers()
    {
        var workers = GetUnits(Units.Workers);
        var idleWorkers = workers.FindAll(w => w.order.AbilityId == 0);

        if (idleWorkers.Count > 0)
        {
            var resourceCenters = GetUnits(Units.ResourceCenters, onlyCompleted: true);
            var mineralFields = GetUnits(Units.MineralFields, onlyVisible: true, alliance: Alliance.Neutral);

            foreach (var rc in resourceCenters)
            {
                //get one of the closer mineral fields
                var mf = GetFirstInRange(rc.position, mineralFields, 7);
                if (mf == null) continue;

                //only one at a time
                Logger.Info("Distributing idle worker: {0}", idleWorkers[0].tag);
                idleWorkers[0].Smart(mf);
                return;
            }

            //nothing to be done
            return;
        }
        else
        {
            //let's see if we can distribute between bases                
            var resourceCenters = GetUnits(Units.ResourceCenters, onlyCompleted: true);
            Unit? transferFrom = null;
            Unit? transferTo = null;

            transferFrom = GetUnits(Units.ResourceCenters)
                .FirstOrDefault(x => x.assignedWorkers > x.idealWorkers);

            transferTo = GetUnits(Units.ResourceCenters)
                .FirstOrDefault(x => x.assignedWorkers < x.idealWorkers);

            if (transferFrom != null && transferTo != null)
            {
                var mineralFields = GetUnits(Units.MineralFields, onlyVisible: true, alliance: Alliance.Neutral);

                var sqrDistance = 7 * 7;
                foreach (var worker in workers)
                {
                    if (worker.order.AbilityId != Abilities.GATHER_MINERALS) continue;
                    if (Vector3.DistanceSquared(worker.position, transferFrom.position) > sqrDistance) continue;

                    var mf = GetFirstInRange(transferTo.position, mineralFields, 10);
                    if (mf == null) continue;

                    //only one at a time
                    Logger.Info("Distributing idle worker: {0}", worker.tag);
                    worker.Smart(mf);
                    return;
                }
            }
        }

        // Fill up gas
        var refineries = GetUnits(Units.REFINERY);
        foreach (var refinery in refineries)
            if (refinery.assignedWorkers < refinery.idealWorkers)
            {
                var scv = GetFirstInRange(refinery.position, GetUnits(Units.SCV), 7);
                if (scv != null)
                    scv.Smart(refinery);
                else
                    Logger.Info("Cant find SCV for refinery :*(");
            }
    }


    public static Unit? GetAvailableWorker(Vector3 targetPosition)
    {
        // TODO MC Use method parameter
        var workers = GetUnits(Units.Workers).Where(w => w.order.AbilityId == Abilities.GATHER_MINERALS);
        return workers.FirstOrDefault();
    }

    public static bool IsInRange(Vector3 targetPosition, List<Unit> units, float maxDistance)
    {
        return GetFirstInRange(targetPosition, units, maxDistance) != null;
    }

    public static Unit? GetFirstInRange(Vector3 targetPosition, List<Unit> units, float maxDistance)
    {
        //squared distance is faster to calculate
        var maxDistanceSqr = maxDistance * maxDistance;
        foreach (var unit in units)
            if (Vector3.DistanceSquared(targetPosition, unit.position) <= maxDistanceSqr)
                return unit;

        return null;
    }

    public static IEnumerable<Unit> GetInRange(Vector3 targetPosition, List<Unit> units, float maxDistance)
    {
        //squared distance is faster to calculate
        var maxDistanceSqr = maxDistance * maxDistance;
        foreach (var unit in units)
            if (Vector3.DistanceSquared(targetPosition, unit.position) <= maxDistanceSqr)
                yield return unit;
    }


    public static void ConstructAtPosition(uint unitType, Vector3 position)
    {
        var worker = GetAvailableWorker(position);
        if (worker == null)
        {
            Logger.Error("Unable to find worker to construct: {0}", GetUnitName(unitType));
            return;
        }

        var abilityID = Abilities.GetID(unitType);
        var constructAction = CreateRawUnitCommand(abilityID);
        constructAction.ActionRaw.UnitCommand.UnitTags.Add(worker.tag);
        constructAction.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
        constructAction.ActionRaw.UnitCommand.TargetWorldSpacePos.X = position.X;
        constructAction.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = position.Y;
        AddAction(constructAction);

        Logger.Info("Constructing: {0} @ {1} / {2}", GetUnitName(unitType), position.X, position.Y);
    }

    public static void Construct(uint unitType, Vector3? startingSpot = null, int radius = 17)
    {
        var resourceCenters = GetUnits(Units.ResourceCenters);
        if (startingSpot == null && resourceCenters.Count > 0) startingSpot = startingLocation;

        //Logger.Error("Unable to construct: {0}. No resource center was found.", GetUnitName(unitType));
        //return;
        //trying to find a valid construction spot
        var mineralFields = GetUnits(Units.MineralFields, onlyVisible: true, alliance: Alliance.Neutral);
        Vector3 constructionSpot;
        var nbRetry = 0;
        while (true)
        {
            constructionSpot = new Vector3(startingSpot.Value.X + random.Next(-radius, radius + 1),
                startingSpot.Value.Y + random.Next(-radius, radius + 1), startingSpot.Value.Z);

            if (nbRetry > 200)
            {
                AddDebugCommand(new DebugCommand
                {
                    Draw = new DebugDraw
                    {
                        Boxes =
                        {
                            new DebugBox
                            {
                                Max = (startingSpot.Value + new Vector3(radius, radius, 2))
                                    .ToPoint(),
                                Min = (startingSpot.Value - new Vector3(radius, radius, 0))
                                    .ToPoint()
                            }
                        }
                    }
                });

                // TODO This is just temporary so we don't have infinite loop. Fix this
                Logger.Warning("Could not find space for building " + unitType);
                break;
            }

            //avoid building in the mineral line
            if (IsInRange(constructionSpot, mineralFields, 5)) continue;

            //check if the building fits
            if (!CanPlace(unitType, constructionSpot)) continue;

            //ok, we found a spot
            break;
        }

        ConstructAtPosition(unitType, constructionSpot);
    }

    public static List<Unit> GetResourceCenters()
    {
        return GetUnits(Units.ResourceCenters);
    }
}