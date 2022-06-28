using System.Numerics;
using Bot.Queries;
using SC2APIProtocol;

namespace Bot;

public class BuildingModule
{
    public async Task OnFrame()
    {
        // TODO MC Not sure if here, but repair damaged buildings 
        if (!BuildOrderQueries.IsBuildOrderCompleted())
        {
            await AdvanceBuildOrder(); 
        }
        else
        {
            await BuildRefineries();

            await BuildSupplyDepots();

            await BuildResearch();

            await BuildUnitProducers();

            if (IsTimeForExpandQuery.Get())
            {
                await BuildExpansion();
            }

            UpgradeCommandCenter();


            BuildBuildingExtensions(Units.BARRACKS, new HashSet<uint>
            {
                Units.BARRACKS_TECHLAB,
                Units.BARRACKS_REACTOR
            });

            BuildBuildingExtensions(Units.FACTORY, new HashSet<uint>
            {
                Units.FACTORY_TECHLAB,
                Units.FACTORY_REACTOR
            });

            BuildBuildingExtensions(Units.STARPORT, new HashSet<uint>
            {
                Units.STARPORT_REACTOR,
                Units.STARPORT_TECHLAB
            });
        }
    }


    private async Task AdvanceBuildOrder()
    {
        var nextBuildOrderResult = BuildOrderQueries.GetNextBuildOrderUnit();
        if (!nextBuildOrderResult.HasValue)
        {
            Logger.Error("u should have value here");
            return;
        }

        var nextUnit = nextBuildOrderResult.Value;
        if (nextUnit == Units.ORBITAL_COMMAND)
        {
            // TODO MC Maybe we can cancel SCV currently training to make orbital faster?
            UpgradeCommandCenter();
        }
        else if (nextUnit == Units.COMMAND_CENTER)
        {
            await BuildExpansion();
        }
        else if (nextUnit == Units.REFINERY)
        {
            var cc = Controller.GetResourceCenters().FirstOrDefault(cc => cc.buildProgress >= 1);
            if (cc == null)
            {
                Logger.Warning("Trying to build refinery from build order but could not find CC. Are we loosing? :(");                
            }
            else
            {
                await BuildRefinery(cc.position);    
            }
            
            //await BuildRefineries();
        }
        else if (nextUnit == Units.BARRACKS_TECHLAB || nextUnit == Units.BARRACKS_REACTOR)
        {
            BuildBuildingExtensions(Units.BARRACKS, new HashSet<uint> { nextUnit });
        }
        else if (nextUnit == Units.FACTORY_TECHLAB || nextUnit == Units.FACTORY_REACTOR)
        {
            BuildBuildingExtensions(Units.FACTORY, new HashSet<uint> { nextUnit });
        }
        else if (nextUnit == Units.STARPORT_TECHLAB || nextUnit == Units.STARPORT_REACTOR)
        {
            BuildBuildingExtensions(Units.STARPORT, new HashSet<uint> { nextUnit });
        }
        else
        {
            await BuildIfPossible(nextUnit);
        }
    }

    private void UpgradeCommandCenter()
    {
        var ccs = Controller.GetUnits(Units.COMMAND_CENTER);
        foreach (var cc in ccs)
        {
            if (Controller.CanAfford(Units.ORBITAL_COMMAND))
            {
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
        if (Controller.CanAfford(Units.COMMAND_CENTER))
        {
            var allMinerals = Controller.GetUnits(Units.MineralFields, Alliance.Neutral);
            var allOwnedMinerals = new List<Unit>();

            var ccs = Controller.GetUnits(Units.ResourceCenters);

            foreach (var cc in ccs) allOwnedMinerals.AddRange(Controller.GetInRange(cc.position, allMinerals, 10));

            var freeMinerals = allMinerals.Except(allOwnedMinerals).ToList();

            var targetMineral = freeMinerals.OrderBy(
                fm => (fm.position - Controller.startingLocation).LengthSquared()
            ).First();

            var mineralCluster = Controller.GetInRange(targetMineral.position, allMinerals, 10).ToList();
            var gasGeyser = Controller.GetInRange(targetMineral.position, Controller.GetGeysers(), 14).ToList();

            var avgX = mineralCluster.Concat(gasGeyser).Select(m => m.position.X).Average();
            var avgY = mineralCluster.Concat(gasGeyser).Select(m => m.position.Y).Average();
            var avgZ = mineralCluster.Concat(gasGeyser).Select(m => m.position.Z).Average();

            // TODO MC probably not the method to call, we need something more specific for how to place a CC correctly
            await Controller.Construct(Units.COMMAND_CENTER, new Vector3(avgX, avgY, avgZ), 5);
        }
    }

    private void BuildBuildingExtensions(uint building, HashSet<uint> allowedExtensions)
    {
        var producers = Controller.GetUnits(building).ToList();
        var extensions = Controller.GetUnits(allowedExtensions).ToList();

        if (extensions.Count < producers.Count) 
            foreach (var producer in producers)
            {
                if (producer.GetAddonType().HasValue)
                {
                    continue;
                }
                
                var extensionType = allowedExtensions.First();
                // TODO MC Do also reactors some time ya know
                if (Controller.CanConstruct(extensionType)
                    && !(producer.buildProgress < 1)
                    && producer.order.AbilityId == 0)
                {
                    producer.Train(extensionType);

                    return;
                }
            }
    }

    private async Task BuildUnitProducers()
    {
        var nbRcs = Controller.GetUnits(Units.ResourceCenters).Sum(r => r.idealWorkers);

        var barrackTargetCount = 1 * (nbRcs / 10);
        var factoryTargetCount = 1 * (nbRcs / 20);
        var starportTargetCount = 1 * (nbRcs / 20);

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
        //keep on buildings depots if supply is tight
        if (Controller.maxSupply - Controller.currentSupply <= 8
            && Controller.GetPendingCount(Units.SUPPLY_DEPOT) == 0)
        {
            await BuildIfPossible(Units.SUPPLY_DEPOT);
        }
    }

    private async Task BuildRefineries()
    {
        var ccs = Controller.GetUnits(Units.COMMAND_CENTER);
        var refineries = Controller.GetUnits(Units.REFINERY);

        foreach (var cc in ccs)
        {
            var refCount = refineries.Count(r => (r.position - cc.position).Length() < 8);
            if (cc.assignedWorkers > 13 && refCount < 1)
            {
                await BuildRefinery(cc.position);
            }
            else if (cc.assignedWorkers > 15 && refCount < 2)
            {
                await BuildRefinery(cc.position);
            }
        }
    }

    private async Task BuildIfPossible(uint unit)
    {
        if (Controller.CanConstruct(unit))
        {
            await Controller.Construct(unit);
        }
    }

    private async Task BuildRefinery(Vector3 basePosition)
    {
        var geysers = Controller.GetUnits(Units.GasGeysers, Alliance.Neutral)
            .Where(r => (r.position - basePosition).Length() < 12);

        foreach (var geyser in geysers)
            if (await Controller.CanPlace(Units.REFINERY, geyser.position))
            {
                Controller.ConstructGas(Units.REFINERY, geyser);
                return;
            }
    }
}