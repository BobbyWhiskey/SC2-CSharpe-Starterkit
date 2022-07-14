using System.Numerics;
using SC2APIProtocol;

namespace Bot.Modules;

public class ArmyMovementModule
{
    private Vector3? _lastAttackPosition;
    private ulong _lastAttackPositionUpdate;
    private int attackUnitCountThreshhold = 25;

    private ArmyMovementState state = ArmyMovementState.Defending;

    public void OnFrame()
    {
        var army = Controller.GetUnits(Units.ArmyUnits);
        var attackingUnits = GetCloseToBaseArmy().ToList();
        if (attackingUnits.Any())
        {
            // Defend against an attacking unit
            _lastAttackPosition = attackingUnits.First().position;
            _lastAttackPositionUpdate = Controller.frame;
            Controller.Attack(army, _lastAttackPosition.Value);
        }
        else if (army.Count > attackUnitCountThreshhold && GetOwnArmyValue() > GetEnemyArmyValue())
        {
            if (Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy).Any())
            {
                _lastAttackPosition = Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy).First().position;
                _lastAttackPositionUpdate = Controller.frame;
                Controller.Attack(army, _lastAttackPosition.Value);
            }
            else if (_lastAttackPosition.HasValue)
            {
                if (Controller.frame - _lastAttackPositionUpdate > 200)
                {
                    var enemies = Controller.GetUnits(Units.All, Alliance.Enemy);
                    if (enemies.Any())
                    {
                        _lastAttackPosition = enemies.First().position;
                        _lastAttackPositionUpdate = Controller.frame;
                    }
                    else
                    {
                        if (_lastAttackPosition.HasValue)
                        {
                            _lastAttackPosition += Vector3.Multiply(0.2f, Controller.enemyLocations[0] - _lastAttackPosition.Value);    
                        }
                        
                        //_lastAttackPosition = _lastAttackPosition.Value.MidWay(Controller.enemyLocations[0]);
                        _lastAttackPositionUpdate = Controller.frame;
                    }
                }

                Controller.Attack(army, _lastAttackPosition.Value);
            }
            else if (Controller.enemyLocations.Count > 0)
            {
                _lastAttackPosition = Controller.startingLocation +
                                      Vector3.Multiply(0.3f, Controller.enemyLocations[0] - Controller.startingLocation);
               // _lastAttackPosition = Controller.enemyLocations[0].MidWay(Controller.GetResourceCenters().First().position);
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

    private IEnumerable<Unit> GetCloseToBaseArmy()
    {
        var enemyArmy = Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy, onlyVisible: true);
        var resourceCenters = Controller.GetResourceCenters();
        return enemyArmy.Where(unit => resourceCenters.Any(rc => (rc.position - unit.position).Length() < 25));
    }

    private int GetOwnArmyValue()
    {
        var myArmy = Controller.GetUnits(Units.ArmyUnits);
        return (int)myArmy.Sum(u => Controller.gameData.Units[(int)u.unitType].MineralCost);
    }
    
    private int GetEnemyArmyValue()
    {
        var units = Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy, onlyVisible:true);
        return (int)units.Sum(u => Controller.gameData.Units[(int)u.unitType].MineralCost);
    }
}

internal enum ArmyMovementState
{
    Defending,
    Attacking,
    Retreating,
}