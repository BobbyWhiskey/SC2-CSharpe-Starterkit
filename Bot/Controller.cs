﻿using System.Numerics;
using AStar;
using AStar.Heuristics;
using AStar.Options;
using Bot.BuildOrders;
using Bot.Queries;
using SC2APIProtocol;
using Action = SC2APIProtocol.Action;

// ReSharper disable MemberCanBePrivate.Global

namespace Bot;

public static class Controller
{
    public const double FRAMES_PER_SECOND = 22.4;

    public static bool IsDebug = false;

    //editable
    private static readonly int frameDelay = 0; //too fast? increase this to e.g. 20
    
    // Reserved by any of the module/micro
    private static readonly HashSet<ulong> ReservedUnits = new();

    //don't edit
    private static readonly List<Action> Actions = new();
    private static readonly List<DebugCommand> DebugCommands = new();
    private static readonly Random Random = new();
    
    //public static readonly ICollection<Vector3> MineralClusters = new List<Vector3>();
    public static ResponseGameInfo GameInfo = null!;
    public static ResponseData GameData = null!;
    public static ResponseObservation Obs = null!;
    public static ulong Frame;
    public static uint CurrentSupply;
    public static uint MaxSupply;
    public static uint Minerals;
    public static uint Vespene;

    public static readonly List<Vector3> EnemyLocations = new();
    public static Vector3 StartingLocation;
    public static readonly List<string> ChatLog = new();

    // Debug data
    private static uint DebugNextUnitToTrain;

    //public static Astar AStarPathingGrid { get; set; }
    public static WorldGrid? WorldGrid { get; set; }
    public static IFindAPath? PathFinder { get; set; }

    public static bool[][]? PathingMap { get; set; }

    public static void Pause()
    {
        Console.WriteLine("Press any key to continue...");
        while (Console.ReadKey().Key != ConsoleKey.Enter)
        {
            //do nothing
        }
    }

    public static void ReserveUnit(ulong tag)
    {
        if (ReservedUnits.Contains(tag) && Controller.IsDebug)
        {
            throw new Exception("Unit already reseved duuuuh!");
        }

        ReservedUnits.Add(tag);
    }

    public static void ReleaseUnit(ulong tag)
    {
        if (!ReservedUnits.Contains(tag)&& Controller.IsDebug)
        {
            throw new Exception("Unit already released duuuuh!");
        }
        
        ReservedUnits.Remove(tag); 
    }

    public static void ExtractMap()
    {
        var canMoveMap = GameInfo.StartRaw.PathingGrid.Data.ToByteArray()
            .SelectMany(ByteToBools)
            .ToList();
        var canMoveLines = canMoveMap.Chunk(GameInfo.StartRaw.MapSize.X)
            // .Select(x => x.Reverse().ToArray())
            .ToArray();

        PathingMap = canMoveLines;

        var gridShort = new short[GameInfo.StartRaw.MapSize.X,GameInfo.StartRaw.MapSize.Y];

        var x = 0;

        foreach (var line in canMoveLines)
        {
            var y = 0;
            foreach (var value in line)
            {
                gridShort[y, x] = (short)(value ? 1 : 0);
                y++;
            }
            x++;
        }
        
        // Manually add mineral fields because they are not there by default
        foreach (var mineral in Controller.GetUnits(Units.MINERAL_FIELD_450, Alliance.Neutral))
        {
            gridShort[(int)(mineral.Position.X - 1), (int)mineral.Position.Y] = 0;
            gridShort[(int)(mineral.Position.X), (int)mineral.Position.Y] = 0;
        }
        
        foreach (var rock in Controller.GetUnits(365, Alliance.Neutral))
        {
            for (x = -3; x < 3; x++)
            {
                for (int y = -3; y < 3; y++)
                {
                    gridShort[(int)(rock.Position.X+x), (int)rock.Position.Y+y] = 0;
                }
            }
        }
        
        // Remove the command center in the grid
        for(x = (int)(Controller.StartingLocation.X - 3); x < Controller.StartingLocation.X + 3; x++)
        {
            for(int y = (int)(Controller.StartingLocation.Y - 3); y < Controller.StartingLocation.Y + 3; y++)
            {
                gridShort[x, y] = 1;
            }   
        }

        WorldGrid = new WorldGrid(gridShort);
        PathFinder = new CachedPathFinder(WorldGrid, 
            new PathFinderOptions()
            {
                SearchLimit = int.MaxValue,
                UseDiagonals = false,
                HeuristicFormula = HeuristicFormula.Custom1
            });
    }

