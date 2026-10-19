# CLAUDE R0 REVIEW — Reference Breakdown, Difference Matrix, R1 Asset Spec, Dual-Aspect Review

**Ngày:** 2026-10-19 · **Phạm vi:** chỉ phân tích, **không chỉnh sửa Unity project** · **Dừng trước khi sản xuất asset.**

## Bộ tài liệu R0

| File | Nội dung |
|---|---|
| `REFERENCE_SHOTLIST.csv` | 30 shot: clip, timecode, file khung, nhóm, cần lấy gì, palette đã đo, tỉ lệ, phần phải loại bỏ (watermark/phụ đề/editor) |
| `DIFFERENCE_MATRIX.csv` | 20 hạng mục: tham chiếu, ảnh, trạng thái Unity, sai khác, P0/P1/P2, giải pháp, Canva/Cursor, nghiệm thu, **điểm Portrait** và **điểm Landscape** riêng |
| `ASSET_MANIFEST_R1.csv` | Đặc tả 3 nhóm asset R1 (Ground, Tree, Slash VFX) bám theo inventory thật |
| `review/` | 3 ảnh tham chiếu để duyệt (mục 6) |
| `ref_frames/`, `contact_sheets/` | 45 khung gốc 576×1024 và 29 contact sheet 16 khung/clip |

> Thư mục `GameReference/R1_Rebuild/` là bản nháp trước đó (trước khi có `PROJECT_AUDIT.md` và `CURRENT_ASSET_INVENTORY.csv`). Bộ R0 này **thay thế** bản nháp đó. Tôi vẫn để `CLIP_CATALOG.md` cũ lại làm danh mục đủ 30 clip.

---

## 0. Đầu vào và giới hạn

| Đầu vào | Trạng thái |
|---|---|
| 30 video (ZIP) | Có, đã giải nén tại `Downloads/Telegram Desktop`. 29/30 đọc được |
| **Clip 21** | **NOT REVIEWED.** Chromium báo lỗi decode. Cần xuất lại sang H.264 |
| Reference Rebuild Plan Pack | **Không tìm thấy trên máy.** Số thứ tự clip giả định theo hậu tố tên file (`(N)`, file không số = 01). Mô tả các clip ưu tiên trong yêu cầu (04 nền rừng, 19 cây, 20/22 trại goblin, 03/06 hero + HUD, 12 đông quái, 18/26 telegraph) **khớp** với nội dung tôi thấy, nên cách đánh số nhiều khả năng giống bản Plan Pack |
| `PROJECT_AUDIT.md` | Có, đã đọc toàn bộ |
| `CURRENT_ASSET_INVENTORY.csv` | Có (`Docs/ReferenceRebuild/`, 140 dòng, có cả pivot). **Là nguồn kích thước cho manifest** |
| Baseline Unity | 18 ảnh trong `Assets/Screenshots/R0_Baseline/` |

Các giới hạn khác:
- Không có ffmpeg/Python. Khung hình được trích bằng HTML5 video trong trình duyệt tích hợp, sai số timecode khoảng ±0.1 s.
- Baseline `03_r_attack_combo` **không bắt được khung chém**, và `04_combat_goblins` gần như trùng `06` (chỉ có 1 goblin). Vì vậy số sát thương, combo và lưỡi liềm của Unity **chưa kiểm chứng được bằng ảnh**.
- Tất cả watermark TikTok, phụ đề chữ to, bình luận, giao diện Godot/Aseprite/Discord và bảng DAMAGE TEST đã bị **loại** khỏi đặc tả thiết kế (cột `exclude_from_design` trong shotlist).
- Lớp ánh sáng trong video (tia nắng ở C04, tông đêm ở C19) **không vẽ vào asset**. Palette chuẩn lấy theo ánh sáng ban ngày trung tính (C25, C22).
- Mã màu ghi **(sampled)** là lấy mẫu median 5×5 px từ khung video. Ghi **(đề xuất)** là màu tôi suy ra để hoàn thiện ramp màu.

Tỉ lệ pixel tham chiếu, đo trên Goblin ở C22 0:04.8: 1 px art ≈ 3.3 px video, nên vùng nhìn dọc ≈ **172×306 px art**.
- Hero ≈ 18×22 px. Unity: 16×20 trong ô 32×32 — khớp.
- Goblin ≈ 20×16. Unity: ô 26×22 — khớp.

---

## 1. Phân tích video — các clip ưu tiên

