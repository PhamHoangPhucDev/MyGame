# Bộ kỹ năng riêng cho 5 class (combo + skill + hiệu ứng) — bản đề xuất, chờ duyệt

**Ngày:** 2026-10-20 · **Chỉ là thiết kế, chưa code.** Ảnh minh hoạ hiệu ứng: `CLASS_VFX_CONCEPTS.png`.

## 0. Nguyên tắc chung

- **Giữ nguyên phím của hệ hiện tại**, người chơi không phải học lại:
  - **R:** combo đánh thường.
  - **2, 3, 4, V, C:** 5 skill.
  - Giữ R thì đánh liên tục; combo reset sau `SlashChainReset`.
- **Ngân sách sát thương:** lấy bộ hiện tại làm chuẩn (R 18 sát thương, chuỗi 3 đòn, hồi 0.35 s; skill 20–42 sát thương, hồi 4–10 s). Mỗi class lệch theo vai trò, nhưng tổng DPS chênh không quá khoảng 15%. Tank đổi sát thương lấy máu và khống chế; sát thủ đổi máu lấy sát thương dồn.
- **Mỗi class có một bảng màu VFX riêng** và **cảm giác đòn riêng** (độ dài hitstop, rung camera). Nhìn màu là biết class nào đang đánh.
- **VFX gắn đúng chỗ:**
  - Cận chiến: gắn vào mũi vũ khí (như vệt chém Aren đã duyệt).
  - Tầm xa: gắn vào đầu đạn.
  - Hiệu ứng mặt đất: vẽ elip dẹt theo góc nhìn top-down.
- **Thân mỗi class cần frame tấn công riêng:** búa bổ (Brom), giương cung (Lyra), vung gậy và niệm (Selene), đâm hai tay (Vex). Aren dùng bộ đã duyệt.

## 1. Chỉ số gốc

| Class | Máu | Tốc chạy | Giảm sát thương | Tầm đánh thường | Vai trò |
|---|---|---|---|---|---|
| Aren — Hiệp sĩ | ×1.0 | ×1.0 | 0% | Cận, cung 1.35 | Cân bằng, có khiên |
| Brom — Người lùn | ×1.5 | ×0.85 | 15% | Cận, cung 1.5, chậm | Tank, khống chế diện rộng |
| Lyra — Elf | ×0.85 | ×1.1 | 0% | Xa 6 ô | DPS tầm xa, thả diều |
| Selene — Phù thủy | ×0.8 | ×1.0 | 0% | Xa 5.5 ô | Sát thương phép diện rộng |
| Vex — Sát thủ | ×0.8 | ×1.15 | 0% | Cận, cung 1.1, rất nhanh | Dồn sát thương, cơ động |

## 2. Aren — Hiệp sĩ · VFX: trắng, xanh băng, điểm vàng

| Phím | Tên | Hiệu quả | Số liệu | Hiệu ứng |
|---|---|---|---|---|
| R | **Kiếm Hiệp Sĩ** (3 đòn) | Chém chéo phải → chém chéo trái → **đâm thẳng** (đòn kết: đường đâm dài 1.8, đẩy lùi mạnh) | 18 / 18 / 26 · hồi 0.35 s | Lưỡi liềm xanh băng ở mũi kiếm (đã duyệt). Đòn kết: vệt đâm thẳng trắng + tia vàng |
| 2 | **Khiên Đập** | Lao 2 ô bằng khiên, choáng 0.6 s | 24 · hồi 5 s | Sóng hình nón màu vàng + loé sáng trên khiên |
| 3 | **Xung Phong** | Lao 4 ô, trúng mọi quái trên đường | 26 · hồi 6 s | Bóng mờ xanh (3 bóng) + bụi chân |
| 4 | **Lốc Kiếm** | Xoay 3 vòng quanh người | 20 × 3 · hồi 7 s | 3 vòng cung xanh quanh thân |
| V | **Thành Lũy** | Nhận 35% sát thương trong 1.4 s, phản 20% | hồi 8 s | Khiên lục giác vàng nhạt bao quanh |
| C | **Chém Bình Minh** | Lưỡi liềm lớn phía trước, đẩy lùi | 42 · hồi 10 s | Lưỡi liềm lớn trắng–vàng có lõi sáng |

**Cảm giác đòn:** hitstop 0.048 s (đòn kết 0.058 s), rung vừa. Giữ như hiện tại.

## 3. Brom — Người lùn Tank · VFX: cam, nâu đất, tia lửa thép

