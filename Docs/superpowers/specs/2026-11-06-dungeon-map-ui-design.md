# Bản đồ phó bản (Dungeon Map) — Thiết kế chi tiết

Ngày: 2026-11-06 · Landscape 16:9 (1920×1080 ref) · Mockup đã chốt: `Docs/UI/mockups/landscape/L07b_Dungeon_Map_LOCKED.png`

> Màn này thay bảng chọn dạng danh sách cũ (`L07_Dungeon_Select.png`, `LevelFlowUI.BuildLevelSelect`).
> **Giữ nguyên bố cục mockup đã chốt.** Không chia lại 3 cột. Không đổi style.

## 1. Mục đích

Người chơi ở Village gặp **Lính canh (Warden)** và nhấn `E`. Họ cần thấy ngay 3 điều:
1. Ải nào đã mở, ải nào đã xong.
2. Ải tiếp theo khó cỡ nào và cho phần thưởng gì.
3. Nút để vào đánh.

Luồng: chọn ghim → xem nhanh → **Dịch chuyển** → bảng đóng → Hero vào map → Wave 1.

- **Mục tiêu:** vào trận trong tối đa 2 click (mở bảng đã chọn sẵn ải tiếp theo, chỉ cần bấm Dịch chuyển).
- **Không làm:** menu con, popup xác nhận, đoạn chữ dài, màn portrait.
- **Coi là hỏng nếu:** mở bảng mà phải tự tìm ải tiếp theo, hoặc bấm được Dịch chuyển vào ải đang khóa.

## 2. Bố cục (theo mockup đã chốt, toạ độ ref 1920×1080)

| Vùng | Rect (x,y,w,h) | Nội dung |
|---|---|---|
| Tiêu đề | 44,30 | "Bản đồ phó bản" (vàng) |
| Tab vùng | 560–1490, 28, h 56 | 3 tab: Trại Goblin / Đầm Lầy Mục 🔒 / Hẻm Núi Xương 🔒 |
| Nút đóng | 1875,52 | `X`, cũng đóng bằng `Esc` / `E` |
| Bản đồ | 36,106,1232,936 | Art vùng + đường + ghim ải + sương tím vùng khóa |
| Khung thông tin | 1300,106,580,936 | Xem §4 |

## 3. Ghim ải trên bản đồ

Toạ độ ghim **chuẩn hoá theo khung bản đồ** (0..1, gốc trên-trái), snap theo lưới pixel (giống world map).

| Trạng thái | Hình ghim | Bấm được |
|---|---|---|
| Đã xong | Ô số viền xanh + dấu ✓ | Có (chơi lại) |
| Đang mở (chưa xong) | Ô số viền vàng, nhấp nháy nhẹ 1 Hz | Có |
| Đang chọn | Viền vàng dày + mốc đỏ phía trên | — |
| Khóa | Icon 🔒 trong vùng sương tím | Có, xem được thông tin, nhưng nút bị tắt |
| Boss | Icon sọ thay số | Như trên, tuỳ trạng thái |
| COMING SOON | 🔒 + nhãn nhỏ "Sắp ra mắt" | Xem được thông tin, nút bị tắt |

Dưới mỗi ghim có một icon nhỏ cho loại địa hình. Ải đã xong và ải đang mở nối nhau bằng đường đất. Đoạn đường tới ải khóa bị sương tím che.

**Khi mở bảng, tự chọn:** ải nhỏ nhất đã mở mà chưa xong. Nếu đã xong hết thì chọn ải cao nhất đã mở.

## 4. Khung thông tin (từ trên xuống)

