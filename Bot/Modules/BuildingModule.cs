using System.Numerics;
using Bot.BuildOrders;
using Bot.Queries;
using SC2APIProtocol;

namespace Bot;

public class BuildingModule
{
    public async Task OnFrame()
    {
        if ( BuildOrderQueries.IsBuildOrderCompleted() 
             || BuildOrderQueries.GetNextStep() is WaitStep
             || BuildOrderQueries.IsBuildOrderStuck())
        {
            await BuildSupplyDepots();
        }

        // TODO MC Not sure if here, but repair damaged buildings 
        if (!BuildOrderQueries.IsBuildOrderCompleted() && !BuildOrderQueries.IsBuildOrderStuck())
        {
            await AdvanceBuildOrder();
        }
        else
        {
            await BuildRefineries();

            await BuildResearch();

            await BuildUnitProducers();

            if (IsTimeForExpandQuery.Get())
            {
                await BuildExpansion();
            }

            UpgradeCommandCenter();


            BuildBuildingAddons(Units.BARRACKS, new HashSet<uint>
            {
                Units.BARRACKS_TECHLAB,
                Units.BARRACKS_REACTOR
            });

            BuildBuildingAddons(Units.FACTORY, new HashSet<uint>
            {
                Units.FACTORY_TECHLAB,
                Units.FACTORY_REACTOR
            });

            BuildBuildingAddons(Units.STARPORT, new HashSet<uint>
            {
                Units.STARPORT_TECHLAB,
                Units.STARPORT_REACTOR
            });
        }

        // Leave this meanwhile ithere is a better strategy to place buildiings
        var depots = Controller.GetUnits(Units.SUPPLY_DEPOT);
        foreach (var depot in depots)
        {
            depot.Ability(Abilities.DEPOT_LOWER);
        }
    }


    private async Task AdvanceBuildOrder()
    {
        var nextBuildOrderResult = BuildOrderQueries.GetNextStep();

        if (nextBuildOrderResult is BuildingStep buildingStep)
        {
            // if (!nextBuildOrderResult.HasValue)
            // {
            //     Logger.Error("u should have value here");
            //     return;
            // }

            var nextUnit = buildingStep.BuildingType;
            if (nextUnit == Units.ORBITAL_COMMAND)
            {
                if (Controller.GetUnits(Units.BARRACKS, onlyCompleted: true).Any())
                {
                    UpgradeCommandCenter();
                }
            }
            else if (nextUnit == Units.COMMAND_CENTER)
            {
                await BuildExpansion();
            }
            else if (nextUnit == Units.REFINERY)
            {
                var cc = Controller.GetResourceCenters().FirstOrDefault(cc => cc.BuildProgress >= 1);
                if (cc == null)
                {
                    Logger.Warning(
                        "Trying to build refinery from build order but could not find CC. Are we loosing? :(");
                }
                else
                {
                    await BuildRefinery(cc.Position);
                }

                //await BuildRefineries();
            }
            else if (nextUnit == Units.BARRACKS_TECHLAB || nextUnit == Units.BARRACKS_REACTOR)
            {
                BuildBuildingAddons(Units.BARRACKS, new HashSet<uint> { nextUnit });
            }
            else if (nextUnit == Units.FACTORY_TECHLAB || nextUnit == Units.FACTORY_REACTOR)
            {
                BuildBuildingAddons(Units.FACTORY, new HashSet<uint> { nextUnit });
            }
            else if (nextUnit == Units.STARPORT_TECHLAB || nextUnit == Units.STARPORT_REACTOR)
            {
                BuildBuildingAddons(Units.STARPORT, new HashSet<uint> { nextUnit });
            }
            else if (nextUnit == Units.BUNKER)
            {
                //asdfasdfsdfgdfglkj
                //BuildBuildingAddons(Units.STARPORT, new HashSet<uint> { nextUnit });

                if (Controller.GetPendingCount(Units.BUNKER) == 0)
                {
                    var start = new Vector3(Controller.StartingLocation.X + 3, Controller.StartingLocation.Y, 0);
                    var path = Controller.PathFinder.FindPath(start, Controller.EnemyLocations.First());
                    var position = path[(int)(path.Length * 0.2)];
                    await Controller.Construct(Units.BUNKER, position.ToVector3(), 0);
                }
            }
            else
            {
                await BuildIfPossible(nextUnit, allowParalelBuild: true);
            }
        }
    }

    private void UpgradeCommandCenter()
    {
        var ccs = Controller.GetUnits(Units.COMMAND_CENTER);
        foreach (var cc in ccs)
        {
            if (Controller.CanAfford(Units.ORBITAL_COMMAND))
            {
                cc.Ability(Abilities.CANCEL_LAST);
                cc.Train(Units.ORBITAL_COMMAND);
            }
        }
    }

    private async Task BuildResearch()
    {
        if (Controller.GetUnits(Units.ENGINEERING_BAY).Count == 0
            && Controller.GetUnits(Units.BARRACKS, onlyCompleted: true).Count > 0)
        {
            await BuildIfPossible(Units.ENGINEERING_BAY);
        }

        if (Controller.GetUnits(Units.ARMORY).Count == 0
            && Controller.GetUnits(Units.FACTORY, onlyCompleted: true).Count > 0)
        {
            await BuildIfPossible(Units.ARMORY);
        }
    }