    private static bool[] ByteToBools(byte value)
    {
        var values = new bool[8];

        values[0] = (value & 1) == 0 ? false : true;
        values[1] = (value & 2) == 0 ? false : true;
        values[2] = (value & 4) == 0 ? false : true;
        values[3] = (value & 8) == 0 ? false : true;
        values[4] = (value & 16) == 0 ? false : true;
        values[5] = (value & 32) == 0 ? false : true;
        values[6] = (value & 64) == 0 ? false : true;
        values[7] = (value & 128) == 0 ? false : true;

        return values.Reverse().ToArray();
    }

    public static ulong SecsToFrames(int seconds)
    {
        return (ulong)(FRAMES_PER_SECOND * seconds);
    }


    public static (IEnumerable<Action>, IEnumerable<DebugCommand>) CloseFrame()
    {
        return (Actions, DebugCommands);
    }


    public static void OpenFrame()
    {
        if (GameInfo == null || GameData == null || Obs == null)
        {
            if (GameInfo == null)
            {
                Logger.Info("GameInfo is null! The application will terminate.");
            }
            else if (GameData == null)
            {
                Logger.Info("GameData is null! The application will terminate.");
            }
            else
            {
                Logger.Info("ResponseObservation is null! The application will terminate.");
            }
            Pause();
            Environment.Exit(0);
        }



        Actions.Clear();
        DebugCommands.Clear();

        foreach (var chat in Obs.Chat)
        {
            ChatLog.Add(chat.Message);
        }

        Frame = Obs.Observation.GameLoop;
        CurrentSupply = Obs.Observation.PlayerCommon.FoodUsed;
        MaxSupply = Obs.Observation.PlayerCommon.FoodCap;
        Minerals = Obs.Observation.PlayerCommon.Minerals;
        Vespene = Obs.Observation.PlayerCommon.Vespene;

        //initialization
        if (Frame == 0)
        {
            var resourceCenters = GetUnits(Units.ResourceCenters);
            if (resourceCenters.Count > 0)
            {
                var rcPosition = resourceCenters[0].Position;
                StartingLocation = rcPosition;

                foreach (var startLocation in GameInfo.StartRaw.StartLocations)
                {
                    var enemyLocation = new Vector3(startLocation.X, startLocation.Y, 0);
                    var distance = Vector3.Distance(enemyLocation, rcPosition);
                    if (distance > 30)
                    {
                        EnemyLocations.Add(enemyLocation);
                    }
                }
            }
        }
        
        if (WorldGrid == null)
        {
            ExtractMap();
        }
        
        if (HeightMap == null)
        {
            InitHeightMap();
        }

        AddDebugDataOnScreen();

        if (frameDelay > 0)
        {
            Thread.Sleep(frameDelay);
        }
    }

    public static void SetDebugPriorityUnitToTrain(uint unit)
    {
        DebugNextUnitToTrain = unit;
    }

    private static void AddDebugDataOnScreen()
    {
        if (!IsDebug)
        {
            return;
        }

        //ShowPlayerPosisions();
        //ShowDebugAStarGrid();
        ShowDebugMineralLines();
        //ShowDebugNeutralUnits();
        ShowDebugAllUnitsList();

        var nextBuildStep = BuildOrderQueries.GetNextStep() as BuildingStep;
        var nextWaitOrder = BuildOrderQueries.GetNextStep() as WaitStep;
        var nextOrderStr = nextBuildStep != null ? GetUnitName(nextBuildStep.BuildingType) : "NA";
        if (nextWaitOrder != null)
        {
            nextOrderStr = "Waiting " + nextWaitOrder.Delay / FRAMES_PER_SECOND + " sec";
        }

        if (BuildOrderQueries.IsBuildOrderStuck())
        {
            nextOrderStr = "BUILD ORDER IS STUCK!! :(";
        }

        AddDebugCommand(new DebugCommand
        {
            Draw = new DebugDraw
            {
                Text =
                {
                    new[]
                    {
                        new DebugText
                        {
                            Text = "Next build order : " + nextOrderStr + "\n" +
                                   "Waiting for expand : " + IsTimeForExpandQuery.Get() + "\n" +
                                   "Next unit to train : " + GetUnitName(DebugNextUnitToTrain),
                            VirtualPos = new Point(),
                            Size = 12
                        }
                    }
                }
            }
        });
    }

