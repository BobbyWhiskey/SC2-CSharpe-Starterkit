using Bot.Queries;

namespace Bot;

public class SpawnerModule
{
    private readonly Random _random = new();

    public void OnFrame()
    {
        //if (!IsTimeForExpandQuery.Get())
        {
            var nextBuildOrder = BuildOrderQueries.GetNextBuildOrderUnit();
            // TODO NEed to handle if building has a reactor wink wink
            foreach (var barracks in Controller.GetUnits(Units.BARRACKS, onlyCompleted: true))
            {
                // Dont build if we are waiting to create an addon
                if (!barracks.GetAddonType().HasValue
                    && nextBuildOrder.HasValue
                    && Units.BarrackAddOns.Contains(nextBuildOrder.Value))
                {
                    continue;
                }

                if (_random.NextDouble() > 0.7)
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
                        barracks.Train(Units.MARINE, true);
                    }

                    if (barracks.GetAddonType() == Units.BARRACKS_REACTOR
                        && Controller.CanConstruct(Units.MARINE)
                        && barracks.orders.Count < 2)
                    {
                        barracks.Train(Units.MARINE, true);
                    }
                }
            }

            foreach (var factory in Controller.GetUnits(Units.FACTORY, onlyCompleted: true))
            {
                // Dont build if we are waiting to create an addon
                if (!factory.GetAddonType().HasValue
                    && nextBuildOrder.HasValue
                    && Units.FactoryAddOns.Contains(nextBuildOrder.Value))
                {
                    continue;
                }
                
                if (Controller.CanConstruct(Units.SIEGE_TANK)
                    && factory.order.AbilityId == 0)
                {
                    factory.Train(Units.SIEGE_TANK);
                }
            }

            foreach (var starport in Controller.GetUnits(Units.STARPORT, onlyCompleted: true))
            {
                // Dont build if we are waiting to create an addon
                if (!starport.GetAddonType().HasValue
                    && nextBuildOrder.HasValue
                    && Units.StarportAddOns.Contains(nextBuildOrder.Value))
                {
                    continue;
                }
                
                // TODO Temporary fix to use reactors, use GetAddonType instead
                if (Controller.CanConstruct(Units.MEDIVAC) && starport.orders.Count < 2)
                {
                    starport.Train(Units.MEDIVAC, true);
                }
            }
        }
    }
}