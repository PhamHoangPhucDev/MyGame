# TÁI THIẾT KẾ GAME THEO VIDEO THAM CHIẾU — MASTER PLAN

Ngày phân tích: 2026-10-19. Trạng thái Unity do người dùng/Cursor báo cáo: Phase 12D.1 đã hoàn thành; Phase 12E tạm dừng; landscape 16:9, URP Pixel Perfect 480×270, PPU16, Level 1–4, HUD R bottom-right, skill 2/3/4/V/C bottom-center, mob labels phải giữ lại.

## 1. Dữ liệu tham chiếu đã kiểm tra
- ZIP chứa 30 file .mp4, trong đó 29 file có thể giải mã; clip số 21 lỗi `moov atom not found`.
- Tổng thời lượng 29 clip: khoảng 1248,3 giây (~20,8 phút); mỗi clip giải mã được có khung 576×1024 (9:16), 30fps và track âm thanh.
- Đây là video có phụ đề/TikTok overlay/shot biên tập và một số footage editor/menus. KHÔNG coi chữ overlay, watermark, black letterbox hoặc màn hình editor là art của game.
- Các clip là nhiều tính năng và cảnh quan khác nhau, không phải một sequence liên tục. Cần chọn mẫu gameplay canonical trước khi chốt đặc tả.
- Bản phân tích hình ảnh dựa trên nhiều frame từ tất cả 29 clip giải mã được. Âm thanh chưa được thẩm định bằng nghe trực tiếp nên không có danh mục SFX chi tiết dựa trên tiếng nghe.

## 2. Phong cách lõi quan sát được
1. Camera top-down 2D với nhân vật tóc trắng, trang phục xanh đậm; thân/sprite có độ tương phản cao, nhân vật được giữ trong khu vực nhìn thấy khi camera cuộn.
2. Rừng fantasy với cỏ xanh lam nhạt có variation **nhuyễn, tự nhiên**, đường đất màu be/cam mềm, đường viền pixel không đều, cây nhiều lớp, bóng đổ trên đất, bụi/cỏ/hoa/đá nhỏ rải tiết chế.
3. Mob như Goblin Lv.3, Gobrider Lv.5 và các boss khác đều dùng label tên/cấp độ dễ thấy và HP bar; nhãn hiện diện khi combat.
4. Combat liên hoàn: vệt chém xanh-trắng, hit spark, số damage lớn nổi, xN HITS, phép lửa/vòng cảnh báo AoE, phản ứng vật lý khi bị đánh.
5. Hotbar Pixel Art viền tối với icon màu sắc, cooldown số, dải phím ở đáy; trên bản người dùng yêu cầu R phải lớn ở góc phải.
6. Các hệ thống bổ sung trong các clip: 3 cấp độ mưa; custom nhân vật; menu kỹ năng/passive/gem; town NPC, building; boss telegraph nhiều pha.

## 3. Khác biệt với game hiện tại (theo báo cáo và ảnh/video các lượt trước)
| Cụm | Hiện tại | Mẫu tham chiếu | Chênh lệch quan trọng |
|---|---|---|---|
| Khung | 16:9 ngang | 9:16 dọc | Cực lớn đối với bố cục/hotbar/camera; giữ 16:9 làm bản bảo toàn và tạo chế độ portrait thử nghiệm |
| Ground | Nền cỏ đất đã cải thiện nhưng đơn giản | Ground chuyển tiếp organic, nhiều details nhỏ, bóng môi trường | P0 hình ảnh |
| Hero | Có thân/kiếm; từng lỗi sorting | Hero tóc trắng nổi bật, di chuyển/đánh nhiều frame, định hướng rõ | P0 sprite/animation |
| Mobs | Goblin/Gobrider, labels đã có | Nhiều mob và boss có silhouette/hit reaction khác biệt | P1 |
| VFX | Có combo/slash, hitstop, knockback | Combo rõ, nhiều spell tròn/đường thẳng, damage đa lớp | P0 combat feel |
| UI | R lớn cuối phải, 5 skill giữa | Hotbar pixel frame phủ gần đáy 9:16, timer/icon nổi | P1 art & layout |
| Meta | L1–4; L5 đang dừng | Town, quest, skill/gem, customization, thời tiết, boss events | P2 sau core |