| Thuộc tính | **C04** nền rừng | **C19** cây/canopy | **C20** trại đêm | **C22** trại ngày | **C03** hero/combat | **C06** hero/HUD | **C12** đông quái |
|---|---|---|---|---|---|---|---|
| Timecode chính | 0:08.1, **0:09.5**, 0:11.0 | **0:08.4**, 0:02.3 | 0:13.7, 0:41.2, **0:46.7**, 0:57.7 | **0:04.8**, 0:08.0, 0:33.8, 0:37.0 | 0:19.2, 0:44.9, **0:53.4**, 0:57.7 | 0:28.9, **0:35.7**, 0:49.2 | **0:04.9**, 0:12.7 |
| Palette | Cỏ `#6CA27A` và đất `#E2AE81` (có lớp nắng) | Tán `#6A9F81`/`#4A727C`, thân `#70463B` (tông đêm) | Tông đêm xanh lục đậm | Cỏ `#71A184`, mảng tối `#5A665B`, mũ goblin `#4E3441`, mặt `#619B39` | Cỏ mint, lưỡi liềm cyan | Cỏ mint, đất cam, số vàng | Cỏ mint sáng, nhà mái tranh nâu |
| Sprite scale | Hero ≈ 18×22 | Cây ≈ 73×80 (tán ≈ 73×45, thân thấy ≈ 30) | Lều chỉ huy ≈ 105 px rộng | Lều ≈ 105×100, bát ≈ 52×60 | Số ≈ 12 px cao | Số vàng ≈ 12 px cao | Cây lớn ≈ 120 px rộng |
| Ground | Mảng cỏ có viền tối răng cưa phủ lên đất; đất tối dần sát mép; ngọn cỏ ngắn rải thưa | Mảng cỏ tối viền răng cưa | Mảng cỏ tối lớn quanh lều | Mảng tối quanh bát lửa, cỏ có ngọn | Mép đường đất răng cưa | Đường đất mép răng cưa ở trên | Cỏ mint, hoa tím/vàng |
| Silhouette nhân vật | Hero chibi tóc trắng, áo tối | Hero nhỏ, Dryad ở xa | Goblin có mũ trùm, Archer cầm cung, hero cầm kiếm chéo | Goblin tròn thấp, Gobrider to hơn | Hero quay lưng khi chém | Hero + Satyr | Dryad cưỡi nai đọc rõ |
| Sword animation | — | — | Kiếm dài khoảng 22 px, thấy rõ khi đứng | — | Thân xoay, kiếm vung theo cung | Thân xoay theo nhát chém | — |
| Slash VFX | — | — | Vệt mảnh | — | 2 lưỡi liềm cyan đối xứng (bản cũ); 0:57.7 lưỡi liềm lớn | **Lưỡi liềm trắng→cyan lớn** (0:49.2) | — |
| Mob label + HP | — | Nhãn nhỏ ở xa | Nhãn trắng; HP bar chỉ có ở quái đã bị đánh | Nhiều nhãn "Goblin Lv.3"; HP bar khi bị đánh | Có | Nhãn + HP bar viền tối, vạch sáng | **Nhãn luôn hiện, không có HP bar khi chưa bị đánh** |
| Damage numbers | — | — | Số vàng 266 (boss) | Số trắng 34 chồng lệch | Số trắng "58" rất to | **74 vàng (chí mạng) + 37 trắng** | — |
| Combo | — | — | "x1 HITS" | "x91 HITS" | "x14 HITS" bên phải mục tiêu | "x15 HITS" bên phải | — |
| Hotbar | Hàng 1·2·3·4·R·V·C·Z + ô F4/F5 phía trên trái | Hàng 1..Z | Hàng 2..Z, ô xám bo góc | Hàng 2·3·4·R·V·C | Hàng 1..F + 2 ô tiêu hao | Hàng 2..Z + thanh XP 22/30 | Hàng 2..Z |
| Camera | Top-down, hero ở giữa | Hero ở giữa | Hero ở giữa | Hero ở giữa | Hero ở giữa, có shake | Có thanh mục tiêu trên đỉnh | Hero ở giữa |

**Clip để dành giai đoạn sau:**
- C18 (vòng cam cảnh báo trong tutorial, nền gạch) và C26 (vòng đỏ AoE, băng chữ X, nứt đất, hotbar có cooldown) → giai đoạn Boss Telegraph. Đã có trong shotlist (S26–S28, trạng thái DEFERRED).

**Các clip còn lại:** tóm tắt trong `GameReference/R1_Rebuild/CLIP_CATALOG.md`. Phần lớn là tính năng tuỳ chọn (thời tiết, ngày/đêm, thu thập, nhà/ruộng, thị trấn), menu, hoặc footage editor và ngoài game.

