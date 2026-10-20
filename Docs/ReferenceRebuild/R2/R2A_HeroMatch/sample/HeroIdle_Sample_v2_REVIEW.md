# HeroIdle_Sample_v2: chờ duyệt

**Ngày:** 2026-10-20 · **Không import Unity** · Tạo bằng `tools/HeroIdleSampleGen.cs`, `Run(outDir, 2)`. v1 vẫn tạo lại được y hệt (đã kiểm tra md5).

## File

| File | Nội dung |
|---|---|
| `HeroIdle_Sample_v2.png` | 128×32: 4 ô 32×32, nền trong suốt. **File giao nộp** |
| `HeroIdle_Sample_v2_review_x8.png` | Hàng 1: v2 phóng ×8, ô đỏ đánh dấu các px khác F0. Hàng 2: v1 để so sánh. Hàng 3: v2 cạnh C06 0:01.3 cùng tỉ lệ, kèm kích thước thật ×1 và ×4 |
| `HeroIdle_Sample_v2_ingame_9x16_16x9.png` | 4 frame v2 trên nền cỏ, khung Portrait 144×256 và Landscape 480×270 |

## Thay đổi so với v1

1. **Viền tóc:**
   - Bỏ `#B4B4B8` ở vòm tóc (hàng 0–2). Mép trên giờ là `#FCFCFC`, giống tham chiếu.
   - Giữ viền xám ở hai bên tóc (hàng 3–12) và ở đuôi tóc.
2. **Tóc đung đưa rõ hơn:** cả hai đuôi tóc cùng nhấc lên. F1 và F3 nhấc 1 px, F2 nhấc 2 px kèm vạt áo xoè 1 px.

   | Frame | Thay đổi | Số px khác F0 |
   |---|---|---|
   | F0 | Tư thế gốc | 0 |
   | F1 | 2 đuôi tóc nhấc 1 px | 2 |
   | F2 | 2 đuôi tóc nhấc 2 px, vạt áo xoè | 10 |
   | F3 | Như F1 (đang hạ xuống) | 2 |

   v1 chỉ đổi 1–4 px mỗi frame và chỉ lệch một bên.
3. **Thời gian đề xuất:**
   - Giữ tổng vòng lặp 0.60 s như C05, nhưng chia đều hơn để F2 không bị nuốt mất: **F0 0.20 s, F1 0.133 s, F2 0.133 s, F3 0.133 s** (6/4/4/4 khung video).
   - v1 chia 11/4/2/1, khi đó F2 chỉ hiện 0.067 s.

## Không đổi

- Thân 14×21 ở cả 4 frame (cột 9–22, hàng 9–29), đế giày hàng 29.
- 0 px ngoài HR06.
- Mặt, mắt và thân đứng yên.
- Không vẽ kiếm; back_mount đề xuất (23, 13).

## Điểm cần anh/chị quyết

- **Kiểu đung đưa:** v2 là nhấc tóc đối xứng (nhịp thở). Nếu muốn tóc lắc qua lại trái–phải: F1 nhấc bên trái 2 px, F3 nhấc bên phải 2 px. Mỗi frame vẫn đổi khoảng 4 px.
- Ở ×1 (kích thước thật) chuyển động vẫn nhỏ, đúng tinh thần tham chiếu vì thân không nhún. Ở ×4 (Portrait trên máy) đã nhìn thấy được.
