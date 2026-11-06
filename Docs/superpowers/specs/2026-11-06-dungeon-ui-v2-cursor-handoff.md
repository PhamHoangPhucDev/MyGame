# Phó bản Trại Goblin: UI v2 và map 5 ải (giao Cursor)

Ngày: 2026-11-06. Người duyệt: user. Art và spec do Claude làm; code, scene và test do Cursor làm.
Tài liệu này dựa trên spec trước: `2026-11-06-goblin-camp-stages-design.md`. Các mục A1–A8 (phần còn thiếu của hệ thống) và bảng wave/thưởng/mốc 3 sao vẫn giữ nguyên.
Mockup đã duyệt nằm trong `Docs/UI/mockups/landscape/dungeon_v2/`. Script vẽ lại nằm trong `Docs/UI/tools/`.

## 0. Quy tắc chung
- Chỉ dùng khung ngang 16:9: canvas UI 640×360 kit px, nhân ×3 thành 1920×1080. Camera world nhân ×4 (1 ô 16 px = 64 px màn hình).
- **UI phải dùng kit đang có trong game**, không dùng style xám phẳng của mockup cũ L07b:
  - cửa sổ `UI_Panel_Main`, ribbon xanh `UI_Header_Ribbon`, nút đóng `UI_Button_Close_Normal`;
  - nút gỗ `UI_Button_Secondary_*`: Primary (vàng) cho tab đang chọn, Disabled cho tab khóa;
  - pin bản đồ `UI_WorldMap_*`;
  - font VT323 qua `UiKit`, chữ in hoa, viền `#141018`.
- Không đổi serialized field, GUID hay prefab ngoài phạm vi việc này. Không thêm dependency.
- Lưu chơi thật (PlayerPrefs) phải được sao lưu và khôi phục trong test.

## 1. Màn chọn ải: `L07c_Dungeon_Map_v2.png`
Sửa `DungeonSelectPanel` cho khớp mockup. Chức năng giữ nguyên, chỉ đổi skin và bố cục. Tọa độ dưới đây tính bằng kit px, gốc ở góc trên-trái canvas 640×360.

| Phần | Vị trí / kích thước | Sprite |
|---|---|---|
| Nền | Toàn màn hình: world phía sau, phủ `#0E1014` alpha 170/255 | — |
| Cửa sổ | (6, 8) 628×350, border 9 | `UI_Panel_Main` |
| Ribbon tiêu đề `BẢN ĐỒ PHÓ BẢN` | (240, 0) 160×18, border L/R 12 | `UI_Header_Ribbon` |
| Nút đóng | (618, 2) 16×16 | `UI_Button_Close_Normal` |
| 3 tab | y 19, cao 18, rộng 128, x = 17 / 149 / 281 | Primary (đang chọn, chữ `#3A2412`); Secondary_Disabled + `UI_Icon_Lock` (khóa, chữ `#7A8090`) |
| Khung bản đồ | (16, 40) 412×312 | `UI_Panel_Inset` (border 2) |
| Ảnh bản đồ | (18, 42), tỉ lệ 1:1 (408×308) | `UI_Dungeon_Board_GoblinCamp` + fog Stage2–5 như hiện tại |
| Pin | giữ mảng `PinAt` hiện có | Cleared + `UI_WorldMap_Pin_Check`, Selected + `UI_WorldMap_Pin_SelectHalo`, Locked, Boss_Locked. Badge loại map nằm dưới pin, số ải nằm trên pin |
| Đường nối giữa các pin | chấm 2×2 có viền 4×4, cách nhau 6 px | vàng `#F2D84A` cho đoạn đã qua, xám `#7A8090` cho đoạn chưa tới |
| La bàn | (406, 48) | `UI_WorldMap_Compass` |
| Panel phải | (436, 40) 188×312 | `UI_Panel_Inset` |

Nội dung panel phải (x = 446; y là toạ độ trên cùng):

