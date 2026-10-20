# R2A — Hero Reference Matching: Visual Audit

**Ngày:** 2026-10-20 · **Phạm vi:** chỉ phân tích, không sửa Unity
**Nguồn:** clip tham chiếu 03, 06, 12 (ưu tiên), 05, 17, 25 (đối chiếu từ R2A trước), video Sandbox 13.27 s (clip 102), `R1_SANDBOX_RESULTS.md`, Hero Phase 4, Hero R2b, `PlayerVisual.cs`.

**Ảnh kèm theo:**
- `COMPARE_hero_phase4_r2b_reference.png`: so sánh Phase 4, R2b và tham chiếu (idle, side, attack).
- `sandbox/SANDBOX_hero_crops_x5.png`: Hero trong Sandbox tại 2.0 / 5.5 / 9.5 / 10.5 / 12.0 s.
- `ref_timecoded/`: 16 dải khung (`strip_*`, tên = clip + mm:ss) và 16 khung đơn (`frame_*`, tên = clip + timecode + trạng thái).

---

## 0. Phát hiện quan trọng trước khi đọc tiếp

Có **hai bộ Hero** trong project (thay đổi Cursor làm sau commit `abdeac8`, chưa commit):

| Nơi dùng | File | Nội dung |
|---|---|---|
| Level 1–4 (`Hero.controller`) | `Assets/Art/GoblinCampPhase4/Hero_Sheet.png` | **Phase 4** (đã được trả về, md5 trùng `IncomingAssets/backup_before_R2B/`) |
| Sandbox (`Hero_R2.controller`) | `Assets/Art/Rebuild/R2/Hero/Hero_Sheet.png` | **R2b** |

Vậy Hero trong video Sandbox 13 s là **R2b**. Việc tách này đúng với ràng buộc "không đưa art Rebuild vào Level 1–4 trước khi duyệt", nên tôi giữ nguyên.

---

## 1. Phương pháp đo

- **Kích thước khối pixel** của mỗi clip: tự tương quan cạnh trên toàn khung (`BlockScale.cs`). Kết quả: C12 ≈ 4.05, C03 ≈ 3.1, C06 (đoạn chạy) ≈ 4.05, C22 ≈ 3.95, C25 ≈ 4.3 px video cho mỗi px art. Riêng C06 0:01.3 bị camera zoom (≈ 6.3).
- **Chiều cao, chiều rộng Hero:** profile theo hàng, neo vào tóc trắng (`HeroProfile.cs`), rồi chia cho kích thước khối.
- **Đối chiếu chéo:** hai clip độc lập cho cùng một chiều cao (bảng dưới).
- **Mức tin cậy:**
  - **ĐO:** có số đo pixel.
  - **TỈ LỆ:** đo theo phần trăm trên ảnh zoom C06, chưa quy ra px art chính xác.
  - **CHƯA ĐO:** ghi rõ, Canva không được tự đặt số cho các mục này.

---

## 2. Tỉ lệ và silhouette

