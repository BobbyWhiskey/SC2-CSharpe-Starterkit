using SC2APIProtocol;

namespace Bot.Modules;

public class ScvDefenseModule
{
    public void OnFrame()
    {
        // TODO Check if that was causing bad games
        //return;
        
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
            var closeEnemy = Controller.GetInRange(cc.Position, enemyArmy, 10).ToList();
            var closeArmy = Controller.GetInRange(cc.Position, Controller.GetUnits(Units.ArmyUnits), 10).ToList();

            if (closeEnemy.Any())
            {
                var scv = scvs
                    .Where(x => x.Order.AbilityId != Abilities.ATTACK)
                    .Where(x => (x.Position - cc.Position).LengthSquared() < Math.Pow(12, 2));

                // If the CC or scvs are under attack we make them all attack
                if (cc.Integrity < 0.5)
                {
                    foreach (var unit in scv)
                    {
                        unit.Ability(Abilities.ATTACK, closeEnemy.First().Position);
                    }
                }
            }
            else
            {
                var scv = scvs
                    .Where(x => x.Order.AbilityId == Abilities.ATTACK)
                    .Where(x => (x.Position - cc.Position).LengthSquared() < Math.Pow(12, 2))
                    .ToList();

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