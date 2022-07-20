namespace Bot.Modules;

public class ScoutingModule
{
    private static ulong ScoutingInterval = Controller.SecsToFrames(240);
    private static ulong ScoutingStartingFrame = Controller.SecsToFrames(60);
    private ulong LastScoutingFrame = ulong.MinValue;

    private ulong CurrentScoutingUnit = 0;

    public void OnFrame()
    {
        // Just temporarly scan just once at the begining of the game
        if (LastScoutingFrame != ulong.MinValue)
        {
            return;
        }
        
        
        if (CurrentScoutingUnit != 0)
        {
            var unit = Controller.GetUnits(Units.All).FirstOrDefault(x => x.Tag == CurrentScoutingUnit);
            if (unit == null)
            {
                Controller.ReleaseUnit(CurrentScoutingUnit);
                CurrentScoutingUnit = 0;
            }
            else if (unit.Order.AbilityId == 0)
            {
                Controller.ReleaseUnit(unit.Tag);
            }
        }

        if (Controller.Frame > LastScoutingFrame + ScoutingInterval
            || (Controller.Frame > ScoutingStartingFrame && LastScoutingFrame == 0))
        {
            var scvs = Controller.GetUnits(Units.SCV);
            if (scvs.Any())
            {
                var scv = scvs.First();

                Controller.ReserveUnit(scv.Tag);
                CurrentScoutingUnit = scv.Tag;

                scv.Move(Controller.EnemyLocations.First());

                LastScoutingFrame = Controller.Frame;
            }
        }
    }

}