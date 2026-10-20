# R2C — Nhân vật Hero mới (thiết kế gốc): chọn concept

**Ngày:** 2026-10-20 · **Không import Unity** · Tạo bằng `tools/HeroConceptGen.cs`

## Vì sao làm lại

Hero R2A (v1–v3) bám rất sát nhân vật trong video tham chiếu: tóc trắng dài ôm mặt, áo khoác xanh thép, kiếm sau lưng, palette HR06 lấy màu trực tiếp từ video. Phong cách vẽ (pixel art, top-down, chibi) không phải là thứ bị bảo hộ, nhưng **thiết kế nhân vật cụ thể thì có thể bị**. Vì vậy cần một nhân vật có nhận diện riêng.

**Giữ nguyên** (thông số kỹ thuật và gameplay, không phải nhận diện):
- Ô 32×32, đế giày hàng 29, pivot (0.5, 0.0625), PPU 16.
- Cao 21 px, đầu ≈ 50–55%.
- Tên slice và cấu trúc 3 hướng + lật.
- Kiếm là sprite riêng.

**Thay mới:** tóc, trang phục, palette, silhouette, kiểu kiếm (làm sau khi chọn concept).

**Không dùng nữa:** `HeroIdle_Sample_v1–v3`, palette HR06 (vì lấy màu từ nhân vật tham chiếu). Các file cũ vẫn giữ trong `R2A_HeroMatch/sample/` để đối chiếu kích thước.

## Ba concept (Idle Down, frame 0)

Ảnh: `concepts/R2C_concept_board.png`. PNG từng concept: `concepts/HeroConcept_*_IdleDown_F0.png`.

| | A — Kael | B — Mira | C — Rook |
|---|---|---|---|
| Vai | Lãng khách | Thợ săn / nhà thám hiểm | Hiệp sĩ tập sự |
| Tóc | Nâu rối, mái lởm chởm | Đồng cam, bím tóc vắt vai phải | Đen, có một lọn trắng chéo |
| Điểm nhận diện | Khăn quàng đỏ, đuôi khăn bên phải | Kính bảo hộ trên trán, áo choàng màu mận | Giáp vai thép nhô ra hai bên, áo choàng đỏ thẫm có huy hiệu vàng |
| Mắt | Nâu đen | Xanh lục | Hổ phách |
| Silhouette | 12×21, hình chữ nhật, ấm | 12×21, có điểm sáng ở trán (kính) | **14×21**, hình chữ T nhờ giáp vai |
| Số màu | 13 | 16 | 14 |
| Đọc trên nền cỏ ×1 | Tốt (khăn đỏ) | Tốt nhất (tóc cam) | Tốt (đỏ và đen tương phản) |
| Gợi ý kiếm | Kiếm ngắn cán gỗ | Dao găm đôi hoặc kiếm cong | Kiếm dài thẳng, chuôi vàng |

Cả ba đều khác hẳn nhân vật tham chiếu về màu tóc, kiểu tóc, trang phục và palette. Tôi cũng tránh các tổ hợp quá quen của nhân vật nổi tiếng, ví dụ áo xanh lá + mũ chóp.

## Sau khi chọn

1. Vẽ đủ **Idle Down 4 frame** cho concept được chọn, dùng kiểu lắc tóc / khăn / bím qua lại như v3. Dừng để duyệt.
2. Khoá palette mới thay cho HR06 (thay thế HR06 trong brief).
3. Cập nhật `R2_CANVA_HERO_BRIEF.md`:
   - Giữ các thông số kỹ thuật.
   - Thay mục nhận diện bằng concept mới.
   - Bỏ yêu cầu "giống tham chiếu" về tóc và áo.
   - Chỉ giữ tham chiếu cho **chuyển động, tỉ lệ, góc camera và nhịp hoạt ảnh**.
4. Thiết kế kiếm riêng cho nhân vật.

## Lưu ý về phạm vi bản quyền

Bước này chỉ xử lý Hero. Goblin, Gobrider, tile và VFX trong spec R2 cũng được phân tích để khớp video tham chiếu. Nếu chúng sao chép sát thiết kế gốc, cũng nên làm lại theo cách tương tự. Bản quyền với game thương mại là vấn đề pháp lý; ghi chú này không thay cho tư vấn luật.

## v2: làm lại cho đẹp hơn (phản hồi: "xấu")

Ảnh: `concepts/R2C_concept_board_v2.png` (v2 ở trên, v1 ở dưới để so sánh). PNG: `concepts/HeroConcept_*_IdleDown_F0_v2.png`.

| Lỗi của v1 | Sửa trong v2 |
|---|---|
| Thân rộng bằng đầu, trông như cục gạch | Đầu 14 px, thân 10 px → dáng chibi |
| Mỗi vùng chỉ một màu phẳng | Tóc 5 sắc độ (viền, tối, giữa, sáng, bóng). Áo 3 sắc độ, ánh sáng từ trên trái |
| Viền gần đen quanh mọi thứ | Viền theo màu từng vùng: tóc nâu thì viền nâu đậm, tóc cam thì viền cam đậm |
| Mắt 1 px, mặt vô hồn | Mắt 2×2 có điểm sáng trắng, má hồng (A, B), hàm có đổ bóng |
| Tóc là khối vuông | Vòm tóc tròn, mái có răng, tóc hai bên ôm mặt |

Kích thước vẫn đúng: cả ba đều 14×21, đế giày hàng 29.

**Giới hạn nói thật:** vẽ pixel bằng lưới ký tự ở cỡ 14×21 thì chỉ đẹp được tới một mức nhất định. Nếu cần chất lượng như game thương mại, nên để họa sĩ pixel (Aseprite) tinh chỉnh concept được chọn. File PNG và lưới ở đây dùng làm bản phác đúng kích thước.
