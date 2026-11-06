# Bản đồ phó bản (Dungeon Map) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thay bảng chọn Dungeon dạng danh sách của Warden bằng màn Bản đồ phó bản đã chốt: bản đồ có ghim ải, khung thông tin bên phải và nút Dịch chuyển.

**Architecture:** Dữ liệu tĩnh và quy tắc thuần (trạng thái ải, tự chọn ải, có vào được không, format thời gian) nằm trong `DungeonMapData`, không phụ thuộc UI nên test được. `DungeonMapPanel` là UI dựng bằng code, làm theo đúng mẫu `WorldMapPanel` (static `Open/Close/IsOpen`, `Build()`, `Apply()`, accessor public để test). `WorldNavigator.OpenDungeonBoard()` chuyển sang gọi `DungeonMapPanel.Open()`.

**Tech Stack:** Unity uGUI (`UnityEngine.UI.Text/Image/Button`), `UiKit`, `Loc`, `GameProgress`, `LevelFlow`. Test là static `Run()` trả string trong `Assets/Editor`, chạy qua Unity MCP eval (giống `WorldMapRulesTests`).

**Spec:** `Docs/superpowers/specs/2026-11-06-dungeon-map-ui-design.md` · Mockup: `Docs/UI/mockups/landscape/L07b_Dungeon_Map_LOCKED.png`

## Global Constraints

- Chỉ landscape 16:9, tham chiếu 1920×1080. Không làm layout portrait.
- Giữ nguyên bố cục mockup đã chốt (tiêu đề, 3 tab, bản đồ 36,106,1232,936, khung phải 1300,106,580,936). Không chia lại 3 cột.
- Không đổi field serialized của `LevelConfig`, không đổi GUID/meta, scene hay prefab. Không thêm dependency.
- Chuỗi gốc viết bằng tiếng Anh và đi qua `Loc.T(en)`. Bản tiếng Việt thêm cặp `(en, vi)` vào `Loc.Village.cs`. Tên VN lấy đúng theo spec §5.
- Không viết đè save thật: test phải lưu và khôi phục `gp.unlockedLevel` cùng các key best (giống `WorldMapRulesTests`).
- Art demo dùng tài sản có sẵn: `UI_WorldMap_Frame`, `UI_WorldMap_Outdoor_Art`, `UI_WorldMap_Fog_Region{n}`, `UI_Icon_Dungeon/Lock/Boss`, `UI_Star_Large_Filled/Empty`, `UI_Icon_Currency`, `UI_Icon_EXP`.

## Review Focus

1. **Ải nằm ngoài campaign (ải 5, khi `n > LevelFlow.CampaignLevelCount` = 4):** phải hiện "Sắp ra mắt" và tắt nút, kể cả khi `GetLevelConfig(5)` khác null. Test nằm ở Task 1.
2. **Save mới tinh (`unlockedLevel` = 1, chưa có kỷ lục):** tự chọn ải 1 và ẩn dòng kỷ lục. Test nằm ở Task 1 và Task 2.
3. **Đã xong toàn bộ campaign:** tự chọn ải cao nhất đã mở, không chọn ải COMING SOON. Test nằm ở Task 1.
4. **Bấm ghim ải khóa:** vẫn đổi thông tin bên phải nhưng nút tắt kèm chữ lý do. Bấm Dịch chuyển khi đó không làm gì. Test nằm ở Task 2.
5. **Mở bảng hai lần liên tiếp, hoặc mở khi đang có panel khác:** chỉ có một instance (`Ensure()` giống `WorldMapPanel`). `WorldServices.HidePanel()` và `CloseBag()` vẫn được gọi. Test nằm ở Task 3.

---

### Task 1: Dữ liệu ải và quy tắc thuần

**Files:**
- Create: `Assets/Scripts/UI/DungeonMapData.cs`
- Test: `Assets/Editor/DungeonMapRulesTests.cs`

