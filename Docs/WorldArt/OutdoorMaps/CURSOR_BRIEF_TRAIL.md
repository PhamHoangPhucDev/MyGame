# Brief cho Cursor: dựng map Forest Trail trong Unity

Hãy đọc `CURSOR_BRIEF_CAMP.md` trước. Các mục về import, tile dual-grid, cây tách lớp, gió, lửa, hạt và cách nghiệm thu áp dụng nguyên cho Trail. Brief này chỉ ghi phần khác.

Spec art: `Docs/superpowers/specs/2026-11-04-outdoor-maps-art-design.md`, mục 6 dòng Trail.

## 1. Dùng lại gì, có gì mới

- **Dùng lại nguyên, không cần bản mới:** `Camp/Ground`, `Camp/Trees` và `Camp/Props/Flag_Tattered`. Lý do: Trail dùng chung bảng màu gốc với Camp.
- **Dùng lại từ Shared:**
  - Vách đá, thác nước, lửa nhỏ, ghế, khúc gỗ đổ, đá rêu, rương.
  - Cầu: `Bridge_Deck_H` và `Bridge_Rail_H`.
  - Điểm tài nguyên: `Herb_*` (Thảo dược) và `Mushroom_*` (Nấm), theo spec bố cục.
  - Hạt `Firefly`.
- **Mới:**

| File | Dùng để |
|---|---|
| `Shared/Fortify/Palisade_Post_22..29.png` | Cọc gỗ cao 22–29 px. Trail dùng cho vòng cọc sân boss; Outpost và Stronghold cũng sẽ dùng. |
| `Trail/Props/Bush_Rustle.png` | Bụi rung 3 frame, che lối bí mật. **Không có collider**, người chơi đi xuyên qua được. |
| `Trail/Props/Hedge_2..4.png` | Một đoạn 3–5 bụi đè lên nhau, dùng để dựng nhanh hàng rào bụi hai bên đường. Đặt collider mỗi 14 px. |
| `Trail/Trail_Overview*.png`, `Trail/Trail_Layout.json` | Toàn map 2200×1200 px, bản trơn và bản có ghi chú, kèm tọa độ 2485 vật thể và 32 quái |
| `Trail_ArtKit_Board.png`, `Trail_ArtKit_Manifest.csv` | Ảnh xem trước và manifest của phần mới |

## 2. Những điểm riêng của Trail

- **Bố cục [Chốt 2026-11-04: "1 con đường chính và có nhiều ngã rẽ tới nhiều bãi quái để train"]:**
  - Đường chính `paths[0]` rộng 13 px, chạy từ tây (cổng ⛩) sang đông (sân boss).
  - **6 ngã rẽ** `paths[1..6]`, rộng 9 px, xen kẽ bắc và nam, mỗi ngã rẽ dẫn tới một bãi quái có bối cảnh riêng. Cấp quái tăng dần về phía đông:

    | # | Bãi | Hướng | Quái (demo) | Đồ trang trí |
    |---|---|---|---|---|
    | 1 | Trại Goblin | bắc | 5 Goblin, Lv5 | 2 `Tent_Horn`, lửa nhỏ, 2 `Log_Seat`, `Rack_Spears` |
    | 2 | Đồng hoa Pixie | nam | 5 Pixie, Lv6 | 3 điểm thảo dược, 2 gốc cây, hoa |
    | 3 | Ổ cung thủ | bắc | 3 Goblin Archer và 2 Goblin, Lv7 | 5 `Boulder_*` |
    | 4 | Bãi Satyr | nam, sát hồ cuối suối | 5 Satyr, Lv8 | 2 `Log_Fallen` |
    | 5 | Chuồng Gobrider | bắc | 4 Gobrider, Lv9 | `Hay`, `Barrel`, `Crate`, giá phơi da, `Log_Seat` |
    | 6 | Rừng nấm | nam, vùng tối | 3 Pixie và 2 Satyr, Lv10 | 4 điểm nấm, 2 gốc cây |

  - Mỗi ngã rẽ có một `Signpost` ở chỗ rẽ, tọa độ nằm trong `terrain.branches`, kèm tên bãi để làm bảng tên nếu cần. Quái và cấp cụ thể theo spec bố cục.
  - `paths[7]` là lối lên trạm nghỉ, `paths[8]` là lối bí mật.
  - Hai bên **đường chính** là hàng rào bụi cách tâm đường 24–32 px, mở ra ở mỗi ngã rẽ. Phần còn lại là rừng kín, mật độ khoảng 0.9 lấy từ khoảng cách tới đường hoặc bãi gần nhất. Không được để lộ cỏ trống.
- **Tối dần theo chiều sâu:** phủ một dải chuyển màu theo tọa độ thế giới, đi từ x = 800 (trong suốt) tới mép đông (độ đậm 0.42, màu `#060e16`). Dải này phải phủ trên cây và quái, nhưng nằm dưới hạt hiệu ứng và HUD.
  - Đom đóm (`Firefly`, cộng sáng, nhấp nháy theo sin) xuất hiện nhiều dần theo độ tối, tối đa khoảng 12 con/s trong khung hình.
- **Cấp quái tăng theo x**, chia 4 dải. Số liệu cụ thể theo spec bố cục.
- **Gobrider tuần tra** đi qua lại theo các điểm trong trường `patrol` của layout.
- **Bí mật:** spur hẹp rộng 5 px đi lên phía bắc tại x ≈ 800, lối vào có `Bush_Rustle` che, hai bên có thêm bụi thường.
  - Bụi rung khi nhân vật đứng trong vòng 14 px, và khi không có ai thì tự rung mỗi khoảng 2 s.
  - Cuối spur là một khoảng rừng trống nhỏ có rương.
- **Trạm nghỉ:** khoảng rừng trống phía bắc tại (1420, 430), có lửa nhỏ, ghế và khúc gỗ.
- **Sân boss:** khoảng trống tại (2060, 550). Vòng `Palisade_Post_*` đặt cách nhau khoảng 15 px, chừa cửa mở về phía tây. Có 2 `Flag_Tattered` và 3 đá rêu. Boss là Gobrider Captain, dùng Elite Gobrider phóng to theo spec bố cục.
- **Suối và cầu:** thác ở x = 1100. Suối chảy dọc xuống, đổ vào hồ ở (1150, 1050) cạnh bãi Satyr. Cầu nằm trong `terrain.bridges`.

## 3. Nghiệm thu

Chụp các vị trí sau trong Unity và trong demo (`demo/outdoor2.html?map=1`): cổng, một ngã rẽ có biển chỉ đường, 6 bãi quái, cầu, lối bí mật, trạm nghỉ, sân boss. So từng cặp ảnh, độ tối ở sân boss phải gần giống demo.
