# R1 Reference Rebuild — Audit & Asset Production Brief

Tài liệu audit để kéo game Unity hiện tại (Phase 12) lại gần bộ 30 video tham chiếu. **Chưa thay đổi gì trong project**; mọi file nằm trong `GameReference/R1_Rebuild/`, ngoài thư mục `Assets/`.

## Nội dung thư mục

| File / thư mục | Dùng cho |
|---|---|
| `CLIP_CATALOG.md` | Danh mục 30 clip: thời lượng, loại (CORE/OPT/MENU/EDITOR), timecode tốt nhất, khung tham chiếu theo thành phần, tỉ lệ pixel |
| `DIFFERENCE_MATRIX.md` | 26 sai khác: tham chiếu, trạng thái Unity, mức P0/P1/P2, cách sửa, Canva hay Cursor làm, cách nghiệm thu. Kèm danh sách phần Phase 12 phải giữ |
| `ASSET_MANIFEST.csv` | 31 asset cho Canva: kích thước, số frame, palette có mã màu, nền trong suốt hay không, pivot/PPU cần xác nhận, đích trong Unity, ảnh tham chiếu |
| `ref_crops/` | 17 ảnh cắt sẵn theo thành phần (Hero, Goblin, cỏ, đất, cây, đòn R, số sát thương, hotbar, VFX băng, boss, trại, HUD landscape) |
| `ref_frames/` | 30 khung gốc 576×1024 ở các timecode quan trọng (`frame_<clip>_<giây>.png`) |
| `contact_sheets/` | 29 contact sheet, mỗi clip 16 khung có in timecode |

## Đầu vào và giới hạn

- **Có:**
  - 30 clip MP4 trong `Downloads/Telegram Desktop`.
  - `IncomingAssets/R0_Audit/ASSET_INVENTORY.csv` + `UNITY_SNAPSHOT.txt`.
  - 7 ảnh `Assets/Screenshots/R0_Baseline/` và các ảnh `phase12*`.
- **Không tìm thấy trên máy:** "Reference Rebuild Plan Pack" và `PROJECT_AUDIT.md`. Contact sheet và danh mục clip ở đây là tôi dựng lại từ MP4.
- **Clip 21 không giải mã được** trong trình duyệt. Cần xuất lại sang H.264 hoặc gửi screenshot.
- Không có ffmpeg/Python, nên khung hình được trích bằng trình duyệt tích hợp (HTML5 video). Mỗi clip có 16 khung, chia đều theo thời lượng.
- Baseline `03_r_attack_combo.png` không bắt được khung đang chém. Các dòng về đòn R, số sát thương và chữ HITS trong matrix ghi "cần xác nhận" và nên chụp lại.
- Lần này không mở Play Mode. Trạng thái Unity lấy từ ảnh và file audit.
- Game tham chiếu làm bằng Godot. Chỉ dùng làm tham chiếu hình và cảm giác. **Asset phải vẽ mới, không cắt hay copy từ video.**

## Kết luận nhanh

1. **Cỏ và đường đất là khác biệt lớn nhất.**
   - Tham chiếu: cỏ xanh bạc hà ngả xám `#76A989`, có mảng cỏ tối viền răng cưa `#5A665B`, đường đất cam `#D59C6B` hình hữu cơ.
   - Unity: cỏ xanh vàng đậm `#6C9946`, đất `#B09262` ghép từ khối vuông lộ lưới.
   - Phần này chiếm 60–70% màn hình nên ảnh hưởng mạnh nhất tới độ giống.
2. **Cây** đã gần đúng phong cách nhưng tán tối và xám hơn, thiếu viền tím, và nhỏ hơn khoảng 1.5 lần.
3. **Đòn R** của tham chiếu là lưỡi liềm lõi trắng đặc, mép cyan, cao khoảng 4 lần hero. Đây là hình ảnh "chặt chém" xuất hiện trong hầu hết clip combat.
4. **Hotbar** khác bố cục: tham chiếu là một hàng 2·3·4·R·V·C, R là ô thứ 4. Unity đang dùng bố cục mobile, có nút R to tách riêng và ô joystick trắng.
5. **Prop trại goblin quá nhỏ:** lều nhỏ hơn khoảng 2.5 lần, bát lửa nhỏ hơn 2 lần.
6. Hero, Goblin, tỉ lệ camera, hạt bụi tím, nhãn quái và HP bar đã gần khớp. **Giữ nguyên.**

