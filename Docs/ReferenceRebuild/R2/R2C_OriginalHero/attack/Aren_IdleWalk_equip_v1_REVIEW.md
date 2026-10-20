# Aren — Idle + Walk có kiếm và khiên (anchor v1): chờ duyệt

**Ngày:** 2026-10-20 · **Không import Unity** · Thân Idle/Walk đã duyệt, giữ nguyên.
**Tạo lại:** `tools/RenderEquipMove.ps1`.

## File

| File | Nội dung |
|---|---|
| `Aren_Idle_equip_v1_review.png` | Idle 3 hướng × 4 frame, thân + kiếm hạ + khiên thủ |
| `Aren_Walk_equip_v1_review.png` | Walk 3 hướng × 4 frame |
| `Aren_IdleWalk_anchors_v1.csv` | 24 dòng: anchor, tư thế và thứ tự vẽ cho từng frame Idle/Walk |

## Tư thế nghỉ

| Hướng | Kiếm (tay chính) | Khiên (tay phụ) |
|---|---|---|
| Down | Hạ chĩa xuống, bên trái người xem, vẽ trước thân, có găng tay đè lên chuôi | Mặt khiên, bên phải, trước thân |
| Up | Hạ chĩa xuống, bên phải người xem (tay phải), trước thân | Mặt trong có quai, bên trái, trước thân |
| Side | Tay xa, mũi chéo ra sau, vẽ **sau** thân | Tay gần, trước thân |

## Chuyển động

- **Idle:** tay không đổi vị trí. Ở F2 chỉ có mũ gật, kiếm và khiên đứng yên. Thời gian như Idle đã duyệt (0.25 / 0.15 / 0.25 / 0.15 s).
- **Walk:**
  - F0 và F2 (tiếp đất): kiếm và khiên hạ 1 px theo thân.
  - F1 và F3 (chuyển bước): kiếm lệch 1 px ra sau, rồi ra trước (vung tay).
  - Mỗi frame 0.125 s.

## Thay đổi khác

- **Thêm găng tay** (`EquipGen.Gauntlet`, lớp `hand`, màu thép) vẽ đè lên chuôi kiếm, để kiếm hạ trông như đang được cầm.
- **Khiên hướng Side rộng hơn** (3 → 4 px) và dời ra ngoài 1 px, vì bản cũ lẫn vào thân. Thay đổi này cũng áp dụng cho Attack Side đã duyệt (chỉ khiên to hơn; thân và kiếm không đổi).
- **Walk Side F1:** lúc đầu kiếm chĩa thẳng xuống giữa hai chân, trông lạ. Đổi thành giữ hướng chéo và chỉ dịch qua lại.

## Bước tiếp theo đề xuất

- Vệt chém (VFX) gắn vào mũi kiếm ở F1 của Attack.
- Hurt và Death cho Aren (thân + rơi kiếm/khiên).
- Sau đó là Attack cho 4 nhân vật còn lại. Lyra cần tách ống tên khỏi thân trước.
- Khi bộ Aren đủ (Idle, Walk, Attack, Hurt, Death): import vào Sandbox để thử, kèm component `EquipmentVisual`. Bước này cần duyệt riêng.
