namespace Bot.BuildOrders;

public abstract class BuildOrderDefinition
{
    public ICollection<IBuildStep> buildOrder = new List<IBuildStep>();

    public Dictionary<uint, double> idealUnitFixedNumber = new();
    public Dictionary<uint, double> idealUnitMax = new();

    public Dictionary<uint, double> idealUnitRatio = new();


    // TODO Add optimal unit ratios
}

public interface IBuildStep
{
}

public class WaitStep : IBuildStep
{

    public WaitStep(ulong delaySeconds)
    {
        Delay = (ulong)(delaySeconds * Controller.FRAMES_PER_SECOND);
    }

    public ulong Delay { get; }
    public ulong StartedFrame { get; set; } = 0;
}

public class BuildingStep : IBuildStep
{
    public BuildingStep(uint buildingType)
    {
        BuildingType = buildingType;
    }

    public uint BuildingType { get; set; }
}

public class ResearchStep : IBuildStep
{
    public ResearchStep(uint researchId)
    {
        ResearchId = researchId;
    }

    public uint ResearchId { get; set; }
}