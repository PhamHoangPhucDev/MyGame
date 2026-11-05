# Brief cho Cursor: dựng map Gobrider Stronghold trong Unity

Đọc `CURSOR_BRIEF_CAMP.md` trước. Phần import, tile dual-grid, cây tách lớp, gió, lửa, hạt và cách nghiệm thu áp dụng nguyên cho map này. Brief này chỉ ghi phần khác.

Spec art: `Docs/superpowers/specs/2026-11-04-outdoor-maps-art-design.md`, mục 5 và mục 6, dòng Stronghold.

## 1. Những gì có trong bộ

| File | Dùng để |
|---|---|
| `Stronghold/Ground/*` | Mặt đất tối và lạnh hơn các map khác. **Riêng `Fill_Pave` và `Dual_DirtToPave*` là đá lát dạng phiến không đều**, dùng cho sân trong và đường dẫn vào nhà chính. |
| `Stronghold/Trees/*` | 5 loại cây (tán và gốc tách lớp) và 3 loại bụi |
| `Stronghold/Fortify/Wall_Stone_H_0..3.png` | Tường đá đoạn ngang 16×30, có răng cưa trên đỉnh. Ghép mỗi 16 px, xoay vòng 4 biến thể. |
| `Stronghold/Fortify/Wall_Stone_V.png` | Tường đá đoạn dọc 16×38. Xếp chồng mỗi 16 px, đoạn sau che mặt tường của đoạn trước. |
| `Stronghold/Fortify/Tower_Stone.png` | Tháp đá 32×58, đặt ở các góc và hai bên mỗi cổng |
| `Stronghold/Fortify/Palisade_Post_20..29.png`, `Palisade_V.png` | Tường cọc trong: đoạn ngang dùng 2 hàng cọc so le, đoạn dọc xếp chồng `Palisade_V` mỗi 14 px. Cọc cỡ 20–24 dùng làm cọc tập. |
| `Stronghold/Props/Great_Hall.png` | Nhà chính 136×96: mái ván, sừng ở hai đầu nóc, cờ đỏ đầu lâu, cửa nằm ngay pivot |
| `Stronghold/Props/Tent_Horn.png`, `Flag_Tattered.png` | Lều sừng và cờ |
| `Stronghold/Stronghold_Overview*.png`, `Stronghold_Layout.json` | Toàn map 1600×1200 px, kèm tọa độ 1269 vật thể và 45 quái |
| `Stronghold_ArtKit_Board.png`, `Stronghold_ArtKit_Manifest.csv` | Ảnh xem trước và manifest |

**Lấy từ Shared:**
- Điểm tài nguyên: `IronOre_*` (Sắt) và `Spoils_*` (Chiến lợi phẩm).
- Thác nước và bọt chân thác, cửa hang, `Stepping_Stone`.
- `Brazier` cùng `Fire_Small`, rương, cổng, ghế, khúc gỗ, gốc cây.

## 2. Bố cục [Chốt 2026-11-05: nhiều bãi quái khắp map]

- **Tường đá ngoài** bao hình chữ nhật từ (330, 330) đến (1270, 960), có 3 cổng: nam (x = 800, rộng 48 px), tây và đông (y = 650, rộng 40 px).
  - Mỗi góc và hai bên mỗi cổng đặt một `Tower_Stone`.
  - Cổng nam có 2 `Brazier` và lửa. Mỗi cổng có Elite đứng gác.
- **Tường cọc trong** bao hình chữ nhật từ (560, 410) đến (1040, 760), có cổng nam.
  - Bên trong là sân lát đá, `Great_Hall`, 2 lò lửa, 2 cờ, giá đồ và thùng.
  - Boss Gobrider Warlord đứng trước cửa nhà chính, kèm 2 Elite và 3 Goblin.
  - Đường lát đá chạy từ cổng trong xuống đường vành đai.
- **8 bãi quái**, mỗi chỗ rẽ có `Signpost`:

| Vị trí | # | Bãi | Quái (demo) | Đồ trang trí |
|---|---|---|---|---|
| Giữa hai tường, tây nam | B1 | Doanh trại | 5 Goblin, Lv18 | 2 `Tent_Horn`, ghế |
| Giữa hai tường, đông nam | B2 | Lò rèn | 4 Gobrider, Lv19 | 2 `Brazier` có lửa, giá giáo, giá, 2 `IronOre` |
| Giữa hai tường, tây bắc | B3 | Chuồng sói | 4 Gobrider, Lv19 | rơm, thùng, bao, giá |
| Giữa hai tường, đông bắc | B4 | Bãi tập | 4 Elite, Lv20 | 6 cọc tập, 2 `Spoils` |
| Ngoài thành, tây nam | O1 | Trại tiền tiêu | 5 Goblin, Lv17 | lều, ghế, `IronOre` |
| Ngoài thành, đông nam | O2 | Bãi đốn gỗ | 4 Goblin và 1 cung thủ, Lv17 | 2 `Log_Fallen`, 2 gốc cây, `Spoils` |
| Ngoài cổng tây | O3 | Trại thợ săn | 4 Goblin, Lv17 | 2 giá phơi da, ghế |
| Ngoài cổng đông | O4 | Bãi ven thác | 4 Gobrider, Lv19 | `IronOre`, cạnh hồ |

- **Đường đi:**
  - Giữa hai lớp tường có đường vành đai nối B1 → B3 ở phía tây và B2 → B4 ở phía đông, phía nam nối qua đường ngang.
  - Đường từ cổng tây đi ra O3. Đường từ cổng đông đi ra O4.
  - Ngoài cổng nam có đường ngang sang O1 và O2.
- **Bí mật:** đi qua các `Stepping_Stone` trên hồ dưới thác, theo lối mòn tới cửa hang (x = 1500). Trong hang có rương kho vũ khí và giá giáo.
- Quái và cấp cụ thể theo spec bố cục: Goblin ở tường ngoài và ngoài thành, Gobrider ở giữa hai lớp tường, Warlord trong sân nhà chính.

## 3. Nghiệm thu

Chụp trong Unity và trong demo (`demo/outdoor2.html?map=4`) ở các vị trí: cổng nam, sân lát đá và nhà chính, 8 bãi quái, bậc đá kê chân và cửa hang. So từng cặp ảnh. Tường đá đoạn dọc và đoạn ngang phải nối liền nhau ở các góc tháp.
