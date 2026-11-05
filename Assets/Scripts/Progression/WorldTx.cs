using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// All-or-nothing write of several gw.v1 keys. The final values go to a journal key first, then to their
    /// real keys, then the journal is cleared. A journal left by a crash is replayed on the next start, and the
    /// replay is idempotent because it only stores absolute values.
    /// </summary>
    public sealed class WorldTx
    {
        public const string JournalKey = WorldCatalog.SavePrefix + "tx.journal";

        readonly List<(char kind, string key, string value)> _ops = new();
        readonly List<Action> _after = new();

        public static WorldTx Begin() => new WorldTx();

        public WorldTx SetInt(string key, int value)
        {
            Put('i', key, value.ToString());
            return this;
        }

        public WorldTx SetString(string key, string value)
        {
            Put('s', key, value ?? "");
            return this;
        }

        public WorldTx Delete(string key)
        {
            Put('d', key, "");
            return this;
        }

        public WorldTx After(Action action)
        {
            if (action != null) _after.Add(action);
            return this;
        }

        public bool Empty => _ops.Count == 0;

        void Put(char kind, string key, string value)
        {
            for (int i = 0; i < _ops.Count; i++)
            {
                if (_ops[i].key != key) continue;
                _ops[i] = (kind, key, value);
                return;
            }
            _ops.Add((kind, key, value));
        }

        public void Commit()
        {
            if (_ops.Count > 0)
            {
                PlayerPrefs.SetString(JournalKey, Encode(_ops));
                PlayerPrefs.Save();
                Apply(_ops);
                PlayerPrefs.DeleteKey(JournalKey);
                PlayerPrefs.Save();
            }
            for (int i = 0; i < _after.Count; i++) _after[i]();
        }

        /// <summary>Replays an unfinished journal. Returns true when one was found.</summary>
        public static bool Recover()
        {
            if (!PlayerPrefs.HasKey(JournalKey)) return false;
            var ops = Decode(PlayerPrefs.GetString(JournalKey, ""));
            if (ops != null) Apply(ops);
            else Debug.LogWarning("[WorldTx] Journal unreadable; kept for inspection under " + JournalKey + ".bad");
            if (ops == null) PlayerPrefs.SetString(JournalKey + ".bad", PlayerPrefs.GetString(JournalKey, ""));
            PlayerPrefs.DeleteKey(JournalKey);
            PlayerPrefs.Save();
            return true;
        }

        static void Apply(List<(char kind, string key, string value)> ops)
        {
            foreach (var op in ops)
            {
                if (op.kind == 'i') PlayerPrefs.SetInt(op.key, int.Parse(op.value));
                else if (op.kind == 's') PlayerPrefs.SetString(op.key, op.value);
                else if (op.kind == 'd') PlayerPrefs.DeleteKey(op.key);
            }
        }

        static string Encode(List<(char kind, string key, string value)> ops)
        {
            var sb = new StringBuilder();
            foreach (var op in ops)
            {
                sb.Append(op.kind).Append('|')
                  .Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(op.key))).Append('|')
                  .Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(op.value))).Append('\n');
            }
            return sb.ToString();
        }

        static List<(char, string, string)> Decode(string raw)
        {
            try
            {
                var list = new List<(char, string, string)>();
                foreach (var line in raw.Split('\n'))
                {
                    if (string.IsNullOrEmpty(line)) continue;
                    var parts = line.Split('|');
                    if (parts.Length != 3 || parts[0].Length != 1) return null;
                    char kind = parts[0][0];
                    string key = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
                    string value = Encoding.UTF8.GetString(Convert.FromBase64String(parts[2]));
                    if (kind == 'i' && !int.TryParse(value, out _)) return null;
                    if (kind != 'i' && kind != 's' && kind != 'd') return null;
                    list.Add((kind, key, value));
                }
                return list;
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
