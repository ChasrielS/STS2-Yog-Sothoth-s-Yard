using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

public abstract class KnifeFormCard<T>(int amount) : HaaLouLingCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    where T : KnifeFormPower
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];
    protected override bool UsesKnife => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", amount)];
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Apply<T>(context, DynamicVars["Amount"].IntValue);
    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}

[RegisterCard(typeof(TokenCardPool))]
public sealed class QiShi() : KnifeFormCard<QiShiPower>(1);

[RegisterCard(typeof(TokenCardPool))]
public sealed class ChengShi() : KnifeFormCard<ChengShiPower>(1);

[RegisterCard(typeof(TokenCardPool))]
public sealed class ZhuanShi() : KnifeFormCard<ZhuanShiPower>(2);

[RegisterCard(typeof(TokenCardPool))]
public sealed class HeShi() : KnifeFormCard<HeShiPower>(2);