**Kết luận phân tích:**
- Nhận diện hình ảnh của tham chiếu dựa trên 3 yếu tố:
  1. Nền cỏ bạc hà có mảng tối và mép răng cưa.
  2. Cây tán tròn 2 tông, viền tối.
  3. Lưỡi liềm trắng–cyan lớn.
- Đây đúng là 3 nhóm asset R1.
- HUD, label và quái là yếu tố thứ cấp.

---

## 2. Chấm điểm

Thang 0–10. **Không trừ điểm vì khác aspect ratio.**
- **Portrait visual:** chấm nội dung art so với tham chiếu dọc, không phụ thuộc khung hình. Unity chưa có profile dọc nên điểm phản ánh art hiện tại.
- **Landscape adaptation:** chấm mức độ bản 16:9 hiện tại thích nghi tốt, đối chiếu thêm bản PC ngang của chính tham chiếu (C15 0:26.1).

| Hạng mục | Trọng số | Portrait visual | Landscape adaptation | Ghi chú |
|---|---|---|---|---|
| Ground (cỏ + đất + mảng tối) | 25% | **3** | 6 | Sai tông, phẳng, cạnh vuông. Ở 16:9 diện tích nền còn lớn hơn |
| Tree / canopy | 15% | **5** | 6 | Dáng gần đúng, sai màu, có vết cắt khi mờ |
| Slash VFX R | 15% | **3** | 5 | Nhỏ khoảng 3 lần, scale không nguyên |
| Hero | 10% | 7 | 7 | Gần khớp |
| Goblin / Gobrider | 10% | 6 | 6 | Goblin khớp; Gobrider/Elite chỉ là tô màu |
| Mob label + HP bar | 5% | 8 | 8 | Giữ nguyên |
| Hotbar / HUD | 10% | 4 | 4 | Bố cục mobile (joystick, R tách riêng) cả trên PC |
| Camp props (lều, bát lửa) | 10% | 4 | 5 | Nhỏ hơn 2–2.5 lần |
| **Tổng có trọng số** | 100% | **≈ 4.5 / 10** | **≈ 5.8 / 10** | |

Camera landscape (480×270) đạt **8/10**: hero ≈ 7.4% chiều cao, tham chiếu dọc ≈ 7.2%. Mục này không tính vào tổng vì là thông số trình bày.

**Ước lượng nếu hoàn thành đủ R1** (3 nhóm Ground, Tree, Slash): Portrait visual có thể lên khoảng **6.5–7 / 10**, vì 55% trọng số nằm ở 3 nhóm này.

---

## 3. Điểm nhấn của Difference Matrix

**P0 (đều thuộc R1):**
- **D01** màu và texture cỏ.
- **D02** hình dạng mép đường đất.
- **D04** palette cây.
- **D05** vết cắt tán cây khi mờ.
- **D08** lưỡi liềm R.

**P1, làm cùng hoặc ngay sau R1:**
- D03 mảng cỏ tối, D06 bóng cây, D12 chụp lại baseline lúc trúng đòn, D14 hotbar theo profile, D18 camera profile dọc.

**Phát hiện cụ thể đã kiểm chứng trong code và asset:**
1. Mask đất là **4 bit theo cạnh** (Bắc=1, Đông=2, Nam=4, Tây=8, trong `CampLayoutPass`/`ForestTilemapBuilder`). Không có tile góc trong, nên vẽ lại 16 tile vẫn sẽ còn "khía" ở góc trong. Cần thêm 4 tile `DirtIC_*` cùng một thay đổi nhỏ trong builder (G06, P1).
2. `TreeCanopyOcclusion` cắt sprite theo **ngưỡng chiều cao** (≥75 px → 24 px thân; 65–74 → 20 px). Tán vẽ lấn xuống dải thân sẽ không mờ, gây vết cắt thẳng thấy ở `07_tree_occlusion.png`. Giải pháp là **quy tắc vẽ**, không cần sửa code (T01/T02).
3. `CombatVfx` phóng lưỡi liềm theo hệ số không nguyên (0.92→1.06, nhát cuối ×1.45, skill C ×1.7), làm méo pixel. Sprite R1 vẽ sẵn ở kích thước thật, chạy theo frame, scale = 1 (V01–V03 cần Cursor chỉnh phần hiển thị).
4. `GrassSoft.png` và `Dirt.png` dùng Sprite Mode **Multiple với 1 sprite** (`GrassSoft_0`, `Dirt_0`), và tile asset tham chiếu sprite theo tên. Khi thay ảnh phải **giữ tên sprite và GUID**.
5. `Flower.png`/`Pebble.png` là tile **trong suốt** (lớp trang trí). Không bắt buộc vẽ lại cùng nền cỏ.
6. Cây trên map đều là instance của prefab `Tree_Forest_A/B` (364 tham chiếu trong scene). Thay PNG giữ GUID là cập nhật toàn bộ map, không phải đụng scene.

