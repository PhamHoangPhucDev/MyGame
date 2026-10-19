using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Sets the Game View to a fixed test resolution for dual-aspect QA.
    /// Does not call Screen.SetResolution — Editor only.
    /// </summary>
    public static class AspectGameViewHelper
    {
        const BindingFlags Inst = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>Selects (adding if needed) a FixedResolution Game View size. Returns true when the
        /// Game View target actually reports width x height afterwards.</summary>
        public static bool SetSize(int width, int height)
        {
            try
            {
                var asm = typeof(EditorWindow).Assembly;
                var gvType = asm.GetType("UnityEditor.GameView");
                var sizesType = asm.GetType("UnityEditor.GameViewSizes");
                if (gvType == null || sizesType == null) return false;

                // GameViewSizes is a ScriptableSingleton<GameViewSizes>; "instance" lives on the generic base.
                var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType)
                    .GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
                var sizesInstance = singleton?.GetValue(null, null);
                if (sizesInstance == null) return false;

                var currentGroup = sizesType.GetProperty("currentGroup")?.GetValue(sizesInstance, null);
                if (currentGroup == null) return false;

                int index = FindOrAddFixedSize(currentGroup, currentGroup.GetType(), width, height);
                if (index < 0) return false;

                var gameView = EditorWindow.GetWindow(gvType, false, null, false);
                // SizeSelectionCallback is what the Game View dropdown calls; it applies the size and
                // refreshes the render target. Setting selectedSizeIndex alone does not.
                var select = gvType.GetMethod("SizeSelectionCallback", Inst);
                if (select != null)
                    select.Invoke(gameView, new object[] { index, null });
                else
                {
                    gvType.GetProperty("selectedSizeIndex", Inst)?.SetValue(gameView, index, null);
                    gvType.GetMethod("UpdateZoomAreaAndParent", Inst)?.Invoke(gameView, null);
                }
                gameView.Repaint();

                var target = TargetSize();
                bool ok = target.x == width && target.y == height;
                if (!ok)
                    Debug.LogWarning($"[Aspect] Game View asked for {width}x{height} but reports {target.x}x{target.y} (slot {index}).");
                return ok;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Aspect] Game View size set failed: " + e.Message);
                return false;
            }
        }

        /// <summary>Current Game View render target size (what Screen.width/height report in Play Mode).</summary>
        public static Vector2Int TargetSize()
        {
            var m = typeof(EditorWindow).Assembly.GetType("UnityEditor.PlayModeView")
                ?.GetMethod("GetMainPlayModeViewTargetSize", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (m == null) return Vector2Int.zero;
            var v = (Vector2)m.Invoke(null, null);
            return new Vector2Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
        }

        static int FindOrAddFixedSize(object group, Type groupType, int width, int height)
        {
            var asm = typeof(EditorWindow).Assembly;
            var sizeTypeEnum = asm.GetType("UnityEditor.GameViewSizeType");
            if (sizeTypeEnum == null) return -1;
            var fixedRes = Enum.Parse(sizeTypeEnum, "FixedResolution");

            var getTotal = groupType.GetMethod("GetTotalCount");
            var getSize = groupType.GetMethod("GetGameViewSize");
            int total = getTotal != null ? (int)getTotal.Invoke(group, null) : 0;
            for (int i = 0; i < total; i++)
            {
                var size = getSize.Invoke(group, new object[] { i });
                var t = size.GetType();
                // Only FixedResolution entries: an AspectRatio entry with the same numbers is a ratio, not pixels.
                if (!Equals(t.GetProperty("sizeType").GetValue(size, null), fixedRes)) continue;
                if ((int)t.GetProperty("width").GetValue(size, null) == width &&
                    (int)t.GetProperty("height").GetValue(size, null) == height)
                    return i;
            }

            var sizeType = asm.GetType("UnityEditor.GameViewSize");
            var ctor = sizeType?.GetConstructor(new[] { sizeTypeEnum, typeof(int), typeof(int), typeof(string) });
            if (ctor == null) return -1;
            var newSize = ctor.Invoke(new object[] { fixedRes, width, height, $"R1 {width}x{height}" });
            groupType.GetMethod("AddCustomSize")?.Invoke(group, new[] { newSize });
            total = getTotal != null ? (int)getTotal.Invoke(group, null) : total + 1;
            return total - 1;
        }
    }
}
