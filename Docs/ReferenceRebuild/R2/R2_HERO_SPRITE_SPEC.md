# R2 — HERO SPRITE SPEC

Trạng thái: **đặc tả, chờ duyệt**. Không có asset nào được sản xuất.

## 1. Ràng buộc kỹ thuật (giữ để không vỡ Unity)

| Mục | Giá trị | Nguồn |
|---|---|---|
| Ô frame | **32×32 px** (giữ như `Hero_Sheet.png`) | `.meta` hiện tại |
| PPU | 16 | Inventory |
| Pivot | **(0.5, 0.0625)** = chân tại hàng pixel 2 từ đáy | `.meta` hiện tại; R1 HeroExtra dùng cùng pivot |
| Khung thân (idle) | **16–18 × 20–22 px** nằm trong ô 32×32 | Unity 16×20; tham chiếu ≈18×22 (±2) |
| Hướng | Down, Side (vẽ hướng **trái**, Unity lật `flipX` khi sang phải), Up | `PlayerVisual.UpdateFlip`: "Side sheets face left". Tham chiếu chỉ cho thấy 3 hướng |
| Tên state Animator | `Hero_Idle_*`, `Hero_Walk_*`, `Hero_Attack_*`, `Hero_Hurt_*`, `Hero_Death` | `Hero.controller`, `PlayerVisual` |
| Hit timing | Đòn R gây sát thương ở **0.12 s** sau khi bấm (`slashImpactDelay`) | `PlayerCombat` (R1). Frame "Impact" của Attack phải rơi vào mốc này |
| Filter / nén | Point · Uncompressed · không mipmap | Inventory |

> Nếu Hero mới cần ô lớn hơn 32×32 (ví dụ áo choàng xoè khi đánh), **không** đổi ô của body. Phần vượt ra ngoài nằm ở sprite kiếm riêng (§4) hoặc VFX.

## 2. Hình dáng (từ tham chiếu)

Khung chính: `ref_strips/strip_C05_hero_idle_1s.png` (C05 0:01.0–2.3, cận cảnh trước mặt) và `frame_22_8.0.png` (trong game).

| Phần | Mô tả | Tỉ lệ trên thân ≈20–22 px cao |
|---|---|---|
| Đầu + tóc | Khối tóc trắng lớn, tròn ở đỉnh. Mái ngố che trán, hai mảng tóc nhọn ngang tai chĩa ra hai bên, tóc dài phủ qua vai xuống ngực | Đầu + tóc ≈ **11–12 px** (≈55% chiều cao, kiểu chibi) |
| Mặt | Da rất sáng, mắt xanh 2×2 px mỗi bên, không thấy miệng | Mặt lộ ≈ 6×4 px |
| Thân | Áo khoác/áo choàng xanh teal dài tới gối, cổ áo xám sáng, vạt áo xoè hình chữ A | ≈ **7–8 px** |
| Chân | Ủng tối, chỉ lộ 2–3 px dưới vạt áo | ≈ **2–3 px** |
| Kiếm (nghỉ) | Đeo sau lưng; chuôi cam nhô lên cạnh phải đầu (ở hướng Down) | Chuôi ≈ 2×3 px |

