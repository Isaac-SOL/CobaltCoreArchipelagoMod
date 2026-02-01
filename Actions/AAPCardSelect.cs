using System.Collections.Generic;

namespace CobaltCoreArchipelago.Actions;

public class AAPCardSelect : ACardSelect
{
    public AAPCardSelect()
    {
        browseSource = CardBrowse.Source.Deck;
    }
    
    public override Route? BeginWithRoute(G g, State s, Combat c)
    {
        var cardBrowse = base.BeginWithRoute(g, s, c);
        DB.currentLocale.strings["__saltyisaac_archipelago_pickUnlockedCard"] = "Pick an unlocked card to add to your deck.";
        if (cardBrowse is { } cardBrowseNotNull)
        {
            ModEntry.Instance.Helper.ModData.SetModData(cardBrowseNotNull, "SelectAPUnlockedCard", true);
        }
        return cardBrowse;
    }
}

public class CardSelectAdd : CardAction
{
    public override Route? BeginWithRoute(G g, State s, Combat c)
    {
        if (selectedCard is not null) s.deck.Add(selectedCard.CopyWithNewId());
        return null;
    }

    public override string? GetCardSelectText(State s)
    {
        return "Select a card to add to your deck.";
    }
}