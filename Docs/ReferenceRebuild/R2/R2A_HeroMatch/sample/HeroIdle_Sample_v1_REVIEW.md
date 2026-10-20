# HeroIdle_Sample_v1: chờ duyệt

**Ngày:** 2026-10-20 · **Không import Unity** · Tạo bằng `tools/HeroIdleSampleGen.cs` (chạy lại sẽ ra đúng file này)

## File

| File | Nội dung |
|---|---|
| `HeroIdle_Sample_v1.png` | 128×32: 4 ô 32×32, nền trong suốt. **Đây là file giao nộp** |
| `HeroIdle_Sample_v1_review_x8.png` | Hàng 1: 4 frame phóng ×8. Hàng 2: Phase 4, R2b, v1 và tham chiếu C06 0:01.3, C03 0:02.1, tất cả cùng tỉ lệ px art |
| `HeroIdle_Sample_v1_ingame_9x16_16x9.png` | Đặt Phase 4, v1, R2b lên nền cỏ trong khung Portrait 144×256 và Landscape 480×270 (hiển thị ×2) |

Bóng elip trong ảnh xem trước chỉ để minh hoạ, **không** nằm trong PNG (Unity dùng sprite Shadow riêng).

## Tự kiểm tra (bằng code)

| Tiêu chí | Kết quả |
|---|---|
| Kích thước thân | ✔ 14×21 ở cả 4 frame (cột 9–22, hàng 9–29) |
| Đế giày | ✔ hàng 29, giống Phase 4 và R2b (pivot 0.5, 0.0625 giữ nguyên) |
| Màu ngoài HR06 | ✔ 0 px. Dùng 11 trên 16 màu (không dùng nhóm kiếm) |
| Lưới pixel | ✔ vẽ trực tiếp từng px, không khử răng cưa |
| Kiếm | Không vẽ, theo spec. **back_mount đề xuất: (23, 13)** trong ô 32×32 (chuôi nhô ở mép phải tóc, ngang hàng 4 của thân, đo theo C06) |

## Bố cục (hàng tính từ đỉnh tóc)

- **0–2:** vòm tóc, rộng 6 → 10 → 12 px.
- **3–12:** tóc đủ 14 px, có mảng `#D8D8D8` và viền ngoài `#B4B4B8`.
- **6:** mái tóc, có da lộ ra.
- **7:** hàng mi `#1C1A26`, 2 px mỗi mắt (34% chiều cao).
- **8–9:** mắt 2×2 `#6C7CD0`. Mặt rộng 4 px, cách mắt mỗi bên.
- **10–11:** cằm, vai áo, hai đầu cổ áo `#F0FCFC`, khe áo `#1C1A26` ở giữa.
- **12–18:** áo hình thang. Bên trái sáng `#60909C`, viền `#303C48`. Hai vạt áo mở ra ở hàng 16–18 để lộ chân sáng.
- **19–20:** giày `#3C3C48`.

## Chuyển động

Thân, mặt và mắt đứng yên ở cả 4 frame. Chỉ thay đổi các chi tiết sau:

| Frame | Thay đổi | Giữ đề xuất |
|---|---|---|
| F0 | Tư thế gốc | 11 khung video ≈ 0.367 s |
| F1 | Đuôi tóc trái nhấc lên 1 px | 4 khung video ≈ 0.133 s |
| F2 | Vạt áo xoè thêm 1 px hai bên | 2 khung video ≈ 0.067 s |
| F3 | Đuôi tóc phải nhấc lên 1 px | 1 khung video ≈ 0.033 s |

Tổng 0.60 s, đúng vòng lặp đo ở C05. Theo C05, khung giữ thứ tư rất ngắn, nên tôi chia 18 khung video thành 11/4/2/1. Cách chia này là suy luận, có thể đổi khi dựng preview động.

## Khác biệt còn lại so với tham chiếu (để quyết khi duyệt)

1. **Viền tóc `#B4B4B8`** (đã duyệt ở HR06) làm vòm tóc có mép xám. Trên tham chiếu, mép tóc gần như trắng tinh trên nền cỏ. Nếu muốn giống hơn: chỉ giữ viền xám ở nửa dưới và hai bên tóc, bỏ viền ở vòm trên.
2. **Chuyển động rất nhẹ** (đúng tham chiếu, nơi thân không nhún). Nhưng F2 khó thấy ở kích thước thật. Có thể tăng biên độ: tóc lệch 1 px ở cả hai bên cùng lúc.
3. **Áo** trên tham chiếu sáng và xanh hơn một chút, có highlight trắng ở mép. Palette HR06 không có màu đó, nên tôi giữ HR06.
4. **Chân:** tham chiếu chỉ thấy một cột sáng 1–2 px. Bản v1 dùng 2 px, giày 2–4 px.

## Sửa trong tài liệu R2A

Khi đo lại chân trên ảnh zoom C06, tôi thấy tham chiếu có **chân sáng và giày tối**, không phải "giày sáng". Đã sửa trong `R2_HERO_VISUAL_AUDIT.md`, `R2_CANVA_HERO_BRIEF.md` và `R2_HERO_ANIMATION_SPEC.csv`. Màu giày `#3C3C48` của HR06 vì vậy là đúng.
