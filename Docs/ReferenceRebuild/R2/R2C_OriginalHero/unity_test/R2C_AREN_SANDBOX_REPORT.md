# R2C — Aren trong Sandbox (sheet + Animator + EquipmentVisual): báo cáo

**Ngày:** 2026-10-20 · **Nhánh:** `reference_rebuild_r2c` · **Chưa commit**
**Phạm vi:** chỉ `ReferenceCombatSandbox`. Level 1–4 (scene `GameplayPrototype_Phase4Test`, `Hero.controller`, `Hero_Sheet` Phase 4) **không đổi**.
**Sao lưu scene trước khi sửa:** `IncomingAssets/backup_before_R2C_Aren/ReferenceCombatSandbox.unity`

## 1. Đã làm

| Bước | Kết quả |
|---|---|
| Xuất asset | `tools/ArenUnityExport.cs` → `unity_export/`: sheet 128×288 (cùng bố cục ô với R2b, hàng Side lật để quay trái theo quy ước "Side sheets face left" của `PlayerVisual`), 6 sprite Hurt, 3 sprite Death, kiếm 8 tư thế, khiên 3 hướng, găng tay, vệt chém 6 frame, bảng anchor 42 dòng |
| Import | `Assets/Art/Rebuild/R2C/Aren/**`: PPU 16, Point, không mipmap, không nén. Sheet cắt 33 ô, tên `Aren_{Anim}_{Hướng}_{frame}`, pivot (0.5, 0.0625). Vũ khí và khiên: pivot = tay cầm. Vệt chém: pivot = pivot thân |
| Clip | 13 clip mới. Idle 0 / 0.25 / 0.40 / 0.65 (dừng 0.80, lặp). Walk 4 × 0.125 (lặp). **Attack giữ đúng mốc của combat: 0 / 0.12 / 0.19, dừng 0.26** (impact 0.12 s, khớp `slashImpactDelay`). Hurt 0 / 0.06 (dừng 0.18). Death 0 / 0.15 / 0.30 (giữ) |
| Controller | `Aren.controller` = bản sao `Hero_R2.controller` (giữ tham số và chuyển trạng thái). Đã gán lại **13/13** state |
| Component | `Assets/Scripts/Player/EquipmentVisual.cs` (mới) |
| Scene | Chỉ Sandbox, chỉ đối tượng `Player`: `PlayerVisual.bodyController` → `Aren.controller`, thêm `EquipmentVisual`. Diff scene: 41 dòng |
| Công cụ | `Assets/Editor/R2CArenSandboxSetup.cs` (menu *Tools → Gameplay Prototype → R2C → Setup Sandbox Aren (equipment)*). Chạy lại sẽ dựng lại toàn bộ |

## 2. Cách EquipmentVisual hoạt động

1. Mỗi frame đọc tên sprite thân (`Aren_Attack_Side_1` → khoá `Attack_Side_1`) và tra `Aren_Anchors.csv`.
2. Đặt 4 lớp con dưới renderer thân:
   - `Equip_Main`: kiếm theo 1 trong 8 tư thế, **không xoay lúc chạy**.
   - `Equip_Hand`: găng tay.
   - `Equip_Off`: khiên theo hướng.
   - `Equip_Slash`: vệt chém, chỉ bật ở Attack F1 và F2.
3. **Vị trí:** `(x − 16, 30 − y) / 16`. Khi thân `flipX`, đảo dấu x và lật sprite, nên mọi lớp lật theo thân.
4. **Thứ tự vẽ:** thân ± 1/2/3, theo cột `*_order` của bảng. Thứ tự này đi theo `SpriteSorter` của thân.
5. **Màu:** copy `body.color` (nháy máu của `Health` áp cho cả trang bị). Ở Hurt F1 nhân thêm sắc đỏ.
6. **Kiếm placeholder cũ** (`Player/Sword`) chỉ bị **ẩn** khi component bật. `PlayerVisual` không bị sửa dòng nào. Tắt component thì kiếm cũ hiện lại.
7. **Đổi trang bị:** thay mảng `weaponPoses` / `shieldViews` / `slashFrames`; bảng anchor giữ nguyên.

## 3. Kiểm thử trong Play Mode (16:9 = 1920×1080 → ảnh 1280×720; 9:16 = 576×1024)

Mỗi trạng thái được dừng đúng frame: `timeScale = 0`, chạy Animator tới frame cần chụp (vì khi đóng băng, `PlayerVisual` đặt speed về 0), rồi chụp. Toạ độ Hero trong ảnh ghi ở `capture_positions.txt`.

