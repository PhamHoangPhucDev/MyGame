# Selene — art set + skill kit: spec kỹ thuật cho Cursor (bản nháp, chờ duyệt)

**Ngày:** 2026-10-20 · **Người viết:** Claude (chỉ art + spec, không sửa code, scene hay prefab)
**Thiết kế gốc:** `classkit/CLASS_KIT_DESIGN.md` mục 1, 5 và 9 · **Ảnh mẫu đã duyệt:** `selene/SELENE_CANDIDATE_v2.png` (gậy ngắn, ngọc tròn)
**Nguồn sinh art:** `tools/SeleneGen.cs`, `tools/SeleneKit.cs`, chạy `tools/RenderSelene.ps1` (export, bảng duyệt và kiểm tra pixel).

## 0. Phạm vi và ràng buộc

- **Chỉ dùng trong Sandbox** (ClassCombat, slot trống F4). Không đụng Level 1–4.
- Tái dùng hệ của Aren/Vex/Brom/Lyra: `PlayerVisual`, `EquipmentVisual` + CSV anchor, `ClassKit`/`ClassCombat`, `KitProjectile` (đạn, xem spec Lyra mục 3), `HitStop`, `CameraFollow.Shake`, `CombatTargeting`, `Health`.
- Art nằm trong `Docs/.../unity_export_selene/`, **chưa import vào `Assets/`**.
- **Cần anh/chị duyệt riêng:**
  - Làm chậm quái (Vòng Phù Chú) dùng chung cơ chế với dây leo của Lyra.
  - Khiên hấp thụ (Khiên Ma Lực) động vào `Health`.
  - Dịch chuyển cần kiểm tra va chạm tường.

## 1. File giao (`unity_export_selene/`)

Tất cả: PPU 16, Filter Point, Compression None, sprite mode Multiple (cắt theo ô), không mipmap.

| File | Ô | Lưới | Pivot (px từ trên-trái) | Unity pivot | Ghi chú |
|---|---|---|---|---|---|
| `Selene_Sheet.png` | 32×32 | 4×9 | 16, 30 | (0.5, 0.0625) | Hàng: Idle D/U/S, Walk D/U/S, Attack D/U/S. Attack 3 cột: **giơ gậy / niệm / thu**; động tác nằm ở gậy. Side quay trái |
| `Extra/Selene_Hurt_{Down,Up,Side}_{0,1}.png` | 32×32 | 1 | 16, 30 | (0.5, 0.0625) | 0 = loé trắng, 1 = ửng đỏ |
| `Extra/Selene_Death_Down_{0,1,2}.png` | 32×32 | 1 | 16, 30 | (0.5, 0.0625) | Quỳ → ngã → nằm (giữ frame 2) |
| `Gear/Selene_Staff.png` | 32×32 | 8×2 | 16, 16 | (0.5, 0.5) | **Chỉ số = len×8 + dir.** dir: R, UR, U, UL, L, DL, D, DR. len 0 = gậy dài (nghỉ, giơ), len 1 = gậy ngắn (nghiêng khi niệm). Pivot = tay cầm. Không xoay ở runtime |
| `Gear/Selene_Hand.png` | 4×4 | 1 | 1, 1 | (0.25, 0.75) | Nắm tay trên cán gậy (slot hand). Kín ô là cố ý |
| `Gear/Selene_Orb.png` | 16×16 | 4×1 | 8, 8 | (0.5, 0.5) | Cột: thường f0, thường f1, lớn f0, lớn f1. Hình tròn, không có hướng |
| `Gear/Selene_Sparkle.png` | 8×8 | 3×1 | 4, 4 | (0.5, 0.5) | Hạt lấp lánh rơi sau cầu phép: 3 frame |
| `Gear/Selene_Charge.png` | 24×24 | 2×2 | 12, 12 | (0.5, 0.5) | Ngọc sáng lúc Attack F0. Hàng: thường / phát kết; 2 frame. **Pivot = tâm ngọc** |
| `Gear/Selene_ComboVfx.png` | 48×48 | 18×1 | 24, 38 | (0.5, 0.2083) | Chỉ số = step×6 + view×2 + frame. Frame 0 = loé ở ngọc lúc niệm (F1), frame 1 = tàn sáng (F2). Side quay trái |
| `Gear/Selene_OrbHit.png` | 32×32 | 3×1 | 16, 16 | (0.5, 0.5) | Cầu phép trúng (phát 1–2) |
| `Gear/Selene_StarBurst.png` | 48×48 | 4×1 | 24, 28 | (0.5, 0.4167) | Phát 3: nổ sao tại mục tiêu, vòng đất bán kính 19 px (1.2 ô). Pivot = tâm vòng trên đất |
| `Skills/Selene_Nova.png` | 80×56 | 4×1 | 40, 34 | (0.5, 0.3929) | 2 Cầu Tinh Tú: nổ, vòng đất tới bán kính 29 px (1.8 ô) |
| `Skills/Selene_Blink.png` | 32×32 | 4×1 | 16, 30 | (0.5, 0.0625) | 3 Dịch Chuyển: 0 tụ sáng, 1 cột sáng, 2 bụi sao, 3 tan |
| `Skills/Selene_Rune.png` | 80×40 | 5×1 | 40, 20 | (0.5, 0.5) | 4 Vòng Phù Chú: bán kính 35 px (2.2 ô), sao 4 cánh xoay. Frame 0 hiện, 1–3 lặp, 4 tắt. Lớp mặt đất |
| `Skills/Selene_Bubble.png` | 48×48 | 6×1 | 24, 40 | (0.5, 0.1667) | V Khiên Ma Lực: 0 hiện, 1–2 giữ (lặp), 3 loé khi đỡ đòn, 4–5 vỡ. **Trước thân** |
| `Skills/Selene_Meteor.png` | 32×48 | 4×1 | 16, 44 | (0.5, 0.0833) | C Mưa Sao Băng, một sao: 0–1 rơi chéo, 2 chạm, 3 tan. Pivot = điểm chạm |
| `Skills/Selene_MeteorZone.png` | 112×56 | 2×1 | 56, 28 | (0.5, 0.5) | Vòng đánh dấu bán kính 48 px (3 ô), lặp 2 frame. Lớp mặt đất |
| `Selene_Anchors.csv` | — | — | — | — | Cùng cột với Aren/Brom/Lyra (xem bên dưới) |
| `SELENE_MANIFEST.csv` | — | — | — | — | Bảng trên ở dạng máy đọc |

