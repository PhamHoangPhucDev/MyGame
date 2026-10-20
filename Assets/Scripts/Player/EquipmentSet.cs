using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// R2C: one swappable gear + anchor set for EquipmentVisual (a class's default weapons, or a skin).
    /// Anchor table rows: key,main_x,main_y,main_dir,main_order,hand_x,hand_y,off_x,off_y,off_view,off_order,slash,slash_order[,off_dir]
    /// Keys may carry a combo step ("Attack_Down_1@2"); slashFrames may hold one block of 6 per combo step.
    /// </summary>
    [CreateAssetMenu(menuName = "Gameplay Prototype/Equipment Set", fileName = "EquipmentSet")]
    public class EquipmentSet : ScriptableObject
    {
        public TextAsset anchorTable;
        [Tooltip("Main hand, 8 poses: R, UR, U, UL, L, DL, D, DR. Pivot = hand.")]
        public Sprite[] weaponPoses = new Sprite[8];
        [Tooltip("Optional off-hand weapon, 8 poses (used when the row has off_dir >= 0).")]
        public Sprite[] offPoses = new Sprite[0];
        [Tooltip("Optional shield: Down, Up, Side (used when off_dir < 0).")]
        public Sprite[] shieldViews = new Sprite[0];
        public Sprite hand;
        [Tooltip("Blocks of 6 (Down0, Down1, Up0, Up1, Side0, Side1), one block per combo step.")]
        public Sprite[] slashFrames = new Sprite[0];
        public bool replaceLegacyRSlash = true;
    }
}
