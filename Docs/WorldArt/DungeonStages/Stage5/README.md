# Ải 5 · Lều xương: art UI boss (bàn giao cho Cursor)

Spec: `Docs/superpowers/specs/2026-11-06-goblin-camp-stages-design.md` mục C, phần Ải 5.
Vẽ lại bằng: `powershell -File Docs/WorldArt/DungeonStages/tools/stage5_art.ps1 -Out Docs/WorldArt/DungeonStages/Stage5`

| File | Kích thước | Import Unity | Dùng |
|---|---|---|---|
| `Stage5_RitualRing.png` | 64×32 | Sprite, PPU 16, Filter **Point**, Compression None, pivot Center | Vòng nghi lễ bằng xương, có 4 sọ nhỏ, đặt trên nền giữa sân `GobriderStronghold` (world, dưới nhân vật). Boss đứng trong vòng. Có thể scale ×2 (tức 8×4 ô) nếu sân rộng. Chỉ scale theo số nguyên |
| `Stage5_RitualTelegraph_Sheet_4f.png` | 256×32 = 4 frame 64×32 | Multiple, Grid 64×32, PPU 16, Point, pivot Center | Báo trước đòn mạnh của boss, đặt chồng **trên** vòng xương, cùng vị trí và cùng scale. Frame 0→1→2 trong 0.9 s (đỏ lan từ ngoài vào), frame 3 là chớp sáng 0.1 s đúng lúc đòn đánh trúng, sau đó ẩn |
| `Stage5_BossPhaseNotch_2f.png` | 8×12 = 2 frame 4×12 | Multiple, Grid 4×12, PPU 16, Point, pivot Center | Vạch pha trên thanh máu boss (`BossHudUI`). Đặt 2 vạch ở **66% và 33%** chiều rộng fill. Frame 0 là bình thường. Khi máu tụt qua vạch nào thì vạch đó đổi sang frame 1 (chớp trắng) trong 0.3 s, rồi giữ frame 1 với alpha 0.5 để đánh dấu pha đã qua. Thanh hiển thị cao 22 px (landscape), nên scale vạch theo chiều cao thanh |
| `Stage5_ChapterBanner_9s.png` | 96×32, Border L 24 / R 24 / T 0 / B 0 | Sprite, PPU 16, Point, Mesh Full Rect | Banner "Chương hoàn thành" khi hạ boss. UI Image **Sliced** (hoặc Tiled cho phần giữa), rộng khoảng 200 px trên canvas 640×360. **Chữ do Unity render**: dùng font pixel của `UiKit`, màu `#f2d84a`, viền `#141018`. Text gợi ý: `Chương 1 hoàn thành` / `Trại Goblin`. Trượt từ trên xuống trong 0.3 s, giữ 2 s |

Bảng màu: xương `#e8e0c8` / `#b4a88a` / `#7a6e58`, viền `#141018`. Telegraph `#e04848` / `#ff7a6a` / `#ffffff`. Vạch `#f2d84a` / `#ba7517`. Banner tím `#26215c` / `#534ab7` / `#7f77dd`, viền vàng `#f2d84a` / `#ba7517`.

Đã kiểm: pixel sắc, nền trong suốt, các frame cùng khung, telegraph khớp đúng vòng xương, frame chớp không còn lỗ ở tâm.
Ảnh `Stage5_Preview_x5.png` chỉ để xem: 4 frame telegraph đặt trên vòng, thanh máu giả có 2 vạch (một vạch vừa vỡ), banner kéo dài bằng 9-slice, góc phải là vòng xương đứng riêng.
