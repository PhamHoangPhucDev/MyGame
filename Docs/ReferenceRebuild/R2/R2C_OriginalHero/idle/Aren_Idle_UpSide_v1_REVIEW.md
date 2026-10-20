# Aren — Idle Up + Idle Side (3/4) v1: chờ duyệt

**Ngày:** 2026-10-20 · **Không import Unity** · Idle Down v1 đã duyệt và **không đổi** (md5 `544E5543…` giữ nguyên sau khi tạo lại).
**Tạo lại:** `tools/RenderArenIdle.ps1` (dùng `HeroConceptGen.cs` + `ArenIdleGen.cs`).

## File

| File | Nội dung |
|---|---|
| `Aren_IdleUp_v1.png` | 128×32, 4 frame, hướng sau lưng. **Giao nộp** |
| `Aren_IdleSide_v1.png` | 128×32, 4 frame, 3/4 quay phải. Quay trái dùng flipX trong Unity. **Giao nộp** |
| `Aren_IdleDown_v1.png` | Đã duyệt (không đổi) |
| `Aren_Idle_3views_v1_review.png` | 3 hướng × 4 frame phóng ×8, ô đỏ là px khác F0 |
| `Aren_Idle_turnaround_v1.png` | F0 xoay 4 hướng (Down, phải, Up, trái) ×4, cùng cỡ thật trong Portrait 144×256 và Landscape 480×270 |

## Thiết kế

**Up (sau lưng):**
- Chỏm lông chạy dọc giữa gáy mũ, xuống tới viền mũ.
- Áo choàng navy từ vai trái phủ nửa phải lưng (phía người xem), dài tới gấu, gấu rách.
- Vai phải (bên trái người xem) là giáp bạc phủ vải trắng.
- Thắt lưng bị áo choàng che một phần.

**Side 3/4 (quay phải):**
- Gờ mũ và lưới khe dịch sang phải. Chỏm lông bay ra sau (sang trái).
- Áo choàng nằm phía sau bên trái. Vai gần (bên phải) phủ vải trắng.
- Mũi giày hướng sang phải.

## Chuyển động (giống Idle Down)

| Frame | Up | Side | Thời gian |
|---|---|---|---|
| F0 | Gốc | Gốc | 0.25 s |
| F1 | Đỉnh chỏm lông lệch phải, gấu áo choàng xòe | Chỏm lông bay ra sau hơn, đuôi lông nhấc lên, gấu áo choàng xòe | 0.15 s |
| F2 | Thở: mũ gật 1 px | Thở: mũ gật 1 px | 0.25 s |
| F3 | Đỉnh chỏm lông lệch trái, đuôi lông nhạt | Chỏm lông quay về, đuôi lông rủ xuống | 0.15 s |

Thân 14×21 (F2 là 14×20 do mũ hạ). Đế giày ở hàng 29, pivot (0.5, 0.0625) giữ nguyên.

## Đã sửa trong lúc làm

- **Side:** lúc đầu lưới mũ chỉ có 2 khe nên trông như đôi mắt. Đã thêm thành 4 khe để đọc ra là lưới.
- **Code:** thêm tham số giữ cột vào hàm gật mũ, để Idle Down đã duyệt không bị thay đổi.

## Điểm cần quyết

- **Side flipX:** khi quay trái, áo choàng chuyển sang bên phải và vải trắng sang bên trái. Đây là cách thông thường của sprite top-down. Nếu muốn áo choàng luôn ở vai trái thật, cần vẽ riêng hướng trái (thêm 4 frame).
