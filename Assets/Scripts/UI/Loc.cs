using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace GameplayPrototype
{
    public enum Lang
    {
        EN = 0,
        VI = 1
    }

    /// <summary>
    /// EN/VI text. The English literal written in code is the key; a missing entry stays English.
    /// Saved in PlayerPrefs "gp.language" (0 EN, 1 VI default).
    /// </summary>
    public static partial class Loc
    {
        public const string PrefKey = "gp.language";

        /// <summary>Tone marks on: the UI font is VT323 (UiKit.PixelFontPath), which has the Vietnamese glyphs.</summary>
        public static bool ViAccents = true;

        static Lang? _current;
        static readonly Dictionary<string, string> _cache = new();

        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _current = null;
            _cache.Clear();
            _patterns = null;
            _vi = null;
            _plainEn = null;
            _enAccent = null;
            Changed = null;
        }

        public static Lang Current
        {
            get
            {
                // Vietnamese is the default; English only when the player picked it.
                if (_current == null) _current = PlayerPrefs.GetInt(PrefKey, (int)Lang.VI) == 1 ? Lang.VI : Lang.EN;
                return _current.Value;
            }
        }

        public static void Set(Lang lang)
        {
            if (Current == lang) return;
            _current = lang;
            _cache.Clear();
            PlayerPrefs.SetInt(PrefKey, (int)lang);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void Toggle() => Set(Current == Lang.EN ? Lang.VI : Lang.EN);

        public static string T(string en)
        {
            if (string.IsNullOrEmpty(en)) return en;
            if (Current == Lang.EN) return EnglishOf(en);
            if (_cache.TryGetValue(en, out var vi)) return vi;
            if (en.IndexOf('\n') >= 0)
            {
                var lines = en.Split('\n');
                for (int i = 0; i < lines.Length; i++) lines[i] = Line(lines[i]);
                vi = string.Join("\n", lines);
            }
            else vi = Line(en);
            if (!ViAccents) vi = Plain(vi);
            if (_cache.Count > 512) _cache.Clear();
            _cache[en] = vi;
            return vi;
        }

        /// <summary>Longest of the English text and its Vietnamese form, for widgets sized once.</summary>
        public static int MaxLength(string en)
        {
            if (string.IsNullOrEmpty(en)) return 0;
            var vi = Lookup(en);
            return vi != null ? Mathf.Max(en.Length, vi.Length) : en.Length;
        }

        /// <summary>Skill name in the current language; key is the unaccented name stored on the ClassKit.</summary>
        public static string Skill(string kitName)
        {
            if (string.IsNullOrEmpty(kitName) || !Skills.TryGetValue(kitName, out var row)) return kitName;
            if (Current != Lang.VI) return row.en;
            return ViAccents ? row.vi : Plain(row.vi);
        }

        /// <summary>Vietnamese without tone marks: "CÀI ĐẶT" becomes "CAI DAT".</summary>
        public static string Plain(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char c in s.Normalize(System.Text.NormalizationForm.FormD))
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
                sb.Append(c == 'đ' ? 'd' : c == 'Đ' ? 'D' : c);
            }
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        /// <summary>One line; joined notices ("a  |  b") are split. An all-caps source gives an all-caps result.</summary>
        static string Line(string en)
        {
            string vi;
            if (en.Contains("  |  "))
            {
                var parts = en.Split(new[] { "  |  " }, StringSplitOptions.None);
                for (int i = 0; i < parts.Length; i++) parts[i] = Word(parts[i]);
                vi = string.Join("  |  ", parts);
            }
            else vi = Word(en);
            return IsCaps(en) ? vi.ToUpperInvariant() : vi;
        }

        /// <summary>Dictionary, then item id, then patterns; unknown text stays as it is.</summary>
        static string Word(string en)
        {
            if (string.IsNullOrEmpty(en)) return en;
            var vi = Lookup(en);
            if (vi != null) return vi;
            if (en.IndexOf('_') > 0)
            {
                var item = WorldContent.FindItem(en);
                if (!string.IsNullOrEmpty(item.id)) return Word(item.displayName);
            }
            foreach (var (re, fmt) in AllPatterns)
            {
                var m = re.Match(en);
                if (m.Success) return fmt(m);
            }
            return en;
        }

        /// <summary>Plain housing keys ("XAY DUNG") become English. Real English is left as written.</summary>
        static string EnglishOf(string en)
        {
            if (en.IndexOf('\n') >= 0)
            {
                var lines = en.Split('\n');
                for (int i = 0; i < lines.Length; i++) lines[i] = EnglishLine(lines[i]);
                return string.Join("\n", lines);
            }
            return EnglishLine(en);
        }

        static string EnglishLine(string en)
        {
            if (en.Contains("  |  "))
            {
                var parts = en.Split(new[] { "  |  " }, StringSplitOptions.None);
                for (int i = 0; i < parts.Length; i++) parts[i] = EnglishWord(parts[i]);
                return string.Join("  |  ", parts);
            }
            return EnglishWord(en);
        }

        static string EnglishWord(string en)
        {
            if (string.IsNullOrEmpty(en)) return en;
            if (PlainEn.TryGetValue(en, out var eng)) return IsCaps(en) ? eng.ToUpperInvariant() : eng;
            foreach (var (re, fmt) in EnAccent)
            {
                var m = re.Match(en);
                if (!m.Success) continue;
                string got = fmt(m);
                return IsCaps(en) ? got.ToUpperInvariant() : got;
            }
            return en;
        }

        static Dictionary<string, string> _plainEn;
        static Dictionary<string, string> PlainEn
        {
            get
            {
                if (_plainEn != null) return _plainEn;
                _plainEn = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var (plain, en, _) in AccentVi) _plainEn[plain] = en;
                return _plainEn;
            }
        }

        static List<(Regex, Func<Match, string>)> _enAccent;
        static List<(Regex, Func<Match, string>)> EnAccent
        {
            get
            {
                if (_enAccent != null) return _enAccent;
                _enAccent = new List<(Regex, Func<Match, string>)>(EnAccentPatterns());
                return _enAccent;
            }
        }

        static string Lookup(string en)
        {
            if (!Vi.TryGetValue(en, out var vi)) return null;
            if (IsCaps(en)) return vi.ToUpperInvariant();
            if (IsCaps(vi) && vi.Length > 1) return vi.Substring(0, 1) + vi.Substring(1).ToLowerInvariant();
            return vi;
        }

        static bool IsCaps(string s)
        {
            bool letter = false;
            foreach (char c in s)
            {
                if (!char.IsLetter(c)) continue;
                letter = true;
                if (char.IsLower(c)) return false;
            }
            return letter;
        }

        static (Regex, Func<Match, string>) P(string pattern, Func<Match, string> fmt)
            => (new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), fmt);

        static Dictionary<string, string> _vi;
        static Dictionary<string, string> Vi
        {
            get
            {
                if (_vi != null) return _vi;
                _vi = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                // Plain-capital housing phrases first, so an existing EN entry with the same key still wins.
                foreach (var (plain, _, vi) in AccentVi) _vi[plain] = vi;
                foreach (var kv in CoreVi) _vi[kv.Key] = kv.Value;
                foreach (var (en, vi) in VillageVi) _vi[en] = vi;
                return _vi;
            }
        }

        static List<(Regex, Func<Match, string>)> _patterns;
        static List<(Regex, Func<Match, string>)> AllPatterns
        {
            get
            {
                if (_patterns != null) return _patterns;
                _patterns = new List<(Regex, Func<Match, string>)>(CorePatterns);
                // Housing plain-caps (XAC NHAN MO 4 PHIA …) must beat greedy village "(.+) (\\d+)".
                _patterns.AddRange(AccentPatterns());
                _patterns.AddRange(VillagePatterns());
                return _patterns;
            }
        }

        static readonly (Regex, Func<Match, string>)[] CorePatterns =
        {
            (new Regex(@"^WAVE (\d+)/(\d+)$"), m => $"ĐỢT {m.Groups[1]}/{m.Groups[2]}"),
            (new Regex(@"^WAVE (\d+) — (.+)$"), m => $"ĐỢT {m.Groups[1]} — {Word(m.Groups[2].Value)}"),
            (new Regex(@"^ENCOUNTER (\d+) — (.+)$"), m => $"TRẬN {m.Groups[1]} — {Word(m.Groups[2].Value)}"),
            (new Regex(@"^LEVEL (\d+)$"), m => $"MÀN {m.Groups[1]}"),
            (new Regex(@"^L(\d+)  (.+)$"), m => $"L{m.Groups[1]}  {Word(m.Groups[2].Value)}"),
            (new Regex(@"^(\d+)  (.+)$"), m => $"{m.Groups[1]}  {Word(m.Groups[2].Value)}"),
            (new Regex(@"^(.*)   Best (\d+)$"), m => $"{m.Groups[1]}   Cao nhất {m.Groups[2]}"),
            (new Regex(@"^Wave (\d+)/(\d+)$"), m => $"Đợt {m.Groups[1]}/{m.Groups[2]}"),
            (new Regex(@"^Kills (\d+)$"), m => $"Hạ gục {m.Groups[1]}"),
            (new Regex(@"^Best Combo x(\d+)$"), m => $"Combo cao nhất x{m.Groups[1]}"),
            (new Regex(@"^Time (.+)$"), m => $"Thời gian {m.Groups[1]}"),
            (new Regex(@"^Level (\d+)$"), m => $"Màn {m.Groups[1]}"),
            (new Regex(@"^Total Enemies Defeated: (\d+)$"), m => $"Tổng quái hạ gục: {m.Groups[1]}"),
            (new Regex(@"^Highest Combo: x(\d+)$"), m => $"Combo cao nhất: x{m.Groups[1]}"),
            (new Regex(@"^Completion Time: (.+)$"), m => $"Hoàn thành trong: {m.Groups[1]}"),
            (new Regex(@"^Score (\d+)$"), m => $"Điểm {m.Groups[1]}"),
            (new Regex(@"^x(\d+) HITS$"), m => $"x{m.Groups[1]} ĐÒN"),
            (new Regex(@"^(.+)  —  PHASE (\d+)$"), m => $"{m.Groups[1]}  —  GIAI ĐOẠN {m.Groups[2]}"),
        };

        static readonly Dictionary<string, string> CoreVi = new()
        {
            // Settings
            { "SETTINGS", "CÀI ĐẶT" },
            { "MUSIC", "NHẠC" },
            { "SFX", "HIỆU ỨNG" },
            { "MUTE TOGGLE", "BẬT/TẮT TIẾNG" },
            { "LANGUAGE: EN", "NGÔN NGỮ: VI" },
            { "CLOSE", "ĐÓNG" },
            // Main menu
            { "GOBLIN CAMP", "TRẠI GOBLIN" },
            { "PIXEL ACTION RPG", "NHẬP VAI HÀNH ĐỘNG" },
            { "VILLAGE", "LÀNG" },
            { "LEVEL SELECT", "CHỌN MÀN" },
            { "SELECT LEVEL", "CHỌN MÀN" },
            { "CONTINUE", "TIẾP TỤC" },
            { "EXIT", "THOÁT" },
            // Level select
            { "PLAY", "CHƠI" },
            { "BACK", "QUAY LẠI" },
            { "LOCKED", "ĐÃ KHÓA" },
            { "SOON", "SẮP CÓ" },
            { "COMING SOON", "SẮP RA MẮT" },
            { "FOREST CAMP", "TRẠI RỪNG" },
            { "GOBLIN FOREST CAMP", "TRẠI RỪNG GOBLIN" },
            { "FOREST TRAIL", "ĐƯỜNG RỪNG" },
            { "ABANDONED CAMP", "TRẠI BỎ HOANG" },
            { "GOBLIN OUTPOST", "TIỀN ĐỒN GOBLIN" },
            { "GOBRIDER STRONGHOLD", "PHÁO ĐÀI GOBRIDER" },
            // Pause / results
            { "PAUSED", "TẠM DỪNG" },
            { "RESUME", "TIẾP TỤC" },
            { "RESTART", "CHƠI LẠI" },
            { "MAIN MENU", "MENU CHÍNH" },
            { "GAME OVER", "THUA CUỘC" },
            { "RETRY", "THỬ LẠI" },
            { "BACK TO MENU", "VỀ MENU" },
            { "LEVEL COMPLETE!", "HOÀN THÀNH MÀN!" },
            { "GAME COMPLETE", "PHÁ ĐẢO!" },
            { "CHAPTER 1 COMPLETE!", "XONG CHƯƠNG 1!" },
            { "NEXT LEVEL", "MÀN TIẾP" },
            { "REPLAY LEVEL", "CHƠI LẠI MÀN" },
            { "VICTORY", "CHIẾN THẮNG" },
            { "Boss Defeated: YES", "Đã hạ Boss: CÓ" },
            // HUD and banners
            { "HERO", "ANH HÙNG" },
            { "CLEAR WAVES TO OPEN EXIT", "DIỆT HẾT ĐỢT ĐỂ MỞ LỐI RA" },
            { "CLEAR ENCOUNTERS — DEFEAT THE WARLORD", "VƯỢT CÁC TRẬN — HẠ THỦ LĨNH" },
            { "CLEAR WAVES", "DIỆT HẾT CÁC ĐỢT" },
            { "WAVE CLEAR", "XONG ĐỢT" },
            { "REACH THE EXIT", "ĐẾN LỐI RA" },
            { "EXIT OPEN", "LỐI RA ĐÃ MỞ" },
            // Wave names
            { "FIRST CONTACT", "CHẠM TRÁN" },
            { "CAMP DEFENSE", "GIỮ TRẠI" },
            { "ARCHER LINE", "HÀNG CUNG THỦ" },
            { "FINAL ASSAULT", "TỔNG TẤN CÔNG" },
            { "TRAIL SCOUTS", "TRINH SÁT" },
            { "FLANK AMBUSH", "PHỤC KÍCH SƯỜN" },
            { "RIDER PATROL", "KỴ BINH TUẦN TRA" },
            { "PATH BLOCKADE", "CHẶN ĐƯỜNG" },
            { "ABANDONED GUARDS", "LÍNH GÁC CŨ" },
            { "SIDE AMBUSH", "PHỤC KÍCH HÔNG" },
            { "RIDER ARRIVAL", "KỴ BINH TỚI" },
            { "FINAL DEFENSE", "PHÒNG TUYẾN CUỐI" },
            { "ELITE GATE", "CỔNG TINH NHUỆ" },
            { "WARLORD", "THỦ LĨNH" },
            { "STRONGHOLD GUARDS", "LÍNH GÁC THÀNH" },
            { "INNER DEFENSE", "PHÒNG TUYẾN TRONG" },
            { "WARLORD BATTLE", "ĐẠI CHIẾN THỦ LĨNH" },
        };

        static readonly Dictionary<string, (string en, string vi)> Skills = new()
        {
            { "Shield Bash", ("Shield Bash", "Đập Khiên") },
            { "Charge", ("Charge", "Xung Phong") },
            { "Whirl Ring", ("Whirl Ring", "Vòng Xoáy") },
            { "Bulwark", ("Bulwark", "Tường Khiên") },
            { "Dawn", ("Dawn", "Bình Minh") },
            { "Nen Dat", ("Ground Slam", "Nện Đất") },
            { "Huc Khien", ("Shield Ram", "Húc Khiên") },
            { "Gam Thach", ("Stone Roar", "Gầm Thạch") },
            { "Tuong Sat", ("Iron Wall", "Tường Sắt") },
            { "Dong Dat", ("Quake", "Động Đất") },
            { "Ten Quat", ("Arrow Fan", "Tên Quạt") },
            { "Nhay Lui", ("Back Leap", "Nhảy Lùi") },
            { "Mua Ten", ("Arrow Rain", "Mưa Tên") },
            { "Buoc Gio", ("Wind Step", "Bước Gió") },
            { "Ten Xuyen Rung", ("Forest Piercer", "Tên Xuyên Rừng") },
            { "Cau Tinh Tu", ("Star Orb", "Cầu Tinh Tú") },
            { "Dich Chuyen", ("Blink", "Dịch Chuyển") },
            { "Vong Phu Chu", ("Rune Circle", "Vòng Phù Chú") },
            { "Khien Ma Luc", ("Mana Shield", "Khiên Ma Lực") },
            { "Mua Sao Bang", ("Meteor Shower", "Mưa Sao Băng") },
            { "Buoc Bong", ("Shadow Step", "Bước Bóng") },
            { "Phi Dao", ("Throwing Knives", "Phi Đao") },
            { "Vu Dieu Luoi Dao", ("Blade Dance", "Vũ Điệu Lưỡi Dao") },
            { "Bom Khoi", ("Smoke Bomb", "Bom Khói") },
            { "Doat Menh", ("Execute", "Đoạt Mệnh") },
        };
    }

    /// <summary>Keeps one UI Text in the current language. Code keeps writing English; this rewrites it.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Text))]
    public class LocText : MonoBehaviour
    {
        Text _text;
        string _source;
        string _shown;

        public static void Attach(Text text)
        {
            if (text != null && text.GetComponent<LocText>() == null) text.gameObject.AddComponent<LocText>();
        }

        void Awake() => _text = GetComponent<Text>();

        void OnEnable()
        {
            Loc.Changed += Refresh;
            Sync();
        }

        void OnDisable() => Loc.Changed -= Refresh;

        void LateUpdate() => Sync();

        void Sync()
        {
            if (_text == null) return;
            string current = _text.text;
            if (_shown != null && current == _shown) return;
            _source = current;
            Apply();
        }

        void Refresh()
        {
            if (_text == null) return;
            if (_source == null || _text.text != _shown) _source = _text.text;
            Apply();
        }

        void Apply()
        {
            _shown = Loc.T(_source);
            if (_text.text != _shown) _text.text = _shown;
        }
    }
}
