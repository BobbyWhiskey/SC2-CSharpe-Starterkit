﻿using System.Numerics;
using Bot.Queries;
using SC2APIProtocol;

namespace Bot;

public class BuildingModule
{
    private int barrackTargetCount = 2;
    private int factoryTargetCount = 1;
    private int starportTargetCount = 1;

    public void OnFrame()
    {
        if (Controller.frame % 50 == 0) BuildRefineries();


        BuildSupplyDepots();

        if (Controller.frame % 20 == 0) BuildResearch();

        if (Controller.frame % 20 == 0) BuildUnitProducers();

        BuildExpansion();

        if (Controller.frame % 50 == 0)
        {
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

    private void BuildResearch()
    {
        if (!Controller.GetUnits(Units.ENGINEERING_BAY).Any()
            && Controller.GetUnits(Units.BARRACKS, onlyCompleted: true).Count > 0)
            BuildIfPossible(Units.ENGINEERING_BAY);

        if (Controller.GetUnits(Units.ARMORY).Count == 0
            && Controller.GetUnits(Units.FACTORY, onlyCompleted: true).Count > 0)
            BuildIfPossible(Units.ARMORY);
    }

    private void BuildExpansion()
    {
        if (IsTimeForExpandQuery.Get()
            && Controller.CanAfford(Units.COMMAND_CENTER))
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

            Controller.Construct(Units.COMMAND_CENTER, new Vector3(avgX, avgY, avgZ), 6);
        }
    }

    private void BuildBuildingExtensions(uint building, HashSet<uint> allowedExtensions)
    {
        var producers = Controller.GetUnits(building).ToList();
        var extensions = Controller.GetUnits(allowedExtensions).ToList();

        if (extensions.Count < producers.Count)
            foreach (var producer in producers)
            {
                var extensionType = allowedExtensions.First();
                // TODO MC Do also reactors some time ya know
                if (Controller.CanConstruct(extensionType)
                    && !(producer.buildProgress < 1)
                    && producer.order.AbilityId == 0)
                    producer.Train(extensionType);
            }
    }

    private void BuildUnitProducers()
    {
        var nbRcs = Controller.GetUnits(Units.ResourceCenters).Sum(r => r.idealWorkers);

        barrackTargetCount = 1 * (nbRcs / 8);
        factoryTargetCount = 1 * (nbRcs / 15);
        starportTargetCount = 1 * (nbRcs / 15);

        if (barrackTargetCount > Controller.GetTotalCount(Units.BARRACKS)) BuildIfPossible(Units.BARRACKS);

        if (factoryTargetCount > Controller.GetTotalCount(Units.FACTORY)
            && Controller.GetUnits(Units.BARRACKS, onlyCompleted: true).Any())
            BuildIfPossible(Units.FACTORY);

        if (starportTargetCount > Controller.GetTotalCount(Units.STARPORT)
            && Controller.GetUnits(Units.FACTORY, onlyCompleted: true).Any())
            BuildIfPossible(Units.STARPORT);
    }

    private void BuildSupplyDepots()
    {
        //keep on buildings depots if supply is tight
        if (Controller.maxSupply - Controller.currentSupply <= 8
            && Controller.GetPendingCount(Units.SUPPLY_DEPOT) == 0)
            BuildIfPossible(Units.SUPPLY_DEPOT);
    }

    private void BuildRefineries()
    {
        var ccs = Controller.GetUnits(Units.COMMAND_CENTER);
        var refineries = Controller.GetUnits(Units.REFINERY);

        foreach (var cc in ccs)
        {
            var refCount = refineries.Count(r => (r.position - cc.position).Length() < 8);
            if (cc.assignedWorkers > 13 && refCount < 1)
                BuildRefinery(cc.position);
            else if (cc.assignedWorkers > 15 && refCount < 2) BuildRefinery(cc.position);
        }
    }

    private void BuildIfPossible(uint unit)
    {
        if (Controller.CanConstruct(unit)) Controller.Construct(unit);
    }

    private void BuildRefinery(Vector3 basePosition)
    {
        var geysers = Controller.GetUnits(Units.GasGeysers, Alliance.Neutral)
            .Where(r => (r.position - basePosition).Length() < 12);

        foreach (var geyser in geysers)
            if (Controller.CanPlace(Units.REFINERY, geyser.position))
            {
                Controller.ConstructGas(Units.REFINERY, geyser);
                return;
            }
    }
}