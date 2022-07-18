using SC2APIProtocol;

namespace Bot.Micro;

public class MedicavMicro : IUnitMicro
{
    public void OnFrame()
    {
        var medivacs = Controller.GetUnits(Units.MEDIVAC);
        var dangerousUnits = Controller.GetUnits(Units.All, Alliance.Enemy)
            .Where(x => Controller.CanUnitAttackAir(x.unitType)).ToList();

        foreach (var medivac in medivacs)
        {
            var enemy = Controller.GetFirstInRange(medivac.position,
                dangerousUnits
                , 6);
            if (enemy != null)
            {
                medivac.Move(medivac.position - enemy.position + medivac.position);
            }
        }
    }
}