using System.Numerics;
using Bot.AStar;
using SC2APIProtocol;

namespace Bot.Modules;

public class ArmyModuleV2
{
    private IList<Vector2> _mainPath;
    private bool _isInitialized;
    private ArmyState ArmyState { get; set; } = ArmyState.DEFEND;

    private double attackPercentage { get; set; } = 0.2;

    private Vector2 LastAttackPosition { get; set; }

    public void OnFrame()
    {
        if (!_isInitialized)
        {
            // Just a bit of offset to get our of the CC range
            var delta = 3; 
            
            IEnumerable<Vector2> pathStack = null;
            var startPosition = new Vector2((int)Controller.startingLocation.X ,
                (int)Controller.startingLocation.Y + delta);
            var toPosition = new Vector2((int)Controller.enemyLocations.First().X ,
                (int)Controller.enemyLocations.First().Y);

            pathStack = Controller.AStarPathingGrid.FindPath(startPosition, toPosition);

            if (pathStack == null)
            {
                Controller.ShowDebugPath(new List<Vector2>( new []{startPosition}), new Color(){G = 250, B = 1, R = 1}, 16);
                Controller.ShowDebugPath(new List<Vector2>( new []{toPosition}), new Color(){G = 1, B = 250, R = 1}, 16);
                
                //_isInitialized = true;
                return;
                throw new Exception("CANNOT CREATE ATTACK PATH");
            }

            _mainPath = pathStack.ToList();

            _isInitialized = true;
        }
        
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
            attackPercentage = 0.2;
            return;
        }

        if (Controller.frame % ((int)Controller.FRAMES_PER_SECOND * 1) == 0)
        {
            attackPercentage += 0.03;
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

        AttackMoveToMainPath(0.2);
    }

    private void AttackMoveToMainPath(double d)
    {
        var position = this._mainPath[Math.Max((int)(this._mainPath.Count * d), _mainPath.Count - 1)];

        var army = Controller.GetUnits(Units.ArmyUnits);

        // TODO MC Figure out why i have to reverse Y and X :(
        if (LastAttackPosition != position ||
            LastAttackFrame + 5 * Controller.FRAMES_PER_SECOND < Controller.frame)
        {
            Controller.Attack(army, new Vector3(position.Y, position.X, 0));
            LastAttackPosition = position;
            LastAttackFrame = Controller.frame;
        }

        Controller.ShowDebugPath(this._mainPath);


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

    public ulong LastAttackFrame { get; set; }
}

public enum ArmyState
{
    ATTACK,
    RETREAT,
    DEFEND,
    ROAM,
}