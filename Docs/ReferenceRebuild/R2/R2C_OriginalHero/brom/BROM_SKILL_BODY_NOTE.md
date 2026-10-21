# Brom — frame thân riêng cho skill: ghi chú cho Cursor

**Ngày:** 2026-10-21 · **Người viết:** Claude (chỉ art + ghi chú, không sửa code, scene hay prefab)
**Đã duyệt tư thế:** `brom/BROM_SKILL_BODY_CANDIDATE_v1.png` · **Bảng duyệt bản xuất:** `brom/Brom_skill_body_review.png`
**Nguồn:** `tools/BromSkillGen.cs`, chạy `tools/RenderBromSkills.ps1` (xuất PNG, bảng duyệt và kiểm tra pixel).
**Liên quan:** `brom/BROM_KIT_SPEC.md`. Số liệu skill giữ nguyên; tài liệu này thay các clip Attack/Walk/Idle đang dùng tạm bằng frame riêng.

## 1. File giao (`unity_export_brom/Skills/`)

Không đụng `Brom_Sheet.png` và `Brom_Anchors.csv`.

| File | Nội dung |
|---|---|
| `Brom_SkillSheet.png` | 128×384, ô 32×32, pivot (16, 30) = (0.5, 0.0625), PPU 16, Point, không nén. **12 hàng** (bảng dưới). Side **quay trái** như sheet Brom |
| `Brom_SkillAnchors.csv` | 14 cột giống `Brom_Anchors.csv`: `key,main_x,main_y,main_dir,main_order,hand_x,hand_y,off_x,off_y,off_view,off_order,slash,slash_order,off_dir`. Dùng lại `Brom_Hammer` (8 hướng), `Brom_Hand`, `Brom_Shield`; slash = −1 vì VFX skill nằm trong `Skills/` của bộ Brom |
| `BROM_SKILL_LAYOUT.csv` | Hàng → skill, view, số frame, danh sách key |

| Hàng | Skill | View | Frame | Key |
|---|---|---|---|---|
| 0–2 | Nện Đất | Down / Up / Side | 3 (giơ búa qua đầu, nện xuống, giữ lúc va chạm) | `Skill_GroundSlam_{View}_{0,1,2}` |
| 3–5 | Húc Khiên | Down / Up / Side | 2 (lặp lao) | `Skill_ShieldCharge_{View}_{0,1}` |
| 6–8 | Gầm Thách | Down / Up / Side | 2 (hít vào, gầm dang tay) | `Skill_TauntRoar_{View}_{0,1}` |
| 9–11 | Tường Sắt | Down / Up / Side | 1 (giữ thế trụ, khiên cắm đất) | `Skill_IronWall_{View}_0` |
| — | Động Đất | dùng lại Nện Đất | F0 → F1, lặp 3 lần | không có frame riêng |

**Tên sprite khi cắt:** `Brom_` + key, ví dụ `Brom_Skill_IronWall_Side_0`.

## 2. Việc cần làm phía Unity (Cursor)

1. Copy `unity_export_brom/Skills/` vào thư mục art của Brom trong `Assets/`, rồi cắt sprite theo bảng trên.
2. **Anchor:** nối các dòng của `Brom_SkillAnchors.csv` vào bảng anchor của Brom (mọi key đều mới), hoặc cho `EquipmentSet` đọc thêm CSV này.
3. **Animator:** gọi từ `SkillBehaviour` của bộ Brom:

| Clip | Frame × thời gian | Gắn với |
|---|---|---|
| GroundSlam | F0 0.15 s → F1 0.06 s (impact, sinh `Brom_GroundSlam`) → F2 0.15 s | phím 2 Nện Đất (impact sau 0.15 s như spec) |
| ShieldCharge | F0/F1 × 0.07 s, lặp suốt 0.28 s lao | phím 3 Húc Khiên |
| TauntRoar | F0 0.12 s → F1 giữ 0.3 s (sinh `Brom_Roar`) | phím 4 Gầm Thách |
| IronWall | F0 giữ suốt 2 s | phím V Tường Sắt |
| Earthquake | (GroundSlam F0 0.12 s → F1 0.23 s) × 3, mỗi đợt sóng 0.35 s | phím C Động Đất |

4. **Flip:** như Idle/Walk/Attack của Brom (`body.flipX` khi quay phải). Anchor và hướng búa ở view Side đã được lật sẵn trong CSV.
5. **Kiểm tra trong Sandbox:** dùng 4 skill (và C) ở 3 hướng × quay trái/phải. Búa, găng và khiên đúng tay, không giật khi đổi frame; chân trên hàng 29. Level 1–4 không đổi.

## 3. Kiểm tra pixel đã chạy

`Brom_SkillSheet.png`: 0 pixel ở viền ô, 0 pixel bán trong suốt, **mọi ô đặt chân ở hàng 29**. Màu lấy từ bảng màu roster Brom đã duyệt.
