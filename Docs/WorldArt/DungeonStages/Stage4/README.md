# Ải 4 · Cầu vực: art UI và cầu (bàn giao cho Cursor)

Spec: `Docs/superpowers/specs/2026-11-06-goblin-camp-stages-design.md` mục C, phần Ải 4.
Vẽ lại bằng: `powershell -File Docs/WorldArt/DungeonStages/tools/stage4_art.ps1 -Out Docs/WorldArt/DungeonStages/Stage4`

| File | Kích thước | Import Unity | Dùng |
|---|---|---|---|
| `Stage4_Bridge_Tiles_4x16x32.png` | 64×32 = 4 ô 16×32 | Multiple, Grid 16×32, PPU 16, Filter **Point**, Compression None, pivot Center | Cầu nằm ngang bắc qua khe vực dọc trong layout `GoblinOutpost`. Thứ tự ô: **0 đầu cầu** (cột trái), **1 giữa** (lát lặp liền mạch), **2 cuối cầu** (cột phải), **3 đoạn vỡ** (mất 1 tấm ván, dây dưới đứt). Cầu dài 3 đoạn, mỗi đoạn là một wave. Đề xuất: `0,1,1,1,3,1,1,1,3,1,1,2` (1 ô = 1 đơn vị). Khe giữa các tấm ván để trong suốt cho thấy vực bên dưới |
| `Stage4_BridgeBar_Frame.png` | 80×14 | Sprite, PPU 16, Point. Không cần 9-slice | Thanh tiến độ cầu ở trên-giữa (canvas 640×360), thay ObjectivePlate trong ải 4. Icon cầu bên trái, 3 ô trống ở x = 18 / 38 / 58, y = 3 |
| `Stage4_BridgeBar_Pips_3x18x8.png` | 54×8 = 3 ô 18×8 | Multiple, Grid 18×8, PPU 16, Point | Ô 0 = **đã qua** (vàng), 1 = **đang đi** (sáng, nhấp nháy 0↔1 ở 3 fps), 2 = **chưa tới** (xám). Đặt vào 3 ô trống của frame. Có thể thêm text `Đoạn 2/3` bên phải thanh |
| `Stage4_EdgeDanger_9s.png` | 48×48, Border 16/16/16/16 | Sprite, PPU 16, Point, Mesh Full Rect | Viền đỏ toàn màn (UI Image **Sliced**, Fill Center **tắt**). Hiện khi Hero đứng cách mép cầu ≤ 0.5 ô, alpha fade-in 0.15 s. Ở đây kéo giãn được vì phần giữa trong suốt |

Bảng màu: gỗ `#b07840` / `#8a5a30` / `#5a3a20`, dây `#d4b483` / `#8a6a3a`, đinh `#3a3a3a`, viền `#141018`. Thanh: nền `#1c1f26`, mép `#4a5260`, pip vàng `#f2d84a`. Viền cảnh báo: `#e04848`, alpha 40–238.

Đã kiểm: pixel sắc, nền trong suốt, ô giữa cầu nối liền 2 đầu, các frame cùng kích thước.
Ảnh `Stage4_Preview_x5.png` chỉ để xem: khe vực tối, cầu ghép theo `0,1,1,3,1,2`, thanh hiện đủ 3 trạng thái.
