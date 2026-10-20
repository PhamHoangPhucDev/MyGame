# Aren — bộ kỹ năng riêng: spec kỹ thuật cho Cursor (bản nháp)

**Ngày:** 2026-10-20 · **Người viết:** Claude (chỉ art + spec, không sửa code hay scene)
**Thiết kế gốc:** `classkit/CLASS_KIT_DESIGN.md` mục 2 và 9 (bộ skill hiện tại chỉ là demo; mỗi class có bộ riêng).
**Ảnh mẫu VFX (chờ duyệt):** `aren_kit/AREN_KIT_VFX_CANDIDATE.png`. Sprite sheet đầy đủ chỉ làm sau khi ảnh mẫu được duyệt (mục 4).

## 0. Phạm vi và ràng buộc

- **Sandbox** (`ReferenceCombatSandbox`, ClassCombat F1): thay `ClassKit_Aren` từ bộ "giống hệt cũ" sang bộ dưới đây.
- **Campaign L1–4:** hiện Aren chạy không kit (bộ demo, theo `R3_AB_COMBAT_REPORT.md`). **Có đưa bộ này vào campaign hay không là quyết định của anh/chị.** Spec này không giả định điều đó.
- **Giữ nguyên:**
  - Đường combat duy nhất trong `PlayerCombat` (input, thời gian hồi, sự kiện cho hotbar).
  - `HitStop` (0.04–0.06 s), `CameraFollow.Shake`, `CombatTargeting`, `Health`.
  - `EquipmentVisual` và anchor của Aren.
  - Cách R3 phân biệt 3 đòn qua hướng vũ khí và vệt chém (một clip Attack cho thân).
- **Không** thêm frame thân mới trong bước này. Skill dùng clip Attack hoặc Walk có sẵn, cộng VFX.

## 1. Chỉ số gốc

Máu ×1.0, tốc chạy ×1.0. Không đổi so với hiện tại.

## 2. Combo R — "Kiếm Hiệp Sĩ"

| Đòn | Hướng vung | Sát thương | Vùng trúng | Đẩy lùi | Hitstop / rung | VFX |
|---|---|---|---|---|---|---|
| 1 | facing + perp × 0.85 | 18 | cung bán kính 1.35 (như hiện tại) | 2.8 | 0.048 / 0.0625–0.07 | vệt chém Aren đã duyệt |
| 2 | facing − perp × 0.85 | 18 | như trên | 2.8 | 0.048 / 0.0625–0.07 | vệt chém lật (R3) |
| 3 **đâm thẳng (mới)** | facing | **26** | **hình chữ nhật dài 1.8 × rộng 0.6**, bắt đầu cách 0.3 trước mặt | 3.5 | 0.058 / 0.09–0.10 | **`Aren_Thrust`** (vệt đâm trắng–xanh + tia vàng ở mũi). Thay vệt chém thường ở đòn 3 |

- **Cần thêm:**
  - `ComboStep.shape` (`Arc` / `Thrust`) cùng `thrustLength`, `thrustWidth`.
  - Một hàm `ApplyBoxDamage(origin, dir, length, width, …)` bên cạnh `ApplyAreaDamage`.
- Thời gian hồi 0.35 s và impact 0.12 s **giữ nguyên** (khớp clip Attack).

## 3. Năm skill

