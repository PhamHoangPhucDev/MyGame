# Lyra — art set + skill kit: spec kỹ thuật cho Cursor (bản nháp, chờ duyệt)

**Ngày:** 2026-10-20 · **Người viết:** Claude (chỉ art + spec, không sửa code, scene hay prefab)
**Thiết kế gốc:** `classkit/CLASS_KIT_DESIGN.md` mục 1, 4 và 9 · **Ảnh mẫu đã duyệt:** `lyra/LYRA_CANDIDATE_v6.png`
**Nguồn sinh art:** `tools/LyraGen.cs`, `tools/LyraKit.cs`, chạy `tools/RenderLyra.ps1` (export, bảng duyệt và kiểm tra pixel).

## 0. Phạm vi và ràng buộc

- **Chỉ dùng trong Sandbox** (ClassCombat, slot trống F3). Không đụng Level 1–4.
- Tái dùng hệ của Aren/Vex/Brom: `PlayerVisual` (Side quay trái trong sheet, flip để quay phải), `EquipmentVisual` + CSV anchor, `ClassKit`/`ClassCombat`, `HitStop`, `CameraFollow.Shake`, `CombatTargeting`, `Health`.
- **Lyra là class tầm xa đầu tiên.** Cần hệ đạn (`KitProjectile` đã có từ bộ Vex). Mục 3–4 ghi rõ chỗ nào cần mở rộng.
- Art nằm trong `Docs/.../unity_export_lyra/`, **chưa import vào `Assets/`**.
- **Cần anh/chị duyệt riêng:**
  - Làm chậm quái (dây leo) và né đòn (Bước Gió) động vào AI quái và `Health`.
  - Mưa Tên nhắm vào một vị trí, không nhắm vào quái.

## 1. File giao (`unity_export_lyra/`)

Tất cả: PPU 16, Filter Point, Compression None, sprite mode Multiple (cắt theo ô), không mipmap.

| File | Ô | Lưới | Pivot (px từ trên-trái) | Unity pivot | Ghi chú |
|---|---|---|---|---|---|
| `Lyra_Sheet.png` | 32×32 | 4×9 | 16, 30 | (0.5, 0.0625) | Hàng: Idle D/U/S, Walk D/U/S, Attack D/U/S. Attack 3 cột: **lắp tên / kéo dây / buông**, **tay giương cung vẽ sẵn trong thân**. Side quay trái |
| `Extra/Lyra_Hurt_{Down,Up,Side}_{0,1}.png` | 32×32 | 1 | 16, 30 | (0.5, 0.0625) | 0 = loé trắng, 1 = ửng đỏ |
| `Extra/Lyra_Death_Down_{0,1,2}.png` | 32×32 | 1 | 16, 30 | (0.5, 0.0625) | Quỳ → ngã → nằm (giữ frame 2) |
| `Gear/Lyra_Bow.png` | 32×32 | 8×4 | 16, 16 | (0.5, 0.5) | **Chỉ số = state×8 + dir.** dir: R, UR, U, UL, L, DL, D, DR. state: 0 nghỉ/buông, 1 lắp tên, 2 kéo hết dây. **Ô 24 = đầu cung nhô sau vai** (Down khi đeo sau lưng). Pivot = tay cầm. Không xoay ở runtime |
| `Gear/Lyra_Hand.png` | 4×4 | 1 | 1, 1 | (0.25, 0.75) | Nắm tay, dùng cho **cả 2 tay**: tay cầm cung (slot hand) và tay kéo dây (slot off). Kín ô là cố ý |
| `Gear/Lyra_Arrow.png` | 16×16 | 8×3 | 8, 8 | (0.5, 0.5) | Hàng: thường / nạp f0 / nạp f1. Cột = 8 hướng, vẽ sẵn, **không xoay** |
| `Gear/Lyra_ArrowTrail.png` | 32×32 | 8×4 | 16, 16 | (0.5, 0.5) | Hàng: thường f0, thường f1, nạp f0, nạp f1. **Pivot trùng pivot mũi tên**, nên đặt cùng vị trí với mũi tên |
| `Gear/Lyra_ComboVfx.png` | 48×48 | 18×1 | 24, 38 | (0.5, 0.2083) | Chỉ số = step×6 + view×2 + frame. view: Down 0, Up 1, Side 2. Frame 0 = lúc kéo dây (F1), frame 1 = lúc buông (F2). Side quay trái |
| `Skills/Lyra_FanBurst.png` | 64×64 | 3×3 | 32, 42 | (0.5, 0.3438) | Hàng Down/Up/Side (Side quay trái), 3 frame |
| `Skills/Lyra_LeafBurst.png` | 32×32 | 3×1 | 16, 30 | (0.5, 0.0625) | Lá bung khi nhảy lùi |
| `Skills/Lyra_VineSnare.png` | 64×40 | 5×1 | 32, 20 | (0.5, 0.5) | Vòng dây leo trên đất, bán kính 24 px (1.5 ô). Frame 0 mọc mầm, 1 lớn dần, 2–3 giữ (lặp), 4 héo |
| `Skills/Lyra_RainZone.png` | 80×48 | 2×1 | 40, 24 | (0.5, 0.5) | Vòng đánh dấu bán kính 32 px (2 ô), lặp 2 frame |
| `Skills/Lyra_RainArrow.png` | 16×32 | 3×1 | 8, 30 | (0.5, 0.0625) | Một mũi tên rơi: frame 0 rơi, 1 chạm đất, 2 tan. Pivot = điểm chạm |
| `Skills/Lyra_WindAura.png` | 32×32 | 4×1 | 16, 30 | (0.5, 0.0625) | Gió quanh chân, lặp suốt thời gian buff |
| `Skills/Lyra_DodgePuff.png` | 32×32 | 3×1 | 16, 30 | (0.5, 0.0625) | Lốc lá khi né được một đòn |
| `Skills/Lyra_Beam.png` | 32×32 | 9×2 | 16, 16 | (0.5, 0.5) | Hàng 0 ngang (hướng **phải**, flipX cho trái), hàng 1 dọc (hướng **xuống**, flipY cho lên). Cột: Start f0–2, Mid f0–2, End f0–2 |
| `Lyra_Anchors.csv` | — | — | — | — | Cùng cột với Aren/Brom (xem bên dưới) |
| `LYRA_MANIFEST.csv` | — | — | — | — | Bảng trên ở dạng máy đọc |

