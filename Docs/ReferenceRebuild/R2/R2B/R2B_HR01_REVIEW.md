# R2B — HR01 `Hero_Sheet_R2.png` (chờ duyệt, CHƯA import Unity)

## File

| File | Nội dung |
|---|---|
| `Hero_Sheet_R2.png` | **128×288**, 33 ô 32×32, nền trong suốt. Thứ tự hàng/cột **trùng** `Hero_Sheet.png` hiện tại (Idle_Down, Idle_Up, Idle_Side, Walk_Down, Walk_Up, Walk_Side, Attack_Down, Attack_Up, Attack_Side; hàng Attack có 3 ô, ô thứ 4 trống) |
| `HR01_sheet_preview_x5.png` | Toàn bộ sheet phóng ×5 trên nền cỏ R1 |
| `HR01_compare_current_vs_ref.png` | So sánh 3 cột: Hero hiện tại / HR01 / tham chiếu (×6) |
| `tools/HeroSheetR2Gen.cs` | Bộ dựng: tư thế vẽ bằng lưới ký tự, ghép thành 33 frame. Chạy lại được để chỉnh |

## Kiểm tra kỹ thuật (đã đo)

| Tiêu chí (spec §6) | Kết quả |
|---|---|
| Kích thước sheet / ô | 128×288 / 32×32 ✔ |
| Chân chạm hàng 2 từ đáy (pivot 0.0625) | Tất cả frame đáy ở hàng ảnh 29 ✔. Riêng frame "nhún" của Walk ở hàng 28 (cố ý nhấc 1 px) |
| Khung thân idle | 18×22 (Down/Up), 17×22 (Side) ✔ trong 16–18 × 20–22 |
| Palette | **0 pixel ngoài HR06**, dùng 15/16 màu (không dùng `blade_edge`), không có pixel bán trong suốt ✔ |
| Không tím trong tóc/áo | ✔ |
| Viền tóc | `#B4B4B8` (theo đề xuất đã duyệt). Viền tối `#303C48` chỉ ở mặt, áo, ủng ✔ |
| Side vẽ hướng **trái** | ✔ |
| Không vẽ kiếm vào frame body | ✔ Chỉ có chuôi đeo sau lưng (Down/Side) và kiếm treo dọc ở hướng Up |

## Thay đổi so với Hero hiện tại

- **Áo** đổi sang teal, cổ áo sáng, khe áo tối ở giữa (khớp C05).
- **Tóc** dài phủ vai, viền xám sáng thay cho viền đen, bóng xám trung tính.
- **Chuôi kiếm** nhô ra bên phải tóc, ngang tầm tai (C05).
- **Hướng Up:** áo choàng gần đen `#1C1A26`, kiếm treo dọc giữa lưng (C17 0:36.62). Đây là thay đổi lớn nhất về silhouette.
- **Walk Side:** đầu và vai ngả về trước 2 px, tóc kéo ra sau 1–2 px, bước chân dang.
- **Attack (3 frame mỗi hướng):**
  - 0 = khom 1 px, áo bắt đầu xoè.
  - 1 = khom 2 px, áo xoè rộng (Side: xoè 4 px về sau), đầu lao trước 3 px.
  - 2 = hồi.
- **Idle:** 4 frame chỉ đổi tóc và vạt áo, thân không nhún (đúng kết quả đo diff C05).

## Hạn chế còn lại — cần bạn quyết định

Sheet này đạt **palette, tỉ lệ, silhouette Down/Up** và giữ được khung kỹ thuật. Nhưng hai animation P0 vẫn **chưa đạt độ "động"** của tham chiếu:

1. **Walk Side** (C17 0:36.45): tham chiếu ngả thân gần nằm ngang, chân duỗi dài lộ bàn chân màu da. HR01 chỉ ngả đầu và vai 2 px, chân vẫn ngắn dưới vạt áo.
2. **Attack Side** (C25 0:02.53): tham chiếu khom sâu, áo choàng xoè thành vệt ngang dài. HR01 đã xoè áo nhưng thân vẫn đứng.

Lý do: lưới tư thế được ghép từ cùng một khối đầu/thân, nên chỉ dịch và co được, không vẽ lại được dáng ngả hay chân duỗi.

**Đề xuất:** duyệt HR01 làm **bản nền**, đủ để import và test trong Unity. Riêng 4 frame Walk_Side và 3 frame Attack_Side sẽ được vẽ lại riêng thành dáng mới (HR01b), có thể nhờ pixel artist hoặc Canva + pixel cleanup. Giữ đúng khung 32×32, pivot và palette.

## Ghi chú cho bước import (chưa làm)

- Ghi đè pixel vào `Assets/Art/GoblinCampPhase4/Hero_Sheet.png`, **giữ `.meta`**, nên 33 slice `Hero_Sheet_Hero_<State>_<Dir>_<n>` và mọi clip Animator vẫn trỏ đúng.
- Cần sao lưu bản cũ trước khi ghi đè.
- Frame windup (`HeroExtra/Hero_Windup_*`) và Hurt/Death của R1 vẫn dùng palette cũ cho tới khi làm HR02/HR03. Sẽ có lệch màu ngắn trong animation.
- Ảnh tham chiếu Attack trong `HR01_compare_current_vs_ref.png` bị cắt lệch vùng. Dùng `ref_strips/strip_C25_attack.png` (#4–#9) để so.