**Interfaces:**
- Produces:
  - `public enum StageStatus { Cleared, Open, Locked, ComingSoon }`
  - `public struct StageInfo { public int level; public string name, terrain, objective, boss, hint; public int recLevel; public Vector2 pin; public bool isBoss; public string[] rewards; public string firstReward; public string[] wardenLines; }` (`rewards` gồm 3 chuỗi, mỗi chuỗi dạng `"<sprite>|<label EN>"`)
  - `public static class DungeonMapData`
    - `public static readonly string[] Regions = { "Goblin Camp", "Rotting Marsh", "Bone Canyon" };`
    - `public static StageInfo[] Stages` (5 phần tử, index 0 tương ứng ải 1)
    - `public static StageStatus Status(int level, int highestUnlocked, int campaignCount, bool hasConfig)`
    - `public static int AutoSelect(int highestUnlocked, int campaignCount)` trả số ải (1-based)
    - `public static bool CanEnter(StageStatus s)` trả true khi Cleared hoặc Open
    - `public static string FormatTime(float seconds)` trả `"mm:ss"`, và `"--:--"` khi ≤ 0
    - `public static int ThreeStarSeconds(int level)` trả 150 / 140 / 160 / 210 / 280

- [ ] **Step 1: Viết test lỗi trước** trong `DungeonMapRulesTests.Run()`. Test trả `"OK"` hoặc tên assert đầu tiên bị sai:
  - `Status(1,1,4,true)==Open`; `Status(1,3,4,true)==Cleared`; `Status(3,3,4,true)==Open`; `Status(4,3,4,true)==Locked`
  - `Status(5,5,4,true)==ComingSoon`; `Status(5,2,4,true)==ComingSoon`; `Status(2,4,4,false)==ComingSoon`
  - `AutoSelect(1,4)==1`; `AutoSelect(3,4)==3`; `AutoSelect(5,4)==4`
  - `CanEnter(Locked)==false`; `CanEnter(ComingSoon)==false`; `CanEnter(Cleared)==true`
  - `FormatTime(141.2f)=="02:21"`; `FormatTime(0f)=="--:--"`
  - `Stages.Length==5`; `Stages[2].recLevel==8`; `Stages[4].isBoss==true`; `Stages[3].boss=="Cyclops"`
  - Với mọi ải: `rewards.Length==3`, `wardenLines.Length==3`, pin nằm trong [0,1]
- [ ] **Step 2: Chạy test, phải FAIL**. Chạy qua Unity MCP eval: `GameplayPrototype.DungeonMapRulesTests.Run()`. Kỳ vọng lỗi compile vì `DungeonMapData` chưa có.
- [ ] **Step 3: Viết `DungeonMapData`.** Quy tắc `Status`: `level > campaignCount || !hasConfig` thì ComingSoon; `level > highestUnlocked` thì Locked; `level < highestUnlocked` thì Cleared; còn lại là Open. `AutoSelect` trả `Mathf.Clamp(highestUnlocked, 1, campaignCount)`.
  - Dữ liệu 5 ải chép từ spec §5: tên EN (`Goblin Forest Camp`, `Forest Trail`, `Campfire Clearing`, `Goblin Outpost`, `Gobrider Stronghold`), địa hình, mục tiêu, boss, Lv 1/4/8/11/14, phần thưởng, thưởng lần đầu, 3 dòng thoại Warden, hint (`"Clear stage N to unlock stage N+1"`; ải 5 là `"Defeat the boss to unlock Rotting Marsh"`).
  - `pin` của bản demo lấy theo `WorldMapPanel.PinAt[1..5]` chia cho kích thước map 408×308, để ghim khớp art outdoor đang dùng. Toạ độ trong spec §5 dành cho art bản đồ riêng sau này. Ghi chú điều này trong comment một dòng.
- [ ] **Step 4: Chạy lại test**, kỳ vọng kết quả `"OK"`.
- [ ] **Step 5: Commit** — `git add Assets/Scripts/UI/DungeonMapData.cs* Assets/Editor/DungeonMapRulesTests.cs*` rồi commit với message "Add dungeon map stage data and unlock rules."

