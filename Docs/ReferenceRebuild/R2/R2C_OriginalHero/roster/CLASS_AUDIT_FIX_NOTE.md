# Bổ sung sau khi kiểm tra art 5 class (bàn giao cho Cursor)

**Ngày:** 2026-10-21 · **Người viết:** Claude (chỉ art, không sửa code hay scene) · **Trạng thái:** chờ duyệt, **chưa import vào `Assets/`**

Kiểm tra art của 5 class tìm ra 3 chỗ thiếu. Cả 3 đã bổ sung trong Docs.

## 1. Vex: VFX cho Bước Bóng (phím 2) và Vũ Điệu Lưỡi Dao (phím 4)

Hiện code dùng tạm khói bom và lưỡi liềm procedural nhuộm đỏ. Nay có sprite riêng trong `unity_export_vex/Gear/`:

| File | Ô | Frame | Pivot | Cách dùng |
|---|---|---|---|---|
| `Vex_ShadowStep.png` | 32×32 | 4 | (16,30) = (0.5, 0.0625), **chân** | Cột khói mực bốc lên, vệt đỏ, mắt đỏ. Phát **2 lần**: tại chỗ Vex biến mất và tại chỗ Vex hiện ra sau lưng quái. 0 nổ + loé mắt, 1 cao nhất, 2 tan, 3 sợi khói. Khoảng 0.06 s/frame |
| `Vex_BladeDance_Back.png` | 64×48 | 6 | (32,28) = (0.5, 0.4167), tâm thân | Frame = nhát 1–6, mỗi nhát 0.1 s (khớp 6 nhát trong 0.6 s). Vẽ **sau thân** |
| `Vex_BladeDance_Front.png` | 64×48 | 6 | (32,28) | Cùng frame, vẽ **trước thân** |

Ảnh duyệt: `vex/VEX_VFX_ADD_CANDIDATE_v2.png`. Code sinh: `tools/VexVfxGen.cs`.

## 2. Death đủ 3 hướng cho cả 5 class

Trước đây chỉ có `Death_Down_0..2`. Nay mỗi class có thêm 6 file trong `Extra/`:

| Frame | Up | Side (vẽ quay trái) |
|---|---|---|
| 0 quỳ | Idle Up hạ thân trên 3 px xuống chân | Idle Side hạ thân trên 3 px |
| 1 ngã | Death_Down_1 lật ngang | Death_Down_1 |
| 2 nằm (giữ) | Death_Down_2 lật ngang | Death_Down_2 |

- **Anchor** thêm vào `{Class}_Anchors.csv`, ngay sau `Death_Down_2`: `Death_Up_0..2`, `Death_Side_0..2`.
  - Frame 0: lấy anchor `Hurt_{view}_0` (vũ khí đeo sau lưng) hạ 3 px.
  - Frame 1: không vẽ trang bị (−99), giống Down.
  - Frame 2: vũ khí rơi; ở Up được lật theo thân, kể cả chỉ số hướng (MirrorDir).
- Sheet Side vẫn quay trái, runtime dùng `flipX` như mọi frame thân.
- Code cũ chỉ phát `Death_Down` vẫn chạy bình thường. Muốn dùng hướng mới thì chọn `Death_{hướng lúc chết}`.
- Ảnh duyệt: `roster/DEATH_VIEWS_REVIEW.png`.

## 3. Manifest cho Aren và Vex

- Thêm `unity_export/AREN_MANIFEST.csv` và `unity_export_vex/VEX_MANIFEST.csv`, cùng định dạng với Brom/Lyra/Selene. Liệt kê sheet, Extra (Hurt + Death 3 hướng), Gear, Skills.
- Pivot trang bị của Aren/Vex ghi theo bản đã import (giữa ô), trừ hai VFX mới của Vex.
- `BROM_MANIFEST.csv`, `LYRA_MANIFEST.csv`, `SELENE_MANIFEST.csv` được thêm các dòng Death Up/Side.

## Xuất lại

Chạy `tools/RenderClassAuditFix.ps1` **sau cùng**: sau các bản xuất class và sau `RenderBackGearExport.ps1`. Chạy lại nhiều lần vẫn an toàn, không nhân đôi dòng.
