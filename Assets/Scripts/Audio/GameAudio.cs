using System.Collections.Generic;
using UnityEngine;

namespace GameplayPrototype
{
    public enum Sfx
    {
        SlashSwing,
        SlashHit,
        SlashFinisher,
        SkillFlame,
        SkillDash,
        SkillWhirl,
        SkillGuard,
        SkillCrescent,
        EnemyHurt,
        EnemyDeath,
        EnemyWarn,
        HeroHurt,
        HeroDeath,
        UiReady,
        ComboMilestone,
        GameOver
    }

    /// <summary>
    /// Pooled 2D SFX player. Clips load from Resources/Audio/SFX by file name, so replacing a
    /// .wav with a recording of the same name swaps it in without touching code or scenes.
    /// Each id has a volume, pitch jitter, a minimum retrigger gap and a voice cap so a
    /// 10-enemy cleave does not stack ten identical hits.
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        struct Def
        {
            public string file;
            public float volume;
            public float pitchJitter;
            public float minGap;
            public int maxVoices;
        }

        static readonly Dictionary<Sfx, Def> Defs = new()
        {
            { Sfx.SlashSwing, new Def { file = "sfx_slash_swing", volume = 0.85f, pitchJitter = 0.08f, minGap = 0.05f, maxVoices = 2 } },
            { Sfx.SlashHit, new Def { file = "sfx_slash_hit", volume = 0.7f, pitchJitter = 0.1f, minGap = 0.04f, maxVoices = 3 } },
            { Sfx.SlashFinisher, new Def { file = "sfx_slash_finisher", volume = 0.8f, pitchJitter = 0.05f, minGap = 0.1f, maxVoices = 1 } },
            { Sfx.SkillFlame, new Def { file = "sfx_skill_flame", volume = 0.7f, pitchJitter = 0.04f, minGap = 0.2f, maxVoices = 1 } },
            { Sfx.SkillDash, new Def { file = "sfx_skill_dash", volume = 0.7f, pitchJitter = 0.05f, minGap = 0.2f, maxVoices = 1 } },
            { Sfx.SkillWhirl, new Def { file = "sfx_skill_whirl", volume = 0.6f, pitchJitter = 0.06f, minGap = 0.08f, maxVoices = 2 } },
            { Sfx.SkillGuard, new Def { file = "sfx_skill_guard", volume = 0.6f, pitchJitter = 0f, minGap = 0.2f, maxVoices = 1 } },
            { Sfx.SkillCrescent, new Def { file = "sfx_skill_crescent", volume = 0.85f, pitchJitter = 0.03f, minGap = 0.2f, maxVoices = 1 } },
            { Sfx.EnemyHurt, new Def { file = "sfx_enemy_hurt", volume = 0.45f, pitchJitter = 0.15f, minGap = 0.05f, maxVoices = 3 } },
            { Sfx.EnemyDeath, new Def { file = "sfx_enemy_death", volume = 0.55f, pitchJitter = 0.12f, minGap = 0.06f, maxVoices = 3 } },
            { Sfx.EnemyWarn, new Def { file = "sfx_enemy_warn", volume = 0.45f, pitchJitter = 0.05f, minGap = 0.15f, maxVoices = 1 } },
            { Sfx.HeroHurt, new Def { file = "sfx_hero_hurt", volume = 0.7f, pitchJitter = 0.06f, minGap = 0.15f, maxVoices = 1 } },
            { Sfx.HeroDeath, new Def { file = "sfx_hero_death", volume = 0.8f, pitchJitter = 0f, minGap = 1f, maxVoices = 1 } },
            { Sfx.UiReady, new Def { file = "sfx_ui_ready", volume = 0.35f, pitchJitter = 0f, minGap = 0.08f, maxVoices = 1 } },
            { Sfx.ComboMilestone, new Def { file = "sfx_combo_milestone", volume = 0.5f, pitchJitter = 0f, minGap = 0.3f, maxVoices = 1 } },
            { Sfx.GameOver, new Def { file = "sfx_gameover", volume = 0.7f, pitchJitter = 0f, minGap = 1f, maxVoices = 1 } },
        };

        const string Folder = "Audio/SFX/";
        const int VoiceCount = 14;

