# HeroIdle_Sample_v3: chờ duyệt

**Ngày:** 2026-10-20 · **Không import Unity** · Tạo bằng `tools/HeroIdleSampleGen.cs`, `Run(outDir, 3)`. v1 và v2 vẫn tạo lại được y hệt (đã kiểm tra md5).

## File

| File | Nội dung |
|---|---|
| `HeroIdle_Sample_v3.png` | 128×32: 4 ô 32×32, nền trong suốt. **File giao nộp** |
| `HeroIdle_Sample_v3_review_x8.png` | Hàng 1: v3 phóng ×8, ô đỏ đánh dấu px khác F0. Hàng 2: v2 để so sánh. Hàng 3: v3 ở ×1 và ×4 |
| `HeroIdle_Sample_v3_ingame_9x16_16x9.png` | 4 frame v3 trên nền cỏ, khung Portrait 144×256 và Landscape 480×270 |

## Thay đổi so với v2

Giữ vòm tóc trắng như v2: viền xám chỉ ở hai bên và nửa dưới. Đổi kiểu đung đưa từ nhấc đối xứng sang **lắc qua lại**:

| Frame | Thay đổi | Số px khác F0 |
|---|---|---|
| F0 | Tư thế gốc | 0 |
| F1 | **Đuôi tóc trái nhấc 2 px** | 4 |
| F2 | Hai đuôi tóc nhấc 1 px (tóc đi qua giữa) | 2 |
| F3 | **Đuôi tóc phải nhấc 2 px** | 4 |

- Bỏ vạt áo xoè ở F2 của v2, để chuyển động chỉ nằm ở tóc và đọc rõ là lắc ngang.
- F2 là frame chuyển tiếp do tôi chọn (anh/chị chỉ quy định F1 và F3). Có thể đổi F2 về y hệt F0 nếu muốn nhịp trái–nghỉ–phải.

## Thời gian đề xuất

**Mỗi frame 0.15 s, vòng lặp 0.60 s.** Lắc qua lại cần nhịp đều; chia không đều như v1 (11/4/2/1) sẽ làm bên phải gần như không hiện.

## Không đổi

- Thân 14×21 ở cả 4 frame (cột 9–22, hàng 9–29), đế giày hàng 29.
- 0 px ngoài HR06.
- Mặt, mắt và thân đứng yên.
- Không vẽ kiếm; back_mount đề xuất (23, 13).

## Lưu ý khi duyệt

- **Kiếm che đuôi tóc phải:** sau này `Sword_Back` có chuôi ở vai phải, mũi lộ ở dưới bên trái. Đuôi tóc phải (F3) có thể bị chuôi che một phần. Sẽ kiểm tra khi làm Sword_Back.
- **Nhịp lặp:** từ F3 (bên phải nhấc) quay về F0 (nghỉ) là một bước 2 px, lớn hơn các bước khác (1–2 px). Ở ×4 trông như tóc rơi xuống, không giật.
