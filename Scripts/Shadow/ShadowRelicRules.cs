using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

internal static class ShadowRelicRules
{
    private static readonly ConditionalWeakTable<DynamicVarSet, Applied> AppliedRules = new();
    private static readonly ConditionalWeakTable<RelicModel, Activation> Activations = new();
    private static readonly AsyncLocal<RelicModel?> Charging = new();
    private static readonly Dictionary<(Type Relic, int Rank), Rule> Rules = new()
    {
        [(typeof(CentennialPuzzle), 1)] = new(3, "AfterDamageReceived", [("Cards", 4m)]),
        [(typeof(MealTicket), 1)] = new(8, "AfterRoomEntered", [("Heal", 20m)]),
        [(typeof(Strawberry), 1)] = new(20, "Pickup", [("MaxHp", 9m)]),
        [(typeof(StrikeDummy), 1)] = new(3, "End", [("ExtraDamage", 4m)]),
        [(typeof(Gorget), 1)] = new(3, "AfterRoomEntered", [("PlatingPower", 5m)]),
        [(typeof(RegalPillow), 1)] = new(10, "AfterRestSiteHeal", [("Heal", 20m)]),
        [(typeof(FestivePopper), 1)] = new(3, "AfterPlayerTurnStart", [("Damage", 12m)]),
        [(typeof(Anchor), 1)] = new(3, "BeforeCombatStart", [("Block", 13m)]),
        [(typeof(BronzeScales), 1)] = new(3, "AfterRoomEntered", [("ThornsPower", 4m)]),
        [(typeof(BookOfFiveRings), 1)] = new(10, "AfterCardChangedPiles", [("Heal", 25m)]),
        [(typeof(WarPaint), 1)] = new(25, "Pickup", [("Cards", 3m)]),
        [(typeof(AmethystAubergine), 1)] = new(3, "AfterModifyingRewards", [("Gold", 20m)]),
        [(typeof(Orichalcum), 1)] = new(2, "BeforeSideTurnEnd", [("Block", 8m)]),
        [(typeof(RippleBasin), 1)] = new(1, "BeforeSideTurnEnd", [("Block", 5m)]),
        [(typeof(Akabeko), 1)] = new(3, "AfterSideTurnStart", [("VigorPower", 10m)]),
        [(typeof(HornCleat), 1)] = new(4, "AfterBlockCleared", [("Block", 19m)]),
        [(typeof(PenNib), 1)] = new(2, "BeforeCardPlayed"),
        [(typeof(Planisphere), 1)] = new(3, "AfterRoomEntered", [("Heal", 6m)]),
        [(typeof(JossPaper), 1)] = new(2, "AfterSideTurnEnd", [("ExhaustAmount", 4m)]),
        [(typeof(OrnamentalFan), 1)] = new(1, "AfterCardPlayed", [("Block", 5m)]),
        [(typeof(LetterOpener), 1)] = new(1, "AfterCardPlayed", [("Damage", 6m)]),
        [(typeof(Pear), 1)] = new(25, "Pickup", [("MaxHp", 13m)]),
        [(typeof(ReptileTrinket), 1)] = new(2, "AfterPotionUsed", [("StrengthPower", 4m)]),
        [(typeof(PetrifiedToad), 1)] = new(0, null),
        [(typeof(Nunchaku), 1)] = new(2, "AfterCardPlayed", [("Cards", 8m)]),
        [(typeof(MercuryHourglass), 1)] = new(1, "AfterPlayerTurnStart", [("Damage", 4m)]),
        [(typeof(Pantograph), 1)] = new(4, "BeforeCombatStart", [("Heal", 33m)]),
        [(typeof(Kusarigama), 1)] = new(1, "AfterCardPlayed", [("Damage", 8m)]),
        [(typeof(MiniatureCannon), 1)] = new(4, "End", [("ExtraDamage", 4m)]),
        [(typeof(TuningFork), 1)] = new(2, "AfterCardPlayed", [("Block", 10m)]),
        [(typeof(Permafrost), 1)] = new(4, "AfterCardPlayed", [("Block", 9m)]),
        [(typeof(EternalFeather), 1)] = new(4, "AfterRoomEntered", [("Heal", 4m)]),
        [(typeof(BowlerHat), 1)] = new(4, "AfterModifyingGoldGained", [("GoldIncrease", 1.33m)]),
        [(typeof(LuckyFysh), 1)] = new(4, "AfterCardChangedPiles", [("Gold", 20m)]),
        [(typeof(ParryingShield), 1)] = new(1, "AfterSideTurnEnd", [("Damage", 8m)]),
        [(typeof(MeatOnTheBone), 1)] = new(5, "AfterCombatVictoryEarly", [("Heal", 16m)]),
        [(typeof(Chandelier), 1)] = new(5, "AfterSideTurnStart", [("Energy", 4m)]),
        [(typeof(CaptainsWheel), 1)] = new(5, "AfterBlockCleared", [("Block", 24m)]),
        [(typeof(OldCoin), 1)] = new(33, "Pickup", [("Gold", 400m)]),
        [(typeof(IntimidatingHelmet), 1)] = new(1, "BeforeCardPlayed", [("Block", 6m)]),
        [(typeof(Pocketwatch), 1)] = new(2, "AfterModifyingHandDraw", [("Cards", 4m)]),
        [(typeof(StoneCalendar), 1)] = new(5, "BeforeSideTurnEnd", [("Damage", 70m)]),
        [(typeof(BeatingRemnant), 1)] = new(2, "AfterModifyingHpLostAfterOsty", [("MaxHpLoss", 16m)]),
        [(typeof(Mango), 1)] = new(33, "Pickup", [("MaxHp", 19m)]),
        [(typeof(TheCourier), 1)] = new(3, "Shop", [("Discount", 25m)]),
        [(typeof(LizardTail), 1)] = new(33, "AfterPreventingDeath", [("Heal", 66m)]),
        [(typeof(BoneTea), 1)] = new(0, null),
        [(typeof(SwordOfStone), 1)] = new(0, null, [("Elites", 4m)]),
        [(typeof(SwordOfStone), 2)] = new(0, null, [("Elites", 3m)]),
        [(typeof(SwordOfStone), 3)] = new(0, null, [("Elites", 2m)]),
        [(typeof(SwordOfJade), 1)] = new(4, "CombatRoom", [("StrengthPower", 4m)]),
        [(typeof(SwordOfJade), 2)] = new(6, "CombatRoom", [("StrengthPower", 5m)]),
        [(typeof(SwordOfJade), 3)] = new(8, "CombatRoom", [("StrengthPower", 7m)]),
        [(typeof(WongosMysteryTicket), 1)] = new(0, null),
        [(typeof(WongosMysteryTicket), 2)] = new(0, null),
        [(typeof(WongosMysteryTicket), 3)] = new(0, null),
        [(typeof(FrozenEgg), 1)] = new(0, null),
        [(typeof(FrozenEgg), 2)] = new(0, null),
        [(typeof(FrozenEgg), 3)] = new(0, null),
        [(typeof(ToxicEgg), 1)] = new(0, null),
        [(typeof(ToxicEgg), 2)] = new(0, null),
        [(typeof(ToxicEgg), 3)] = new(0, null),
        [(typeof(MoltenEgg), 1)] = new(0, null),
        [(typeof(MoltenEgg), 2)] = new(0, null),
        [(typeof(MoltenEgg), 3)] = new(0, null),
        [(typeof(RazorTooth), 1)] = new(0, null),
        [(typeof(RazorTooth), 2)] = new(0, null),
        [(typeof(RazorTooth), 3)] = new(0, null),
        [(typeof(Bellows), 1)] = new(0, null),
        [(typeof(Bellows), 2)] = new(0, null),
        [(typeof(Bellows), 3)] = new(0, null),
        [(typeof(SturdyClamp), 1)] = new(1, "AfterPreventingBlockClear", [("Block", 13m)]),
        [(typeof(BingBong), 3)] = new(10, null),
        [(typeof(DaughterOfTheWind), 3)] = new(1, "AfterCardPlayed", [("Block", 2m)]),
        [(typeof(MrStruggles), 1)] = new(1, null),
        [(typeof(MrStruggles), 2)] = new(1, null),
        [(typeof(MrStruggles), 3)] = new(1, null),
        [(typeof(TheBoot), 1)] = new(1, "AfterModifyingHpLostAfterOsty", [("DamageMinimum", 7m), ("DamageThreshold", 4m)]),
        [(typeof(TheBoot), 2)] = new(1, "AfterModifyingHpLostAfterOsty", [("DamageMinimum", 9m), ("DamageThreshold", 5m)]),
        [(typeof(TheBoot), 3)] = new(1, "AfterModifyingHpLostAfterOsty", [("DamageMinimum", 12m), ("DamageThreshold", 7m)]),
        [(typeof(DreamCatcher), 1)] = new(10, "Rest"),
        [(typeof(DreamCatcher), 2)] = new(12, "Rest"),
        [(typeof(PollinousCore), 1)] = new(2, "AfterModifyingHandDraw", [("Cards", 2m), ("Turns", 3m)]),
        [(typeof(PollinousCore), 2)] = new(1, "AfterModifyingHandDraw", [("Cards", 1m), ("Turns", 1m)]),
        [(typeof(PollinousCore), 3)] = new(3, "AfterModifyingHandDraw", [("Cards", 3m), ("Turns", 2m)]),
        [(typeof(EmberTea), 1)] = new(3, "AfterRoomEntered", [("StrengthPower", 2m)]),
        [(typeof(TeaOfDiscourtesy), 1)] = new(0, null),
        [(typeof(Byrdpip), 1)] = new(0, null),
        [(typeof(ChosenCheese), 1)] = new(3, "AfterCombatEnd", [("MaxHp", 1m)]),
        [(typeof(DarkstonePeriapt), 1)] = new(12, "AfterCardChangedPiles", [("MaxHp", 8m)]),
        [(typeof(MawBank), 1)] = new(3, "AfterRoomEntered", [("Gold", 16m)]),
        [(typeof(FresnelLens), 1)] = new(5, "Enchant", [("NimbleAmount", 3m)]),
        [(typeof(FakeOrichalcum), 1)] = new(2, "BeforeSideTurnEnd", [("Block", 4m)]),
        [(typeof(FakeHappyFlower), 1)] = new(1, "AfterSideTurnStart", [("Turns", 4m)]),
        [(typeof(FakeHappyFlower), 2)] = new(1, "AfterSideTurnStart", [("Turns", 3m)]),
        [(typeof(FakeHappyFlower), 3)] = new(2, "AfterSideTurnStart", [("Turns", 2m)]),
        [(typeof(FakeLeesWaffle), 1)] = new(33, "Pickup", [("Heal", 13m)]),
        [(typeof(FakeLeesWaffle), 2)] = new(48, "Pickup", [("Heal", 17m)]),
        [(typeof(FakeLeesWaffle), 3)] = new(66, "Pickup", [("Heal", 23m)]),
        [(typeof(FakeMango), 1)] = new(33, "Pickup", [("MaxHp", 4m)]),
        [(typeof(FakeAnchor), 1)] = new(3, "Start", [("Block", 5m)]),
        [(typeof(FakeMerchantsRug), 1)] = new(20, "Pickup"),
        [(typeof(LostWisp), 1)] = new(2, "AfterCardPlayed", [("Damage", 10m)]),
        [(typeof(RoyalPoison), 1)] = new(5, "AfterPlayerTurnStart", [("Damage", 5m)]),
        [(typeof(BigMushroom), 1)] = new(25, "Pickup", [("MaxHp", 25m), ("Cards", 2m)]),
        [(typeof(FragrantMushroom), 1)] = new(25, "Pickup", [("HpLoss", 12m), ("Cards", 2m)]),
        [(typeof(BoneTea), 2)] = new(0, null),
        [(typeof(BoneTea), 3)] = new(0, null),
        [(typeof(TeaOfDiscourtesy), 2)] = new(0, null),
        [(typeof(TeaOfDiscourtesy), 3)] = new(0, null),
        [(typeof(Byrdpip), 2)] = new(0, null),
        [(typeof(Byrdpip), 3)] = new(0, null),
        [(typeof(EmberTea), 2)] = new(4, "AfterRoomEntered", [("StrengthPower", 2m)]),
        [(typeof(EmberTea), 3)] = new(5, "AfterRoomEntered", [("StrengthPower", 3m)]),
        [(typeof(ChosenCheese), 2)] = new(4, "AfterCombatEnd", [("MaxHp", 1m)]),
        [(typeof(ChosenCheese), 3)] = new(5, "AfterCombatEnd", [("MaxHp", 2m)]),
        [(typeof(DarkstonePeriapt), 2)] = new(18, "AfterCardChangedPiles", [("MaxHp", 11m)]),
        [(typeof(DarkstonePeriapt), 3)] = new(24, "AfterCardChangedPiles", [("MaxHp", 15m)]),
        [(typeof(MawBank), 2)] = new(4, "AfterRoomEntered", [("Gold", 21m)]),
        [(typeof(MawBank), 3)] = new(5, "AfterRoomEntered", [("Gold", 28m)]),
        [(typeof(FresnelLens), 2)] = new(7, "Enchant", [("NimbleAmount", 4m)]),
        [(typeof(FresnelLens), 3)] = new(10, "Enchant", [("NimbleAmount", 6m)]),
        [(typeof(FakeOrichalcum), 2)] = new(2, "BeforeSideTurnEnd", [("Block", 5m)]),
        [(typeof(FakeOrichalcum), 3)] = new(3, "BeforeSideTurnEnd", [("Block", 7m)]),
        [(typeof(FakeMango), 2)] = new(48, "Pickup", [("MaxHp", 5m)]),
        [(typeof(FakeMango), 3)] = new(66, "Pickup", [("MaxHp", 7m)]),
        [(typeof(FakeAnchor), 2)] = new(4, "Start", [("Block", 7m)]),
        [(typeof(FakeAnchor), 3)] = new(5, "Start", [("Block", 10m)]),
        [(typeof(FakeMerchantsRug), 2)] = new(25, "Pickup"),
        [(typeof(FakeMerchantsRug), 3)] = new(33, "Pickup"),
        [(typeof(LostWisp), 2)] = new(2, "AfterCardPlayed", [("Damage", 13m)]),
        [(typeof(LostWisp), 3)] = new(3, "AfterCardPlayed", [("Damage", 18m)]),
        [(typeof(RoyalPoison), 2)] = new(7, "AfterPlayerTurnStart", [("Damage", 7m)]),
        [(typeof(RoyalPoison), 3)] = new(10, "AfterPlayerTurnStart", [("Damage", 10m)]),
        [(typeof(BigMushroom), 2)] = new(33, "Pickup", [("MaxHp", 33m), ("Cards", 2m)]),
        [(typeof(BigMushroom), 3)] = new(45, "Pickup", [("MaxHp", 40m), ("Cards", 1m)]),
        [(typeof(FragrantMushroom), 2)] = new(33, "Pickup", [("HpLoss", 12m), ("Cards", 3m)]),
        [(typeof(FragrantMushroom), 3)] = new(45, "Pickup", [("HpLoss", 10m), ("Cards", 4m)]),
        [(typeof(ForgottenSoul), 3)] = new(1, nameof(ForgottenSoul.AfterCardExhausted), [("Damage", 2m)]),
        [(typeof(HistoryCourse), 2)] = new(1, null),
        [(typeof(HistoryCourse), 3)] = new(2, null),
        [(typeof(Pendulum), 2)] = new(1, "AfterModifyingHandDraw", [("Turns", 2m), ("Cards", 1m)]),
        [(typeof(Pendulum), 3)] = new(2, "AfterModifyingHandDraw", [("Turns", 3m), ("Cards", 2m)]),
        [(typeof(BagOfMarbles), 3)] = new(5, "Start", [("VulnerablePower", 2m)]),
        [(typeof(Lantern), 3)] = new(5, "Start", [("Energy", 2m)]),
        [(typeof(JuzuBracelet), 2)] = new(3, "Unknown"),
        [(typeof(VenerableTeaSet), 2)] = new(4, "Start", [("Energy", 3m)]),
        [(typeof(VenerableTeaSet), 3)] = new(5, "Start", [("Energy", 4m)]),
        [(typeof(StrikeDummy), 2)] = new(4, "End", [("ExtraDamage", 5m)]),
        [(typeof(StrikeDummy), 3)] = new(5, "End", [("ExtraDamage", 7m)]),
        [(typeof(Anchor), 2)] = new(4, "Start", [("Block", 17m)]),
        [(typeof(Anchor), 3)] = new(5, "Start", [("Block", 23m)]),
        [(typeof(Orichalcum), 2)] = new(2, "BeforeSideTurnEnd", [("Block", 11m)]),
        [(typeof(Orichalcum), 3)] = new(3, "BeforeSideTurnEnd", [("Block", 15m)]),
        [(typeof(Mango), 2)] = new(48, "Pickup", [("MaxHp", 26m)]),
        [(typeof(Mango), 3)] = new(66, "Pickup", [("MaxHp", 36m)]),
        [(typeof(LeesWaffle), 1)] = new(33, "Pickup", [("MaxHp", 9m)]),
        [(typeof(LeesWaffle), 2)] = new(48, "Pickup", [("MaxHp", 12m)]),
        [(typeof(LeesWaffle), 3)] = new(66, "Pickup", [("MaxHp", 16m)]),
        [(typeof(SneckoEye), 2)] = new(7, "Start", [("Cards", 3m)]),
        [(typeof(SneckoEye), 3)] = new(10, "Start", [("Cards", 4m)]),
        [(typeof(RedMask), 3)] = new(5, "Start", [("WeakPower", 2m)]),
        [(typeof(Vajra), 3)] = new(5, "Start", [("StrengthPower", 2m)]),
        [(typeof(HappyFlower), 2)] = new(1, "AfterSideTurnStart", [("Turns", 2m), ("Energy", 1m)]),
        [(typeof(HappyFlower), 3)] = new(2, "AfterSideTurnStart", [("Turns", 3m), ("Energy", 2m)]),
        [(typeof(Whetstone), 2)] = new(25, "Pickup", [("Cards", 3m)]),
        [(typeof(Whetstone), 3)] = new(30, "Pickup", [("Cards", 4m)]),
        [(typeof(BloodVial), 2)] = new(4, "Start", [("Heal", 3m)]),
        [(typeof(BloodVial), 3)] = new(5, "Start", [("Heal", 4m)]),
        [(typeof(PotionBelt), 2)] = new(25, "Pickup", [("PotionSlots", 3m)]),
        [(typeof(PotionBelt), 3)] = new(30, "Pickup", [("PotionSlots", 4m)]),
        [(typeof(OddlySmoothStone), 3)] = new(5, "Start", [("DexterityPower", 2m)]),
        [(typeof(BagOfPreparation), 2)] = new(4, "Start", [("Cards", 3m)]),
        [(typeof(BagOfPreparation), 3)] = new(5, "Start", [("Cards", 4m)]),
        [(typeof(Vambrace), 3)] = new(8, "End"),
        [(typeof(LastingCandy), 3)] = new(8, "CardReward"),
        [(typeof(GremlinHorn), 2)] = new(2, "AfterDeath", [("Energy", 2m), ("Cards", 1m)]),
        [(typeof(GremlinHorn), 3)] = new(2, "AfterDeath", [("Energy", 2m), ("Cards", 2m)]),
        [(typeof(SparklingRouge), 2)] = new(6, "AfterBlockCleared", [("StrengthPower", 2m), ("DexterityPower", 1m)]),
        [(typeof(SparklingRouge), 3)] = new(8, "AfterBlockCleared", [("StrengthPower", 2m), ("DexterityPower", 2m)]),
        [(typeof(PetrifiedToad), 2)] = new(0, null),
        [(typeof(PetrifiedToad), 3)] = new(0, null),
        [(typeof(StoneCracker), 2)] = new(6, "Start", [("Cards", 3m)]),
        [(typeof(StoneCracker), 3)] = new(8, "Start", [("Cards", 4m)]),
        [(typeof(TinyMailbox), 2)] = new(18, "Rest"),
        [(typeof(TinyMailbox), 3)] = new(24, "Rest"),
        [(typeof(Candelabra), 2)] = new(6, "AfterSideTurnStart", [("Energy", 3m)]),
        [(typeof(Candelabra), 3)] = new(8, "AfterSideTurnStart", [("Energy", 4m)]),
        [(typeof(WhiteBeastStatue), 2)] = new(7, "CombatReward"),
        [(typeof(WhiteBeastStatue), 3)] = new(10, "CombatReward"),
        [(typeof(WhiteStar), 3)] = new(10, "CombatReward"),
        [(typeof(IceCream), 3)] = new(10, "End"),
        [(typeof(UnsettlingLamp), 3)] = new(10, "End"),
        [(typeof(UnceasingTop), 3)] = new(1, "Manual"),
        [(typeof(RainbowRing), 2)] = new(2, "AfterCardPlayed", [("StrengthPower", 2m), ("DexterityPower", 2m)]),
        [(typeof(RainbowRing), 3)] = new(3, "AfterCardPlayed", [("StrengthPower", 2m), ("DexterityPower", 2m)]),
        [(typeof(Shovel), 2)] = new(21, "Manual"),
        [(typeof(Shovel), 3)] = new(30, "Manual"),
        [(typeof(CloakClasp), 3)] = new(1, "BeforeSideTurnEnd", [("Block", 2m)]),
        [(typeof(GamblingChip), 3)] = new(10, "End"),
        [(typeof(VexingPuzzlebox), 3)] = new(10, "Start"),
        [(typeof(MummifiedHand), 3)] = new(2, "Manual"),
        [(typeof(Girya), 3)] = new(30, "Manual"),
        [(typeof(Kunai), 2)] = new(1, "AfterCardPlayed", [("Cards", 2m), ("DexterityPower", 1m)]),
        [(typeof(Kunai), 3)] = new(2, "AfterCardPlayed", [("Cards", 3m), ("DexterityPower", 2m)]),
        [(typeof(GamePiece), 3)] = new(2, "AfterCardPlayed", [("Cards", 2m)]),
        [(typeof(Shuriken), 2)] = new(1, "AfterCardPlayed", [("Cards", 2m), ("StrengthPower", 1m)]),
        [(typeof(Shuriken), 3)] = new(2, "AfterCardPlayed", [("Cards", 3m), ("StrengthPower", 2m)]),
        [(typeof(ArtOfWar), 3)] = new(2, "AfterEnergyReset", [("Energy", 2m)]),
        [(typeof(TungstenRod), 3)] = new(2, "AfterModifyingHpLostAfterOsty", [("HpLossReduction", 2m)]),
        [(typeof(PrayerWheel), 3)] = new(10, "CombatReward"),
        [(typeof(HandDrill), 2)] = new(2, "AfterBlockBroken", [("VulnerablePower", 3m)]),
        [(typeof(HandDrill), 3)] = new(3, "Manual", [("VulnerablePower", 3m)]),
        [(typeof(DreamCatcher), 3)] = new(15, "Rest"),
        [(typeof(FakeStrikeDummy), 3)] = new(5, "End", [("ExtraDamage", 2m)]),
        [(typeof(FakeVenerableTeaSet), 3)] = new(5, "Start", [("Energy", 1m)]),
        [(typeof(FakeBloodVial), 3)] = new(5, "Start", [("Heal", 2m)]),
        [(typeof(FakeSneckoEye), 2)] = new(7, "Start"),
        [(typeof(FakeSneckoEye), 3)] = new(10, "Start"),
        [(typeof(Byrdpip), 1)] = new(1, "TurnStart"),
        [(typeof(Byrdpip), 2)] = new(1, "TurnStart"),
        [(typeof(Byrdpip), 3)] = new(2, "TurnStart"),
        [(typeof(Byrdine), 1)] = new(2, "TurnStart"),
        [(typeof(Byrdine), 2)] = new(2, "TurnStart"),
        [(typeof(Byrdine), 3)] = new(3, "TurnStart"),
        [(typeof(ObsidianVice), 3)] = new(2, "BeforeSideTurnEnd", [("Damage", 2m)]),
        [(typeof(Thumbscrew), 3)] = new(4, null),
        [(typeof(TormentousCollar), 2)] = new(6, "BeforeSideTurnStart", [("WeakPower", 3m)]),
        [(typeof(TormentousCollar), 3)] = new(8, "BeforeSideTurnStart", [("WeakPower", 4m)]),
        [(typeof(MightyWings), 1)] = new(2, "AfterDamageGiven", [("DamageThreshold", 25m), ("StrengthPower", 2m)]),
        [(typeof(MightyWings), 2)] = new(3, "AfterDamageGiven", [("DamageThreshold", 25m), ("StrengthPower", 3m)]),
        [(typeof(MightyWings), 3)] = new(4, "AfterDamageGiven", [("DamageThreshold", 21m), ("StrengthPower", 4m)]),
        [(typeof(BathTowel), 1)] = new(5, nameof(BathTowel.AfterPlayerTurnStartLate), [("Heal", 10m)]),
        [(typeof(BathTowel), 2)] = new(7, nameof(BathTowel.AfterPlayerTurnStartLate), [("Heal", 14m)]),
        [(typeof(BathTowel), 3)] = new(10, nameof(BathTowel.AfterPlayerTurnStartLate), [("Heal", 20m)]),
        [(typeof(BathRobes), 3)] = new(10, "BeforeCombatStart"),

        [(typeof(BeautifulBracelet), 1)] = new(33, "Pickup", [("Cards", 5m), ("Swift", 2m)]),
        [(typeof(BeautifulBracelet), 2)] = new(48, "Pickup", [("Cards", 5m), ("Swift", 3m)]),
        [(typeof(BeautifulBracelet), 3)] = new(66, "Pickup", [("Cards", 6m), ("Swift", 4m)]),
        [(typeof(BlessedAntler), 2)] = new(0, null, [("Energy", 2m), ("Cards", 4m)]),
        [(typeof(BlessedAntler), 3)] = new(0, null, [("Energy", 3m), ("Cards", 5m)]),
        [(typeof(BrilliantScarf), 1)] = new(1, "Manual", [("Cards", 4m)]),
        [(typeof(BrilliantScarf), 2)] = new(1, "Manual", [("Cards", 3m)]),
        [(typeof(BrilliantScarf), 3)] = new(2, "TurnStart", [("Cards", 2m)]),
        [(typeof(DelicateFrond), 2)] = new(7, "Start"),
        [(typeof(DelicateFrond), 3)] = new(10, "Start"),
        [(typeof(DiamondDiadem), 1)] = new(5, "Start", [("Block", 26m)]),
        [(typeof(DiamondDiadem), 2)] = new(7, "Start", [("Block", 34m)]),
        [(typeof(DiamondDiadem), 3)] = new(10, "Start", [("Block", 44m)]),
        [(typeof(FurCoat), 1)] = new(33, "Pickup", [("Combats", 10m)]),
        [(typeof(FurCoat), 2)] = new(48, "Pickup", [("Combats", 13m)]),
        [(typeof(FurCoat), 3)] = new(66, "Pickup", [("Combats", 17m)]),
        [(typeof(Glitter), 3)] = new(0, null),
        [(typeof(JewelryBox), 3)] = new(66, "Pickup"),
        [(typeof(SignetRing), 1)] = new(33, "Pickup", [("Gold", 999m)]),
        [(typeof(SignetRing), 2)] = new(48, "Pickup", [("Gold", 1122m)]),
        [(typeof(SignetRing), 3)] = new(66, "Pickup", [("Gold", 1234m)]),
        [(typeof(JiuShiZhenLingFu), 1)] = new(3, "BeforeCombatStart", [("Amount", 15m)]),
        [(typeof(JiuShiZhenLingFu), 2)] = new(4, "BeforeCombatStart", [("Amount", 20m)]),
        [(typeof(JiuShiZhenLingFu), 3)] = new(5, "BeforeCombatStart", [("Amount", 30m)]),
        [(typeof(TongBingChaoShao), 1)] = new(1, "Manual", [("Damage", 8m)]),
        [(typeof(TongBingChaoShao), 2)] = new(2, "Manual", [("Threshold", 10m), ("Damage", 10m)]),
        [(typeof(TongBingChaoShao), 3)] = new(2, "Manual", [("Threshold", 8m), ("Damage", 12m)]),
        [(typeof(SuiShenCaiLan), 1)] = new(4, "BeforeCombatStart", [("Amount", 4m)]),
        [(typeof(SuiShenCaiLan), 2)] = new(6, "BeforeCombatStart", [("Amount", 5m)]),
        [(typeof(SuiShenCaiLan), 3)] = new(8, "BeforeCombatStart", [("Amount", 7m)]),
        [(typeof(TangDaoJiuQiao), 3)] = new(10, "BeforeCombatStart", [("Amount", 2m)]),
        [(typeof(ShuangCengZhengLong), 3)] = new(10, "BeforeCombatStart", [("Amount", 2m)]),
        [(typeof(XiaLuJiaDaoPu), 2)] = new(2, "Manual", [("Threshold", 2m)]),
        [(typeof(XiaLuJiaDaoPu), 3)] = new(3, "Manual", [("Threshold", 2m), ("Amount", 2m)]),
    };

