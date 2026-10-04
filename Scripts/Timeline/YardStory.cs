using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Timeline.Scaffolding;

namespace YogSothothsYard.Scripts;

[RegisterStory]
public sealed class YardStory : ModStoryTemplate
{
    internal const string Key = "yog_sothoths_yard";
    protected override string StoryKey => Key;
}

[RegisterEpoch]
[RegisterStoryEpoch(typeof(YardStory), Order = 0)]
// 门扉时期包含 Invitation0～7；从最后一列之后分配独立列。
[AutoTimelineSlotAfterColumn(EpochEra.Invitation7)]
public sealed class XunRenQiChengEpoch : ModEpochTemplate
{
    public const string EpochId = "YOG_SOTHOTHS_YARD_XUN_REN_QI_CHENG_EPOCH";
    private const string PortraitPath = "res://images/timeline/epoch_portraits/xun_ren_qi_cheng.png";

    public override string Id => EpochId;
    public override string StoryId => YardStory.Key;
    public override EpochAssetProfile AssetProfile => new(
        PackedPortraitPath: PortraitPath,
        BigPortraitPath: PortraitPath
    );

    public override void QueueUnlocks()
    {
        NTimelineScreen.Instance.QueueCharacterUnlock<HaaLouLing>(this);
        SaveManager.Instance.Progress.PendingCharacterUnlock = ModelDb.Character<HaaLouLing>().Id;
    }
}