| Phím | Tên | Hiệu quả | Số liệu | Hiệu ứng |
|---|---|---|---|---|
| R | **Búa Rèn** (3 đòn) | Vung ngang → bổ xuống → **nện đất** (đòn kết: rung nhỏ, vùng bán kính 1.6) | 22 / 22 / 30 · hồi 0.5 s | Bụi cam và mảnh đá văng. Đòn kết: vòng nứt đất + tia lửa |
| 2 | **Nện Đất** | Vùng bán kính 2.2 quanh người, choáng 0.8 s | 28 · hồi 6 s | Vết nứt toả ra + vòng bụi nâu + đá văng |
| 3 | **Húc Khiên** | Lao 2.5 ô, hất quái sang hai bên | 20 · hồi 6 s | Khiên tròn loé vàng + vệt bụi |
| 4 | **Gầm Thách** | Kéo quái trong bán kính 4 về phía mình 3 s, bản thân giảm 20% sát thương | hồi 10 s | Vòng sóng âm cam–đỏ toả ra |
| V | **Tường Sắt** | Nhận 20% sát thương trong 2 s, đứng yên | hồi 10 s | Vòm thép xám có đinh tán |
| C | **Động Đất** | 3 đợt rung lan dần (bán kính 1.5 → 2.5 → 3.5) | 25 × 3 · hồi 14 s | 3 vòng nứt lan dần + đá bắn lên |

**Cảm giác đòn:** **hitstop dài 0.07 s**, rung camera mạnh (0.12). Đòn chậm nhưng nặng.

## 4. Lyra — Elf Cung thủ · VFX: xanh lá, lá cây, vàng

| Phím | Tên | Hiệu quả | Số liệu | Hiệu ứng |
|---|---|---|---|---|
| R | **Mũi Tên Rừng** (3 phát) | 2 phát nhanh → **phát nạp** (xuyên qua 3 quái) | 14 / 14 / 24 · tầm 6 · hồi 0.38 s | Mũi tên có vệt xanh. Phát nạp: mũi tên phát sáng + lá bay |
| 2 | **Tên Quạt** | 5 mũi tên toả hình quạt 60° | 16 mỗi mũi · hồi 5 s | 5 vệt xanh toả quạt |
| 3 | **Nhảy Lùi** | Lùi 3 ô, để lại dây leo trói (giảm 50% tốc chạy trong 2 s) | hồi 6 s | Lá cây bung + vòng dây leo trên đất |
| 4 | **Mưa Tên** | Vùng bán kính 2, cách 4 ô phía trước, 6 lượt trúng trong 1.2 s | 10 × 6 · hồi 8 s | Tên rơi xiên + tia xanh khi chạm đất |
| V | **Bước Gió** | +40% tốc chạy 3 s, né đòn kế tiếp | hồi 9 s | Vệt gió xanh nhạt sau chân |
| C | **Tên Xuyên Rừng** | Tia thẳng tầm 9, xuyên mọi quái | 45 · hồi 11 s | Tia dài xanh–vàng có lá xoắn quanh |

**Cảm giác đòn:** hitstop ngắn 0.035 s, không rung (chỉ rung ở C). Nhẹ, nhanh, thả diều.

## 5. Selene — Phù thủy · VFX: tím hồng, tím, trắng sao

| Phím | Tên | Hiệu quả | Số liệu | Hiệu ứng |
|---|---|---|---|---|
| R | **Tia Phép** (3 phát) | 2 cầu phép → **nổ sao** (đòn kết: nổ vùng bán kính 1.2 tại mục tiêu) | 15 / 15 / 26 · tầm 5.5 · hồi 0.4 s | Cầu tím hồng kéo vệt lấp lánh. Đòn kết: sao 4 cánh nổ |
| 2 | **Cầu Tinh Tú** | Cầu bay chậm, nổ vùng bán kính 1.8 | 32 · hồi 5 s | Cầu tím lớn + nổ hình sao |
| 3 | **Dịch Chuyển** | Dịch chuyển 3.5 ô | hồi 5 s | Bụi sao tím ở điểm đi và điểm đến |
| 4 | **Vòng Phù Chú** | Vòng bán kính 2.2 tại mục tiêu: giảm 40% tốc chạy, 8 sát thương mỗi 0.5 s trong 3 s | hồi 9 s | Vòng phù chú tím xoay trên đất |
| V | **Khiên Ma Lực** | Hấp thụ 40 sát thương trong 4 s | hồi 10 s | Bong bóng tím trong suốt |
| C | **Mưa Sao Băng** | 5 sao rơi trong vùng bán kính 3 trong 1.5 s | 28 mỗi sao · hồi 13 s | Vệt sao rơi chéo + nổ sáng |

**Cảm giác đòn:** hitstop 0.04 s, rung nhẹ khi nổ. Màn hình loé tím rất nhẹ ở C.

## 6. Vex — Sát thủ Ma tộc · VFX: đỏ thẫm, khói đen, bạc

