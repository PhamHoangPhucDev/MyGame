# Phó bản và màn hình chọn ải: thiết kế

Ngày: 2026-11-05 · Trạng thái: chờ duyệt · Nhánh: `claude/ui-map-pho-bang-design-348da3`

## 1. Mục đích

Người chơi nói chuyện với **NPC lính canh**. Lính canh mở **màn hình chọn ải**: chọn phó bản, chọn ải, bấm **Dịch chuyển**. Mỗi phó bản là một chuỗi 5 ải: qua ải trước mới mở ải sau, qua ải 5 (boss) thì mở phó bản tiếp theo.

Bối cảnh và nhịp truyện lấy cảm hứng từ 3 video tham khảo của người dùng (đồng cỏ goblin, lều chiến dựng bằng xương có khói tím, tướng goblin trên ngai xương, đấu trường có vòng nghi lễ). **Chỉ học phong cách và nhịp truyện**: art, tên và lời thoại đều làm mới, không đồ lại hình hay chép lời của @baredevs (xem memory *village-style-match-original-art*).

Việc đầu tiên cần làm đẹp là **màn hình chọn ải**. Art của map trong từng ải sẽ là một việc riêng làm sau.

## 2. Mục tiêu / Không làm / Không được thành ra

**Mục tiêu**
- Màn hình chọn ải đẹp, đọc được ngay, đúng style UI video reference và cùng nét với world map ngoài trời đã làm.
- Nhìn vào là biết: đang ở phó bản nào, ải nào đã qua, ải nào đang mở, ải nào còn khoá, ải boss nằm ở đâu.
- Dữ liệu phó bản, ải và cảnh thoại phải cấu hình được, để người dùng tự sắp xếp linh hoạt.

**Không làm (trong đợt này)**
- Art tilemap của 4 kiểu map ải và của phòng boss.
- Minimap trong ải, bảng đếm quái, rương.
- Màn hình dọc 9:16 (đã bỏ, chỉ làm ngang 16:9).

**Không được thành ra**
- Một popup gỗ hoặc giấy da kiểu UI kit cũ.
- Một bản sao của game trong video.
- Bố cục dọc, hoặc chữ nhỏ tới mức không đọc nổi ở 1920×1080.

## 3. Cấu trúc phó bản

| Ải | Nội dung | Map |
|---|---|---|
| 1–4 | Cảnh thoại mở ải → đánh quái, nhặt đồ rơi → tới điểm kết ải | Một trong 4 kiểu map, mỗi kiểu dùng đúng một lần |
| 5 | Cảnh thoại với boss → đánh boss | Map boss cố định của phó bản |

**4 kiểu map cho ải 1–4**
1. **Đường vòng vèo:** lối đất ngoằn ngoèo, quái đứng rải dọc đường.
2. **Mê cung:** tường chia hành lang hẹp, quái nấp ở góc, có ngõ cụt.
3. **Bãi đất trống:** khoảng rộng, quái kéo đến theo bầy.
4. **Đường hẹp ven vực / cầu gỗ:** đi một hàng dọc, quái chặn cả đầu lẫn đuôi. *(Do Claude đề xuất; người dùng chưa phản đối, có thể đổi.)*

**Xáo kiểu map:** mỗi phó bản gán 4 kiểu map cho ải 1–4 theo một thứ tự khác nhau để đỡ nhàm. Ví dụ Trại Goblin: vòng vèo → mê cung → bãi trống → ven vực. *Claude chọn cách:* xáo **một lần khi thiết kế** và lưu vào dữ liệu phó bản, chứ không xáo lại mỗi lần vào. Như vậy màn hình chọn ải có thể hiện đúng biểu tượng map của từng ải.

**Phó bản đầu tiên: Trại Goblin.** Đồng cỏ có goblin cấp thấp, cuối đường là lều chiến dựng bằng xương có khói tím. Ải 5 là phòng ngai: ngai xương có sọ thú sừng to, rèm đỏ tía, nền đất đỏ có vòng nghi lễ. Tên các phó bản sau (Đầm Lầy Mục, Hẻm Núi Xương) chỉ là tên tạm.

## 4. Cảnh thoại (linh hoạt)

Mỗi ải có một **chuỗi cảnh thoại** cấu hình được. Người nói và thứ tự nói do người dùng tự đặt. Các mẫu người dùng muốn có:
- Hero bước vào vùng trống, tự nói một mình → quái nhảy ra.
- Quái nhảy ra trước rồi nói.
- Boss nói trước → hero đáp lại.

Mỗi dòng thoại gồm: người nói (hero / quái / boss / lính canh), câu thoại và một hành động kèm theo nếu có (ví dụ: quái xuất hiện, boss nhảy xuống đấu trường). Hết chuỗi thoại thì mới bắt đầu đánh. Ô thoại dùng style bong bóng trong video reference (nền kem `#F6EFE2`, viền `#2A2228`), chữ hiện dần từng chữ, câu đe doạ của boss có thể in to hơn ở chữ cuối.

## 5. Màn hình chọn ải

Khung ngang 16:9 thiết kế ở 1920×1080. Art vẽ ở độ phân giải thấp rồi phóng lên theo số nguyên, giống world map (art 408×308).

