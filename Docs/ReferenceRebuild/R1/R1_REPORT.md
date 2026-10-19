# R1 REPORT — Ground, Tree, Sword Slash

**Ngày:** 2026-10-19 · **Đầu vào đã duyệt:**
- 3 ảnh tham chiếu trong `R0/review/`.
- PPC portrait 180×320.
- Làm luôn tile góc trong `DirtIC_*`.

## Kết quả

| Nhóm | Trạng thái | Bằng chứng |
|---|---|---|
| Ground (cỏ, đất, mép, góc trong) | ✅ Áp dụng cho cả 5 map | `compare/CMP_01`, `CMP_02`. Màu đo trên ảnh 1920×1080: cỏ **#74A587** (mục tiêu #74A587), đất **#D59C6A** (mục tiêu #D59C6B) |
| Tree (thân, tán, bóng) | ✅ 70 cây trên 5 map | `compare/CMP_01`, `CMP_03`. 0 pixel tán trong dải thân (đã kiểm bằng script). Khi tán mờ không còn vết cắt thẳng hay đoạn thân bị mờ |
| Sword Slash VFX | ✅ R chuỗi 3 nhát + skill C | `compare/CMP_04` (dàn cảnh frame 1). Có lưỡi liềm `VFX_SwordSlash_C` trong pool ở combat thật. Scale = 1, pixel không méo |
| Console | ✅ 0 lỗi, 0 cảnh báo | — |
| Soak 5 phút (L4) | ✅ Ổn định | `Logs/soak_20261019_155251.csv`: p99 trung bình 2.7 ms, frame tệ nhất 76 ms; texture 1413→1420 (tăng lúc tải đầu rồi đứng yên); object 699→703; VFX tạo 20 / trả pool 20. **Giới hạn:** chỉ có tải combat trong khoảng 40 s đầu (4 wave L4 → 21 kill), sau đó hết quái vì L4 chạy theo LevelFlow, không theo chế độ vô tận của spawner |

## Asset đã tạo hoặc thay

Tất cả sinh bằng `Assets/Editor/R1ArtGenerator.cs` (menu **Tools → Gameplay Prototype → R1**), palette theo `R0/ASSET_MANIFEST_R1.csv`.
- **File cũ:** ghi đè pixel, giữ nguyên GUID, `.meta`, tên sprite (`GrassSoft_0`, `Dirt_0`…) và kích thước.
- **File mới:** PPU 16, Point, không nén, không mipmap, Full Rect.

| File | Loại | Kích thước | Ghi chú |
|---|---|---|---|
| `Tiles/GrassSoft.png` | thay | 16×16 | Nền chính, có ngọn cỏ chữ V |
| `Tiles/GrassSoft_V1.png`, `_V2.png` (+ Tile) | mới | 16×16 | Rải ngẫu nhiên 15% / 15% để phá lặp |
| `Tiles/GrassDark.png` | thay | 16×16 | **Lệch manifest:** dùng `#62907A` thay vì `#5A665B`. Mảng cỏ tối hiện vẫn là ô vuông, màu quá tương phản sẽ lộ các ô vuông. Mảng tối có mép răng cưa (D03) để giai đoạn sau |
| `Tiles/Dirt.png`, `DirtM0–15.png` | thay | 16×16 | Mép cỏ răng cưa sâu 2–6 px, viền tối 1 px, bóng đất. Đầu mép cố định 3 px để các tile nối liền |
| `Tiles/DirtIC_*.png` (+ Tile) | mới | 16×16 | **15 tile** góc trong, trong suốt: 4 góc đơn `DirtIC_NE/SE/SW/NW` + 11 tổ hợp, để ô có nhiều góc cùng lúc vẫn đúng |
| `GoblinCampPhase4/Tree_Forest_A.png` | thay | 62×78 | Dải thân 24 px. Tán 2 tông có đường vảy, viền tối, không viền tím |
| `GoblinCampPhase4/Tree_Forest_B.png` | thay | 54×68 | Dải thân 20 px |
| `GoblinCampPhase4/Tree_Shadow_A.png`, `_B.png` | mới | 56×14, 48×12 | **Lệch manifest:** bán trong suốt (`#1E2C28`, alpha 110) giống `Shadow_Large` hiện có, thay vì màu đặc, để đồng bộ với bóng các prop khác |
| `Resources/VFX/VFX_SwordSlash_A.png`, `_B.png` | mới | 384×96 (4 × 96×96) | Pivot (12, 48) = tâm cung. A quét xuống, B quét lên. **Lệch manifest:** nằm trong `Resources/VFX` (không phải `Art/VFX`) để tải lúc chạy, không cần gán trong scene |
| `Resources/VFX/VFX_SwordSlash_C.png` | mới | 512×128 (4 × 128×128) | Nhát cuối + skill C, có vệt cung phụ bên trong |

## Thay đổi code và scene