    private static void ShowPlayerPosisions()
    {
        DrawText(new DebugText()
        {
            Color = new Color(),
            WorldPos = Controller.StartingLocation.ToPoint(),
            Text = "Starting position",
        });
        
        DrawText(new DebugText()
        {
            Color = new Color(),
            WorldPos = Controller.EnemyLocations.First().ToPoint(),
            Text = "Enemy position"
        });
    }

    private static void ShowDebugAllUnitsList()
    {
        if (!IsDebug)
        {
            return;
        }
        
        var allUnits = Controller.Obs.Observation.RawData.Units
            .Where(x => x.Alliance != Alliance.Self )
            .Select(x => new Unit(x))
            .GroupBy(x => x.UnitType)
            .Select(x => Controller.GetUnitName(x.Key))
            .OrderBy(x => x);
        
        AddDebugCommand(new DebugCommand
        {
            Draw = new DebugDraw
            {
                Text =
                {
                    new[]
                    {
                        new DebugText
                        {
                            Text = "All units but mine:\n-" + String.Join(",\n-", allUnits),
                            VirtualPos = new Point(){X = 0.80f, Y = 0.05f},
                            Size = 12
                        }
                    }
                }
            }
        });
    }

    private static void ShowDebugNeutralUnits()
    {
        if (!IsDebug)
        {
            return;
        }
        
        //var neutrals = Controller.GetUnits(Units.ALL_UNITS_UINT, Alliance.Neutral);
        var neutrals = Controller.Obs.Observation.RawData.Units.Select(x => new Unit(x));

        foreach (var unit in neutrals)
        {
            var color = new Color();
            if (unit.Original.Alliance == Alliance.Self)
            {
                color = new Color(){ R = 1, G = 250, B = 1};
            }
            else if (unit.Original.Alliance == Alliance.Enemy)
            {
                color = new Color(){ R = 250, G = 1, B = 1};
            }
            
            Controller.DrawText(new DebugText()
            {
                Text = Controller.GetUnitName(unit.UnitType) + " " + unit.UnitType,
                Color = color,
                WorldPos = unit.Position.ToPoint(),
            });

            Controller.DrawSphere(new DebugSphere()
            {
                R = 1,
                Color = color,
                P = unit.Position.ToPoint(),
            });
        }
    }

    private static void DrawText(DebugText debugText)
    {
        AddDebugCommand(new DebugCommand
        {
            Draw = new DebugDraw
            {
                Text =
                {
                    debugText
                }
            }
        });
    }

    private static void ShowDebugMineralLines()
    {
        var debugTexts = new List<DebugText>();
        var ordered = MineralLinesQueries.GetLineralLinesInfo().OrderBy(x => x.WalkingDistanceToStartingLocation);
        var index = 1;
        
        foreach (var mineralCluster in ordered)
        {
            var color = new Color();
            if (mineralCluster.Owner == Alliance.Self)
            {
                color = new Color(){R = 1, G = 200, B = 1};
            }
            else if(mineralCluster.Owner == Alliance.Enemy)
            {
                color = new Color(){R = 250, G = 1, B = 1};
            }
            debugTexts.Add(new DebugText()
            {
                Text = "Mineral line " + index++,
                Color = color,
                Size = 16,
                WorldPos = mineralCluster.CenterPosition.ToPoint()
            });
        }
        
        AddDebugCommand(new DebugCommand()
        {
            Draw = new DebugDraw()
            {
                Text = { debugTexts }
            }
        });
    }

