using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Shadow;

namespace YogSothothsYard.Scripts;

[RegisterRelic(typeof(HaaLouLingRelicPool))]
public sealed class LingHunShiQuSuiZhiQing : HaaLouLingRelic, IShadowSoulstone
{
    protected override string ImageName => "ling_hun_shi_qu_sui_zhi_qing";
    public override RelicRarity Rarity => RelicRarity.Starter;
    ShadowSoulstoneKind IShadowSoulstone.SoulstoneKind => ShadowSoulstoneKind.QuSuiZhiQing;
    (Color Deep, Color Light) IShadowSoulstone.ErosionMistColors => (new("259779"), new("b3f4d9"));

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext context, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState) =>
        side == CombatSide.Player && participants.Contains(Owner.Creature) && Owner.Creature.IsAlive &&
        ShadowRun.Level(Owner) > 0 && ShadowRun.Erosion(Owner) >= ShadowRun.ErosionLimit
            ? CreatureCmd.LoseMaxHp(context, Owner.Creature, 1m, cardSource: null)
            : Task.CompletedTask;

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play) =>
        play.Player == Owner ? ShadowRun.AddErosion(Owner, ShadowRanks.CardCost(play.Card), context) : Task.CompletedTask;

    public override Task AfterCardDrawn(PlayerChoiceContext context, CardModel card, bool fromHandDraw) =>
        card.Owner == Owner && ShadowCardRules.TryGet(card, ShadowRanks.Of(card), out var rule)
            ? ShadowRun.AddErosion(Owner, rule.DrawErosion, context) : Task.CompletedTask;

    public override Task AfterCardExhausted(PlayerChoiceContext context, CardModel card, bool causedByEthereal) =>
        card.Owner == Owner && ShadowCardRules.TryGet(card, ShadowRanks.Of(card), out var rule)
            ? ShadowRun.AddErosion(Owner, rule.ExhaustErosion, context) : Task.CompletedTask;

    public override Task AfterPotionUsed(PotionModel potion, Creature? target) =>
        potion.Owner == Owner ? ShadowRun.AddErosion(Owner, ShadowRanks.PotionCost(potion)) : Task.CompletedTask;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player) =>
        player == Owner ? ShadowRelicRules.ChargeTriggers(Owner, "TurnStart", context) : Task.CompletedTask;

    public override Task BeforeCombatStartLate() => ShadowRelicRules.ChargeTriggers(Owner, "Start");

    public override Task AfterEnergyReset(Player player)
    {
        if (player != Owner || ShadowRun.Level(player) == 0 || player.PlayerCombatState?.TurnNumber != 1)
            return Task.CompletedTask;
        var energy = ShadowRun.TakeStoredEnergy(player);
        return energy > 0 && player.GetRelic<IceCream>() is { } relic && ShadowRanks.Of(relic) == 3
            ? PlayerCmd.GainEnergy(energy, player) : Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (ShadowRun.Level(Owner) > 0 && Owner.GetRelic<IceCream>() is { } relic && ShadowRanks.Of(relic) == 3)
            ShadowRun.StoreEnergy(Owner, Owner.PlayerCombatState?.Energy ?? 0);
        return ShadowRelicRules.ChargeTriggers(Owner, "End");
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (ShadowRun.Level(Owner) == 0 || Owner.RunState.CurrentMapPoint?.PointType != MapPointType.Unknown ||
            Owner.GetRelic<JuzuBracelet>() is not { } relic || ShadowRanks.Of(relic) != 2 || !ShadowRun.EnterUnknownFloor(Owner))
            return Task.CompletedTask;
        return ShadowRelicRules.ChargeTriggers(Owner, "Unknown");
    }

    public override Task AfterRestSiteHeal(Player player, bool isMimicked) =>
        player == Owner ? ShadowRelicRules.ChargeTriggers(Owner, "Rest") : Task.CompletedTask;

    public override Task AfterItemPurchased(Player player, MerchantEntry itemPurchased, int goldSpent) =>
        player == Owner && itemPurchased is MerchantCardEntry or MerchantRelicEntry or MerchantPotionEntry
            ? ShadowRelicRules.ChargeTriggers(Owner, "Shop") : Task.CompletedTask;

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy) =>
        card.Owner == Owner && card.Pile?.Type == PileType.Deck
            ? ShadowDeckAdditionPatch.ChargeLens(card) : Task.CompletedTask;
}
