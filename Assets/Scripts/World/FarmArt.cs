using System.Collections.Generic;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Runtime slicing of the home-farm sheets (Resources/World/HomeDistrict/Farm, grids from
    /// Docs/WorldArt/HomeDistrict/farm/FARM_LIVESTOCK_HANDOFF.md). Rows count from the top of the image.
    /// </summary>
    public static class FarmArt
    {
        const string Dir = "World/HomeDistrict/Farm/";
        static readonly Vector2 Feet = new Vector2(0.5f, 0f);
        static readonly Vector2 Mid = new Vector2(0.5f, 0.5f);
        static readonly Dictionary<string, Sprite[,]> _cache = new();

        static readonly string[] ItemOrder = { "carrot", "cabbage", "tomato", "pumpkin", "corn", "egg", "duck_egg", "feather" };

        public static Sprite[,] ChickenWhite => Grid("FM_Chicken_White", 6, 3, 16, 16, Feet);
        public static Sprite[,] ChickenBrown => Grid("FM_Chicken_Brown", 6, 3, 16, 16, Feet);
        public static Sprite[,] DuckWhite => Grid("FM_Duck_White", 6, 3, 16, 16, Feet);
        public static Sprite[,] DuckMallard => Grid("FM_Duck_Mallard", 6, 3, 16, 16, Feet);
        public static Sprite[,] Dog => Grid("FM_Dog", 6, 3, 24, 20, Feet);
        public static Sprite[,] Crops => Grid("FM_Crops", 4, 5, 16, 16, Mid);
        public static Sprite[,] Soil => Grid("FM_Soil", 2, 1, 16, 16, Mid);
        public static Sprite[,] UiTiles => Grid("FM_UiTiles", 3, 1, 16, 16, Mid);

        public static Sprite Emote(int i) => Cell(Grid("FM_Emotes", 4, 1, 8, 8, new Vector2(0.5f, 0f)), 0, i);

        static readonly string[] CropOrder = { "carrot", "cabbage", "tomato", "pumpkin", "corn" };

        /// <summary>Crop overlay: row by crop, column = stage 0 seeds .. 3 ripe.</summary>
        public static Sprite Crop(string crop, int stage) => Cell(Crops, System.Array.IndexOf(CropOrder, crop), stage);

        /// <summary>Inventory icon for a farm item id (seed_* shows its crop, as in ui_farm_mode.png), or null.</summary>
        public static Sprite Item(string id)
        {
            if (id != null && id.StartsWith("seed_")) id = id.Substring(5);
            int i = System.Array.IndexOf(ItemOrder, id);
            return i < 0 ? null : Cell(Grid("FM_Items", 8, 1, 16, 16, Mid), 0, i);
        }

        public static bool IsFarmItem(string id) =>
            System.Array.IndexOf(ItemOrder, id) >= 0 || (id != null && id.StartsWith("seed_") && System.Array.IndexOf(CropOrder, id.Substring(5)) >= 0);

        public static Sprite Cell(Sprite[,] grid, int row, int col)
        {
            if (grid == null || row < 0 || col < 0 || row >= grid.GetLength(0) || col >= grid.GetLength(1)) return null;
            return grid[row, col];
        }

        public static Sprite[,] Grid(string file, int cols, int rows, int cw, int ch, Vector2 pivot)
        {
            if (_cache.TryGetValue(file, out var cached) && cached != null && cached[0, 0] != null) return cached;
            var tex = Resources.Load<Texture2D>(Dir + file);
            if (tex == null || tex.width < cols * cw || tex.height < rows * ch)
            {
                Debug.LogWarning("FarmArt: missing or undersized sheet " + file);
                return null;
            }
            var grid = new Sprite[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    var rect = new Rect(c * cw, tex.height - (r + 1) * ch, cw, ch);
                    var s = Sprite.Create(tex, rect, pivot, 16f, 0, SpriteMeshType.FullRect);
                    s.name = file + "_" + r + "_" + c;
                    grid[r, c] = s;
                }
            _cache[file] = grid;
            return grid;
        }
    }
}
