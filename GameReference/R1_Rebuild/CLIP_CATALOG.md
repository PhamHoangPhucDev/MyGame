# CLIP_CATALOG — 30 clip tham chiếu (R1 Rebuild)

Nguồn: `C:\Users\PC-119\Downloads\Telegram Desktop\video_2026-10-19_14-36-38 (N).mp4` (giải nén từ `video_2026-10-19_14-36-38 (4).zip`). Clip 01 = file không có số `(N)`.
Tất cả clip: 576×1024, dọc 9:16, TikTok @baredevs. Game gốc làm bằng **Godot** (thấy trong C24, C13), không phải Unity.

## Cách phân tích và giới hạn

- Máy không có ffmpeg/Python. Contact sheet được dựng bằng trình duyệt tích hợp (HTML5 `<video>` + canvas): 16 khung/clip, chia đều theo thời lượng. Timecode trong tài liệu là timecode của các khung đó (sai số ±0.1 s).
- **C21 không giải mã được** trong Chromium (lỗi decode, có thể do codec). Chưa phân tích C21. Cần xuất lại sang H.264 hoặc gửi screenshot.
- 16 khung/clip có thể bỏ sót chi tiết diễn ra rất nhanh (một frame hitstop, một frame VFX). Các khung chính đã được trích ở độ phân giải gốc trong `ref_frames/`.
- **"Reference Rebuild Plan Pack" và `PROJECT_AUDIT.md` không có trong máy.** Chỉ có `IncomingAssets/R0_Audit/ASSET_INVENTORY.csv` và `UNITY_SNAPSHOT.txt`. Contact sheet trong `contact_sheets/` là do tôi dựng lại.
- Có chữ phụ đề TikTok và watermark che một phần hình ở nhiều khung.

Ký hiệu loại: **CORE** = gameplay chính · **OPT** = tính năng tuỳ chọn · **MENU** = màn menu/UI phụ · **EDITOR** = footage editor/ngoài game · **OUTRO** = logo TikTok.

