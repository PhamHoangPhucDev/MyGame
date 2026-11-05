using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Art slots for the village hub. Empty slots use the built-in pixel placeholders.
    /// Drop a finished sprite here later; gameplay positions stay put.
    /// </summary>
    public class VillageArtSet : ScriptableObject
    {
        public Sprite[] grass;
        public Sprite tree;
        public Sprite treeSmall;
        public Sprite dirt;
        public Sprite stall;
        public Sprite forge;
        public Sprite crates;
        public Sprite board;
        public Sprite arch;
        public Sprite post;
        public Sprite banner;
        public Sprite well;
    }
}
