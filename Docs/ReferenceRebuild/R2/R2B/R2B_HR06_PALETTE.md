# R2B — HR06 Hero Palette Swatch (chờ duyệt)

**Quyết định áp dụng:** PPC portrait **144×256** theo Plan Pack (đã cập nhật trong `R2_CHARACTER_DIFFERENCE_MATRIX.md`). R2A đã được duyệt.

## File

| File | Nội dung |
|---|---|
| `Hero_Palette_Swatch.png` | 64×16 px, 16 ô 8×8, đúng đặc tả HR06. Thứ tự ô theo bảng dưới (hàng 1 = ô 1–8, hàng 2 = ô 9–16). **Chỉ dùng cho tài liệu, không import Unity** |
| `HR06_palette_review.png` | Bảng duyệt có tên và mã màu |
| `HR06_palette_on_current_hero.png` | **Ảnh xem trước:** palette mới áp tạm lên 15 frame của `Hero_Sheet` hiện tại, đặt cạnh tham chiếu C05 0:00.8. Không ghi gì vào `Assets/` |

## Palette khoá (16 màu)

| # | Vai trò | Hex | Nguồn |
|---|---|---|---|
| 1 | hair_light | `#FCFCFC` | Histogram C05 |
| 2 | hair_shade | `#D8D8D8` | Histogram C05 |
| 3 | hair_deep | `#B4B4B8` | Đề xuất |
| 4 | skin | `#FCF0D8` | Histogram C05 |
| 5 | eye | `#6C7CD0` | Unity hiện có, khớp tham chiếu |
| 6 | coat | `#3C546C` | Histogram C05 |
| 7 | coat_light | `#60909C` | Histogram C05 |
| 8 | coat_dark (viền áo) | `#303C48` | Histogram C05 |
| 9 | cloak_back | `#1C1A26` | Unity hiện có, khớp C17 (hướng Up) |
| 10 | collar_hi | `#F0FCFC` | Histogram C05 |
| 11 | boots | `#3C3C48` | Histogram C05 |
| 12 | hilt | `#E09A52` | Unity hiện có |
| 13 | hilt_shade | `#A0602C` | Đề xuất |
| 14 | blade_light | `#C8D8E0` | Đề xuất từ C25 |
| 15 | blade_mid | `#7890A0` | Đề xuất từ C25 |
| 16 | blade_edge | `#3C4C58` | Đề xuất từ C25 |

## Bảng đổi màu dùng cho ảnh xem trước

18 màu của `Hero_Sheet` hiện tại đều quy về 16 màu trên (không còn màu nào bị sót):

| Hiện tại | → | Mới | Ghi chú |
|---|---|---|---|
| `#F2F2FA` `#FFFFFF` | → | `#FCFCFC` | |
| `#C6C9E4` | → | `#D8D8D8` | Bỏ tông tím |
| `#9FB4CC` | → | `#B4B4B8` | |
| `#F6DCCB` `#F2A8B0` | → | `#FCF0D8` | Má hồng `#F2A8B0` bị gộp vào da. Tham chiếu không thấy má hồng |
| `#2C4062` | → | `#3C546C` | |
| `#47678F` `#7BA2CC` | → | `#60909C` | |
| `#2A2032` | → | `#303C48` | |
| `#E4EDF6` | → | `#F0FCFC` | |
| `#252B66` | → | `#3C3C48` | |
| `#F0C040` | → | `#E09A52` | |
| `#A2582A` | → | `#A0602C` | |
| `#8FE8FF` | → | `#C8D8E0` | Ánh xanh trên kiếm ở frame Attack |
| `#1C1A26` `#6C7CD0` `#E09A52` | = | giữ | |

## Nhận xét từ ảnh xem trước

1. **Áo:** sau khi đổi sang teal, màu áo gần tham chiếu hơn rõ rệt (hết sắc tím). ✔
2. **Tóc:** bóng xám trung tính làm khối tóc **phẳng hơn** bản cũ. Tham chiếu vẫn có khối nhờ các đốm xanh xám nhạt và viền tóc sáng.
   - **Đề xuất cho HR01** (không phải đổi palette): viền ngoài tóc dùng `#B4B4B8` thay cho `#303C48`. Dùng `#D8D8D8` thành từng mảng lọn tóc thay vì chấm rải.
   - Viền tối `#303C48` chỉ giữ ở mặt, áo và ủng. Điều này khớp C05, nơi viền tóc không tối.
3. **Hướng Up:** chỉ đổi palette là chưa đủ. Áo choàng sau lưng ở tham chiếu gần đen đặc (`#1C1A26`), còn sprite hiện tại vẫn là áo teal có sọc kiếm chéo. Cần vẽ lại (H08) đúng như spec.
4. **Kiếm trong frame Attack:** ánh xanh được đổi thành thép. Kiếm sẽ được tách sang HR05 (attachment), nên frame body mới **không vẽ kiếm**.

## Sau khi duyệt

Thứ tự sản xuất tiếp theo (theo manifest):
1. **HR01** `Hero_Sheet_R2.png`, giữ đúng 33 ô và tên slice.
2. **HR05** `Hero_Sword_Swing.png`.
3. **HR02** windup.
4. **HR04** kiếm nghỉ.
5. **HR03** death.

Mỗi asset dừng để duyệt trước khi import vào Unity.