| Thông số | Tham chiếu | Cách đo | Phase 4 | R2b | Nhận xét |
|---|---|---|---|---|---|
| Chiều cao thân (đỉnh tóc → đế giày) | **21 px art** | ĐO: C12 85 px / 4.05; C03 0:02.1 65 px / 3.1. Hai clip khớp nhau | 20 | 22 | Phase 4 thiếu 1, R2b thừa 1 |
| Chiều rộng tóc, idle mặt trước | **≈ 14 px art** (±1) | ĐO: C03 44 px / 3.1. C12 ở tư thế 3/4 nên hẹp hơn (≈ 11.4) | 16 | 18 | **R2b quá rộng 4 px**, silhouette thành khối vuông |
| Rộng / cao | 0.60–0.68 | TỈ LỆ: C06 zoom 87 / 133 | 0.80 | 0.82 | Cả hai bộ đều quá "bè" |
| Đầu + tóc | ≈ 55% chiều cao (≈ 11–12 hàng) | TỈ LỆ | ≈ 50% | ≈ 50% | Đầu chibi lớn. Giữ |
| Hàng mắt | ≈ 34% (≈ hàng 7/21) | TỈ LỆ | hàng ≈ 8 | hàng ≈ 8 | Gần đúng |
| Mặt (da) | ≈ hàng 7–10 | TỈ LỆ | ✔ | ✔ | |
| Tóc rộng nhất | ngang tầm mắt, hai bên xuống tới ≈ 53% chiều cao | TỈ LỆ | ✔ | tóc thẳng, rộng đều | Tham chiếu có tóc **ôm mặt, thuôn dần xuống vai** |
| Áo khoác | ≈ 46–95% (hàng ≈ 10–20) | TỈ LỆ | ✔ | ✔ | Vạt áo xoè hình thang, có khe giữa |
| Chân, giày | ≈ 15% cuối: chân sáng (≈ 2 px), giày tối `#3C3C48` ≈ 2 hàng | TỈ LỆ (C06 zoom chân) | giày đen | giày tối | Đúng hướng; chân Phase 4 / R2b quá rộng |
| Bóng đổ | elip tối dưới chân | quan sát | sprite riêng | sprite riêng | Giữ sprite Shadow riêng |
| Kiếm (mũi → hết chuôi) | **≈ 1.7 × chiều cao Hero** (≈ 35–38 px art, ±3) | ĐO: C03 0:49.1, 110 px video | 16 (placeholder 6×16) | 16 | **Kiếm hiện tại chỉ bằng ≈ 0.45 độ dài tham chiếu** |
| Độ rộng lưỡi kiếm | CHƯA ĐO (≈ 3–4 px art, ước theo mắt) | — | 2–3 | — | Cần đo lại trên C03 0:49 trước khi vẽ kiếm |

**Silhouette tham chiếu:** khối tóc trắng rộng phía trên, áo xanh đậm hình thang xoè dần xuống dưới, chân sáng mảnh ở giữa và giày tối. Đọc như "cây nấm trắng úp ngược lên áo choàng". Phase 4 gần với silhouette này nhất; R2b vuông và nặng hơn.

---

## 3. Góc camera và hướng nhân vật

- **Camera:** top-down chếch khoảng 3/4 (đồ hoạ RPG kiểu "oblique"). Nhân vật vẽ thẳng đứng, nền nhìn từ trên xuống. Không có phối cảnh thu nhỏ.
- **Các hướng thấy được trong tham chiếu:**

| Hướng | Thấy ở | Ghi chú |
|---|---|---|
| Mặt trước (Down) | C03 0:02.1, C06 0:01.3 | Idle |
| 3/4 mặt trước, lật trái/phải | C12 0:06.9–0:17.2, C06 0:01.2–1.97 | **Đi bộ ngang dùng 3/4 mặt trước**, không dùng profile |
| Sau lưng (Up) | C12 khi quay đầu, C03 0:53.4 | Kiếm chéo sau lưng; chém lên |
| Profile ngang | C06 0:28.6, C03 0:23.5, C03 0:57.6 | **Chỉ dùng khi chạy nhanh / lao và khi chém ngang** |

- **Kết luận:** 3 hướng vẽ (Down, Up, Side) + lật ngang là đủ, đúng với cấu trúc sheet hiện tại. Không cần 8 hướng. Điểm khác là **ô "Side" của Walk nên là dáng 3/4 mặt trước**, còn dáng profile nghiêng người dành cho Sprint và Attack.

---

## 4. Màu và phong cách pixel art

- **Palette:** HR06 đã duyệt (viền tóc `#B4B4B8`). Tham chiếu: tóc trắng gần `#FCFCFC` với bóng xám lạnh, da `#FCF0D8`, áo xanh thép `#3C546C` có highlight xanh nhạt ở mép, chuôi kiếm cam `#E09A52`.
- **Viền:** viền ngoài tối nhưng **không đen tuyệt đối**. Tóc có viền xám nhạt, áo có viền xanh rất đậm. Phase 4 dùng viền đen đặc nên trông nặng hơn tham chiếu.
- **Đổ bóng:** 2–3 sắc độ mỗi vùng, không dither, không khử răng cưa.
- **Mắt:** 2 khối xanh tím đậm, mỗi khối 1×2 hoặc 2×2 px (CHƯA ĐO chính xác), có 1 px highlight. Khoảng cách giữa hai mắt rộng.
- **Pixel art thật:** lưới đều, Point filter, không xoay sprite lúc chạy (xoay làm méo pixel; xem mục 7).

