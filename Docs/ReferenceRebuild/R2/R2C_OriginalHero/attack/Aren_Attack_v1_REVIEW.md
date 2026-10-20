# Aren — Attack v1 (kiếm + khiên, 3 hướng × 3 frame): chờ duyệt

**Ngày:** 2026-10-20 · **Không import Unity** · Hệ thống trang bị: xem `EQUIPMENT_SYSTEM.md`.
**Tạo lại:** `tools/RenderEquip.ps1`.

## File

| File | Nội dung |
|---|---|
| `Aren_AttackDown_body_v1.png`, `Aren_AttackUp_body_v1.png`, `Aren_AttackSide_body_v1.png` | Thân 3 frame (96×32), **không có vũ khí** |
| `Weapon_sword_steel_8dir.png` | Kiếm dài thép: 8 tư thế, ô 48×48, tâm ô là tay cầm |
| `Weapon_sword_rune_8dir.png`, `Weapon_dagger_steel_8dir.png` | Kiếm hàn (skin vũ khí), dao găm của Vex |
| `Shield_heater_{Down,Up,Side}.png`, `Shield_roundwood_{Down,Up,Side}.png` | Khiên, ô 16×16, tâm ô là quai cầm |
| `Aren_Attack_anchors_v1.csv` | Anchor, tư thế và thứ tự vẽ của từng frame |
| `Aren_Attack_v1_review.png` | Ghép thân + kiếm + khiên: 3 hướng × 3 frame |
| `Equip_swap_demo_v1.png` | Cùng frame thân, đổi kiếm, khiên và skin |
| `Equip_weapon_sheets_v1.png` | Toàn bộ sprite vũ khí và khiên |
| `Equip_roster_weapons_v1.png` | Vũ khí mặc định của Brom, Lyra, Selene, Vex ở tư thế đứng |

## Chuyển động

| Frame | Thân | Kiếm (Down / Up / Side) | Khiên | Thời gian |
|---|---|---|---|---|
| F0 windup | Như Idle F1 (áo choàng xòe) | Giơ chéo lên sau vai, vẽ sau thân / qua vai phải, vẽ trước thân (gần camera) / giơ ra sau, vẽ sau thân | Trước thân, giữ thủ | 0.08 s |
| F1 strike | Lao tới 1 px + sải chân (như frame tiếp đất của Walk) | Chém chéo xuống trước / chém lên phía trước, vẽ sau thân / chém ngang về trước | Trước thân, hạ 1 px theo thân | 0.10 s (impact khoảng 0.12 s, khớp `slashImpactDelay`) |
| F2 recover | Như Idle F0 | Chĩa xuống / chéo về trái / chéo xuống | Trước thân | 0.08 s |

Tổng 0.26 s, bằng thời lượng clip Attack hiện tại.

## Đã sửa trong lúc làm

- **Kiếm quá dài:** lúc đầu khoảng 17 px (như cây giáo), rút còn 13 px, rồi theo phản hồi rút tiếp **lưỡi còn 6 px** (cả chuôi khoảng 10–11 px, bằng nửa chiều cao thân). Kiếm hàn: lưỡi 7 px.
- **Huy hiệu khiên** lúc đầu là chữ thập vàng, dễ gợi biểu tượng tôn giáo. Đổi thành dải ngang vàng. Khiên của Brom cũng đổi núm từ hình chữ thập sang khối vuông.
- **Búa của Brom** dính vào mũ: dời ra xa thân.

## Điểm cần quyết

1. **Tay cầm kiếm:** ở hướng Down, kiếm ở tay bên trái người xem (dưới áo choàng), khiên ở tay có vai phủ vải trắng. Có thể đổi bên nếu muốn.
2. **Khiên luôn ở trước thân** cả 3 frame. Có thể đặt khiên ra sau ở frame chém hướng Up.
3. Vệt chém (VFX) chưa vẽ ở bước này. Nó sẽ gắn vào mũi kiếm của F1.

## Sửa theo phản hồi (2026-10-20)

- **Kiếm sĩ "kiếm dài quá":** lưỡi kiếm 9 → 6 px.
- **Elf "như đang không cầm cung":**
  - Cung vẽ lại với vòng cung rộng hơn (gỗ cách dây 3 px).
  - Cung đặt sát bên người.
  - Thêm **lớp tay** (`EquipGen.Hand`, nắm tay 2×2 màu da) vẽ đè lên chuôi cung.
  - Lớp tay dùng chung cho mọi vũ khí cầm phía trước, khi cần thấy rõ là đang cầm. Đã ghi vào hệ thống trang bị.
