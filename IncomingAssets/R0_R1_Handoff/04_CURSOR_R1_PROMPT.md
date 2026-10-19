# Prompt Cursor — R1 Sandbox execution (chỉ sau khi user duyệt R0 + Canva samples)
Bạn là Unity Engineer. Chỉ bắt đầu khi đã có phê duyệt R0 `DIFFERENCE_MATRIX`, Asset Manifest và 3 Canva sample art. Không làm Phase 12E và không overwrite Level1–4.

1) Tạo sandbox scene `ReferenceCombatSandbox` và backup/tag. Giữ shared PlayerCombat, EnemyController, LevelFlow logic nhưng sandbox không ghi thay đổi GameProgress của Level thật.
2) Tạo/áp dụng profile 9:16 (144×256 ref, test 576×1024) VÀ 16:9 (480×270 ref, test 1920×1080) theo URP Pixel Perfect/PPU16 existing; anchor UI tự tái bố cục. Một gameplay core, một bộ input+listener, 2 camera/UI profiles. Không áp dụng Screen.SetResolution cứng cho thiết bị mobile. Kiểm tra Sprite screen-space size/height, world-height và map bounds.
3) Import approved grass/dirt transitions, tree canopy/trunk/shadow, slash in `Assets/Art/Rebuild`; preserve originals. Point filtering, slice frame, PPU16, alpha + no mipmaps and sensible compression.
4) Sandbox: forest clearing và dirt path tự nhiên, 2 cây có TreeCanopyOcclusion, Hero tóc trắng + Sword, 5 Goblin + 1 Gobrider với labels tên/cấp/HP ALWAYS on; R 3hit & hold, 2/3/4/V/C, hitstop, knockback, damagepopup, xN hits. Không thêm hệ thống trùng lặp.
5) Test tree fade 1.0→0.4→1.0, trunk collider, sorting body-sword-ground, Hero visible 60s, no stale sprite cache 3 Stop/Play, mob labels persist after knockback, 10s Hold R no lost attacks.
6) Capture matched ref/current side-by-side ở portrait 9:16 (3 frames: idle/environment, 5 Goblins melee, attack R) và landscape adapted frames. Quay video 20–30s ở cả profiles nếu tool cho phép.
7) Multi-res optional/regression landscape 1600×900, 1280×720; check HUD safe area. Test Level1–4 unchanged and L2 ExitGate+save. Compile and Play Mode: PASS/FAIL/NOT TESTED with evidence.
8) Xuất `R1_SANDBOX_RESULTS.md`, `R1_BEFORE_AFTER/`, delta list, list asset missing, actual pixel-size scale comparison. DỪNG trước rollout Level1–4 và chờ user nghiệm thu.
