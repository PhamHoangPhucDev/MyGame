# R0–R1 / Dual-Aspect Reference Rebuild Plan

## Quyết định đã chốt
- **Hai profile**: Portrait 9:16 = chuẩn đối chiếu video; Landscape 16:9 = profile game hiện hữu phải tiếp tục chơi được.
- **Pause Phase 12E / Level 5**. Không đổi Progress, không xóa Level 1–4, không bỏ labels của Mob, không làm lại Combat System trong R0/R1.
- Dữ liệu mẫu: ZIP 30 clip, 29 clip đọc được, 1 clip (21) lỗi MP4; các clip đọc được 576×1024. Không coi phụ đề, watermark, gameplay editor là HUD của game.
- Mục tiêu độ giống 90–100% là đích hướng tới, không phải cam kết hoặc kết quả tự nhận. Đánh giá 9:16 bằng visual comparison; 16:9 bằng tương đồng art/gameplay + usability của bản thích nghi.

## Nguyên tắc bảo toàn
- Có Git branch/tag + backup Scene/Prefab & save sample test trước khi thay đổi.
- Không hardcode hoặc cưỡng ép Screen.SetResolution trên Android để giả lập 9:16.
- Một gameplay core và một save flow; 2 cấu hình View/HUD, không sao chép LevelFlow, WaveManager, PlayerCombat, EnemySpawner hay handler input.
- Sprite Sorting: Ground dưới characters; Sword order theo body; TreeCanopyOcclusion fade có hoàn nguyên; tất cả Mob tên/cấp độ/HP vẫn xuất hiện.
- Unity URP Pixel Perfect PPU16 giữ nguyên; đề xuất cấu hình profile portrait reference 144×256 / test 576×1024 và landscape reference 480×270 / test 1920×1080. Camera view height 16 vs 16.875 world units; yêu cầu đo Hero viewport size và cân nhắc đồng bộ **cảm giác tỷ lệ**, không bóp méo sprite. 1600×900 & 1280×720 cần thử pixel scaling tại mức không nguyên.

## R0. Freeze + inventory + visual audit (NO gameplay edits)
### R0.1 Audit nguồn
- Ghi version Unity/URP, scene, build targets, PC/Android orientation, camera/PPC config, UI canvas hierarchy và save keys.
- Inventory Sprite/Atlas: path, px size, cell/frame size, animation mapping, PPU, filtering, compression, alpha, pivot, sorting, usages; liệt kê Tilemap và từng ground tile gây pattern.
- Audit PlayerCombat, enemy AI, camera-follow, sorting, tree occlusion, mob labels, hotbar, current VFX, LevelFlow L1–4, GameProgress và Data/Level assets.
- Freeze trạng thái Level 1–4 / Save / keybind R & 2/3/4/V/C / mob labels.

### R0.2 Canonical reference + alignment
- Clip 04 / 19 → forest ground and tree/occlusion; 20 / 22 → Goblin camp, enemy labels, HP; 03 / 06 / 12 → Hero and battle/VFX; 26/18 → boss telegraph (later); 14/25 → meta (later).
- Mỗi shot ghi `clip_id`, `timecode`, `frame ref`, `camera`, `hero scale in pixels`, `foreground/midground`, `mob count`, `HUD relevant`, `what is video overlay`.
- Screenshot thật của game theo scene + profile, cùng ngữ cảnh và khoảng cách camera; không so hero idle với boss cảnh đông.
- Đầu ra `REFERENCE_SHOTLIST.csv`, `CURRENT_ASSET_INVENTORY.csv`, `DIFFERENCE_MATRIX.csv`, `BASELINE_TESTS.md`, screenshot, `REBUILD_DECISIONS.md`.

### R0.3 Dual-aspect architecture review, may only implement after explicit R1 go-ahead
- Đề xuất ScriptableObject `AspectVisualProfile` (hoặc mở rộng class hiện có): portrait 144×256 ref, UI 576×1024, preferred camera offset; landscape 480×270 ref, UI 1920×1080, camera offset; common PPU 16.
- Có thể dùng adaptive Canvas reflow qua anchor/profile-specific container trong một Canvas; không duplicate UI input processing. Profile chọn từ aspect/build setting/Editor test preset và phải hỗ trợ thực nghiệm ở 2 mode.
- Camera/world map: test tại Hero center và mép map, chặn lộ vùng trống, hiệu ứng occlusion ở mọi profile.
- Device: chọn orientation qua Player Settings/build behavior đã phê duyệt; trên device đang xoay, preserve input state an toàn; chưa thiết lập auto-rotation nếu chưa yêu cầu.

### R0 Gate (hoàn thành trước khi tạo sprite)
- Backups/branch kiểm chứng được.
- 12 screenshot baseline có profile, scale và shot match.
- 10 sai khác P0/P1/P2; 3 asset mẫu đầu tiên và acceptance specs.
- 0 file core gameplay bị đổi ngoài checkpoint/audit.