1. `Ải N · <trạng thái>`, màu vàng. Trạng thái: Đang mở / Đã hoàn thành / Đã khóa / Sắp ra mắt.
2. **Tên ải**, chữ lớn.
3. Icon và tên địa hình.
4. `Đề nghị Lv.X`, màu xanh dương. Chuyển đỏ nếu level Hero < X − 2.
5. **Dòng thử thách (thêm vào mockup, 1 dòng nhỏ):** `⚔ 4 wave · ☠ <Boss hoặc —> · 🎯 <mục tiêu ngắn>`
6. **Dòng kỷ lục (chỉ hiện khi đã xong ít nhất một lần):** `★★☆  ·  1174 điểm  ·  02:21`. Lấy từ `GameProgress` (sao, điểm, thời gian tốt nhất).
7. Đường kẻ.
8. **Phần thưởng:** 3 ô icon 96×96, hover/giữ để xem tooltip tên và số lượng. Lần xong đầu tiên có viền vàng "Lần đầu". Đã nhận rồi thì icon mờ.
9. Gợi ý tiến trình, màu xanh lá: `Hạ ải N để mở ải N+1`. Ải cuối thì ghi `Hạ boss để mở Đầm Lầy Mục`.
10. **Nút Dịch chuyển** (580×92). Khi bị tắt, chữ ghi lý do: `Đã khóa` / `Sắp ra mắt`.
11. **Khung Lính canh:** chân dung và 3 dòng thoại riêng cho từng ải (§5).

Phím tắt: `←/→` hoặc `A/D` để chuyển ải, `Q/E` để chuyển tab, `Enter`/`Space` để Dịch chuyển, `Esc` để đóng.

## 5. Chi tiết từng ải — Vùng 1: Trại Goblin

Số wave, quái và thời gian mục tiêu lấy từ `Assets/Data/Levels/*.asset`. Tên tiếng Việt, Lv đề nghị, phần thưởng và thoại là **thiết kế mới**.

### Ải 1 — Trại Rừng Goblin (`GOBLIN FOREST CAMP`)
- Ghim: **(0.13, 0.85)**, đầu đường, cạnh hàng rào và cờ đỏ. Địa hình: *Bìa rừng*.
- Mở sẵn. Đề nghị **Lv.1**. Mục tiêu: *Dọn sạch 4 wave*.
- Wave: (1) First Contact 2 goblin + 1 cung · (2) Camp Defense 3+2 · (3) Archer Line 3+2 · (4) Final Assault 5+3. Boss: —
- Thời gian 3 sao: 02:30.
- Thưởng: Vàng ×100 · Gỗ ×5 · EXP ×50. Lần đầu: +Bình máu ×2.
- Lính canh: "Bọn goblin vừa dựng trại ở bìa rừng. / Chúng có cung thủ núp sau. / Diệt cung trước nhé."

### Ải 2 — Đường Mòn Rừng (`FOREST TRAIL`)
- Ghim: **(0.36, 0.59)**, trong pháo đài gỗ hình mê cung. Địa hình: *Mê cung gỗ*.
- Mở khi xong ải 1. Đề nghị **Lv.4**. Mục tiêu: *Dọn 4 wave rồi tới cổng thoát*.
- Wave: (1) Trail Scouts 2 goblin + 1 cung · (2) Flank Ambush 2+2 · (3) Rider Patrol 2 + 1 gobrider + 1 cung · (4) Path Blockade 2 + 2 gobrider + 1 cung. Boss: —
- Thời gian 3 sao: 02:20.
- Thưởng: Vàng ×150 · Gỗ ×8 · Mảnh trang bị ×1. Lần đầu: +Đá cường hoá ×1.
- Lính canh: "Đường mòn bị chặn bằng hàng rào. / Kỵ binh goblin đi tuần ở đó. / Dọn xong thì chạy ra cổng."

### Ải 3 — Bãi Lửa Trại (`ABANDONED CAMP`)
- Ghim: **(0.56, 0.76)**, bãi đất trống có lều và lửa trại. Địa hình: *Bãi đất trống*.
- Mở khi xong ải 2. Đề nghị **Lv.8** (theo mockup). Mục tiêu: *Dọn sạch 4 wave*.
- Wave: (1) Abandoned Guards 2 satyr + 1 dryad · (2) Side Ambush 2 + 2 + 1 dryad rider · (3) Rider Arrival 2+2+2 · (4) Final Defense 2+2+2. Boss: —
- Thời gian 3 sao: 02:40.
- Thưởng: Vàng ×220 · Gỗ ×10 · Ngọc xanh ×1. Lần đầu: +Trứng pet (mảnh) ×3.
- Lính canh: "Bọn goblin dựng lửa trại / ngay giữa bãi trống. / Coi chừng bị vây đấy." (giống mockup)

