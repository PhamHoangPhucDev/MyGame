# Brief cho Cursor: dựng map Abandoned Camp trong Unity

Đọc `CURSOR_BRIEF_CAMP.md` trước. Phần import, tile dual-grid, cây tách lớp, gió, lửa, hạt và cách nghiệm thu áp dụng nguyên cho map này. Brief này chỉ ghi phần khác.

Spec art: `Docs/superpowers/specs/2026-11-04-outdoor-maps-art-design.md`, mục 5 và mục 6, dòng Abandoned.

## 1. Những gì có trong bộ

| File | Dùng để |
|---|---|
| `Abandoned/Ground/*` | Mặt đất đã đổi màu theo bảng của Abandoned: cỏ ngả xám-lam, đất xám. Gồm `Fill_*`, `Dual_*` và `_Overlay`, cỏ cao, decal (hoa tím và xanh), `Deco_*`. Thay cho bộ của Camp. |
| `Abandoned/Trees/*` | 5 loại cây (tán và gốc tách lớp) và 3 loại bụi, đã giảm độ bão hòa |
| `Abandoned/Props/SacredTree_Canopy.png` / `_Trunk.png` | Cây thiêng 176×160. Hai lớp cùng kích thước và cùng pivot. |
| `Abandoned/Props/Menhir_0..10.png` | 11 phiến đá dựng cao 22–30 px, có rêu và rune. Mỗi phiến một vị trí trong layout. |
| `Abandoned/Props/Menhir_RuneGlow.png` | Ánh rune 4 frame, cộng sáng lên trên các phiến đá |
| `Abandoned/Props/Tent_Ruined.png` | Lều sừng rách, bạc màu, có rêu |
| `Abandoned/Abandoned_Overview*.png`, `Abandoned_Layout.json` | Toàn map 1600×1120 px, kèm tọa độ 966 vật thể và 14 quái (Dryad Rider có thông số `orbit`) |
| `Abandoned_ArtKit_Board.png`, `Abandoned_ArtKit_Manifest.csv` | Ảnh xem trước và manifest |

**Lấy từ Shared:**
- Điểm tài nguyên: `SpiritFlower_*` (Hoa linh) và `Sap_*` (Nhựa cây).
- Hiệu ứng: `Glow_Violet`, `Mist_Blob`, `Firefly`.
- Đồ: ghế, khúc gỗ, rương, cổng, `Fire_Small` cho trạm nghỉ.

## 2. Những điểm riêng của Abandoned

- **Cây thiêng** đặt giữa sân tại (800, 604), gốc ở pivot.
  - Tán đung đưa tối đa 0,35 px, gốc đứng yên. Collider khoảng 28×12 ở gốc.
  - Dưới gốc đặt `Glow_Violet` cộng sáng, kích thước khoảng 220×110 px, alpha 0.18 ± 0.05 dao động chậm.
  - Hạt sáng màu `#e8d0ff`, `#bfa0ff`, `#9fe6dc` bay lên trong vùng tán, khoảng 8 hạt/s. Lá rơi từ cây thiêng màu tím `#c8a8f0`.
  - Tán mờ xuống 42% khi nhân vật lệch ngang dưới 70 px và cao hơn gốc dưới 130 px.
- **Vòng đá dựng:** elip bán kính 136×90 quanh cây, có 11 phiến. Rune nhấp nháy, thỉnh thoảng có hạt `#6fe0d6` bay lên từ phiến đá.
- **Sương phủ cả map:** 6 khối `Mist_Blob` phóng to 3–5 lần, alpha 0.16, trôi chậm. Viền tối quanh màn hình đậm hơn các map khác, alpha ở mép 0.68.
- **Quái:** theo vòng như spec bố cục.
  - Satyr ở vòng ngoài, quanh hồ phía tây nam.
  - Pixie (Dryad) ở vòng giữa.
  - 3 Dryad Rider chạy vòng quanh cây trên elip 170×112, tốc độ khoảng 34 px/s, cách đều nhau.
  - Boss Elder Dryad Rider đứng phía bắc cây. Dùng Dryad Rider phóng to theo spec bố cục.
- **Bí mật:** rương nằm sát rễ, phía bắc gốc cây, có tia lấp lánh.
- **Đổi tài nguyên so với demo:** demo đặt Thảo dược ở các ô `Herb_Full`. Bản thật đổi thành **`Sap_Full`** (Nhựa cây) ở cùng các vị trí đó. Hoa linh giữ nguyên.

## 3. Nghiệm thu

Chụp trong Unity và trong demo (`demo/outdoor2.html?map=2`) ở 4 vị trí: cổng, phía nam vòng đá nhìn lên cây thiêng, hồ Satyr, lối mòn phía đông bắc dẫn tới bãi Pixie. So từng cặp ảnh, phải ra cùng tông xám-lam, có sương và có quầng tím.
