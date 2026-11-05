# Art 5 map dã ngoại: thiết kế

Ngày: 2026-11-04 · Trạng thái: người dùng đã duyệt demo v2 cho cả 5 map trong chat ("Duyệt, làm spec") và đang chờ duyệt bản viết này.

Tài liệu đi kèm:
- Spec bố cục và gameplay: `Docs/superpowers/specs/2026-11-03-outdoor-training-maps-design.md` (nhánh `claude/map-train-outdoor-regions-701d4d`). Từ đây gọi là **spec bố cục**.
- Demo chạy được: `Docs/WorldArt/OutdoorMaps/demo/outdoor2.html`. Cách mở xem ở mục 9.
- Clip tham khảo: `video_2026-10-19_14-36-38 (10).mp4`, nằm ngoài repo, chỉ dùng để tham khảo phong cách.

Ghi chú nguồn quyết định:
- **[Chốt]**: người dùng đã đồng ý trong chat, hoặc đã duyệt khi xem demo.
- **[Claude]**: Claude tự quyết định khi làm demo hoặc viết spec, người dùng nên kiểm tra lại.
- **[Builder]**: để người làm (Cursor) quyết định.

## 1. Mục đích

Đây là spec hình ảnh cho 5 map train dã ngoại: Goblin Forest Camp, Forest Trail, Abandoned Camp, Goblin Outpost và Gobrider Stronghold. Spec bố cục đã định map nào có gì. Spec này định từng map **trông thế nào và chuyển động ra sao**, để Claude vẽ bộ art và Cursor dựng lại trong Unity cho đúng bản demo đã duyệt.

Lý do làm lại từ v1: người dùng chê bản v1 là "chưa mượt, bối cảnh chưa phù hợp, thưa thớt, không sinh động, nhìn rất AI". Bản v2 được duyệt vì sửa đúng mấy chỗ đó:
- camera gần như trong clip;
- mặt đất chồng nhiều lớp đậm nhạt;
- rừng dày;
- cái gì cũng có chuyển động;
- dùng sprite thật của game thay cho hình khối sinh bằng code.

## 2. Mục tiêu / Không làm / Tránh

**Mục tiêu**
- Mỗi map vào Unity phải ra cùng cảm giác với demo v2: dày, có chiều sâu, chuyển động liên tục, cùng tông màu xanh xám-lam của clip.
- Vẽ một bộ art dùng chung cho cả 5 map, cộng thêm một bộ riêng cho từng map. Mỗi map phải nhận ra được ngay từ một ảnh chụp màn hình.

**Không làm trong đợt này**
- Không thiết kế lại HUD. HUD trong demo (khung chân dung, bản đồ nhỏ, nhiệm vụ, thanh phím, banner tên vùng) chỉ để minh họa và **không nằm trong phạm vi art**. HUD thật đi theo `Docs/UI/Reference/GAME01_UI_VIDEO_REFERENCE.md` và kit UI hiện có.
- Không vẽ quái mới. Quái dùng sheet R2C có sẵn trong `Assets/Art/Rebuild/R2C/Monsters`. Boss là bản phóng to của quái đã có, theo spec bố cục.
- Không làm âm thanh, không làm hiệu ứng skill.

**Tránh (làm vậy thì coi như hỏng, dù chạy được)**
- Nền phẳng một màu, rải đồ lác đác như bản v1.
- Cây, đá, nhà là hình khối trơn không có đổ bóng hay viền tối, nhìn ra ngay là sinh bằng code.
- Ở góc nhìn bình thường mà vẫn thấy nền trống không có chi tiết.
- Sao chép, can lại hay dò từ clip. Chỉ được học theo màu, tỉ lệ và mật độ (memory "Village: style-match, original art").
- 5 map chỉ khác nhau mỗi màu.

## 3. Ràng buộc

- Khung hình 16:9 ngang. Không làm layout dọc.
- Sprite dùng 16 px mỗi unit (PPU 16), lọc điểm ảnh kiểu point, không nén mờ, giống `Goblin_Goblin_Sheet.png` hiện có.
- Không đổi input hay render pipeline. Không thêm package khi chưa được duyệt.
- Không đụng vào các sheet quái và nhân vật hiện có. Chỉ được đọc, không sửa.
- Phân vai: Claude vẽ PNG, viết spec và brief. Cursor làm import, scene, code và test Unity.