```
┌ Bản đồ phó bản   [Trại Goblin][Đầm Lầy Mục 🔒][Hẻm Núi Xương 🔒]        ✕ ┐
│┌────────── bản đồ phó bản ──────────┐┌── bảng ải ──────┐│
││ ải1 ✓ ─ ải2 ✓ ─ (ải3) ─ ải4🔒       ││ Ải 3 · Đang mở  ││
││            sương tím ─ ải5 lều boss  ││ Tên ải          ││
││                                      ││ [icon kiểu map] ││
││                                      ││ Đề nghị Lv.8    ││
││                                      ││ Phần thưởng □□□ ││
│└──────────────────────────────────────┘│ [ Dịch chuyển ] │└
│ [mặt lính canh] Lính canh: câu thoại theo ải đang chọn            │
└──────────────────────────────────────────────────────────────────┘
```

- **Tab phó bản** ở trên: tab đang chọn có viền vàng `#F2D84A`. Phó bản chưa mở thì chữ xám kèm ổ khoá.
- **Bản đồ:** art pixel riêng của từng phó bản, có đường đất nối 5 ải. Đoạn đường đã đi qua có nét đứt màu vàng.
- **Ghim ải**, 4 trạng thái:
  - đã qua: nền xanh đậm và dấu tick vàng;
  - đang mở: nền vàng, vòng sáng nhấp nháy, mũi tên đỏ chỉ "bạn ở đây";
  - bị khoá: nền xám và ổ khoá;
  - ải 5: biểu tượng lều hoặc sọ boss, phủ sương tím khi còn khoá.
  Mỗi ghim có một **icon nhỏ cho biết kiểu map** của ải đó.
- **Vùng còn khoá** phủ sương tím mờ (cùng cách làm sương của world map và của Home).
- **Bảng ải:** dòng trạng thái màu vàng, tên ải chữ trắng, icon kiểu map, level đề nghị màu cyan `#5ED0E0`, 3 ô phần thưởng (nền `#20202A`, viền `#5A5E6A`), dòng mở khoá màu xanh lá `#7ED67E`. Nút **Dịch chuyển** nền mận `#4A3140`, viền `#B89AA0`. Ải bị khoá thì nút ghi "Bị khoá" chữ xám; ải đã qua thì ghi "Vào lại" (đánh lại để farm).
- **Dải lính canh** ở dưới: ô chân dung, tên màu vàng, câu thoại màu kem, đổi theo ải đang chọn.
- Nền panel `#1C1F26`, hàng `#252A33`, viền `#4A5260` 1px. Chữ pixel hẹp, có viền tối `#141018`.

**Mở màn hình:** nói chuyện với lính canh. *Claude đề xuất:* lúc mở thì tự chọn sẵn ải cao nhất đang mở.

**Bấm Dịch chuyển:** màn hình chuyển cảnh, nhân vật xuất hiện ở đầu map của ải đó, rồi chạy chuỗi cảnh thoại của ải.

## 6. Ràng buộc

- Bám `Docs/UI/Reference/GAME01_UI_VIDEO_REFERENCE.md` (file nằm ở checkout chính, chưa commit vào nhánh này). Chỉ làm ngang 16:9.
- Pixel art không khử răng cưa, lọc Point, phóng lên theo số nguyên; ghim đặt đúng lưới pixel, giống world map.
- Phân vai: Claude vẽ PNG và viết spec; Cursor viết code, ráp scene/prefab và test trong Unity.
- Nên dùng lại khung và logic của `Assets/Scripts/UI/WorldMapPanel.cs` (ghim, vòng sáng, sương, hàng, nút Travel) thay vì làm hệ thống mới. Hướng làm cụ thể do Cursor chọn.

## 7. Art cần vẽ (đợt màn hình chọn ải)

| Asset | Ghi chú |
|---|---|
| `UI_Dungeon_Board_GoblinCamp` | Art bản đồ Trại Goblin, cỡ cùng tầm world map |
| `UI_Dungeon_Fog_GoblinCamp` | Lớp sương tím cho vùng còn khoá |
| `UI_Dungeon_Pin_Cleared / Open / Locked` | Ghim ải 1–4, cùng cỡ ghim world map (13×13) |
| `UI_Dungeon_Pin_Boss` + `UI_Dungeon_Pin_Boss_Locked` | Ghim lều boss |
| `UI_Dungeon_MapType_Winding / Maze / Field / Ledge` | Icon nhỏ cho 4 kiểu map |
| `UI_Dungeon_Tab / Tab_Selected / Tab_Locked` | Tab phó bản, cắt 9-slice |
| `UI_Dungeon_GuardStrip` + `UI_Dungeon_Guard_Portrait` | Dải thoại và chân dung lính canh |
| `UI_Dungeon_RewardSlot` | Ô phần thưởng |

Làm theo quy trình art của dự án: đưa **một mẫu** (art bản đồ + ghim) để người dùng duyệt trước, rồi mới vẽ đủ bộ. PNG xuất ra kèm manifest.

## 8. Những điểm Claude tự chọn (người dùng cần kiểm tra)

- Tiến độ phó bản lưu **riêng** cho từng phó bản (ải đã qua cao nhất), không dùng chung với level của world map.
- Đánh lại ải đã qua vẫn có quái và đồ rơi như bình thường.
- Chỉ mở được màn hình chọn ải từ lính canh, không mở được khi đang ở trong ải.
- Kiểu map thứ 4 là "đường hẹp ven vực".
- Xáo kiểu map một lần khi thiết kế, không xáo lại mỗi lần vào.

## 9. Để người làm tự quyết

- Kiểu dữ liệu cụ thể (ScriptableObject hay bảng) cho phó bản, ải và chuỗi thoại.
- Hiệu ứng chuyển cảnh khi dịch chuyển.
- Vị trí đặt NPC lính canh trong làng hoặc Guild.
- Số liệu level đề nghị và phần thưởng (trên mockup chỉ là số tạm).
