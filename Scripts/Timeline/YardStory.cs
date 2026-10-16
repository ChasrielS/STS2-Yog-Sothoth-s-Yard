using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Timeline;
using STS2RitsuLib.Timeline.Scaffolding;

namespace YogSothothsYard.Scripts;

[RegisterStory]
public sealed class YardStory : ModStoryTemplate
{
    internal const string Key = "yog_sothoths_yard";
    protected override string StoryKey => Key;

    internal static void RegisterTimelineLayout()
    {
        // 在门扉时期之后分配独立列；原版位置 0 在最下方。
        ModTimelineLayoutRegistry.RegisterAutoTimelineSlotAfterEraColumn(
            typeof(YiTongGuiJiaEpoch), EpochEra.Invitation7, Entry.ModId);
        var era = new YiTongGuiJiaEpoch().Era;
        ModTimelineLayoutRegistry.RegisterTimelineSlot(typeof(ShenZhiZhongEpoch), era, 4, Entry.ModId);
        ModTimelineLayoutRegistry.RegisterTimelineSlot(typeof(YongHengZhiWenEpoch), era, 3, Entry.ModId);
        ModTimelineLayoutRegistry.RegisterTimelineSlot(typeof(XunRenQiChengEpoch), era, 2, Entry.ModId);
        ModTimelineLayoutRegistry.RegisterTimelineSlot(typeof(YinYingEpoch), era, 1, Entry.ModId);
    }
}

public abstract class YardEpoch : ModEpochTemplate
{
    // 尚未开放的节点暂用现有插画，锁定时沿用原版的模糊显示。
    private const string PortraitPath = "res://images/timeline/epoch_portraits/xun_ren_qi_cheng.png";

    public override string StoryId => YardStory.Key;
    public override EpochAssetProfile AssetProfile => new(
        PackedPortraitPath: PortraitPath,
        BigPortraitPath: PortraitPath
    );
}

// 占位节点只注册槽位，不注册获得条件，暂时无法解锁。
[RegisterEpoch]
[RegisterStoryEpoch(typeof(YardStory), Order = 0)]
public sealed class ShenZhiZhongEpoch : YardEpoch
{
    public override string Id => "YOG_SOTHOTHS_YARD_SHEN_ZHI_ZHONG_EPOCH";
}

[RegisterEpoch]
[RegisterStoryEpoch(typeof(YardStory), Order = 1)]
public sealed class YongHengZhiWenEpoch : YardEpoch
{
    public override string Id => "YOG_SOTHOTHS_YARD_YONG_HENG_ZHI_WEN_EPOCH";
}

[RegisterEpoch]
[RegisterStoryEpoch(typeof(YardStory), Order = 2)]
public sealed class XunRenQiChengEpoch : YardEpoch
{
    public const string EpochId = "YOG_SOTHOTHS_YARD_XUN_REN_QI_CHENG_EPOCH";

    public override string Id => EpochId;

    public override void QueueUnlocks()
    {
        NTimelineScreen.Instance.QueueCharacterUnlock<HaaLouLing>(this);
        SaveManager.Instance.Progress.PendingCharacterUnlock = ModelDb.Character<HaaLouLing>().Id;
    }
}

[RegisterEpoch]
[RegisterStoryEpoch(typeof(YardStory), Order = 3)]
public sealed class YinYingEpoch : YardEpoch
{
    public override string Id => "YOG_SOTHOTHS_YARD_YIN_YING_EPOCH";
}

[RegisterEpoch]
[RegisterStoryEpoch(typeof(YardStory), Order = 4)]
public sealed class YiTongGuiJiaEpoch : YardEpoch
{
    public override string Id => "YOG_SOTHOTHS_YARD_YI_TONG_GUI_JIA_EPOCH";
}
