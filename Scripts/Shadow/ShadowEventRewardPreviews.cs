using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;

namespace YogSothothsYard.Scripts.Shadow;

internal static partial class ShadowEvents
{
    private static readonly Func<RelicTrader, IReadOnlyList<RelicModel>> TradeOffers =
        AccessTools.MethodDelegate<Func<RelicTrader, IReadOnlyList<RelicModel>>>(
            AccessTools.PropertyGetter(typeof(RelicTrader), "NewRelics"));

    private static bool PrepareRewardPreviews(EventModel model, EventOption option, Choice choice)
    {
        var tips = new List<IHoverTip>();
        var relics = new List<RelicModel>();
        var offeredRelic = option.Relic?.CanonicalInstance ?? ((choice.EventId, choice.OptionKey) switch
        {
            ("TEA_MASTER", "BONE_TEA") => ModelDb.Relic<BoneTea>(),
            ("TEA_MASTER", "EMBER_TEA") => ModelDb.Relic<EmberTea>(),
            ("TEA_MASTER", "TEA_OF_DISCOURTESY") => ModelDb.Relic<TeaOfDiscourtesy>(),
            ("WELCOME_TO_WONGOS", "MYSTERY_BOX") => ModelDb.Relic<WongosMysteryTicket>(),
            ("THERMAE_NONUPEIPE", "THERMAL_BATHS") => ModelDb.Relic<BathTowel>(),
            ("THERMAE_NONUPEIPE", "PRIVATE_OASIS") => ModelDb.Relic<BathRobes>(),
            ("THERMAE_NONUPEIPE", "DRINK_WATERS") when model is ThermaeNonupeipe bath => bath.DrinkRelic.CanonicalInstance,
            ("RELIC_TRADER", _) => GetTradeOffer(model, choice.OptionKey),
            _ => null
        });
        if (offeredRelic != null) relics.Add(offeredRelic);

        // The exchange shows what the player gives up first, then the reward.
        var exchangedRelic = choice.EventId == "RELIC_TRADER" ? option.HoverTips
            .Select(tip => tip.CanonicalModel).OfType<RelicModel>().FirstOrDefault() : null;
        foreach (var tip in option.HoverTips)
        {
            switch (tip.CanonicalModel)
            {
                case CardModel card when card is LanternKey && model is WarHistorianRepy:
                    if (ErodedLanternKeys(choice.Player).FirstOrDefault() is { } key)
                        tips.Add(HoverTipFactory.FromCard(key));
                    break;
                case RelicModel relic when (choice.Mode & RandomRelic) == Effects.Relic && relic != exchangedRelic:
                    if (!relics.Contains(relic)) relics.Add(relic);
                    break;
                case PotionModel potion when choice.Mode.HasFlag(Effects.Potion):
                    AddPotionPreviews(tips, potion, choice);
                    break;
                default:
                    AddCardOrOriginalTip(tips, tip, choice);
                    break;
            }
        }
        foreach (var relic in relics)
        {
            if (ResolveRelicRank(relic, 1) == 0) return false;
            AddRelicPreviews(tips, relic, choice);
        }
        if (choice is { EventId: "WUPPY_IN_TROUBLE", OptionKey: "HELP_IMMEDIATELY" or "BE_CAUTIOUS" })
        {
            var wuppy = ModelDb.Relic<Wuppy>();
            var nativeTip = new HoverTip(wuppy.Title, new LocString("relics", "WUPPY.eventDescription"));
            tips.RemoveAll(tip => tip.Id == nativeTip.Id || tip.CanonicalModel is Wuppy);
            var preview = (Wuppy)wuppy.ToMutable();
            var hp = model.DynamicVars[choice.OptionKey == "HELP_IMMEDIATELY"
                ? "ImmediateWuppyHp" : "CautiousWuppyHp"].IntValue;
            preview.MaxHp = preview.CurrentHp = hp;
            tips.Add(RelicTip(wuppy, preview.DynamicDescription));
            tips.AddRange(preview.HoverTipsExcludingRelic);
        }
        if (choice is { EventId: "MUSEUM_OF_TORTURE", OptionKey: "BREAK_OUT" })
            option.ThatDecreasesMaxHp(model.DynamicVars.MaxHp.BaseValue);
        if (offeredRelic is FragrantMushroom)
        {
            var possibleHpLoss = RewardRanks(choice, EventRelicTier(offeredRelic, choice))
                .Select(rank => CreateRelicPreview(offeredRelic, ResolveRelicRank(offeredRelic, rank)).DynamicVars.HpLoss.IntValue);
            option.ThatDoesDamage(possibleHpLoss.Min());
        }
        option.HoverTips = tips;
        return true;
    }

