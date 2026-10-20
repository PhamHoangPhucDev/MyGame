# R2C — Bộ Vex (Sát thủ Ma tộc) trong Sandbox: báo cáo

**Ngày:** 2026-10-20 · **Nhánh:** `reference_rebuild_r2c` · **Chưa commit** · Chỉ Sandbox (phím **F5**)
**Sao lưu scene:** `IncomingAssets/backup_before_R2C_Aren/ReferenceCombatSandbox_beforeVex.unity`

## 1. Art (thiết kế Vex v5 đã duyệt)

| Phần | File | Ghi chú |
|---|---|---|
| Thân 3 hướng | `tools/VexGen.cs` · ảnh `vex/Vex_body_v1_review.png` | Down = sprite đã duyệt. **Up** (mới): tóc bạc phía sau, nút bịt mắt đỏ với 2 đuôi buông trên cổ áo, áo choàng xám phủ lưng. **Side 3/4** (mới): dải bịt mắt ngang mặt, đuôi bay ra sau, áo choàng phía sau |
| Idle × 4 | | Đuôi bịt mắt bay (F1, F3), thở (F2). 0.25 / 0.15 / 0.25 / 0.15 s |
| Walk × 4 | | Tiếp đất hạ 1 px, sải chân. 4 × 0.125 s |
| Attack × 3 | | Windup / strike (lao tới + sải) / recover. Mốc 0 / 0.12 / 0.19 s, khớp combat |
| Hurt, Death | `unity_export_vex/Extra` | Loé trắng rồi nhuộm đỏ; quỳ → nằm (xoay 90° không méo) |
| Song đao | `Vex_Dagger.png` (8 tư thế) | Tay chính và tay phụ dùng chung sprite, gắn theo anchor. **Mỗi đòn trong combo có anchor riêng** (khoá `Attack_*_1@step`): 1 đâm tay phải · 2 đâm tay trái · 3 bắt chéo · 4 mở chữ X |
| Vệt chém đỏ thẫm | `Vex_Slash.png` (24 frame) | 4 đòn × 3 hướng × 2 frame: vạch chéo trái, vạch chéo phải, chữ thập, chữ X lớn + tia đỏ. Ảnh: `vex/Vex_combo_v1_review.png` |
| VFX skill | `Vex_Smoke.png` (4), `Vex_Knife.png` (8 hướng + vệt đỏ), `Vex_ExecuteX.png` (2) | Ảnh: `vex/Vex_skillvfx_v1_review.png` |

## 2. Code (Sandbox; Level 1–4 không bị ảnh hưởng)

| File | Thay đổi |
|---|---|
| `ClassKit.cs` | Thêm hành vi `ShadowStep`, `ProjectileFan`, `Stealth`, `Execute`; trường cho từng class (bleed, spread, threshold, alpha, tint, frame VFX, sprite đạn); `bodyController` + `equipment` để đổi hình khi đổi class; hàm `FillVex()` |
| `PlayerCombat.cs` | Các hành vi mới (chỉ chạy khi có kit): Bước Bóng, Phi Dao, Bom Khói, Đoạt Mệnh, bleed ở đòn kết, ×2 cho đòn kế sau Bước Bóng, vòng lốc nhuộm màu class. Thêm `CurrentComboStep` |
| `EquipmentVisual.cs` | `Configure(EquipmentSet)` để đổi bộ trang bị lúc chạy; vũ khí tay phụ có 8 tư thế (`off_dir`); anchor và vệt chém riêng cho từng đòn combo. Aren dùng như cũ |
| `EquipmentSet.cs` (mới) | ScriptableObject cho bộ trang bị: anchor, vũ khí chính và phụ, khiên, tay, vệt chém |
| `PlayerVisual.cs` | Thêm `RebindBodyController()`: đổi controller lúc chạy. Chỉ `ClassCombat` gọi |
| `ClassCombat.cs` | Đổi class thì đổi luôn controller của thân và bộ trang bị |
| `StatusBleed.cs`, `KitProjectile.cs`, `KitVfx.cs` (mới) | Chảy máu theo thời gian; đạn bay thẳng; phát VFX dạng sprite và phủ tối màn hình |
| `CombatVfx.cs` | Thêm `SpawnCrescentTinted` (hàm mới; hàm cũ giữ nguyên) |
| `R2CVexSandboxSetup.cs` (editor) | Import, dựng `Vex.controller` (13/13 state), `EquipmentSet_Vex` và `EquipmentSet_Aren`, `ClassKit_Vex`, gán vào F5 |

## 3. Số liệu Vex (theo `CLASS_KIT_DESIGN.md`)

