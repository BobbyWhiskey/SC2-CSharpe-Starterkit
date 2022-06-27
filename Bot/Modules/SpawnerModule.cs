using Bot.Queries;

namespace Bot;

public class SpawnerModule
{
    private readonly Random _random = new();

    public void OnFrame()
    {
        //if (!IsTimeForExpandQuery.Get())
        {
            // TODO NEed to handle if building has a reactor wink wink
            foreach (var barracks in Controller.GetUnits(Units.BARRACKS, onlyCompleted: true))
                if (_random.NextDouble() > 0.7)
                {
                    if (Controller.CanConstruct(Units.MARAUDER) && barracks.order.AbilityId == 0)
                    {
                        barracks.Train(Units.MARAUDER);
                    }
                }
                else
                {
                    // Temporary fix to use reactors
                    if (Controller.CanConstruct(Units.MARINE) && barracks.orders.Count < 2)
                    {
                        barracks.Train(Units.MARINE, true);
                    }
                }

            foreach (var factory in Controller.GetUnits(Units.FACTORY, onlyCompleted: true))
            {
                if (Controller.CanConstruct(Units.SIEGE_TANK)
                    && factory.order.AbilityId == 0)
                {
                    factory.Train(Units.SIEGE_TANK);
                }
            }

            foreach (var starport in Controller.GetUnits(Units.STARPORT, onlyCompleted: true))
            {
                // Temporary fix to use reactors
                if (Controller.CanConstruct(Units.MEDIVAC) && starport.orders.Count < 2)
                {
                    starport.Train(Units.MEDIVAC, true);
                }
            }
        }
    }
}