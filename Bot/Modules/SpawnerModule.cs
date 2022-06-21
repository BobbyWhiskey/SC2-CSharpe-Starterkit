using System;
using Bot.Queries;

namespace Bot
{
    public class SpawnerModule
    {
        private Random _random = new Random();
        public void OnFrame()
        {
            if (!IsTimeForExpandQuery.Get())
            {
                // TODO NEed to handle if building has a reactor wink wink
                foreach (var barracks in Controller.GetUnits(Units.BARRACKS, onlyCompleted:true)) {
                    if (_random.Next() > int.MaxValue * 0.7)
                    {
                        if (Controller.CanConstruct(Units.MARAUDER) && barracks.order.AbilityId == 0)
                        {
                            barracks.Train(Units.MARAUDER);
                        }    
                    }
                    else
                    {
                        if (Controller.CanConstruct(Units.MARINE) && barracks.order.AbilityId == 0)
                        {
                            barracks.Train(Units.MARINE);
                        }
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