## R1. Reference Combat Sandbox (không sửa Level 1–4)
### R1.1 Scene sandbox
- Tạo scene thử `ReferenceCombatSandbox` hoặc prefab test trong scene debug riêng. Ground clearing, natural dirt path, 2 tree trunk + separate canopy, 5 Goblin và 1 Gobrider, Hero.
- R Hold 3-hit, 2/3/4/V/C skill, knockback, hitstop, xN HITS, damage popup, tên+Lv+HP mọi Mob. Không thêm boss hay progression.
- Dual profile 9:16 chính cho video benchmark và 16:9 để xác minh adaptable. Các điểm thử ứng với same world anchors.

### R1.2 Canva Asset Batch 01
- Grass/dirt set: 16×16 cell theo grid nếu audit xác nhận; center, transition 4 cạnh+góc trong/ngoài, >=3 biến thể cỏ và đốm trang trí nhẹ. Không checkerboard/sọc, không tạo seam.
- Tree 01: canopy/trunk/shadow PNG riêng với pivot, pixel size/final palette được chốt từ audit; tree opacity 1 → 0.4 → 1 khi che Hero, trunk opaque + chân collider.
- Blue/white sword arc sheet: các frame có alpha rõ, vị trí/pivot/frame cell thống nhất; chốt frame count theo PlayerCombat animation existing. Thêm impact dust/spark nếu asset hiện có chưa đủ.
- Canva làm concept/previews và xuất PNG; ảnh Canva cần pixel-cleanup/tilesheet editor trước import, không tự coi output là pixel-perfect production sprite.

### R1.3 Cursor integration
- Đặt asset mới trong `Assets/Art/Rebuild/...`, KHÔNG overwrite Phase4 originals trước khi được duyệt.
- Import Point Filtering, Sprite (2D and UI), alpha, no mipmaps, PPU16, compression thích hợp platform; slicing/animation validated.
- Dùng material instance hoặc per-renderer property cho occlusion, không đụng shared material.
- HUD responsive 2 profile: R right, skill row center, joystick left mobile, Hp/top, Mob labels preserved.
- Capture 2× portrait gameplay snapshots and 1× landscape run, 20–30s test video each.

### R1 Gate / Criteria
1. Ground không có checker/sọc, 0 seam rõ tại patch test; đường đất liền và tự nhiên, tile không blur.
2. Hero body + sword hiện nhất quán, Ground không che; 0 mất Hero tại test 60s 120 mẫu như benchmark cũ.
3. Tree canopy alpha ~0.4 khi che Hero, trả 1 khi rời; collider chỉ thân; slash không biến mất hoàn toàn.
4. Label Mob luôn hiển thị và theo Mob; mọi Goblin/Gobrider có đúng tên/cấp và HP (giảm overlap, KHÔNG hide labels).
5. Giữ R 10s đánh liên tục; 3 nhát và hitbox/VFX khớp; combo từ hit thực và mỗi cast không double-hit.
6. Test 576×1024 portrait, 1920×1080 landscape, thêm 1600×900/1280×720 nếu sửa responsive; UI không clip, không stale-cache Domain Reload.
7. L1–4, Level2 Exit, GameProgress, Victory/Retry không bị thay đổi; Console không có new errors.
8. Cursor reports PASS/FAIL/NOT TESTED with objective evidence. **DỪNG** trước merge/rollout tất cả Level; đợi duyệt shot sandbox.

## Rubric so sánh (same scene; tránh chấm khác nội dung)
| Factor | Weight | What to measure |
|---|---:|---|
| Environment & tiles | 30% | Palette, geometry, organic paths, tree/occlusion, pixel quality |
| Hero & mobs | 20% | Screen-space scale, silhouette, frame pacing, labels |
| Combat & VFX | 25% | Slash shape/timing, density, AoE, hit reactions, combos |
| Camera & HUD | 15% | Profile 9:16 match, hierarchy, legibility, hotbar |
| Interaction | 10% | Input feel, target choice, stagger, spawn/recovery |

Score portrait against matching reference shot; 16:9 gets UX/adaptation score separately, not falsely 100% identical layout.

## Tool-to-tool handoff
- **Cursor R0** → PROJECT_AUDIT/asset inventory + 12 current captures (read-only except checkpoint).
- **Claude** receives original reference ZIP, contact sheets/index, Cursor audit/screenshots; produces SHOTLIST, DIFFERENCE_MATRIX, ART_MANIFEST, sample priority, no project edits.
- **User approves** canonical 9:16 shots + Canva specifications.
- **Canva** creates only three Batch01 prototypes, independent transparent PNG/tiles, no full game mockup.
- **Cursor R1** imports approved PNG into sandbox, implements dual aspect and QA, returns captures/tests.
- **Claude** QA review against shot list, marks remaining deltas; **user approves** before updating existing Levels.

## Copyright & unknowns
- Video reference can guide original inspired assets; if assets belong to third party, don't repackage exact protected game assets for distribution without rights.
- Clip 21 unreadable in supplied ZIP.
- Claude/Canva/Cursor interconnection & Unity MCP cannot be assumed available from ChatGPT; run prompts in their respective tools with user-provided attachments.
