# Brom — art set + skill kit: spec kỹ thuật cho Cursor (bản nháp, chờ duyệt)

**Ngày:** 2026-10-20 · **Người viết:** Claude (chỉ art + spec, không sửa code, scene hay prefab)
**Thiết kế gốc:** `classkit/CLASS_KIT_DESIGN.md` mục 1, 3 và 9 · **Ảnh mẫu đã duyệt:** `brom/BROM_CANDIDATE_v1.png`
**Nguồn sinh art:** `tools/BromGen.cs`, `tools/BromKit.cs`, chạy `tools/RenderBrom.ps1` (export, bảng duyệt và kiểm tra pixel).

## 0. Phạm vi và ràng buộc

- **Chỉ dùng trong Sandbox** (`ReferenceCombatSandbox`, ClassCombat F2 hoặc slot trống). Không đụng Level 1–4.
- Tái dùng **đúng hệ của Aren/Vex**: `PlayerVisual` (Side quay trái trong sheet, flip để quay phải), `EquipmentVisual` + CSV anchor, `ClassKit`/`ClassCombat`, `HitStop`, `CameraFollow.Shake`, `CombatTargeting`, `Health`.
- Art nằm trong `Docs/.../unity_export_brom/`, **chưa import vào `Assets/`**. Cursor import theo mục 1.
- **Cần anh/chị duyệt riêng** vì động vào hệ khác: choáng và khiêu khích (taunt) cần sửa AI quái; giảm sát thương gốc 15% phải tách khỏi Guard (mục 4).

## 1. File giao (`unity_export_brom/`)

Tất cả: PPU 16, Filter Point, Compression None, sprite mode Multiple (cắt theo ô), không mipmap.

| File | Ô | Lưới | Pivot (px từ trên-trái) | Unity pivot | Ghi chú |
|---|---|---|---|---|---|
| `Brom_Sheet.png` | 32×32 | 4×9 | 16, 30 | (0.5, 0.0625) | Hàng: Idle D/U/S, Walk D/U/S, Attack D/U/S (Attack 3 cột). Side quay trái |
| `Extra/Brom_Hurt_{Down,Up,Side}_{0,1}.png` | 32×32 | 1 | 16, 30 | (0.5, 0.0625) | 0 = loé trắng, 1 = ửng đỏ |
| `Extra/Brom_Death_Down_{0,1,2}.png` | 32×32 | 1 | 16, 30 | (0.5, 0.0625) | Quỳ → ngã → nằm (giữ frame 2) |
| `Gear/Brom_Hammer.png` | 48×48 | 8×1 | 24, 24 | (0.5, 0.5) | 8 tư thế R, UR, U, UL, L, DL, D, DR. Pivot = tay cầm. **Không xoay ở runtime** |
| `Gear/Brom_Shield.png` | 16×16 | 3×1 | 8, 8 | (0.5, 0.5) | Down (mặt khiên) / Up (quai sau) / Side (cạnh, quay trái) |
| `Gear/Brom_Hand.png` | 4×4 | 1 | 1, 1 | (0.25, 0.75) | Găng phủ lên cán búa. 4×4 kín ô là cố ý, giống của Aren |
| `Gear/Brom_ComboVfx.png` | 48×48 | 18×1 | 24, 38 | (0.5, 0.2083) | Chỉ số = step×6 + view×2 + frame (view: Down 0, Up 1, Side 2). Side quay trái |
| `Skills/Brom_GroundSlam.png` | 80×56 | 4×1 | 40, 36 | (0.5, 0.3571) | Lớp mặt đất, **sau thân** |
| `Skills/Brom_ShieldGlow.png` | 24×24 | 3×1 | 12, 12 | (0.5, 0.5) | Gắn vào anchor khiên |
| `Skills/Brom_ChargeDust.png` | 16×16 | 3×1 | 8, 15 | (0.5, 0.0625) | Ở chân |
| `Skills/Brom_Roar.png` | 64×64 | 4×1 | 32, 48 | (0.5, 0.25) | **Trước thân** |
| `Skills/Brom_TauntMark.png` | 16×16 | 2×1 | 8, 15 | (0.5, 0.0625) | Dấu "!" trên đầu quái bị khiêu khích |
| `Skills/Brom_IronWall_Back.png` / `_Front.png` | 48×48 | 6×1 | 24, 40 | (0.5, 0.1667) | Back sau thân, Front trước thân, cùng frame |
| `Skills/Brom_Quake.png` | 112×64 | 2×3 | 56, 40 | (0.5, 0.375) | Hàng = đợt 1–3, cột = mạnh/nhạt. Lớp mặt đất, sau thân |
| `Brom_Anchors.csv` | — | — | — | — | Cùng cột với Aren: `key,main_x,main_y,main_dir,main_order,hand_x,hand_y,off_x,off_y,off_view,off_order,slash,slash_order,off_dir` |
| `BROM_MANIFEST.csv` | — | — | — | — | Bảng trên ở dạng máy đọc |

