using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Updates sorting order from Y position so lower objects draw in front.
    /// </summary>
    public class SpriteSorter : MonoBehaviour
    {
        public int baseOrder;
        SpriteRenderer _sr;

        void Awake() => _sr = GetComponent<SpriteRenderer>();

        void LateUpdate()
        {
            if (_sr == null) return;
            // Tilemaps sit near 0. North of the origin, -Y*100 goes negative and the
            // body draws under the ground while a fixed-order sword stays visible.
            const int aboveGround = 4000;
            _sr.sortingOrder = baseOrder + aboveGround + Mathf.RoundToInt(-transform.position.y * 100f);
        }
    }
}
