using Bot.Queries;

namespace Bot
{
    public class SpawnerModule
    {
        public void OnFrame()
        {
            if (!IsTimeForExpandQuery.Get())
            {
                foreach (var barracks in Controller.GetUnits(Units.BARRACKS, onlyCompleted:true)) {
                    if (Controller.CanConstruct(Units.MARINE) && barracks.order.AbilityId == 0)
                    {
                        barracks.Train(Units.MARINE);
                    }
                }
            
                foreach (var factory in Controller.GetUnits(Units.FACTORY, onlyCompleted:true)) {
                    if (Controller.CanConstruct(Units.SIEGE_TANK) 
                        && factory.order.AbilityId == 0)
                    {
                        factory.Train(Units.SIEGE_TANK);
                    }
                }
            
                foreach (var starport in Controller.GetUnits(Units.STARPORT, onlyCompleted:true)) {
                    if (Controller.CanConstruct(Units.MEDIVAC) && starport.order.AbilityId == 0)
                    {
                        starport.Train(Units.MEDIVAC);
                    }
                }
            }
        }
    }
}