    internal static bool Supports(RelicModel relic, int rank) => Rules.ContainsKey((relic.GetType(), rank));
    internal static bool TryGet(RelicModel relic, out Rule rule) =>
        Rules.TryGetValue((relic.GetType(), ShadowRanks.Of(relic)), out rule);

    internal static void Apply(RelicModel relic, DynamicVarSet variables)
    {
        if (!TryGet(relic, out var rule) || rule.Values == null) return;
        var rank = ShadowRanks.Of(relic);
        if (AppliedRules.TryGetValue(variables, out var applied) && applied.Rank == rank) return;
        AppliedRules.Remove(variables);
        AppliedRules.Add(variables, new Applied(rank));
        foreach (var (name, value) in rule.Values)
            if (variables.TryGetValue(name, out var variable)) variable.BaseValue = value;
    }

    internal static IEnumerable<ModPatchTarget> ActivationTargets()
    {
        var visited = new HashSet<(Type, string)>();
        foreach (var (key, rule) in Rules)
        {
            if (rule.Trigger == null || !visited.Add((key.Relic, rule.Trigger))) continue;
            var method = AccessTools.DeclaredMethod(key.Relic, rule.Trigger);
            if (method?.ReturnType != typeof(Task)) continue;
            yield return new ModPatchTarget(key.Relic, method.Name, method.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
        }
    }

    internal static int FlashCount(RelicModel relic) =>
        Activations.TryGetValue(relic, out var count) ? count.Count : 0;

    internal static void MarkFlash(RelicModel relic)
    {
        if (TryGet(relic, out var rule) && rule.Amount > 0)
            Activations.GetValue(relic, static _ => new Activation()).Count++;
    }

    internal static async Task FinishActivation(Task original, RelicModel relic, int flashes)
    {
        await original;
        if (FlashCount(relic) <= flashes || !TryGet(relic, out var rule)) return;
        if (relic is ChosenCheese)
            await CreatureCmd.Heal(relic.Owner.Creature, ShadowRanks.Of(relic) == 1 ? 1m : 2m);
        await Charge(relic, rule.Amount * (FlashCount(relic) - flashes));
    }

    internal static async Task Charge(RelicModel relic, int amount, PlayerChoiceContext? context = null)
    {
        if (amount <= 0 || Charging.Value == relic) return;
        var previous = Charging.Value;
        Charging.Value = relic;
        try { await ShadowRun.AddErosion(relic.Owner, amount, context); }
        finally { Charging.Value = previous; }
    }

    internal static async Task ChargeTriggers(Player player, string trigger, PlayerChoiceContext? context = null)
    {
        if (ShadowRun.Level(player) == 0) return;
        foreach (var relic in player.Relics.ToArray())
            if (!relic.IsMelted && TryGet(relic, out var rule) && rule.Trigger == trigger)
            {
                if (trigger == "TurnStart") relic.Flash();
                await Charge(relic, rule.Amount, context);
            }
    }

    internal readonly record struct Rule(int Amount, string? Trigger, (string Name, decimal Amount)[]? Values = null);
    private sealed record Applied(int Rank);
    private sealed class Activation { internal int Count; }
}

public sealed class ShadowRelicVarsPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_relic_variables";
    public static ModPatchTarget[] GetTargets() => [new(typeof(RelicModel), "get_DynamicVars", Type.EmptyTypes)];
    public static void Postfix(RelicModel __instance, DynamicVarSet __result)
    {
        if (__instance.IsMutable) ShadowRelicRules.Apply(__instance, __result);
    }
}