### Anchor (`Lyra_Anchors.csv`)

- Toạ độ tính theo sheet (Side đã lật). Local position = ((x−16)/16, (30−y)/16), âm x khi flip.
- **`main_*` = cung.** `main_dir` là **chỉ số ô trong `Lyra_Bow.png`** (0..24), không phải 0..7 như búa hay kiếm. Với Side, phần hướng đã được lật sẵn (state×8 + MirrorDir[dir]). Khi flip ở runtime, đổi ô: `idx = state*8 + MirrorDir[idx%8]`; ô 24 không đổi.
- **`hand_*`** = nắm tay giữ cung. −99 nghĩa là không có tay: Idle/Walk thì cung đeo sau lưng.
- **`off_*`** khi `off_view = Hand`: đây là **nắm tay kéo dây** (dùng `Lyra_Hand.png`), còn `off_order` là thứ tự vẽ. Ngoài Attack thì off = −99.
- **Idle/Walk:** cung đeo chéo giữa lưng.
  - Up: ô 5 (nghỉ, DL), vẽ **trước** thân.
  - Side: ô 7 sau khi lật, vẽ **sau** thân.
  - Down: ô 24 (đầu cung nhô sau vai phải), vẽ **sau** thân.
  - Walk F0/F2: y +1.
- **Attack:** `Attack_{view}_{0,1,2}` và `Attack_{view}_{1,2}@{step}` cho step 0/1/2. Tư thế giống nhau ở cả 3 phát, khác nhau ở VFX và mũi tên.
- `slash` = view×2 + frame (0..5). Ô VFX cần lấy = step×6 + slash. `slash_order`: +3 trước thân; view Up là −3 vì cung và VFX ở phía trên đầu, sau thân.
- `Death_Down_1`: main_x = −99 nghĩa là ẩn trang bị. `Death_Down_2`: cung rơi nằm trên đất (ô 2).

### Điểm bắn ra mũi tên (toạ độ sheet; Side = sheet đã lật)

| View | Tay cầm (main) | Hướng bắn | Điểm sinh mũi tên = pivot của `Lyra_Arrow` |
|---|---|---|---|
| Down | (18, 27) | xuống | (18, 31) → local (0.125, −0.0625) |
| Up | (16, 6) | lên | (16, 2) → local (0, 1.75) |
| Side (sheet quay trái) | (6, 21) | trái (flip → phải) | (2, 21) → local (−0.875, 0.5625), âm x khi flip |

Nếu `CombatTargeting` cho phép bắn chéo, dùng cột hướng tương ứng của `Lyra_Arrow`/`ArrowTrail`. Thân và cung vẫn dùng view gần nhất.

## 2. Chỉ số gốc

Máu ×0.85 · tốc chạy ×1.1 · giảm sát thương 0% · tầm đánh thường 6 ô · **hitstop 0.035 s, không rung** (chỉ rung ở C).

## 3. Combo R — "Mũi Tên Rừng"

Thân: clip Attack 3 frame (F0 lắp tên, F1 kéo dây, F2 buông). **Mũi tên sinh ra ở đầu F2.**

