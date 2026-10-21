# R2C — Mục lục bàn giao art cho Cursor

**Ngày:** 2026-10-21 · **Người viết:** Claude (chỉ art + tài liệu; không sửa code, scene hay prefab)
**Nhánh:** `reference_rebuild_r2c` · **Gốc:** `Docs/ReferenceRebuild/R2/R2C_OriginalHero/`

Đây là điểm bắt đầu duy nhất. Mỗi mục trỏ tới spec hoặc ghi chú chi tiết, và tài liệu đó là nguồn chính thức. Mọi PNG đều nằm trong Docs; **chưa có gì được import vào `Assets/`**, trừ những phần Aren và Vex đã import từ trước.

## 1. Quy ước chung (áp dụng cho mọi file)

| Mục | Quy ước |
|---|---|
| Thân nhân vật | Ô 32×32, thân 14×21 ở cột 9–22 và hàng 9–29, **chân ở hàng 29**. Pivot (16, 30) = (0.5, 0.0625). PPU 16, Filter Point, Compression None, không mipmap |
| Sheet thân | Hàng Idle D/U/S, Walk D/U/S, Attack D/U/S. **Hàng Side vẽ quay trái**, runtime `flipX` để quay phải |
| Trang bị | Sprite riêng, pivot = tay cầm, **vẽ sẵn 8 hướng, không xoay ở runtime** (R, UR, U, UL, L, DL, D, DR). Khi lật Side: `MirrorDir = {4,3,2,1,0,7,6,5}` |
| Anchor CSV | `key,main_x,main_y,main_dir,main_order,hand_x,hand_y,off_x,off_y,off_view,off_order,slash,slash_order[,off_dir]` theo toạ độ sheet. Local = ((x−16)/16, (30−y)/16), âm x khi flip. −99 = không vẽ. order > 0 trước thân, < 0 sau thân |
| VFX combo | Chỉ số ô = step×6 + view×2 + frame. Key theo từng đòn: `Attack_{View}_{1,2}@{step}` |
| Frame skill | File riêng `*_SkillSheet.png` + `*_SkillAnchors.csv` trong thư mục `Skills/` của mỗi class. Key `Skill_{Tên}_{View}_{f}`, tên sprite = `{Class}_` + key |
| **Vũ khí khi nghỉ** | **Idle / Walk / Hurt: mọi class đeo vũ khí sau lưng, tay để trống.** Chỉ Attack và skill mới cầm vũ khí. Hướng Down dùng ô "peek" (chuôi, đầu cung, đầu búa, ngọc) nhô qua vai |
| Pixel | Không khử răng cưa, chừa 1 px viền trong suốt mỗi ô (ngoại lệ có ghi trong từng note), chỉ dùng màu trong bảng màu đã duyệt |

Chi tiết hệ trang bị: `attack/EQUIPMENT_SYSTEM.md`. Thiết kế bộ skill 5 class: `classkit/CLASS_KIT_DESIGN.md`.

## 2. Theo từng class

