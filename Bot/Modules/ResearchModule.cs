namespace Bot;

public class ResearchModule
{
    public void OnFrame()
    {
        var bay = Controller.GetUnits(Units.ENGINEERING_BAY)
            .Where(b => b.Order.AbilityId == 0 && !(b.BuildProgress < 1));
        var armory = Controller.GetUnits(Units.ARMORY)
            .Where(b => b.Order.AbilityId == 0 && !(b.BuildProgress < 1));

        foreach (var unit in bay)
        {
            // TODO MC This is spammy af
            unit.Ability(Abilities.RESEARCH_UPGRADE_INFANTRY_WEAPON);
            unit.Ability(Abilities.RESEARCH_UPGRADE_INFANTRY_ARMOR);
        }

        foreach (var unit in armory)
        {
            unit.Ability(Abilities.RESEARCH_UPGRADE_MECH_GROUND);
        }

        var barrackTechLabs = Controller.GetUnits(Units.BARRACKS_TECHLAB)
            .Where(b => b.Order.AbilityId == 0 && !(b.BuildProgress < 1));

        foreach (var barrackTechLab in barrackTechLabs)
        {
            // TODO MC This is spammy af
            barrackTechLab.Ability(Abilities.RESEARCH_STIMPACK);
            barrackTechLab.Ability(Abilities.RESEARCH_COMBAT_SHIELD);
        }
    }
}