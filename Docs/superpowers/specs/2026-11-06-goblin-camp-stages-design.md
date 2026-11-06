# Trại Goblin: 5 ải (thiết kế từng map và UI trong trận)

Ngày: 2026-11-06 · Landscape 16:9 · Nguồn sự thật cho tên, kiểu, Lv và lời lính canh: `Assets/Scripts/Level/DungeonCatalog.cs` (đã có).
Màn chọn ải: `DungeonSelectPanel` (đã có). Spec trước: `2026-11-06-dungeon-map-ui-design.md` (phần tên ải và thưởng trong spec đó được thay bằng file này).

> Phân vai: Claude viết spec và làm art/VFX sheet. **Cursor viết code Unity, scene và test.** Các mục ghi **[Cursor]** là việc code.

---

## A. Phần còn thiếu của hệ thống phó bản hiện có

| # | Thiếu | Hậu quả hiện tại | Sửa |
|---|---|---|---|
| A1 | Ải không khớp map. `Go()` nạp `LevelConfig` số N | Ải 1 "Đường đất đỏ · Đường vòng vèo" lại mở trại vòng tròn `Camp`. Ải 2 "Hàng rào nhọn · Mê cung" lại mở đường mòn `Trail` | **[Cursor]** Thêm bảng ải → layout + wave trong `DungeonCatalog` (mục C). Không dùng số level campaign nữa |
| A2 | Banner đầu trận ghi `LEVEL 1 / GOBLIN FOREST CAMP` (tiếng Anh, tên campaign) | Lệch với tên ải người chơi vừa chọn | **[Cursor]** Khi `DungeonRun.Active`: banner `Ải N` / `<tên ải>`, plate góc trái `Ải N · <tên>` |
| A3 | Phần thưởng chỉ có chữ "Vàng/Gỗ/Ngọc", không có số, và không được trao khi qua ải | Ô thưởng vô nghĩa | **[Cursor]** `DungeonStage.rewards` thành `(itemId, amount)` theo mục C. Qua ải thì trao qua `PendingRewards`/`RewardBanner`. Lần đầu nhân 2 |
| A4 | Màn thắng: nút Next đi theo `nextLevelNumber` của campaign | Bấm Next nhảy sai ải | **[Cursor]** Khi `DungeonRun.Active`: Next = ải N+1 cùng phó bản (nếu đã mở), thêm nút "Về làng" |
| A5 | Kỷ lục, sao và thời gian đang ghi theo `levelNumber` campaign | Phó bản 2, 3 dùng lại map nhưng ghi chung kỷ lục | **[Cursor]** Key kỷ lục theo `dg.best.{dungeon}.{stage}` |
| A6 | 4 phần bổ sung trên màn chọn: dòng thử thách, dòng kỷ lục, phím A/D/←/→/Enter, Lv đỏ | — | Đã có brief và test nháp: `Docs/superpowers/plans/DungeonSelectExtrasTests.cs.txt`. Dòng thử thách đọc wave từ bảng mới ở mục C |
| A7 | Chưa có map kiểu Mê cung (ải 2) | Không có layout nào khớp | Art mới (Claude) + root mới `FenceMazeRoot` **[Cursor]** |
| A8 | Đầm Lầy Mục và Hẻm Núi Xương chỉ là bản sao tên | Tab mở rồi nhưng nội dung giống hệt | Để sau. Giữ khóa tới khi có spec riêng |

---

## B. UI chung trong trận (mọi ải)

Toạ độ tham chiếu 1920×1080 (canvas 640×360 ×3, giống world map).

| Vùng | Nội dung | Ghi chú |
|---|---|---|
| Trên-trái (dưới thanh HP) | Plate `Ải 3 · Bãi lửa trại` + `Wave 2/4` | Dùng `WavePlate` đang có, đổi text |
| Trên-giữa | Mục tiêu ngắn: `Diệt hết quái` / `Tới cổng thoát` / `Hạ boss` | `ObjectivePlate` đang có |
| Trên-phải | Minimap (đã có `MiniMapHud`) + đồng hồ `02:14` + mốc 3 sao `≤ 02:30` | Đồng hồ chuyển vàng rồi đỏ khi quá mốc |
| Giữa màn | Banner mở đầu `Ải N` + tên, 1.6 s. Banner wave `WAVE 2 — <tên wave>` | Tên wave lấy từ bảng C |
| Dưới | Hotbar skill (đã có) | Không đổi |
| Riêng từng ải | Xem mục C, phần "UI riêng" | Mỗi ải có 1 thành phần UI đặc trưng |

