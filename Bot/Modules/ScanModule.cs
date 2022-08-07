using System.Numerics;
using SC2APIProtocol;

namespace Bot.Modules;

public class ScanModule
{
    private readonly ulong invisibleScanDelay = (ulong)(Controller.FRAMES_PER_SECOND * 10);
    private readonly Dictionary<Vector3, ulong> lastClusterScan = new();

    private ulong lastInvisibleScan;
    private ICollection<Vector3> mineralClusters { get; } = new List<Vector3>();

    public void OnFrame()
    {
        // Initialization
        if (!mineralClusters.Any())
        {
            var allMinerals = Controller.GetUnits(Units.MineralFields, Alliance.Neutral);
            var processedMinerals = new List<Unit>();
            while (true)
            {
                var mineral = allMinerals.Except(processedMinerals).FirstOrDefault();
                if (mineral == null)
                {
                    break;
                }
                var cluster = Controller.GetInRange(mineral.Position, allMinerals, 14).ToList();
                var clusterPosition = cluster.First().Position;
                mineralClusters.Add(clusterPosition);
                lastClusterScan.Add(clusterPosition, 0);
                processedMinerals.AddRange(cluster);
            }
        }

        var ocs = Controller.GetUnits(Units.ORBITAL_COMMAND);

        if (lastInvisibleScan + invisibleScanDelay < Controller.Frame)
        {
            var invisibleOrBorrowedUnits = Controller.Obs.Observation.RawData.Units.Where(x =>
                x.UnitType == Units.ROACH_BURROWED
                || x.DisplayType == DisplayType.Hidden ).Select(x => new Unit(x))
                .ToList();
            
            if (Controller.Obs.Observation.RawData.Units.Any(x =>
                    x.UnitType == Units.ROACH_BURROWED))
            {
                Logger.Info("Units.ROACH_BURROWED detected!!");
            }
            
            if (invisibleOrBorrowedUnits.Any())
            {
                var unitToScan = invisibleOrBorrowedUnits.First();
                if (Controller.GetFirstInRange(unitToScan.Position, Controller.GetUnits(Units.ArmyUnits), 15) != null)
                {
                    var cc = ocs.FirstOrDefault(x => x.Energy > 50);
                    if (cc != null)
                    {
                        // TODO Check we got units or building close by before scanning?
                        cc.Ability(Abilities.SCANNER_SWEEP, unitToScan.Position);
                        Logger.Info("Burrowed/cloacked unit scanned!!");
                        lastInvisibleScan = Controller.Frame;
                    }
                }
            }
        }

        if (Controller.Obs.Observation.RawData.Units.Any(x =>
                x.Cloak == CloakState.Cloaked))
        {
            Logger.Info("Observed Cloaked unit!!");
        }


        if (Controller.Obs.Observation.RawData.Units.Any(x =>
                x.IsBurrowed))
        {
            Logger.Info("Burrowed unit detected!!");
        }
        
        if (Controller.Obs.Observation.RawData.Units.Any(x =>
                x.UnitType == Units.ROACH_BURROWED))
        {
            Logger.Info("Units.ROACH_BURROWED detected!!");
        }

        if (Controller.Obs.Observation.RawData.Units.Any(x =>
                x.Cloak == CloakState.CloakedDetected))
        {
            Logger.Info("CloakedDetected unit!!");
        }

        if (Controller.Obs.Observation.RawData.Units.Any(x =>
                x.DisplayType == DisplayType.Hidden))
        {
            Logger.Info("DisplayType.Hidden unit!!");
        }
        // if (Controller.obs.Observation.RawData.Units.Any(x => x.UnitType == Units.OBSERVER))
        // {
        //     Logger.Info("Observer cloaked unit!!");
        // }
        //
        //

        // TODO Scanning invisibile units 
        // if (lastInvisibleScan + invisibleScanDelay < Controller.frame)
        // {
        //     var invisibleUnits = Controller.GetUnits(Units.All, Alliance.Enemy).Where(x => x.cloak == CloakState.Cloaked).ToList();
        //     var cc = ocs.FirstOrDefault(x => x.energy > 50);
        //     if (cc != null && invisibleUnits.Any())
        //     {
        //         // TODO Check we got units or building close by before scanning?
        //         cc.Ability(Abilities.SCANNER_SWEEP, invisibleUnits.First());
        //         lastInvisibleScan = Controller.frame;
        //     }
        // }

        // Scouting all extensions
        foreach (var unit in ocs)
        {
            if (unit.Energy > 110)
            {
                var orderedClusters = lastClusterScan.ToList().OrderBy(x => x.Value).ToList();
                var targetScan = orderedClusters.First();

                // For all equals, find closest to enemy base:
                targetScan = orderedClusters.Where(x => x.Value == targetScan.Value).MinBy(x => (x.Key - Controller.EnemyLocations.First()).LengthSquared());

                lastClusterScan[targetScan.Key] = Controller.Frame;

                unit.Ability(Abilities.SCANNER_SWEEP, targetScan.Key);
            }
        }
    }
}