| File | Thay đổi | Không đổi |
|---|---|---|
| `Scripts/FX/CombatVfx.cs` | Thêm `SpawnSwordSlashFrames()`: chạy 4 frame, scale 1, hướng làm tròn theo 8 hướng, tự lật khi chém sang trái, dùng chung pool. Thiếu sprite thì trả `false` | Bản lưỡi liềm vẽ bằng code vẫn còn làm dự phòng; các VFX khác giữ nguyên |
| `Scripts/Combat/PlayerCombat.cs` | Ghi lại bước chuỗi (`_pendingSlashVariant`): nhát 1 → A, 2 → B, 3 → C. Skill C dùng C | Timing (`slashImpactDelay`), damage, radius, knockback, hitstop, shake, buffer, auto-aim |
| `Editor/R1MapPass.cs` (mới) | Pass trên scene: tính lại mask đất 4 bit cho mọi ô đất, đặt `DirtIC` lên tilemap mới `DirtCorners` (order = Ground + 1), rải biến thể cỏ, gán bóng cây mới cho `ContactShadow` | Không đổi ô nào từ đất sang cỏ hay ngược lại. Không đụng collider, spawn, bounds, LevelConfig |
| `Scenes/GameplayPrototype_Phase4Test.unity` | Đã chạy pass và lưu: Camp 39 ô đổi mask + 14 góc trong; Trail 56 + 17; AbandonedCamp 62 + 18; GoblinOutpost 86 + 22; GobriderStronghold 88 + 18. Gán 70 bóng cây | — |

**Lỗi sửa kèm:** L4 và L5 trước đây bake toàn bộ mép đường bằng `DirtM2` (mép cỏ phía Đông), gây ra các vạch cỏ dọc trong baseline `06`. Pass đã tính lại mask đúng (`compare/CMP_02`).

## Đã giữ nguyên (theo yêu cầu R0)

- Level 1–4 (và 5), LevelFlow, GameProgress.
- Enemy AI.
- Combat: R và mọi skill.
- Mob Labels và Enemy HP Bars.
- Tree Occlusion: vẫn tách theo ngưỡng chiều cao, art đã tuân thủ dải thân.
- Sprite Sorting (+4000 − y×100): pivot cây vẫn (0.5, 0); tile không đi qua SpriteSorter.

## Còn tồn tại / bước tiếp theo

1. **PPC portrait 180×320:** đã ghi nhận quyết định, **chưa triển khai**. Thuộc hạng mục profile 9:16 (sửa 4 CanvasScaler cố định 1920×1080, `LevelFlowUI.cs:225`, label clamp, hotbar 2 layout). R1 art không phụ thuộc vào phần này.
2. **Builder cũ** (`CampLayoutPass`, `ForestTilemapBuilder`) chưa biết `DirtIC`. Nếu bake lại map bằng các builder này, phải chạy lại **R1 → Apply Ground + Tree Pass**.
3. **Mảng cỏ tối** vẫn là ô vuông (D03, P1). Cần thêm bộ viền răng cưa cho cỏ tối, làm giống `DirtIC`.
4. **Bụi cây** vẫn palette xanh đậm cũ, lệch tông với nền mới (D06, P1). Chỉ cần đổi màu 5 sprite.
5. **Lưỡi liềm lớn hơn vùng gây sát thương:** nhát A phủ khoảng 1.2–2.5 đơn vị trước hero, trong khi vùng đánh R tối đa khoảng 1.7 đơn vị. Combat được giữ nguyên theo yêu cầu. Nếu muốn khớp hơn, chỉnh `SlashPullBack` trong `CombatVfx`, hoặc nới `slashRadius` (thuộc phần cân bằng).
6. Lặp tile cỏ vẫn hơi thấy ở diện rộng. Có thể thêm biến thể V3/V4 hoặc giảm ngọn cỏ trên tile gốc.
7. Mép map (dải tối) vẫn thấy khi camera ra sát biên (D19, ngoài R1).

## Ghi chú về scene

Mọi thay đổi R1 đã được lưu vào `GameplayPrototype_Phase4Test.unity` (đã kiểm: 5 tilemap `DirtCorners`, 70 tham chiếu bóng cây mới, các tile mới). Sau khi thoát Play Mode, Editor vẫn báo scene "chưa lưu" vì một thay đổi trong bộ nhớ không do R1 tạo ra. **Trạng thái đó không được lưu.** Nếu Unity hỏi khi đóng, nên chọn *Don't Save*, trừ khi bạn biết đó là thay đổi của mình.

## Khôi phục

- **Sao lưu trước R1:** `IncomingAssets/backup_before_R1/`, gồm tile + `.meta`, 2 cây + `.meta`, `CombatVfx.cs`, `PlayerCombat.cs`, scene.
- **Git:** các file tracked có thể quay về commit `dca4cfc` (R0 checkpoint). Thay đổi R1 **chưa commit**.