| Phím | Tên | Hiệu quả | Số liệu | Hiệu ứng |
|---|---|---|---|---|
| R | **Song Đao** (4 đòn) | Đâm trái → đâm phải → chém chéo → **chém chữ X** (đòn kết gây chảy máu 3 s) | 10 / 10 / 10 / 22 · hồi 0.22 s | Vệt chém mảnh đỏ. Đòn kết: chữ X đỏ + tia đỏ |
| 2 | **Bước Bóng** | Dịch chuyển ra sau lưng quái gần nhất (tầm 5), đòn kế gây ×2 sát thương | hồi 6 s | Khói đen phụt + loé mắt đỏ |
| 3 | **Phi Dao** | Ném 3 dao hình quạt, gây chảy máu | 14 mỗi dao · hồi 5 s | Dao bạc kéo vệt đỏ |
| 4 | **Vũ Điệu Lưỡi Dao** | 6 nhát nhanh quanh người trong 0.6 s | 9 × 6 · hồi 8 s | Lưỡi liềm đỏ dồn dập quanh thân |
| V | **Bom Khói** | Tàng hình 2 s, quái mất mục tiêu | hồi 12 s | Mây khói xám đen |
| C | **Đoạt Mệnh** | Lao tới mục tiêu; ×3 sát thương nếu mục tiêu dưới 30% máu | 35 (105) · hồi 12 s | Chữ X đỏ thẫm lớn + loé tối màn hình |

**Cảm giác đòn:** hitstop rất ngắn 0.03 s, không rung (đòn kết và C rung mạnh, ngắn). Nhanh, sắc.

## 7. Kiến trúc đề xuất (chưa code)

1. **`ClassKit` (ScriptableObject)**, mỗi class một bộ:
   - Chỉ số gốc.
   - `ComboStep[]`: sát thương, hình dạng (Cung / Đâm / Đạn / Vùng), hướng vung, thời điểm impact, VFX, hitstop, rung, frame thân.
   - `SkillDef[5]`: phím, thời gian hồi, kiểu (Cung cận chiến, Lao, Đạn, Đạn quạt, Vùng đất, Vùng quanh người, Buff, Dịch chuyển, Vùng kéo dài), tham số, VFX, âm thanh, icon.
2. **`VfxLibrary`:** id → frame sprite + quy tắc gắn (mũi vũ khí, đầu đạn, mặt đất, quanh thân). Thay dần hiệu ứng vẽ procedural của `CombatVfx`.
3. **`ClassCombat`** (component mới) chạy `ClassKit`, chỉ dùng trong Sandbox:
   - Tái dùng `HitStop`, `CameraFollow.Shake`, `CombatTargeting`, `Health`.
   - Có phím đổi class trong Sandbox (ví dụ F1–F5) để thử nhanh.
   - `PlayerCombat` hiện tại giữ nguyên cho Level 1–4. Khi được duyệt, bộ của Aren sẽ thay đúng bằng số liệu hiện tại.
4. **Hotbar:** đọc tên, icon và thời gian hồi từ `SkillDef`.
5. **Trang bị:** dùng `EquipmentVisual` như Aren. Vũ khí tầm xa có anchor đầu đạn (đầu cung, đỉnh gậy).

## 8. Thứ tự làm đề xuất (mỗi bước dừng duyệt)

1. Duyệt bản thiết kế này và ảnh VFX concept.
2. `ClassKit` + `ClassCombat` với **bộ của Aren** (trùng hành vi hiện tại) trong Sandbox.
3. Lần lượt từng class: frame tấn công của thân → VFX → số liệu → thử trong Sandbox. Thứ tự gợi ý: Vex (cận chiến, gần Aren nhất) → Brom → Lyra → Selene (cần hệ đạn).

## 9. Quyết định 2026-10-20

**Bộ skill hiện tại của `PlayerCombat`** (Flame Burst, Dash Slash, Whirlwind, Guard, Crescent) **chỉ là bản demo.** Mỗi class dùng bộ riêng như các mục 2–6:
- **Aren:** chuyển từ bộ "giống hệt cũ" (`ClassKit_Aren` hiện tại, chỉ để chuyển đổi an toàn) sang bộ đã thiết kế: Khiên Đập, Xung Phong, Lốc Kiếm, Thành Lũy, Chém Bình Minh, đòn kết đâm thẳng 26.
- **Vex:** đã xong (commit `bee0df0`).
- **Brom, Lyra, Selene:** còn thiếu animation và bộ kỹ năng.
- **Kiểm thử "giống hệt cũ":** chỉ còn dùng để kiểm tra Level 1–4 cho tới khi các level này chuyển sang class kit. Việc chuyển đó cần duyệt riêng.