## 4. Ưu tiên kỹ thuật — không làm đồng thời

### R0. Chốt canonical reference, đo baseline và khóa checkpoint
- Branch/tag: `reference_rebuild_r0` hoặc checkpoint do Cursor chọn.
- Đối chiếu 12 ảnh hiện tại (L1–4, hero idle/walk/R, 5 skill, boss, HUD, occlusion) với các frame ref cùng ngữ cảnh.
- Chọn tập clip canonical cho mỗi hệ thống: gameplay thường `03,06,08,11,12,20,22,26`; environment `01,04,10,19,24,27,28`; UI/meta `07,09,14,25`; weather `02`.
- Chốt tỷ lệ đầu ra: nếu ưu tiên 90% bố cục tương tự video, mode portrait 9:16 là bắt buộc; landscape 16:9 phải được đánh giá theo gameplay/asset/UX chứ không thể pixel-match bố cục.
- Tạo shot comparison template tại cùng vùng nội dung và chuẩn scale, bỏ phụ đề/watermark.

### R1. Rebuild Environment & Tiles
- Sửa cỏ checker/sọc/tint, edge dirt; base seamless 16×16 theo grid/PPU hiện có; tạo tile góc, mép trong/ngoài, trang trí grass/flowers/rocks và shadow.
- Thiết kế cây theo tách trunk/canopy và fade che khuất, không làm mờ toàn cây; giữ collider gốc, SortingGroup ổn.
- Duyệt ảnh cảnh rừng không chiến đấu trước khi triển khai toàn L1–4.

### R2. Hero & Monster Visual Fidelity
- Lock character proportions: xác định sprite rect, silhouette từ file gốc dự án và video; hero tóc trắng áo xanh, chuyển động idle/walk/attack/hurt/death, 4/8 hướng tùy art hiện có.
- Chuyển sword thành attachment dựa trên animation state, order tương ứng Hero và world/local transform nhất quán.
- Giữ label tên+cấp độ mọi mob; collision-aware placement, HP below name, không auto-hide.

### R3. Combat/VFX Fidelity
- Chuỗi R 3 hit, target selection, giữ-phím liên tục, knockback/stagger, hitstop không nhân chồng với AoE, vệt chém xanh-trắng đồng bộ từng frame.
- Damage numbers phân biệt normal/critical, cap số popup nhưng không làm mất hit, xN HITS không che mục tiêu.
- Skill 2/3/4/V/C theo chức năng đã có, VFX riêng, telegraph đúng timeline, pool animation/VFX.

### R4. HUD Pixel Art & Aspect Modes
- Thiết kế một bộ HUD prefab có profile portrait và landscape, nhưng không phá layout R-right + 5 skills-center đã duyệt.
- Đúng icon, cooldown radial/overlay và số, pixel font hợp lệ, safe area, đa resolution, multitouch.
- Bố cục 9:16 đối chiếu trực tiếp video; 16:9 là phiên bản adapted có thể dùng song song nếu người dùng duyệt.

### R5. Enemy Content, Weather, Boss & RPG Meta
- Thêm mob/thế giới chỉ sau khi R1–R4 ổn; boss charge/sweep/AoE, 3-level rain, town, skill/gem theo phân kỳ sản phẩm.
- Phase 12E Level5 phải dựa trên Boss spec mới và không làm trước khi người dùng xác nhận hướng art/combat.

## 5. Công việc chia theo công cụ

### Claude Desktop / Claude ở Chrome — Research/Audit (không tự nhận đã có MCP)
Đầu vào: original ZIP; 3 reference sheets; current Unity screenshots; `LevelFlow`, `PlayerCombat`, `EnemyController`, `SpriteSorter`, camera, hero prefabs và asset index do Cursor xuất.
Đầu ra: `REFERENCE_BREAKDOWN.md` gồm clip/timecode/feature; `DIFFERENCE_MATRIX.csv` gồm severity; `ASSET_MANIFEST.csv`; review Pull Request của Cursor với checklist. Nếu Claude không truy cập Unity MCP, chỉ phân tích file/report nhận được và ghi `NOT VERIFIED`.

