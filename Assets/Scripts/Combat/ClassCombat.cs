using UnityEngine;
using UnityEngine.InputSystem;

namespace GameplayPrototype
{
    /// <summary>
    /// R2C Sandbox: owns the class kits (F1..F5 = Aren, Brom, Lyra, Selene, Vex) and hands the active one to
    /// PlayerCombat, which keeps the single combat path (input, cooldowns, hotbar events, PlayerVisual facing).
    /// Applies the kit's base stats on top of the scene values.
    /// Campaign Level 1-4 has one instance on the scene Player. The Player_Aren prefab does not.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class ClassCombat : MonoBehaviour
    {
        [Tooltip("Slot i = key F(i+1). Empty slot = class not built yet.")]
        [SerializeField] ClassKit[] kits = new ClassKit[5];
        [SerializeField] string[] slotNames = { "Aren", "Brom", "Lyra", "Selene", "Vex" };
        [SerializeField] int startSlot;
        [SerializeField] bool showLabel = true;
        [SerializeField] ClassUiIcons uiIcons;

        PlayerCombat _combat;
        PlayerMovement _movement;
        Health _health;
        int _baseMaxHp;
        int _active = -1;
        string _toast;
        float _toastUntil;

        public ClassKit Active => _active >= 0 ? kits[_active] : null;
        public int ActiveSlot => _active;
        /// <summary>Class whose hotbar icons are showing. Updates on F1–F5 even when that kit is not built yet.</summary>
        public string IconClassId { get; private set; } = "Aren";
        public event System.Action IconClassChanged;

        void Awake()
        {
            _combat = GetComponent<PlayerCombat>();
            _movement = GetComponent<PlayerMovement>();
            _health = GetComponent<Health>();
            _baseMaxHp = _health != null ? _health.MaxHp : 0;
            if (uiIcons != null) ClassUiIcons.SetActive(uiIcons);
        }

        void OnDestroy()
        {
            if (uiIcons != null && ClassUiIcons.Active == uiIcons)
                ClassUiIcons.SetActive(null);
            if (_active >= 0 && _active < kits.Length && kits[_active] != null
                && kits[_active].slot2 != null
                && kits[_active].slot2.behaviour == SkillBehaviour.ShieldBash)
                ArenKitVfx.PreferDesignedSkills = false;
        }

        void Start() => Select(startSlot);

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.f1Key.wasPressedThisFrame) Select(0);
            if (kb.f2Key.wasPressedThisFrame) Select(1);
            if (kb.f3Key.wasPressedThisFrame) Select(2);
            if (kb.f4Key.wasPressedThisFrame) Select(3);
            if (kb.f5Key.wasPressedThisFrame) Select(4);
        }

        /// <summary>Switch class. Returns false (and shows a notice) when that slot has no kit yet.</summary>
        public bool Select(int slot)
        {
            if (slot < 0 || slot >= kits.Length) return false;
            var kit = kits[slot];
            string name = slot < slotNames.Length ? slotNames[slot] : ("Slot " + (slot + 1));
            IconClassId = name;
            IconClassChanged?.Invoke();
            if (kit == null)
            {
                Toast(name + ": chua co bo ky nang");
                return false;
            }
            _active = slot;
            // Designed numbers live on ClassKit_Aren_Designed. Do not rewrite ClassKit_Aren in memory.
            bool designedAren = (string.Equals(kit.classId, "aren", System.StringComparison.OrdinalIgnoreCase)
                        || string.Equals(kit.displayName, "Aren", System.StringComparison.OrdinalIgnoreCase))
                        && kit.slot2 != null && kit.slot2.behaviour == SkillBehaviour.ShieldBash;
            ArenKitVfx.PreferDesignedSkills = designedAren;
            if (_combat != null) _combat.SetKit(kit);
            if (kit.bodyController != null) GetComponent<PlayerVisual>()?.RebindBodyController(kit.bodyController);
            if (kit.equipment != null) GetComponent<EquipmentVisual>()?.Configure(kit.equipment);
            if (_movement != null) _movement.SpeedMultiplier = kit.moveSpeedMultiplier;
            if (_health != null && _baseMaxHp > 0)
            {
                // keep the current HP ratio when the max changes
                float ratio = _health.Normalized;
                int max = Mathf.Max(1, Mathf.RoundToInt(_baseMaxHp * kit.maxHpMultiplier));
                _health.Configure(max, refill: false);
                if (ratio >= 0.999f) _health.Configure(max, refill: true);
            }
            Toast("Class: " + kit.displayName);
            Debug.Log("[ClassCombat] " + kit.displayName + " (F" + (slot + 1) + ") combo " + kit.combo.steps.Length +
                      " hits, skills: " + kit.slot2.displayName + " / " + kit.slot3.displayName + " / " + kit.slot4.displayName +
                      " / " + kit.slotV.displayName + " / " + kit.slotC.displayName);
            return true;
        }

        void Toast(string msg)
        {
            _toast = msg;
            _toastUntil = Time.unscaledTime + 2f;
        }

        void OnGUI()
        {
            if (!showLabel) return;
            var style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(12, Screen.height / 60), fontStyle = FontStyle.Bold };
            string cur = Active != null ? Active.displayName : "-";
            GUI.Label(new Rect(10, 10, 600, 30), "Class: " + cur + "   [F1 Aren  F2 Brom  F3 Lyra  F4 Selene  F5 Vex]", style);
            if (_toast != null && Time.unscaledTime < _toastUntil)
                GUI.Label(new Rect(10, 10 + style.fontSize * 1.6f, 600, 30), _toast, style);
        }
    }
}
