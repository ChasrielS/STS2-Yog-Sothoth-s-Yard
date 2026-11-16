using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Potions;
using STS2RitsuLib;
using STS2RitsuLib.Models.Capabilities;

namespace YogSothothsYard.Scripts.Shadow;

internal static class ShadowRanks
{
    private static ModelSavedData<CardModel, ShadowRank> _cards = null!;
    private static ModelSavedData<RelicModel, ShadowRank> _relics = null!;
    private static ModelSavedData<PotionModel, ShadowRank> _potions = null!;
    private static ModelSavedData<RelicModel, ShadowBathRobesUses> _bathRobesUses = null!;

    internal static void Register()
    {
        var store = RitsuLibFramework.GetModelSavedDataStore(Entry.ModId);
        var options = new ModelSavedDataOptions { ClonePolicy = ModelSavedDataClonePolicy.Copy };
        _cards = store.Register<CardModel, ShadowRank>("card_erosion", static () => new ShadowRank(), options);
        _relics = store.Register<RelicModel, ShadowRank>("relic_erosion", static () => new ShadowRank(), options);
        _potions = store.Register<PotionModel, ShadowRank>("potion_erosion", static () => new ShadowRank(), options);
        _bathRobesUses = store.Register<RelicModel, ShadowBathRobesUses>("bath_robes_uses", static () => new ShadowBathRobesUses(), options);
    }

    internal static int Of(CardModel card) =>
        card.IsMutable && _cards.TryGet(card, out var value) ? value.Rank : 0;

    internal static int Of(RelicModel relic) =>
        relic.IsMutable && _relics.TryGet(relic, out var value) ? value.Rank : 0;

    internal static int Of(PotionModel potion) =>
        potion.IsMutable && _potions.TryGet(potion, out var value) ? value.Rank : 0;

    internal static void Set(CardModel card, int rank) => _cards.Set(card, new ShadowRank { Rank = rank });
    internal static void Set(RelicModel relic, int rank) => _relics.Set(relic, new ShadowRank { Rank = rank });
    internal static void Set(PotionModel potion, int rank) => _potions.Set(potion, new ShadowRank { Rank = rank });

    internal static int BathRobesUses(RelicModel relic) =>
        _bathRobesUses.TryGet(relic, out var state) ? state.Count : 0;

    internal static void RecordBathRobesUse(RelicModel relic) => _bathRobesUses.Modify(relic, state => state.Count++);

    internal static int CardCost(CardModel card) => CardCost(card, Of(card));

    internal static int CardCost(CardModel card, int rank)
    {
        if (rank <= 0) return 0;
        if (ShadowCardRules.TryGet(card, rank, out var rule) && rule.Cost is { } cost) return cost;
        if (card is ByrdSwoop) return 0;
        if (card is Clash or Outmaneuver or Rebound or Stack) return rank;
        if (card.Type is CardType.Status or CardType.Curse || card.Rarity == CardRarity.Quest) return 0;
        return card.Rarity switch
        {
            CardRarity.Rare or CardRarity.Ancient => rank switch { 1 => 3, 2 => 5, _ => 8 },
            CardRarity.Uncommon or CardRarity.Event => rank switch { 1 => 2, 2 => 3, _ => 5 },
            _ => rank
        };
    }

    internal static int PotionCost(PotionModel potion)
    {
        var rank = Of(potion);
        if (rank <= 0) return 0;
        var rarity = potion is GlowwaterPotion ? PotionRarity.Rare : potion.Rarity;
        return rarity switch
        {
            PotionRarity.Rare => rank switch { 1 => 16, 2 => 20, _ => 25 },
            PotionRarity.Uncommon => rank switch { 1 => 12, 2 => 15, _ => 18 },
            _ => rank switch { 1 => 8, 2 => 10, _ => 12 }
        };
    }
}

public sealed class ShadowRank
{
    public int Rank { get; set; }
}

public sealed class ShadowBathRobesUses
{
    public int Count { get; set; }
}