        [Range(0f, 1f)] [SerializeField] float masterVolume = 0.8f;
        [Range(0f, 1f)] [SerializeField] float ambienceVolume = 0.35f;
        [SerializeField] bool playAmbience = true;

        static GameAudio _instance;
        readonly Dictionary<Sfx, AudioClip> _clips = new();
        readonly Dictionary<Sfx, float> _lastPlayed = new();
        AudioSource[] _voices;
        Sfx[] _voiceIds;
        AudioSource _ambience;

        public static GameAudio Instance => _instance;
        public int ActiveVoices
        {
            get
            {
                int n = 0;
                if (_voices != null)
                    foreach (var v in _voices) if (v.isPlaying) n++;
                return n;
            }
        }

        public static void Play(Sfx id) => _instance?.PlayInternal(id, 1f);
        public static void Play(Sfx id, float volumeScale) => _instance?.PlayInternal(id, volumeScale);

        public static void SetMusicVolume(float volume)
        {
            if (_instance == null) return;
            _instance.ambienceVolume = Mathf.Clamp01(volume);
            if (_instance._ambience != null)
                _instance._ambience.volume = _instance.ambienceVolume * _instance.masterVolume * (_instance._muted ? 0f : 1f);
        }

        public static void SetSfxVolume(float volume)
        {
            if (_instance == null) return;
            _instance.masterVolume = Mathf.Clamp01(volume);
            if (_instance._ambience != null)
                _instance._ambience.volume = _instance.ambienceVolume * _instance.masterVolume * (_instance._muted ? 0f : 1f);
        }

        public static void SetMuted(bool muted)
        {
            if (_instance == null) return;
            _instance._muted = muted;
            if (_instance._ambience != null)
                _instance._ambience.volume = muted ? 0f : _instance.ambienceVolume * _instance.masterVolume;
            if (muted && _instance._voices != null)
            {
                foreach (var v in _instance._voices)
                    if (v != null) v.Stop();
            }
        }

        bool _muted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _instance = null; // Domain Reload is off in this project.

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;

            _voices = new AudioSource[VoiceCount];
            _voiceIds = new Sfx[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                _voices[i] = src;
            }

            foreach (var kv in Defs)
            {
                var clip = Resources.Load<AudioClip>(Folder + kv.Value.file);
                if (clip != null)
                    _clips[kv.Key] = clip;
                else
                    Debug.LogWarning($"[GameAudio] Missing Resources/{Folder}{kv.Value.file}.wav — run Tools/Gameplay Prototype/Generate Placeholder SFX.");
            }

            var amb = Resources.Load<AudioClip>(Folder + "amb_forest_loop");
            masterVolume = GameProgress.SfxVolume;
            ambienceVolume = GameProgress.MusicVolume;
            _muted = GameProgress.Muted;

            if (playAmbience && amb != null)
            {
                _ambience = gameObject.AddComponent<AudioSource>();
                _ambience.clip = amb;
                _ambience.loop = true;
                _ambience.volume = _muted ? 0f : ambienceVolume * masterVolume;
                _ambience.Play();
            }
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        void PlayInternal(Sfx id, float volumeScale)
        {
            if (_muted) return;
            if (!_clips.TryGetValue(id, out var clip)) return;
            var def = Defs[id];
            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(id, out float last) && now - last < def.minGap)
                return;

            int sameId = 0, free = -1, oldest = -1;
            float longestPlayed = -1f;
            for (int i = 0; i < _voices.Length; i++)
            {
                var v = _voices[i];
                if (!v.isPlaying)
                {
                    if (free < 0) free = i;
                    continue;
                }
                if (_voiceIds[i] == id) sameId++;
                if (v.time <= longestPlayed) continue;
                longestPlayed = v.time;
                oldest = i;
            }
            if (sameId >= def.maxVoices) return;
            int slot = free >= 0 ? free : oldest; // steal the voice that has played longest
            if (slot < 0) return;

            _lastPlayed[id] = now;
            var src = _voices[slot];
            _voiceIds[slot] = id;
            src.clip = clip;
            src.volume = def.volume * volumeScale * masterVolume;
            src.pitch = 1f + Random.Range(-def.pitchJitter, def.pitchJitter);
            src.Play();
        }
    }
}
