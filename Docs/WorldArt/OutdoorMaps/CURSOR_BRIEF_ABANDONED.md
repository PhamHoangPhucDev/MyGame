# Brief cho Cursor: dựng map Abandoned Camp trong Unity

Đọc `CURSOR_BRIEF_CAMP.md` trước. Phần import, tile dual-grid, cây tách lớp, gió, lửa, hạt và cách nghiệm thu áp dụng nguyên cho map này. Brief này chỉ ghi phần khác.

Spec art: `Docs/superpowers/specs/2026-11-04-outdoor-maps-art-design.md`, mục 5 và mục 6, dòng Abandoned.

## 1. Những gì có trong bộ

| File | Dùng để |
|---|---|
| `Abandoned/Ground/*` | Mặt đất đã đổi màu theo bảng của Abandoned: cỏ ngả xám-lam, đất xám. Gồm `Fill_*`, `Dual_*` và `_Overlay`, cỏ cao, decal (hoa tím và xanh), `Deco_*`. Thay cho bộ của Camp. |
| `Abandoned/Trees/*` | 5 loại cây (tán và gốc tách lớp) và 3 loại bụi, đã giảm độ bão hòa |
| `Abandoned/Props/SacredTree_Canopy.png` / `_Trunk.png` | Cây thiêng 176×160. Hai lớp cùng kích thước và cùng pivot. |
| `Abandoned/Props/Menhir_0..16.png` | 17 phiến đá dựng cao 22–30 px, có rêu và rune: 11 phiến quanh cây thiêng, 6 phiến ở vòng đá cổ (bãi 5). Mỗi phiến một vị trí trong layout. |
| `Abandoned/Props/Menhir_RuneGlow.png` | Ánh rune 4 frame, cộng sáng lên trên các phiến đá |
| `Abandoned/Props/Tent_Ruined.png` | Lều sừng rách, bạc màu, có rêu |
| `Abandoned/Abandoned_Overview*.png`, `Abandoned_Layout.json` | Toàn map 1600×1120 px, kèm tọa độ 923 vật thể và 31 quái (Dryad Rider có thông số `orbit`) |
| `Abandoned_ArtKit_Board.png`, `Abandoned_ArtKit_Manifest.csv` | Ảnh xem trước và manifest |

**Lấy từ Shared:**
- Điểm tài nguyên: `SpiritFlower_*` (Hoa linh) và `Sap_*` (Nhựa cây).
- Hiệu ứng: `Glow_Violet`, `Mist_Blob`, `Firefly`.
- Đồ: ghế, khúc gỗ, rương, cổng, `Fire_Small` cho trạm nghỉ.

## 2. Những điểm riêng của Abandoned

- **Bố cục [Chốt 2026-11-04: thêm nhiều bãi quái, không để quái chỉ ở trung tâm]:**
  - Trung tâm là sân cây thiêng. Quanh đó là một **đường vòng** uốn lượn `paths[1]`, bán kính khoảng 340×245, nối vào trung tâm bằng 3 lối ở phía bắc, tây và đông. Đường từ cổng ở phía nam đi thẳng vào trung tâm.
  - Từ đường vòng tách ra **6 nhánh** `paths[5..10]`, mỗi nhánh dẫn tới một bãi quái. Chỗ tách ra đều có `Signpost`.

    | # | Bãi | Vị trí | Quái (demo) | Đồ trang trí |
    |---|---|---|---|---|
    | 1 | Bãi Satyr ven hồ | tây nam | 5 Satyr, Lv9 | 2 `Log_Fallen`, hồ |
    | 2 | Trại hoang | tây bắc | 5 Pixie, Lv10 | 3 `Tent_Ruined`, `Log_Seat`, `Crate`, `Barrel`, `Hay` |
    | 3 | Đồng hoa linh | bắc, sát vách | 3 Pixie và 1 Dryad Rider, Lv11 | 4 `SpiritFlower` |
    | 4 | Rừng cây nhựa | đông bắc | 4 Satyr, Lv11 | 3 `Sap`, 2 gốc cây |
    | 5 | Vòng đá cổ | đông | 3 Dryad Rider, Lv12 | 6 `Menhir` xếp vòng |
    | 6 | Bãi gỗ mục | đông nam | 2 Pixie và 2 Satyr, Lv12 | 3 `Log_Fallen`, 1 `Sap` |

  - Trung tâm có boss, 3 Rider chạy vòng quanh cây, 2 Pixie và 3 hoa linh. Cấp và số con cụ thể theo spec bố cục.
  - Giữa các bãi là cây thưa xen bụi và đá. Rừng dày chỉ ở mép map và vài lùm.

- **Cây thiêng** đặt giữa sân tại (800, 604), gốc ở pivot.
  - Tán đung đưa tối đa 0,35 px, gốc đứng yên. Collider khoảng 28×12 ở gốc.
  - Dưới gốc đặt `Glow_Violet` cộng sáng, kích thước khoảng 220×110 px, alpha 0.18 ± 0.05 dao động chậm.
  - Hạt sáng màu `#e8d0ff`, `#bfa0ff`, `#9fe6dc` bay lên trong vùng tán, khoảng 8 hạt/s. Lá rơi từ cây thiêng màu tím `#c8a8f0`.
  - Tán mờ xuống 42% khi nhân vật lệch ngang dưới 70 px và cao hơn gốc dưới 130 px.
- **Vòng đá dựng:** elip bán kính 136×90 quanh cây, có 11 phiến. Rune nhấp nháy, thỉnh thoảng có hạt `#6fe0d6` bay lên từ phiến đá.
- **Sương phủ cả map:** 6 khối `Mist_Blob` phóng to 3–5 lần, alpha 0.16, trôi chậm. Viền tối quanh màn hình đậm hơn các map khác, alpha ở mép 0.68.
- **Quái ở trung tâm:** 3 Dryad Rider chạy vòng quanh cây trên elip 170×112, tốc độ khoảng 34 px/s, cách đều nhau. Boss Elder Dryad Rider đứng phía bắc cây, dùng Dryad Rider phóng to theo spec bố cục.
- **Bí mật:** rương nằm sát rễ, phía bắc gốc cây, có tia lấp lánh.
- **Tài nguyên:** `SpiritFlower` (Hoa linh) và `Sap` (Nhựa cây), demo đã đặt đúng cả hai loại.

## 3. Nghiệm thu

Chụp trong Unity và trong demo (`demo/outdoor2.html?map=2`) ở các vị trí: cổng, phía nam vòng đá nhìn lên cây thiêng, một chỗ rẽ có biển chỉ đường trên đường vòng, và 6 bãi quái. So từng cặp ảnh, phải ra cùng tông xám-lam, có sương và có quầng tím.
