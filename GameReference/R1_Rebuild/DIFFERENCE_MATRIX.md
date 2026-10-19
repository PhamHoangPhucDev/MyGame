# DIFFERENCE_MATRIX — Unity (Phase 12, R0 baseline) so với video tham chiếu

**Nguồn trạng thái Unity:**
- 7 ảnh trong `Assets/Screenshots/R0_Baseline/`, cùng `phase12*_*.png` (boss, L4 combat).
- `IncomingAssets/R0_Audit/UNITY_SNAPSHOT.txt` + `ASSET_INVENTORY.csv`.
- Lần này **không mở Play Mode** (theo yêu cầu chỉ audit).
- Dòng nào ghi "cần xác nhận" là vì baseline không chụp được trạng thái đó.

**Mức ưu tiên:**
- **P0** — nhìn là thấy khác ngay, chiếm nhiều diện tích màn hình hoặc là cảm giác cốt lõi.
- **P1** — khác rõ khi chơi.
- **P2** — tinh chỉnh.

**Người làm:**
- **Canva** — vẽ hoặc xuất asset pixel art.
- **Cursor** — import, layout, thay sprite, chỉnh tham số. Không viết lại hệ thống.

Mã màu lấy mẫu trực tiếp từ khung video và screenshot (median 5×5 px).

