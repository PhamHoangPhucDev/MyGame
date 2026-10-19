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
        public static void SetSize(int width, int height)
        {
            try
            {
                var gvType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                var sizesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizes");
                if (gvType == null || sizesType == null) return;

                var singleton = sizesType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
                var sizesInstance = singleton?.GetValue(null, null);
                if (sizesInstance == null) return;

                var currentGroup = sizesType.GetProperty("currentGroup")?.GetValue(sizesInstance, null);
                if (currentGroup == null) return;

                var groupType = currentGroup.GetType();
                int index = FindOrAddCustomSize(currentGroup, groupType, width, height);
                if (index < 0) return;

                var gameView = EditorWindow.GetWindow(gvType, false, null, false);
                var sizeProp = gvType.GetProperty("selectedSizeIndex",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                sizeProp?.SetValue(gameView, index, null);
                gvType.GetMethod("UpdateZoomAreaAndParent", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(gameView, null);
                gameView.Repaint();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Aspect] Game View size set failed: " + e.Message);
            }
        }

        static int FindOrAddCustomSize(object group, Type groupType, int width, int height)
        {
            var getTotal = groupType.GetMethod("GetTotalCount");
            int total = getTotal != null ? (int)getTotal.Invoke(group, null) : 0;
            var getSize = groupType.GetMethod("GetGameViewSize");
            for (int i = 0; i < total; i++)
            {
                var size = getSize.Invoke(group, new object[] { i });
                var w = (int)size.GetType().GetProperty("width").GetValue(size, null);
                var h = (int)size.GetType().GetProperty("height").GetValue(size, null);
                if (w == width && h == height) return i;
            }

            var sizeType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSize");
            var sizeTypeEnum = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizeType");
            if (sizeType == null || sizeTypeEnum == null) return -1;
            var fixedRes = Enum.Parse(sizeTypeEnum, "FixedResolution");
            var ctor = sizeType.GetConstructor(new[] { sizeTypeEnum, typeof(int), typeof(int), typeof(string) });
            var newSize = ctor.Invoke(new object[] { fixedRes, width, height, $"R1 {width}x{height}" });
            groupType.GetMethod("AddCustomSize")?.Invoke(group, new[] { newSize });
            total = getTotal != null ? (int)getTotal.Invoke(group, null) : total + 1;
            return total - 1;
        }
    }
}
