using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    // 計画画面だけの素材。旧版・事件・月報の見た目には適用しない。
    public sealed class OpsPlanningArt : ScriptableObject
    {
        public Sprite round12, round16, round20, round24, round28, stageTop, shadow;
        public Sprite officeBlur, gradient, stageShade, shine, ribbon, tail, petal, snow, hinataShadow;
        public Sprite audit, listen, map, rest, upgrade, menu, tool, star, starMuted, morale, markerBubble;
        public Sprite logoIcon, logoWordmark, titleKeyVisual;
    }
}