| # | Thành phần | Tham chiếu (clip / timecode) | Unity hiện tại | Sai khác | Mức | Cách sửa | Người làm | Nghiệm thu |
|---|---|---|---|---|---|---|---|---|
| D01 | **Màu cỏ nền** | C25 0:02.9, C22 0:04.8 — `#76A989` / `#71A184`, xanh bạc hà ngả xám | Baseline `01`: `#6C9946`, xanh lá ngả vàng, độ bão hoà cao | Khác cả tông màu (hue ~142° so với ~92°) lẫn độ bão hoà. Đây là khác biệt **lớn nhất** vì chiếm 60–70% màn hình | **P0** | Vẽ bộ tile cỏ mới theo palette tham chiếu: 1 tile nền + 3 biến thể có ngọn cỏ + hoa nhỏ tím/vàng. Thay sprite cho các Tile asset hiện có (`GrassA/B/C`), giữ nguyên tên và GUID | Canva vẽ · Cursor thay sprite trong tile asset | Ảnh 1920×1080 cùng góc: màu cỏ trung vị lệch ≤ 8 đơn vị RGB so với `#74A587`. Không thấy lưới tile |
| D02 | **Mảng cỏ tối** | C22 0:04.8 (vùng quanh bát lửa), C17 0:10.5 — `#5A665B`, viền răng cưa 1–2 px | Không có. Nền cỏ chỉ một màu | Thiếu lớp mảng cỏ tối hình hữu cơ, vốn là đặc trưng của tham chiếu | **P0** | Vẽ RuleTile "cỏ tối trên cỏ" (16 tile viền + 1 tile giữa) có viền răng cưa. Cursor thêm một Tilemap overlay và vẽ mảng theo layout từng map | Canva vẽ · Cursor dựng RuleTile và vẽ mảng | Mỗi map có ≥ 3 mảng cỏ tối. Viền liền mạch, không lộ góc vuông |
| D03 | **Đường đất** | C25 0:02.9, C28 0:04.8 — `#D59C6B`, mép răng cưa, hình hữu cơ | Baseline `01`, `03`: `#B09262`, ghép từ khối vuông 16 px, thấy rõ lưới, mép thẳng | Khác màu (tham chiếu cam hơn), khác hình (khối vuông so với hữu cơ) | **P0** | Vẽ RuleTile "đất trên cỏ" kiểu 47 hoặc 16 tile, viền cỏ răng cưa phủ lên đất. Cursor đổi Tile asset của lớp đường sang RuleTile. **Giữ nguyên ô đã vẽ**, không đổi layout hay va chạm | Canva vẽ · Cursor thay tile | Không còn cạnh vuông 16 px thấy được ở 1080p. Màu đất trung vị trong khoảng `#D59C6B` ± 10 |
| D04 | Hố trên bãi đất | C26 0:01.2 — hố `#CD8766` hình elip | Không có | Bãi đất trống, thiếu chi tiết | P2 | 3 sprite hố/đá nhỏ để rải trang trí | Canva | ≥ 5 chi tiết trên mỗi bãi đất lớn |
| D05 | **Cây** | C25 0:02.9 (trong game), C24 0:14.5 (sprite gốc) — tán `#7FC09F`, bóng tán xanh lam, **viền tím `#7F60B6`**, thân `#88574A`, bóng tròn dưới gốc | Baseline: tán xám xanh `#70929D` / `#566C72`, không có viền tím, kích thước 62×78 / 54×68 px. Cây xếp chồng thành bức tường đều nhau | Tán tối và xám hơn, thiếu viền tím (đặc trưng "rừng phép"), nhỏ hơn khoảng 1.5 lần, thiếu biến thể | **P0** | Vẽ 3 cây: lớn khoảng 96×112, vừa khoảng 72×84, bạch dương khoảng 40×88. Có viền tím 1 px, bóng gốc tách riêng. Cursor thay sprite trong prefab `Tree_Forest_A/B`, giữ collider gốc và `TreeCanopyOcclusion` (cây mờ khi hero đi phía sau) | Canva vẽ · Cursor thay sprite và chỉnh collider | So với C25 0:02.9: thấy viền tím ở 1080p. Cây lớn rộng gấp 4–5 lần hero. Hiệu ứng che/mờ vẫn chạy |
| D06 | Bụi cây | C25 0:02.9, C12 0:12.7 — bụi tròn mint có viền tối | Có bụi (Bush A–E) nhưng màu xanh đậm | Lệch màu giống D01 | P1 | Đổi palette 5 sprite bụi sang mint / lam (có thể chỉnh màu, không cần vẽ lại hình) | Canva chỉnh màu | Màu khớp dải màu tán cây D05 |
| D07 | **Đòn R (lưỡi liềm)** | C25 0:02.9, C06 0:49.2, C03 0:57.7 — lưỡi liềm **lõi trắng đặc `#FFFFFF` + mép cyan `#6CE1D6`**, cao khoảng 4 lần hero, mép pixel cứng, 3 nhát trong combo | `CombatVfx` vẽ lưỡi liềm bằng code (48 px), mảnh. Baseline `03` không bắt được khung chém (**cần xác nhận lại bằng ảnh**) | Nhỏ và mảnh hơn nhiều, thiếu khối trắng đặc. Đây là cảm giác "chặt chém" chính | **P0** | Canva vẽ sprite sheet lưỡi liềm: 3 biến thể (chém trái, chém phải, nhát cuối to) × 4 frame. Cursor chỉ thay sprite trong `CombatVfx.SpawnSwordSlash`, **giữ nguyên timing, hitstop, shake, chuỗi 3 nhát** | Canva vẽ · Cursor thay sprite | Chụp đúng frame chém: lưỡi liềm cao ≥ 3.5 lần hero, lõi trắng chiếm ≥ 40% diện tích. 3 nhát nhìn khác nhau |
| D08 | **Số sát thương** | C17 0:10.5, C26 0:13.6 — chữ số pixel to, đậm, gấp khoảng 3 lần nhãn tên, có viền và bóng, chồng lệch nhau; số chí mạng màu vàng | Có pool `DamagePopup` + font PressStart2P (Phase 6+). Chưa có ảnh baseline lúc trúng đòn (**cần xác nhận**) | Có thể nhỏ hơn và thiếu chồng lệch, thiếu màu vàng chí mạng | P1 | Canva vẽ bitmap font số 0–9 (trắng + vàng) cao khoảng 12 px. Cursor dùng font này cho `DamagePopup`, offset chồng ngẫu nhiên ±6 px | Canva vẽ font · Cursor chỉnh popup | Ảnh trúng đòn: số đọc rõ, cao gấp khoảng 3 lần nhãn "Goblin Lv.3" |
| D09 | **"xN HITS" và "N DAMAGE"** | C17 0:10.5, C03 0:23.5 — "x19 HITS" nằm **cạnh phải mục tiêu** trong thế giới game; tổng "4,295 DAMAGE" nằm cạnh hero | `HitComboCounter` hiện chữ `xN HITS` cố định trên HUD (khoảng 82% chiều cao màn hình). Chưa có chữ tổng DAMAGE | Vị trí khác: tham chiếu đặt trong thế giới game, cạnh mục tiêu | P1 | Cursor chuyển nhãn combo sang dạng neo theo thế giới game (cạnh phải mục tiêu gần nhất) và thêm bộ đếm tổng sát thương hiện ra khi combo kết thúc. Chỉ thay phần hiển thị, không đổi cách đếm | Cursor | Ảnh: chữ HITS nằm trong khoảng 1–2 ô bên phải nhóm quái bị đánh |
| D10 | **Hotbar** | C22 0:04.8, C26 0:13.6 — **một hàng** 2·3·4·**R**·V·C (R là ô thứ 4, cùng cỡ ô khác), khung bo xám `#808C85`, nền icon đỏ đậm `#8A0F10`, cooldown làm tối icon + số ở góc trên phải, ô bình máu nổi phía trên ở giữa. Landscape C15: một hàng nhỏ giữa đáy | Baseline `08`: 5 ô ở giữa + **nút R to tách riêng bên phải** + ô joystick trắng bên trái (bố cục mobile) | Khác bố cục rõ rệt, ô joystick là thứ dư thừa trên PC | **P0** | Cursor: chế độ PC = một hàng 6 ô theo thứ tự 2,3,4,R,V,C, ẩn joystick và nút R to. Giữ bố cục mobile làm tuỳ chọn. Canva: khung ô bo góc + 6 icon 16×16 vẽ lại theo tham chiếu + ô bình máu | Cursor layout · Canva khung + icon | Ảnh 1920×1080 khớp C22 0:04.8: một hàng, R ở vị trí 4, có số cooldown góc trên phải, không còn ô trắng joystick |
| D11 | Ô bình máu | C25 0:02.9, C20 0:57.7 — ô đỏ nổi phía trên hotbar, có số cooldown | Không có | Thiếu | P2 | Chỉ thêm hình (ô trống) trước. Logic bình máu làm sau, ngoài phạm vi | Canva icon · Cursor đặt ô | Ô hiển thị đúng vị trí |
| D12 | HUD trái trên | C15 0:26.1, C07 0:07.0 — chân dung hero, "115 / 115", Lv, thanh XP | Thanh HP đỏ trơn + "L1" + "WAVE 1/4" font mặc định | Thiếu chân dung, thiếu số HP | P1 | Canva vẽ khung chân dung 32×32 + ảnh mặt hero. Cursor ghép vào HUD, **giữ chữ WAVE/Level của Phase 12** | Canva + Cursor | HUD trái trên có chân dung + số HP |
| D13 | Font HUD | Tham chiếu dùng pixel font cho mọi chữ | "CLEAR WAVES", "L1", "WAVE 1/4" dùng font mặc định (LegacyRuntime) | Lệch phong cách | P1 | Cursor đổi các chữ này sang `PressStart2P` (đã có trong `Resources/Fonts`) | Cursor | Không còn chữ font mặc định trên màn hình |
| D14 | **Lều goblin** | C22 0:04.8 — lều xanh / tím sừng trâu + sọ, khoảng 105×100 px, **chiếm gần nửa khung hình** | `TentA/B` 40×48 (chỉ to khoảng 30×38 trên màn hình) | Nhỏ hơn khoảng 2.5 lần. Trại không còn cảm giác "trại goblin" | P1 | Canva vẽ lều lớn khoảng 96×96 (2 màu) + lều chỉ huy khoảng 128×112 (C20 0:57.7). Cursor thay sprite và chỉnh collider. Có thể phải dời vài prop cho khỏi chồng lấn | Canva + Cursor | Lều lớn rộng ≥ 5 lần hero. Không chặn đường đi của wave |
| D15 | Bát lửa | C22 0:04.8 — bát `#C69F80` khoảng 52×60 | Brazier 26×29 | Nhỏ hơn 2 lần | P1 | Vẽ lại ở kích thước khoảng 48×56, giữ quầng lửa của `EnvironmentAmbience` | Canva | Bát cao khoảng 2.5 lần hero |
| D16 | Goblin | C22 0:04.8 — mũ trùm `#4E3441`, mặt `#619B39`, mắt vàng | Phase 4: gần giống | Nhỏ: chi tiết mắt và tông mặt | P2 | Giữ nguyên. Chỉ chỉnh màu mặt cho khớp `#619B39` nếu cần | Canva (tuỳ chọn) | — |
| D17 | Gobrider | `GameReference/frames/04_enemy_approach` — goblin cưỡi lợn rừng nâu | Baseline boss: khối đỏ sẫm, khó đọc là "cưỡi thú" | Hình khối chưa đọc được | P1 | Canva vẽ sheet Gobrider (idle 2, walk 4, attack 3, hurt 1) ở khoảng 28×24 | Canva · Cursor gắn vào Animator có sẵn | Nhìn ảnh nhận ra ngay "goblin cưỡi lợn" |
| D18 | Quái mới Archer / Spearman | C20 0:13.7, 0:30.2 | Không có | Tuỳ chọn | P2 | Ngoài phạm vi R1 | — | — |
| D19 | **Boss** | C20 0:57.7 — thanh **ngang toàn màn hình** ở mép trên + tên "Goblin Chief Lv.11"; vùng cảnh báo nón đỏ (1:08.6); C11 0:32.4 — vòng đỏ AoE | Phase 12: thanh boss ở giữa trên, rộng khoảng 27% màn hình, có "— PHASE 1". Warlord có thể chưa có vùng cảnh báo AoE hình tròn/nón (**cần xác nhận**) | Thanh hẹp hơn. Thiếu sprite cảnh báo | P1 | Cursor kéo thanh boss rộng gần hết màn hình, **giữ chữ PHASE**. Canva vẽ sprite vòng đỏ + nón đỏ bán trong suốt | Cursor + Canva | Ảnh boss: thanh rộng ≥ 80% màn hình. Đòn mạnh có vùng đỏ báo trước ≥ 0.5 s |
| D20 | Hình boss | C20 0:57.7 Goblin Chief, C30 0:24.4 vua goblin | Gobrider Warlord: khối đỏ lớn | Thiếu silhouette riêng | P1 | Canva vẽ boss khoảng 40×40 (idle 2, walk 4, telegraph 2, attack 4) | Canva | Boss khác hẳn Gobrider thường |
| D21 | **Nhãn quái + HP bar** | C22 — chữ trắng pixel nhỏ, HP bar viền tối + đỏ + đoạn vàng trễ (C26 0:13.6) | Có đủ (Phase 12) | Chỉ thiếu đoạn vàng trễ | P2 | **Giữ nguyên.** Tuỳ chọn thêm đoạn vàng trễ giống thanh HP hero | Cursor (tuỳ chọn) | Nhãn và HP bar vẫn hiện như hiện tại |
| D22 | VFX skill | C26 0:06.2 lửa, 0:13.6 băng chữ X, 0:21.0 nứt đất, C01 0:06.3 meteor, 0:08.9 freeze | Ngôi sao / vòng tròn vẽ bằng code | Thiếu hình đặc trưng từng skill | P1 | Canva vẽ 5 sprite sheet VFX (mỗi skill 1 cái). Cursor chỉ thay sprite trong `CombatVfx` theo skill | Canva + Cursor | Mỗi skill nhận ra được qua ảnh chụp |
| D23 | Mép bản đồ | C10 0:01.1 vách đất, C25 rừng dày | Baseline `01`, `03`: dải xanh đậm thẳng đứng ở mép phải | Mép giả, lộ ranh giới | P1 | Canva vẽ tile vách đất + tường cây. Cursor thay dải mép | Canva + Cursor | Không còn dải màu thẳng ở mép |
| D24 | Bóng tiếp đất | Tham chiếu: mọi nhân vật / prop có bóng elip tối | Cây có bóng. Hero/quái có bóng không (**cần xác nhận**) | Có thể thiếu | P2 | Dùng `Shadow_Small/Medium` có sẵn | Cursor | Hero và quái đều có bóng |
| D25 | Hạt bụi tím | Tham chiếu có | Có (`EnvironmentAmbience`) | Khớp | — | Giữ | — | — |
| D26 | Camera / tỉ lệ | Vùng nhìn khoảng 172×306 px art (dọc) | 480×270 PPU 16 landscape | Chiều cao vùng nhìn tương đương | — | Giữ | — | — |

