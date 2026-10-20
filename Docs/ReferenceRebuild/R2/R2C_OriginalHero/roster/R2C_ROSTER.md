# R2C — Đội hình 5 nhân vật (Idle Down, frame 0)

**Ngày:** 2026-10-20 · **Không import Unity** · Tạo bằng `tools/HeroConceptGen.cs`, `Run(outDir, 3)` (mảng `Roster`)

Ảnh tổng: `R2C_roster_board.png`. PNG từng nhân vật: `HeroConcept_<số>_<tên>_IdleDown_F0_roster.png` (32×32, nền trong suốt).

## Khuôn mẫu chung (giữ như Hero hiện tại)

- Ô 32×32, thân 14×21 (cột 9–22, hàng 9–29), đế giày ở hàng 29, pivot (0.5, 0.0625), PPU 16. Cả 5 nhân vật đã kiểm tra bằng code.
- Kỹ thuật vẽ như concept v2:
  - Ánh sáng từ trên trái.
  - Tóc 5 sắc độ.
  - Viền theo màu từng vùng (không dùng viền đen).
  - Mắt 2×2 có điểm sáng.
  - Đầu to, thân nhỏ.
- Vũ khí là **sprite riêng**, không vẽ vào thân (rìu hoặc búa, cung, gậy phép, kiếm, dao găm sẽ làm sau).

## Đội hình

| # | Tên | Tộc – lớp | Điểm nhận diện | Silhouette | Vũ khí đề xuất |
|---|---|---|---|---|---|
| 1 | **Brom** | Người lùn – Tank | Mũ sắt có sừng, râu đỏ cam rậm phủ ngực, giáp vai thép, giày rộng | Rộng và nặng nhất, hình chữ nhật | Búa hoặc rìu + khiên tròn |
| 2 | **Lyra** (v3) | Elf – Cung thủ (kiểm lâm) | Tóc vàng tết bím vắt vai phải, kẹp lá xanh trên tóc, tai nhọn. Áo kem không tay, tay áo choàng ngắn xanh lá. Áo nịt da nâu. Váy xanh lá xẻ nhọn khoác ngoài váy lót kem. Ống tên xanh có lông tên trắng sau vai trái. Ủng nâu | Bím tóc một bên, ống tên một bên, váy xẻ nhọn | Cung gỗ (sprite riêng) |
| 3 | **Selene** | Phù thủy – Pháp sư | Mũ nhọn tím có ngôi sao, tóc bạc tím, mắt hồng, áo choàng xanh đêm viền vàng | Cao nhất nhờ chóp mũ, thân hình chuông | Gậy phép có pha lê |
| 4 | **Aren** (v3) | Nhân tộc – Kiếm sĩ | Hiệp sĩ giáp toàn thân. Mũ kín có gờ giữa và lưới khe dọc, chỏm lông xanh navy buông sau mũ. Áo choàng navy vắt vai trái, nếp dọc, gấu rách. Vai phải giáp bạc phủ vải trắng rách. Thắt lưng da nâu có khóa bạc và túi | Mũ kín và chỏm lông là điểm nhận diện; áo choàng một bên giữ dáng lệch | Trường kiếm (chống trước người khi Idle) |
| 5 | **Vex** (v5) | Ma tộc – Sát thủ | Tóc bạc tro ánh tím, rối và dựng. 2 sừng nhỏ trên đỉnh. **Dải bịt mắt đỏ thẫm**, đuôi buông bên phải, lọn tóc vắt qua. Da nhợt, môi hồng. Cổ áo cao màu đen ôm tới cằm. Áo choàng xám nhạt mở trước, khoác ngoài bộ đồ tối, khóa thắt lưng bạc | Đầu tóc bạc sáng, thân tối, hai vạt áo choàng xám hai bên | Trường kiếm hoặc dao găm (chưa chốt) |

Ở kích thước thật (×1), mỗi nhân vật vẫn phân biệt được nhờ màu chủ đạo: cam (Brom), vàng, nâu và xanh lá nhạt (Lyra), tím (Selene), bạc và navy (Aren), tóc bạc và dải bịt mắt đỏ (Vex).

## Ghi chú

- **ĐỘI HÌNH ĐÃ CHỐT (2026-10-20): Brom, Lyra v3, Selene, Aren v3, Vex v5.** Hero chính: **Aren**. **Lyra v3** vẽ lại theo ảnh mood kiểm lâm (ảnh có watermark DeviantArt nên chỉ lấy nét chung, không chép thiết kế). Bản cũ: `HeroConcept_2_Lyra_*_roster_v1.png`, `_v2.png`; so sánh: `R2C_redraw_lyra.png`. **Aren v3** vẽ lại theo ảnh mood hiệp sĩ giáp toàn thân (chỉ lấy nét chung của kiểu hiệp sĩ, không chép tranh). Bản cũ: `HeroConcept_4_Aren_*_roster_v1.png`, `_v2.png`; so sánh: `R2C_redraw_aren.png`. **Vex v5** làm theo ảnh mood thứ hai (tóc trắng rối, bịt mắt đen, cổ cao). Tổ hợp tóc trắng dựng + bịt mắt đen + cổ cao gần như là dấu hiệu nhận diện của **Gojo Satoru** (Jujutsu Kaisen). Vì vậy v5 cố ý đổi: tóc bạc tro ánh tím, thêm 2 sừng nhỏ, **bịt mắt màu đỏ thẫm**, thêm áo choàng xám. Mục đích là giữ không khí của ảnh mà không trùng nhân vật có bản quyền. So sánh v1 → v5: `R2C_redraw_vex.png`. Bản cũ: `HeroConcept_5_Vex_*_roster_v1..v4.png`.
- Cả 5 đều là thiết kế gốc. Chỉ dùng các kiểu nhân vật phổ biến (người lùn râu đỏ, elf tai nhọn, phù thủy mũ nhọn), không lấy theo nhân vật cụ thể của game nào.
- Game hiện có **một** Hero (`PlayerVisual`, `Hero.controller`). Muốn có nhiều nhân vật chơi được thì cần hệ thống chọn nhân vật và một sheet riêng cho mỗi nhân vật. Đây là bước thiết kế và code riêng, chưa làm.

## Bước tiếp theo đề xuất

1. Chọn nhân vật làm Hero chính (hoặc xác nhận làm cả 5).
2. Vẽ Idle Down 4 frame cho nhân vật đó (lắc qua lại như v3: râu, tóc, mũ, áo choàng hoặc khăn). Dừng để duyệt.
3. Khoá palette riêng cho từng nhân vật.
- Tạo lại toàn bộ ảnh: chạy `tools/RenderRoster.ps1`.