| Phát | Thời gian | Sát thương | Đạn | Tốc đạn / tầm | Xuyên | Hitstop | VFX |
|---|---|---|---|---|---|---|---|
| 1 nhanh | F0 0.06 · F1 0.08 · F2 0.12 | 14 | `Lyra_Arrow` hàng 0 + `ArrowTrail` hàng 0/1 | 14 ô/s · 6 ô | 0 (trúng 1 quái) | 0.035 | ô combo 0–5 |
| 2 nhanh | như trên | 14 | như trên | như trên | 0 | 0.035 | ô combo 6–11 (cùng hình phát 1) |
| 3 **nạp xuyên (kết)** | F0 0.06 · **F1 0.22** (giữ dây, có vòng sáng) · F2 0.14 | **24** | `Lyra_Arrow` hàng 1/2 (lặp 2 frame 0.06 s) + `ArrowTrail` hàng 2/3 | 18 ô/s · 7 ô | **3 quái** | 0.045 | ô combo 12–17 (vòng nạp + loé lá) |

- Thời gian hồi **0.38 s**. Combo reset theo `SlashChainReset` như các class khác.
- `ArrowTrail`: frame 0 lúc đạn đang bay, đổi sang frame 1 khi đạn còn khoảng 0.1 s; tắt cùng lúc với đạn.
- **Cần thêm vào `ComboStep`:** `shape = Projectile`, kèm `projectileSpeed`, `range`, `pierce`, `arrowRow`. `KitProjectile` phải hỗ trợ xuyên N quái (mỗi quái trúng 1 lần) và đổi sprite theo 8 hướng.

## 4. Năm skill

| Phím | Tên | Hành vi (`SkillBehaviour` đề xuất) | Số liệu | Thân | VFX | Hitstop / rung |
|---|---|---|---|---|---|---|
| **2** | **Tên Quạt** | `ProjectileFan` (có từ bộ Vex): 5 mũi tên toả quạt **60°** (−30, −15, 0, 15, 30) | 16 mỗi mũi · hồi 5 s · tầm 6 · tốc 14 ô/s | Attack F1 → F2 | `Lyra_FanBurst` hàng view, 3 frame × 0.06 s, đặt ở pivot thân. Mỗi mũi tên dùng `Lyra_Arrow` hàng 0 + trail, chọn cột hướng gần nhất | 0.035 / 0 |
| **3** | **Nhảy Lùi** | `LeapBack`: lùi **3 ô** ngược hướng mặt trong 0.22 s, không đổi hướng mặt. Đặt **vùng dây leo** ở chỗ đứng cũ, bán kính 1.5 ô, tồn tại 2 s: quái trong vùng **giảm 50% tốc chạy** | hồi 6 s | Walk (animator ×1.8) | `Lyra_LeafBurst` 3 frame × 0.06 s tại chỗ nhảy. `Lyra_VineSnare` (lớp mặt đất): 0–1 mỗi frame 0.08 s, 2–3 lặp 0.2 s đến hết 2 s, 4 héo 0.15 s | 0 / 0 |
| **4** | **Mưa Tên** | `ArrowRain`: vùng tròn bán kính **2 ô**, tâm cách **4 ô** phía trước; **6 lượt** trúng trong 1.2 s (mỗi 0.2 s), mỗi lượt trúng mọi quái trong vùng | 10 × 6 · hồi 8 s | Attack (Up F1 → F2, giơ cung lên) | `Lyra_RainZone` lặp 0.15 s suốt 1.2 s. Mỗi lượt sinh 3–4 `Lyra_RainArrow` ở vị trí ngẫu nhiên trong vùng: frame 0 rơi (0.1 s, từ cao 1 ô xuống), 1 chạm (0.06 s), 2 tan (0.1 s) | 0 / 0 |
| **V** | **Bước Gió** | `WindStep`: **+40% tốc chạy** trong 3 s; **né trọn 1 đòn kế tiếp** (đòn đầu tiên trong 3 s không gây sát thương), rồi mất khả năng né | hồi 9 s | Idle/Walk như thường | `Lyra_WindAura` lặp 4 frame × 0.08 s ở pivot thân. Khi né: `Lyra_DodgePuff` 3 frame × 0.06 s và thân nháy trắng (dùng `Hurt_*_0` 1 frame) | 0 / 0 |
| **C** | **Tên Xuyên Rừng** | `PierceBeam`: tia thẳng dài **9 ô** theo hướng view (4 hướng), **xuyên mọi quái** trên đường (rộng 0.5 ô), gây sát thương 1 lần | 45 · hồi 11 s · đẩy lùi 2.0 | Attack F1 giữ 0.25 s → F2 | `Lyra_Beam`: Start đặt ở điểm sinh mũi tên (mục 1), Mid lát mỗi 32 px (2 ô) đến 9 ô, End ở cuối. Ba frame lặp 0.05 s trong 0.35 s rồi tắt. Đặt hàng 0 cho trái/phải, hàng 1 cho lên/xuống | 0.05 / **0.10** |