    private static void InitHeightMap() {
        HeightMap = new List<List<float>>();
        for (var x = 0; x < GameInfo.StartRaw.MapSize.X; x++) {
            HeightMap.Add(new List<float>(new float[GameInfo.StartRaw.MapSize.Y]));
        }

        var heightVector = Controller.GameInfo.StartRaw.TerrainHeight.Data
            .ToByteArray()
            .Select(ByteToFloat)
            .ToList();

        for (var x = 0; x < GameInfo.StartRaw.MapSize.X; x++) {
            for (var y = 0; y < GameInfo.StartRaw.MapSize.Y; y++) {
                HeightMap[x][y] = heightVector[y * GameInfo.StartRaw.MapSize.X + x]; // heightVector[4] is (4, 0)
            }
        }
    }
    
    private static float ByteToFloat(byte byteValue) {
        // Computed from 3 unit positions and 3 height map bytes
        // Seems to work fine
        return 0.125f * byteValue - 15.888f;
    }

    public static List<List<float>> HeightMap { get; set; }

    public static string? GetUnitName(uint unitType)
    {
        return GameData.Units[(int)unitType].Name;
    }

    public static void AddAction(Action action)
    {
        Actions.Add(action);
    }

    public static void AddDebugCommand(DebugCommand command)
    {
        if (!IsDebug)
        {
            return;
        }

        DebugCommands.Add(command);
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
        var worker = GetAvailableWorker(geyser.Position);
        if (worker == null)
        {
            return false;
        }

        var abilityID = GetAbilityID(buildingToConstruct);
        var constructAction = CreateRawUnitCommand(abilityID);
        constructAction.ActionRaw.UnitCommand.UnitTags.Add(worker.Tag);
        constructAction.ActionRaw.UnitCommand.TargetUnitTag = geyser.Tag;
        AddAction(constructAction);

        Logger.Info("Constructing: {0} @ {1} / {2}", GetUnitName(buildingToConstruct), geyser.Position.X,
            geyser.Position.Y);
        return true;
    }

    private static int GetAbilityID(uint unit)
    {
        return (int)GameData.Units[(int)unit].AbilityId;
    }

    public static List<Unit> GetGeysers()
    {
        return GetUnits(Units.GasGeysers, Alliance.Neutral);
    }

    public static void Attack(List<Unit> units, Vector3 target, bool queueAction = false)
    {
        var action = CreateRawUnitCommand(Abilities.ATTACK);
        action.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
        action.ActionRaw.UnitCommand.TargetWorldSpacePos.X = target.X;
        action.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = target.Y;
        action.ActionRaw.UnitCommand.QueueCommand = queueAction;
        foreach (var unit in units)
        {
            action.ActionRaw.UnitCommand.UnitTags.Add(unit.Tag);
        }
        AddAction(action);
    }
    
    public static void Move(List<Unit> units, Vector3 target, bool queueAction = false)
    {
        var action = CreateRawUnitCommand(Abilities.MOVE);
        action.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
        action.ActionRaw.UnitCommand.TargetWorldSpacePos.X = target.X;
        action.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = target.Y;
        action.ActionRaw.UnitCommand.QueueCommand = queueAction;
        foreach (var unit in units)
        {
            action.ActionRaw.UnitCommand.UnitTags.Add(unit.Tag);
        }
        AddAction(action);
    }


    public static int GetTotalCount(uint unitType)
    {
        var pendingCount = GetPendingCount(unitType, false);
        var constructionCount = GetUnits(unitType).Count;
        return pendingCount + constructionCount;
    }


    public static int GetPendingCount(HashSet<uint> unitTypes, bool inConstruction = true)
    {
        var total = 0;
        foreach (var unitType in unitTypes)
        {
            total += GetPendingCount(unitType);
        }

        return total;
    }

    public static int GetPendingCount(uint unitType, bool inConstruction = true)
    {
        var workers = GetUnits(Units.Workers);
        var abilityID = Abilities.GetID(unitType);

        var counter = 0;

        // TODO Fix this method. If we are constructing one building, it returns 2

        //count workers that have been sent to build this structure
        foreach (var worker in workers)
        {
            if (worker.Order.AbilityId == abilityID)
            {
                counter += 1;
            }
        }

        //count buildings that are already in construction
        if (inConstruction)
        {
            foreach (var unit in GetUnits(unitType))
            {
                if (unit.BuildProgress < 1)
                {
                    counter += 1;
                }
            }
        }

        return counter;
    }