---

## 4. Review kiến trúc — hai profile 9:16 và 16:9

### 4.1 Hiện trạng (theo audit + code)

- Chỉ có landscape. PPC `480×270`, PPU 16. `PortraitPixelSetup` chỉ đặt màu nền.
- **4 CanvasScaler gán cứng 1920×1080:**
  - `SkillHotbarUI.cs:179`
  - `LevelFlowUI.cs:265`
  - `BossHudUI.cs:86`
  - `WorldLabelsCanvas.cs:57`
- `LevelFlowUI.cs:225` so khớp **đúng** giá trị 1920×1080 để quyết định có dựng lại canvas. Đổi reference mà không sửa đoạn này có thể khiến UI bị dựng lại mỗi lần, hoặc giữ canvas sai.
- `EnemyWorldUI.cs:229` kẹp nhãn quái theo `Screen.height - 72f` (pixel cố định, không theo scale).
- `CameraFollow` tính `halfW` theo `_cam.aspect` (động), bounds và `KeepTargetOnScreen` tự thích nghi. `EnemySpawner.IsOnScreen` và `EnvironmentAmbience` cũng tính theo viewport/aspect. **Ba phần này không phải sửa.**
- Có `IncomingAssets/backup_before_r0_dualaspect/` (scene, prefab, level data) nhưng **chưa có code dual-aspect** (`git diff` trên `Assets/Scripts` so với `dca4cfc` trống).

### 4.2 Đề xuất

Một ScriptableObject `PresentationProfile`, chọn lúc chạy theo aspect màn hình (có override trong Editor). **Không tách scene**, không đụng gameplay.

| Thông số | Portrait 9:16 (chuẩn đối chiếu) | Landscape 16:9 (giữ hoạt động) |
|---|---|---|
| PPC reference | **180×320** (×6 ở 1080×1920, ×4 ở 720×1280; hero ≈ 6.9% chiều cao, tham chiếu ≈ 7.2%) | 480×270 (giữ nguyên) |
| CanvasScaler | 1080×1920, match = 0 (theo chiều rộng) | 1920×1080, match = 0.5 (giữ nguyên) |
| Hotbar | Một hàng dưới đáy 2·3·4·R·V·C, R cùng cỡ (C22 0:04.8) | Hàng nhỏ giữa đáy (C15 0:26.1). Joystick chỉ hiện khi có touch |
| HUD trên | HP trái trên, thanh boss/mục tiêu rộng toàn màn | Giữ |
| Camera bounds | Dùng bounds level hiện có. Kiểm tra mép trên/dưới vì vùng nhìn dọc cao 20 đơn vị | Giữ |
| Label clamp | Margin theo `scaleFactor` thay cho 72 px cố định | Giữ |

**Phản biện đề xuất trong audit (PPC dọc 270×480):**
- Ở 270×480, hero chỉ chiếm khoảng 4.6% chiều cao, nhỏ hơn tham chiếu khoảng 1.6 lần.
- Màn hình sẽ thấy 30 đơn vị chiều dọc, trong khi bounds các map hiện cao khoảng 26 đơn vị, nên lộ mép map.
- **Đề xuất 180×320** (hoặc 162×288 nếu muốn sát tham chiếu hơn, nhưng scale không nguyên ở 1080 px).

**Thứ tự an toàn:**
1. Đổi CanvasScaler sang đọc từ profile (sửa cả phép so khớp ở `LevelFlowUI.cs:225`).
2. PPC theo profile.
3. Hotbar 2 layout.
4. Label clamp.
5. Chụp đủ ma trận test.

R1 art **không phụ thuộc** dual-aspect và có thể làm song song.

### 4.3 Phần phải giữ và cách bảo vệ

| Phải giữ | R1 có đụng không | Cách bảo vệ |
|---|---|---|
| Level 1–4 (cùng map, collider, spawn) | Chỉ đổi pixel tile/cây | Giữ GUID, tên sprite, kích thước canvas cây. Không chạy lại builder |
| LevelFlow | Không | Profile chỉ đổi scaler. Sửa so khớp 1920×1080 cẩn thận |
| GameProgress | Không | — |
| Enemy AI | Không | — |
| Combat (R + mọi skill) | Chỉ phần hiển thị lưỡi liềm | Không đổi `slashImpactDelay`, damage, radius, hitstop, shake, chuỗi 3 nhát |
| Mob Labels + Enemy HP Bars | Không | Giữ luôn hiện (D10) |
| Tree Occlusion | Đổi sprite cây | Quy tắc dải thân 24/20 px; giữ pivot (0.5,0) và kích thước 62×78 / 54×68 |
| Sprite Sorting (+4000 − y×100) | Không | Pivot cây đáy-giữa; shadow có order thấp hơn thân; tile không qua SpriteSorter |
| PPU 16 / Point / Uncompressed | Có (asset mới) | Cột `unity_import` trong manifest |