| Phím | Tên | Hành vi (`SkillBehaviour` đề xuất) | Số liệu | Thân | VFX (file sẽ giao) | Hitstop / rung |
|---|---|---|---|---|---|---|
| **2** | **Khiên Đập** | `ShieldBash` (mới): lao 2 ô theo hướng mặt trong 0.18 s; khi kết thúc gây sát thương hình nón 70° bán kính 1.4 phía trước, **choáng 0.6 s** | 24 · hồi 5 s · đẩy lùi 4.0 | Walk Side/Up/Down khi lao, rồi Attack F1 | `Aren_ShieldBash` (sóng nón vàng 3 frame), `Aren_ShieldGlint` (loé trên khiên 2 frame), `Aren_StunStars` (sao choáng 4 frame, lặp trên đầu quái) | 0.05 / 0.08 · 0.09 |
| **3** | **Xung Phong** | `Charge` (mới): lao 4 ô trong 0.30 s, mỗi quái trên đường bị trúng **1 lần** (độ rộng 0.8) | 26 · hồi 6 s · đẩy lùi 3.0 ngang hướng lao | Walk (tốc độ animator ×1.6) | **Bóng mờ runtime:** mỗi 0.04 s sinh 1 SpriteRenderer copy sprite thân hiện tại, màu `(0.55, 0.8, 1, 0.55)`, mờ dần trong 0.15 s, tối đa 4 bóng. `Aren_ChargeDust` (bụi 3 frame ở chân) | 0.05 / 0.06 · 0.08 |
| **4** | **Lốc Kiếm** | `Whirlwind` (có sẵn) với `useTint` hoặc sprite: 3 vòng | 20 × 3 · bán kính 2.2 · hồi 7 s · đẩy lùi 3.0 | Attack (skipWindup) mỗi vòng | `Aren_WhirlRing` (vòng elip 4 frame, tách **Back** vẽ sau thân và **Front** vẽ trước thân) | 0.05 / 0.08 · 0.09 |
| **V** | **Thành Lũy** | `Guard` + phản đòn (mở rộng): nhận ×0.35 sát thương trong 1.4 s, **phản 20%** sát thương nhận vào lên kẻ đánh | hồi 8 s | Idle | `Aren_Bulwark` (khiên lục giác vàng: xuất hiện 2 frame, lặp 4 frame, vỡ 2 frame; tách Back và Front) | — |
| **C** | **Chém Bình Minh** | `CrescentStrike` (có sẵn) với sprite VFX: vung trước 0.12 s, rồi lưỡi liềm lớn phía trước | 42 · bán kính 2.1 · hồi 10 s · đẩy lùi 5.2 | Attack | `Aren_Dawn` (lưỡi liềm lớn trắng–vàng + tia, 3 frame mỗi hướng) **thay** cho `CombatVfx` crescent | 0.06 / 0.12 · 0.12 |

**Phản đòn của Thành Lũy:** cần hook `Health.Damaged` (người đánh có thể lấy từ `hitPoint` hoặc `CombatTargeting.FindNearestEnemy`, giống cách `PlayerVisual.OnDamaged` đang làm). Phản bằng `TakeDamage(Round(applied × 0.2))`.

**Choáng của Khiên Đập:** cần một trạng thái choáng trên quái. Enemy AI thuộc nhóm hệ thống phải giữ nguyên, nên **cần anh/chị duyệt**. Phương án ít xâm lấn: component `StatusStun` tắt `EnemyController.enabled` trong 0.6 s rồi bật lại (cùng kiểu `StatusBleed` của Vex).

## 4. VFX sẽ giao (sau khi duyệt ảnh mẫu)

Quy ước giống bộ Aren hiện có:
- PNG pixel thật, nền trong suốt; import PPU 16, Point, không mipmap, không nén.
- Pivot = pivot thân `(16, 30)` trong ô 32×32 gốc của Aren, trừ khi ghi khác.
- Hướng Side vẽ **quay trái** như sheet thân; dùng flipX khi quay phải.
- Thứ tự vẽ tương đối với thân: Front = +3, Back = −3.

| File | Ô | Frame | Pivot | Gắn vào | Thời gian |
|---|---|---|---|---|---|
| `Aren_Thrust.png` | **80×80 (đã chốt 2026-10-20)** | 3 hướng × 3 (mạnh → mỏng → tàn) | (40,50) → Unity (0.5, 0.375) | thân, ở đòn 3 frame F1 và F2 | 0.06 / 0.05 / 0.05 s |
| `Aren_ShieldBash.png` | 64×64 | 3 hướng × 3 (nở dần) | (32,40) → (0.5, 0.375) | thân, khi kết thúc lao | 0.05 × 3 |
| `Aren_ShieldGlint.png` | 16×16 | 2 | giữa | anchor khiên (`off_x`, `off_y`) | 0.06 × 2 |
| `Aren_StunStars.png` | 16×16 | 4 (lặp) | giữa | trên đầu quái (+0.9 wu) | 8 fps trong lúc choáng |
| `Aren_ChargeDust.png` | 16×16 | 3 | (8,15) → (0.5, 0.0625) | chân Aren, mỗi 0.08 s khi lao | 0.06 × 3 |
| `Aren_WhirlRing_Back.png` / `_Front.png` | 64×48 | 4 | (32,34) → (0.5, 0.2917) | thân | 0.05 × 4 mỗi vòng |
| `Aren_Bulwark_Back.png` / `_Front.png` | 48×48 | 8 ô ngang: 0–1 xuất hiện, 2–5 lặp, 6–7 vỡ | (24,40) → (0.5, 0.1667) | thân | 0.06 / 8 fps / 0.06 |
| `Aren_Dawn.png` | 96×96 | 3 hướng × 3 | (48,64) → (0.5, 0.3333) | thân, ở frame impact của C | 0.06 / 0.06 / 0.08 s |