## 3 hạng mục nên làm mẫu đầu tiên

Chọn theo tiêu chí: **cải thiện độ giống nhiều nhất trên mỗi asset**, chỉ cần thay hình, không phải viết lại hệ thống.

### 1. Bộ nền cỏ + mảng cỏ tối + đường đất mép răng cưa
- **Sai khác:** D01, D02, D03.
- **Asset:** A01, A02, A04, A05.
- **Lý do:** chiếm phần lớn khung hình ở mọi level. Chỉ riêng việc đổi palette và bỏ cạnh vuông đã khiến ảnh chụp gần tham chiếu hơn hẳn.
- **Phần Cursor:** thay sprite trong tile asset có sẵn, giữ GUID. Dựng 2 RuleTile, giữ nguyên các ô đã vẽ. Không đổi va chạm hay layout level.
- **Nghiệm thu:** chụp L1 và L4 ở 1920×1080, đặt cạnh C25 0:02.9 và C22 0:04.8. Màu cỏ trung vị lệch ≤ 8 RGB so với `#74A587`. Không còn cạnh vuông 16 px. Mỗi map có ≥ 3 mảng cỏ tối.

### 2. Lưỡi liềm đòn R (3 biến thể theo chuỗi 3 nhát)
- **Sai khác:** D07.
- **Asset:** A12.
- **Lý do:** đây là "chữ ký" hình ảnh của video và quyết định cảm giác chặt chém. Hệ thống đã có sẵn: chuỗi 3 nhát, hitstop, shake, pool VFX. Chỉ thiếu sprite.
- **Phần Cursor:** thay sprite procedural trong `CombatVfx.SpawnSwordSlash` bằng sprite sheet, chọn biến thể theo bước chuỗi. Giữ nguyên timing và sát thương.
- **Nghiệm thu:** chụp đúng frame chém ở 1080p. Lưỡi liềm cao ≥ 3.5 lần hero, lõi trắng chiếm ≥ 40% diện tích. 3 nhát nhìn khác nhau. FPS không giảm.

### 3. Cây sồi lớn/vừa có viền tím + bóng gốc
- **Sai khác:** D05.
- **Asset:** A07, A08, A10.
- **Lý do:** cây bao quanh mọi level và tạo nên silhouette "rừng phép" của tham chiếu. Hệ cây mờ khi hero đi phía sau đã có, chỉ cần đổi sprite.
- **Phần Cursor:** thay sprite trong prefab `Tree_Forest_A/B` và chỉnh collider gốc cây. Giữ `TreeCanopyOcclusion`.
- **Nghiệm thu:** ảnh 1080p thấy rõ viền tím. Cây lớn rộng gấp 4–5 lần hero. Hero đi sau tán thì tán mờ đi.

**Hạng mục ứng viên tiếp theo** (nếu 3 mẫu trên đạt): hotbar một hàng (D10: A15, A16), lều và bát lửa cỡ tham chiếu (D14–D15: A20, A22), số sát thương (D08: A13).

## Quy trình đề xuất

1. Canva vẽ 3 mẫu ở đúng kích thước pixel trong manifest, xuất PNG nền trong suốt (tile cỏ/đất thì nền đặc). Kèm bản phóng ×4 để duyệt.
2. Duyệt mẫu bằng cách đặt cạnh ảnh `ref_crops/`.
3. Cursor import:
   - PPU 16, Filter Point, không nén, không mipmap.
   - Pivot theo cột `pivot_to_confirm`.
   - Sao lưu vào `IncomingAssets/backup_before_R1_<hạng mục>` trước khi thay.
4. Chụp lại đúng góc baseline R0 và đối chiếu theo cột "Nghiệm thu" trong `DIFFERENCE_MATRIX.md`.
