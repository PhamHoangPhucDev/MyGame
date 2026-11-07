using UnityEngine;

namespace GameplayPrototype
{
    public enum DungeonMapKind { Winding, Maze, Field, Ledge, Boss }

    public struct DungeonLine
    {
        public string speaker;
        public string text;
        public string action;
    }

    public struct DungeonReward
    {
        public string itemId;
        public int amount;
    }

    public struct DungeonStage
    {
        public string name;
        public DungeonMapKind kind;
        public int level;
        public string layoutId;
        public LevelObjectiveType objective;
        public WaveConfig[] waves;
        public int threeStarSeconds;
        public DungeonReward[] rewards;
        public string guard;
        public DungeonLine[] talk;
    }

    public struct DungeonDef
    {
        public string id;
        public string name;
        public bool hasBoard;
        public DungeonStage[] stages;
    }

    /// <summary>Stage-select data. Progress is per dungeon and is not the world-map level.</summary>
    public static class DungeonCatalog
    {
        /// <summary>First clear pays this many times. Set to 1 to turn the bonus off.</summary>
        public const int FirstClearRewardMultiplier = 2;
        public static readonly string[] SpriteNames =
        {
            "UI_Dungeon_Board_GoblinCamp",
            "UI_Dungeon_Fog_GoblinCamp_Stage2",
            "UI_Dungeon_Fog_GoblinCamp_Stage3",
            "UI_Dungeon_Fog_GoblinCamp_Stage4",
            "UI_Dungeon_Fog_GoblinCamp_Stage5",
            "UI_Dungeon_GuardStrip",
            "UI_Dungeon_Guard_Portrait",
            "UI_Dungeon_MapType_Field",
            "UI_Dungeon_MapType_Ledge",
            "UI_Dungeon_MapType_Maze",
            "UI_Dungeon_MapType_Winding",
            "UI_Dungeon_Pin_Boss",
            "UI_Dungeon_Pin_Boss_Locked",
            "UI_Dungeon_RewardSlot",
            "UI_Dungeon_Tab",
            "UI_Dungeon_Tab_Locked",
            "UI_Dungeon_Tab_Selected"
        };

        static DungeonDef[] _dungeons;