| Dòng | y | Nội dung |
|---|---|---|
| 1 | 46 | `ẢI N · ĐANG MỞ`, màu vàng |
| 2 | 57 | Tên ải, size 20, màu trắng |
| 3 | 80 | Icon loại map 13 px + tên địa hình, màu `#AEB6C8` |
| 4 | 94 | `ĐỀ NGHỊ LV.X`, màu cyan; đổi sang đỏ khi `LevelWarn` |
| 5 | 106 | `N ĐỢT · KHÔNG BOSS / BOSS …` |
| 6 | 118 | `MỤC TIÊU: …` |
| — | 134 | Divider `UI_Divider_Line` |
| 7 | 139 | 3 sao `UI_Star_Large_Filled` 12 px + `RecordLine` (vd `1174 ĐIỂM · 02:21`) |
| — | 156 | Divider |
| 8 | 160 | `PHẦN THƯỞNG` |
| 9 | 174 | 3 ô `UI_Dungeon_RewardSlot` 36×36 (sliced 3), cách nhau 42. Icon item 16 px phóng ×2. Số lượng nằm góc dưới-phải |
| — | 214 | Divider |
| 10 | 218 | Gợi ý mở ải tiếp, màu xanh `#7ED67E` |
| 11 | 232 | Nút `DỊCH CHUYỂN` 168×22 (`UI_Button_Positive_Normal`), có `UI_KeyCap` "E" ở bên phải |
| 12 | 262 | Thẻ lính canh 168×84 (`UI_NPC_DialoguePanel`): `UI_PortraitFrame` 30 px + `UI_Dungeon_Guard_Portrait`, chữ `LÍNH CANH` vàng, tên, 3 dòng thoại |

## 2. HUD trong ải (cả 5 ải): `S1…S5_*_run.png`
Giữ nguyên HUD hiện có: khung Hero, nút skill, joystick, túi/bản đồ/pause. Thêm hoặc đổi các phần sau (kit px):

- **Trên-giữa:** ribbon xanh `ĐỢT n/N` ở (266, 6) 108×18. Ngay dưới là dải `UI_Panel_Inset` (226, 27) 188×14, ghi `ẢI n · <mục tiêu>`.
  - Ải 4 thay dải này bằng thanh tiến độ cầu.
  - Ải 5 thay bằng thanh máu boss.
- **Minimap (trên-phải):**
  - khung `UI_Minimap_Frame` tại (553, 38), vùng vẽ 68×68 ở offset (6, 6);
  - dưới khung là ribbon tên vùng, chữ cyan: (547, 118) 92×18;
  - dưới ribbon là ô đồng hồ `UI_NamePlate` (555, 137) 76×15: thời gian bên trái, `UI_Star_Large_Filled` 12 px ở x+36, mốc 3 sao bên phải màu vàng. Đồng hồ đổi sang vàng/đỏ khi vượt mốc 3 sao, đúng như spec trước;
  - `MiniMapHud` hiện vẽ 4 px cho 1 đơn vị. Mockup dùng **2 px cho 1 ô**, tức 34×34 ô quanh Hero. Ưu tiên giữ code hiện có, chỉ đổi khung và phần chữ;
  - marker dùng `UI_Minimap_Marker_*` (Player, Enemy, Boss, Chest, Gate). Cổng nằm ngoài minimap thì kẹp ở mép bằng `UI_Map_EdgePlate`.
- **Nhãn quái:** tên + `Lv.N` cỡ 26 px màn hình, nằm ngay trên đầu quái. Quái elite có chữ `ELITE` màu đỏ `#E04848`. Thanh máu nhỏ 64×9 chỉ hiện khi đã mất máu.
- **Số sát thương:** VT323 cỡ 54–70 px, viền dày `#141018`. Màu trắng cho đòn thường, vàng `#F2D84A` cho đòn chí mạng.
- **Banner đợt:** ví dụ `ĐỢT 4 — VÒNG VÂY`, màu cam `#F08A3A`, size 20.