---

## 5. Hoạt ảnh

| State | Tham chiếu | Phase 4 | R2b (Sandbox) | Đánh giá |
|---|---|---|---|---|
| Idle Down | 4 thay đổi trong vòng lặp 0.60 s; giữ ≈ 11/4/3/x khung video ở 30 fps (đo ở C05). Chủ yếu tóc và vạt áo đung đưa, thân không nhún | 4 frame | 4 frame | Số frame đúng. Cần vẽ lại cho thân hẹp 14 px và kiếm sau lưng |
| Walk 3/4 (ngang) | 3/4 mặt trước, sải ngắn, kiếm trên lưng (chuôi qua vai, mũi lộ sau áo), bụi chân. C12 0:16.4–17.2 | profile | profile nghiêng người, không có kiếm | **Sai dáng.** R2b Walk_Side giống dáng *Sprint* của tham chiếu nhưng lại không có kiếm |
| Walk Down / Up | Thấy ngắn ở C12 và C17 | 4 | 4 | Giữ số frame, vẽ lại cho kiếm sau lưng |
| Sprint (profile) | Thân đổ về trước 20–30°, tóc bay ra sau, **kiếm rút ra cầm tay, kéo lê phía sau**; dừng lại thì kiếm về lưng. C06 0:28.6, C03 0:23.5 | — | (Walk_Side đang mang dáng này) | Game chưa có cơ chế sprint → P2 |
| Attack Side | Ngồi thấp, áo xoè ngang, kiếm chéo xuống trước rồi dựng đứng; trăng lưỡi liềm xuất hiện ≈ 2 khung video sau tư thế chém. C03 0:57.6, C25 0:02.5 | 3 + kiếm vẽ sẵn | 3, **không có kiếm** | Dáng R2b đúng tinh thần; thiếu kiếm |
| Attack Up | Quay lưng, kiếm dựng trên đầu. C03 0:53.4 | 3 | 3 | Vẽ lại kèm tham chiếu kiếm |
| Attack Down | Không thấy trực tiếp; suy từ Side | 3 | 3 | Thiết kế bổ sung |
| Kiếm giơ cao lúc đứng | C03 0:49.1 | — | — | Tham khảo cho dáng cầm kiếm |
| Roll / Dash | Lộn có viền trắng (C03 0:57.3), lao dịch chuyển (C03 0:57.9) | — | — | Không có trong game → P2 |
| Skill | Lốc xoáy C03 0:14.6, C03 0:27.4 | — | — | Chủ yếu là VFX; thân dùng lại frame Attack |
| Hurt / Death | Nháy màu cả sprite; ngã nằm + hạt vuông đen (C22) | 2 + 4 | dùng lại | Giữ cách làm của R1 |

---

## 6. Hero trong Sandbox so với tham chiếu (video 102)

| Thời điểm | Quan sát | Mức |
|---|---|---|
| 2.0 s, 5.5 s (idle / đi) | Thân R2b vuông, rộng hơn tham chiếu rõ rệt. Kiếm placeholder ở tư thế nghỉ trông như một **"cái đuôi" tối chéo** sau lưng, không đọc ra là kiếm | P0 |
| 7.5–9.5 s (đánh) | **Kiếm biến mất hoàn toàn khi chém** (nguyên nhân ở mục 7). Chỉ còn trăng lưỡi liềm | P0 |
| 9.5–12.0 s | Trăng lưỡi liềm **tách rời khỏi Hero** (do offset `SlashPullBack`); trong tham chiếu nó dính sát cạnh kiếm | P1 |
| Toàn video | Đi ngang dùng dáng profile nghiêng người; tham chiếu dùng 3/4 mặt trước | P0 (art) |
| Toàn video | Kích thước so với màn hình hợp lý (Hero thân ≈ 0.086 chiều cao màn hình ở Portrait) | OK |

---

