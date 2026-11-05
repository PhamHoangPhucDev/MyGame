using System;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// A spend shows XAC NHAN / HUY first. Accept (the button or E) runs it.
    /// Another click on the ground only keeps the question open.
    /// </summary>
    public static class SpendConfirm
    {
        static string _key;
        static float _until;
        static Action _accept;

        public static string Line { get; private set; }
        public static bool Pending => _key != null && Time.unscaledTime <= _until;

        public static bool Is(string key) => Pending && _key == key;

        public static void Arm(string key, string line, Action accept)
        {
            _key = key;
            Line = line;
            _accept = accept;
            _until = Time.unscaledTime + 12f;
        }

        public static void Accept()
        {
            if (!Pending || _accept == null) return;
            var run = _accept;
            run();
        }

        /// <summary>True once, when this armed key is the one being accepted.</summary>
        public static bool Take(string key)
        {
            if (!Is(key)) return false;
            Clear();
            return true;
        }

        public static void Clear()
        {
            _key = null;
            Line = null;
            _until = 0f;
            _accept = null;
        }
    }
}