## 3. Từng ải
| Ải | Map / art | UI riêng (art đã có trong `Docs/WorldArt/DungeonStages/StageN`) |
|---|---|---|
| 1 Đường đất đỏ | Vệt đất đỏ uốn S xuyên rừng, có 2 khoảng trống. Nền đất tô theo kiểu: 4 tông `#9A4A26 / #B45E32 / #C8763E / #DC9250`, mảng sáng tối lớn và mềm, dither Bayer, sỏi tròn có viền `#5A2A14` và đỉnh sáng `#F2C88C`. Mép cỏ có viền `#3C6E2C` và ngọn cỏ sáng `#9CD460`. Trong Unity có thể làm bằng decal `Stage1_RedDirtPath_9s` (Tiled) cộng lớp viền cỏ; mockup là chuẩn để so | Mũi tên chỉ đường trên vệt đất, icon cổng ở mép màn. Nắng lốm đốm và đom đóm (tùy chọn) |
| 2 Hàng rào nhọn | **Tường mê cung đổi sang đá:** `Stage2_StoneWall_Autotile_4x4.png`, cùng quy tắc mask với rào (xem README Stage2). Cổng gỗ `Stage2_FenceGate_2f` đặt trên đoạn tường ngang. Có cọc nhọn và rương ở ngõ cụt. Đuốc trên tường, ánh sáng chạng vạng | Minimap phủ sương, dấu ✕ ngõ cụt, la bàn. Cổng ra phát sáng vàng |
| 3 Bãi lửa trại | Bãi tròn, lửa trại ở giữa, 3 lều, thùng, cờ. Buổi tối, lửa hắt sáng ấm | Mũi tên cảnh báo vây, vòng vây dưới chân Hero, banner đợt |
| 4 Cầu vực | Vực dọc có vách đá và sương, cầu 3 đoạn theo chuỗi `0,1,1,1,3,1,1,1,3,1,1,2`, bóng cầu đổ xuống vực. Tiền đồn có đuốc | Thanh tiến độ cầu ở (240, 27) 160×28: frame ×2, pip ×2. Viền đỏ toàn màn khi Hero sát mép cầu |
| 5 Lều xương | Sân rào có lều xương, lư lửa, xương rải rác. Vòng nghi lễ ×2 ở giữa sân, boss đứng trong vòng. Hoàng hôn tím | Thanh máu boss `UI_BossBar_Frame` (215, 28) 210×22 có vạch pha 66% và 33%. Khi boss sắp ra đòn nghi lễ, vòng telegraph sáng đỏ |

Ánh sáng (ambient, đuốc, lửa, sương, viền tối quanh khung) trong mockup chỉ để định hướng không khí. Cursor kiểm tra render pipeline đang dùng trước:
- nếu có 2D Light thì dùng light;
- nếu không, dùng sprite glow additive cùng màu mockup.

Không bắt buộc khớp từng pixel.

## 4. Thứ tự làm (đề xuất)
1. Làm A1–A8 của spec trước (gắn ải với layout, dữ liệu wave và thưởng, kỷ lục theo ải).
2. Đổi skin `DungeonSelectPanel` theo mục 1.
3. Làm HUD chung theo mục 2: ribbon đợt, minimap + đồng hồ, nhãn quái, số sát thương.
4. Làm từng ải theo mục 3, bắt đầu từ root `FenceMaze` với tường đá.
5. Test editor qua `unity cmd --port 7801 eval`: chuyển ải, kỷ lục, mốc 3 sao, nhận thưởng; lưu PlayerPrefs trước test và khôi phục sau.

## 5. File art mới trong đợt này
- `Docs/WorldArt/DungeonStages/Stage2/Stage2_StoneWall_Autotile_4x4.png` (+ ảnh preview). Vẽ lại bằng `tools/stage2_stonewall.ps1`.
- Mockup: `L07c_Dungeon_Map_v2.png`, `L08_Dungeon_Minimap_v2.png`, `L08b_Minimap_States_v2.png`, `S1…S5_*_run.png`. Bản Canva v1 (không dùng) nằm trong `_rejected_canva_v1/`.