| Class | Thư mục xuất | Spec bộ skill + VFX | Frame thân skill | Ghi chú thêm | Trạng thái Unity |
|---|---|---|---|---|---|
| **Aren** (hiệp sĩ) | `unity_export/` (sheet, Extra, Gear, **Skills/**) + VFX skill `aren_kit/sheets/` | `aren_kit/AREN_KIT_TECH_SPEC.md` | `aren_kit/AREN_SKILL_BODY_NOTE.md` | `roster/BACK_GEAR_NOTE.md` (Idle/Walk đeo sau lưng, `Aren_Sword.png` thêm ô 8) | Đã import sheet/gear cũ. **Cần cập nhật:** anchor Idle/Walk/Hurt, `Aren_Sword` 9 ô, VFX skill, Skills/ |
| **Vex** (sát thủ) | `unity_export_vex/` (+ **Skills/**) | `classkit/CLASS_KIT_DESIGN.md` mục 6, `vex/R2C_VEX_REPORT.md` (bộ kit đã có code) | `vex/VEX_SKILL_BODY_NOTE.md` | `vex/VEX_BACK_DAGGERS_NOTE.md` (dao chữ X sau lưng, `Vex_Dagger` 10 ô) | Đã import. **Cần cập nhật:** `Vex_Dagger` 10 ô, anchor Idle/Walk/Hurt, Skills/ |
| **Brom** (người lùn tank) | `unity_export_brom/` (+ **Skills/**) | `brom/BROM_KIT_SPEC.md` | `brom/BROM_SKILL_BODY_NOTE.md` | `roster/BACK_GEAR_NOTE.md` | **Chưa import** |
| **Lyra** (elf cung thủ) | `unity_export_lyra/` (+ **Skills/**) | `lyra/LYRA_KIT_SPEC.md` | `lyra/LYRA_SKILL_BODY_NOTE.md` | cung đeo sau lưng đã có sẵn trong bản xuất | **Chưa import** |
| **Selene** (phù thủy) | `unity_export_selene/` (+ **Skills/**) | `selene/SELENE_KIT_SPEC.md` | `selene/SELENE_SKILL_BODY_NOTE.md` | `roster/BACK_GEAR_NOTE.md` | **Chưa import** |

## 3. Dùng chung

| Nội dung | Thư mục | Ghi chú |
|---|---|---|
| Icon hotbar: 25 skill + 5 nút R (32×32, khung theo class) | `icons/export/` (PNG, atlas 192×160, manifest) | `icons/ICONS_NOTE.md`: hiện ở 64 px; nút R 96 px |
| Icon trạng thái: 11 buff/debuff (24×24, viền đỏ là debuff, viền xanh là buff) | `icons/status/` (PNG, atlas 264×24, manifest) | `icons/STATUS_NOTE.md`: hiện ở 48 px trên đầu quái và cạnh thanh máu |
| Quái: Goblin, Goblin Archer (+ mũi tên 8 hướng), Satyr, Dryad (+ đạn lá 8 hướng), Gobrider, Gobrider tinh nhuệ, Gobrider thủ lĩnh (Idle/Walk/Attack/Hurt/Death × 3 hướng, vũ khí vẽ sẵn) | `unity_export_monsters/` (sheet, `MONSTERS_MANIFEST.csv`, `MONSTERS_FRAMES.csv`) | `unity_export_monsters/MONSTERS_NOTE.md`: ô 48 (boss 64), chân theo từng quái; goblin + Goblin Archer đã duyệt, **Satyr + Dryad chờ duyệt** |

## 4. Thứ tự import đề xuất

Mỗi bước kết thúc bằng một lần thử trong Sandbox, không đụng Level 1–4.

1. **Cập nhật Aren + Vex đang có:**
   - Cắt lại `Aren_Sword` (9 ô) và `Vex_Dagger` (10 ô), thay anchor Idle/Walk/Hurt.
   - Kiểm tra Idle/Walk đeo vũ khí sau lưng và Attack vẫn rút vũ khí.
2. **Icon:** bảng tra icon hotbar và icon trạng thái, đổi icon theo class khi bấm F1–F5.
3. **Aren bộ skill mới:** VFX `aren_kit/sheets/` + frame thân `unity_export/Skills/`, theo `AREN_KIT_TECH_SPEC.md`.
4. **Vex frame thân skill** (code kit đã có): nối `Skills/` vào các `SkillBehaviour` sẵn có.
5. **Brom**, rồi **Lyra** (cần hệ đạn xuyên và 8 hướng), rồi **Selene** (đạn nổ vùng, khiên hấp thụ). Mỗi class: sheet, gear, anchor, VFX, Skills/, kit.

## 5. Cần anh/chị quyết trước khi Cursor code

| Quyết định | Ảnh hưởng | Nêu ở |
|---|---|---|
| Choáng (`StatusStun` chung) | AI quái phải dừng di chuyển và đánh | Aren spec, Brom spec |
| Khiêu khích | AI quái có mục tiêu ép buộc kèm thời hạn | `BROM_KIT_SPEC.md` mục 4 |
| Làm chậm quái | `MoveSpeedMultiplier` trên quái | Lyra spec, Selene spec |
| Né đòn kế tiếp, khiên hấp thụ | `Health` (`dodgeNextHit`, `shieldPoints`) | Lyra spec, Selene spec |
| Giảm sát thương gốc 15% của Brom | gộp chung một chỗ trong `Health`, tách khỏi Guard demo | `BROM_KIT_SPEC.md` mục 4 |
| Tư thế đeo vũ khí sau lưng | **chỉ Sandbox hay cả campaign Level 1–4** (Aren campaign dùng chung `Aren_Anchors.csv`) | `roster/BACK_GEAR_NOTE.md` mục 3 |
| Chuyển Level 1–4 sang class kit | thay bộ skill demo của `PlayerCombat` | `CLASS_KIT_DESIGN.md` mục 9 |

## 6. Sinh lại art (nếu cần)

- Mọi PNG và CSV đều sinh từ script trong `tools/`, chạy bằng PowerShell (`Add-Type` C#, System.Drawing). Mỗi bảng duyệt đều có script `Render*.ps1` tương ứng.
- **Thứ tự bắt buộc khi sinh lại một class:**
  1. Chạy script xuất chính: `RenderBrom.ps1`, `RenderLyra.ps1`, `RenderSelene.ps1`, `RenderVexEquip.ps1`, hoặc export của Aren.
  2. Chạy script frame skill: `Render*Skills.ps1`.
  3. Với Aren, Brom, Selene, chạy **`RenderBackGearExport.ps1` sau cùng**. Nếu không, Idle/Walk sẽ quay về tư thế cầm vũ khí trên tay.
- Icon: `RenderIcons.ps1` (hotbar), `RenderStatusIcons.ps1` (trạng thái).

## 7. Lịch sử commit art (nhánh `reference_rebuild_r2c`)

| Commit | Nội dung |
|---|---|
| `8f22308` | Bộ kit Aren/Brom/Lyra/Selene, dao Vex sau lưng, 25 icon skill |
| `c2e7703` | 5 icon nút R |
| `f7b27b1` | Frame thân skill Aren |
| `d885cef` | 11 icon trạng thái |
| `98b6633` | Frame thân skill Brom |
| `9781213` | Frame thân skill Lyra |
| `a95abe3` | Vũ khí đeo sau lưng (Aren, Brom, Selene) |
| `6495b75` | Frame thân skill Selene |
| `c9c69c7` | Frame thân skill Vex |