**Pivot mặc định của mọi VFX** trùng pivot thân (chân Brom), trừ `ShieldGlow` gắn vào anchor khiên và `TauntMark` gắn trên đầu quái.

### Anchor (`Brom_Anchors.csv`)

- Toạ độ tính theo sheet (Side đã lật). Local position = ((x−16)/16, (30−y)/16), âm x khi flip. Hướng Side lật qua `MirrorDir {4,3,2,1,0,7,6,5}`, giống Aren.
- Key: `Idle_*`, `Walk_*`, `Attack_{view}_{0,1,2}`, `Hurt_{view}_{0,1}`, `Death_Down_{0,1,2}`.
- **Theo từng đòn combo:** `Attack_{view}_1@{step}` và `Attack_{view}_2@{step}`, với step 0/1/2. Frame 0 (vung búa lên) dùng chung cho cả 3 đòn.
- `slash` = view×2 + frame (0..5). Ô VFX cần lấy = step×6 + slash; −1 = không có VFX.
- `slash_order`: +3 trước thân, −3 sau thân. **Đòn 3 (nện đất) luôn −3** vì là vòng nứt trên mặt đất.
- `main_order` / `off_order`: >0 trước thân, <0 sau thân.
- `Death_Down_1`: main_x = −99 nghĩa là **ẩn toàn bộ trang bị** (đang ngã). `Death_Down_2`: búa rơi nằm cạnh người, ẩn găng.

## 2. Chỉ số gốc

Máu ×1.5 · tốc chạy ×0.85 · **giảm sát thương 15%** (luôn bật, xem mục 4) · hitstop 0.07 s · rung camera 0.12.

## 3. Combo R — "Búa Rèn"

Thân: clip Attack 3 frame (F0 vung lên, F1 đánh, F2 thu về) cho cả 3 đòn. Ba đòn khác nhau qua **tư thế búa** (anchor `@step`) và **VFX**, giống cách R3 làm với Aren.

| Đòn | Tư thế búa ở F1 | Sát thương | Vùng trúng | Đẩy lùi | Hitstop / rung | VFX (ô `Brom_ComboVfx`) |
|---|---|---|---|---|---|---|
| 1 vung ngang | ngang trước mặt | 22 | cung 150°, bán kính 1.5 | 3.0 | 0.07 / 0.10 | 0–5: cung bụi cam theo đầu búa |
| 2 bổ xuống | chéo xuống, đầu búa chạm đất trước mặt | 22 | cung 90°, bán kính 1.5 | 3.0 | 0.07 / 0.10 | 6–11: chấm va chạm + đá văng ở đầu búa |
| 3 **nện đất (kết)** | thẳng xuống, sát chân | **30** | **tròn quanh người, bán kính 1.6** | 4.0 (ra xa tâm) | **0.08 / 0.12** | 12–17: vòng nứt đất + tia lửa, **sau thân** |

- Thời gian hồi **0.5 s**. Impact ở **0.16 s** (đầu F1). Đề xuất fps Attack = 3 frame / 0.42 s.
- Frame VFX 0 hiện ở F1, frame 1 hiện ở F2.
- **Cần thêm vào `ComboStep`:** `shape = Circle` (vùng tròn quanh người, không cần hướng) cho đòn 3. Nếu đã có `ApplyAreaDamage(origin, radius)` thì chỉ cần cờ chọn.

## 4. Năm skill

| Phím | Tên | Hành vi (`SkillBehaviour` đề xuất) | Số liệu | Thân | VFX | Hitstop / rung |
|---|---|---|---|---|---|---|
| **2** | **Nện Đất** | `GroundSlam`: sau 0.15 s, đánh vùng tròn bán kính **2.2** quanh người, **choáng 0.8 s** | 28 · hồi 6 s · đẩy lùi 3.0 | Attack F0 → F1 (dùng anchor `@2`) | `Brom_GroundSlam` 4 frame × 0.07 s (sau thân) + 1 lần `Brom_ComboVfx` ô 12/13 | 0.08 / 0.14 |
| **3** | **Húc Khiên** | `ShieldCharge`: lao **2.5 ô** trong 0.28 s, mỗi quái trúng 1 lần (rộng 0.9), **đẩy sang hai bên** (vuông góc hướng lao, theo phía quái đứng) | 20 · hồi 6 s · đẩy lùi 4.0 | Walk (animator ×1.5) | `Brom_ShieldGlow` 3 frame × 0.06 s trên anchor khiên (lặp khi lao); `Brom_ChargeDust` 3 frame × 0.08 s, sinh mỗi 0.08 s tại chân | 0.06 / 0.08 |
| **4** | **Gầm Thách** | `Taunt`: mọi quái trong bán kính **4** đổi mục tiêu sang Brom trong **3 s**; Brom nhận **−20%** sát thương trong 3 s | hồi 10 s | Idle F2 (cúi đầu gầm) giữ 0.3 s | `Brom_Roar` 4 frame × 0.08 s (trước thân, pivot ở chân → vòng toả từ đầu); `Brom_TauntMark` lặp 2 frame × 0.2 s trên đầu mỗi quái bị kéo | 0 / 0.06 |
| **V** | **Tường Sắt** | `IronWall`: đứng yên **2 s** (khoá di chuyển, vẫn xoay hướng), chỉ nhận **20%** sát thương | hồi 10 s | Idle (giữ F0) | `Brom_IronWall_Back` + `_Front` cùng frame: 0–1 hiện (0.06 s mỗi frame), 2–3 lặp (0.15 s) đến hết 2 s, 4–5 vỡ (0.06 s) | 0 / 0 |
| **C** | **Động Đất** | `Quake`: 3 đợt cách nhau 0.35 s, bán kính **1.5 → 2.5 → 3.5**, mỗi đợt trúng mỗi quái 1 lần | 25 × 3 · hồi 14 s | Attack (lặp anchor `@2`) mỗi đợt | `Brom_Quake` hàng w (đợt w+1): cột 0 lúc đánh, cột 1 sau 0.12 s, tắt sau 0.25 s. Sau thân | 0.07 / 0.12 mỗi đợt |