- **Chỉ số:** máu ×0.8 (320), tốc chạy ×1.15.
- **R – Song Đao:** 10 / 10 / 10 / **22** (đòn kết chảy máu 3 sát thương mỗi 0.5 s trong 3 s).
- **2 – Bước Bóng:** tầm 5, ra sau lưng quái, đòn R kế ×2 · hồi 6 s.
- **3 – Phi Dao:** 3 dao, quạt 30°, 14 sát thương + chảy máu 2×4 · hồi 5 s.
- **4 – Vũ Điệu Lưỡi Dao:** 6 nhát × 9, bán kính 1.6 · hồi 8 s.
- **V – Bom Khói:** 2 s, mờ 35%, miễn sát thương · hồi 12 s.
- **C – Đoạt Mệnh:** 35, **×3 khi mục tiêu dưới 30% máu** · hồi 12 s.

**Lệch so với thiết kế (có lý do):**
1. **Thời gian hồi của combo là 0.26 s thay vì 0.22 s.** Thân giữ trạng thái tấn công 0.26 s (`PlayerVisual`). Nhanh hơn thì frame chém bị cắt.
2. **Hitstop 0.03 s thành 0.04 s,** vì `HitStop` giới hạn trong 0.04–0.06 s.
3. **Bom Khói:** quái **chưa mất mục tiêu.** Việc đó cần sửa AI quái, mà AI quái thuộc nhóm hệ thống phải giữ nguyên. Hiện Vex miễn sát thương và mờ đi trong 2 s. Nếu muốn quái mất dấu thật, cần duyệt riêng một thay đổi nhỏ trong Enemy AI.

## 4. Kiểm thử trong Play Mode (Sandbox)

**Kiểm thử chức năng (đóng băng thời gian, đo trực tiếp):** `vex/unity_test/vex_functional_test.txt`

| Mục | Kết quả |
|---|---|
| F5 | ✔ controller `Vex`, `EquipmentSet_Vex`, máu 320, tốc chạy ×1.15 |
| Combo 4 đòn | ✔ 10 / 10 / 10 / 22 sát thương thực trúng; bước combo 0 → 3; bán kính 1.10 / 1.43; thời gian hồi 0.26 s; hitstop 0.040 / 0.050; **chảy máu chỉ ở đòn 4** |
| Bước Bóng | ✔ dịch chuyển 1.3, ra **sau lưng** quái; đòn R kế = **20** (×2), hệ số trở về 1 sau đó |
| Đoạt Mệnh | ✔ quái đầy máu: 35 · quái 25% máu: **kết liễu** · hitstop 0.060 |
| Bom Khói | ✔ đang tàng hình, hệ số sát thương nhận = 0; sau 2.5 s: hết tàng hình, hệ số = 1, độ mờ trở lại 1 |
| Phi Dao (thời gian thật) | ✔ bắn 3 dao; 1 dao trúng (−14), chảy máu tick thêm (tổng −20); đạn tự huỷ |
| Vũ Điệu Lưỡi Dao (thời gian thật) | ✔ đủ 6 đợt; HP cả nhóm quái 270 → 105 |

**Ảnh trong Unity:** `vex/unity_test/Vex_unity_crops_x3.png`
- `V01–V04`: 4 đòn combo, mỗi đòn một vệt chém khác nhau.
- `V2_*`: chụp đóng băng ngay lúc dùng skill. Thấy rõ chữ X của Đoạt Mệnh, khói và thân mờ của Bom Khói. Dao bay của Phi Dao khó nhìn giữa đám quái, nhưng số liệu đo đã xác nhận.

**Lỗi tìm thấy khi chụp và đã sửa:** skill có động tác vung (Phi Dao, Vũ Điệu, Đoạt Mệnh) hiện nhầm vệt chém của đòn combo trước đó (chữ X). Giờ skill dùng vệt thường. Bản sửa chỉ áp cho các hành vi của Vex, nên Aren không đổi.

## 5. Đảm bảo không làm hỏng phần đã có

| Kiểm tra | Kết quả |
|---|---|
| Aren sau khi đổi Vex → F1 | ✔ controller `Aren`, `EquipmentSet_Aren`, máu 400. **So kit với hành vi cũ: 16/16 khớp** (sát thương, bán kính, hướng vung, thời gian hồi, hitstop). Xem `vex/unity_test/aren_recheck_after_vex.txt` |
| Level 1–4 (Play) | ✔ không có kit, `ClassCombat` hay `EquipmentVisual`; R, phím 2 và C chạy bình thường; **0 lỗi** |
| Biên dịch và console | ✔ 0 lỗi |

## 6. Còn lại / đề xuất

1. Hotbar chưa hiện tên và icon skill theo class (đang hiện tên của bộ cũ).
2. Bom Khói khiến quái mất mục tiêu: cần duyệt thay đổi nhỏ trong Enemy AI.
3. Class kế tiếp đề xuất: **Brom**. Cần thêm giảm sát thương gốc tách khỏi Guard, hành vi kéo quái (Gầm Thách), và động đất nhiều đợt.