**Thắng:** bảng kết quả gồm sao, điểm, thời gian, thưởng (icon + số) và 3 nút `Ải tiếp` / `Chơi lại` / `Về làng`.
**Thua:** nút `Thử lại` / `Về làng`. Kèm 1 dòng gợi ý lấy từ lời lính canh của ải đó.

---

## C. Thiết kế từng ải

Quái đang có trong code: goblin, archer, gobrider, elite gobrider, satyr, dryad, dryad rider, warlord, cyclops, boss warlord.
Wave ghi theo dạng `goblin+archer+rider(elite)`. Thời gian là mốc 3 sao.

### Ải 1 · Đường đất đỏ (Đường vòng vèo) · Lv 3
- **Map:** dùng lại root `Trail` (đường mòn rừng). Một vệt đất đỏ uốn chữ S từ cổng vào ở phía nam tới cổng trại ở phía bắc. Hai bên là rừng rậm, không đi xuyên được. Có 3 khoảng trống dọc đường làm chỗ đánh.
- **Mục tiêu:** dọn wave rồi tới cổng (`ClearWavesThenReachExit`).
- **Wave (4):**
  1. Trinh sát: 3 goblin
  2. Cung bên đường: 2 goblin + 2 archer
  3. Chặn khúc cua: 3 goblin + 2 archer
  4. Đội gác cổng: 4 goblin + 2 archer
- **Mốc 3 sao:** 02:20
- **UI riêng:** mũi tên chỉ đường dạng vệt đất phát sáng nhẹ trên nền, hiện sau khi dọn xong mỗi khoảng trống. Thêm icon cổng ở mép màn khi cổng nằm ngoài khung hình.
- **Thưởng:** Vàng 120 · Gỗ 6 · Ngọc 1
- **Art cần:** decal vệt đất đỏ kiểu 9-slice theo đoạn, mũi tên chỉ đường (4 frame), icon cổng ở mép màn.

### Ải 2 · Hàng rào nhọn (Mê cung) · Lv 5
- **Map mới `FenceMaze`:** lưới khoảng 5×4 ô làm từ hàng rào cọc nhọn, có 2 ngõ cụt chứa rương nhỏ. Lối ra nằm ở góc đối diện lối vào. Cọc nhọn gây 5% HP khi chạm.
- **Mục tiêu:** dọn wave rồi tới cổng.
- **Wave (4):**
  1. Gác ngõ: 3 goblin
  2. Phục kích sau rào: 2 goblin + 3 archer
  3. Kỵ tuần: 2 goblin + 2 rider
  4. Chặn lối ra: 3 goblin + 2 rider + 1 archer
- **Mốc 3 sao:** 02:40
- **UI riêng:** minimap ở chế độ mê cung. Ô chưa đi qua bị phủ tối, ô đã đi lộ ra. Ngõ cụt đã thấy được đánh dấu ✕. La bàn nhỏ chỉ hướng lối ra.
- **Thưởng:** Vàng 160 · Gỗ 10 · Ngọc 1. Rương ngõ cụt cho thêm Vàng 30 mỗi rương.
- **Art cần:** tile hàng rào cọc (thẳng, góc, chữ T, cổng), cọc nhọn hazard, rương nhỏ, icon ✕ trên minimap, la bàn.

### Ải 3 · Bãi lửa trại (Bãi đất trống) · Lv 8
- **Map:** dùng lại root `Camp`. Bãi trống hình tròn có lửa trại ở giữa (`FireRing` đã có) và 3 lều xung quanh, khớp mockup đã chốt.
- **Mục tiêu:** dọn sạch wave (`ClearWaves`).
- **Wave (4), quái tràn vào từ 4 phía:**
  1. Lửa bùng: 4 goblin
  2. Vây trái phải: 3 goblin + 3 archer
  3. Kỵ xông: 2 goblin + 3 rider
  4. Vòng vây: 3 goblin + 2 archer + 2 rider + 1 elite
- **Mốc 3 sao:** 02:50
- **UI riêng:** cảnh báo hướng vây. Mũi tên đỏ ở mép màn chỉ phía quái sắp tràn vào, hiện 1.5 s trước khi quái ra. Có vòng đỏ mờ quanh Hero khi bị vây từ 3 phía trở lên.
- **Thưởng:** Vàng 220 · Gỗ 12 · Ngọc 2
- **Art cần:** mũi tên cảnh báo ở mép màn (3 frame nhấp nháy), vòng vây mờ quanh Hero.