### Task 2: Panel Bản đồ phó bản

**Files:**
- Create: `Assets/Scripts/UI/DungeonMapPanel.cs`
- Modify: `Assets/Editor/DungeonMapRulesTests.cs` (thêm phần test panel vào `Run()`)
- Modify: `Assets/Scripts/UI/Loc.Village.cs` (thêm các cặp VI)

**Interfaces:**
- Consumes: `DungeonMapData.*` (Task 1), `GameProgress.HighestUnlockedLevel/GetBestStars/GetBestScore/GetBestTime`, `LevelFlow.Instance.GetLevelConfig(n)`, `LevelFlow.CampaignLevelCount`, `WorldSave.Level`.
- Produces:
  - `public class DungeonMapPanel : MonoBehaviour`
  - `public static bool IsOpen`; `public static void Open()`; `public static void Close()`; `public static void Toggle()`
  - `public void Select(int level)`; `public void Enter()`
  - Accessor để test: `public int Selected`, `public bool EnterOn`, `public string EnterLabel`, `public string HeaderText`, `public bool RecordVisible`, `public string ChallengeText`, `public bool FogOn(int level)`, `public bool CheckOn(int level)`

- [ ] **Step 1: Viết test lỗi trước** (lưu và khôi phục `gp.unlockedLevel` giống `WorldMapRulesTests`, huỷ object `DungeonMapCanvas` trong `finally`):
  - unlocked=1, `Open()`: `Selected==1`, `RecordVisible==false`, `EnterOn==true`, `CheckOn(1)==false`, `FogOn(2)==true`
  - unlocked=3, `Open()`: `Selected==3`, `HeaderText` chứa `"3"` và `Loc.T("Open")`, `CheckOn(1)&&CheckOn(2)`, `FogOn(4)==true`, `ChallengeText` chứa `"4"` (số wave)
  - `Select(4)`: `EnterOn==false`, `EnterLabel==Loc.T("Locked")`; gọi `Enter()` rồi kiểm tra `IsOpen==true` (bảng không đóng)
  - `Select(5)`: `EnterLabel==Loc.T("Coming soon")`
  - unlocked=2 và có `GetBestScore(1)>0` (ghi tạm bằng `GameProgress.RecordRun(1, 500, 1, 2, 141f)`, sau đó khôi phục các key best của ải 1 theo hằng `KeyBest*` trong `GameProgress.cs`): `Select(1)` thì `RecordVisible==true`
- [ ] **Step 2: Chạy test, phải FAIL** (lỗi compile vì chưa có `DungeonMapPanel`).
- [ ] **Step 3: Viết `DungeonMapPanel`** theo mẫu `WorldMapPanel`: `Ensure()` tạo `DungeonMapCanvas` (ScreenSpaceOverlay, CanvasScaler ref 1920×1080, match 0.5), một instance duy nhất, và các helper `Label/Sprite/Corner` tương tự.
  - Bố cục đúng toạ độ trong Global Constraints và spec §2. Tab khóa: rung nhẹ và gọi `RewardBanner`/toast với `Loc.T("Defeat the Goblin Camp boss to unlock")`.
  - Ghim theo spec §3: halo cho ải đang chọn, ✓ cho ải đã xong, sọ cho boss, khóa và sương (`UI_WorldMap_Fog_Region{n}`) cho Locked/ComingSoon. Ghim Open nhấp nháy 1 Hz bằng `Time.unscaledTime`.
  - Khung phải theo đúng thứ tự spec §4, mục 1–11.
    - Lv chuyển đỏ khi `WorldSave.Level < recLevel - 2`.
    - Dòng thử thách: `"{waves} waves · {boss or —} · {objective}"`, trong đó waves là `cfg.waves.Length` (cfg null thì hiện "—").
    - Dòng kỷ lục: chỉ hiện khi `GetBestScore(n) > 0`, gồm 3 sao, điểm và `FormatTime(GetBestTime(n))`.
  - Phím (trong `Update`, dùng input đang có của project): `←/→/A/D` chọn ải trước/sau, `Q/E` đổi tab, `Enter/Space` gọi `Enter()`, `Esc` gọi `Close()`.
  - `Enter()`: nếu `!CanEnter` thì return. Nếu được thì `Close()`, gọi `WorldNavigator.Instance?.CloseDungeonBoard()`, rồi `flow.StartLevel(cfg, true)`, giống `WorldMapPanel.Go()`.
  - Thêm cặp VI vào `Loc.Village.cs` cho mọi chuỗi mới: tên vùng, tên ải, địa hình, mục tiêu, hint, nhãn nút (`Teleport`→`Dịch chuyển`, `Locked`→`Đã khóa`, `Coming soon`→`Sắp ra mắt`, `Open`→`Đang mở`, `Cleared`→`Đã hoàn thành`, `Recommended Lv.`→`Đề nghị Lv.`, `Rewards`→`Phần thưởng`, `Warden`→`Lính canh`, `Dungeon Map`→`Bản đồ phó bản`), và 15 dòng thoại.
