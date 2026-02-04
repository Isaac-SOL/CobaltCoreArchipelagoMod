using CobaltCoreArchipelago.GameplayPatches;

namespace CobaltCoreArchipelago.Actions;

public class AAPArtifactOffering : AArtifactOffering
{
    public required ArtifactOfferingAPData data;
    
    public override Route? BeginWithRoute(G g, State s, Combat c)
    {
        timer = 0.0;
        ArtifactOfferingPatch.nextOverridingData = data;
        return new ArtifactReward
        {
            artifacts = ArtifactReward.GetOffering(g.state, amount, limitDeck, limitPools),
            canSkip = canSkip
        };
    }
}

public class ArtifactOfferingAPData
{
    public FilterMode filterMode;
    
    public enum FilterMode
    {
        UnlockedArtifactsNotInDeck,
        FoundMissingLocations
    }
}