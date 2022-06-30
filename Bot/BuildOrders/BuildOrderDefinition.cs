namespace Bot.BuildOrders;

public abstract class BuildOrderDefinition
{
    public ICollection<IBuildStep> buildOrder = new List<IBuildStep>();
    public Dictionary<uint, double> idealUnitRatio = new Dictionary<uint, double>();

    // TODO Add optimal unit ratios

    public BuildOrderDefinition()
    {
    }

    public interface IBuildStep
    {
        
    }

    public class BuildingStep: IBuildStep
    {
        public BuildingStep(uint buildingType)
        {
            BuildingType = buildingType;
        }

        public uint BuildingType { get; set; }
    }
    
    public class ResearchStep:IBuildStep
    {
        public ResearchStep(uint researchId)
        {
            ResearchId = researchId;
        }

        public uint ResearchId { get; set; }
    }

}