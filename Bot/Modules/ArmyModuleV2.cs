using System.Numerics;
using SC2APIProtocol;
using Point = System.Drawing.Point;

namespace Bot.Modules;

public class ArmyModuleV2
{
    private bool _isInitialized;
    private List<Point> _mainPath;
    private ArmyState ArmyState { get; set; } = ArmyState.DEFEND;

    private double attackPercentage { get; set; } = 0.25;

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
                Controller.ShowDebugPath(new List<Point>(new[] { startPosition }), new Color
                    { G = 250, B = 1, R = 1 }, 16);
                Controller.ShowDebugPath(new List<Point>(new[] { toPosition }), new Color
                    { G = 1, B = 250, R = 1 }, 16);

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
        var attackPosition = GetAttackPercentageToPosition(attackPercentage);
        
        if (Controller.Frame % 15 == 0)
        {
            var positions = Controller.GetUnits(Units.ArmyUnits).Select(x => x.Position).ToList();
            if (positions.Any())
            {
                this.AverageArmyPosition = new Vector3(positions.Average(x => x.X), positions.Average(x => x.Y), positions.Average(x => x.Z));

                var divergences = positions.Select(x => x - AverageArmyPosition).ToList();
                this.AverageArmyDivergence = (float)divergences.Average(x => Math.Log(x.LengthSquared()));

                this.DivergenceWithAttackPercentage = (float)Math.Log((new Vector3(attackPosition.X, attackPosition.Y, AverageArmyPosition.Z) - AverageArmyPosition).Length());
            }
        }
        Controller.AddDebugCommand(new DebugCommand()
        {
            Draw = new DebugDraw()
            {
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
                        P = new SC2APIProtocol.Point(){X = attackPosition.X, Y = attackPosition.Y, Z = AverageArmyPosition.Z},
                        R = 5,
                        Color = new Color()
                        {
                            R = 250,
                            B = 1,
                            G = 1
                        }
                    }
                },
                Text =
                {
                    new DebugText()
                    {
                        WorldPos = new SC2APIProtocol.Point(){X = attackPosition.X, Y = attackPosition.Y, Z = AverageArmyPosition.Z},
                        Text = "Army divergence " + AverageArmyDivergence +
                               "\nArmyDivergence to AttackPosition " + DivergenceWithAttackPercentage
                    }
                }
            }

        });
    }

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
        if (Controller.GetUnits(Units.ArmyUnits).Count < 20)
        {
            ArmyState = ArmyState.DEFEND;
            attackPercentage = 0.25;
            return;
        }

        if (Controller.Frame % ((int)Controller.FRAMES_PER_SECOND * 1) == 0
            && AverageArmyDivergence < 7
            && DivergenceWithAttackPercentage < 3.5 )
        {
            attackPercentage += 0.015;
        }

        if (attackPercentage > 1)
        {
            attackPercentage = 1;
        }

        AttackMoveToMainPath(attackPercentage);
    }

    private void Defend()
    {
        if (Controller.GetUnits(Units.ArmyUnits).Count > 25)
        {
            ArmyState = ArmyState.ATTACK;
            return;
        }

        AttackMoveToMainPath(0.25);
    }

    private Point GetAttackPercentageToPosition(double d)
    {
        return _mainPath[Math.Min((int)(_mainPath.Count * d), _mainPath.Count - 1)];
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

        Controller.ShowDebugPath(_mainPath);


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