        public static DungeonDef[] Dungeons
        {
            get
            {
                if (_dungeons != null) return _dungeons;
                var camp = new[]
                {
                    Stage("Đường đất đỏ", DungeonMapKind.Winding, 3, DungeonStageMap.RedTrail, LevelObjectiveType.ClearWavesThenReachExit, 140,
                        Loot(120, 6, 1),
                        new[]
                        {
                            Wave("ĐỢT 1 — TRINH SÁT", goblins: 3),
                            Wave("ĐỢT 2 — CUNG BÊN ĐƯỜNG", goblins: 2, archers: 2),
                            Wave("ĐỢT 3 — CHẶN KHÚC CUA", goblins: 3, archers: 2),
                            Wave("ĐỢT 4 — ĐỘI GÁC CỔNG", goblins: 4, archers: 2)
                        },
                        "Đường này vòng trước khi vào trại. Đi sát vệt đất.",
                        "Lính canh", "Cổng trại ở cuối đường vòng.", "",
                        "Goblin", "Đứng lại!", "spawn"),
                    Stage("Hàng rào nhọn", DungeonMapKind.Maze, 5, "FenceMaze", LevelObjectiveType.ClearWavesThenReachExit, 160,
                        Loot(160, 10, 1),
                        new[]
                        {
                            Wave("ĐỢT 1 — GÁC NGÕ", goblins: 3),
                            Wave("ĐỢT 2 — PHỤC KÍCH", goblins: 2, archers: 3),
                            Wave("ĐỢT 3 — KỴ TUẦN", goblins: 2, riders: 2),
                            Wave("ĐỢT 4 — CHẶN LỐI RA", goblins: 3, riders: 2, archers: 1)
                        },
                        "Bên trong là hàng rào. Dễ lạc ở ngõ cụt.",
                        "Lính canh", "Đừng rẽ vào ngõ cụt.", "",
                        "Goblin", "Chặn lối!", "spawn"),
                    Stage("Bãi lửa trại", DungeonMapKind.Field, 8, DungeonStageMap.FireCamp, LevelObjectiveType.ClearWaves, 170,
                        Loot(220, 12, 2),
                        new[]
                        {
                            Wave("ĐỢT 1 — LỬA BÙNG", goblins: 4),
                            Wave("ĐỢT 2 — VÂY TRÁI PHẢI", goblins: 3, archers: 3),
                            Wave("ĐỢT 3 — KỴ XÔNG", goblins: 2, riders: 3),
                            Wave("ĐỢT 4 — VÒNG VÂY", goblins: 3, archers: 2, riders: 3, elite: 1)
                        },
                        "Bọn goblin dựng lửa trại ngay giữa bãi trống. Coi chừng bị vây đấy.",
                        "Hero", "Bãi trống. Chúng sẽ kéo đến.", "",
                        "Goblin", "Vây lấy!", "spawn"),
                    Stage("Cầu vực", DungeonMapKind.Ledge, 11, DungeonStageMap.Chasm, LevelObjectiveType.ClearWavesThenReachExit, 210,
                        Loot(320, 14, 3),
                        new[]
                        {
                            Wave("ĐỢT 1 — ĐẦU CẦU", riders: 4, elite: 2),
                            Wave("ĐỢT 2 — GIỮA CẦU", archers: 2, riders: 2, elite: 2, warlords: 1),
                            Wave("ĐỢT 3 — CUỐI CẦU", cyclops: 1)
                        },
                        "Cầu gỗ bắc qua vực. Một hàng, không có chỗ né.",
                        "Lính canh", "Chúng chặn cả hai đầu cầu.", "",
                        "Goblin", "Đẩy xuống!", "spawn"),
                    Stage("Lều xương", DungeonMapKind.Boss, 14, DungeonStageMap.BoneYard, LevelObjectiveType.DefeatBossChapterComplete, 280,
                        Loot(500, 20, 5),
                        new[]
                        {
                            Wave("ĐỢT 1 — LÍNH GÁC", goblins: 4, archers: 2),
                            Wave("ĐỢT 2 — CẬN VỆ", goblins: 3, riders: 2, elite: 2),
                            Wave("ĐỢT 3 — TƯỚNG GOBRIDER", boss: 1)
                        },
                        "Lều xương cuối đường. Tướng goblin ngồi trong vòng nghi lễ.",
                        "Boss", "Ngai này là của ta.", "boss",
                        "Hero", "Vậy thì bước xuống.", "")
                };
                _dungeons = new[]
                {
                    new DungeonDef { id = "goblin_camp", name = "Trại Goblin", hasBoard = true, stages = camp },
                    new DungeonDef { id = "murk_marsh", name = "Đầm Lầy Mục", hasBoard = false, stages = CopyShape(camp, "Đầm") },
                    new DungeonDef { id = "bone_pass", name = "Hẻm Núi Xương", hasBoard = false, stages = CopyShape(camp, "Hẻm") }
                };
                return _dungeons;
            }
        }

        public static DungeonDef Get(int dungeon)
        {
            if (dungeon < 0 || dungeon >= Dungeons.Length) dungeon = 0;
            return Dungeons[dungeon];
        }

        public static DungeonLine[] Talk(int dungeon, int stage)
        {
            var stages = Get(dungeon).stages;
            int i = Mathf.Clamp(stage, 1, stages.Length) - 1;
            return stages[i].talk;
        }

        public static DungeonStage StageAt(int dungeon, int stage)
        {
            var stages = Get(dungeon).stages;
            int i = Mathf.Clamp(stage, 1, stages.Length) - 1;
            return stages[i];
        }

        public static bool LevelWarn(int recommended) => WorldSave.Level < recommended;

        public static string ObjectiveLine(DungeonStage stage)
        {
            if (stage.objective == LevelObjectiveType.DefeatBossChapterComplete) return "Hạ boss";
            if (stage.objective == LevelObjectiveType.ClearWavesThenReachExit) return "Dọn đợt rồi tới cổng";
            return "Diệt hết quái";
        }

