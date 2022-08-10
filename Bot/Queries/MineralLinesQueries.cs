using System.Numerics;
using SC2APIProtocol;

namespace Bot.Queries;

public static class MineralLinesQueries
{
        
    private static List<MineralOwnershipInfo>? _mineralLinesInfo;

    public static List<MineralOwnershipInfo> GetLineralLinesInfo()
    {
        if (_mineralLinesInfo == null)
        {
            var mineralClusters = new List<Vector3>();
            
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
                cluster.AddRange(Controller.GetInRange(mineral.Position, Controller.GetGeysers(), 14));
                
                var clusterPosition = new Vector3(cluster.Average(x => x.Position.X),cluster.Average(x => x.Position.Y), cluster.Average(x => x.Position.Z) );
                mineralClusters.Add(clusterPosition);
                processedMinerals.AddRange(cluster);
            }
            
            _mineralLinesInfo = new List<MineralOwnershipInfo>();
            var infos = mineralClusters.Select(CreateMineralOwnershipInfo);
            //
            // var infos = mineralClusters.Select(x => new MineralOwnershipInfo()
            // {
            //     CenterPosition = x,
            //     WalkingDistanceToStartingLocation =  Controller.PathFinder!.FindPath(Controller.StartingLocation with{ X= Controller.StartingLocation.X}, x).Length,
            //     WalkingDistanceToEnemyLocation =  Controller.PathFinder!.FindPath(Controller.EnemyLocations.First() with{ X= Controller.EnemyLocations.First().X}, x).Length
            // });

            foreach (var mineralOwnershipInfo in infos)
            {
                _mineralLinesInfo.Add(mineralOwnershipInfo);
            }
        }

        // Cache for one frame
        if (Controller.Frame != LastFrameUpdatedOwnership)
        {
            LastFrameUpdatedOwnership = Controller.Frame;
            
            // Update the ownership
            foreach (var mineralOwnershipInfo in _mineralLinesInfo.OrderBy(x => x.WalkingDistanceToStartingLocation))
            {
                var allAllyResourceCenters = Controller.GetUnits(Units.ResourceCenters);
                var allyBuildingInRange = Controller.GetFirstInRange(mineralOwnershipInfo.CenterPosition, allAllyResourceCenters, 15);
                var enemyBuildingInRange = Controller.GetFirstInRange(mineralOwnershipInfo.CenterPosition, Controller.GetUnits(Units.ResourceCenters, Alliance.Enemy), 15);
                if (allyBuildingInRange != null)
                {
                    mineralOwnershipInfo.Owner = Alliance.Self;
                }
                else if (enemyBuildingInRange != null)
                {
                    mineralOwnershipInfo.Owner = Alliance.Enemy;
                }
                else
                {
                    mineralOwnershipInfo.Owner = Alliance.Neutral;
                }
            }
        }

        return _mineralLinesInfo;
    }

    private static MineralOwnershipInfo CreateMineralOwnershipInfo(Vector3 clusterPosition)
    {
        var walkingDistanceToStartingLocation = Controller.PathFinder!.FindPath(Controller.StartingLocation with
        {
            X = Controller.StartingLocation.X
        }, clusterPosition).Length;

        var walkingDistanceToEnemyLocation = Controller.PathFinder!.FindPath(Controller.EnemyLocations.First() with
        {
            X = Controller.EnemyLocations.First().X
        }, clusterPosition).Length;
        
        return new MineralOwnershipInfo()
        {
            CenterPosition = clusterPosition,
            WalkingDistanceToStartingLocation = walkingDistanceToStartingLocation == 0 ? 9999 : walkingDistanceToStartingLocation,
            WalkingDistanceToEnemyLocation = walkingDistanceToEnemyLocation == 0 ? 9999 : walkingDistanceToEnemyLocation
        };
    }

    public static MineralOwnershipInfo GetLastExpension()
    {
        return GetLineralLinesInfo()
            .Where(x => x.Owner == Alliance.Self)
            .OrderBy(x => x.WalkingDistanceToStartingLocation)
            .Last();
    }

    public static ulong LastFrameUpdatedOwnership { get; set; }

    public class MineralOwnershipInfo
    {
        public Vector3 CenterPosition { get; set; }
        
        public int WalkingDistanceToStartingLocation { get; set; }
        
        public Alliance Owner { get; set; }
        
        public int WalkingDistanceToEnemyLocation { get; set; }
    }
}