### Anchor (`Selene_Anchors.csv`)

- Toạ độ tính theo sheet (Side đã lật). Local position = ((x−16)/16, (30−y)/16), âm x khi flip.
- **`main_*` = gậy**, `main_dir` = **chỉ số ô trong `Selene_Staff.png`** (0..15), như cách Lyra dùng cho cung. Với Side, phần hướng đã được lật sẵn. Khi flip ở runtime, đổi ô: `idx = (idx/8)*8 + MirrorDir[idx%8]`.
- `hand_*` = nắm tay trên cán gậy. `off_*` không dùng (−99).
- **Idle/Walk:** gậy dựng đứng trong tay phải, vẽ trước thân. Walk F0/F2: y +1.
- **Attack:** `Attack_{view}_{0,1,2}` và `Attack_{view}_{1,2}@{step}`. Tư thế giống nhau ở cả 3 phát; khác nhau ở VFX, `Charge` và loại đạn.
  - View Up: gậy ở F0/F1 vẽ sau thân (order −2).
- `slash` = view×2 + frame. Ô VFX = step×6 + slash. `slash_order`: +3 trước thân, Up là −3.
- `Death_Down_1`: −99 nghĩa là ẩn gậy. `Death_Down_2`: gậy nằm ngang trên đất, không có tay.

### Vị trí ngọc (toạ độ sheet; Side = sheet đã lật)

| Lúc | Down | Up | Side (sheet quay trái) |
|---|---|---|---|
| F0 giơ gậy: tâm `Selene_Charge` | (10, 10) → local (−0.375, 1.25) | (21, 10) → (0.3125, 1.25) | (10, 10) → (−0.375, 1.25), âm x khi flip |
| F1 niệm: **điểm sinh cầu phép** | (27, 31) → (0.6875, −0.0625) | (27, 11) → (0.6875, 1.1875) | (3, 14) → (−0.8125, 1.0), âm x khi flip |

## 2. Chỉ số gốc

Máu ×0.8 · tốc chạy ×1.0 · giảm sát thương 0% · tầm đánh thường 5.5 ô · **hitstop 0.04 s**, rung nhẹ chỉ khi nổ (0.05).