**Silhouette chuẩn theo hướng:**
- **Down:** khối tóc tròn rộng hơn thân, vạt áo chữ A, gần đối xứng (chỉ lệch ở chuôi kiếm).
- **Side (chạy):** thân **ngả về trước khoảng 20–30°**, tóc kéo dài ra sau, kiếm chĩa chéo xuống phía sau, chân bước dài. Nguồn: C17 0:36.45 (`strip_C17_run_right` #0–#4).
- **Up:** vòm tóc trắng trên khối áo choàng gần như đen, kiếm treo dọc sau lưng, ủng lộ khi bước. Nguồn: C17 0:36.62–36.95 (#5–#15).

## 3. Palette

Lấy từ histogram vùng nhân vật ở C05 (bỏ màu cỏ). Màu ghi "đề xuất" là màu suy ra để đủ ramp.

| Vai trò | Tham chiếu | Unity hiện tại | Ghi chú |
|---|---|---|---|
| Tóc sáng | `#FCFCFC` | `#F2F2FA` | |
| Tóc bóng | `#D8D8D8` (xám trung tính) | `#C6C9E4` (ngả tím) | **Bỏ tông tím** |
| Tóc bóng sâu | `#B4B4B8` (đề xuất) | — | |
| Da | `#FCF0D8` | `#F6DCCB` | |
| Mắt | `#6C7CD0` (Unity đang có; tham chiếu thấy xanh) | `#6C7CD0` | Giữ |
| Áo chủ đạo | **`#3C546C`** | `#2C4062` | Teal xanh lam, không tím |
| Áo sáng | **`#60909C`** | `#47678F` | |
| Áo tối / viền áo | **`#303C48`** | `#2A2032` | Viền theo màu, không gần đen |
| Áo choàng sau lưng (Up) | `#1C1A26` | `#1C1A26` | Tham chiếu gần đen đặc ở hướng Up |
| Cổ áo | `#D8D8D8` / `#F0FCFC` | `#C6C9E4` | |
| Ủng | `#3C3C48` | `#252B66` | |
| Chuôi kiếm | `#E09A52` + bóng `#A0602C` (đề xuất) | `#E09A52` | |
| Lưỡi kiếm | `#C8D8E0` sáng / `#7890A0` tối / viền `#3C4C58` (đề xuất từ C25) | Placeholder | |

## 4. Kiếm — attachment riêng (thay `Sword.png` 6×16)

Nguồn: C25 0:02.53–2.70 (`strip_C25_attack` #4–#9) và 0:03.07–3.17 (#20–#23).

| Asset | Kích thước | Mô tả | Pivot |
|---|---|---|---|
| `Hero_Sword_Idle.png` | 8×24 | Kiếm dựng dọc, chuôi cam có chắn tay ngang, lưỡi thép viền sáng | Chuôi = (0.5, 0.17) |
| `Hero_Sword_Swing.png` | 32×32, 3 frame (sheet 96×32) | 3 góc đặt sẵn để không phải xoay sprite (xoay sẽ vỡ pixel): chéo xuống trước (C25 #4–#9), ngang, dựng đứng trên đầu (C25 #20–#23) | Chuôi theo từng frame (ghi trong file xuất) |

**Chiều dài kiếm:** khoảng **28–32 px** (≈1.5–1.8 lần chiều cao thân; đo trên C25 #6: kiếm dài khoảng 100 px video ≈ 30 px art).

Unity:
- Hiện `PlayerVisual` xoay sprite kiếm liên tục, nên pixel sẽ méo.
- Spec yêu cầu đổi sang **chọn frame theo góc** (làm tròn về 3 frame × lật).
- Phần code này thuộc R2B, chưa làm ở R2A.

## 5. Animation

Chi tiết từng clip trong `R2_ANIMATION_SPEC.csv`. Tóm tắt:

| Animation | Tham chiếu | Số frame | Ghi chú |
|---|---|---|---|
| Idle (Down) | C05 0:00.5–5.5, **đo bằng diff** | **4 lần đổi hình trong mỗi vòng lặp 0.60 s**; giữ ≈ 11 / 4 / 3 / (12) khung video | Đo được 2 vòng liên tiếp (1.217→1.817→2.417 s). Đổi hình chủ yếu ở tóc và vạt áo; thân không nhún rõ |
| Idle Side / Up | Không có cảnh đứng yên rõ | NOT MEASURED | Đề xuất dùng cùng nhịp với Idle Down (**thiết kế bổ sung**) |
| Walk/Run Side | C17 0:36.45–36.58 | Thấy ít nhất **2 tư thế chân khác nhau** trong 5 khung video; vòng đầy đủ NOT MEASURED (camera chạy theo) | Đề xuất 4 frame (giữ số hiện tại). Tư thế theo §2 |
| Walk Down | C04 0:08.7–9.6 | NOT MEASURED (camera chạy theo, nhân vật gần như giữ dáng; thay đổi rất nhỏ ở vạt áo) | Đề xuất 4 frame (giữ) |
| Walk Up | C17 0:36.62–36.95 | Thấy ít nhất **3 trạng thái chân** (chân trái, đứng, chân phải) trong 11 khung video | Đề xuất 4 frame |
| Attack (R, mỗi nhát) | C25 0:02.53–2.73 (nhát 1), 0:02.80–3.00 (nhát 2) | Body: **2 tư thế** mỗi nhát (khom lấy đà → khom chém). Kiếm giữ một góc trong khoảng 6 khung video (0.2 s) rồi biến mất. Lưỡi liềm xuất hiện khoảng 2 khung video sau tư thế chém | Giữ cấu trúc R1 (windup + 3 frame, 0.26 s). Vẽ lại tư thế khom, áo xoè |
| Hurt | C22 0:44.7–45.6 | 0 frame mới: **tint hồng–đỏ toàn sprite** | Giữ cơ chế tint của R1 |
| Death | C22 0:45.7–46.1 | **1 tư thế nằm** + VFX hạt đen | Thay 4 frame xoay của R1 bằng 1–2 frame vẽ thật |

## 6. Kiểm tra đạt chuẩn khi nhận asset

1. Mỗi frame đúng 32×32. Chân chạm hàng pixel 2 từ đáy (pivot 0.0625). Khung thân idle nằm trong 16–18 × 20–22.
2. Palette chỉ dùng các màu ở §3 (±2 mỗi kênh). Không có màu tím trong tóc hoặc áo.
3. Side vẽ hướng trái. Lật trong Unity không làm lệch chuôi kiếm.
4. Đặt cạnh `strip_C05_hero_idle_1s.png` và `strip_C17_run_right.png` ở cùng tỉ lệ ×4: silhouette khớp (đầu ≈55% chiều cao, áo chữ A, chạy ngả trước).
5. Trong Unity: đòn R vẫn trúng ở 0.12 s; `Hero_Hurt` / `Hero_Death` vẫn chạy; Animator không báo thiếu state.
6. Tiêu chí R2 của Plan Pack (`README_KHUNG_KE_HOACH.md`):
   - 100% frame Hero hiển thị đủ, opacity 1 khi không fade hoặc hurt.
   - Kiếm đồng bộ với thân.
   - **0 frame Hero mất thân trong bài test 60 s** (120 mẫu, như benchmark cũ).