## Phần Phase 12 phải giữ nguyên

Cursor **không được** viết lại hay đổi hành vi các phần sau. R1 chỉ thay sprite, palette, layout UI:

- 5 level (Camp, Trail, AbandonedCamp, GoblinOutpost, GobriderStronghold), màn chọn level, màn chiến thắng / chapter complete.
- Hệ wave ("WAVE n/4", "CLEAR WAVES TO OPEN EXIT"), mở lối ra.
- Boss Gobrider Warlord + thanh boss + chữ PHASE.
- **Mob Name Label và Enemy HP Bar** (`EnemyWorldUI`, `WorldLabelsCanvas`).
- Combat: R chuỗi 3 nhát, auto-aim, hitstop, camera shake, skill 2/3/4/V/C, cooldown.
- Hero: Animator có Windup/Hurt/Death, `PlayerVisual`.
- Pool VFX / popup, `GameAudio` + SFX, `EnvironmentAmbience`, `TreeCanopyOcclusion`.
- Pixel Perfect Camera 480×270, PPU 16, filter Point.

Quy tắc cho Cursor:
- Thay sprite **trong asset có sẵn** (giữ GUID).
- Nếu sprite mới có kích thước khác thì chỉnh collider.
- Trước mỗi bước, sao lưu scene và prefab vào `IncomingAssets/backup_before_R1_*`.
