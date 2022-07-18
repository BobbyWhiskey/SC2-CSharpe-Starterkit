using SC2APIProtocol;

namespace Bot.Micro;

public class MedicavMicro : IUnitMicro
{
    public void OnFrame()
    {
        var medivacs = Controller.GetUnits(Units.MEDIVAC);

        foreach (var medivac in medivacs)
        {
            var enemy = Controller.GetFirstInRange(medivac.position,
                Controller.GetUnits(Units.ArmyUnits, Alliance.Enemy)
                    .Where(x => Controller.CanUnitAttackAir(x.unitType)).ToList()
                , 8);
            if (enemy != null)
            {
                medivac.Move(medivac.position - enemy.position + medivac.position);
            }
        }
    }
}