public sealed class ShadowRelicFlashPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_relic_flash";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(RelicModel), nameof(RelicModel.Flash), [typeof(IEnumerable<Creature>)])];
    public static void Postfix(RelicModel __instance) => ShadowRelicRules.MarkFlash(__instance);
}

public sealed class ShadowRelicActivationPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_relic_activation";
    public static ModPatchTarget[] GetTargets() =>
        ShadowRelicRules.ActivationTargets().ToArray();

    public static void Prefix(RelicModel __instance, out int __state) => __state = ShadowRelicRules.FlashCount(__instance);

    public static void Postfix(RelicModel __instance, MethodBase __originalMethod, int __state, ref Task __result)
    {
        if (ShadowRelicRules.TryGet(__instance, out var rule) && rule.Trigger == __originalMethod.Name)
            __result = ShadowRelicRules.FinishActivation(__result, __instance, __state);
    }
}

public sealed class ShadowRelicObtainPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_relic_obtain";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(RelicCmd), nameof(RelicCmd.Obtain), [typeof(RelicModel), typeof(Player), typeof(int)])];

    public static void Prefix(RelicModel relic, Player player)
    {
        if (relic is Byrdpip)
        {
            var rank = player.Deck.Cards.OfType<ByrdonisEgg>().Select(ShadowRanks.Of).DefaultIfEmpty().Max();
            if (rank > 0) ShadowRanks.Set(relic, rank);
        }
        ShadowEvents.PrepareRelic(relic, player);
    }
    public static void Postfix(ref Task<RelicModel> __result) => __result = AfterObtain(__result);

    private static async Task<RelicModel> AfterObtain(Task<RelicModel> original)
    {
        var relic = await original;
        if (ShadowRanks.Of(relic) == 0) return relic;
        if (relic is EmberTea tea) tea.CombatsLeft = ShadowRanks.Of(relic) == 1 ? 6 : 8;
        if (ShadowRelicRules.TryGet(relic, out var rule) && rule.Trigger == "Pickup")
            await ShadowRelicRules.Charge(relic, rule.Amount);
        return relic;
    }
}

public sealed class ShadowRelicDescriptionPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_relic_description";
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(RelicModel), "get_DynamicDescription", Type.EmptyTypes),
        new(typeof(RelicModel), "get_DynamicEventDescription", Type.EmptyTypes)
    ];

    public static void Postfix(RelicModel __instance, ref LocString __result)
    {
        if (!ShadowRelicRules.TryGet(__instance, out var rule)) return;
        var rank = ShadowRanks.Of(__instance);
        var descriptionKey = __instance.Id.Entry + ".shadowDescription";
        if (__instance is Byrdpip && rule.Trigger == "TurnStart") descriptionKey += ".evolution";
        if (LocString.GetIfExists("relics", descriptionKey) is { } alternate)
        {
            alternate.AddVariablesFrom(__result);
            __result = alternate;
            __result.Add("Rank", rank == 1 ? "I" : rank == 2 ? "II" : "III");
            __result.Add("MaxHp", rank == 3 ? 2 : 1);
            __result.Add("Heal", rank == 1 ? 1 : 2);
            __result.Add("Copies", rank == 3 ? 2 : 1);
            __result.Add("TurnOffset", rank == 2 ? 2 : 1);
            __result.Add("Hits", rank == 3 ? "两次" : "");
            __result.Add("Choices", rank == 1 ? 4 : 5);
            __result.Add("Selections", rank == 3 ? "两张" : "一张");
            __result.Add("ExtraDraw", rank - 1);
            __result.Add("RareOnly", rank == 3);
            if (__instance is DiamondDiadem) __result.Add("BlockTurn", rank == 3 ? 3 : 2);
            if (__instance is Byrdpip) __result.Add("EvolvePlays", ShadowByrdEvolutionPatch.RequiredPlays(__instance));
        }
        var description = __result;
        if (rule.Amount != 0)
        {
            var timing = rule.Trigger switch
            {
                "Pickup" => "RELIC_PICKUP",
                "CombatRoom" or "Start" => "RELIC_START",
                "End" or "AfterCombatEnd" => "RELIC_END",
                "Shop" => "RELIC_SHOP",
                "Unknown" => "RELIC_UNKNOWN",
                "TurnStart" => "RELIC_TURN_START",
                _ when __instance is BathTowel or Anchor or BronzeScales or Gorget or FestivePopper or Akabeko or
                    JiuShiZhenLingFu or SuiShenCaiLan or TangDaoJiuQiao or ShuangCengZhengLong or SwordOfJade or RoyalPoison => "RELIC_START",
                _ => "RELIC_TRIGGER"
            };
            description = NShadowSelector.Loc(timing);
            description.Add("Description", __result);
            description.Add("Amount", rule.Amount);
        }
        __result = NShadowSelector.Loc("RELIC_RANK_" + new string('I', rank));
        __result.Add("Effect", description);
    }
}