- [ ] **Step 4: Chạy lại test**, kỳ vọng `"OK"` cho cả phần của Task 1 và Task 2.
- [ ] **Step 5: Commit** — "Add the dungeon map panel with stage pins and info card."

### Task 3: Nối Warden và nút HUD vào panel mới

**Files:**
- Modify: `Assets/Scripts/World/WorldNavigator.cs:324-…` (`OpenDungeonBoard`)
- Modify: `Assets/Editor/DungeonMapRulesTests.cs`

**Interfaces:**
- Consumes: `DungeonMapPanel.Open()/IsOpen/Close()` (Task 2)

- [ ] **Step 1: Viết test lỗi trước:** gọi `WorldNavigator.Instance.OpenDungeonBoard()` hai lần, kỳ vọng `DungeonMapPanel.IsOpen==true` và `Object.FindObjectsByType<DungeonMapPanel>(FindObjectsSortMode.None).Length==1`. Nếu không có `WorldNavigator.Instance` trong edit mode thì test trả `"SKIP nav"` và chạy bước này ở Play Mode.
- [ ] **Step 2: Chạy test, phải FAIL**, vì board cũ vẫn mở và `IsOpen` vẫn là false.
- [ ] **Step 3: Sửa `OpenDungeonBoard()`:** giữ `Build(); WorldServices.Instance?.HidePanel(); CloseBag();` rồi gọi `DungeonMapPanel.Open(); return;`. Giữ code board cũ phía dưới (không xoá ở bước này) để rollback dễ, nhưng code đó không còn chạy tới. Đổi `talk.Line` của Warden thành `"Pick a stage, then teleport."` và thêm cặp VI `"Chọn ải rồi dịch chuyển."`.
- [ ] **Step 4: Chạy test**, kỳ vọng `"OK"`. Sau đó chạy lại `WorldMapRulesTests.Run()` để chắc world map không bị ảnh hưởng (kết quả phải giống trước khi sửa).
- [ ] **Step 5: Kiểm tra trong Play Mode:** vào Village, nhấn `E` ở Warden, bảng mở và chọn sẵn ải tiếp theo. Bấm Dịch chuyển thì vào ải và bắt đầu Wave 1. Chụp screenshot Game view 1920×1080 để so với mockup.
- [ ] **Step 6: Commit** — "Open the dungeon map from the Warden and the HUD."

## Ngoài phạm vi (làm sau)

- Art bản đồ riêng cho vùng Trại Goblin (dùng toạ độ ghim trong spec §5), icon phần thưởng thật, nội dung của vùng 2–3.
- Xoá code board danh sách cũ trong `OpenDungeonBoard` sau khi bản mới chạy ổn.