## 4. Quy tắc nhìn chung [Chốt khi duyệt demo]

**Camera và tỉ lệ**
- Màn hình hiển thị khoảng 384–426 × 216–240 px game, phóng to theo bội số nguyên (×3 ở 1280×720). Nhân vật cao khoảng 1/9 chiều cao màn hình, giống clip.
- Với PPU 16, chiều cao màn hình khoảng 13,5–15 unit, tức orthographic size khoảng 7. Con số chính xác do [Builder] chọn, miễn giữ được tỉ lệ này.
- Camera bám theo nhân vật mượt, có nhìn trước khoảng 14 px theo hướng đi. Camera nhích theo từng phần nhỏ của điểm ảnh, không giật từng ô.

**Mặt đất**
- Cỏ có 5 tông. Các mảng đậm nhạt có hình hữu cơ, chồng lên nhau và có viền răng cưa.
- Mảng tối có viền 1 px tối hơn. Mảng sáng có cạnh trên sáng hơn 1 px.
- Rải dày nét cỏ hình "v" nhỏ 2–3 px, hoa từng cụm, nấm sát gốc cây.
- Đất có mép sẫm, có bóng ở mép trên, và có túm cỏ chồm ra đè lên mép đất.
- Sân đất lớn thì để lại các đảo cỏ, nhưng quanh tâm sân (đống lửa, cây thiêng) giữ trống.

**Vách đá phía trên**
- Mọi map đều có vách đá ở mép trên.
- Vách chia khối dọc rộng 6–12 px, có gờ ngang ở khoảng giữa, có vết nứt và dây leo rủ.
- Trên đỉnh vách là một hàng cây tối màu. Chân vách đổ bóng xuống cỏ khoảng 6 px.

**Mật độ**
- Rìa map là rừng dày 2–3 lớp. Trong map có các lùm cây và bụi.
- Giữa map vẫn có cây lẻ, bụi, đá rêu và gốc cây, khoảng cách khoảng 40 px.
- Cây không mọc trên đường, trên sân và trong vùng "giữ trống" của mỗi map.

**Chuyển động: phải có đủ, vì đây là lý do bản v2 được duyệt**
- Cỏ cao lay theo từng đợt gió quét ngang map (3 frame), và rạp sang hai bên khi nhân vật đi qua.
- Tán cây đung đưa dưới 1 px, gốc cây đứng yên. Bụi cũng đung đưa nhẹ.
- Lá rơi từ các tán cây đang nằm trong màn hình. Bóng mây lớn trôi chậm. Thỉnh thoảng có vệt gió trắng mờ chạy chéo màn hình như trong clip.
- Lửa có frame cháy, tia lửa, khói và vầng sáng ấm nhấp nháy. Cờ bay theo cột.
- Nước có điểm lấp lánh, có vệt chảy trên suối, có gợn tròn trên hồ. Thác có chân bọt và hơi nước.
- Có bướm bay. Map nào tối thì có đom đóm.
- Nhân vật đi thì tung bụi chân.

**Người chơi phải nhìn ra được những thứ tương tác**
- Rương ẩn: tia lấp lánh.
- Điểm thảo dược và hoa: lấp lánh theo màu riêng.
- Cổng ⛩: hạt sáng xoay quanh.
- Lối bí mật: bụi rung (Trail), hoặc vết nứt hay lỗ trên tường.

**Mờ khi đứng sau**: tán cây và cây thiêng mờ xuống khoảng 40% khi nhân vật đứng phía sau.

**Bóng đổ**: mọi vật đứng đều có bóng elip điểm ảnh màu `rgba(14,30,30,0.34)` dưới chân.

## 5. Bảng màu từng map [Claude, lấy mẫu từ clip, đã duyệt qua demo]

Bảng gốc dùng cho Camp và Trail:

| Nhóm | Mã màu |
|---|---|
| Cỏ, tối → sáng | `#2c4a47` `#365a55` `#44716a` `#4f7d6e` `#5c8a76` · viền mảng tối `#2c4b47` · nét cỏ `#2f504b` `#57876f` `#6b9b7f` |
| Đất | `#b08259` · sẫm `#9a704e` · sáng `#c49568` · mép `#7a5840` · bóng mép trên `#8e6646` |
| Vách đá | `#43291f` `#64402f` `#7e5640` `#956a4d` `#ab805d` |
| Nước | sâu `#244550` `#2c5661` `#3a6c73` · nông `#5f958f` · bờ `#223c3a` · bọt `#b6d8cd` |
| Gỗ (cọc, cầu) | `#4a2e20` `#6e4630` `#8e5e3e` `#a87448` |

