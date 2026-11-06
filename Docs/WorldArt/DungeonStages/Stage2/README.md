# Ải 2 · Hàng rào nhọn: tile mê cung `FenceMaze` (bàn giao cho Cursor)

Spec: `Docs/superpowers/specs/2026-11-06-goblin-camp-stages-design.md` mục C, phần Ải 2.
Vẽ lại bằng: `powershell -File Docs/WorldArt/DungeonStages/tools/stage2_art.ps1 -Out Docs/WorldArt/DungeonStages/Stage2`

Mọi file đều import như nhau: Sprite, PPU 16, Filter **Point**, Compression None, pivot Center, trừ khi bảng ghi khác. Một ô 16×16 tương ứng 1 ô lưới.

| File | Kích thước | Dùng |
|---|---|---|
| `Stage2_Fence_Autotile_4x4.png` | 64×64 = 16 ô 16×16 (Multiple, Grid 16×16) | Hàng rào cọc nhọn. **Chỉ số ô = mask hàng xóm**: N=1, E=2, S=4, W=8. Ô thứ `mask` xếp theo hàng: cột `mask % 4`, hàng `mask / 4`. Ví dụ: 10 = E+W (đoạn ngang), 5 = N+S (đoạn dọc), 6 = E+S (góc), 15 = ngã tư, 0 = cọc đơn. Có thể dùng Unity Rule Tile, hoặc tự tính mask khi dựng root `FenceMaze` từ lưới ký tự |
| `Stage2_StoneWall_Autotile_4x4.png` | 64×64 = 16 ô 16×16 (Multiple, Grid 16×16) | **Tường đá cho mê cung (thay cho rào cọc, theo yêu cầu 2026-11-06).** Cùng quy tắc mask với rào: N=1, E=2, S=4, W=8, ô ở cột `mask % 4`, hàng `mask / 4`. Mặt trên lát đá có rêu. Mặt gạch phía trước chỉ hiện ở cạnh dưới nào không nối xuống phía nam. Hàng pixel cuối là bóng mờ đổ lên nền. Vẽ lại bằng `tools/stage2_stonewall.ps1 -Out Docs/WorldArt/DungeonStages/Stage2`. Ảnh xem trước: `Stage2_StoneWall_Preview_x4.png` |
| `Stage2_FenceGate_2f.png` | 32×16 = 2 frame (Grid 16×16) | Cổng thoát. Frame 0 đóng, frame 1 mở. **Chỉ đặt trên đoạn rào ngang (E-W)**, tức là lối ra nằm ở tường trên hoặc tường dưới. Mở khi dọn xong wave cuối |
| `Stage2_Spikes.png` | 16×16 | Cọc nhọn hazard. Chạm vào mất 5% HP, hồi chiêu 1 s. Đặt ở ngõ cụt và ở khúc cua |
| `Stage2_Chest_2f.png` | 32×16 = 2 frame (Grid 16×16) | Rương nhỏ ở ngõ cụt. Frame 0 đóng, frame 1 mở. Bấm E để mở, thưởng +30 Vàng, mỗi rương mở một lần mỗi lượt chơi |
| `Stage2_MiniMap_DeadEnd.png` | 8×8 | Dấu ✕ trên minimap cho ngõ cụt người chơi đã thấy |
| `Stage2_ExitCompass.png` | 16×16 | La bàn ở góc minimap. Kim đỏ vẽ chỉ **lên**, xoay để chỉ hướng cổng thoát |

Minimap phủ tối: ô chưa đi qua tô `#0e1014`, alpha 0.85. Ô đã đi qua thì mở dần trong bán kính 2 ô quanh Hero. Phần này không cần art riêng.

Bảng màu: gỗ `#b07840` / `#8a5a30` / `#5a3a20`, đầu cọc `#d4b483`, viền `#141018`, bóng `#141018` alpha 70. Rương viền vàng `#f2d84a` / `#ba7517`. ✕ và kim la bàn `#e04848`.

Đã kiểm: dựng thử mê cung 13×9 trong `Stage2_Preview_x4.png` bằng đúng quy tắc mask ở trên. Góc, chữ T, đầu cụt và cổng đều nối liền. Pixel sắc, nền trong suốt.
Còn mở: đoạn rào dọc nhìn mảnh hơn đoạn ngang (do góc nhìn 2.5D). Nếu thấy dễ đi lọt thì collider vẫn để đủ 1 ô.