| # | Trạng thái | 16:9 | 9:16 | Kết quả |
|---|---|---|---|---|
| 1 | Idle | `L01_idle_side` (đứng quay mặt về goblin gần nhất) | `P01_idle_up` | ✔ Kiếm hạ có găng, khiên thủ |
| 2 | Walk phải | `L02_walk_right` | `P02_walk_right` | ✔ `flipX = true`, kiếm sau thân, khiên trước |
| 3 | Walk lên | `L03_walk_up` | `P03_walk_up` | ✔ Mặt trong khiên bên trái, kiếm bên phải |
| 4 | Attack phải (F1) | `L04_…` | `P04_…` | ✔ Kiếm chém ngang, vệt chém đi qua mũi kiếm |
| 5 | Attack trái (F1) | `L05_…` | `P05_…` | ✔ Mọi lớp lật đúng (`flipX = false`) |
| 6 | Attack xuống (F1) | `L06_…` | `P06_…` | ✔ Lưỡi liềm dưới chân |
| 7 | Attack lên (F1) | `L07_…` | `P07_…` | ✔ Vệt chém **sau** thân |
| 8 | Hurt (bị đánh từ phải) | `L08_hurt_side` | `P08_hurt_side` | ✔ Thân, kiếm, khiên cùng nhuốm đỏ |
| 9 | Death D2 | `L09_death` | `P09_death` | ✔ Nằm, khiên rơi cạnh chân, kiếm rơi |
| — | Console | | | ✔ **0 lỗi** (vẫn còn 1 cảnh báo cũ trong `TreeCanopyOcclusion`) |

Ảnh tổng: `L_crops_x3.png` (16:9, phóng ×3 quanh Hero), `P_crops_x2.png` (9:16, phóng ×2). Ảnh gốc: `L0*.png`, `P0*.png`.

## 4. Chưa kiểm / cần quyết

1. **Chưa thử đòn R thật** (bấm hoặc giữ R, chuỗi 3 đòn), skill, hitstop, đẩy lùi quái. Các ảnh dùng `PlaySlashSwing` gọi trực tiếp, nên **vệt chém cũ của `CombatVfx` không xuất hiện**. Khi đánh thật, cả hai vệt chém sẽ hiện cùng lúc.
   - `CombatVfx` dùng chung với Level 1–4 nên tôi không sửa.
   - **Đề xuất:** thêm một cờ chỉ bật trong Sandbox để tắt vệt chém cũ khi Hero có `EquipmentVisual`. Đây là thay đổi code nhỏ, cần anh/chị duyệt.
2. **Idle hướng Down** không chụp được, vì Hero tự quay về goblin gần nhất khi đứng yên (`PlayerCombat.UpdateFacing`). Clip Idle Down đã được kiểm qua tên sprite trong lúc chạy.
3. **Sandbox hẹp:** ở 16:9, Hero có thể đi ra ngoài nền đất (vùng xanh đậm). Đây là bố cục sẵn có của R1, không liên quan Aren.
4. **Lỗi công cụ chụp:**
   - `capture_game_view` mặc định kéo giãn ảnh 9:16 vào khung 1280×720 (lần chụp đầu bị méo). Đã chụp lại với `--width 576 --height 1024`.
   - Công cụ luôn lưu vào trong `Assets/`. Đã chuyển ảnh sang `Docs/` và xoá thư mục tạm.

## 5. File

- **Mới:**
  - `Assets/Art/Rebuild/R2C/Aren/**` (sheet, Extra, Gear, Animations, `Animators/Aren.controller`, `Aren_Anchors.csv`)
  - `Assets/Scripts/Player/EquipmentVisual.cs`
  - `Assets/Editor/R2CArenSandboxSetup.cs`
  - `Docs/ReferenceRebuild/R2/R2C_OriginalHero/unity_export/**`, `unity_test/**`, `tools/ArenUnityExport.cs`
- **Sửa:** `Assets/Scenes/ReferenceCombatSandbox.unity` (chỉ `Player`)
- **Không đổi:**
  - `PlayerVisual`, `PlayerCombat`, `CombatVfx`, `Health`
  - `Hero.controller`, `Hero_R2.controller` và sheet R2b
  - Phase 4 `Hero_Sheet` (md5 `b6b28203…` giữ nguyên)
  - Scene Level 1–4, LevelFlow, GameProgress, Enemy AI, Mob Labels, HP bar
