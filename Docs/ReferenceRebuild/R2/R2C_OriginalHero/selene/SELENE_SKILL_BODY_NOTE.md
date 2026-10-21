# Selene — frame thân riêng cho skill: ghi chú cho Cursor

**Ngày:** 2026-10-21 · **Người viết:** Claude (chỉ art + ghi chú, không sửa code, scene hay prefab)
**Đã duyệt tư thế:** `selene/SELENE_SKILL_BODY_CANDIDATE_v1.png` · **Bảng duyệt bản xuất:** `selene/Selene_skill_body_review.png`
**Nguồn:** `tools/SeleneSkillGen.cs`, chạy `tools/RenderSeleneSkills.ps1` (xuất PNG, bảng duyệt và kiểm tra pixel).
**Liên quan:** `selene/SELENE_KIT_SPEC.md` (số liệu skill giữ nguyên) và `roster/BACK_GEAR_NOTE.md` (Idle/Walk đeo gậy sau lưng; skill thì cầm gậy trên tay).

## 1. File giao (`unity_export_selene/Skills/`)

Không đụng `Selene_Sheet.png`.

| File | Nội dung |
|---|---|
| `Selene_SkillSheet.png` | 128×480, ô 32×32, pivot (16, 30) = (0.5, 0.0625), PPU 16, Point, không nén. **15 hàng** (bảng dưới). Side **quay trái** |
| `Selene_SkillAnchors.csv` | 14 cột giống `Selene_Anchors.csv`. `main_dir` = **chỉ số ô trong `Selene_Staff.png`** (len×8 + dir; 0–7 gậy dài, 8–15 gậy ngắn). `hand` = nắm tay trên cán gậy. `off` = −99 |
| `SELENE_SKILL_LAYOUT.csv` | Hàng → skill, view, số frame, danh sách key |

| Hàng | Skill | View | Frame | Key |
|---|---|---|---|---|
| 0–2 | Cầu Tinh Tú | Down / Up / Side | 2 (giơ gậy thật cao tụ phép, chĩa gậy phóng cầu) | `Skill_StarOrb_{View}_{0,1}` |
| 3–5 | Dịch Chuyển | Down / Up / Side | 2 (cúi gõ gậy xuống đất, hiện ra với gậy ngang ngực) | `Skill_Blink_{View}_{0,1}` |
| 6–8 | Vòng Phù Chú | Down / Up / Side | 2 (cúi chĩa ngọc xuống đất vẽ vòng, đứng thẳng giơ gậy) | `Skill_RuneCircle_{View}_{0,1}` |
| 9–11 | Khiên Ma Lực | Down / Up / Side | 1 (dựng gậy giữa thân, tập trung) | `Skill_ManaShield_{View}_0` |
| 12–14 | Mưa Sao Băng | Down / Up / Side | 2 (giơ gậy ngả ra sau đầu, vung bổ về phía trước) | `Skill_MeteorShower_{View}_{0,1}` |

**Tên sprite khi cắt:** `Selene_` + key, ví dụ `Selene_Skill_Blink_Side_1`.

## 2. Việc cần làm phía Unity (Cursor)

1. Copy `unity_export_selene/Skills/` vào thư mục art của Selene trong `Assets/`, rồi cắt sprite theo bảng trên.
2. **Anchor:** nối các dòng của `Selene_SkillAnchors.csv` vào bảng anchor của Selene (mọi key đều mới). Khi flip, ô gậy đổi theo `idx = (idx/8)*8 + MirrorDir[idx%8]`.
3. **Animator:** gọi từ `SkillBehaviour` của bộ Selene:

| Clip | Frame × thời gian | Gắn với |
|---|---|---|
| StarOrb | F0 0.20 s (tụ phép, có thể chồng `Selene_Charge` hàng 1 lên ngọc) → F1 0.15 s (cầu lớn sinh ở ngọc) | phím 2 Cầu Tinh Tú |
| Blink | F0 0.08 s tại điểm đi → ẩn thân trong lúc `Selene_Blink` chạy → F1 0.12 s tại điểm đến | phím 3 Dịch Chuyển |
| RuneCircle | F0 0.15 s (`Selene_Rune` frame 0 hiện) → F1 0.15 s | phím 4 Vòng Phù Chú |
| ManaShield | F0 0.25 s lúc bật khiên, rồi trả về Idle/Walk (bong bóng vẫn giữ) | phím V Khiên Ma Lực |
| MeteorShower | F0 giữ 0.25 s → F1 0.15 s (vùng sao băng bắt đầu lúc F1) | phím C Mưa Sao Băng |

4. **Điểm sinh đạn của Cầu Tinh Tú:** ngọc ở F1 dùng cùng tư thế với Attack F1 (Cast), nên toạ độ trùng với bảng "Vị trí ngọc" trong `SELENE_KIT_SPEC.md` mục 1.
5. **Kiểm tra trong Sandbox:** dùng 5 skill ở 3 hướng × quay trái/phải. Gậy và nắm tay đúng chỗ, gậy cầm trên tay chỉ trong lúc dùng skill; hết skill thì Idle/Walk quay lại đeo gậy sau lưng. Level 1–4 không đổi.

## 3. Kiểm tra pixel đã chạy

`Selene_SkillSheet.png`: 0 pixel ở viền ô, 0 pixel bán trong suốt, **mọi ô đặt chân ở hàng 29** (frame cúi người hạ áo choàng 1 px, giày vẫn chạm đất). Màu lấy từ bảng màu roster Selene đã duyệt.