| Clip | Thời lượng | Loại | Nội dung | Timecode đáng dùng |
|---|---|---|---|---|
| C01 | 40.5 s | OPT + CORE | Montage biome: rừng, **lava** (0:19), **hồ nước** (0:26). Skill **meteor lửa** (0:06.3), **đóng băng** số xanh (0:08.9–0:11.4). Pixie, cây. Outro từ 0:36 | 0:06.3 meteor · 0:08.9 freeze · 0:19.0 lava · 0:26.6 nước |
| C02 | 15.7 s | OPT | Thời tiết: drizzle/rain/storm. Hotbar đầy hàng dưới (~12 ô), thanh XP "5/20" | 0:05.4 hotbar + XP |
| C03 | 68.4 s | CORE + EDITOR | Chỉnh tốc độ sprint. Combat: lưỡi liềm xanh, "x35 HITS", "4,295 DAMAGE", boss **Dryad Lv.4** (thanh đỏ trên đỉnh), số trắng "100", số vàng "313", nhà tròn mái tranh. Cuối: Unity/Godot inspector | 0:36.3 boss bar · 0:40.6 số sát thương · 0:57.7 chém x52 |
| C04 | 23.5 s | OPT | Hồi sinh: viền đỏ, "You have died", cột sáng hồi sinh ở shrine, hồi sinh bạn (co-op) | 0:02.2 death panel · 0:08.1 cột sáng |
| C05 | 24.6 s | MENU/OPT | Đổi màu tóc/áo nhân vật. **Hero cận cảnh rõ nhất** | 0:00.8 hero idle |
| C06 | 54.3 s | CORE + OPT | Minimap, quest, NPC. Satyr Lv.2, **OwlBear Lv.5** (boss bar), "QUEST COMPLETED", lưỡi liềm lớn, số vàng | 0:32.3 số 117 · 0:49.2 lưỡi liềm + boss |
| C07 | 45.1 s | MENU | Thiết kế lại UI: chân dung + HP "340/340" + Lv, ô buff/debuff. Menu **Aspects** (cây kỹ năng). Nền gạch dungeon | 0:07.0 HUD chân dung |
| C08 | 52.8 s | OPT | Thu thập: Fox, Deer, thanh tiến trình, bụi cây, đá | 0:11.5 boss bar nhỏ "Fox Lv.1" |
| C09 | 24.3 s | CORE + EDITOR | Bia tập "Training Dummy" + bảng DPS. "x149 HITS", lưỡi liềm xanh đôi. Từ 0:16 là animation thô trong editor | 0:11.4 x98 HITS + lưỡi liềm |
| C10 | 35.2 s | OPT | Đào quặng trong mưa, vách đất phía trên bản đồ | 0:01.1 vách đất |
| C11 | 54.6 s | CORE (boss) | Boss **CYCLOPS**: thanh máu ngang toàn màn hình, **vòng tròn đỏ cảnh báo AoE**, đá rơi, co-op | 0:32.4 chém + vòng đỏ · 0:49.5 |
| C12 | 31.4 s | OPT | Sự kiện tìm kho báu, vòng sóng âm, Dryad cưỡi nai, rương | 0:12.7 rừng + cây |
| C13 | 57.0 s | OPT + EDITOR | Sự kiện **Ward**: "đứng trong vòng và tiêu diệt các đợt", đếm ngược, rương thưởng, cổng xanh. Cuối: chỉnh tilemap trong editor | 0:08.9 ward + waves (gần với hệ wave Phase 12) |
| C14 | 56.4 s | OPT + MENU | Level up, mở khoá skill, waypoint. Menu ngọc / cường hoá trang bị. Hồ nước | 0:05.3 level up · 0:19.4 HUD chân dung |
| C15 | 28.8 s | CORE (landscape) | **Bản PC màn hình ngang** (letterbox): HUD chân dung trái trên, quest tracker phải, hotbar nhỏ giữa đáy, trại goblin, hội thoại NPC | 0:26.1 **HUD landscape** |
| C16 | 11.0 s | OPT | Kiến trúc thị trấn (nhà thờ mái xanh) | — |
| C17 | 67.2 s | CORE + EDITOR | Devlog **số sát thương**: chồng số "58 58 58", "x19 HITS", "374 DAMAGE". Có đoạn code | **0:10.5 số sát thương** · 0:02.1 |
| C18 | 60.8 s | OPT | Tutorial roll/sprint, bia tập nhiều pha, vòng cam | 0:05.7 Goblin Lv.1 boss bar |
| C19 | 24.4 s | OPT | Chu kỳ ngày/đêm quanh lều goblin. Outro từ 0:20 | 0:08.4 lều + cây |
| C20 | 87.9 s | CORE (enemy/boss) | Trại goblin ban đêm: **Gob Archer Lv.8**, **Gob Spearman Lv.6**, boss **Goblin Chief Lv.11**. Đường bắn cung, cảnh báo hình nón đỏ, lều lớn | 0:41.2 tia cung · **0:57.7 Goblin Chief** · 1:08.6 cảnh báo nón |
| C21 | ? | — | **Không giải mã được** | — |
| C22 | 51.5 s | CORE (trại goblin) | Trại ban ngày giống `gameplay_reference.mp4`: Goblin Lv.3 đông, Gobrider, lều sừng, bát lửa, bộ đếm "Souls 10/10", **Cyclops Lv.8 Elite**, "Respawn at Shrine" | **0:04.8 trại + hotbar** · 0:33.8 đông quái · 0:17.7 Cyclops |
| C23 | 36.8 s | OPT + EDITOR | Cửa hàng thị trấn, nền đá cuội. Vẽ bảng hiệu trong Aseprite | — |
| C24 | 35.7 s | CORE + EDITOR | Cây che nhân vật (trong suốt khi đi sau), **cây gốc trong Godot editor** | **0:14.5 cây nguồn** · 0:05.6 |
| C25 | 31.1 s | CORE + MENU | **Đòn R lưỡi liềm trắng-xanh lớn nhất** (0:02.9). Menu skill: "Slash — 3-hit combo slash attack" | **0:02.9 đòn R** · 0:14.6 mô tả Slash |
| C26 | 39.6 s | CORE (VFX) | Đánh Cyclops trên bãi đất: **vòng đỏ AoE**, lửa, **băng chữ X**, bầy hạt đen, **nứt đất**, hotbar có số cooldown | 0:03.7 vòng đỏ · 0:06.2 lửa · **0:13.6 băng + hotbar** · 0:21.0 nứt đất |
| C27 | 39.2 s | OPT | Nhà ở, trồng trọt | — |
| C28 | 51.5 s | OPT | Đặt nhà, làm đường, ruộng | 0:04.8 đường đất mép răng cưa |
| C29 | 73.7 s | EDITOR/ngoài game + CORE | Mốc 1k follower, profile TikTok, Discord, bình chọn class. Combat Satyr: số vàng chồng | 1:06.7 số vàng |
| C30 | 26.9 s | CORE (boss, landscape) | Phòng ngai **vua goblin**: hội thoại "You will DIE here", boss xoay vòng lốc trắng | 0:24.4 boss xoay |

## Phân loại theo nhóm

**CORE — cần khớp:** di chuyển top-down; R = chém 3 nhát (C25 0:14.6); skill 2/3/4/V/C có cooldown; một hàng hotbar; nhãn quái + HP bar; số sát thương + "xN HITS" + "N DAMAGE" tổng; trại goblin đông quái; boss có thanh máu trên đỉnh và vùng cảnh báo (vòng đỏ, hình nón, tia bắn); ô bình máu nổi phía trên hotbar; chết → hồi sinh ở shrine; HUD landscape (C15).

