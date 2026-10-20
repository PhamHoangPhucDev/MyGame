# Aren — frame thân riêng cho 5 skill: ghi chú cho Cursor

**Ngày:** 2026-10-20 · **Người viết:** Claude (chỉ art + ghi chú, không sửa code, scene hay prefab)
**Đã duyệt tư thế:** `aren_kit/AREN_SKILL_BODY_CANDIDATE_v2.png` · **Bảng duyệt bản xuất:** `aren_kit/Aren_skill_body_review.png`
**Nguồn:** `tools/ArenSkillGen.cs`, chạy `tools/RenderArenSkills.ps1` (xuất PNG, bảng duyệt và kiểm tra pixel).
**Liên quan:** `aren_kit/AREN_KIT_TECH_SPEC.md`. Spec đó ghi "không thêm frame thân mới"; tài liệu này bổ sung các frame đó, còn số liệu skill giữ nguyên.

## 1. File giao (`unity_export/Skills/`)

Không đụng `Aren_Sheet.png` và `Aren_Anchors.csv` đã import.

| File | Nội dung |
|---|---|
| `Aren_SkillSheet.png` | 128×416, ô 32×32, pivot (16, 30) = (0.5, 0.0625), PPU 16, Point, không nén. **13 hàng** (bảng dưới). Side **quay trái** như sheet Aren |
| `Aren_SkillAnchors.csv` | 13 cột giống hệt `Aren_Anchors.csv`: `key,main_x,main_y,main_dir,main_order,hand_x,hand_y,off_x,off_y,off_view,off_order,slash,slash_order`. Dùng lại `Aren_Sword`, `Aren_Hand`, `Aren_Shield`; slash = −1 vì VFX skill nằm trong `aren_kit/sheets/` |
| `AREN_SKILL_LAYOUT.csv` | Hàng → skill, view, số frame, danh sách key |

| Hàng | Skill | View | Frame | Key |
|---|---|---|---|---|
| 0–2 | Khiên Đập | Down / Up / Side | 2 (thủ thế, đập) | `Skill_ShieldBash_{View}_{0,1}` |
| 3–5 | Xung Phong | Down / Up / Side | 2 (lặp chạy) | `Skill_Charge_{View}_{0,1}` |
| 6–8 | Thành Lũy | Down / Up / Side | 1 (giữ thế thủ) | `Skill_Bulwark_{View}_0` |
| 9–11 | Chém Bình Minh | Down / Up / Side | 3 (giơ kiếm, chém, vung theo) | `Skill_Dawnbreaker_{View}_{0,1,2}` |
| 12 | Lốc Kiếm | dùng chung mọi hướng | 4 (xoay: Down → Side → Up → Side) | `Skill_Whirl_{0..3}` |

**Tên sprite khi cắt:** `Aren_` + key, ví dụ `Aren_Skill_Bulwark_Side_0`, cùng kiểu với `Aren_Idle_Down_0`, để `EquipmentVisual` tìm được dòng anchor theo key.

## 2. Việc cần làm phía Unity (Cursor)

1. Copy `unity_export/Skills/` vào `Assets/Art/Rebuild/R2C/Aren/Skills/`, rồi cắt sprite theo bảng trên.
2. **Anchor:** nối các dòng của `Aren_SkillAnchors.csv` vào bảng anchor của Aren (hoặc cho `EquipmentSet` đọc thêm một CSV). Mọi key đều mới, không trùng key cũ.
3. **Animator:** 5 clip mới, gọi từ `SkillBehaviour` thay cho clip Attack/Walk đang dùng tạm:

| Clip | Frame × thời gian | Gắn với |
|---|---|---|
| ShieldBash | F0 0.08 s (lúc bắt đầu lao) → F1 giữ đến hết lao + nón vàng (≈0.18 s) | `ShieldBash` |
| Charge | F0/F1 × 0.075 s, lặp suốt 0.30 s lao | `Charge` (bóng mờ runtime chụp đúng frame này) |
| Bulwark | F0 giữ suốt 1.4 s | `Bulwark` |
| Dawnbreaker | F0 0.15 s → F1 0.08 s (impact, sinh `Aren_Dawn`) → F2 0.15 s | `Dawnbreaker` |
| Whirl | F0–F3 × 0.05 s, lặp 3 vòng (khớp 3 lần trúng) | `WhirlSlash` |

4. **Flip:**
   - 4 skill có view (ShieldBash, Charge, Bulwark, Dawnbreaker) flip y như Idle/Walk/Attack: `body.flipX` khi quay phải.
   - **Whirl** là vòng xoay, không phụ thuộc hướng. Hai frame Side (F1, F3) đều vẽ quay trái theo quy ước sheet, nên flip hay không đều khớp trang bị. Đề xuất **không flip** để chiều xoay luôn giống nhau.
5. **Thứ tự vẽ:** theo `main_order` / `off_order` như bộ Aren cũ. Ví dụ Bulwark: khiên +1 trước thân, kiếm −2 sau thân.
6. **Kiểm tra trong Sandbox:** dùng 5 skill ở 3 hướng × quay trái/phải. Kiếm, găng và khiên phải nằm đúng tay, không giật khi đổi frame; chân vẫn đặt trên hàng 29. Level 1–4 không đổi.

## 3. Kiểm tra pixel đã chạy

`Aren_SkillSheet.png`: 0 pixel ở viền ô, 0 pixel bán trong suốt, và **mọi ô đều đặt chân ở hàng 29**. Lunge và Thành Lũy hạ người 1 px nhưng chân vẫn chạm đất. Màu lấy từ bảng màu roster Aren đã duyệt.
