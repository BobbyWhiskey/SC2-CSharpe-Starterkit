using SC2APIProtocol;

namespace Bot.Micro;

public class MedicavMicro : IUnitMicro
{
    public void OnFrame()
    {
        var medivacs = Controller.GetUnits(Units.MEDIVAC);
        var dangerousUnits = Controller.GetUnits(Units.All, Alliance.Enemy)
            .Where(x => Controller.CanUnitAttackAir(x.UnitType)).ToList();

        foreach (var medivac in medivacs)
        {
            var enemy = Controller.GetFirstInRange(medivac.Position,
                dangerousUnits
                , 6);
            if (enemy != null)
            {
                medivac.Move(medivac.Position - enemy.Position + medivac.Position);
            }
        }
    }
}