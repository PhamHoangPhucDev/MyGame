# Aren — Walk 3 hướng v1 (4 frame): chờ duyệt

**Ngày:** 2026-10-20 · **Không import Unity** · Idle 3 hướng đã duyệt.
**Tạo lại:** `tools/RenderArenWalk.ps1` (dùng `HeroConceptGen.cs` + `ArenIdleGen.cs` + `ArenWalkGen.cs`). Frame gốc là Idle F0 đã duyệt của từng hướng.

## File

| File | Nội dung |
|---|---|
| `Aren_WalkDown_v1.png` | 128×32, 4 frame. **Giao nộp** |
| `Aren_WalkUp_v1.png` | 128×32, 4 frame (sau lưng). **Giao nộp** |
| `Aren_WalkSide_v1.png` | 128×32, 4 frame (3/4 quay phải; quay trái dùng flipX). **Giao nộp** |
| `Aren_Walk_3views_v1_review.png` | 3 hướng × 4 frame phóng ×8; vạch vàng là hàng đế giày 29 |
| `Aren_Walk_path_v1.png` | Side đi phải và đi trái (flipX) ×4 trên một đường, cùng cỡ thật ×1 của cả 3 hướng |

## Chu kỳ bước (4 × 0.125 s = 0.5 s, bằng clip Walk cũ)

| Frame | Down / Up | Side |
|---|---|---|
| F0 tiếp đất trái | Thân hạ 1 px. Chân trái chạm đất (hàng 29), chân phải nhấc | Thân hạ 1 px. Sải chân: chân sau bên trái, chân trước bên phải |
| F1 chuyển bước | Chân khép (như Idle), gấu áo choàng xòe | Như Down |
| F2 tiếp đất phải | Thân hạ 1 px. Chân phải chạm đất, chân trái nhấc | Sải chân, đổi sáng tối giữa chân gần và chân xa |
| F3 chuyển bước | Chân khép, chỏm lông bay ngược lại | Như Down |

- **Nhún:** ở frame tiếp đất, phần trên (hàng 0–17) hạ 1 px. Đế giày luôn ở hàng 29, nên nhân vật không trôi.
- **Chân:** dùng thép tông giữa + điểm sáng, viền tối, để bàn chân chạm đất nổi rõ trên bóng đổ. Bản đầu dùng thép tối nên khó thấy; đã sửa.
- Kích thước: 14×21 ở frame chuyển bước, 14×20 ở frame tiếp đất.
- Kiếm vẫn là sprite riêng: lúc đi, kiếm đeo sau lưng hoặc ở hông theo thiết kế kiếm sau này.

## Điểm cần quyết

1. **Tốc độ:** 0.5 s mỗi chu kỳ (khớp clip cũ). Hiệp sĩ nặng có thể chậm hơn (0.6 s), nhưng sẽ lệch với tốc độ di chuyển hiện tại của Hero.
2. **Side quay trái** dùng flipX, nên áo choàng đổi bên (như Idle Side đã duyệt).

## Bước tiếp theo đề xuất

Attack 3 hướng cho Aren (thân 3 frame + windup), có điểm `grip` theo từng frame. Thiết kế sprite trường kiếm (đeo lưng và 3 tư thế chém). Dừng để duyệt.
