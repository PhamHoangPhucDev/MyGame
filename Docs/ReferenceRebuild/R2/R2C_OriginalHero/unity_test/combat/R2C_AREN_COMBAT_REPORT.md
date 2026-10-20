# R2C — Aren: tắt vệt chém cũ + thử đòn R thật, skill, hitstop

**Ngày:** 2026-10-20 · **Nhánh:** `reference_rebuild_r2c` · **Chưa commit** · Chỉ Sandbox

## 1. Cờ tắt vệt chém cũ

| File | Thay đổi |
|---|---|
| `Assets/Scripts/Player/EquipmentVisual.cs` | Thêm `replaceLegacyRSlash` (mặc định bật) và hàm `EquipmentVisual.ReplacesLegacySlash(component)`. Hàm trả về true chỉ khi Hero **có** `EquipmentVisual` đang bật và có đủ 6 frame vệt chém |
| `Assets/Scripts/Combat/PlayerCombat.cs` | `TickSlashStrike` (đòn R) bỏ qua `CombatVfx.SpawnSwordSlashFrames` / `SpawnSwordSlash` khi hàm trên trả về true. **+3 / −1 dòng.** Sát thương, hitstop, rung camera, âm thanh không đổi |

**Phạm vi:**
- Level 1–4 không có `EquipmentVisual` (scene `GameplayPrototype_Phase4Test`: 0 tham chiếu), nên đòn R ở đó vẫn tạo vệt chém cũ như trước.
- Boss (`BossWarlordController`) và skill **Mace** vẫn dùng vệt chém riêng của chúng. Cờ chỉ áp dụng cho đòn R.

## 2. Cách thử

- Chạy Play Mode ở 16:9 (1920×1080). Hero được đặt cạnh nhóm goblin và cho bất tử 600 s chỉ trong lúc chạy (`Health.SetSurvival`), để bài thử không bị ngắt.
- Đi đúng đường combat thật:
  - **Bấm R:** `PlayerCombat.TryCast(SkillId.Sword)`, cùng hàm mà phím R / Space gọi.
  - **Giữ R:** `SetSwordHeld(true)`, đi qua `TickSwordHold` như khi giữ phím.
  - **Skill:** `TryCast(FireDash / MultiSlash / MawBite / Bone / Mace)`.
- **Probe tạm thời** (`EditorApplication.update`, không thêm code vào project) ghi mỗi thay đổi vào `combat_probe.csv`: `timeScale`, bước chuỗi, frame thân, vệt chém mới bật hay tắt, số đối tượng vệt chém cũ (`CombatVfx` kiểu `SwordSlash`), tổng HP quái, `HitStop.LastDuration`.

## 3. Kết quả

| Bài thử | Đòn trúng (frame F1) | Vệt chém mới | Vệt chém cũ (nhiều nhất cùng lúc) | Hitstop | HP quái | Kết luận |
|---|---|---|---|---|---|---|
| **Bấm R** (1 lần) | 1 | ✔ có ở F1 và F2 | **0** | 1 lần, 0.049 s | 400 → 310 | ✔ |
| **Giữ R 3.5 s** | khoảng 10 đòn (probe ghi được 8 lần F1; vài frame 0.07 s bị lấy mẫu sót) | ✔ có ở mọi đòn | **0** | 3 lần (0.049 / 0.058 / 0.048 s), chỉ khi còn quái trong tầm | 310 → 100 (nhóm goblin chết) | ✔ |
| **Chuỗi 3 đòn** | Bước chuỗi lặp `1 → 2 → 0 → 1 → 2 → 0…` | ✔ | 0 | Đòn kết (bước 0) dùng **0.058 s**, đòn thường 0.048–0.049 s | — | ✔ khớp `ApplyHitFeedback` |
| Thả R | Đòn đang dở chạy xong, rồi về Idle | ✔ | 0 | — | — | ✔ |
| Skill **MawBite** (xoáy) | 2 | ✔ (dùng `PlaySlashSwing`) | 0 | 2 lần, 0.051 s | 80 → 60 | ✔ |
| Skill **Mace** (trăng lưỡi liềm) | 1 | ✔ | **1** (vệt riêng của skill, giữ theo thiết kế) | 0.062 s | 60 → 18 | ✔ |
| Skill **FireDash**, **MultiSlash** | 0 | — | 0 | — | không trúng | ⚠ Cast thành công (`TryCast = true`), nhưng ở vị trí thử không còn quái trong tầm. Skill không dùng frame Attack của thân, nên thay đổi lần này không ảnh hưởng |
| Skill **Bone** (thủ) | — | — | 0 | — | — | ✔ Cast được. Ảnh chụp lúc này cho thấy Hurt (Gobrider đánh trúng −10): thân, kiếm, khiên cùng nhuốm đỏ |
| Console | | | | | | ✔ **0 lỗi** |

**Kết luận chính:** đòn R giờ chỉ còn **một** vệt chém, dính vào mũi kiếm. Chuỗi 3 đòn, hitstop (kể cả hitstop dài hơn của đòn kết) và sát thương chạy như trước. Không còn tình trạng 2 vệt chém chồng nhau.

## 4. Ảnh

Thư mục `unity_test/combat/`:
- `combat_crops_x3.png`: phóng ×3 quanh Hero.
- `R01–R03_hold_*.png`: đang giữ R giữa nhóm goblin. Thấy windup với kiếm và khiên, số sát thương 18, không có vệt cũ.
- `S_*.png`: sau mỗi skill.

**Lưu ý:** ảnh chụp qua CLI trễ khoảng 0.25 s, nên ảnh skill phần lớn chụp sau khi hiệu ứng đã tắt. Bằng chứng cho skill nằm ở `combat_probe.csv`, không ở ảnh. Ảnh từng frame Attack có vệt chém xem ở báo cáo trước (`L_crops_x3.png`, `P_crops_x2.png`).

## 5. Chưa kiểm

- Bấm phím thật trên bàn phím (Input System). Bài thử gọi đúng các hàm mà phím gọi, nhưng không đi qua `Keyboard.current`.
- FireDash và MultiSlash khi trúng quái.
- Chạy liên tục 60 s ("thân không bao giờ biến mất").
- Che khuất bởi tán cây khi Hero đứng sau cây.