**OPT — làm sau, không bắt buộc cho độ giống:** mưa (C02), ngày/đêm (C19), biome lava/nước (C01, C14), minimap/quest (C06, C15), thu thập/đào (C08, C10), kho báu (C12), sự kiện Ward (C13), level up (C14), hội thoại NPC (C15, C30), co-op/hồi sinh bạn (C04, C11), thị trấn/cửa hàng (C16, C23), nhà/ruộng (C27, C28), đổi màu nhân vật (C05), roll/sprint (C03, C18), tutorial (C18).

**MENU:** Aspects/cây skill (C07, C25), ngọc/cường hoá (C14), xác nhận cổng (C13), bảng chết (C04, C22).

**EDITOR / ngoài game — không dùng làm tham chiếu hình:** C03 cuối, C09 từ 0:16, C13 cuối, C17 0:06, C23 0:24+, C24 0:14–0:28, C29 (profile, Discord), outro TikTok (C01, C12, C19).

## Khung hình tốt nhất theo thành phần

| Thành phần | Khung chính | Khung phụ | Crop trong `ref_crops/` |
|---|---|---|---|
| Hero | C22 0:04.8 (trong game) | C05 0:00.8 (cận cảnh), C25 0:02.9 (đang chém) | `REF_HERO_ingame`, `REF_HERO_idle`, `REF_HERO_attack_R` |
| Goblin / Gobrider | C22 0:04.8, C22 0:33.8 | C20 0:13.7 (Archer), C20 0:30.2 (Spearman); `GameReference/frames/07_enemy_cluster` | `REF_GOBLIN`, `REF_GOBLIN_crowd` |
| Ground | C25 0:02.9 (cỏ + đường đất) | C26 0:01.2 (bãi đất, hố), C22 0:04.8 (mảng cỏ tối) | `REF_GROUND_grass_dirt`, `REF_GROUND_dirt_arena` |
| Tree | C25 0:02.9 (viền tím trong game) | C24 0:14.5 (sprite gốc), C12 0:12.7 | `REF_TREE_ingame`, `REF_TREE_source` |
| Basic Attack R | C25 0:02.9 | C06 0:49.2, C03 0:57.7, C09 0:11.4 | `REF_HERO_attack_R` |
| Combo & Damage | C17 0:10.5 | C26 0:13.6, C29 1:06.7, C03 0:40.6 | `REF_DAMAGE_COMBO` |
| Skill VFX | C26 0:13.6 (băng) | C26 0:03.7 (vòng đỏ), 0:06.2 (lửa), 0:21.0 (nứt đất); C01 0:06.3 (meteor), 0:08.9 (freeze) | `REF_SKILL_ice_burst` |
| Hotbar | C22 0:04.8 | C26 0:13.6 (cooldown), C25 0:02.9 (ô bình máu), C15 0:26.1 (landscape) | `REF_HOTBAR`, `REF_HOTBAR_cooldown`, `REF_LANDSCAPE_HUD` |
| Boss | C20 0:57.7 (Goblin Chief) | C11 0:32.4 (Cyclops + vòng đỏ), C22 0:17.7 (Cyclops Elite), C30 0:24.4 (vua goblin) | `REF_BOSS_bar_chief` |
| Trại goblin | C22 0:04.8 | C22 0:01.6, C19 0:08.4, C15 0:26.1 | `REF_CAMP_tent`, `REF_CAMP_brazier` |

## Tỉ lệ pixel ước lượng

Đo trên Goblin (C22 0:04.8): mỗi pixel art ≈ 3.3 px video, nên vùng nhìn dọc ≈ **172×306 pixel art**. Unity hiện là landscape 480×270 (PPU 16), chiều cao vùng nhìn tương đương. **Không cần đổi camera.**

| Vật thể | Tham chiếu (pixel art, ước lượng) | Unity hiện tại |
|---|---|---|
| Hero | ≈ 18×22 | 16×20 trong ô 32×32 — khớp |
| Goblin | ≈ 20×16 | ô 26×22 — khớp |
| Cây lớn | ≈ 96×112 (C24 có thước pixel) | 62×78 / 54×68 — nhỏ hơn khoảng 1.5 lần |
| Lều sừng | ≈ 105×100 | TentA 40×48 — nhỏ hơn khoảng 2.5 lần |
| Bát lửa | ≈ 52×60 | 26×29 — nhỏ hơn 2 lần |
| Lưỡi liềm R | ≈ 75–100 px cao (≈ 4 lần chiều cao hero) | Cần đo lại (baseline `03_r_attack_combo` không bắt được khung chém) |
