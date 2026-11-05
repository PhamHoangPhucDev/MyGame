# Brief cho Cursor: dựng map Goblin Outpost trong Unity

Đọc `CURSOR_BRIEF_CAMP.md` trước. Phần import, tile dual-grid, cây tách lớp, gió, lửa, hạt và cách nghiệm thu áp dụng nguyên cho map này. Brief này chỉ ghi phần khác.

Spec art: `Docs/superpowers/specs/2026-11-04-outdoor-maps-art-design.md`, mục 5 và mục 6, dòng Outpost.

## 1. Những gì có trong bộ

| File | Dùng để |
|---|---|
| `Outpost/Ground/*` | Mặt đất màu ô liu khô, đất sáng và ấm. Gồm `Fill_*`, `Dual_*` và `_Overlay`, cỏ cao, decal (hoa vàng và cam), `Deco_*`. |
| `Outpost/Trees/*` | 5 loại cây (tán và gốc tách lớp), tán chuyển sang màu úa vàng, thân giữ màu gỗ nâu. Kèm 3 loại bụi. |
| `Outpost/Fortify/Palisade_Post_20..29.png` | Cọc gỗ theo màu gỗ của Outpost. Cỡ 20–24 cũng dùng làm cọc tập ở bãi 7. |
| `Outpost/Fortify/Watchtower.png` | Tháp canh gỗ 32×60. Cung thủ đứng cao hơn pivot 30 px. |
| `Outpost/Props/Cage_Back.png` / `Cage_Front.png` | Chuồng sắt 108 px, lớp sau có sàn rơm, lớp trước có ổ khóa. Cyclops đứng kẹp giữa hai lớp. |
| `Outpost/Props/Arrow.png` | Mũi tên 6×1 của cung thủ trên tháp |
| `Outpost/Props/Tent_Horn.png`, `Flag_Tattered.png` | Lều sừng và cờ |
| `Outpost/Outpost_Overview*.png`, `Outpost_Layout.json` | Toàn map 1600×1200 px, kèm tọa độ 1969 vật thể và 41 quái |
| `Outpost_ArtKit_Board.png`, `Outpost_ArtKit_Manifest.csv` | Ảnh xem trước và manifest |

**Lấy từ Shared:**
- Điểm tài nguyên: `IronOre_*` (Quặng sắt) và `Stone_*` (Đá).
- `Brazier` cùng `Fire_Small`.
- Ghế, rương, cổng, `Boulder_*`, thùng, bao, rơm.

## 2. Bố cục [Chốt 2026-11-05: nhiều bãi quái khắp map]

- **Ba tường cọc** nằm ngang ở y = 960, 720 và 470, chạy từ x = 150 đến 1450. Ba tường chia map thành 4 khu.
  - Đường chính chạy từ nam lên bắc, xuyên qua cổng giữa mỗi tường.
  - Mỗi tường gồm 2 hàng cọc so le: cọc đặt mỗi 7 px, hàng sau cao hơn 3 px và lệch ngang 3 px. Collider r4 đặt ở hàng trước.
- **Mỗi cổng** rộng 34 px. Hai bên cổng có 2 `Watchtower`, mỗi tháp có một Goblin Archer đứng trên.
  - Cung thủ bắn `Arrow` khi nhân vật cách dưới 130 px, mỗi 1,8–2,6 giây một mũi. Tên bay theo đường vòng cung cao 14 px. Trúng thì trừ 2–4 HP.
  - Hai bên cổng có `Brazier` và lửa đặt cao hơn pivot 14 px.
- **Mỗi khu có một đường ngang**, chỗ rẽ từ đường chính có `Signpost`. Đường ngang dẫn tới 2 bãi quái, một bên trái và một bên phải:

| Khu | # | Bãi | Quái (demo) | Đồ trang trí |
|---|---|---|---|---|
| Ngoài tường 1 | 1 | Mỏ quặng (tây nam) | 5 Goblin, Lv13 | 4 `IronOre`, 3 `Boulder` |
| | 2 | Trạm gác ngoài (đông nam) | 3 Goblin và 2 cung thủ (1 đứng trên tháp), Lv13 | `Watchtower`, `Tent_Horn`, 2 `Stone` |
| Tường 1 → 2 | 3 | Trại lều (tây) | 5 Gobrider, Lv14 | 2 `Tent_Horn`, ghế, giá |
| | 4 | Chuồng Gobrider (đông) | 4 Gobrider, Lv15 | rơm, thùng, bao, giá, ghế |
| Tường 2 → 3 | 5 | Lò rèn (tây) | 4 Elite, Lv16 | 2 `Brazier` có lửa, 2 giá giáo, giá, 2 `Stone` |
| | 6 | Kho hàng (đông) | 3 Elite và 1 cung thủ, Lv16 | lều, thùng gỗ, thùng tròn, bao, thùng thảo dược, rơm |
| Sau tường 3 | 7 | Bãi tập (tây bắc) | 4 Elite, Lv17 | 6 cọc tập, giá |
| | — | Chuồng Cyclops (đông bắc) | Cyclops bị nhốt | `Cage_Back` và `Cage_Front` |

- **Sân sau cổng 3:** Goblin Warlord đứng giữ cổng với 2 Elite bên cạnh. Có 2 cờ và 2 điểm quặng sát vách đá.
- **Bí mật:** tường 2 có lỗ chó rộng 18 px tại x = 1395, chui qua thì vào một khoảng trống nhỏ có vòng bụi bao quanh và rương giấu đồ. Trên tường nên vẽ một vết gãy để người chơi nhận ra lỗ này.
- Quái và cấp cụ thể theo spec bố cục: sau cổng 1 là Gobrider, sau cổng 2 là Elite, Warlord giữ cổng 3, boss khu vực là Cyclops.

## 3. Nghiệm thu

Chụp trong Unity và trong demo (`demo/outdoor2.html?map=3`) ở các vị trí: cổng 1 nhìn thấy cả 2 tháp, một chỗ rẽ có biển chỉ đường, 8 bãi quái, lỗ chó, chuồng Cyclops. So từng cặp ảnh.
