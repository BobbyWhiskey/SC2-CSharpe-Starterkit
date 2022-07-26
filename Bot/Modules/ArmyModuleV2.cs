using System.Numerics;
using SC2APIProtocol;
using Point = System.Drawing.Point;

namespace Bot.Modules;

public class ArmyModuleV2
{
    private bool _isInitialized;
    private List<Point> _mainPath = null!;

    private Vector3 _lastAttackPosition;
    private double _lastAttackMoveTime;
    private ArmyState ArmyState { get; set; } = ArmyState.DEFEND;

    private const double MainDefencePercentage = 0.14;

    private double AttackPercentage { get; set; } = MainDefencePercentage;

    private Point LastAttackPosition { get; set; }

    public ulong LastAttackFrame { get; set; }

    public void OnFrame()
    {
        if (!_isInitialized)
        {
            // Just a bit of offset to get our of the CC range
            var delta = 3;

            var startPosition = new Point((int)Controller.StartingLocation.X,
                (int)Controller.StartingLocation.Y + delta);
            var toPosition = new Point((int)Controller.EnemyLocations.First().X,
                (int)Controller.EnemyLocations.First().Y);

            var path = Controller.PathFinder.FindPath(startPosition, toPosition);

            if (path == null || path.Length == 0)
            {
                Controller.ShowDebugPath(new List<Point>(new[]
                {
                    startPosition
                }), new Color
                {
                    G = 250,
                    B = 1,
                    R = 1
                }, 16);
                Controller.ShowDebugPath(new List<Point>(new[]
                {
                    toPosition
                }), new Color
                {
                    G = 1,
                    B = 250,
                    R = 1
                }, 16);

                //_isInitialized = true;
                return;
                throw new Exception("CANNOT CREATE ATTACK PATH");
            }

            _mainPath = path.ToList();

            _isInitialized = true;
        }

        CollectStats();

        Controller.ShowDebugPath(_mainPath);

        switch (ArmyState)
        {
            case ArmyState.DEFEND:
                Defend();
                break;
            case ArmyState.ATTACK:
                Attack();
                break;
            case ArmyState.RETREAT:
                Retreat();
                break;
            case ArmyState.ROAM:
                Roam();
                break;
        }
    }

    private void CollectStats()
    {
        var attackPosition = GetAttackPercentageToPosition(AttackPercentage);

        if (Controller.Frame % 15 == 0)
        {
            var positions = Controller.GetUnits(Units.ArmyUnits).Select(x => x.Position).ToList();
            if (positions.Any())
            {
                this.AverageArmyPosition = new Vector3(positions.Average(x => x.X), positions.Average(x => x.Y), positions.Average(x => x.Z));
                var mainArmyPositions = Controller.GetUnits(Units.ArmyUnits)
                    //.Where(x => (x.Position - AverageArmyPosition).Length() < AverageArmyDivergence)
                    .OrderBy(x => (x.Position - AverageArmyPosition).Length())
                    .Take(positions.Count() / 2)
                    .Select(x => x.Position).ToList();

                if (mainArmyPositions.Any())
                {
                    this.AdjustedArmyPosition = new Vector3(mainArmyPositions.Average(x => x.X), mainArmyPositions.Average(x => x.Y), mainArmyPositions.Average(x => x.Z));
                }

                var divergences = positions.Select(x => x - AverageArmyPosition).ToList();
                this.AverageArmyDivergence = (float)divergences.Average(x => Math.Min(x.Length(), 15));

                this.DivergenceWithAttackPercentage = (float)Math.Min(
                    (new Vector3(attackPosition.X, attackPosition.Y, 0)
                     - AverageArmyPosition with
                     {
                         Z = 0
                     }
                    ).Length(), 15);
            }
        }
        Controller.AddDebugCommand(new DebugCommand()
        {
            Draw = new DebugDraw()
            {
                Text =
                {
                    new DebugText()
                    {
                        VirtualPos = new SC2APIProtocol.Point()
                        {
                            X = 0.0f,
                            Y = 0.1f,
                        },
                        Size = 12,
                        Text = "Army Divergence " + AverageArmyDivergence +
                               //"ArmyDivergence to AttackPosition\n" + DivergenceWithAttackPercentage +
                               "\nArmyState " + ArmyState
                               + "\n "
                               
                    },
                },
                Spheres =
                {
                    new DebugSphere()
                    {
                        P = AverageArmyPosition.ToPoint(),
                        R = AverageArmyDivergence,
                        Color = new Color()
                        {
                            R = 1,
                            B = 1,
                            G = 250
                        }
                    },
                    new DebugSphere()
                    {
                        P = AdjustedArmyPosition.ToPoint(),
                        R = 5,
                        Color = new Color()
                        {
                            R = 1,
                            B = 250,
                            G = 1,
                        }
                    },
                    new DebugSphere()
                    {
                        P = new SC2APIProtocol.Point()
                        {
                            X = attackPosition.X,
                            Y = attackPosition.Y,
                            Z = AverageArmyPosition.Z + 2
                        },
                        R = 5,
                        Color = new Color()
                        {
                            R = 250,
                            B = 1,
                            G = 1
                        }
                    }
                }
            }

        });
    }