Ba map còn lại đổi bảng màu như sau:

| Map | Đổi gì |
|---|---|
| Abandoned Camp | Cỏ ngả xám-lam: `#2b4447` `#34525a` `#41665f` `#4b7166` `#567b6c`. Đất ngả xám: `#9e8466`. Cây giảm độ bão hòa còn khoảng 50%. Hoa tím và xanh. Viền tối quanh màn hình đậm hơn. |
| Goblin Outpost | Cỏ màu ô liu khô: `#3a4733` `#45553a` `#566a45` `#62744d` `#6e7f55`. Đất sáng và ấm: `#bd9664`. Tán cây chuyển sang vàng úa, xoay màu từ −14 đến −40 độ. Hoa vàng và cam. |
| Gobrider Stronghold | Cỏ tối và lạnh hơn bảng gốc: `#29423f` … `#527a6b`. Đá lát: `#5a5c56` `#74766e` `#85877e` `#9a9c92`. Tường đá: `#3c4046` `#585d64` `#757b81` `#959ba0`. |

Trong demo, cây và bụi của làng (OV) được đổi màu khi chạy bằng filter. Bản Unity sẽ **xuất sẵn PNG đã đổi màu cho từng map** [Claude] để không phải dùng shader.

## 6. Từng map

Phần gameplay (cấp, bãi quái, boss, 2 loại tài nguyên, chỗ bí mật) đi theo **spec bố cục**. Mục này chỉ thêm phần hình ảnh. Chỗ nào demo làm khác spec bố cục thì spec bố cục thắng, và art sẽ làm theo spec bố cục (xem mục 7).

| Map | Kích thước demo (px) | Khung và địa hình | Điểm nhấn hình ảnh |
|---|---|---|---|
| 1 Camp | 1600×1120 | Sân đất lớn quanh đống lửa có đảo cỏ. 4 lối mòn. Suối từ thác trên vách đổ xuống hồ ở phía đông, có cầu gỗ. Bãi đá ở góc tây bắc. | Đống lửa lớn có vòng đá và 4 khúc gỗ ngồi. 5 lều sừng xếp vòng. Cờ rách. Giá phơi da, giá giáo, thùng và bao. Khúc gỗ đổ có mầm như trong clip. |
| 2 Trail | 2200×1200 | **[Chốt 2026-11-04]** Một đường chính uốn khúc từ tây sang đông, có **6 ngã rẽ** xen kẽ bắc và nam dẫn tới 6 bãi quái để train: trại Goblin, đồng hoa Pixie, ổ cung thủ, bãi Satyr, chuồng Gobrider, rừng nấm. Cấp tăng dần về phía đông, mỗi chỗ rẽ có biển chỉ đường. Đường chính có hàng rào bụi hai bên, ngoài là rừng kín. Suối từ thác cắt ngang đường ở cầu gỗ rồi đổ vào hồ. | Càng về phía đông càng tối, tối nhất khoảng 42%, có đom đóm. Sân boss có vòng cọc mở về phía tây và 2 cờ. Khoảng rừng trống để nghỉ có lửa nhỏ. Lối bí mật sau một bụi rung, dẫn tới khoảng rừng có rương. |
| 3 Abandoned | 1600×1120 | **[Chốt 2026-11-04]** Sân cây thiêng ở trung tâm. Quanh đó là một đường vòng uốn lượn, từ đường vòng có **6 nhánh** dẫn tới 6 bãi quái: Satyr ven hồ, trại hoang có lều rách, đồng hoa linh, rừng cây nhựa, vòng đá cổ, bãi gỗ mục. Cấp quái tăng dần, mỗi chỗ rẽ có biển chỉ đường. Có sương trôi phủ cả map. | Cây thiêng tán tím rộng khoảng 176×160 px, gốc to có rễ lan ra, có quầng sáng tím và hạt sáng bay lên. Vòng 10–11 phiến đá dựng có rune xanh ngọc. Lều sừng rách, bạc màu, có rêu. Rương nằm sát rễ. |
| 4 Outpost | 1600×1200 | **[Chốt 2026-11-05]** Đường chính từ nam lên bắc xuyên qua 3 tường cọc nằm ngang, chia map thành 4 khu. Mỗi khu có đường ngang và biển chỉ đường, dẫn tới bãi quái ở hai bên. Tổng cộng 8 bãi: mỏ quặng, trạm gác ngoài, trại lều, chuồng Gobrider, lò rèn, kho hàng, bãi tập, chuồng Cyclops. | Tường cọc 2 hàng so le, có dây buộc. Mỗi cổng có 2 tháp canh gỗ, cung thủ đứng trên tháp và bắn tên. Lò lửa sắt hai bên cổng. Lỗ chó ở tường 2. Chuồng sắt lớn 108 px nhốt Cyclops. |
| 5 Stronghold | 1600×1200 | Tường đá ngoài hình chữ nhật, cổng nam và cổng đông. Bên trong là tường cọc, rồi sân lát đá. | Nhà chính gỗ khoảng 136×96 px, mái ván đỏ nâu, sừng xương ở hai đầu nóc, có cờ đỏ hình đầu lâu. Tháp đá ở góc tường. Lò lửa trước cổng và trước nhà chính. Thác ngoài tường phía đông đổ xuống hồ, có bậc đá kê chân và lối mòn dẫn tới cửa hang có kho vũ khí. |

