# Icon trạng thái (buff / debuff): ghi chú cho Cursor

**Ngày:** 2026-10-21 · **Người viết:** Claude (chỉ art + ghi chú, không sửa code, scene hay prefab)
**Đã duyệt:** `icons/STATUS_CANDIDATE_v6.png` · **Nguồn:** `tools/StatusIconGen.cs`, chạy `tools/RenderStatusIcons.ps1` (xuất PNG và kiểm tra pixel).

## 1. File giao (`icons/status/`)

| File | Nội dung |
|---|---|
| `Status_{Id}.png` × 11 | Mỗi icon **24×24**, có 1 px viền trong suốt, ô vuông bo góc. **Viền đỏ = debuff** (gắn lên quái), **viền xanh dương = buff** (gắn lên người chơi) |
| `Status_Atlas.png` | 264×24, 11 ô theo thứ tự bảng dưới (cột 0–10) |
| `STATUS_MANIFEST.csv` | `file,id,name_vi,type,source,atlas_col,size` |

Import: Sprite (2D and UI), **Filter Point**, **Compression None**, không mipmap, pivot giữa.

| Cột | Id | Tên | Loại | Do skill nào gây ra | Hình |
|---|---|---|---|---|---|
| 0 | Stun | Choáng | debuff | Aren Khiên Đập, Brom Nện Đất | vòng xoáy, sao bay quanh |
| 1 | Bleed | Chảy máu | debuff | Vex đòn kết combo, Phi Dao | vết chém, giọt máu nhỏ xuống |
| 2 | Slow | Làm chậm | debuff | Lyra dây leo, Selene Vòng Phù Chú | ốc sên |
| 3 | Taunt | Khiêu khích | debuff | Brom Gầm Thách | tâm ngắm đỏ, dấu "!" |
| 4 | Absorb | Khiên hấp thụ | buff | Selene Khiên Ma Lực | bong bóng tím bọc khiên, đòn đỏ tan ở mặt bong bóng |
| 5 | DamageReduction | Giảm sát thương | buff | Aren Thành Lũy, Brom Tường Sắt / Gầm Thách | mũi tên đỏ vỡ vụn trên khiên, dấu "−" |
| 6 | Haste | Tăng tốc | buff | Lyra Bước Gió | giày chạy, vệt tốc độ, mũi tên xanh lá chỉ lên |
| 7 | Stealth | Tàng hình | buff | Vex Bom Khói | con mắt bị gạch chéo |
| 8 | DodgeReady | Sẵn sàng né | buff | Lyra Bước Gió | đòn chém trúng bóng mờ, người đã né sang bên, chữ "MISS" |
| 9 | Empowered | Đòn kế ×2 | buff | Vex Bước Bóng | chữ "×2" trên nền tia lực |
| 10 | Reflect | Phản đòn | buff | Aren Thành Lũy | mũi tên đỏ lao vào khiên, mũi tên vàng bật ngược lại |

**Giảm sát thương gốc 15% của Brom luôn bật**, nên đề xuất **không hiện** icon cho phần này. Chỉ hiện khi có skill (Tường Sắt, Gầm Thách) để icon không thường trực.

## 2. Việc cần làm phía Unity (Cursor)

1. Copy `icons/status/` vào `Assets/Art/Rebuild/R2C/Icons/Status/` rồi import như mục 1.
2. **Dữ liệu:** một bảng tra `StatusId → Sprite` (ScriptableObject hoặc mảng theo đúng thứ tự cột). Các trạng thái đã có component (`StatusStun`, `StatusBleed`) và những trạng thái sẽ có (Slow, Taunt, Absorb, Haste, Stealth, DodgeReady, Empowered, Reflect, DamageReduction) đều tra icon qua bảng này.
3. **Trên đầu quái (debuff):**
   - Thêm hàng icon vào `EnemyWorldUI`, ngay trên thanh máu (hiện `worldOffset` = (0, 0.95)).
   - Icon 24 px đặt ở **48 px trên canvas 1080p** (gấp đúng 2 lần, không nhoè), xếp ngang, cách nhau 2 px, tối đa 4 icon.
   - Nếu dùng world-space sprite: PPU 16 thì 24 px = 1.5 ô; nên đặt **PPU 24** để icon cao đúng 1 ô. Pixel sẽ không khớp lưới 16, nên khuyến nghị dùng canvas.
4. **Người chơi (buff):** hàng icon cạnh thanh máu của hero (`SkillHotbarUI`, khu `HeroHpFrame`), cùng cỡ 48 px.
   - Có thể phủ một lớp đếm ngược (radial fill tối, giống hồi chiêu) để thấy thời gian còn lại.
5. **Thứ tự hiện:** debuff kiểm soát trước (Choáng, Khiêu khích, Làm chậm), rồi Chảy máu. Buff theo thứ tự được nhận. Trạng thái hết hạn thì tắt icon ngay.
6. **Kiểm tra trong Sandbox:**
   - Choáng quái bằng Aren Khiên Đập: thấy icon Choáng trên đầu, đúng 0.6 s rồi mất.
   - Vex đánh chảy máu: icon Chảy máu suốt 3 s.
   - Selene bật Khiên Ma Lực: icon Khiên hấp thụ cạnh thanh máu hero, mất khi vỡ khiên.

## 3. Kiểm tra pixel đã chạy

11/11 icon: viền ngoài 1 px trong suốt, 0 pixel bán trong suốt. Màu lấy từ bảng màu nhân vật và VFX của các class.