    public Vector3 AdjustedArmyPosition { get; set; }

    public float DivergenceWithAttackPercentage { get; set; }

    public float AverageArmyDivergence { get; set; }

    public Vector3 AverageArmyPosition { get; set; }

    private void Roam()
    {
    }

    private void Retreat()
    {
    }

    private void Attack()
    {
        var myArmy = Controller.GetUnits(Units.ArmyUnits);
        if (myArmy.Count < 25)
        {
            ArmyState = ArmyState.DEFEND;
            AttackPercentage = MainDefencePercentage;
            return;
        }

        var visibleEnemyArmy = Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy);
        var cachedEnemyUnits = Controller.GetUnits(Units.All, Alliance.Enemy, onlyVisible: false);

        if (GetEnemyArmyValue() > GetOwnArmyValue()
            || GetEnemiesCloseToBaseArmy().Count() > 2)
        {
            ArmyState = ArmyState.DEFEND;
            AttackPercentage = MainDefencePercentage;
            return;
        }

        if (visibleEnemyArmy.Count() > 2)
        {
            AttackWithArmyInSteps(visibleEnemyArmy.First().Position);
        }

        else if (cachedEnemyUnits.Any())
        {
            var target = cachedEnemyUnits.MinBy(x => (x.Position - AverageArmyPosition).LengthSquared());

            AttackWithArmyInSteps(target!.Position);
        }
        else
        {
            if (Controller.Frame % ((int)Controller.FRAMES_PER_SECOND * 1) == 0
                && AverageArmyDivergence < 13
                && DivergenceWithAttackPercentage < 13)
            {
                AttackPercentage += 0.015;
            }

            if (AttackPercentage > 1)
            {
                AttackPercentage = 1;
            }

            AttackMoveToMainPath(AttackPercentage);
        }

    }

    private void Defend()
    {
        var closeArmy = GetEnemiesCloseToBaseArmy().ToList();

        if (Controller.GetUnits(Units.ArmyUnits).Count > 20 && !closeArmy.Any())
        {
            ArmyState = ArmyState.ATTACK;
            return;
        }

        if (closeArmy.Any())
        {
            AttackWithArmy(closeArmy.First().Position);
        }
        else
        {
            AttackMoveToMainPath(MainDefencePercentage);
        }
    }

    private void AttackWithArmyInSteps(Vector3 position)
    {
        const int attackStepSize = 15;
        var attackPath = Controller.PathFinder.FindPath(new Point((int)AdjustedArmyPosition.X, (int)AdjustedArmyPosition.Y), new Point((int)position.X, (int)position.Y));

        Controller.ShowDebugPath(attackPath.ToList(), new Color()
        {
            R = 250,
            B = 1,
            G = 1
        });

        if (attackPath.Length > 0)
        {
            if (attackPath.Length <= attackStepSize)
            {
                AttackWithArmy(attackPath.Last());
            }
            else
            {
                AttackWithArmy(attackPath[attackStepSize]);
            }
        }
        else
        {
            // Sometime the pathfinding fails breafly
            // Fallback on non pathing way.
            AttackWithArmy(position);
        }

        // var delta = AverageArmyDivergence/10 ;
        // var movementVector = position - AverageArmyPosition;
        // var normalizedMovement = movementVector.Normalize();
        //
        // var attackPosition = Vector3.Multiply(movementVector, 0.2f + (float)delta) + AverageArmyPosition;
        // if (movementVector.Length() > 30)
        // {
        //     attackPosition = AverageArmyPosition + (normalizedMovement * 20);
        // }
        // AttackWithArmy(attackPosition);


        //position
        //
        // if (_lastAttackPosition != attackPosition 
        //     || Controller.Frame > _lastAttackMoveTime + Controller.FRAMES_PER_SECOND * 3)
        // {
        //     if (_lastAttackPosition != attackPosition)
        //     {
        //         _lastAttackPosition = attackPosition;
        //         _lastAttackPositionUpdate = Controller.Frame;
        //     }
        //     Controller.Attack(army, _lastAttackPosition);
        //     _lastAttackMoveTime = Controller.Frame;
        // }
    }

    private void AttackWithArmy(Point position)
    {
        AttackWithArmy(new Vector3(position.X, position.Y, 0));
    }

    private void AttackWithArmy(Vector3 position)
    {
        List<Unit> army = Controller.GetUnits(Units.ArmyUnits);

        var positionInInt = new Vector3((int)position.X, (int)position.Y, (int)position.Z);

        if (_lastAttackPosition != positionInInt
            || Controller.Frame > _lastAttackMoveTime + Controller.FRAMES_PER_SECOND * 3)
        {
            _lastAttackPosition = positionInInt;
            Controller.Attack(army, _lastAttackPosition);
            _lastAttackMoveTime = Controller.Frame;
        }

        Controller.AddDebugCommand(new DebugCommand()
        {
            Draw = new DebugDraw()
            {
                Spheres =
                {
                    new DebugSphere()
                    {
                        P = _lastAttackPosition.ToPoint(),
                        R = 2,
                        Color = new Color()
                        {
                            R = 150,
                            B = 50,
                            G = 1
                        }
                    }
                }

            }
        });
    }

    private Point GetAttackPercentageToPosition(double d)
    {
        return _mainPath[Math.Min((int)(_mainPath.Count * d), _mainPath.Count - 1)];
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

    private IEnumerable<Unit> GetEnemiesCloseToBaseArmy()
    {
        var enemyArmy = Controller.GetUnits(Units.All, Alliance.Enemy, onlyVisible: true);
        var resourceCenters = Controller.GetResourceCenters();
        return enemyArmy.Where(unit => resourceCenters.Any(rc => (rc.Position - unit.Position).Length() < 25));
    }

    private void AttackMoveToMainPath(double d)
    {
        var position = GetAttackPercentageToPosition(d);

        var army = Controller.GetUnits(Units.ArmyUnits);

        if (LastAttackPosition != position ||
            LastAttackFrame + 5 * Controller.FRAMES_PER_SECOND < Controller.Frame)
        {
            Controller.Attack(army, new Vector3(position.X, position.Y, 0));
            LastAttackPosition = position;
            LastAttackFrame = Controller.Frame;
        }

        //Controller.ShowDebugPath(_mainPath);


        // Controller.AddDebugCommand(new DebugCommand()
        // {
        //     Draw =
        //     {
        //         Spheres =
        //         {
        //             new []
        //             {
        //                 new DebugSphere()
        //                 {
        //                     P = new Point()
        //                         {X = position.Position.X, Y=position.Position.Y, Z= 10},
        //                     
        //                     Color = new Color()
        //                     {
        //                         R = 250,
        //                     }
        //                     
        //                 }
        //             }
        //         }
        //     }
        // });
    }
}

public enum ArmyState
{
    ATTACK,
    RETREAT,
    DEFEND,
    ROAM
}