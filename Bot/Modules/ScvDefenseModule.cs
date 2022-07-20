using SC2APIProtocol;

namespace Bot.Modules;

public class ScvDefenseModule
{
    public void OnFrame()
    {
        if (Controller.Frame % 10 != 0)
        {
            return;
        }

        var ccs = Controller.GetUnits(Units.ResourceCenters);
        var enemyArmy = Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy);
        var scvs = Controller.GetUnits(Units.SCV);

        foreach (var cc in ccs)
        {
            //var closeArmy = enemyArmy.Where(x => (x.Position - cc.Position).LengthSquared() < Math.Pow(10, 2));
            var closeEnemy = Controller.GetFirstInRange(cc.Position, enemyArmy, 10);

            if (closeEnemy != null)
            {
                var scv = scvs
                    .Where(x => x.Order.AbilityId != Abilities.ATTACK)
                    .Where(x => (x.Position - cc.Position).LengthSquared() < Math.Pow(12, 2));
                foreach (var unit in scv)
                {
                    unit.Ability(Abilities.ATTACK, closeEnemy.Position);
                }
            }
            else
            {
                var scv = scvs
                    .Where(x => x.Order.AbilityId == Abilities.ATTACK)
                    .Where(x => (x.Position - cc.Position).LengthSquared() < Math.Pow(12, 2));

                if (scv.Any())
                {
                    var mineral = Controller.GetFirstInRange(cc.Position, Controller.GetUnits(Units.MineralFields, Alliance.Neutral), 12);
                    foreach (var unit in scv)
                    {
                        if (mineral != null)
                        {
                            unit.Ability(Abilities.GATHER_MINERALS, mineral);
                        }
                        else
                        {
                            unit.Ability(Abilities.MOVE, unit.Position);
                        }
                    }
                }
            }
        }
    }
}