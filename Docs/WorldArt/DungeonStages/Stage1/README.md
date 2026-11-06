# Ải 1 · Đường đất đỏ: art UI riêng (bàn giao cho Cursor)

Spec: `Docs/superpowers/specs/2026-11-06-goblin-camp-stages-design.md` mục C, phần Ải 1.
Vẽ lại bằng: `powershell -File Docs/WorldArt/DungeonStages/tools/stage1_art.ps1 -Out Docs/WorldArt/DungeonStages/Stage1`

| File | Kích thước | Import Unity | Dùng |
|---|---|---|---|
| `Stage1_RedDirtPath_9s.png` | 48×48 | Sprite, PPU 16, Filter **Point**, Compression None, Border **16/16/16/16**, Mesh Full Rect | Decal vệt đất đỏ dọc đường S. SpriteRenderer **Draw Mode = Tiled** (không dùng Sliced, vì phần giữa sẽ bị kéo thành sọc). Sorting ở trên cỏ, dưới nhân vật |
| `Stage1_GuideArrow_Sheet_4f.png` | 64×16 (4 frame 16×16) | Sprite Mode Multiple, Grid 16×16, PPU 16, Point, pivot Center | Mũi tên chỉ đường đặt trên vệt đất. Vẽ hướng sang **phải**, xoay theo hướng đường. Chạy 8 fps, lặp. Hiện sau khi dọn xong mỗi khoảng trống, ẩn khi wave tiếp theo bắt đầu |
| `Stage1_GateEdgeIcon.png` | 16×16 | Sprite, PPU 16, Point, pivot Center | Icon cổng ở mép màn hình (UI, canvas 640×360). Hiện khi cổng thoát nằm ngoài camera. Kẹp ở mép, cách mép 8 px, và có 1 mũi tên frame 1 xoay về phía cổng |

Bảng màu:
- Đất: `#a8704a` (nền), `#8a5636`, `#6e4430` (viền), `#c08a5a`, `#d4a070` (sáng), `#5a3a28` (sỏi)
- Mũi tên: `#ba7517` / `#f2d84a` / `#fff6c0`
- Gỗ cổng: `#b07840` / `#8a5a30` / `#5a3a20`, viền `#141018`

Đã kiểm: pixel không khử răng cưa, nền trong suốt, khung frame cố định 16×16, mép decal không lệch khi lát lặp.
Còn mở: ghép nhiều ô liền nhau thì hoa văn lặp hơi lộ theo chiều ngang. Nếu cần sẽ vẽ thêm 1–2 biến thể ô giữa.
