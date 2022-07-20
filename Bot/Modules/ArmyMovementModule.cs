using System.Numerics;
using SC2APIProtocol;

namespace Bot.Modules;

public class ArmyMovementModule
{
    private readonly int _attackUnitCountThreshold = 25;
    private Vector3? _lastAttackPosition;
    private ulong _lastAttackPositionUpdate;
    private ulong _lastAttackMoveTime;

    public void OnFrame()
    {
        if (Controller.Frame % 5 != 0)
        { 
            return; 
        }
        
        var army = Controller.GetUnits(Units.ArmyUnits);
        var attackingUnits = GetCloseToBaseArmy().ToList();
        if (attackingUnits.Count > 1)
        {
            // Defend against an attacking unit
            AttackWithArmy(attackingUnits.First().Position);
        }
        else if (army.Count > _attackUnitCountThreshold && GetOwnArmyValue() > GetEnemyArmyValue())
        {
            var enemyUnits = Controller.GetUnits(Units.All, Alliance.Enemy, onlyVisible: true);
            if (enemyUnits.Any())
            {
                AttackWithArmy(enemyUnits.First().Position);
            }
            else if (_lastAttackPosition.HasValue)
            {
                if (Controller.Frame - _lastAttackPositionUpdate > 200)
                {
                    var enemies = Controller.GetUnits(Units.All, Alliance.Enemy, onlyVisible:true);
                    if (enemies.Any())
                    {
                        AttackWithArmy(enemies.First().Position);
                    }
                    else
                    {
                        if (_lastAttackPosition.HasValue)
                        {
                            // Meh 
                            var attackLocation = _lastAttackPosition + Vector3.Multiply(0.2f, Controller.EnemyLocations[0] - _lastAttackPosition.Value);
                            AttackWithArmy(attackLocation.Value);
                            //_lastAttackPosition = _lastAttackPosition.Value.MidWay(Controller.enemyLocations[0]);
                            //_lastAttackPositionUpdate = Controller.frame;
                        }
                    }
                }

                //AttackWithArmy(_lastAttackPosition.Value);

//                Controller.Attack(army, _lastAttackPosition.Value);
            }
            else if (Controller.EnemyLocations.Count > 0)
            {
                var attackLocation = Controller.StartingLocation +
                                      Vector3.Multiply(0.3f, Controller.EnemyLocations[0] - Controller.StartingLocation);
                AttackWithArmy(attackLocation);
                
                // _lastAttackPosition = Controller.enemyLocations[0].MidWay(Controller.GetResourceCenters().First().position);
                //_lastAttackPositionUpdate = Controller.frame;
            }
            
            // Dirty fix: Reattack just to make sure we don't leave any units behind
            if (_lastAttackPosition != null)
            {
                AttackWithArmy(_lastAttackPosition.Value);
            }
        }
        else
        {
            // Rally point
            var rcs = Controller.GetResourceCenters()
                .OrderBy(rc => (rc.Position - Controller.EnemyLocations[0]).LengthSquared())
                .ToList();
            if (rcs.Any())
            {
                // Dirty way to just have a Rally point somewhere useful
                var avgX = rcs.Select(m => m.Position.X).Average();
                var avgY = rcs.Select(m => m.Position.Y).Average();
                var avgZ = rcs.Select(m => m.Position.Z).Average();
                var avg = new Vector3(avgX, avgY, avgZ);

                var rallyPoint = avg +
                                 (Controller.EnemyLocations[0] - avg) * (float)0.15;

                _lastAttackPosition = rallyPoint;

                AttackWithArmy(rallyPoint);
            }
        }
    }

    private void AttackWithArmy(Vector3 position)
    {
        List<Unit> army = Controller.GetUnits(Units.ArmyUnits);
        
        if (_lastAttackPosition != position || Controller.Frame > _lastAttackMoveTime + Controller.FRAMES_PER_SECOND * 3)
        {
            if (_lastAttackPosition != position)
            {
                _lastAttackPosition = position;
                _lastAttackPositionUpdate = Controller.Frame;
            }
            Controller.Attack(army, _lastAttackPosition.Value);
            _lastAttackMoveTime = Controller.Frame;
        }
        
    }

    private IEnumerable<Unit> GetCloseToBaseArmy()
    {
        var enemyArmy = Controller.GetUnits(Units.All, Alliance.Enemy, onlyVisible: true);
        var resourceCenters = Controller.GetResourceCenters();
        return enemyArmy.Where(unit => resourceCenters.Any(rc => (rc.Position - unit.Position).Length() < 25));
    }

    private int GetOwnArmyValue()
    {
        var myArmy = Controller.GetUnits(Units.ArmyUnits);
        return (int)myArmy.Sum(u => Controller.GameData.Units[(int)u.UnitType].MineralCost);
    }

    private int GetEnemyArmyValue()
    {
        var units = Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy, onlyVisible: true);
        return (int)units.Sum(u => Controller.GameData.Units[(int)u.UnitType].MineralCost);
    }
}

internal enum ArmyMovementState
{
    Defending,
    Attacking,
    Retreating
}