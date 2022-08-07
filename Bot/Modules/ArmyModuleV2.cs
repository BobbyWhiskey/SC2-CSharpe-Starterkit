using System.Numerics;
using Bot.Queries;
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

    private double ArmyValueDiffThreshold = 1.15;

    private const double MainDefencePercentage = 0.14;
    private const int MainDefenceLength = 13;
    private const int ArmyCountThresholdAttack = 25; 

    private double AttackPercentage { get; set; } = MainDefencePercentage;

    private Point LastAttackPosition { get; set; }
    
    private Vector3 RallyPoint { get; set; }

    public ulong LastAttackFrame { get; set; }

    public async Task OnFrame()
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        CollectStats();

        //Controller.ShowDebugPath(_mainPath);

        switch (ArmyState)
        {
            case ArmyState.DEFEND:
                await Defend();
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

    private void Initialize()
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

        }

        _mainPath = path.ToList();
        _isInitialized = true;
    }

    private Vector3 CalculateArmyApproxPosition(ICollection<Unit> units)
    {
        var positions = units.Select(x => x.Position).ToList();
        var averageArmyPosition = new Vector3(positions.Average(x => x.X), positions.Average(x => x.Y), positions.Average(x => x.Z));
        var mainArmyPositions = units
            .OrderBy(x => (x.Position - averageArmyPosition).Length())
            .Take(positions.Count() / 2 + 1)
            .Select(x => x.Position).ToList();
        
        return new Vector3(mainArmyPositions.Average(x => x.X), mainArmyPositions.Average(x => x.Y), mainArmyPositions.Average(x => x.Z));
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

                var enemyArmy = Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy, onlyVisible:true);
                if (enemyArmy.Any())
                {
                    this.AdjustedEnemyArmyPosition = CalculateArmyApproxPosition(enemyArmy);
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
        if (Controller.IsDebug)
        {
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
                                   "\nArmyState " + ArmyState +
                                   "\nLastRetreatEnemyArmyValue " + LastRetreatEnemyArmyValue +
                                   "\nOwnArmyValue " + GetOwnArmyValue() + 
                                   "\nEnemyArmyValue " + GetEnemyArmyValue()
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
                            P = AdjustedEnemyArmyPosition.ToPoint(),
                            R = 5,
                            Color = new Color()
                            {
                                R = 250,
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
    }

    public Vector3 AdjustedArmyPosition { get; set; }
    public Vector3 AdjustedEnemyArmyPosition { get; set; }

    public float DivergenceWithAttackPercentage { get; set; }

    public float AverageArmyDivergence { get; set; }

    public Vector3 AverageArmyPosition { get; set; }

    private void Roam()
    {
        if (GetEnemiesCloseToBaseArmy().Any())
        {
            ArmyState = ArmyState.DEFEND;
            return;
        }
        
        var armyValue = GetEnemyArmyValue();
        if (armyValue > GetOwnArmyValue())
        {
            ArmyState = ArmyState.RETREAT;
            LastRetreatEnemyArmyValue = armyValue;
            return;
        }
        else if (armyValue > 0)
        {
            ArmyState = ArmyState.ATTACK;
            return;
        }
        
        // Arrived at destination?
        if (RoamingTarget == null || (RoamingTarget - AdjustedArmyPosition).Value.Length() < 5)
        {
            var possibleTargets = MineralLinesQueries.GetLineralLinesInfo()
                .Where(x => x.CenterPosition != RoamingTarget)
                .OrderBy(x => x.WalkingDistanceToEnemyLocation)
                .Take(4);
            
            RoamingTarget = possibleTargets.OrderBy(x => Guid.NewGuid()).First().CenterPosition;
        }
        
        AttackWithArmyInSteps(RoamingTarget.Value);
    }

    public int LastRetreatEnemyArmyValue { get; set; }

    public Vector3? RoamingTarget { get; set; }

    private void Retreat()
    {
        var enemyArmy = Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy);
        if (!enemyArmy.Any() || GetEnemiesCloseToBaseArmy().Any())
        {
            ArmyState = ArmyState.DEFEND;
            return;
        }

        var siegedTanks = Controller.GetUnits(Units.SIEGE_TANK_SIEGED);
        foreach (var tank in siegedTanks)
        {
            tank.Ability(Abilities.UNSIEGE_TANK);
        }
        
        var myArmy = Controller.GetUnits(Units.ArmyUnits);
        var retreatTo = MineralLinesQueries.GetLastExpension().CenterPosition;
        Controller.Move(myArmy, retreatTo);
        // foreach (var unit in myArmy)
        // {
        //     
        //     unit.Move(retreatTo);
        // }
    }

    private void Attack()
    {
        var myArmy = Controller.GetUnits(Units.ArmyUnits);
        if (myArmy.Count < ArmyCountThresholdAttack)
        {
            //ArmyState = ArmyState.DEFEND;
            //AttackPercentage = MainDefencePercentage;
            //return;
        }

        var visibleEnemyArmy = Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy);
        var cachedEnemyUnits = Controller.GetUnits(Units.All, Alliance.Enemy, onlyVisible: false);
        var enemyArmyValue = GetEnemyArmyValue();
        if (enemyArmyValue > GetOwnArmyValue() * ArmyValueDiffThreshold
            || GetEnemiesCloseToBaseArmy().Count() > 2)
        {
            //ArmyState = ArmyState.DEFEND;
            ArmyState = ArmyState.RETREAT;
            LastRetreatEnemyArmyValue = enemyArmyValue;
            AttackPercentage = MainDefencePercentage;
            return;
        }

        if (visibleEnemyArmy.Count() > 2)
        {
            AttackWithArmyInSteps(CalculateArmyApproxPosition(visibleEnemyArmy));
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
    
    private async Task Defend()
    {
        var closeArmy = GetEnemiesCloseToBaseArmy().ToList();

        if (!closeArmy.Any() 
            && Controller.GetUnits(Units.ArmyUnits).Count > StartRoamingThreshold
            && Controller.GetUnits(Units.ArmyUnits).Count < StopRoamingThreshold
            && GetOwnArmyValue() > LastRetreatEnemyArmyValue * ArmyValueDiffThreshold)
        {
            ArmyState = ArmyState.ROAM;
            return;
        }

        if (Controller.GetUnits(Units.ArmyUnits.Except(Units.SupportUnits)).Count > ArmyCountThresholdAttack
            && GetOwnArmyValue() > LastRetreatEnemyArmyValue * ArmyValueDiffThreshold
            && !closeArmy.Any()
            && GetEnemyArmyValue() < GetOwnArmyValue())
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
           await AttackMoveToMainPathDistance(MainDefenceLength);
        }
    }

    private const int StopRoamingThreshold = 30;

    private const int StartRoamingThreshold = 6;

    private void AttackWithArmyInSteps(Vector3 position)
    {
        const int attackStepSize = 12;
        var attackPath = Controller.PathFinder!.FindPath(new Point((int)AdjustedArmyPosition.X, (int)AdjustedArmyPosition.Y), new Point((int)position.X, (int)position.Y));

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
        var myArmy = Controller.GetUnits(Units.ArmyUnits.Except(Units.SupportUnits));
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

    private async Task AttackMoveToMainPathDistance(int length)
    {
        var lastExpansion = MineralLinesQueries.GetLineralLinesInfo()
            .Where(x => x.Owner == Alliance.Self)
            .MaxBy(x => x.WalkingDistanceToStartingLocation);
        if (lastExpansion != null)
        {
            var pathToEnemy = Controller.PathFinder!.FindPath(lastExpansion.CenterPosition, Controller.EnemyLocations.First());
            //Controller.ShowDebugPath(pathToEnemy.ToList());
            AttackWithArmyInSteps(pathToEnemy.Take(length).Last().ToVector3());
        }
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