### Ải 4 — Tiền Đồn Goblin (`GOBLIN OUTPOST`)
- Ghim: **(0.78, 0.56)**, cổng gỗ bên khe vực, nằm trong sương tím cho tới khi mở. Địa hình: *Khe vực*.
- Mở khi xong ải 3. Đề nghị **Lv.11**. Mục tiêu: *Dọn 3 wave rồi tới cổng thoát*.
- Wave: (1) Elite Gate 4 gobrider (4 elite) · (2) Warlord 2 gobrider elite + **1 Warlord** · (3) **Cyclops ×1**. Boss: Cyclops.
- Thời gian 3 sao: 03:30.
- Thưởng: Vàng ×320 · Mảnh trang bị ×3 · Đá cường hoá ×2. Lần đầu: +Rương bạc ×1.
- Lính canh: "Tiền đồn canh khe vực. / Kỵ binh tinh nhuệ giữ cổng, / sau đó là một gã Cyclops."

### Ải 5 — Thành Trì Gobrider (`GOBRIDER STRONGHOLD`) — BOSS
- Ghim: icon sọ **(0.86, 0.39)**, chân núi lều tím. Địa hình: *Thành trì*.
- Mở khi xong ải 4. Hiện tại là **COMING SOON** nếu `GetLevelConfig(5)` chưa có. Đề nghị **Lv.14**. Mục tiêu: *Hạ Gobrider Warlord*.
- Encounter: (1) Stronghold Guards 4 goblin · (2) Inner Defense 5 goblin + 1 gobrider · (3) **Warlord Battle: Boss Gobrider Warlord ×1**.
- Thời gian 3 sao: 04:40.
- Thưởng: Vàng ×500 · Rương vàng ×1 · Ngọc tím ×1. Lần đầu: +Mở vùng **Đầm Lầy Mục**.
- Lính canh: "Thủ lĩnh Warlord ngồi trên đỉnh thành. / Hạ hắn là trại goblin tan rã. / Chuẩn bị kỹ rồi hẵng đi."

### Vùng 2–3 (tab khóa)
- **Đầm Lầy Mục**: mở khi xong ải 5. **Hẻm Núi Xương**: mở khi xong vùng 2.
- Bấm tab khóa: rung tab và hiện toast `Hạ boss Trại Goblin để mở`. Không đổi bản đồ.

## 6. Dữ liệu và luồng (cho người làm code)

- Nguồn dữ liệu: `LevelConfig` (wave, mục tiêu, thời gian) và `GameProgress` (`IsLevelUnlocked`, `GetBestStars`, `GetBestScore`, thời gian tốt nhất).
- Dữ liệu chỉ thuộc UI (tên VN, ghim, địa hình, Lv, thưởng, thoại) để trong một bảng dữ liệu UI riêng. **Không đổi field serialized của `LevelConfig`.**
- Dịch chuyển gọi `LevelFlow.StartLevelByNumber(n)`, giống nút PLAY hiện tại. Chuỗi hiển thị đi qua `Loc`.
- Mở bảng: tương tác `E` với Warden trong Village, dùng cùng đường đã có.

## 7. Art demo (dùng tài sản có sẵn)

- Bản đồ: tạm dùng art outdoor/world map có sẵn, sương tím dùng lại `HomeFogView` (mist v3) đổi tint tím.
- Icon: `UI_Icon_Dungeon`, `Lock`, `Boss`, `UI_Star_Large_*` lấy từ UI kit.
- Phần thưởng: icon có sẵn trong kit. Thiếu thì dùng ô màu placeholder như mockup.
- Chân dung Lính canh: sprite NPC Warden đang có.

## 8. Quyết định của Claude (bạn kiểm tra giúp)

1. Tên tiếng Việt cho ải 1, 2, 4, 5. Ải 3 lấy theo mockup.
2. Lv đề nghị 1 / 4 / 8 / 11 / 14.
3. Toàn bộ số phần thưởng và thoại Lính canh (trừ ải 3).
4. Thêm dòng thử thách và dòng kỷ lục vào khung phải, cỡ chữ nhỏ, không đổi bố cục.
5. Ghim ải 4–5 đặt theo icon khóa và icon sọ có sẵn trên mockup.
6. Quy tắc tự chọn ải và các phím tắt.