    public static List<Unit> GetUnits(IEnumerable<uint> hashset, Alliance alliance = Alliance.Self,
        bool onlyCompleted = false, bool onlyVisible = false, bool includeReservedUnits = false)
    {
        var units = new List<Unit>();
        foreach (var unitType in hashset)
        {
            units.AddRange(GetUnits(unitType, alliance, onlyCompleted, onlyVisible, includeReservedUnits));
        }

        return units;
    }

    public static List<Unit> GetUnits(uint unitType, Alliance alliance = Alliance.Self, bool onlyCompleted = false,
        bool onlyVisible = false, bool includeReservedUnits = false)
    {
        //ideally this should be cached in the future and cleared at each new frame
        var units = new List<Unit>();
        foreach (var unit in Obs.Observation.RawData.Units)
        {
            if ((unitType == 0 || unit.UnitType == unitType) && unit.Alliance == alliance)
            {
                if (onlyCompleted && unit.BuildProgress < 1)
                {
                    continue;
                }

                if (onlyVisible && unit.DisplayType != DisplayType.Visible)
                {
                    continue;
                }

                if (!includeReservedUnits && ReservedUnits.Contains(unit.Tag))
                {
                    continue;
                }

                units.Add(new Unit(unit));
            }
        }

        return units;
    }

    public static bool CanUnitAttackAir(uint unitType)
    {
        var unitData = GameData.Units[(int)unitType];

        return unitData.Weapons.Any(x => x.Type == Weapon.Types.TargetType.Air || x.Type == Weapon.Types.TargetType.Any);
    }

    public static bool CanUnitAttackGround(uint unitType)
    {
        var unitData = GameData.Units[(int)unitType];

        return unitData.Weapons.Any(x => x.Type == Weapon.Types.TargetType.Ground || x.Type == Weapon.Types.TargetType.Any);
    }

    public static bool CanAfford(uint unitType)
    {
        var unitData = GameData.Units[(int)unitType];
        var baseCost = (uint)0;
        if (unitType == Units.ORBITAL_COMMAND)
        {
            baseCost = GameData.Units[(int)Units.COMMAND_CENTER].MineralCost;
        }

        return Minerals >= unitData.MineralCost - baseCost && Vespene >= unitData.VespeneCost;
    }

    public static uint GetProducerBuildingType(uint unitType)
    {
        if (Units.FromBarracks.Contains(unitType))
        {
            return Units.BARRACKS;
        }

        if (Units.FromFactory.Contains(unitType))
        {
            return Units.FACTORY;
        }

        if (Units.FromStarport.Contains(unitType))
        {
            return Units.STARPORT;
        }

        Logger.Error("GetProducerBuildingType cannot find producer building");
        return 0;
    }


