# Ải 3 · Bãi lửa trại: art UI riêng (bàn giao cho Cursor)

Spec: `Docs/superpowers/specs/2026-11-06-goblin-camp-stages-design.md` mục C, phần Ải 3.
Vẽ lại bằng: `powershell -File Docs/WorldArt/DungeonStages/tools/stage3_art.ps1 -Out Docs/WorldArt/DungeonStages/Stage3`

| File | Kích thước | Import Unity | Dùng |
|---|---|---|---|
| `Stage3_SurroundWarn_Sheet_3f.png` | 48×16 (3 frame 16×16) | Sprite Mode Multiple, Grid 16×16, PPU 16, Filter **Point**, Compression None, pivot Center | Cảnh báo hướng vây (UI, canvas 640×360). Vẽ hướng sang **phải**: xoay để mũi chỉ **vào trong màn**, tức là từ phía quái sắp tràn vào. Đặt ở mép màn đúng hướng đó, cách mép 10 px. Hiện **1.5 s trước** khi wave spawn. Thứ tự frame 0→1→0→2 ở 10 fps, rồi ẩn khi quái đầu tiên xuất hiện. Mỗi hướng một mũi tên, tối đa 4 |
| `Stage3_EncircleRing_Sheet_2f.png` | 96×24 (2 frame 48×24) | Sprite Mode Multiple, Grid 48×24, PPU 16, Point, pivot Center | Vòng vây dưới chân Hero (world space, nằm dưới Hero, trên nền). Hiện khi có quái sống ở **≥ 3 trên 4 góc phần tư** quanh Hero, bán kính 4 ô. Nhịp đập frame 0↔1 ở 4 fps. Ẩn ngay khi điều kiện hết |

Bảng màu: viền `#141018`, đỏ `#791f1f` / `#a32d2d` / `#e04848` / `#ff7a6a`, sáng `#f7c1c1` / `#ffffff`.
Vòng vây dùng alpha từ 150 đến 245, là các pixel bán trong suốt đặc, không có khử răng cưa.

Đã kiểm: pixel sắc, nền trong suốt, khung frame cố định, ellipse không chạm mép frame.
Ảnh `Stage3_Preview_x6.png` chỉ để xem: khối tím là Hero giả, nền là màu đất của bãi trại.
