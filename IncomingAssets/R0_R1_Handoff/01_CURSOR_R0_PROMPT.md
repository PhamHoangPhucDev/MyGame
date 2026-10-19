# Prompt Cursor — R0 Audit only
Bạn là Lead Unity Engineer, làm việc trên game Pixel Art Action RPG hiện tại (Level 1–4). TẠM DỪNG Phase 12E.

CHỐT KIẾN TRÚC:
- Hỗ trợ CẢ 9:16 (chuẩn so sánh video) VÀ 16:9 (giữ build hiện tại).
- Không phá Level1–4, Save/Load, Mob Label (tên+Level+HP luôn hiển thị), R Attack ở bottom-right, 2/3/4/V/C ở bottom-center, PPU16, Pixel Perfect, Enemy AI, Combat.
- Video reference ZIP 30 clip, 29 đọc được; clip 21 hỏng. KHÔNG coi caption/watermark là UI.

R0 CHỈ AUDIT, KHÔNG SỬA GAMEPLAY:
1) Tạo Git branch/tag/backup tên gợi nhớ `reference_rebuild_r0`; kiểm tra status.
2) Phát hiện version Unity+URP và đọc scene hierarchy, PlayerController/Combat, Health, EnemyController/Spawner, LevelFlow, GameProgress, CameraFollow, PixelPerfect, SpriteSorter, TreeCanopyOcclusion, SkillHotbarUI, WorldLabelsCanvas, DamagePopup.
3) Inventory mọi ground tile, texture, hero/goblin spritesheets, clip animation, prop, VFX/SFX; export path, kích thước pixel, frame/cell, PPU, filter, compression, alpha, pivot, material, sorting and usages.
4) Chụp 12 screenshot ở tình huống thật: L1 forest, L2 trail, L3 camp, L4 outpost, Hero idle/walk/R1/R2/R3, 5 Goblin melee, Mob Label/HP, Tree Occlusion, Skill Hotbar. Mỗi ảnh ghi camera profile/time/state.
5) Log test baseline ở 16:9 thật và mô tả khả năng 9:16; không cần can thiệp code mới trong R0.
6) Đề xuất `AspectVisualProfile` portrait 144×256 (576×1024) và landscape 480×270 (1920×1080) với shared gameplay; kiểm tra scale Hero, viewport bounds, UI responsive, orientation handling. Chưa triển khai trước phê duyệt.
7) Xuất `Docs/ReferenceRebuild/PROJECT_AUDIT.md`, `CURRENT_ASSET_INVENTORY.csv`, `BASELINE_TESTS.md`, `SCREENSHOTS/` (nếu công cụ cho phép).
8) Ghi PASS/FAIL/NOT TESTED cùng bằng chứng, đặc biệt Mob Label không được tự động ẩn. Liệt kê 10 sai khác lớn giữa game hiện tại và video theo dữ liệu được cung cấp. Nếu không thể xem reference nguyên bản, ghi giới hạn và chờ Claude shotlist.

DỪNG, không tạo Level5, không thay đổi scene/tile/Prefabs ngoài việc tạo backup và báo cáo audit. Gửi file report cho tôi và Claude.