    public static bool CanConstruct(uint unitType)
    {
        //is it a structure?
        if (Units.Structures.Contains(unitType))
        {
            //we need worker for every structure
            if (GetUnits(Units.Workers).Count == 0)
            {
                return false;
            }

            //we need an RC for any structure
            var resourceCenters = GetUnits(Units.ResourceCenters, onlyCompleted: true);
            if (resourceCenters.Count == 0)
            {
                return false;
            }

            if (unitType == Units.COMMAND_CENTER || unitType == Units.SUPPLY_DEPOT)
            {
                return CanAfford(unitType);
            }

            //we need supply depots for the following structures
            var depots = GetUnits(Units.SupplyDepots, onlyCompleted: true);
            if (depots.Count == 0)
            {
                return false;
            }
        }

        //it's an actual unit
        else
        {
            //do we have enough supply?
            var requiredSupply = GameData.Units[(int)unitType].FoodRequired;
            if (requiredSupply > MaxSupply - CurrentSupply)
            {
                return false;
            }

            //do we construct the units from barracks? 
            if (Units.FromBarracks.Contains(unitType))
            {
                var barracks = GetUnits(Units.BARRACKS, onlyCompleted: true);
                if (barracks.Count == 0)
                {
                    return false;
                }
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


    public static async Task<bool> CanPlace(uint unitType, Vector3 targetPos, bool withExtention = true)
    {
        var abilityID = Abilities.GetID(unitType);

        var queryBuildingPlacement = new RequestQueryBuildingPlacement();
        queryBuildingPlacement.AbilityId = abilityID;
        queryBuildingPlacement.TargetPos = new Point2D();
        queryBuildingPlacement.TargetPos.X = targetPos.X;
        queryBuildingPlacement.TargetPos.Y = targetPos.Y;

        var requestQuery = new Request();
        requestQuery.Query = new RequestQuery();
        requestQuery.Query.Placements.Add(queryBuildingPlacement);

        if (GetFirstInRange(targetPos, GetUnits(Units.BuildingsWithAddons), 2) != null)
        {
            return false;
        }

        //Note: this is a blocking call! Use it sparingly, or you will slow down your execution significantly!
        var result = await GetQueryWithTimeout(requestQuery);
        if (result?.Placements.Count > 0)
        {
            if (result.Placements[0].Result == ActionResult.Success)
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

                    result = await GetQueryWithTimeout(requestQuery);

                    if (result?.Placements.Count > 0
                        && result?.Placements[0].Result == ActionResult.Success)
                    {
                        return true;
                    }
                    return false;
                }

                return true;
            }
        }

        return false;
    }

    private static async Task<ResponseQuery?> GetQueryWithTimeout(Request requestQuery)
    {
        var timeout = 500;
        var task = Program.gc.SendQuery(requestQuery.Query);
        if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
        {
            // task completed within timeout
            return task.Result;
        }
        // timeout logic
        Logger.Error("Query TIMEOUT!!!");
        return null;
    }


    public static void DistributeWorkers()
    {
        var workers = GetUnits(Units.Workers);
        var idleWorkers = workers.FindAll(w => w.Order.AbilityId == 0);

        if (idleWorkers.Count > 0)
        {
            var resourceCenters = GetUnits(Units.ResourceCenters, onlyCompleted: true);
            var mineralFields = GetUnits(Units.MineralFields, onlyVisible: true, alliance: Alliance.Neutral);

            foreach (var rc in resourceCenters)
            {
                //get one of the closer mineral fields
                var mf = GetFirstInRange(rc.Position, mineralFields, 7);
                if (mf == null)
                {
                    continue;
                }

                //only one at a time
                Logger.Info("Distributing idle worker: {0}", idleWorkers[0].Tag);
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
                .FirstOrDefault(x => x.AssignedWorkers > x.IdealWorkers);

            transferTo = GetUnits(Units.ResourceCenters)
                .FirstOrDefault(x => x.AssignedWorkers < x.IdealWorkers);

            if (transferFrom != null && transferTo != null)
            {
                var mineralFields = GetUnits(Units.MineralFields, onlyVisible: true, alliance: Alliance.Neutral);

                var sqrDistance = 7 * 7;
                foreach (var worker in workers)
                {
                    if (worker.Order.AbilityId != Abilities.GATHER_MINERALS)
                    {
                        continue;
                    }
                    if (Vector3.DistanceSquared(worker.Position, transferFrom.Position) > sqrDistance)
                    {
                        continue;
                    }

                    var mf = GetFirstInRange(transferTo.Position, mineralFields, 10);
                    if (mf == null)
                    {
                        continue;
                    }

                    //only one at a time
                    Logger.Info("Distributing idle worker: {0}", worker.Tag);
                    worker.Smart(mf);
                    return;
                }
            }
        }

        // Fill up gas
        var refineries = GetUnits(Units.REFINERY);
        var availableWorkers = GetUnits(Units.SCV).Where(u => u.Order.AbilityId != Abilities.RETURN_RESOURCES && u.Order.AbilityId != Abilities.REPAIR).ToList();
        foreach (var refinery in refineries)
        {
            if (refinery.AssignedWorkers < refinery.IdealWorkers)
            {
                var scv = GetFirstInRange(refinery.Position, availableWorkers, 7);
                if (scv != null)
                {
                    scv.Smart(refinery);
                }
                else
                {
                    Logger.Info("Cant find SCV for refinery :*(");
                }
            }
        }
    }


    public static Unit? GetAvailableWorker(Vector3 targetPosition)
    {
        var workers = GetUnits(Units.Workers).Where(w => w.Order.AbilityId == Abilities.GATHER_MINERALS).ToList();
        if (!workers.Any())
        {
            return null;
        }

        return workers.MinBy(x => (x.Position - targetPosition).LengthSquared());
    }

    public static bool IsInRange(Vector3 targetPosition, List<Unit> units, float maxDistance)
    {
        return GetFirstInRange(targetPosition, units, maxDistance) != null;
    }

    public static Unit? GetFirstInRange(Vector3 targetPosition, List<Unit> units, float maxDistance)
    {
        //squared distance is faster to calculate
        var adjustedTargetPosition = targetPosition with
        {
            Z = 0
        };
        var maxDistanceSqr = maxDistance * maxDistance;
        foreach (var unit in units)
        {
            var adjustedUnitPosition = unit.Position with
            {
                Z = 0
            };
            if (Vector3.DistanceSquared(adjustedUnitPosition, adjustedTargetPosition) <= maxDistanceSqr)
            {
                return unit;
            }

        }

        return null;
    }

    public static IEnumerable<Unit> GetInRange(Vector3 targetPosition, List<Unit> units, float maxDistance)
    {
        // TODO MC Should we ajuste for unit height here too?
        
        //squared distance is faster to calculate
        var maxDistanceSqr = maxDistance * maxDistance;
        foreach (var unit in units)
        {
            if (Vector3.DistanceSquared(targetPosition, unit.Position) <= maxDistanceSqr)
            {
                yield return unit;
            }
        }
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
        constructAction.ActionRaw.UnitCommand.UnitTags.Add(worker.Tag);
        constructAction.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
        constructAction.ActionRaw.UnitCommand.TargetWorldSpacePos.X = position.X;
        constructAction.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = position.Y;
        AddAction(constructAction);

        Logger.Info("Constructing: {0} @ {1} / {2}", GetUnitName(unitType), position.X, position.Y);
    }

    public static async Task Construct(uint unitType, Vector3? startingSpot = null, int radius = 17)
    {
        var resourceCenters = GetUnits(Units.ResourceCenters);
        if (startingSpot == null && resourceCenters.Count > 0)
        {
            startingSpot = StartingLocation;
        }

        //Logger.Error("Unable to construct: {0}. No resource center was found.", GetUnitName(unitType));
        //return;
        //trying to find a valid construction spot
        var mineralFields = GetUnits(Units.MineralFields, onlyVisible: true, alliance: Alliance.Neutral);
        Vector3 constructionSpot;
        var nbRetry = 0;
        while (true)
        {
            var adjustedRadius = radius + nbRetry / 200;

            constructionSpot = new Vector3(startingSpot!.Value.X + Random.Next(-adjustedRadius, adjustedRadius + 1),
                startingSpot.Value.Y + Random.Next(-adjustedRadius, adjustedRadius + 1), startingSpot.Value.Z);
            nbRetry++;

            
                AddDebugCommand(new DebugCommand
                {
                    Draw = new DebugDraw
                    {
                        Boxes =
                        {
                            new DebugBox
                            {
                                Max = (startingSpot.Value + new Vector3(radius, radius, 3))
                                    .ToPoint(),
                                Min = (startingSpot.Value - new Vector3(radius, radius, 1))
                                    .ToPoint()
                            }
                        }
                    }
                });
            if (nbRetry > 600)
            {
                // TODO This is just temporary so we don't have infinite loop. Fix this
                Logger.Warning("Could not find space for building " + Controller.GetUnitName(unitType));
                break;
            }

            //avoid building in the mineral line
            if (IsInRange(constructionSpot, mineralFields, 5))
            {
                continue;
            }

            //check if the building fits
            if (!await CanPlace(unitType, constructionSpot))
            {
                continue;
            }

            //ok, we found a spot
            break;
        }

        ConstructAtPosition(unitType, constructionSpot);
    }

    public static void DrawBox(DebugBox box)
    {
        AddDebugCommand(new DebugCommand
        {
            Draw = new DebugDraw
            {
                Boxes =
                {
                    box
                }
            }
        });
    }

    public static void DrawSphere(DebugSphere sphere)
    {
        AddDebugCommand(new DebugCommand
        {
            Draw = new DebugDraw
            {
                Spheres =
                {
                    sphere
                }
            }
        });
    }

    public static List<Unit> GetResourceCenters()
    {
        return GetUnits(Units.ResourceCenters);
    }

    public static bool CanBuildingTrainUnit(Unit building, uint unitType)
    {
        if (building.BuildProgress < 1)
        {
            return false;
        }

        if (Units.NeedsTechLab.Contains(unitType))
        {
            if (building.GetAddonType().HasValue
                && Units.TechLabs.Contains(building.GetAddonType()!.Value))
            {
                return true;
            }

            return false;
        }

        return true;
    }
    
    

    public static void ShowDebugPath(IEnumerable<System.Drawing.Point>? pathStack, Color? color = null, int elevation = 12)
    {
        if (!IsDebug || !Controller.HeightMap.Any())
        {
            return;
        }

        var debugBoxes = new List<DebugBox>();
        
        
        foreach (var node in pathStack!)
        {
            elevation = (int)(Controller.HeightMap[node.X][node.Y] + 1.2);

            debugBoxes.Add(new DebugBox
            {
                Min = new Point
                    { X = (float)(node.X + 0.55), Y = (float)(node.Y + 0.55), Z = 3 },
                Max = new Point
                    { X = (float)(node.X + 0.45), Y = (float)(node.Y + 0.45), Z = elevation },
                Color = color ?? new Color
                    { R = 1, B = 250, G = 1 }
            });
            
        }

        AddDebugCommand(new DebugCommand
        {
            Draw = new DebugDraw
            {
                Boxes = { debugBoxes }
            }
        });
    }

    public static void ShowDebugAStarGrid()
    {
        if (!IsDebug || !HeightMap.Any())
        {
            return;
        }

        var debugBoxes = new List<DebugBox>();

        for (int x = 0; x < GameInfo.StartRaw.MapSize.X; x++)
        {
            for (int y = 0; y < GameInfo.StartRaw.MapSize.Y; y++)
            {
                if (WorldGrid[x, y] == 0 &&
                    ( (x == 0 || y == 0 || x == GameInfo.StartRaw.MapSize.X-1 || y == GameInfo.StartRaw.MapSize.Y-1
                     ||( (WorldGrid[x + 1, y] != 0)
                         || WorldGrid[x, y+ 1] != 0
                         || WorldGrid[x - 1, y] != 0
                         || WorldGrid[x, y - 1] != 0))))
                {
                    debugBoxes.Add(new DebugBox
                    {
                        Min = new Point
                            { X = x + 1, Y = y + 1, Z = 3 },
                        Max = new Point
                            { X = x - 0, Y = y - 0, Z = (int)Controller.HeightMap[x][y] + 2 },
                        Color = new Color
                            { R = 250, B = 1, G = 200 }
                    });
                }
            }
        }
        //
        // foreach (var line in AStarPathingGrid.Grid)
        // {
        //     x = 0;
        //     foreach (var c in line)
        //     {
        //         if (!c.Walkable)
        //         {
        //             // debugTexts.Add(new DebugText()
        //             //     {
        //             //         Text = "NO!",
        //             //         Size = 12,
        //             //         WorldPos = new Point() { X = x, Y = y, Z = 12 }
        //             //     });
        //             //         
        //             //
        //             debugBoxes.Add(new DebugBox
        //             {
        //                 Min = new Point
        //                     { X = x + 1, Y = y + 1, Z = 3 },
        //                 Max = new Point
        //                     { X = x - 0, Y = y - 0, Z = 12 },
        //                 Color = new Color
        //                     { R = 250, B = 1, G = 1 }
        //             });
        //         }
        //         x++;
        //     }
        //     y++;
        // }

        AddDebugCommand(new DebugCommand
        {
            Draw = new DebugDraw
            {
                Boxes = { debugBoxes }
            }
        });
    }

    public static bool IsUnitReserved(ulong unitTag)
    {
        return ReservedUnits.Contains(unitTag);
    }
}