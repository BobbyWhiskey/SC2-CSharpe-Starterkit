using SC2APIProtocol;

namespace Bot.Simulation;

public class ZerglingRushSimulation
{
    private bool _attackDone = false;
    private ulong _attackTiming = Controller.SecsToFrames(180);
    
    public ZerglingRushSimulation()
    {
    }

    public void OnFrame()
    {
        if (!_attackDone 
            && Controller.Frame > _attackTiming)
        {
            _attackDone = true;

            Controller.AddDebugCommand(new DebugCommand()
            {
                CreateUnit = new DebugCreateUnit()
                {
                    Owner = 3,
                    Pos= new Point2D(){X = Controller.StartingLocation.X + 6, Y =Controller.StartingLocation.Y + 6 },
                    
                    UnitType = Units.ZERGLING,
                    Quantity = 10,
                }
            });
            
        }
        
    }
}