        public static string WaveLine(DungeonStage stage)
        {
            int n = stage.waves != null ? stage.waves.Length : 0;
            bool boss = stage.objective == LevelObjectiveType.DefeatBossChapterComplete;
            return n + " ĐỢT · " + (boss ? "BOSS" : "KHÔNG BOSS");
        }

        public static string RewardWord(string itemId)
        {
            if (itemId == "coin") return "Vàng";
            if (itemId == "wood") return "Gỗ";
            if (itemId == "gem_ruby") return "Ngọc";
            return itemId;
        }

        /// <summary>Runtime level. Does not write the campaign LevelConfig assets.</summary>
        public static LevelConfig BuildRun(int dungeon, int stage)
        {
            var s = StageAt(dungeon, stage);
            var cfg = ScriptableObject.CreateInstance<LevelConfig>();
            var flow = LevelFlow.Instance;
            int template = TemplateNumber(s.layoutId);
            var src = flow != null ? flow.GetLevelConfig(template) : null;
            if (src != null)
            {
                cfg.heroSpawn = src.heroSpawn;
                cfg.exitPosition = src.exitPosition;
                cfg.exitSize = src.exitSize;
                cfg.spawnZones = src.spawnZones;
                cfg.cameraBoundsMin = src.cameraBoundsMin;
                cfg.cameraBoundsMax = src.cameraBoundsMax;
            }
            else
            {
                cfg.heroSpawn = new Vector2(0.2f, -6.2f);
                cfg.exitPosition = new Vector2(0.2f, 8f);
                cfg.exitSize = new Vector2(1.6f, 1.2f);
                cfg.cameraBoundsMin = new Vector2(-18f, -13f);
                cfg.cameraBoundsMax = new Vector2(18f, 15f);
            }
            if (DungeonStageMap.Handles(s.layoutId))
            {
                // Stages 1, 3, 4, 5 have their own runtime maps (mockups S1/S3/S4/S5).
                var map = DungeonStageMap.Get(s.layoutId);
                cfg.heroSpawn = map.spawn;
                cfg.exitPosition = map.exit;
                cfg.exitSize = new Vector2(1.6f, 1.6f);
                cfg.spawnZones = map.zones;
                cfg.cameraBoundsMin = map.min;
                cfg.cameraBoundsMax = map.max;
            }
            if (s.layoutId == "FenceMaze")
            {
                // The maze is built at runtime; take hero, gate, spawns and camera from it, not from Trail.
                cfg.heroSpawn = DungeonFenceMaze.HeroSpawn;
                cfg.exitPosition = DungeonFenceMaze.ExitPosition;
                cfg.exitSize = DungeonFenceMaze.ExitSize;
                cfg.spawnZones = DungeonFenceMaze.SpawnZones;
                cfg.cameraBoundsMin = DungeonFenceMaze.BoundsMin;
                cfg.cameraBoundsMax = DungeonFenceMaze.BoundsMax;
            }
            cfg.levelNumber = 100 + dungeon * 10 + stage;
            cfg.levelId = "dg_" + Get(dungeon).id + "_" + stage;
            cfg.levelTitle = s.name;
            cfg.mapLayoutId = string.IsNullOrEmpty(s.layoutId) ? "Trail" : s.layoutId;
            cfg.objectiveType = s.objective;
            cfg.waves = s.waves;
            cfg.targetCompletionTime = Mathf.Max(30f, s.threeStarSeconds);
            cfg.starHpRatio = 0.4f;
            cfg.nextLevelNumber = stage < Get(dungeon).stages.Length ? stage + 1 : 0;
            return cfg;
        }

        /// <summary>Stage 3 sides each wave pours in from (spec C: lửa bùng, vây trái phải, kỵ xông, vòng vây).</summary>
        public static Vector2[] CampWaveSides(int wave)
        {
            switch (wave)
            {
                case 1: return new[] { Vector2.up };
                case 2: return new[] { Vector2.left, Vector2.right };
                case 3: return new[] { Vector2.up, Vector2.down };
                case 4: return new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
                default: return new Vector2[0];
            }
        }