### Ghi chú gameplay (cần duyệt)

- **Làm chậm:** cần `MoveSpeedMultiplier` trên quái, chồng được, có thời hạn (giống `PlayerMovement.SpeedMultiplier`). Vùng dây leo áp dụng mỗi frame cho quái đang đứng trong vùng.
- **Né đòn:** cần `Health` có cờ `dodgeNextHit` (bỏ qua 1 lần sát thương và bắn sự kiện để hiện `DodgePuff`). Không đụng i-frame của dash hiện có.
- **Mưa Tên:** vùng đặt cố định lúc bấm, không đi theo người chơi. Nếu bật auto-aim thì có thể đặt tâm vào quái gần nhất trong tầm 6 ô (đề xuất, cần duyệt).
- **Ngân sách sát thương:** combo khoảng 52 / 1.14 s ≈ 46/s. Thấp hơn Aren (≈59/s) vì Lyra đánh an toàn từ tầm 6 ô; phát 3 xuyên 3 quái bù lại khi đánh đông.

## 5. Thứ tự vẽ (sorting)

Từ dưới lên:
1. Lớp mặt đất: `VineSnare`, `RainZone`.
2. Vật sau thân (order < 0): cung đeo Down/Side; khi bắn view Up thì cung, tay và VFX.
3. **Thân.**
4. Vật trước thân (order > 0): cung đeo Up; khi bắn Down/Side thì cung + tay (+2), tay kéo dây (+3), VFX combo (+3).
5. `FanBurst`, `LeafBurst`, `DodgePuff`, `WindAura`.
6. Đạn và trail.
7. `RainArrow` (sort theo y điểm chạm).
8. `Beam`.

## 6. Kiểm thử đề xuất cho Cursor (Sandbox)

1. Chọn Lyra → Idle và Walk ở 3 hướng, quay trái và phải: cung đeo sau lưng, không có tay cầm cung. Down chỉ thấy đầu cung sau vai phải.
2. Giữ R → đủ 3 frame. Cung xuất hiện trong tay, dây kéo về cằm/má, mũi tên sinh ở đúng điểm (mục 1) và bay đúng hướng. Phát 3 xuyên 3 quái.
3. Flip Side: cung đổi đúng ô (`MirrorDir`), hai nắm tay nằm đúng trên tay cầm và dây.
4. Phím 2 / 4 / C: số lượt trúng đúng, mỗi quái trúng 1 lần mỗi lượt. Beam đủ dài 9 ô, không hở giữa các ô Mid.
5. Phím 3: lùi 3 ô, vùng dây leo làm chậm 50% trong 2 s. Phím V: tốc +40% trong 3 s, đòn đầu tiên bị né và hiện `DodgePuff`.
6. Hurt và Death dùng đúng file `Extra/`.
7. Level 1–4 không đổi.

## 7. Kiểm tra pixel đã chạy (2026-10-20, `RenderLyra.ps1`)

- **Mọi sheet:** 0 pixel bán trong suốt, 0 màu ngoài bảng.
  - Bảng = màu roster Lyra.
  - FX F8FFE0, B8F078, 6AC040, 2E7A2E, vàng F0D060.
  - Gỗ cung 8A5A34 / B07A48 / 5E8A34, dây EEE8DC, viền 2A2218, đầu mũi tên D8DCE4.
  - Héo 7A6A50 / 9A8A60.
- **Viền ô = 0 pixel**, trừ hai chỗ cố ý: `Lyra_Hand.png` (4×4 kín ô, như Aren và Brom) và `Lyra_Beam.png` (ô Start, Mid, End chạm mép ở chỗ nối để lát liền).
- **Thân:** 14×21, đế ở hàng 29. Walk F0/F2 hạ người 1 px, chân vẫn ở hàng 29.
- **Thay đổi nhỏ so với ảnh mẫu v6:** view Down kéo dây hết 4 px như các hướng khác (v6 là 3 px). Dây giờ tới cằm, và một sheet cung dùng chung cho mọi hướng.

**Bảng duyệt:**
- `lyra/Lyra_body_sheet_review.png`: sheet thân, Hurt và Death.
- `lyra/Lyra_combo_review.png`: 3 hướng × (Idle, Walk, lắp tên, kéo dây, buông phát 1, buông phát 2, nạp, buông tên nạp), có cung, tay, VFX và mũi tên minh hoạ.
- `lyra/Lyra_skills_gear_review.png`: 5 skill (ghép lên Lyra nếu gắn vào thân), sheet cung, mũi tên, trail và ô combo VFX.
