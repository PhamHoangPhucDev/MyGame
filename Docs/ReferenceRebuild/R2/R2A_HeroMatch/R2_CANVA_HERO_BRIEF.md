# R2A — Canva Hero Brief

**Ngày:** 2026-10-20 · **Quy tắc:** chỉ làm **mẫu Idle Down** trước. Không vẽ cả spritesheet trước khi mẫu Idle được duyệt.

Đọc kèm: `R2_HERO_VISUAL_AUDIT.md`, `R2_HERO_ANIMATION_SPEC.csv`, `COMPARE_hero_phase4_r2b_reference.png`, `ref_timecoded/`.

---

## 1. Phong cách (bắt buộc)

- **Pixel art thật**, theo đúng video gốc:
  - Lưới đều, mỗi px art là một ô vuông đồng màu.
  - Không khử răng cưa, không gradient, không dither, không blur, không ánh sáng mềm.
- **Không** dùng concept anime vẽ tay, **không** dùng chibi 3D, **không** vẽ ở độ phân giải cao rồi thu nhỏ.
- Camera top-down chếch 3/4. Nhân vật đứng thẳng, không phối cảnh.
- Nếu Canva tạo ra ảnh không phải pixel art, chỉ dùng ảnh đó làm phác thảo dáng. **File giao nộp phải là PNG pixel thật ở kích thước gốc**, và Claude sẽ kiểm tra lưới trước khi nhận.

## 2. Thông số đã đo (không được tự đổi)

| Thông số | Giá trị | Nguồn |
|---|---|---|
| Ô sprite | 32×32 px, nền trong suốt | cấu trúc sheet hiện tại |
| Pivot | (0.5, 0.0625) = đế giày ở hàng 29, hai hàng dưới cùng trống | Phase 4 |
| Thân (đỉnh tóc → đế giày) | **21 px** | ĐO, C12 + C03 |
| Chiều rộng tóc, mặt trước | **14 px** (±1) | ĐO, C03 |
| Đầu + tóc | ≈ 55% chiều cao (≈ 11–12 hàng) | tỉ lệ C06 |
| Hàng mắt | ≈ hàng 7 tính từ đỉnh tóc | tỉ lệ C06 |
| Áo khoác | ≈ hàng 10–20, hình thang xoè, có khe giữa | tỉ lệ C06 |
| Chân + giày | chân sáng 2 px giữa hai vạt áo, giày tối 2 hàng cuối | tỉ lệ C06 |
| Palette | HR06 (`Docs/ReferenceRebuild/R2/R2B/R2B_HR06_PALETTE.md`), viền tóc `#B4B4B8` | đã duyệt |
| Kiếm | **không vẽ vào thân**. Kiếm là sprite riêng (mục 4) | Mô hình A |

**CHƯA ĐO** (người vẽ đề xuất, Claude đo lại trước khi chốt): kích thước mắt (1×2 hay 2×2), độ rộng lưỡi kiếm, chiều rộng chân.

## 3. Bố cục 9:16 và 16:9

- Sprite giống nhau cho cả hai profile (PPU 16; Portrait PPC 144×256, Landscape PPC 480×270). **Không** làm bản riêng cho từng tỉ lệ.
- Ở Portrait, Hero chiếm ≈ 8–9% chiều cao màn hình. Vì vậy silhouette phải đọc được ở kích thước 21 px **với zoom ×4**: tóc trắng tương phản với cỏ `#74A587`, áo tối, chân sáng.
- Khi duyệt, đặt mẫu lên nền cỏ ở cả hai khung 144×256 và 480×270 (Claude làm bước này).

## 4. Kiếm (chỉ làm sau khi Idle được duyệt)

- Ô kiếm riêng 48×48. Dài ≈ 35–38 px tính cả chuôi (ĐO ±3). Chuôi cam `#E09A52`, lưỡi xám xanh có highlight.
- Bốn sprite:
  1. `Sword_Back`: trên lưng, chuôi nhô qua vai phải, mũi lộ dưới vạt áo trái.
  2. `Sword_Diag`: chéo xuống phía trước.
  3. `Sword_Horiz`: ngang.
  4. `Sword_Vert`: dựng đứng.