    private static void AddCardOrOriginalTip(List<IHoverTip> tips, IHoverTip tip, Choice choice)
    {
        if (tip is not CardHoverTip { Card: var card } ||
            (choice.Mode & (Effects.Card | Effects.Transform | Effects.CardReward)) == 0)
        {
            tips.Add(tip);
            return;
        }
        if (card is Dazed && choice.EventId == "SHRINE_OF_THE_WARLORD") return;
        var rank = EventCardRank(card, choice);
        if (rank == 0)
        {
            tips.Add(tip);
            return;
        }
        choice.CardRanks[card.Id] = rank;
        var preview = card.CanonicalInstance.ToMutable();
        preview.Owner = choice.Player;
        ShadowRanks.Set(preview, rank);
        tips.Add(HoverTipFactory.FromCard(preview));
        if (card is MightyWeapon or MightyShield)
            tips.AddRange(preview.HoverTips);
    }

    private static IEnumerable<int> RewardRanks(Choice choice, int tier) => choice.Rank > 0
        ? [choice.Rank] : ShadowChances.PossibleRanks(ShadowRun.Level(choice.Player), tier);

    private static RelicModel CreateRelicPreview(RelicModel relic, int rank)
    {
        var preview = relic.ToMutable();
        ShadowRanks.Set(preview, rank);
        if (preview is EmberTea tea && rank > 0) tea.CombatsLeft = rank == 1 ? 6 : 8;
        return preview;
    }

    private static void AddRelicPreviews(List<IHoverTip> tips, RelicModel relic, Choice choice)
    {
        var ranks = RewardRanks(choice, EventRelicTier(relic, choice)).Select(rank => ResolveRelicRank(relic, rank));
        foreach (var rank in ranks.Distinct().Order())
        {
            var preview = CreateRelicPreview(relic, rank);
            tips.Add(RelicTip(relic, preview.DynamicDescription));
            tips.AddRange(preview.HoverTipsExcludingRelic);
        }
    }

    private static void AddPotionPreviews(List<IHoverTip> tips, PotionModel potion, Choice choice)
    {
        var ranks = RewardRanks(choice, EventPotionTier(choice)).Select(rank => ResolvePotionRank(potion, rank));
        foreach (var rank in ranks.Where(rank => rank > 0).Distinct().Order())
        {
            var preview = potion.ToMutable();
            ShadowRanks.Set(preview, rank);
            var description = NShadowSelector.Loc("POTION_PREVIEW_" + new string('I', rank));
            description.Add("Effect", preview.HoverTip.Description);
            var tip = new HoverTip(potion.Title, description, preview.Image);
            tip.SetCanonicalModel(potion);
            tips.Add(tip);
            tips.AddRange(preview.ExtraHoverTips);
        }
    }

    private static HoverTip RelicTip(RelicModel relic, LocString description)
    {
        var tip = new HoverTip(relic.Title, description);
        tip.SetCanonicalModel(relic);
        return tip;
    }

    private static RelicModel? GetTradeOffer(EventModel model, string optionKey)
    {
        if (model is not RelicTrader trader) return null;
        var index = optionKey switch { "TOP" => 0, "MIDDLE" => 1, "BOTTOM" => 2, _ => -1 };
        var offers = TradeOffers(trader);
        return (uint)index < (uint)offers.Count ? offers[index].CanonicalInstance : null;
    }
}
