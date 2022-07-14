using System.Numerics;
using SC2APIProtocol;

namespace Bot.Modules;

public class ScanModule
{
    private ICollection<Vector3> mineralClusters { get; set; } = new List<Vector3>();
    private Dictionary<Vector3, ulong> lastClusterScan = new();

    private ulong lastInvisibleScan = 0;
    private ulong invisibleScanDelay = 24 * 15;

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
                var cluster = Controller.GetInRange(mineral.position, allMinerals,14).ToList();
                var clusterPosition = cluster.First().position;
                mineralClusters.Add(clusterPosition);
                lastClusterScan.Add(clusterPosition, 0);
                processedMinerals.AddRange(cluster);
            }
        }
        
        var ocs = Controller.GetUnits(Units.ORBITAL_COMMAND);
        
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
            if (unit.energy > 60)
            {
                var orderedClusters = lastClusterScan.ToList().OrderBy(x => x.Value).ToList();
                var targetScan = orderedClusters.First();

                // For all equals, find closest to enemy base:
                targetScan = orderedClusters.Where(x => x.Value == targetScan.Value).MinBy(x => (x.Key - Controller.enemyLocations.First()).LengthSquared());
                
                lastClusterScan[targetScan.Key] = Controller.frame;

                unit.Ability(Abilities.SCANNER_SWEEP, targetScan.Key);
            }
        }
    }
}