**Bảng màu:**
- Xanh băng: `FFFFFF` / `D8F0FF` / `8CC8F0` / `4A8AC8` (trùng vệt chém Aren đã duyệt).
- Vàng: `FFFFFF` / `FFF4C8` / `F0C850` / `C89A30`.
- Bụi: `C8B8A0` / `E8DCC8`.

## 5. Dữ liệu ClassKit cần thêm (đề xuất)

- **`SkillBehaviour`:** thêm `ShieldBash`, `Charge`; `Guard` thêm `reflectRatio`; `CrescentStrike` và `Whirlwind` thêm `vfxFrames` (đã có trường `vfxFrames` / `useTint` từ bộ Vex).
- **`SkillDef`:** `stunDuration`, `coneDeg`, `dashDistance`, `dashTime`, `pathWidth`, `reflectRatio`, `vfxFramesBack`, `vfxFramesFront`.
- **`ComboStep`:** `shape`, `thrustLength`, `thrustWidth`, `vfxFrames` (đòn 3 dùng `Aren_Thrust` thay vệt chém).
- `ClassKit_Aren.FillDesignedAren(...)` thay cho `FillLegacyAren()`. **Giữ** `FillLegacyAren()` để chạy kiểm thử hồi quy cho campaign khi còn chạy không kit.

## 6. Kiểm thử đề xuất cho Cursor

Đóng băng thời gian và đo, như báo cáo Vex:
1. **Đòn 3:** trúng quái ở khoảng cách 1.6 phía trước (cung cũ không với tới); **không** trúng quái lệch 0.6 sang bên.
2. **Khiên Đập:** quãng lao khoảng 2 ô; sát thương 24; quái bị choáng 0.6 s rồi hoạt động lại.
3. **Xung Phong:** quãng lao khoảng 4 ô; 3 quái trên đường mỗi con trúng đúng 1 lần.
4. **Lốc Kiếm:** 3 lượt trúng mỗi quái.
5. **Thành Lũy:** sát thương nhận ×0.35; quái đánh vào chịu 20%.
6. **Chém Bình Minh:** sát thương 42; VFX sprite thay crescent cũ.
7. **Hồi quy:** Vex (F5) giữ nguyên; campaign không lỗi.

## 7. Đã giao sprite sheet (2026-10-20)

- **Thư mục:** `aren_kit/sheets/`, gồm 10 PNG và `AREN_KIT_VFX_MANIFEST.csv`. Manifest ghi ô, số cột và hàng, pivot tính bằng px và pivot Unity đã quy đổi. **Cursor cắt sheet theo manifest này.**
- **Bố cục chung:**
  - Sheet theo hướng (Thrust, ShieldBash, Dawn) có 3 hàng = Down / Up / Side, mỗi hàng 3 cột = frame 0–2.
  - Side vẽ quay trái; dùng flipX khi quay phải.
  - Hàng Up vẽ **sau** thân.
- **Bảng duyệt:** `aren_kit/AREN_KIT_VFX_SHEETS_REVIEW.png`. Mọi frame được ghép trên thân Aren tại đúng pivot.
- **Đã kiểm bằng code:**
  - 0 pixel chạm viền ô ở cả 10 file (mỗi ô có lề trong suốt 1 px).
  - Chỉ dùng màu đã duyệt: xanh băng 4 màu, vàng 4 màu, bụi 3 màu.
  - Không khử răng cưa.
- **Chưa import Unity.** Thông số import: PPU 16, Point, không mipmap, không nén, Sprite Mode Multiple, cắt theo lưới ô trong manifest.
