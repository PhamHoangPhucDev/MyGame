# Brief cho Cursor: dựng map Goblin Forest Camp trong Unity

Tài liệu liên quan:
- Spec art: `Docs/superpowers/specs/2026-11-04-outdoor-maps-art-design.md`
- Spec bố cục và gameplay: `2026-11-03-outdoor-training-maps-design.md` (nhánh `claude/map-train-outdoor-regions-701d4d`)

Phân vai: Claude đã vẽ toàn bộ PNG trong thư mục này. Cursor lo import, dựng scene, viết code và test. Không sửa PNG. Nếu cần chỉnh art thì báo lại để Claude vẽ lại.

## 1. Có gì trong thư mục

| Đường dẫn | Nội dung |
|---|---|
| `Camp_ArtKit_Board.png` | Ảnh xem trước toàn bộ kit, phóng ×3. Chỉ để xem, **không import**. |
| `Camp_ArtKit_Manifest.csv` | Mỗi file một dòng: kích thước, số frame, kích thước frame, pivot (tỉ lệ 0–1, gốc ở góc dưới trái), ghi chú |
| `Camp/Camp_Overview.png` | Toàn map 1600×1120 px chụp từ demo đã duyệt, tỉ lệ 1:1 |
| `Camp/Camp_Overview_Annotated.png` | Ảnh trên có ghi chú vùng, chấm quái và chấm điểm tài nguyên |
| `Camp/Camp_Layout.json` | Tọa độ của 940 vật thể (cây, bụi, đồ, điểm tài nguyên), 23 quái (cấp, tâm vùng đi lại, bán kính, đường tuần tra) và các hình địa hình |
| `Shared/…` | Đồ dùng chung cho cả 5 map: vách đá, thác, hang, lửa, hạt, cổng, rương, cầu, điểm tài nguyên |
| `Camp/Ground`, `Camp/Trees`, `Camp/Props` | Những thứ phụ thuộc bảng màu của Camp: mặt đất, cây, bụi, lều, cờ, bục cung thủ, lều rách |
| `demo/outdoor2.html` | Demo chạy được, dùng để so hình và chuyển động. Đây là tham chiếu, **không phải code để port** |

## 2. Import

- PPU 16. Filter Point, không nén, không tạo mipmap. Giống `Goblin_Goblin_Sheet.png` hiện có.
- File nhiều frame cắt theo `frame_w × frame_h` ở cột tương ứng trong manifest. Strip thì xếp ngang, các file `Dual_*` và `TallGrass` thì xếp lưới 4 hoặc 3 cột.
- Pivot lấy theo manifest. Mặc định là `0.5,0`, tức chính giữa đáy.
- Thư mục đích trong `Assets/Art/` do Cursor chọn. Không đổi GUID của asset đã có.

## 3. Mặt đất: dual-grid 16 ô

- Mỗi file `Dual_<A>To<B>.png` là lưới 4×4 ô, mỗi ô 16 px. Chỉ số ô:

  `index = TL + 2·TR + 4·BL + 8·BR`

  Trong đó 1 nghĩa là góc đó thuộc vật liệu B. Ô thứ `index` nằm ở cột `index % 4`, hàng `index / 4`.
- Lưới dual-grid lệch nửa ô so với lưới dữ liệu: mỗi ô hiển thị đọc 4 ô dữ liệu ở 4 góc của nó.
- Thứ tự lớp gợi ý:
  1. `Fill_Grass` (4 biến thể, rải ngẫu nhiên)
  2. `Dual_GrassToDark` để vẽ các mảng cỏ tối lớn
  3. `Dual_DarkToForest_Overlay` cho nền rừng sâu sát mép
  4. `Dual_GrassToDirt_Overlay` cho đất
  5. `Dual_GrassToWater_Overlay` cho nước
  6. `Dual_DirtToPave_Overlay` (Camp chưa dùng)
- Các bản `_Overlay` để trống phía A, nên chồng lên lớp dưới không bị viền cỏ sai màu. Mình đã ghép thử 26×15 ô có cỏ tối, đất và nước: các mép tile liền nhau.
- Mảng cỏ tối cần **to và có hình hữu cơ**, xem `Camp_Overview.png`. Không rải lốm đốm từng ô.
- Decal rải lên trên nền:
  - `Decals.png` (10 frame: nét cỏ, hoa, nấm, sỏi, xương) và `Deco_0..8`.
  - Hoa đặt thành cụm 4–9 bông.
  - Mật độ tham khảo: khoảng 1 nét cỏ trên 110 px² cỏ.
- Cỏ cao lấy từ `TallGrass.png`: hàng 0 dùng trên cỏ sáng, hàng 1 trên cỏ tối. Frame chạy theo một pha gió chung `sin(t·2.2 − x·0.045 − y·0.01)`. Nếu nhân vật đứng trong vòng 9 px thì cỏ ngả sang phía ngược nhân vật.

## 4. Vách đá và nước

- `Cliff_Face`: dải 16×56, có 6 biến thể, ghép ngang dọc mép trên map.
  - Hàng 0–5 là cỏ trên đỉnh vách, hàng 52–55 là bóng vách đổ xuống cỏ.
  - Cho từng ô lệch 1–3 px theo chiều dọc để mép vách răng cưa như demo.
  - Treo `Vine_0..3` ở mép cỏ.
