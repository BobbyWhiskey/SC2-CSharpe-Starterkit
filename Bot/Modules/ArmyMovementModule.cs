using System.Numerics;
using SC2APIProtocol;

namespace Bot.Modules;

public class ArmyMovementModule
{
    private Vector3? _lastAttackPosition;
    private ulong _lastAttackPositionUpdate;

    public void OnFrame()
    {
        var army = Controller.GetUnits(Units.ArmyUnits);
        if (army.Count > 25)
        {
            if (Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy).Any())
            {
                _lastAttackPosition = Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy).First().position;
                _lastAttackPositionUpdate = Controller.frame;
                Controller.Attack(army, _lastAttackPosition.Value);
            }
            else if (_lastAttackPosition.HasValue)
            {
                if (Controller.frame - _lastAttackPositionUpdate > 500)
                {
                    var enemies = Controller.GetUnits(Units.All, Alliance.Enemy);
                    if (enemies.Any())
                    {
                        _lastAttackPosition = enemies.First().position;
                        _lastAttackPositionUpdate = Controller.frame;
                    }
                    else
                    {
                        _lastAttackPosition = _lastAttackPosition.Value.MidWay(Controller.enemyLocations[0]);
                        _lastAttackPositionUpdate = Controller.frame;
                    }
                }

                Controller.Attack(army, _lastAttackPosition.Value);
            }
            else if (Controller.enemyLocations.Count > 0)
            {
                _lastAttackPosition = Controller.enemyLocations[0].MidWay(Controller.GetResourceCenters().First().position);
                _lastAttackPositionUpdate = Controller.frame;
            }
        }
        else
        {
            // Rally point
            var rcs = Controller.GetResourceCenters()
                .OrderBy(rc => (rc.position - Controller.enemyLocations[0]).LengthSquared())
                .ToList();
            if (rcs.Any())
            {
                // Dirty way to just have a Rally point somewhere useful
                var avgX = rcs.Select(m => m.position.X).Average();
                var avgY = rcs.Select(m => m.position.Y).Average();
                var avgZ = rcs.Select(m => m.position.Z).Average();
                var avg = new Vector3(avgX, avgY, avgZ);

                var rallyPoint = avg +
                                 (Controller.enemyLocations[0] - avg) * (float)0.15;

                Controller.Attack(army, rallyPoint);
            }
        }
    }
}