### Canva — Art concept + asset export
Đầu vào: frame crop không có overlay TikTok, palette trích từ asset hiện có, specification sprite size, grid, pivot.
Đầu ra: hero concept (không thay copyright assets khi thiếu quyền), transparent PNG tree/props/icon/ground mockup, UI popup concept. Ảnh Canva không bảo đảm pixel-perfect/spritesheet; phải được pixel-cleanup ở pixel editor trước khi nhập Unity; không để Canva tự vẽ text lên tile hay xuất JPEG có nền.

### Cursor Agent + Unity MCP — Implementation
Đầu vào: Claude audit + approved art; repo Git Unity.
Đầu ra: Unity asset imports (Point, no compression hoặc cấu hình phù hợp, PPU16), animations, tile palettes, scenes/prefabs, tests, screenshots, frame-by-frame side-by-side baseline; COMPILE PASS và runtime PASS mới được merge.

### Người duyệt
Duyệt một feature/shot mỗi lần: forest clearing → hero 3-hit R → 5 mobs AoE → HUD portrait → boss; không merge 100 asset cùng lúc.

## 6. Gates nghiệm thu định lượng (mục tiêu, không bảo đảm)
- R1: Map không có checkerboard rõ, dirt edges liền mạch, Hero/Enemy nhìn rõ trong 30s gameplay, không hiện mặt dưới Ground.
- R2: 100% frame Hero sống, opacity 1 khi không fade/hurt; sword đồng bộ; 0 frame Hero mất thân trong bài test 60s.
- R3: Attack input hold trong 10s không mất nhịp; hitbox đồng bộ visual, xN HITS chỉ tính hit hợp lệ, 0 enemies kẹt lâu trong test đã định.
- R4: 3 độ phân giải 1920×1080 / 1600×900 / 1280×720 và 576×1024 nếu chọn portrait, UI không bị clip, tất cả mob labels giữ nguyên.
- R5: Không hồi quy L1–4, save/retry/exit gate, 0 console error mới; kết quả không test ghi `NOT TESTED`.
- Chấm độ gần ref theo rubrics có trọng số: ART/scene 30%, hero/mob 20%, combat/VFX 25%, HUD/camera 15%, interaction/game loop 10%; chỉ đánh giá trên scene tương ứng, không gọi kết quả là 90–100% nếu chưa so sánh và người dùng duyệt.

## 7. Cấu trúc repo đề xuất
- `Assets/GameReference/ReferenceAudit/` — bảng CSV/MD và thumbnail được cho phép.
- `Assets/Art/Rebuild/Environment/`, `Characters/`, `VFX/`, `UI/` — asset bản thử nghiệm, không ghi đè ảnh gốc trước review.
- `Assets/Data/VisualProfiles/` — cấu hình profile art/UI và portrait/landscape nếu có.
- `Assets/Screenshots/ReferenceCompare/` — ảnh đối chiếu gameplay.
- `Docs/ReferenceRebuild/` — changelog, asset manifest, test results.

## 8. Việc đầu tiên yêu cầu Cursor
1. Giữ trạng thái Phase12E paused, khóa backup/tag Git.
2. Chạy inventory read-only project: scenes, spritesheets, animations, import settings, tilesets, scripts, existing MCP tests.
3. Xuất 12 ảnh current build có time/scene/state; **không sửa file**.
4. Claude dùng ảnh + reference pack để viết matrix P0/P1/P2.
5. Sau khi người dùng chốt aspect và duyệt sample art, mới cho Canva sản xuất 3 asset mẫu: grass/dirt transition, tree canopy, hero attack slash.
6. Cursor import vào một Scene `VisualSandbox` riêng; tạo video 15–30 giây so sánh, duyệt rồi triển khai đại trà.

## 9. Quyền sử dụng & giới hạn
- Video tham chiếu là nguồn để phân tích thiết kế. Nếu không sở hữu quyền từ chủ game, tránh phát hành sprite/UI/audio nguyên bản được sao chép nguyên xi; dùng asset do đội tự tạo có cảm giác tương đồng.
- Không khẳng định đạt 90–100% trước khi đối chiếu; 9:16 và 16:9 là hai bố cục khác nhau.
- Clip 21 hỏng, cần bản khác nếu có nội dung quan trọng.
