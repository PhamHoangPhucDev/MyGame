using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Runtime-safe list of the GAME01 UI kit sprites (Assets/Art/UI/Game01) and the pixel font.
    /// Lives at Resources/UI/UiStyleCatalog so players load it without AssetDatabase.
    /// Rebuild it from the Editor menu Game01/Rebuild UI Style Catalog after adding sprites.
    /// </summary>
    [CreateAssetMenu(menuName = "Game01/UI Style Catalog", fileName = "UiStyleCatalog")]
    public class UiStyleCatalog : ScriptableObject
    {
        public const string ResourcePath = "UI/UiStyleCatalog";

        [Serializable]
        public struct Entry
        {
            public string name;
            public Sprite sprite;
        }

        public Font font;
        public List<Entry> sprites = new List<Entry>();

        Dictionary<string, Sprite> _map;

        public int Count => sprites != null ? sprites.Count : 0;

        public Sprite Find(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName)) return null;
            if (_map == null)
            {
                _map = new Dictionary<string, Sprite>(Count);
                for (int i = 0; i < Count; i++)
                {
                    var e = sprites[i];
                    if (e.sprite != null && !string.IsNullOrEmpty(e.name)) _map[e.name] = e.sprite;
                }
            }
            _map.TryGetValue(spriteName, out var sprite);
            return sprite;
        }

        void OnValidate() => _map = null;
    }
}
