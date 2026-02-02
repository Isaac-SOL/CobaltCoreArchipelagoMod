using System.Collections.Generic;
using CobaltCoreArchipelago.GameplayPatches;

namespace CobaltCoreArchipelago.Actions;

public class AAPCardSelect : ACardSelect
{
    public required CardBrowseAPData browseData;
    
    public override Route? BeginWithRoute(G g, State s, Combat c)
    {
        var cardBrowse = base.BeginWithRoute(g, s, c);
        if (cardBrowse != null)
        {
            ModEntry.Instance.Helper.ModData.SetModData(cardBrowse, "AdditionalAPData", browseData);
            CardBrowseListPatch.PrepareCache(s, browseData);
        }
        return cardBrowse;
    }

    public override List<Tooltip> GetTooltips(State s) =>
    [
        new TTGlossary("action.searchCardNew", browseData.filterMode switch
        {
            CardBrowseAPData.FilterMode.UnlockedCardsNotInDeck =>
                ModEntry.Instance.Localizations.Localize(["cardBrowse", "bootOptionUnlockedCardDesc"]),
            CardBrowseAPData.FilterMode.FoundMissingLocations =>
                ModEntry.Instance.Localizations.Localize(["cardBrowse", "eventMissedAPCardDesc"]),
            _ => "!!!missing string!!!"
        })
    ];
}

public class CardBrowseAPData
{
    public FilterMode filterMode;
    
    public enum FilterMode
    {
        UnlockedCardsNotInDeck,
        FoundMissingLocations
    }
}

public class CardSelectAdd : CardAction
{
    public override Route? BeginWithRoute(G g, State s, Combat c)
    {
        if (selectedCard is null) return null;
        var newCard = selectedCard.CopyWithNewId();
        s.deck.Add(newCard);
        DB.currentLocale.strings["__saltyisaac_archipelago_addedUnlockedCard"] =
            ModEntry.Instance.Localizations.Localize(["cardBrowse", "addCardActionDone"]);
        return new ShowCards
        {
            messageKey = "__saltyisaac_archipelago_addedUnlockedCard",
            cardIds = [newCard.uuid]
        };
    }

    public override string? GetCardSelectText(State s) =>
        ModEntry.Instance.Localizations.Localize(["cardBrowse", "addCardActionDoing"]);
}