## 3. Combo R — "Tia Phép"

Thân: clip Attack 3 frame (F0 giơ gậy, ngọc sáng; F1 niệm, cầu phép bay ra từ ngọc; F2 thu gậy).

| Phát | Thời gian | Sát thương | Đạn | Tốc đạn / tầm | Khi trúng | Hitstop / rung | VFX |
|---|---|---|---|---|---|---|---|
| 1 | F0 0.10 · F1 0.10 · F2 0.12 | 15 | `Selene_Orb` cột 0/1 (lặp 0.08 s) | 10 ô/s · 5.5 ô | `Selene_OrbHit` 3 frame × 0.05 s | 0.04 / 0 | `Charge` hàng 0 ở F0, ô combo 0–5 |
| 2 | như trên | 15 | như trên | như trên | như trên | 0.04 / 0 | ô combo 6–11 |
| 3 **nổ sao (kết)** | F0 **0.18** · F1 0.10 · F2 0.14 | **26** (vùng) | `Selene_Orb` cột 0/1 | 10 ô/s · 5.5 ô | **Nổ vùng bán kính 1.2 ô** tại chỗ chạm, hoặc ở cuối tầm nếu không trúng ai: `Selene_StarBurst` 4 frame × 0.06 s | 0.05 / 0.05 | `Charge` hàng 1 ở F0, ô combo 12–17 |

- Thời gian hồi **0.4 s**.
- **Lấp lánh:** khi cầu phép bay, cứ 0.04 s sinh 1 `Selene_Sparkle` tại vị trí đạn. Hạt đứng yên, chạy 3 frame × 0.06 s rồi biến mất.
- **Cần thêm vào `ComboStep`:** `onHit = Explode(radius)` cho phát 3, nổ cả khi đạn hết tầm.

## 4. Năm skill

| Phím | Tên | Hành vi (`SkillBehaviour` đề xuất) | Số liệu | Thân | VFX | Hitstop / rung |
|---|---|---|---|---|---|---|
| **2** | **Cầu Tinh Tú** | `SlowOrb`: cầu lớn bay chậm theo hướng ngắm, nổ khi chạm quái hoặc hết tầm, **vùng bán kính 1.8 ô** | 32 · hồi 5 s · tốc 5 ô/s · tầm 5.5 | Attack (F0 0.2 s → F1) | `Selene_Orb` cột 2/3 + `Sparkle`. Nổ: `Selene_Nova` 4 frame × 0.07 s | 0.06 / 0.08 |
| **3** | **Dịch Chuyển** | `Blink`: dịch chuyển **3.5 ô** theo hướng mặt. Nếu trúng tường, dừng ở ô trống cuối cùng. Bất tử 0.1 s | hồi 5 s | Ẩn thân ở frame 1–2 của Blink | Điểm đi: `Selene_Blink` 0→3 (0.05 s mỗi frame). Điểm đến: 1→3 | 0 / 0 |
| **4** | **Vòng Phù Chú** | `RuneZone`: vòng **bán kính 2.2 ô**, tâm tại mục tiêu gần nhất trong tầm 5.5, hoặc 3 ô trước mặt nếu không có. Trong **3 s**: quái trong vòng **giảm 40% tốc chạy**, nhận **8 sát thương mỗi 0.5 s** | 8 × 6 · hồi 9 s | Attack | `Selene_Rune` (lớp mặt đất): 0 hiện 0.1 s, 1–3 lặp 0.1 s mỗi frame suốt 3 s, 4 tắt 0.15 s. Mỗi lượt trúng: `Selene_OrbHit` frame 0 trên quái | 0 / 0 |
| **V** | **Khiên Ma Lực** | `ManaShield`: khiên **hấp thụ 40 sát thương** trong 4 s. Hết điểm hoặc hết giờ thì vỡ | hồi 10 s | Idle/Walk như thường | `Selene_Bubble`: 0 hiện 0.08 s, 1–2 lặp 0.15 s. Mỗi lần đỡ đòn: frame 3 trong 0.06 s. Vỡ: 4–5 mỗi frame 0.06 s | 0 / 0 |
| **C** | **Mưa Sao Băng** | `MeteorShower`: vùng **bán kính 3 ô** tại mục tiêu hoặc 4 ô trước mặt; **5 sao** rơi trong 1.5 s (mỗi 0.3 s, vị trí ngẫu nhiên trong vùng). Mỗi sao gây sát thương vùng bán kính 0.8 ô | 28 mỗi sao · hồi 13 s | Attack (F0 giữ 0.25 s → F1) | `Selene_MeteorZone` lặp 0.15 s suốt 1.5 s. Mỗi sao: `Selene_Meteor` 0–1 rơi (0.06 s mỗi frame), 2 chạm (0.08 s), 3 tan (0.1 s). **Loé màn hình tím rất nhẹ** (alpha 0.08, 0.1 s) lúc sao đầu tiên chạm | 0.04 / 0.06 mỗi sao |

