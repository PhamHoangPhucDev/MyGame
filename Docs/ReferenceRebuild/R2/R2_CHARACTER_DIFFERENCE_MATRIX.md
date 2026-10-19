# R2A — CHARACTER DIFFERENCE MATRIX (Hero · Goblin · Gobrider)

**Ngày:** 2026-10-19 · **Nhánh Unity đọc:** `reference_rebuild_r1` (commit `06edf43` + thay đổi chưa commit) · **Không sửa project.**

Tài liệu đi kèm:
- `R2_HERO_SPRITE_SPEC.md`, `R2_GOBLIN_SPRITE_SPEC.md`, `R2_GOBRIDER_SPRITE_SPEC.md`
- `R2_ANIMATION_SPEC.csv`
- `R2_CANVA_ASSET_MANIFEST.csv` (chỉ Hero)

Ảnh tham chiếu đã cắt: `R2/ref_strips/`.

## 0. Phương pháp và giới hạn

- **Reference Plan Pack:** `Downloads/R0_R1_Dual_Aspect_Handoff.zip` (gồm `00_R0_R1_PLAN.md`, `reference_clips_index.csv`, `reference_sheet_1–3.jpg`). Đã đối chiếu: số thứ tự clip trong Plan Pack **khớp** cách đánh số dùng trong tài liệu này. `reference_clips_index.csv` giống hệt bản trong `Docs/ReferenceRebuild/`.

- **Nguồn video:** 29/30 clip (Clip 21 vẫn NOT REVIEWED, MP4 lỗi `moov atom`).
- **Cách lấy khung:** trích bằng HTML5 video trong trình duyệt tích hợp, đúng từng khung 30 fps (`strip_*.png`, mỗi ô ghi `C<clip> <giây> #<thứ tự>`).
- **Đếm frame animation:** đo bằng chênh lệch điểm ảnh giữa các khung video liên tiếp trong vùng nhân vật (`diff_C05_idle.json`). Chỉ áp dụng khi camera đứng yên. Ở các clip camera di chuyển hoặc có hiệu ứng che, số frame ghi **NOT MEASURED**, không ước lượng.
- **Đo kích thước:** 1 px art ≈ 3.3 px video (đo trên Goblin ở C22 0:04.8, R0). Kích thước nhân vật tham chiếu sai số ±2 px art vì video nén và có phóng.
- **Spritesheet Unity:** đọc trực tiếp PNG và `.meta` (`Hero_Sheet.png` 128×288, `Goblin_Sheet.png` 104×88, `Gobrider_Sheet.png` 120×96).
- **Loại khỏi thiết kế:** watermark, phụ đề, chữ "Souls", "You have died", giao diện editor.

---

## 1. Kiểm tra R1 (dựa trên bằng chứng)

Nguồn: `R1/R1_SANDBOX_RESULTS.md`, 7 ảnh trong `R1/Screenshots/`, `Assets/Data/AspectProfiles/*.asset`, `git log`.

| Hạng mục | Báo cáo R1 nói | Bằng chứng kiểm được | Kết luận |
|---|---|---|---|
| Chạy 16:9 | PASS | 4 ảnh 1920×1080 là ảnh chụp thật trong Play Mode | **ĐẠT** |
| Chạy 9:16 | PASS "framing QA" | 3 ảnh `R1_port_*.png` có kích thước 576×1024, nhưng **nội dung là ảnh ngang bị ép**: vùng chơi rộng ≈283 px ≈ 896 px (vùng chơi ảnh ngang) × 0.30; cây hẹp và cao bất thường. Báo cáo cũng ghi Game View vẫn là 1920×1080 khi chụp. Profile `Portrait_9x16.asset` có `pixelPerfectRef 144×256` (đúng giá trị đã chốt) | **CHƯA KIỂM CHỨNG.** Không có ảnh portrait hợp lệ |
| PPC portrait | 144×256 | File asset ghi 144×256 | Cursor làm **đúng Plan Pack** (`00_R0_R1_PLAN.md` đề xuất 144×256), nhưng **lệch quyết định duyệt sau** (180×320). **Đã chốt 2026-10-19: 144×256 theo Plan Pack** → mục này ĐẠT về cấu hình |
| Ground | PARTIAL | Ảnh ngang: còn thấy rõ ô vuông GrassDark rải kiểu bàn cờ; mép phải đường đất có "răng" lặp đều theo từng tile | **CHƯA ĐẠT** |
| Tree | PASS (α 0.4) | `R1_land_under_canopy.png`: tán mờ đúng, thân không mờ | **ĐẠT** |
| Slash | PASS | `R1_land_hud.png` / `under_canopy`: lưỡi liềm lõi trắng, mép cyan, đúng hướng | **ĐẠT** về hình. Lưỡi liềm lớn hơn vùng gây sát thương (đã ghi ở R1) |
| Camera | Hero/viewport 0.1185 (L) | Số này dùng **hộp ô 32 px**. Thân thật cao 20 px → 20/270 = **0.074**, khớp tham chiếu ≈0.072. Portrait 20/256 = 0.078 (với 144×256) | **Tỉ lệ đúng**, nhưng cách đo trong báo cáo dễ gây hiểu nhầm |
| Mép map | Không nêu | Ảnh ngang: sandbox chỉ rộng ≈14 đơn vị, hai bên lộ nền xanh đậm | Lỗi trình bày của sandbox (P1) |
| Video 20–30 s | NOT TESTED | — | Thiếu |
| Victory / GameOver / Retry | NOT TESTED | — | Thiếu |

