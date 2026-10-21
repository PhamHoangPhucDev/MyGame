# Vex — frame thân riêng cho skill: ghi chú cho Cursor

**Ngày:** 2026-10-21 · **Người viết:** Claude (chỉ art + ghi chú, không sửa code, scene hay prefab)
**Đã duyệt tư thế:** `vex/VEX_SKILL_BODY_CANDIDATE_v1.png` · **Bảng duyệt bản xuất:** `vex/Vex_skill_body_review.png`
**Nguồn:** `tools/VexSkillGen.cs`, chạy `tools/RenderVexSkills.ps1` (xuất PNG, bảng duyệt và kiểm tra pixel).
**Liên quan:** `classkit/CLASS_KIT_DESIGN.md` mục 6 (bộ skill Vex) và `vex/VEX_BACK_DAGGERS_NOTE.md` (Idle/Walk đeo dao chữ X sau lưng; skill thì rút dao ra tay).

## 1. File giao (`unity_export_vex/Skills/`)

Không đụng `Vex_Sheet.png` và `Vex_Anchors.csv`.

| File | Nội dung |
|---|---|
| `Vex_SkillSheet.png` | 128×416, ô 32×32, pivot (16, 30) = (0.5, 0.0625), PPU 16, Point, không nén. **13 hàng** (bảng dưới). Side **quay trái** |
| `Vex_SkillAnchors.csv` | 14 cột giống `Vex_Anchors.csv`: `main` = dao phải (`main_dir` 0–7), `off` = dao trái (`off_dir` 0–7, `off_view` = view), không có lớp tay (`hand_x` = −99). Dùng lại `Vex_Dagger.png` ô 0–7 |
| `VEX_SKILL_LAYOUT.csv` | Hàng → skill, view, số frame, danh sách key |

| Hàng | Skill | View | Frame | Key |
|---|---|---|---|---|
| 0–2 | Bước Bóng | Down / Up / Side | 2 (thụp người lẩn vào bóng, hiện sau lưng địch với dao giơ sẵn) | `Skill_ShadowStep_{View}_{0,1}` |
| 3–5 | Phi Dao | Down / Up / Side | 2 (vung dao ra sau lấy đà, phóng hai tay về trước) | `Skill_KnifeThrow_{View}_{0,1}` |
| 6–8 | Bom Khói | Down / Up / Side | 1 (thụp xuống, ném bom xuống đất) | `Skill_SmokeBomb_{View}_0` |
| 9–11 | Đoạt Mệnh | Down / Up / Side | 2 (lao thấp hai dao chĩa tới, chém chữ X) | `Skill_Execute_{View}_{0,1}` |
| 12 | Vũ Điệu Lưỡi Dao | dùng chung mọi hướng | 4 (xoay: Down → Side → Up → Side, hai dao chĩa hai bên) | `Skill_BladeDance_{0..3}` |

**Tên sprite khi cắt:** `Vex_` + key, ví dụ `Vex_Skill_Execute_Side_1`.

## 2. Việc cần làm phía Unity (Cursor)

1. Copy `unity_export_vex/Skills/` vào `Assets/Art/Rebuild/R2C/Vex/Skills/`, rồi cắt sprite theo bảng trên.
2. **Anchor:** nối các dòng của `Vex_SkillAnchors.csv` vào bảng anchor của Vex (mọi key đều mới).
3. **Animator:** gọi từ các `SkillBehaviour` đã có của Vex (`ShadowStep`, `ProjectileFan`, `Stealth`, `Execute` và vũ điệu lưỡi dao):

| Clip | Frame × thời gian | Gắn với |
|---|---|---|
| ShadowStep | F0 0.08 s (cùng `Vex_Smoke`) → ẩn thân, dịch chuyển → F1 0.15 s tại điểm đến | phím 2 Bước Bóng |
| KnifeThrow | F0 0.10 s → F1 0.12 s (3 dao `Vex_Knife` sinh ở đầu F1) | phím 3 Phi Dao |
| BladeDance | F0–F3 × 0.05 s, lặp 3 vòng (6 nhát trong 0.6 s, mỗi nửa vòng 1 nhát) | phím 4 Vũ Điệu Lưỡi Dao |
| SmokeBomb | F0 0.15 s (khói bung), rồi vào trạng thái tàng hình | phím V Bom Khói |
| Execute | F0 giữ suốt lúc lao → F1 0.12 s (impact, `Vex_ExecuteX`) | phím C Đoạt Mệnh |

4. **Flip:**
   - 4 skill có view flip như Idle/Walk/Attack của Vex.
   - **BladeDance** là vòng xoay, không phụ thuộc hướng. Hai frame Side đều vẽ quay trái theo quy ước sheet, nên đề xuất **không flip** để chiều xoay luôn giống nhau, giống Lốc Kiếm của Aren.
5. **Kiểm tra trong Sandbox (F2):** dùng 5 skill ở 3 hướng × quay trái/phải. Hai dao đúng tay, không giật khi đổi frame, chân ở hàng 29. Hết skill thì Idle/Walk quay lại đeo dao chữ X sau lưng. Level 1–4 không đổi.

## 3. Kiểm tra pixel đã chạy

`Vex_SkillSheet.png`: 0 pixel ở viền ô, 0 pixel bán trong suốt, **mọi ô đặt chân ở hàng 29**. Màu lấy từ bảng màu roster Vex đã duyệt.
