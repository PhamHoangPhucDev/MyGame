using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// One-shot copy of gw.v1 PlayerPrefs from the old Windows company keys into the current one.
    /// Old registry values are left in place. Runs before the first WorldSave read.
    /// </summary>
    public static class CompanySaveMigrate
    {
        const string Flag = WorldCatalog.SavePrefix + "migrated.company";
        const int ErrorMoreData = 234;

        static bool _done;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _done = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot() => Run();

        public static void Run()
        {
            if (_done) return;
            if (Application.platform != RuntimePlatform.WindowsEditor &&
                Application.platform != RuntimePlatform.WindowsPlayer)
            {
                _done = true;
                return;
            }
            if (PlayerPrefs.GetInt(Flag, 0) != 0)
            {
                _done = true;
                return;
            }
            try
            {
                var prefs = new Dictionary<string, Pref>(128);
                ReadHive(@"Software\Unity\UnityEditor\DefaultCompany\My project", true, prefs, false);
                ReadHive(@"Software\Unity\UnityEditor\Hearthvale\Hearthvale", true, prefs, true);
                ReadHive(@"Software\DefaultCompany\My project", false, prefs, true);
                ReadHive(@"Software\Hearthvale\Hearthvale", false, prefs, true);
                foreach (var pair in prefs)
                {
                    if (pair.Key == Flag || pair.Key == "gp.language") continue;
                    if (!Overwrite(pair.Key) && PlayerPrefs.HasKey(pair.Key)) continue;
                    if (pair.Value.Number) PlayerPrefs.SetInt(pair.Key, pair.Value.Int);
                    else PlayerPrefs.SetString(pair.Key, pair.Value.Text ?? "");
                }
                PlayerPrefs.SetInt(Flag, 1);
                PlayerPrefs.Save();
                _done = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Company save migrate failed: " + e.Message);
            }
        }

        /// <summary>Keys the new company name already wrote as defaults, so the old value must replace them.</summary>
        static bool Overwrite(string key)
        {
            const string p = WorldCatalog.SavePrefix;
            if (!key.StartsWith(p, StringComparison.Ordinal)) return false;
            string rest = key.Substring(p.Length);
            if (rest == "home.area" || rest == "home.fixtures" || rest == "hero.hp") return true;
            return rest.StartsWith("home.stock.", StringComparison.Ordinal)
                || rest.StartsWith("mail.", StringComparison.Ordinal)
                || rest.StartsWith("clock.", StringComparison.Ordinal)
                || rest.StartsWith("cd.", StringComparison.Ordinal);
        }

        struct Pref
        {
            public bool Number;
            public int Int;
            public string Text;
        }

        static void ReadHive(string subKey, bool editorHive, Dictionary<string, Pref> into, bool fillOnly)
        {
            if (RegOpenKeyEx(HKCU, subKey, 0, KeyRead, out IntPtr key) != 0) return;
            try
            {
                var name = new StringBuilder(256);
                var data = new byte[4096];
                for (int index = 0; ; )
                {
                    int nameLen = name.Capacity;
                    int dataLen = data.Length;
                    int err = RegEnumValue(key, index, name, ref nameLen, IntPtr.Zero, out int type, data, ref dataLen);
                    if (err == ErrorMoreData)
                    {
                        if (nameLen > name.Capacity) name = new StringBuilder(nameLen);
                        if (dataLen > data.Length) data = new byte[dataLen];
                        continue;
                    }
                    if (err != 0) break;
                    index++;
                    string raw = name.ToString();
                    if (nameLen >= 0 && nameLen < raw.Length) raw = raw.Substring(0, nameLen);
                    string logical = Logical(raw, editorHive);
                    if (logical == null || (fillOnly && into.ContainsKey(logical))) continue;
                    if (type == 4 && dataLen >= 4)
                        into[logical] = new Pref { Number = true, Int = BitConverter.ToInt32(data, 0) };
                    else if (type == 1 || type == 2)
                        into[logical] = new Pref { Text = Encoding.Unicode.GetString(data, 0, dataLen).TrimEnd('\0') };
                    else if (type == 3)
                        into[logical] = new Pref { Text = Encoding.UTF8.GetString(data, 0, dataLen).TrimEnd('\0') };
                }
            }
            finally
            {
                RegCloseKey(key);
            }
        }

        static string Logical(string name, bool editorHive)
        {
            if (!name.StartsWith(WorldCatalog.SavePrefix, StringComparison.Ordinal)) return null;
            if (!editorHive) return name;
            int cut = name.LastIndexOf("_h", StringComparison.Ordinal);
            if (cut <= 0) return name;
            string logical = name.Substring(0, cut);
            if (!uint.TryParse(name.Substring(cut + 2), out uint got)) return null;
            return Hash(logical) == got ? logical : null;
        }

        /// <summary>Unity Editor appends _h plus this hash to each PlayerPrefs name.</summary>
        static uint Hash(string key)
        {
            unchecked
            {
                uint h = 5381;
                byte[] bytes = Encoding.UTF8.GetBytes(key);
                for (int i = 0; i < bytes.Length; i++) h = h * 33 ^ bytes[i];
                return h;
            }
        }

        static readonly IntPtr HKCU = new IntPtr(unchecked((int)0x80000001));
        const int KeyRead = 0x20019;

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegOpenKeyExW")]
        static extern int RegOpenKeyEx(IntPtr hKey, string subKey, int options, int sam, out IntPtr result);

        [DllImport("advapi32.dll", EntryPoint = "RegCloseKey")]
        static extern int RegCloseKey(IntPtr hKey);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegEnumValueW")]
        static extern int RegEnumValue(IntPtr hKey, int index, StringBuilder name, ref int nameLen, IntPtr reserved, out int type, byte[] data, ref int dataLen);
    }
}