**Kết luận R1:**
- Đạt: landscape, Tree, Slash.
- **Chưa đạt hoặc chưa kiểm chứng:** portrait 9:16 (không có ảnh hợp lệ), Ground (bàn cờ + răng lặp).
- **Cần làm trước khi duyệt R1:**
  1. Chụp lại portrait thật: Game View 1080×1920 hoặc 576×1024 với `Screen` thay đổi thật.
  2. ~~Đổi `Portrait_9x16.asset` sang 180×320~~ — đã chốt giữ 144×256 (Plan Pack).
  3. Bỏ rải GrassDark theo ô rời, hoặc chỉ rải thành cụm.

---

## 2. Ma trận khác biệt nhân vật

Mã màu tham chiếu: lấy mẫu từ video (median hoặc histogram). Mã màu Unity: đọc trực tiếp từ PNG.

### Hero

| ID | Hạng mục | Tham chiếu (clip / timecode) | Unity hiện tại | Sai khác | Mức | Hướng sửa |
|---|---|---|---|---|---|---|
| H01 | **Palette áo** | C05 0:01.0–2.3 (`strip_C05_hero_idle_1s`) | Áo `#2C4062` (navy tím), viền `#2A2032` | Tham chiếu là xanh lam ngả teal: `#3C546C` chủ đạo, sáng `#60909C`, tối `#303C48`. Unity tím và tối hơn rõ | **P0** | Đổi ramp áo sang teal (spec H §3) |
| H02 | **Tóc** | C05 0:01.0; C22 0:08.0 | Tóc `#F2F2FA` + bóng `#C6C9E4` (ngả tím) | Tham chiếu: trắng `#FCFCFC`, bóng xám trung tính `#D8D8D8`. Tóc dài phủ qua vai, mái ngố, vuốt nhọn hai bên ngang tai | **P0** | Vẽ lại khối tóc; bóng xám trung tính |
| H03 | **Kiếm đeo sau lưng khi nghỉ** | C05 0:01.0 (chuôi cam nhô bên phải đầu); C22 0:08.0 (kiếm dựng cạnh đầu) | `Sword.png` 6×16 placeholder, `RestSword()` đặt nghiêng sau lưng | Tham chiếu: chuôi/cán cam–nâu `#E09A52` thấy rõ cạnh đầu; lưỡi xám thép dài | **P1** | Sprite kiếm mới (H-W01) + vị trí nghỉ |
| H04 | **Kiếm khi đánh** | C25 0:02.53–2.70 (`strip_C25_attack` #4–#9) | Kiếm trong frame Attack của `Hero_Sheet` + `PlayerVisual` xoay sprite 6×16 | Tham chiếu: kiếm là **sprite riêng, dài ≈ 1.6–1.8 lần chiều cao hero**, chuôi cam có chắn tay, lưỡi thép có viền sáng, cầm chéo xuống trước mặt rồi dựng thẳng trên đầu (C25 #20–#23) | **P0** | Kiếm thành attachment riêng 2–3 tư thế (H-W02) |
| H05 | **Tư thế đánh** | C25 #4–#9, #15–#17 | `Hero_Attack_*` 3 frame + windup | Tham chiếu: thân **khom thấp, áo choàng xoè ngang** khi chém (đổi silhouette rõ). Unity: thân gần như giữ nguyên | **P0** | Vẽ lại Attack Side/Down/Up (spec H §5) |
| H06 | **Idle** | C05 0:00.5–5.5 (đo được) | `Hero_Idle_*` 4 frame, clip dài 0.67 s | Tham chiếu lặp **0.60 s**, 4 lần đổi hình với khoảng giữ không đều 11/4/3 khung video (đo bằng diff). Thay đổi chủ yếu ở tóc và vạt áo | **P1** | Chỉnh lại 4 frame và thời gian giữ |
| H07 | **Chạy ngang** | C17 0:36.45–36.58 (`strip_C17_run_right`) | `Hero_Walk_Side` 4 frame, thân thẳng | Tham chiếu: **thân ngả mạnh về trước**, tóc bay ra sau, kiếm kéo sau lưng chĩa xuống chéo, bước chân dài | **P0** | Vẽ lại Walk Side (spec H §5) |
| H08 | **Quay lưng (Up)** | C17 0:36.62–36.95 (`strip_C17_run_right` #5–#15) | `Hero_Walk_Up` / `Idle_Up` | Tham chiếu: khối tóc trắng hình vòm trên **áo choàng đen gần như đặc** `#1C1A26`, kiếm treo thẳng sau lưng, chân lộ khi bước | **P1** | Vẽ lại Up |
| H09 | Hướng nhân vật | C05 (trước), C17 (ngang, sau), C04 (¾ xuống–phải) | 3 hướng (Down / Side / Up) + lật | Tham chiếu chỉ thấy **3 hướng + lật**. Không có bằng chứng cho 8 hướng | — | **Giữ 3 hướng**. Không tạo chéo |
| H10 | **Hurt** | C22 0:44.7–45.6 (`strip_C22_death` #1–#10) | `Hero_Hurt_*` (R1, tô đỏ nhạt) | Tham chiếu: **cả sprite chuyển hồng–đỏ nhạt** (tint, không đổi tư thế). Ảnh tham chiếu bị lớp vòng AoE đỏ phủ lên nên màu chính xác không xác định | **P2** | Giữ cách tint; không cần frame mới |
| H11 | **Death** | C22 0:45.7–46.1 (`strip_C22_death` #11–#15) | `Hero_Death` 4 frame (R1, xoay sprite nằm) | Tham chiếu: hero **nằm nghiêng sang trái** ngay lập tức, kèm bùng hạt đen vuông nhỏ toả ra, rồi nằm yên. Không thấy bước khuỵu | **P1** | Frame nằm vẽ thật (không xoay sprite); hạt đen là VFX |
| H12 | Kích thước trong viewport | C22 0:04.8 | Thân 16×20 trong ô 32×32 | Tham chiếu ≈18×22 (±2). Tỉ lệ màn hình khớp (0.074 so với 0.072) | — | Giữ khung thân ≈16–18 × 20–22 |
| H13 | Mật độ pixel | C05, C22 | 1 px art = 1 texel, PPU 16 | Như nhau. Viền tham chiếu mềm hơn: viền tối `#303C48` thay vì gần đen | **P1** | Viền theo màu, không dùng gần đen |

### Goblin

| ID | Hạng mục | Tham chiếu | Unity hiện tại | Sai khác | Mức | Hướng sửa |
|---|---|---|---|---|---|---|
| G01 | Silhouette | C22 0:04.8, 0:08.0 (`enemy_zoom.png` phải trên) | Mũ trùm tròn thấp, rộng hơn cao, ô 26×22 | **Khớp tốt**: tham chiếu cũng thấp, rộng, mũ trùm phủ đầu, mắt cam | — | Giữ |
| G02 | Palette | C22 0:08.0 | Mũ `#64293F`, mặt `#84C656`, viền `#2A2032` | Mặt tham chiếu tối hơn `#5D9C3A`; mũ gần `#543641` (biến thể xám–ô liu `#555448` ở nhóm Lv.3 dưới) | **P2** | Giảm sáng mặt khoảng 10–15% |
| G03 | Kích thước so với hero | C22 0:04.8 | Thân ≈20×14 / hero 16×20 | Tham chiếu ≈20×16 / hero 18×22. Tỉ lệ khớp | — | Giữ |
| G04 | Walk / Idle | C22 (đông quái) | 2 idle + 4 walk | Tham chiếu: quái nhỏ, camera chuyển động, hiệu ứng che → **NOT MEASURED** | — | Giữ |
| G05 | Hurt | C22 0:33.4–34.1 (`strip_C22_crowd`) | `Goblin_Hurt` 1 frame (sprite bạc màu) | Tham chiếu: **nhấp nháy trắng đặc** (silhouette trắng), không phải bạc màu | **P1** | Hurt = flash trắng đặc 2 khung (có thể làm bằng shader/tint thay vì vẽ) |
| G06 | Death | C22 0:33.5–33.9 | Biến mất sau 0.25 s (Health destroyDelay) | Tham chiếu: silhouette **chuyển tối/đen**, kèm vài cụm khói trắng tròn bay ra rồi mất | **P1** | 2–3 frame tối dần + VFX khói (VFX, không phải sprite quái) |
| G07 | Knockback | C22 0:33.6–34.1 | Có knockback vật lý | Tham chiếu: quái lùi kèm cụm bụi trắng nhỏ | **P2** | VFX bụi khi bị đẩy |
| G08 | Attack | — | 3 frame có vệt dao | **Không thấy rõ trong tham chiếu** | — | Giữ (ghi chú: thiết kế hiện có, không có tham chiếu) |
| G09 | Nhãn + HP | C22 | "Goblin Lv.3" + HP | Khớp | — | **Giữ nguyên** |

### Gobrider

| ID | Hạng mục | Tham chiếu | Unity hiện tại | Sai khác | Mức | Hướng sửa |
|---|---|---|---|---|---|---|
| R01 | **Silhouette** | C22 0:08.0 (`enemy_zoom.png` trái + giữa); `GameReference/frames/04_enemy_approach_34_0s.jpg` | Lợn rừng nhìn ngang + goblin nhỏ trên lưng, khối **nằm ngang** 30×24 | Tham chiếu: goblin mũ nhọn đỏ ngồi trên **thú khối đứng thẳng** màu hồng–nâu, khối **đứng, gần vuông**; một số cầm vũ khí xương/sừng màu be. Khác hẳn silhouette | **P0** | Vẽ lại hoàn toàn (spec Gobrider) |
| R02 | Kích thước | C22 0:08.0 | Ô 30×24 | Tham chiếu ≈23×26 (±2) → cao hơn hero một chút, to hơn Goblin rõ | **P0** | Ô 32×32, thân ≈24×26 |
| R03 | Palette | C22 0:08.0 | Lợn `#7A4A35`-ish, mũ `#64293F` | Thú `#977371` (hồng nâu), viền `#692F3E`, mũ đỏ sẫm có chóp đỏ tươi, mặt goblin `#98A747` | **P0** | Theo spec |
| R04 | Charge | — (không thấy trong các clip đã xem) | Có trạng thái ChargeTelegraph / ChargeActive trong code | **NOT OBSERVED**. Visual charge sẽ là **thiết kế bổ sung** | **P2** | Ghi rõ "design addition" |
| R05 | Hurt / Death / Knockback | C22 (cùng kiểu Goblin) | Như Goblin | Như G05–G07 | **P1** | Dùng chung hệ flash/tối/khói |
| R06 | Nhãn | Tham chiếu ghi "Gobrider Lv.6" | "Gobrider Lv.5" | Theo yêu cầu: **giữ tên và cấp trong Unity** (Lv.5) | — | Không đổi |

---

## 3. Tổng hợp ưu tiên

| Mức | Hạng mục |
|---|---|
| **P0** | H01 palette áo · H02 tóc · H04 kiếm khi đánh (attachment) · H05 tư thế đánh · H07 chạy ngang · R01–R03 silhouette/kích thước/palette Gobrider |
| **P1** | H03 kiếm nghỉ · H06 nhịp idle · H08 quay lưng · H11 death nằm · H13 màu viền · G05 hurt flash trắng · G06 death tối + khói · R05 |
| **P2** | H10 hurt · G02 palette Goblin · G07 bụi knockback · R04 charge (thiết kế bổ sung) |

**Phạm vi đề xuất cho R2B (chờ duyệt):** chỉ sản xuất **Hero** (manifest `R2_CANVA_ASSET_MANIFEST.csv`). Goblin giữ nguyên art. Gobrider làm sau Hero.

## 4. Khung tham chiếu chính

| Chủ đề | File | Clip @ thời điểm |
|---|---|---|
| Hero idle (trước, cận) | `ref_strips/strip_C05_hero_idle_1s.png`, `frame_05_0.8.png` | C05 0:01.0–2.3 |
| Hero chạy ngang + quay lưng | `ref_strips/strip_C17_run_right.png` | C17 0:36.45–36.95 |
| Hero đánh + kiếm | `ref_strips/strip_C25_attack.png` | C25 0:02.40–3.17 |
| Hero hurt + death | `ref_strips/strip_C22_death.png` | C22 0:44.6–46.9 |
| Goblin / Gobrider | `ref_strips/enemy_zoom.png`, `frame_22_8.0.png` | C22 0:08.0 |
| Hurt flash / death quái | `ref_strips/strip_C22_crowd.png` | C22 0:33.0–34.5 |
| Unity hiện tại | `ref_strips/UNITY_current_*.png` | — |