## 7. Chỗ demo khác spec bố cục: art làm theo spec bố cục

Bảng dưới liệt kê những chỗ demo dùng đồ tạm. Bộ art sẽ vẽ theo cột "Làm theo".

| Map | Demo dùng | Làm theo (spec bố cục) |
|---|---|---|
| Camp | Tài nguyên là đá và thảo dược. Boss dùng sheet Warlord. Cung thủ đứng dưới đất. | Tài nguyên là **gỗ** (đống gỗ, gốc chặt) và **da thú** (giá phơi da). Boss là **Goblin phóng to**. Cung thủ đứng trên **bục gỗ** (tháp canh nhỏ, cùng cách đứng như tháp ở Outpost). |
| Trail | Thảo dược và nấm | Giữ nguyên |
| Abandoned | Hoa linh và thảo dược | **Nhựa cây** (cây rỉ nhựa hổ phách) và **hoa linh** |
| Outpost | Quặng sắt và đá. Có thêm Goblin. | Giữ nguyên tài nguyên. Phân bố quái theo spec bố cục: Gobrider, Elite và Warlord. |
| Stronghold | Quặng sắt và thảo dược | **Sắt** (mạch quặng) và **chiến lợi phẩm** (đống vũ khí, khiên) |
| Tất cả | Cấp quái và chỉ số trong demo | Theo spec bố cục và [Builder] |

Bí mật ở Camp: trong demo là rương giữa vòng bụi ở góc đông bắc, spec bố cục ghi "lều bỏ trống". Gộp hai cách lại [Claude]: một lều rách nằm trong vòng bụi ở góc đông bắc, có rương và tia lấp lánh.

## 8. Bộ art cần vẽ

Tất cả đều là PNG, theo PPU 16 và lọc point. Mỗi map một thư mục `Docs/WorldArt/OutdoorMaps/<Map>/`, cùng một file manifest CSV ghi tên, kích thước, pivot và số frame. Thư mục import vào `Assets/Art/` do [Builder] chọn.

