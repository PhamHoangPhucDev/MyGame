# Hệ thống trang bị & skin (đặc tả + bản mẫu)

**Ngày:** 2026-10-20 · **Chỉ là đặc tả và ảnh mẫu, chưa sửa Unity**
**Bản mẫu:** `tools/EquipGen.cs` + `tools/RenderEquip.ps1` (tạo lại toàn bộ ảnh trong thư mục `attack/`).

## 1. Nguyên tắc

1. **Thân không bao giờ vẽ vũ khí, khiên hay vật đeo lưng.** Mỗi món là một sprite riêng.
2. **Mỗi frame thân công bố anchor:**
   - `grip_main`: tay cầm vũ khí chính.
   - `grip_off`: tay cầm khiên hoặc vũ khí phụ.
   - `back_mount`: điểm gắn đồ sau lưng.
   - Kèm theo **tư thế vũ khí** (1 trong 8 hướng) và **thứ tự vẽ** (trước hoặc sau thân).
3. **Vũ khí vẽ sẵn theo tư thế, không xoay lúc chạy.** Pixel art xoay sẽ bị méo. Hiện tại `PlayerVisual` đang xoay kiếm ±70°; cách mới thay thế việc đó.
4. **Đổi trang bị** = thay bộ sprite của món đó. Anchor của thân giữ nguyên.
5. **Đổi skin:**
   - Thay sheet thân, giữ nguyên tên slice và anchor.
   - Hoặc chỉ đổi palette, nếu skin chỉ khác màu (ví dụ skin "Crimson" của Aren).

## 2. Lớp hiển thị (thứ tự vẽ)

| Lớp | Nội dung | Ghi chú |
|---|---|---|
| `back_item` | Ống tên, kiếm đeo lưng, áo choàng phụ | Sau thân khi nhìn Down/Side, trước thân khi nhìn Up |
| `main_behind` / `off_behind` | Vũ khí hoặc khiên khi ở phía sau | Theo cột `*_order` của từng frame |
| `body` | Sheet thân (skin) | Ô 32×32, pivot (0.5, 0.0625), PPU 16 |
| `off_front` | Khiên khi ở phía trước | |
| `main_front` | Vũ khí khi ở phía trước | Luôn vẽ trên khiên |
| `hand` | Nắm tay 2×2 màu da của skin, vẽ đè lên chuôi vũ khí cầm phía trước (cung, gậy) | Đặt tại `grip_main` hoặc `grip_off`; màu lấy theo skin |
| `vfx` | Vệt chém, hiệu ứng | Gắn vào mũi vũ khí, không gắn vào thân |

## 3. Định dạng asset

| Loại | Kích thước | Anchor | Số sprite |
|---|---|---|---|
| Thân (skin) | Ô 32×32 | Pivot (0.5, 0.0625) | Như sheet hiện tại (Idle/Walk/Attack × 3 hướng) |
| Vũ khí dài (kiếm, búa, gậy) | Ô 48×48 | **Tâm ô = tay cầm** | 8 tư thế: R, UR, U, UL, L, DL, D, DR |
| Vũ khí ngắn (dao găm) | Ô 48×48 | Tâm ô | 8 tư thế |
| Khiên | Ô 16×16 | Tâm ô = quai cầm | 3 hướng: Down (mặt khiên), Up (mặt trong có quai), Side |
| Cung | Ô 16×16 | Tay cầm | 3 hướng (bản mẫu mới có Down) |
| Đồ đeo lưng | Ô 32×32 | `back_mount` | 3 hướng |

**Bảng anchor** (CSV hoặc ScriptableObject), mỗi dòng là một frame:
`character, view, frame, timing_s, body_slice, main_grip_x, main_grip_y, main_pose, main_order, off_grip_x, off_grip_y, off_pose, off_order`.
Ví dụ: `Aren_Attack_anchors_v1.csv`. Toạ độ tính bằng px trong ô 32×32 của thân (0,0 là góc trên trái).

## 4. Trang bị mặc định từng nhân vật

| Nhân vật | Tay chính | Tay phụ / lưng | Kiểu tấn công | Trạng thái |
|---|---|---|---|---|
| **Aren** (hiệp sĩ) | Kiếm dài | Khiên heater | Chém cận chiến | **Attack 3 hướng × 3 frame xong (v1)** |
| **Brom** (người lùn) | Búa | Khiên tròn lớn | Đập cận chiến | Đã có sprite vũ khí và tư thế đứng; chưa có Attack |
| **Lyra** (elf) | Cung | Ống tên sau lưng | Bắn tầm xa | Đã có sprite cung. **Ống tên đang vẽ dính vào thân, cần tách ra thành `back_item`** |
| **Selene** (phù thủy) | Gậy phép | — | Phép tầm xa | Đã có sprite gậy |
| **Vex** (sát thủ) | Dao găm | Dao găm (tay phụ, cầm ngược) | Đâm nhanh 2 tay | Đã có sprite dao |

Vũ khí "skin" làm mẫu: **Kiếm hàn** (đỏ đen), **Khiên gỗ tròn**, và **skin thân Crimson** (đổi palette của áo choàng và chỏm lông). Xem `Equip_swap_demo_v1.png`.

## 5. Kế hoạch đưa vào Unity (bước sau, cần duyệt riêng)

1. **ScriptableObject:**
   - `WeaponDef`: id, 8 sprite theo tư thế, loại (dài, ngắn, cung, khiên), màu VFX.
   - `ShieldDef`: 3 sprite theo hướng.
   - `SkinDef`: sheet thân hoặc bảng đổi palette.
   - `AnchorTable`: mỗi clip × frame → anchor, tư thế, thứ tự vẽ.
2. **`EquipmentVisual`** (component mới, gắn trên Hero):
   - Có 3–4 SpriteRenderer con: `Main`, `Off`, `Back`.
   - Mỗi frame đọc clip và frame hiện tại của Animator, tra `AnchorTable`, rồi đặt `localPosition` (px ÷ PPU 16), sprite theo tư thế, và `sortingOrder` = thân ± 1/2.
   - Hướng trái dùng flipX cho tất cả các lớp, kể cả anchor.
3. **Thay phần kiếm trong `PlayerVisual`:**
   - Bỏ vị trí nghỉ hard-code (`RestSword`).
   - Bỏ việc xoay ±70°.
   - Bỏ dòng ẩn kiếm khi đang Attack (`swordRenderer.enabled = false`). Dòng này là lý do kiếm biến mất trong Sandbox R2b.
4. **Vệt chém:** gắn vào mũi vũ khí của frame strike, để không còn bị tách rời khỏi Hero như hiện tại.
5. **Giữ nguyên:** combat (R, skill), hitbox, timing `slashImpactDelay`, LevelFlow, Enemy AI, sorting theo y.

## 6. Việc còn lại

- ~~Idle và Walk cầm vũ khí~~: **Aren đã xong** (`Aren_IdleWalk_anchors_v1.csv`). 4 nhân vật còn lại làm cùng cách khi vẽ bộ hoạt ảnh của họ.
- Tách ống tên của Lyra khỏi thân.
- Attack cho Brom, Lyra, Selene, Vex. Mỗi nhân vật dùng cùng hệ anchor, nhưng thân có tư thế riêng (đập, giương cung, vung gậy, đâm 2 tay).
- Cung 3 hướng (bản mẫu mới có Down).