## 7. Quan hệ Hero sprite ↔ Sword GameObject ↔ Weapon Anchor

Đọc từ `Assets/Scripts/Player/PlayerVisual.cs`:

- Kiếm là **GameObject con riêng**, sprite `Resources/Placeholders/Sword` (6×16, pivot 0.5, 0.12).
- **Không có Weapon Anchor.** Vị trí là hằng số trong code:
  - Nghỉ: `localPosition (-0.15·side, 0.1)`, xoay `30°·side`, sorting = thân − 1.
  - Chém: `localPosition = hướng × 0.2`, sprite **xoay lúc chạy** từ −70° tới +70° (`SwingDuration 0.16 s`), sorting = thân − 1 khi chém lên, thân + 2 ở các hướng khác.
- **`if (_bodyAnim != null && _bodyAttackTimer > 0f) swordRenderer.enabled = false;`**
  - Dòng này viết cho Phase 4, nơi frame Attack đã vẽ sẵn kiếm.
  - R2b vẽ frame Attack **không có kiếm**, nên trong Sandbox kiếm biến mất khi chém.
  - **Bộ art và code đang giả định ngược nhau.**

**Đề xuất (chỉ đặc tả, chưa sửa):**

1. Chọn **một** mô hình:
   - **Mô hình A (khuyến nghị):** kiếm là sprite riêng cho mọi state; thân không bao giờ vẽ kiếm.
     - Có 3 sprite kiếm theo tư thế: chéo xuống, ngang, dựng đứng. Không xoay lúc chạy.
     - Thêm 1 sprite "kiếm trên lưng".
   - **Mô hình B:** kiếm vẽ thẳng vào thân cho mọi state. Đơn giản hơn nhưng không đổi được vũ khí, và sheet 32×32 không chứa nổi kiếm dài 35–38 px.
   - → **Mô hình A gần như bắt buộc** vì độ dài kiếm đo được (≈ 1.7 × thân) vượt ô 32 px.
2. Mỗi frame thân xuất kèm toạ độ **grip** (tay cầm) và **back-mount** (điểm gắn trên lưng), tính bằng px trong ô 32×32. Unity sẽ dùng các điểm này làm Weapon Anchor theo frame (bước code sau, cần duyệt riêng).
3. Thứ tự vẽ:
   - Kiếm trên lưng: sau thân khi nhìn Down / Side, trước thân khi nhìn Up.
   - Kiếm khi chém: theo hướng (giữ logic sorting hiện có).

---

## 8. Giữ lại từ Phase 4

- Ô 32×32, pivot (0.5, 0.0625), PPU 16, tên slice, cấu trúc 3 hướng + lật.
- Nhận diện: tóc trắng dài, đầu chibi ≈ 55%, áo khoác xanh đậm, mắt xanh tím.
- Bố cục mặt và mắt của Phase 4 (gần tham chiếu hơn R2b ở khoảng cách mắt).
- Số frame: Idle 4, Walk 4, Attack 3 (+ windup dùng lại frame 0), Hurt, Death.
- Shadow sprite riêng.
- Logic sorting kiếm theo hướng trong `PlayerVisual`.

## 9. Cần vẽ lại (Canva hoặc pixel tay)

| Hạng mục | Lý do |
|---|---|
| Toàn bộ thân Idle / Walk / Attack | Thân 14×21 thay cho 16×20 / 18×22. Palette HR06. Chân sáng, giày tối. Viền không đen |
| Walk_Side_0–3 | Đổi từ profile nghiêng sang **3/4 mặt trước có kiếm sau lưng** |
| Sprite kiếm | 1 kiếm sau lưng + 3 tư thế chém, dài ≈ 35–38 px (vượt ô 32 → cần ô kiếm riêng 48×48) |
| Attack frames | Bỏ kiếm vẽ sẵn (Phase 4); giữ dáng ngồi thấp của R2b; xuất toạ độ grip |
| (P2) Sprint profile, Roll | Chỉ khi game có cơ chế tương ứng |

Danh sách ưu tiên P0 / P1 / P2: xem `R2_CANVA_HERO_BRIEF.md` mục 5.
