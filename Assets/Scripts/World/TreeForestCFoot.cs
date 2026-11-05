using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Tree_Forest_C is an 80px-wide canvas. TreeCanopyOcclusion sizes the foot
    /// box from that full width, which would block more than Tree_Forest_A.
    /// This restores A's runtime trunk box after the split. Only on Tree_Forest_C.
    /// </summary>
    [DefaultExecutionOrder(80)]
    public class TreeForestCFoot : MonoBehaviour
    {
        void Start()
        {
            var box = GetComponent<BoxCollider2D>();
            if (box == null)
                return;
            box.size = new Vector2(62f / 16f * 0.35f, 24f / 16f * 0.55f);
            box.offset = new Vector2(0f, 24f / 16f * 0.28f);
        }
    }
}