### Ghi chú gameplay (cần duyệt)

- **Làm chậm:** dùng chung `MoveSpeedMultiplier` của quái như dây leo của Lyra. Rời vùng thì hết chậm.
- **Khiên hấp thụ:** cần `Health` có `shieldPoints` trừ trước máu, có sự kiện để hiện frame 3 và frame vỡ. Không chồng với khiên của class khác.
- **Ngân sách sát thương:** combo khoảng 56 / 1.2 s ≈ 47/s. Phát 3 nổ vùng bù lại khi đánh đông. Selene máu ×0.8 nên đứng xa (5.5 ô).

## 5. Thứ tự vẽ (sorting)

Từ dưới lên:
1. Lớp mặt đất: `Rune`, `MeteorZone`, vòng đất của `StarBurst`/`Nova`. Hai file sau có cả vòng đất và sao nổ; chỉ cần sort theo y của pivot.
2. Gậy khi order < 0 (Up F0/F1).
3. **Thân.**
4. Gậy + tay (+2), VFX combo (+3), `Charge` (+3; view Up là −3).
5. `Blink`, `Bubble`.
6. Đạn, `Sparkle`, `OrbHit`.
7. `Meteor` (sort theo y điểm chạm).

## 6. Kiểm thử đề xuất cho Cursor (Sandbox)

1. Chọn Selene → Idle và Walk ở 3 hướng, quay trái và phải: gậy dựng đứng trong tay, nắm tay nằm trên cán.
2. Giữ R → F0 ngọc sáng (`Charge`), F1 cầu phép sinh **đúng ở ngọc** (mục 1), F2 thu gậy. Phát 3 nổ sao bán kính 1.2 ô, kể cả khi không trúng ai.
3. Flip Side: gậy đổi đúng ô (`MirrorDir`), ngọc và điểm sinh đạn đổi theo.
4. Phím 2 / 4 / C: vùng nổ đúng bán kính, số lượt trúng đúng. Phím 3: không xuyên tường. Phím V: hấp thụ đúng 40 rồi vỡ.
5. Hurt và Death dùng đúng file `Extra/`.
6. Level 1–4 không đổi.

## 7. Kiểm tra pixel đã chạy (2026-10-20, `RenderSelene.ps1`)

- **Mọi sheet:** 0 pixel bán trong suốt, 0 màu ngoài bảng.
  - Bảng = màu roster Selene.
  - FX FFFFFF, FFB8F0, D860C8, 7A3AA8, B8D8FF.
  - Gậy 6A4428 / 9A6A3C / vàng E8C048; ngọc FFD0F0 / C04A8A / 7A2A5A; viền 1A1426.
- **Viền ô = 0 pixel**, trừ `Selene_Hand.png` (4×4 kín ô, cố ý).
- **Thân:** 14×21, đế ở hàng 29. Walk F0/F2 hạ áo choàng 1 px, chỉ giày chạm hàng 29.
- **Vòng Phù Chú** dùng **sao 4 cánh** (motif của Selene), không dùng ngôi sao năm cánh.

**Bảng duyệt:**
- `selene/Selene_body_sheet_review.png`: sheet thân, Hurt và Death.
- `selene/Selene_combo_review.png`: 3 hướng × (Idle, Walk, giơ gậy, niệm, cầu phép bay, giơ gậy phát 3, niệm phát 3, phát 3), có gậy, ngọc sáng, VFX và cầu phép minh hoạ.
- `selene/Selene_skills_gear_review.png`: nổ sao, cầu trúng, 5 skill, sheet gậy, cầu phép, lấp lánh, ngọc sáng và ô combo VFX.