        /// <summary>Multiplier of the last Grant (2 on a first clear), so the result screen shows what was paid.</summary>
        public static int LastGrantMultiplier { get; private set; } = 1;

        public static void Grant(int dungeon, int stage)
        {
            var rewards = StageAt(dungeon, stage).rewards;
            if (rewards == null) return;
            string paid = "dg.paid." + dungeon + "." + stage;
            int mul = PlayerPrefs.HasKey(paid) ? 1 : FirstClearRewardMultiplier;
            LastGrantMultiplier = mul;
            PlayerPrefs.SetInt(paid, 1);
            for (int i = 0; i < rewards.Length; i++)
            {
                int amount = rewards[i].amount * mul;
                if (rewards[i].itemId == "coin") WorldSave.AddCoins(amount);
                else WorldSave.AddItem(rewards[i].itemId, amount);
                RewardBanner.Show(rewards[i].itemId, amount);
            }
            PlayerPrefs.Save();
        }

        public static string CheckLayouts()
        {
            var a = BuildRun(0, 1);
            var b = BuildRun(0, 2);
            var c = BuildRun(0, 3);
            string layouts = a.mapLayoutId + " " + b.mapLayoutId + " " + c.mapLayoutId + " w" + a.waves.Length + " g" + b.waves[0].goblins
                + " maze spawn=" + b.heroSpawn + " exit=" + b.exitPosition + " zones=" + (b.spawnZones != null ? b.spawnZones.Length : 0)
                + " camp spawn=" + c.heroSpawn;
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
            Object.DestroyImmediate(c);
            return layouts;
        }

        static int TemplateNumber(string layout)
        {
            // Level 1 is the circular Camp; level 3 is AbandonedCamp and puts the hero outside the camp ring.
            if (layout == "Camp") return 1;
            if (layout == "GoblinOutpost") return 4;
            if (layout == "GobriderStronghold") return 5;
            return 2;
        }

        static DungeonReward[] Loot(int coin, int wood, int gem)
        {
            return new[]
            {
                new DungeonReward { itemId = "coin", amount = coin },
                new DungeonReward { itemId = "wood", amount = wood },
                new DungeonReward { itemId = "gem_ruby", amount = gem }
            };
        }

        static WaveConfig Wave(string name, int goblins = 0, int archers = 0, int riders = 0, int elite = 0, int warlords = 0, int cyclops = 0, int boss = 0)
        {
            int bodies = goblins + archers + riders + warlords + cyclops + boss;
            return new WaveConfig
            {
                displayName = name,
                goblins = goblins,
                archers = archers,
                gobriders = riders,
                eliteGobrider = elite,
                warlords = warlords,
                cyclops = cyclops,
                bossWarlord = boss,
                aliveCap = Mathf.Max(1, bodies),
                allowTopUp = false,
                transitionDelay = 1.75f
            };
        }

        static DungeonStage Stage(string name, DungeonMapKind kind, int level, string layout, LevelObjectiveType objective, int seconds,
            DungeonReward[] loot, WaveConfig[] waves, string guard,
            string a, string aText, string aAct, string b, string bText, string bAct)
        {
            return new DungeonStage
            {
                name = name, kind = kind, level = level, layoutId = layout, objective = objective,
                threeStarSeconds = seconds, rewards = loot, waves = waves, guard = guard,
                talk = new[]
                {
                    new DungeonLine { speaker = a, text = aText, action = aAct },
                    new DungeonLine { speaker = b, text = bText, action = bAct }
                }
            };
        }

        static DungeonStage[] CopyShape(DungeonStage[] src, string prefix)
        {
            var copy = new DungeonStage[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                copy[i] = src[i];
                copy[i].name = prefix + " " + (i + 1);
            }
            return copy;
        }

        public static string KindName(DungeonMapKind kind)
        {
            if (kind == DungeonMapKind.Winding) return "Đường vòng vèo";
            if (kind == DungeonMapKind.Maze) return "Mê cung";
            if (kind == DungeonMapKind.Field) return "Bãi đất trống";
            if (kind == DungeonMapKind.Ledge) return "Ven vực";
            return "Lều boss";
        }