### Ải 4 · Cầu vực (Ven vực) · Lv 11
- **Map:** dùng lại root `GoblinOutpost` (khe vực). Thêm một cầu gỗ dài 3 đoạn bắc qua vực, đầu bên kia là tiền đồn. Cầu hẹp, hai bên là vực.
- **Mục tiêu:** dọn wave rồi tới cổng.
- **Wave (3), mỗi đoạn cầu một wave:**
  1. Đầu cầu: 4 rider (2 elite)
  2. Giữa cầu: 2 archer bắn từ bờ bên kia + 2 rider elite + 1 warlord
  3. Cuối cầu: 1 cyclops
- **Mốc 3 sao:** 03:30
- **UI riêng:** thanh tiến độ cầu ở trên-giữa, thay cho ObjectivePlate: `▮▮▯ Đoạn 2/3`. Khi đứng sát mép cầu, viền màn ửng đỏ. Cyclops có thanh máu boss nhỏ (dùng `BossHudUI`).
- **Thưởng:** Vàng 320 · Gỗ 14 · Ngọc 3
- **Art cần:** cầu gỗ theo đoạn (đầu, giữa, cuối, phần vỡ), thanh tiến độ cầu, hiệu ứng viền đỏ cảnh báo.

### Ải 5 · Lều xương (Lều boss) · Lv 14
- **Map:** dùng lại root `GobriderStronghold`. Cuối đường có lều xương tím, giữa sân là vòng nghi lễ bằng xương.
- **Mục tiêu:** hạ Tướng Gobrider (`DefeatBossChapterComplete`).
- **Wave (3):**
  1. Lính gác: 4 goblin + 2 archer
  2. Cận vệ: 3 goblin + 2 rider elite
  3. Tướng Gobrider: boss warlord ×1. Mỗi lần mất 33% máu thì gọi 2 goblin.
- **Mốc 3 sao:** 04:40
- **UI riêng:** thanh máu boss lớn ở trên-giữa (`BossHudUI`), có tên `Tướng Gobrider` và 3 vạch pha (66% / 33%). Trước mỗi đòn mạnh, vòng nghi lễ trên nền sáng đỏ (telegraph). Hội thoại mở đầu dùng `DungeonTalk` (đã có).
- **Thưởng:** Vàng 500 · Gỗ 20 · Ngọc 5. Lần đầu mở **Đầm Lầy Mục**.
- **Art cần:** vòng nghi lễ xương (nền + telegraph đỏ 4 frame), 3 vạch pha trên thanh máu boss, banner "Chương hoàn thành".

---

## D. Dữ liệu cho Cursor (đề xuất cấu trúc)

```
DungeonStage += layoutId (string), objective (LevelObjectiveType), waves (WaveConfig[]),
                threeStarSeconds (int), rewards (string itemId, int amount)[3]
Go(): dùng stage.layoutId + stage.waves thay vì GetLevelConfig(stage).
       Cách dễ nhất: clone LevelConfig của layout tương ứng ở runtime, rồi ghi đè waves/objective/time.
Không đổi field serialized của LevelConfig. Không sửa asset Level*.asset.
```

Bảng ải → layout: 1 `Trail` · 2 `FenceMaze` (mới) · 3 `Camp` · 4 `GoblinOutpost` · 5 `GobriderStronghold`.

## E. Thứ tự làm

1. **[Cursor]** A6, rồi A1 + A2 + A3 + A4 + A5 (chỉ code, dùng map có sẵn). Ải 2 tạm dùng `Trail` cho tới khi có map mê cung.
2. **[Claude] ✅ Xong:** art ải 1, 3, 4, 5 nằm trong `Docs/WorldArt/DungeonStages/Stage{1,3,4,5}/`. Mỗi thư mục có README ghi thông số import và cách dùng.
3. **[Claude]** Art tile mê cung (ải 2). **[Cursor]** dựng root `FenceMaze`.
4. **[Cursor]** UI riêng từng ải theo mục C.

## F. Quyết định của Claude (bạn kiểm tra giúp)

- Ghép ải với layout có sẵn: ải 3 dùng `Camp`, vì trại này có lửa trại, khớp tên "Bãi lửa trại" và mockup. Ải 1 dùng `Trail`.
- Toàn bộ wave, mốc 3 sao, số phần thưởng và cơ chế "mỗi ải có 1 UI riêng".
- Ải 2 cần map mới. Đây là phần tốn công nhất.
