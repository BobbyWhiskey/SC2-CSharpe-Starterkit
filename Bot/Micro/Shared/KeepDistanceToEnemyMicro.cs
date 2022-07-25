namespace Bot.Micro.Shared;

public static class KeepDistanceToEnemyMicro
{
    private static readonly Dictionary<ulong, ulong> _lastActivationTimeMap = new();

    public static void OnFrame(Unit unit, List<Unit> dangerousUnits, ulong frequency, int rangeToFlee, ulong reservedPeriod)
    {
        var found = _lastActivationTimeMap.TryGetValue(unit.Tag, out var lastActivationTime);
        if (!found && Controller.Frame % frequency == 0)
        {
            var enemy = Controller.GetFirstInRange(unit.Position,
                dangerousUnits
                , rangeToFlee);
            if (enemy != null)
            {
                unit.Move(unit.Position - enemy.Position + unit.Position);
                if (!Controller.IsUnitReserved(unit.Tag))
                {
                    Controller.ReserveUnit(unit.Tag);
                    _lastActivationTimeMap[unit.Tag] = Controller.Frame;
                }
            }
        }

        if (found && lastActivationTime < Controller.Frame - reservedPeriod)
        {
            Controller.ReleaseUnit(unit.Tag);
            _lastActivationTimeMap.Remove(unit.Tag);
        }
        else if (found && unit.Order.AbilityId == 0)
        {
            Controller.ReleaseUnit(unit.Tag);
            _lastActivationTimeMap.Remove(unit.Tag);
        }
    }
}