- Không xoay sprite trong Unity. Mỗi frame thân xuất kèm toạ độ `grip` (tay cầm) và `back_mount` (điểm gắn trên lưng), tính bằng px trong ô 32×32.

## 5. Danh sách ưu tiên

### P0 — chặn việc khớp tham chiếu
1. **Hero_Idle_Down_0..3**: mẫu đầu tiên, thân 14×21 (mục 6).
2. **Sword_Back** + toạ độ back_mount cho Idle Down. Thay cho "cái đuôi" tối trong Sandbox.
3. **Hero_Walk_Side_0..3**: đổi từ profile nghiêng sang 3/4 mặt trước có kiếm sau lưng.
4. **Hero_Attack_Side / Up / Down 0..2**: giữ dáng ngồi thấp của R2b, thân 14×21, không vẽ kiếm, có toạ độ grip.
5. **Sword_Diag / Horiz / Vert**.
6. (Code, bước sau, cần duyệt riêng) Bỏ dòng ẩn kiếm khi đang Attack trong `PlayerVisual`; thêm Weapon Anchor theo frame; bỏ xoay kiếm lúc chạy.

### P1 — hoàn thiện
7. Hero_Idle_Up, Hero_Idle_Side (3/4), Hero_Walk_Down, Hero_Walk_Up.
8. Hero_Death: 2 frame (va chạm + nằm).
9. Trăng lưỡi liềm bắt đầu ở mép lưỡi kiếm, không tách khỏi Hero (chỉnh `SlashPullBack`, bước code sau).

### P2 — chỉ khi có cơ chế tương ứng
10. Sprint profile kéo kiếm (có thể dùng lại art Walk_Side của R2b làm nền).
11. Roll có viền trắng.
12. Hurt: giữ cách tô màu của R1.
13. Skill: thân dùng lại frame Attack; VFX làm theo brief riêng.

## 6. Mẫu cần tạo đầu tiên: **Hero_Idle_Down (4 frame)**

| Mục | Yêu cầu |
|---|---|
| File | `HeroIdle_Sample_v1.png`: dải 4 ô 32×32 ngang (128×32), nền trong suốt. Kèm bản phóng ×8 để xem |
| Thân | 21 px cao, 14 px rộng (tóc ở tầm mắt). Đỉnh tóc ở hàng 9, đế giày ở hàng 29 (tính từ 0 ở trên cùng); hàng 30–31 trống (pivot 0.0625). Phase 4 và R2b cũng đặt đế giày ở hàng 29 (đã kiểm tra) |
| Hướng | Mặt trước (Down) |
| Kiếm | Không vẽ. Đánh dấu điểm `back_mount` bằng một ghi chú riêng (toạ độ x, y), không vẽ vào PNG |
| Chuyển động | Frame 0 là tư thế gốc. Frame 1–3: tóc hai bên và vạt áo lệch 1 px, luân phiên. Thân **không nhún**, mắt và mặt giữ nguyên vị trí |
| Thời gian (để Claude dựng preview) | Vòng lặp 0.60 s; giữ ≈ 0.367 / 0.133 / 0.100 / 0.000+ s (đo ở C05) |
| Palette | Chỉ màu trong HR06 |
| Tham chiếu chính | `ref_timecoded/frame_C03_0m02.1_idle_front.png`, `frame_C06_0m01.3_idle_zoom.png`, `strip_R03_idle_0m02.png`, `strip_R06_idle_zoom_0m01.png` |
| Giữ từ Phase 4 | Bố cục mặt, khoảng cách hai mắt, mái tóc trước trán |
| Tiêu chí duyệt | (1) Lưới pixel sạch. (2) Đúng 14×21. (3) Silhouette "nấm trắng trên áo hình thang", đứng cạnh tham chiếu ở cùng zoom phải trông cùng họ. (4) Đọc được trên nền cỏ ở Portrait 144×256 và Landscape 480×270. (5) Không lẫn màu ngoài HR06 |

**Dừng lại sau mẫu này để duyệt.** Chỉ khi mẫu Idle Down được duyệt mới làm tiếp Sword_Back và các mục P0 còn lại.
