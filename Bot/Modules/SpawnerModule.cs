using Bot.BuildOrders;
using Bot.Queries;

namespace Bot;

public class SpawnerModule
{
    private readonly Random _random = new();

    public void OnFrame()
    {
        bool useRatioFromBuild = true;
        if (IsTimeForExpandQuery.Get() && Controller.minerals < 500)
        {
            return;
        }

        if (useRatioFromBuild)
        {
            // Fixed number of units
            foreach (var keyValuePair in BuildOrderQueries.currentBuild.idealUnitFixedNumber)
            {
                if (Controller.GetUnits(keyValuePair.Key).Count < keyValuePair.Value)
                {
                    TrainUnit(keyValuePair.Key);
                }
            }
            
            // Ratio units
            var myArmy = Controller.GetUnits(Units.ArmyUnits).ToList();
            var groupedArmy = myArmy.GroupBy(GroupByArmyDuplicateUnit).ToList();
            var unitRatio = groupedArmy.Select(x => (x.Key, (double)x.Count() / myArmy.Count));
            var normalizedTargetRatio = BuildOrderQueries.currentBuild.idealUnitRatio.Select(x =>
                (x.Key, x.Value / BuildOrderQueries.currentBuild.idealUnitRatio.Sum(y => y.Value)));

            var diffUnitRatios = normalizedTargetRatio.Select(x =>
                (x.Key, x.Item2 - (unitRatio.FirstOrDefault(y => y.Key == x.Key).Item2)));

            // Skip unit type we can't produce yet
            var targetUnitToTrain = diffUnitRatios
                .Where(x => !BuildOrderQueries.IsUnitCountMaxed(x.Key))
                .OrderByDescending(x => x.Item2)
                .Where(x => Controller.GetUnits(Controller.GetProducerBuildingType(x.Key))
                    .Where(b => Controller.CanBuildingTrainUnit(b, x.Key))
                    .Any(b => b.order.AbilityId == 0 || 
                              (b.GetAddonType().HasValue
                               && Units.Reactors.Contains(b.GetAddonType()!.Value) 
                               && b.orders.Count < 2 )) )
                .Select(x => x.Key)
                .FirstOrDefault();
            
            if (targetUnitToTrain != 0)
            {
                TrainUnit(targetUnitToTrain);
            }

            Controller.SetDebugPriorityUnitToTrain(targetUnitToTrain);
            
        }
        else 
        {
            var nextBuildOrder = BuildOrderQueries.GetNextStep() as BuildingStep;
            
            foreach (var barracks in Controller.GetUnits(Units.BARRACKS, onlyCompleted: true))
            {
                // Dont build if we are waiting to create an addon
                if (!barracks.GetAddonType().HasValue
                    && nextBuildOrder != null
                    && Units.BarrackAddOns.Contains(nextBuildOrder.BuildingType))
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
                    && nextBuildOrder != null
                    && Units.FactoryAddOns.Contains(nextBuildOrder.BuildingType))
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
                    && nextBuildOrder != null
                    && Units.StarportAddOns.Contains(nextBuildOrder.BuildingType))
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


    private void TrainUnit(uint targetUnitToTrain)
    {
        if (Units.FromBarracks.Contains(targetUnitToTrain))
        {
            GetAvailableProducerAndTrainUnit(Units.BARRACKS, targetUnitToTrain);
        }
        else if (Units.FromFactory.Contains(targetUnitToTrain))
        {
            GetAvailableProducerAndTrainUnit(Units.FACTORY, targetUnitToTrain);
        }
        else if (Units.FromStarport.Contains(targetUnitToTrain))
        {
            GetAvailableProducerAndTrainUnit(Units.STARPORT, targetUnitToTrain);
        }
    }

    private void GetAvailableProducerAndTrainUnit(uint producerType, uint unitType)
    {
        if (!Controller.CanConstruct(unitType))
        {
            return;
        }
        
        var producers = Controller.GetUnits(producerType);
        foreach (var producer in producers)
        {
            if (Controller.CanConstruct(unitType) && producer.order.AbilityId == 0)
            {
                producer.Train(unitType, false);
                break;
            }
            else if (producer.GetAddonType().HasValue
                && !Units.NeedsTechLab.Contains(unitType)
                && Units.Reactors.Contains(producer.GetAddonType()!.Value)
                && Controller.CanConstruct(unitType)
                && producer.orders.Count < 2)
            {
                producer.Train(unitType, true);
                break;
            }
        }
    }

    private uint GroupByArmyDuplicateUnit(Unit unit)
    {
        // TODO Add also helion too since we can convert them?
        
        if (Units.SiegeTanks.Contains(unit.unitType))
        {
            return Units.SIEGE_TANK;
        }
        else
        {
            return unit.unitType;
        }
    }
}