- Hàng cây trên đỉnh vách: dùng `Tree_TealDark` và `Tree_LeafDark`, đặt sát mép.
- `Waterfall`: 6 frame, 12 fps, đặt tại x = 1296 theo `terrain.waterfallX`. Dưới chân thác đặt `Waterfall_Foam` 4 frame và phát hạt `Mist_Blob` hoặc `Px_White`.
- Suối và hồ dùng `Dual_GrassToWater_Overlay`.
  - Vệt lấp lánh trên nước: hạt `Px_White` màu `#cfe9e2`.
  - Gợn tròn trên hồ: `Ripple`, mỗi gợn 1,6 s.
- Cầu: ghép `Bridge_Deck_H` theo bề ngang suối, mép trên đặt `Bridge_Rail_H`. Tọa độ cầu nằm ở `terrain.bridges` trong `Camp_Layout.json`.

## 5. Cây, bụi, đồ

- Mỗi cây gồm 2 sprite **cùng kích thước 80×72 và cùng pivot**: `_Trunk` đứng yên, `_Canopy` đung đưa tối đa 0,55 px theo `sin(t·1.7 − x·0.022 − y·0.008) + 0.6·sin(t·0.6 − x·0.006)`.
  - Khi nhân vật đứng sau cây (lệch ngang dưới 30 px, cao hơn gốc từ 4 đến 64 px), tán mờ xuống 42%, chuyển dần trong khoảng 0,12 s.
  - Collider của cây là elip bán kính 7×4 ở gốc. Bóng cây dùng `Tree_Shadow`.
- Bụi: 18 hàng px trên đung đưa 0,35 px, 8 hàng dưới đứng yên. Collider bán kính 9×4.
- Mọi vật đứng đều có bóng elip `rgba(14,30,30,0.34)` dưới chân. Vẽ theo trục Y.
- `Flag_Tattered`: lượn sóng theo từng cột, độ lệch dọc là `sin(t·4.2 − col·0.45)·1.3`. Các cột 0–3 (cán cờ) đứng yên.
- `Archer_Platform`: Goblin Archer đứng cao hơn pivot 30 px, không di chuyển, bắn tên khi nhân vật cách dưới 130 px.

## 6. Lửa và không khí

- Đống lửa trung tâm: `FireRing_Big` dưới đất, `Fire_Big` 10 fps, `Glow_Warm` cộng sáng với độ sáng nhấp nháy 0.85–1.0.
  - Hạt tia lửa: khoảng 16 hạt/s, màu `#ffd36a`, `#ff9a3c`, `#fff0b0`, bay lên 16–30 px/s.
  - Khói `Smoke`: khoảng 3 hạt/s, sống 3 s.
- Lá rơi `Leaf` từ những tán cây đang trong màn hình: khoảng 5 lá/s, có lắc ngang theo sin.
- Bóng mây `Cloud_Shadow`: 6 đốm, phóng to 3–5 lần, alpha 0.14, trôi theo vận tốc (9, 3) px/s.
- Vệt gió `Wind_Streak`: tối đa 7 vệt cùng lúc, vận tốc (150, 38) px/s.
- Hạt bụi bay `Px_White` màu `#f4f2d0`.
- Cổng ⛩: `Portal_Frame` và `Portal_Swirl` cộng sáng, 8 fps, kèm hạt sáng xoay quanh.
- Rương ẩn có tia lấp lánh khoảng 1,6 lần/s. Điểm thảo dược cũng lấp lánh.
- Cách làm các hiệu ứng này (particle system, Light2D hay sprite) do Cursor chọn, nhưng phải giữ nguyên render pipeline.

## 7. Bố cục Camp và các chỗ phải sửa so với demo

- Đặt đồ theo `Camp_Layout.json`, đơn vị px, gốc tọa độ ở góc trên trái.
  - Đổi sang tọa độ Unity: `worldX = x/16`, `worldY = −y/16`.
  - `layer`: `plateau` là cây trên đỉnh vách, `tree`, `prop`, `node`.
  - Có thể đặt tay lại cho đẹp, miễn giữ đúng các vùng như trong `Camp_Overview_Annotated.png`.
- **Ba chỗ phải sửa so với demo, theo spec bố cục:**
  1. Điểm tài nguyên: đổi `Stone_Full` thành `Wood_Full` (Gỗ) và `Herb_Full` thành `Hide_Full` (Da thú). Mỗi loại có bản `_Depleted` hiện ra trong lúc chờ hồi.
  2. Boss "Goblin Chief": demo dùng sheet Warlord, bản thật dùng **Goblin phóng to**, chỉ số theo spec bố cục.
  3. Bí mật: đặt `Tent_Ruined` cùng `Chest_Closed` tại `secretChest`, nằm trong vòng bụi. Mở rương thì đổi sang `Chest_Open`.
- Cung thủ đứng trên `Archer_Platform` cạnh các lều, không đứng dưới đất.

## 8. Cách nghiệm thu

1. Chụp màn hình Unity ở 1280×720, cùng 4 vị trí của demo: cổng, sân trại, thác và cầu, bãi tài nguyên tây bắc.
2. Đặt cạnh ảnh chụp demo (`demo/outdoor2.html?map=0`). Màu, độ dày cây cối và chuyển động phải giống.
3. Kiểm tra mép tile không có khe hở hay đường sai màu khi camera di chuyển, và sprite không bị mờ.
