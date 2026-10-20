# Icon hotbar — 25 skill + 5 nút R (5 class): ghi chú cho Cursor

**Ngày:** 2026-10-20 · **Người viết:** Claude (chỉ art + ghi chú, không sửa code, scene hay prefab)
**Đã duyệt mẫu:** `icons/ICONS_CANDIDATE_v2.png` (phong cách v2) · **Bảng duyệt:** `icons/ICONS_30_review.png` (25 skill + 5 nút R; bản cũ `ICONS_25_review.png`)
**Nguồn:** `tools/IconGen.cs`, chạy `tools/RenderIcons.ps1` (xuất PNG, bảng duyệt và kiểm tra pixel).

## 1. File giao (`icons/export/`)

| File | Nội dung |
|---|---|
| `Icon_{Class}_{Key}_{SkillId}.png` × 25 | Mỗi icon **32×32**, có 1 px viền trong suốt, nền và khung đã vẽ sẵn trong icon |
| `Icon_{Class}_R_Combo.png` × 5 | **Nút R (đòn thường)** của từng class, cùng phong cách và khung (thêm 2026-10-20) |
| `Icons_Skills_Atlas.png` | **192×160**: hàng = class (Aren, Brom, Lyra, Selene, Vex), cột 0–4 = phím (2, 3, 4, V, C), **cột 5 = R**, mỗi ô 32×32 |
| `ICONS_MANIFEST.csv` | `file,class,key,skill_id,name_vi,atlas_col,atlas_row,size` |

Import: Sprite (2D and UI), **Filter Point**, **Compression None**, không mipmap, pivot giữa. PPU không ảnh hưởng vì Image trên UI lấy theo kích thước RectTransform.

### Danh sách (phím 2 / 3 / 4 / V / C)

| Class | 2 | 3 | 4 | V | C |
|---|---|---|---|---|---|
| Aren | ShieldBash · Khiên Đập | Charge · Xung Phong | WhirlSlash · Lốc Kiếm | Bulwark · Thành Lũy | Dawnbreaker · Chém Bình Minh |
| Brom | GroundSlam · Nện Đất | ShieldCharge · Húc Khiên | TauntRoar · Gầm Thách | IronWall · Tường Sắt | Earthquake · Động Đất |
| Lyra | FanShot · Tên Quạt | LeapBack · Nhảy Lùi | ArrowRain · Mưa Tên | WindStep · Bước Gió | ForestPierce · Tên Xuyên Rừng |
| Selene | StarOrb · Cầu Tinh Tú | Blink · Dịch Chuyển | RuneCircle · Vòng Phù Chú | ManaShield · Khiên Ma Lực | MeteorShower · Mưa Sao Băng |
| Vex | ShadowStep · Bước Bóng | KnifeThrow · Phi Dao | BladeDance · Vũ Điệu Lưỡi Dao | SmokeBomb · Bom Khói | Execute · Đoạt Mệnh |

**Nút R:** Aren · Kiếm Hiệp Sĩ (kiếm + lưỡi liềm băng) · Brom · Búa Rèn (búa + vệt vung cam) · Lyra · Mũi Tên Rừng (cung kéo dây, tên phát sáng) · Selene · Tia Phép (gậy + cầu phép lấp lánh) · Vex · Song Đao (hai dao găm chéo + vệt chém đỏ). Trong manifest: `key = R`, `skill_id = Combo`.

Thứ tự Vex theo `classkit/CLASS_KIT_DESIGN.md` mục 6. Nếu `ClassKit_Vex` hiện gán phím khác, chỉ cần map lại theo `skill_id`.

## 2. Khung theo class (đã vẽ trong icon)

| Class | Khung | Nền | Hoa văn chìm |
|---|---|---|---|
| Aren | thép bóng, đinh vàng | xanh navy đêm | thập tự hiệp sĩ |
| Brom | sắt rèn, đinh đồng | nâu lò rèn | cái đe |
| Lyra | gỗ khắc, đinh lá xanh | xanh rừng sâu | chiếc lá |
| Selene | tím bạc, đinh sao vàng | tím đêm | trăng khuyết |
| Vex | thép đen, đinh đỏ thẫm | đen đỏ | chữ X |

## 3. Việc cần làm phía Unity (Cursor)

1. Copy `icons/export/` vào `Assets/Art/Rebuild/R2C/Icons/` rồi import như mục 1.
2. **`ClassKit`:** thêm trường `Sprite icon` cho mỗi skill (SkillDef). Gán 5 icon cho mỗi `ClassKit_*` theo `skill_id`.
3. **`SkillHotbarUI`:**
   - Khi đổi class (F1–F5 trong Sandbox), lấy sprite từ `ClassKit` thay cho `UiPixelArt.Icon(...)`. Giữ icon cũ cho Level 1–4 nếu chưa chuyển sang class kit.
   - **Kích thước:** icon đang là `slotSize * 0.72` (khoảng 49 px, dòng 296), ra tỷ lệ 1.53 lần nên pixel không đều. Đổi thành **64 px** (gấp đúng 2 lần). Icon đã có khung riêng, nên có thể ẩn hoặc làm mờ khung ô cũ phía sau cho đỡ trùng hai lớp viền.
   - Giữ nguyên lớp phủ hồi chiêu và chữ phím (2/3/4/V/C) đè lên trên icon.
   - **Nút R:** đang dùng `UiPixelArt.Icon("sword")` cho mọi class. Thay bằng `Icon_{Class}_R_Combo.png` theo class hiện tại (có thể thêm `Sprite comboIcon` vào `ClassKit`). Nút R rộng 120 px: đặt icon **96 px** (gấp đúng 3 lần) thay cho 72% (≈86 px).
4. Kiểm tra: đổi lần lượt F1–F5 thấy đúng bộ 5 icon; hồi chiêu vẫn che đúng; ở độ phân giải 1080p không bị nhoè (Filter Point, kích thước gấp đúng 2 lần).

## 4. Kiểm tra pixel đã chạy

30/30 icon: viền ngoài 1 px trong suốt, 0 pixel bán trong suốt. Màu lấy từ bảng màu nhân vật và bảng VFX của từng class (cùng nguồn với sprite và VFX đã giao).