    private async Task BuildExpansion()
    {
        if (Controller.CanAfford(Units.COMMAND_CENTER) && Controller.GetPendingCount(Units.COMMAND_CENTER, false) == 0)
        {
            var allMinerals = Controller.GetUnits(Units.MineralFields, Alliance.Neutral);
            var allOwnedMinerals = new List<Unit>();

            var ccs = Controller.GetUnits(Units.ResourceCenters);

            foreach (var cc in ccs)
            {
                allOwnedMinerals.AddRange(Controller.GetInRange(cc.Position, allMinerals, 10));
            }

            var freeMinerals = allMinerals.Except(allOwnedMinerals).ToList();

            var start = new Vector3(Controller.StartingLocation.X , Controller.StartingLocation.Y+ 3, 0);
            var path = Controller.PathFinder.FindPath(start, Controller.EnemyLocations.First());
            var approxExpansionPosition = path[(int)(path.Length * 0.1)].ToVector3();
            approxExpansionPosition.Z = Controller.StartingLocation.Z;
            
            var targetMineral = freeMinerals.OrderBy(
                fm => (fm.Position - approxExpansionPosition).LengthSquared()
            ).First();

            var mineralCluster = Controller.GetInRange(targetMineral.Position, allMinerals, 10).ToList();
            var gasGeyser = Controller.GetInRange(targetMineral.Position, Controller.GetGeysers(), 14).ToList();

            var avgX = mineralCluster.Concat(gasGeyser).Select(m => m.Position.X).Average();
            var avgY = mineralCluster.Concat(gasGeyser).Select(m => m.Position.Y).Average();
            var avgZ = mineralCluster.Concat(gasGeyser).Select(m => m.Position.Z).Average();

            // TODO MC probably not the method to call, we need something more specific for how to place a CC correctly
            await Controller.Construct(Units.COMMAND_CENTER, new Vector3(avgX, avgY, avgZ), 5);
        }
    }

    private void BuildBuildingAddons(uint building, HashSet<uint> allowedAddons)
    {
        var producers = Controller.GetUnits(building).ToList();
        var addons = Controller.GetUnits(allowedAddons).ToList();

        if (addons.Count < producers.Count)
        {
            foreach (var producer in producers)
            {
                if (producer.GetAddonType().HasValue)
                {
                    continue;
                }

                // TODO MC Do other types of addons some time ya know
                var extensionType = allowedAddons.First();

                if (Controller.CanConstruct(extensionType)
                    && !(producer.BuildProgress < 1)
                    && producer.Order.AbilityId == 0)
                {
                    producer.Train(extensionType);

                    return;
                }
            }
        }
    }

    private async Task BuildUnitProducers()
    {
        var nbRcs = Controller.GetUnits(Units.ResourceCenters).Sum(r => r.IdealWorkers);

        var barrackTargetCount = 1 * (nbRcs / 7);
        var factoryTargetCount = 1 * (nbRcs / 18);
        var starportTargetCount = 1 * (nbRcs / 18);

        if (starportTargetCount > Controller.GetTotalCount(Units.STARPORT)
            && Controller.GetUnits(Units.FACTORY, onlyCompleted: true).Any())
        {
            await BuildIfPossible(Units.STARPORT);
        }

        if (factoryTargetCount > Controller.GetTotalCount(Units.FACTORY)
            && Controller.GetUnits(Units.BARRACKS, onlyCompleted: true).Any())
        {
            await BuildIfPossible(Units.FACTORY);
        }

        if (barrackTargetCount > Controller.GetTotalCount(Units.BARRACKS))
        {
            await BuildIfPossible(Units.BARRACKS);
        }
    }

    private async Task BuildSupplyDepots()
    {
        var position = Controller.GetUnits(Units.SupplyDepots).FirstOrDefault()?.Position;

        if (200 == Controller.CurrentSupply)
        {
            return;
        }

        //keep on buildings depots if supply is tight
        if (Controller.MaxSupply - Controller.CurrentSupply <= 8
            && Controller.GetPendingCount(Units.SupplyDepots) == 0)
        {
            await BuildIfPossible(Units.SUPPLY_DEPOT, startingSpot: position, radius: 5);
        }

        if (Controller.MaxSupply - Controller.CurrentSupply <= 3
            && Controller.GetPendingCount(Units.SupplyDepots) < 4)
        {
            await BuildIfPossible(Units.SUPPLY_DEPOT, allowParalelBuild: true, startingSpot: position, radius: 5);
        }
    }

    private async Task BuildRefineries()
    {
        var ccs = Controller.GetUnits(Units.ResourceCenters);
        var refineries = Controller.GetUnits(Units.REFINERY);

        foreach (var cc in ccs)
        {
            var refCount = refineries.Count(r => (r.Position - cc.Position).Length() < 8);
            if (cc.AssignedWorkers > 13 && refCount < 1)
            {
                await BuildRefinery(cc.Position);
            }
            else if (cc.AssignedWorkers > 15 && refCount < 2)
            {
                await BuildRefinery(cc.Position);
            }
        }
    }

    private async Task BuildIfPossible(
        uint unit, int radius = -1,
        bool allowParalelBuild = false,
        Vector3? startingSpot = null)
    {
        if (Controller.CanConstruct(unit))
        {
            if (!allowParalelBuild && Controller.GetPendingCount(unit, false) != 0)
            {
                return;
            }

            if (radius == -1)
            {
                await Controller.Construct(unit, startingSpot);
            }
            else
            {
                await Controller.Construct(unit, startingSpot, radius);
            }

        }
    }

    private async Task BuildRefinery(Vector3 basePosition)
    {
        if (Controller.GetPendingCount(Units.REFINERY, false) != 0)
        {
            return;
        }

        var geysers = Controller.GetUnits(Units.GasGeysers, Alliance.Neutral)
            .Where(r => (r.Position - basePosition).Length() < 12);

        foreach (var geyser in geysers)
        {
            if (await Controller.CanPlace(Units.REFINERY, geyser.Position))
            {
                Controller.ConstructGas(Units.REFINERY, geyser);
                return;
            }
        }
    }
}