### 4.4 Ma trận test cần có trước khi duyệt R1

- **Độ phân giải:** 1920×1080, 1280×720 (landscape); 1080×1920, 720×1280 (portrait, khi có profile).
- **Ảnh cần chụp mỗi cấu hình:**
  1. Hero idle trên đường đất.
  2. Hero đứng sau cây (tán mờ).
  3. Đúng frame chém R nhát 1 và nhát 3.
  4. Trúng đòn có số sát thương + combo (thiếu trong R0).
  5. Đông quái L4.
- Chạy lại `SoakTestRunner` 5 phút sau khi đổi VFX để xác nhận pool không tăng.
- Commit Git trước mỗi bước. Repo đã có tại `dca4cfc`; hiện có nhiều file R0 chưa track.

---

## 5. Ghi chú cho ASSET_MANIFEST_R1

- **Ground:**
  - Thay pixel cho `GrassSoft`, `GrassDark`, `Dirt`, `DirtM0–15` (16×16, PPU 16, pivot 0.5/0.5, đục).
  - Mới: `GrassSoft_V1/V2` và `DirtIC_*` (tuỳ chọn, cần Cursor).
- **Tree:**
  - Thay `Tree_Forest_A` (62×78, dải thân 24) và `Tree_Forest_B` (54×68, dải thân 20), pivot (0.5,0).
  - Thân/tán vẽ thành layer riêng làm file nguồn, nhưng **chỉ import bản ghép**.
  - Bóng mới `Tree_Shadow_A/B` (56×14, 48×12, theo cỡ `Shadow_Large` 48×12).
  - **Không vẽ viền tím.**
- **Slash:**
  - Mới `VFX_SwordSlash_A/B` (4 frame 96×96) và `_C` (4 frame 128×128), pivot ở tâm cung phía lõm, vẽ hướng chém sang phải.
  - Kích thước suy từ tham chiếu (≈86×91 px art). Ô 96 = 3 lần ô hero 32 px trong inventory.
  - Cần Cursor đổi `CombatVfx` sang chạy frame.

---

## 6. Ba ảnh tham chiếu quan trọng nhất để duyệt

| # | Ảnh | Dùng để duyệt |
|---|---|---|
| 1 | `review/REVIEW_1_ground_C04_0m09s5.png` — **Clip 04 @ 0:09.5** | Ground: mảng cỏ, viền tối răng cưa phủ lên đất, đất tối dần sát mép, ngọn cỏ. Lưu ý: lớp nắng làm sáng màu; đối chiếu màu với C25 0:02.9 |
| 2 | `review/REVIEW_2_tree_canopy_C19_0m08s4.png` — **Clip 19 @ 0:08.4** | Tree: tán tròn mép vảy 2 tông, viền tối, thân rễ loe, bóng gốc. Cỡ ≈ 73×80 px, khớp canvas 62×78. Lưu ý: tông đêm; đối chiếu màu với C25 0:02.9 |
| 3 | `review/REVIEW_3_slash_R_C25_0m02s9.png` — **Clip 25 @ 0:02.9** | Slash R: lưỡi liềm lõi trắng đặc + mép cyan, ≈ 86×91 px art, hero ở phía lõm. Khung sạch nhất (C06 0:49.2 bị phụ đề che) |

Mỗi ảnh gồm khung gốc (bên trái, khung vàng đánh dấu vùng) và vùng phóng ×2 (bên phải).

---

## 7. Điểm DỪNG — cần bạn quyết định trước R1

1. **Duyệt 3 ảnh tham chiếu** ở mục 6.
2. **Có làm tile góc trong (`DirtIC_*`) ngay trong R1 không?** Nếu có, cần Cursor sửa nhỏ trong builder và bake lại map. Nếu không, chấp nhận còn khía ở góc trong.
3. **PPC portrait 180×320** (đề xuất) hay 270×480 (theo audit)?
4. **Clip 21:** gửi lại bản H.264 nếu cần đánh giá.
5. Gửi Plan Pack nếu có, để đối chiếu số thứ tự clip.

Không có asset nào được sản xuất. Không có file nào trong `Assets/` hay scene bị thay đổi.