        public static string KindSprite(DungeonMapKind kind)
        {
            if (kind == DungeonMapKind.Winding) return "UI_Dungeon_MapType_Winding";
            if (kind == DungeonMapKind.Maze) return "UI_Dungeon_MapType_Maze";
            if (kind == DungeonMapKind.Field) return "UI_Dungeon_MapType_Field";
            if (kind == DungeonMapKind.Ledge) return "UI_Dungeon_MapType_Ledge";
            return "";
        }
    }

    public static class DungeonProgress
    {
        public static int Cleared(int dungeon) => PlayerPrefs.GetInt(Key(dungeon), 0);

        public static void SetCleared(int dungeon, int stage)
        {
            PlayerPrefs.SetInt(Key(dungeon), Mathf.Clamp(stage, 0, 5));
            PlayerPrefs.Save();
        }

        public static void Clear(int dungeon, int stage)
        {
            if (stage > Cleared(dungeon)) SetCleared(dungeon, stage);
        }

        public static bool StageCleared(int dungeon, int stage) => stage >= 1 && stage <= Cleared(dungeon);

        public static bool StageUnlocked(int dungeon, int stage) => stage >= 1 && stage <= 5 && stage <= Cleared(dungeon) + 1;

        public static int HighestUnlocked(int dungeon) => Mathf.Clamp(Cleared(dungeon) + 1, 1, 5);

        public static bool DungeonUnlocked(int dungeon) => dungeon <= 0 || Cleared(dungeon - 1) >= 5;

        static string Key(int dungeon) => "dg.cleared." + dungeon;
    }

    public static class DungeonRecords
    {
        public static void Save(int dungeon, int stage, int score, int stars, float seconds)
        {
            string key = Key(dungeon, stage);
            int bestScore = 0, bestStars = 0;
            float bestTime = 0f;
            bool had = TryRead(key, out bestScore, out bestStars, out bestTime);
            bool better = !had || stars > bestStars || (stars == bestStars && (score > bestScore || (score == bestScore && seconds < bestTime)));
            if (!better) return;
            PlayerPrefs.SetString(key, score + "|" + stars + "|" + seconds.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }

        public static string Line(int dungeon, int stage)
        {
            if (!TryRead(Key(dungeon, stage), out int score, out int stars, out float seconds))
                return "CHƯA CÓ KỶ LỤC";
            int s = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return score + " ĐIỂM · " + (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
        }

        public static int Stars(int dungeon, int stage)
        {
            TryRead(Key(dungeon, stage), out _, out int stars, out _);
            return stars;
        }

        public static string CheckRoundtrip()
        {
            const string key = "dg.best.0.1";
            bool had = PlayerPrefs.HasKey(key);
            string old = PlayerPrefs.GetString(key, "");
            Save(0, 1, 1174, 3, 141f);
            string line = Line(0, 1);
            if (had) PlayerPrefs.SetString(key, old);
            else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            return line;
        }

        static bool TryRead(string key, out int score, out int stars, out float seconds)
        {
            score = 0;
            stars = 0;
            seconds = 0f;
            string raw = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(raw)) return false;
            var parts = raw.Split('|');
            if (parts.Length < 3) return false;
            int.TryParse(parts[0], out score);
            int.TryParse(parts[1], out stars);
            float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out seconds);
            return true;
        }

        static string Key(int dungeon, int stage) => "dg.best." + dungeon + "." + stage;
    }

    /// <summary>One stage run, separate from the outdoor world map.</summary>
    public static class DungeonRun
    {
        public static bool Active { get; private set; }
        public static bool Hold { get; private set; }
        public static int Dungeon { get; private set; }
        public static int Stage { get; private set; }

        public static void Arm(int dungeon, int stage)
        {
            Active = true;
            Hold = true;
            Dungeon = dungeon;
            Stage = stage;
        }

        public static void ReleaseHold() => Hold = false;

        public static void NoteClear()
        {
            if (Active) DungeonProgress.Clear(Dungeon, Stage);
        }

        public static void Leave()
        {
            Active = false;
            Hold = false;
            DungeonTalk.Close();
        }
    }
}