### Ghi chú gameplay (cần duyệt)

- **Choáng:** Aren `ShieldBash` cũng cần trạng thái này. Đề xuất một component `StatusStun` chung (giống `StatusBleed`) và cho AI quái kiểm tra trước khi di chuyển hay đánh. Sao choáng: dùng tạm `aren_kit/sheets/Aren_StunStars.png` hoặc làm bản cam riêng nếu được yêu cầu.
- **Khiêu khích:** cần AI quái có "mục tiêu ép buộc" kèm thời hạn. Hiện quái luôn nhắm Player duy nhất, nên ở Sandbox một người chơi, taunt **chỉ có tác dụng kéo quái lại gần** (cho quái chạy về Brom 3 s, bỏ qua hành vi bỏ chạy hay giữ khoảng cách). Phần "đổi mục tiêu" chỉ có ý nghĩa khi có đồng đội hoặc NPC.
- **Giảm sát thương:** gốc 15%, Gầm Thách 20%, Tường Sắt 80%. Đề xuất **nhân chồng** (1 − 0.15) × (1 − 0.2)… và đặt chung một chỗ trong `Health` (ví dụ `damageTakenMultiplier`). Tách hẳn khỏi Guard demo cũ để Guard không ghi đè giá trị gốc.
- **Ngân sách sát thương:** DPS combo khoảng 74 / 1.5 s ≈ 49/s, so với Aren 62 / 1.05 s ≈ 59/s (−17%). Thấp hơn Aren một chút, hợp vai tank (mục 0 của thiết kế cho phép chênh khoảng 15%; có thể tăng đòn 3 lên 32 nếu muốn sát hơn).

## 5. Thứ tự vẽ (sorting)

Từ dưới lên: lớp mặt đất (`GroundSlam`, `Quake`, combo đòn 3, `ChargeDust`) → `IronWall_Back` → vật sau thân (order < 0) → **thân** → vật trước thân (order > 0) → combo đòn 1–2 (order +3, riêng view Up là −3) → `ShieldGlow` → `IronWall_Front` → `Roar`.

## 6. Kiểm thử đề xuất cho Cursor (Sandbox)

1. F2 chọn Brom → Idle, Walk ở 3 hướng, cả quay trái và phải: chân đứng yên một chỗ, búa và khiên không giật.
2. Giữ R → 3 đòn có tư thế búa khác nhau. Đòn 3 có vòng nứt sau thân và trúng quái phía sau lưng.
3. Hitstop 0.07–0.08 s, rung 0.12 (đo bằng `_remaining` như lần test Aren).
4. Phím 2 / C: quái trong bán kính bị trúng đúng 1 lần mỗi đợt. Phím 3: quái bị hất sang hai bên, không bị kéo theo.
5. Phím V: không di chuyển được 2 s, sát thương nhận = 20% × 0.85.
6. Hurt và Death dùng đúng file `Extra/`, trang bị ẩn ở `Death_Down_1`.
7. Level 1–4 không đổi.

## 7. Kiểm tra pixel đã chạy (2026-10-20, `RenderBrom.ps1`)

- **Mọi sheet:** 0 pixel ở viền ô (trừ găng 4×4 kín ô, cố ý), 0 pixel bán trong suốt (không khử răng cưa).
- **Màu:** thân dùng bảng màu roster Brom đã duyệt. FX dùng FFF0C8, F0A050, C8692E, 7A4426, tia lửa D8D8E0. Tường Sắt dùng xám thép của khiên.
- **Thân:** 14×21, cột 9–22, đế ở hàng 29 trong mọi frame (Attack F1 hạ người 1 px, chân vẫn ở hàng 29).

**Bảng duyệt:**
- `brom/Brom_body_sheet_review.png`: sheet thân, Hurt và Death.
- `brom/Brom_combo_review.png`: 3 hướng × (Idle, vung lên, 3 đòn, thu về), có trang bị và VFX.
- `brom/Brom_skills_gear_review.png`: 5 skill ghép lên Brom, 8 tư thế búa, khiên và toàn bộ ô combo VFX.