**8.1 Bộ dùng chung**, bản đổi màu xuất riêng cho từng map khi cần:
- **Mặt đất** dạng tilemap 16 px, dùng bộ 16 ô dual-grid cho mỗi cặp chuyển tiếp: cỏ → cỏ tối, cỏ tối → rừng sâu, cỏ → đất, cỏ → nước, đất → đá lát. Có ô biến thể cho cỏ và đất. Tấm decal rời cho nét cỏ, hoa, nấm, sỏi, xương và vết chân [Claude: chọn dual-grid vì ít ô và ghép mép tự nhiên].
- **Vách đá**: dải mặt vách cao 3 ô có 4–6 biến thể, ô mép cỏ trên đỉnh, ô chân vách có bóng, dây leo rủ. Thác nước 4 frame, chân bọt 4 frame. Cửa hang.
- **Cây**: tán và gốc **tách 2 lớp** để tán đung đưa còn gốc đứng yên. Bóng cây. Bụi 3 loại, đá rêu 3 cỡ, gốc cây, khúc gỗ đổ có mầm, khúc gỗ ngồi.
- **Cỏ cao**: 3 frame (nghiêng trái, đứng, nghiêng phải) × 2 tông.
- **Lửa**: lửa lớn 6 frame, lửa nhỏ 6 frame, vòng đá, lò lửa sắt. Sprite hạt: tia lửa, khói, lá, đom đóm, hạt bụi, vòng gợn nước, vệt lấp lánh trên nước, khối sương và bóng mây (mờ mềm).
- **Cầu gỗ** ngang và dọc (mặt cầu và lan can), bậc đá kê chân trên nước.
- **Tương tác**: cổng ⛩ đá (khung và xoáy sáng 6 frame), ghế trạm nghỉ, biển chỉ đường, rương đóng và mở, chuẩn bị cho 3 hạng rương của spec bố cục.
- **Điểm tài nguyên**, mỗi loại có trạng thái đầy và cạn: gỗ, da thú, thảo dược, nấm, nhựa cây, hoa linh, đá, quặng sắt, sắt, chiến lợi phẩm.

**8.2 Bộ riêng từng map**:
- **Camp**: lều sừng (dùng `Tent_GoblinHorn` hiện có), cờ, giá da, giá giáo, bục cung thủ, thùng và bao (dùng OV hoặc đổi màu), lều rách cho chỗ bí mật.
- **Trail**: hàng rào bụi theo đoạn, cọc sân boss, bụi rung (3 frame lắc).
- **Abandoned**: cây thiêng (tán và gốc tách lớp), đá dựng có rune 3 biến thể và rune sáng 4 frame, lều rách, quầng sáng tím.
- **Outpost**: cọc tường 4 chiều cao, tháp canh gỗ, lỗ chó, chuồng sắt (lớp sau và lớp trước để Cyclops đứng kẹp giữa), mũi tên.
- **Stronghold**: tường đá đoạn ngang và đoạn dọc, tháp đá góc, tường cọc đoạn dọc, nhà chính, cờ đầu lâu, đá lát, đống chiến lợi phẩm, kho vũ khí trong hang.

## 9. Demo tham chiếu

- Các file: `Docs/WorldArt/OutdoorMaps/demo/outdoor2.html`, thư mục `ov/` và `mob/` (bản sao sprite đang dùng), và `outdoor_v1.html` (bản v1 bị chê, giữ lại để so sánh).
- Cách mở: chạy `npx http-server Docs/WorldArt/OutdoorMaps/demo -p 8765`, rồi mở `http://localhost:8765/outdoor2.html?map=0`. Thay `0` bằng 0–4 để chọn map.
  - Điều khiển: WASD đi, Space chém, E tương tác, phím 1–5 đổi map, M bật/tắt bản đồ nhỏ, L bật/tắt nhãn quái.
- Demo là **nguồn tham chiếu hình ảnh và chuyển động**, không phải code để port. Cây và bụi được rải theo seed cố định. Khi dựng map trong Unity thì đặt tay theo các vùng ở mục 6.

## 10. Để người làm quyết định [Builder]

- Dùng Tilemap với Rule Tile cho bộ dual-grid, hay cách khác, miễn ra đúng chuyển tiếp và độ dày của mục 4.
- Cách làm gió: cùng một pha sóng chạy qua cỏ, cây và bụi. Có thể dùng shader hoặc đổi frame, miễn không làm hình bị mờ.
- Vệt sáng lửa, sương, bóng mây và độ tối dần của Trail: làm bằng sprite cộng sáng hay bằng Light2D, miễn giữ nguyên render pipeline hiện có.
- Thứ tự vẽ theo trục Y, vị trí pivot và collider của từng vật.
- Vị trí đặt từng vật trên mỗi map, miễn đúng khung và bối cảnh của mục 6 và spec bố cục.

## 11. Thứ tự làm

1. Claude: bộ dùng chung (mục 8.1) cùng bộ riêng của **Camp**. Xuất PNG, manifest và brief cho Cursor.
2. Cursor: dựng Camp trong Unity. Hai bên so ảnh chụp với demo.
3. Claude và Cursor: Trail, rồi Abandoned, Outpost, Stronghold. Mỗi map có một vòng so ảnh với demo.