public sealed class ShadowHistoryCoursePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_history_course";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(HistoryCourse), nameof(HistoryCourse.AfterAutoPrePlayPhaseEntered), [typeof(PlayerChoiceContext), typeof(Player)])];

    public static bool Prefix(HistoryCourse __instance, PlayerChoiceContext choiceContext, Player player, ref Task __result)
    {
        var rank = ShadowRanks.Of(__instance);
        if (rank == 0) return true;
        __result = PlayLastCard(__instance, choiceContext, player, rank);
        return false;
    }

    private static async Task PlayLastCard(HistoryCourse relic, PlayerChoiceContext context, Player player, int rank)
    {
        if (player != relic.Owner || player.PlayerCombatState?.TurnNumber <= 1) return;
        var card = CombatManager.Instance.History.CardPlaysFinished.LastOrDefault(entry =>
            entry.CardPlay.Player == player && entry.HappenedLastPlayerTurn(player) &&
            entry.CardPlay.Card.Type is (CardType.Attack or CardType.Skill or CardType.Power) &&
            !entry.CardPlay.Card.IsDupe)?.CardPlay.Card;
        if (card == null) return;
        relic.Flash();
        for (var i = 0; i < rank - 1 && player.Creature.IsAlive; i++)
            await CardCmd.AutoPlay(context, card.CreateDupe(player), null);
        await ShadowRelicRules.Charge(relic, rank - 1, context);
    }
}

public sealed class ShadowPetrifiedToadPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_petrified_toad";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(PetrifiedToad), nameof(PetrifiedToad.BeforeCombatStartLate), Type.EmptyTypes)];

    public static bool Prefix(PetrifiedToad __instance, ref Task __result)
    {
        var rank = ShadowRanks.Of(__instance);
        if (!ShadowPotionRules.Supports(ModelDb.Potion<PotionShapedRock>(), rank)) return true;
        __instance.Flash();
        var potion = ModelDb.Potion<PotionShapedRock>().ToMutable();
        ShadowRanks.Set(potion, rank);
        __result = PotionCmd.TryToProcure(potion, __instance.Owner);
        return false;
    }
}
