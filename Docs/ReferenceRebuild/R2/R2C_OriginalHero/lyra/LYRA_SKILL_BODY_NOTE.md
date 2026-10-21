# Lyra — frame thân riêng cho skill: ghi chú cho Cursor

**Ngày:** 2026-10-21 · **Người viết:** Claude (chỉ art + ghi chú, không sửa code, scene hay prefab)
**Đã duyệt tư thế:** `lyra/LYRA_SKILL_BODY_CANDIDATE_v1.png` · **Bảng duyệt bản xuất:** `lyra/Lyra_skill_body_review.png`
**Nguồn:** `tools/LyraSkillGen.cs`, chạy `tools/RenderLyraSkills.ps1` (xuất PNG, bảng duyệt và kiểm tra pixel).
**Liên quan:** `lyra/LYRA_KIT_SPEC.md`. Số liệu skill giữ nguyên.

## 1. File giao (`unity_export_lyra/Skills/`)

Không đụng `Lyra_Sheet.png` và `Lyra_Anchors.csv`.

| File | Nội dung |
|---|---|
| `Lyra_SkillSheet.png` | 128×384, ô 32×32, pivot (16, 30) = (0.5, 0.0625), PPU 16, Point, không nén. **12 hàng** (bảng dưới). Side **quay trái**. **Tay giương cung vẽ sẵn trong thân**, như clip Attack của Lyra |
| `Lyra_SkillAnchors.csv` | 14 cột giống `Lyra_Anchors.csv`. `main_dir` = **chỉ số ô trong `Lyra_Bow.png`** (state×8 + dir; 24 = đầu cung nhô sau vai). `hand` = nắm tay cầm cung. `off` (off_view = `Hand`) = nắm tay kéo dây. Ở các frame cung đeo sau lưng, hand và off = −99 |
| `LYRA_SKILL_LAYOUT.csv` | Hàng → skill, view, số frame, danh sách key |

| Hàng | Skill | View | Frame | Key |
|---|---|---|---|---|
| 0–2 | Tên Quạt | Down / Up / Side | 2 (kéo căng dây, buông và vung cung quét quạt) | `Skill_FanShot_{View}_{0,1}` |
| 3–5 | Nhảy Lùi | Down / Up / Side | 2 (khuỵu gối, **bật lên trên không**) | `Skill_LeapBack_{View}_{0,1}` |
| 6–8 | Mưa Tên | Down / Up / Side | 2 (giương cung lên trời kéo dây, buông) | `Skill_ArrowRain_{View}_{0,1}` |
| 9–11 | Tên Xuyên Rừng | Down / Up / Side | 2 (quỳ thấp kéo dây sâu, buông và giật lùi) | `Skill_ForestPierce_{View}_{0,1}` |
| — | Bước Gió | dùng clip Walk | — | không có frame riêng |

**Tên sprite khi cắt:** `Lyra_` + key, ví dụ `Lyra_Skill_ArrowRain_Side_0`.

## 2. Việc cần làm phía Unity (Cursor)

1. Copy `unity_export_lyra/Skills/` vào thư mục art của Lyra trong `Assets/`, rồi cắt sprite theo bảng trên.
2. **Anchor:** nối các dòng của `Lyra_SkillAnchors.csv` vào bảng anchor của Lyra (mọi key đều mới). Khi flip, ô cung đổi theo `idx = state*8 + MirrorDir[idx%8]`; ô 24 giữ nguyên, như bộ Lyra cũ.
3. **Animator:** gọi từ `SkillBehaviour` của bộ Lyra:

| Clip | Frame × thời gian | Gắn với |
|---|---|---|
| FanShot | F0 0.12 s → F1 0.14 s (5 mũi tên sinh ở đầu F1, cùng `Lyra_FanBurst`) | phím 2 Tên Quạt |
| LeapBack | F0 0.06 s → F1 giữ suốt 0.22 s lùi | phím 3 Nhảy Lùi |
| ArrowRain | F0 0.15 s → F1 0.15 s (vùng mưa tên bắt đầu lúc F1) | phím 4 Mưa Tên |
| ForestPierce | F0 giữ 0.25 s → F1 0.15 s (tia sinh ở đầu F1) | phím C Tên Xuyên Rừng |

4. **Nhảy Lùi F1 đang ở trên không:** chân cao hơn hàng 29 khoảng 4 px. Bóng (shadow) vẫn để trên mặt đất, không đi theo sprite.
5. **Mưa Tên:** cung chĩa lên trời ở cả 3 hướng. Không cần điểm sinh đạn, vì mũi tên rơi xuống từ trên vùng mục tiêu.
6. **Điểm sinh đạn** (Tên Quạt, Tên Xuyên Rừng) giống clip Attack: lấy mũi cung theo grip và hướng trong CSV, công thức ở spec Lyra mục 1. Riêng Tên Xuyên Rừng, grip hạ 1 px vì đang quỳ.
7. **Kiểm tra trong Sandbox:** dùng 4 skill ở 3 hướng × quay trái/phải. Cung và hai nắm tay đúng chỗ; frame cung đeo sau lưng không có tay cầm. Level 1–4 không đổi.

## 3. Kiểm tra pixel đã chạy

`Lyra_SkillSheet.png`: 0 pixel ở viền ô, 0 pixel bán trong suốt. Mọi ô đặt chân ở hàng 29, **trừ 3 ô Nhảy Lùi F1** (Down/Up/Side, chân ở hàng 25, đang trên không, cố ý). Màu lấy từ bảng màu roster Lyra đã duyệt.
