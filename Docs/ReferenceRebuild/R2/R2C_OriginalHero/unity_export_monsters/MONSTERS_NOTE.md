# Quái R2C: Goblin, Goblin Archer, Satyr, Dryad, Dryad cưỡi nai, Gobrider, Gobrider tinh nhuệ, Gobrider thủ lĩnh (bàn giao cho Cursor)

**Ngày:** 2026-10-21 · **Người viết:** Claude (chỉ art, không sửa code hay scene) · **Trạng thái:** chờ duyệt art, **chưa import vào `Assets/`**

## 1. File

| Quái | Sheet | Ô | Chân (hàng) | Pivot Unity |
|---|---|---|---|---|
| Goblin | `Goblin/Goblin_Sheet.png` | 48×48 | 37 | (0.5, 0.2083) |
| Goblin Archer | `GoblinArcher/GoblinArcher_Sheet.png` + mũi tên `GoblinArcher/GoblinArcher_Arrow.png` (16×16, 8 ô) | 48×48 | 37 | (0.5, 0.2083); mũi tên (0.5, 0.5) |
| Satyr | `Satyr/Satyr_Sheet.png` | 48×48 | 37 | (0.5, 0.2083) |
| Dryad | `Dryad/Dryad_Sheet.png` + đạn lá `Dryad/Dryad_LeafBolt.png` (16×16, 8 ô) | 48×48 | 37 | (0.5, 0.2083); đạn (0.5, 0.5) |
| Dryad cưỡi nai | `DryadRider/DryadRider_Sheet.png` (dùng chung đạn `Dryad/Dryad_LeafBolt.png`) | 48×48 | 41 | (0.5, 0.125) |
| Gobrider | `Gobrider/Gobrider_Sheet.png` | 48×48 | 41 | (0.5, 0.125) |
| Gobrider tinh nhuệ | `GobriderElite/GobriderElite_Sheet.png` | 48×48 | 41 | (0.5, 0.125) |
| Gobrider thủ lĩnh (boss) | `GobriderWarlord/GobriderWarlord_Sheet.png` | 64×64 | 57 | (0.5, 0.0938) |

- `MONSTERS_MANIFEST.csv`: một dòng cho mỗi sheet (kích thước ô, số cột/hàng, pivot).
- `MONSTERS_FRAMES.csv`: một dòng cho mỗi frame. Cột: `sprite,file,col,row,x,y,w,h,anim,view,frame,frames,fps,mode`. Tên sprite là `{Quái}_{Anim}_{View}_{f}`, ví dụ `Gobrider_Attack_Side_1`.
- Import: PPU 16, Filter Point, Compression None, không mipmap, Sprite Mode Multiple, cắt theo lưới ô.
- **Cùng tỉ lệ PPU 16 với hero.** Hero 21 px; goblin 12 px; goblin archer, satyr, dryad 16 px; gobrider khoảng 25 px; thủ lĩnh khoảng 30 px.

## 2. Bố cục sheet (giống hero)

4 cột × 15 hàng. Hàng = anim × hướng, theo thứ tự:

| Hàng | Anim | Frame | FPS | Chế độ |
|---|---|---|---|---|
| 0–2 | Idle (Down/Up/Side) | 4 | 6 | lặp |
| 3–5 | Walk | 4 | 8 | lặp |
| 6–8 | Attack | 3 | 10 | một lần; **frame 1 là lúc ra đòn** |
| 9–11 | Hurt | 2 | 10 | một lần |
| 12–14 | Death | 4 | 8 | một lần, giữ frame cuối |

- Ô trống (ví dụ cột 3 của Attack) trong suốt.
- **Hàng Side vẽ quay trái**, runtime dùng `flipX` để quay phải, giống hero.
- Chọn hướng giống hero: 3 view Down / Up / Side theo hướng di chuyển hoặc hướng tới mục tiêu.

## 3. Khác hero: vũ khí vẽ sẵn trong frame

- Quái cầm vũ khí trên tay (dao rỉ của Goblin, chuỳ xương của Elite và thủ lĩnh), **đã vẽ luôn vào từng frame**.
- Không có anchor CSV, không có lớp trang bị riêng. Một `SpriteRenderer` + `Animator` là đủ.
- Gobrider thường không có vũ khí: đòn đánh là heo húc tới (lùi 1 px rồi lao 2 px).
- Goblin Archer: cung vẽ sẵn trong frame, **mũi tên bay là sprite riêng** (mục 3a).
- Satyr không cầm vũ khí: đòn đánh là húc sừng (thụp xuống lùi 1 px, lao tới 2 px).
- Dryad: quả cầu phép vẽ sẵn trong frame, **đạn lá bay là sprite riêng** (mục 3b).

## 3a. Mũi tên của Goblin Archer

- `GoblinArcher/GoblinArcher_Arrow.png`: 8 ô 16×16, thứ tự R, UR, U, UL, L, DL, D, DR. Đầu mũi tên chỉ theo hướng của ô.
- Pivot giữa ô. **Không xoay ở runtime**: chọn ô theo hướng bay gần nhất (giống trang bị hero).
- Sinh mũi tên ở **Attack frame 1** (lúc buông dây). Điểm sinh, local tính từ pivot chân (đơn vị Unity, PPU 16):

| View | Điểm sinh (x, y) | Ghi chú |
|---|---|---|
| Down | (0, 0.125) | giữa cây cung ngang hông |
| Up | (0, 0.625) | trước mặt, phía trên đầu |
| Side (sheet quay trái) | (−0.625, 0.4375) | đầu mũi tên trên dây cung; khi `flipX` quay phải thì đổi thành (+0.625, 0.4375) |

- Video có một tia đỏ mảnh ngắm tới người chơi trước khi bắn. Đây là telegraph do code vẽ (LineRenderer 1 px màu `#D83A3A`), art không xuất file riêng.

## 3b. Đạn lá của Dryad

- `Dryad/Dryad_LeafBolt.png`: 8 ô 16×16, thứ tự R, UR, U, UL, L, DL, D, DR. Mũi lá chỉ theo hướng bay, vệt sáng ở đuôi.
- Pivot giữa ô, không xoay ở runtime: chọn ô theo hướng bay gần nhất, giống mũi tên.
- Sinh đạn ở **Attack frame 1** (quả cầu to nhất). Điểm sinh là tâm quả cầu, local tính từ pivot chân:

| View | Điểm sinh (x, y) | Ghi chú |
|---|---|---|
| Down | (0, 0.25) | trước ngực |
| Up | (0, 1.125) | giơ cao trên mũ lá |
| Side (sheet quay trái) | (−0.4375, 0.375) | trước tay; khi `flipX` quay phải thì đổi thành (+0.4375, 0.375) |

**Dryad cưỡi nai** bắn cùng loại đạn lá, cũng ở Attack frame 1. Điểm sinh tính từ pivot chân của sheet DryadRider:

| View | Điểm sinh (x, y) |
|---|---|
| Down | (0, 0.5625) |
| Up | (0, 1.5) |
| Side (sheet quay trái) | (−0.625, 0.6875); khi `flipX` thì (+0.625, 0.6875) |

## 4. Nội dung từng anim

- **Idle:** frame 1–2 hạ 1 px (goblin thở; người cưỡi nhún trên lưng heo).
- **Walk:**
  - Goblin bước chân.
  - Goblin Archer, Satyr, Dryad: frame 0 và 2 hạ 1 px, đổi chân bước.
  - Heo nhấc lần lượt chân sau/trái (frame 0) rồi chân trước/phải (frame 2); người cưỡi nhún ở frame 1 và 3.
  - Dryad cưỡi nai: nhấc xen kẽ từng cặp chân (frame 0 và 2), thân trên nhún ở frame 1 và 3.
- **Attack:**
  - Goblin: lấy đà, đâm (lao 1 px), thu dao.
  - Goblin Archer: kéo dây ngắm, buông (giật lùi 1 px, sinh mũi tên), hạ cung.
  - Satyr: thụp xuống lùi 1 px, húc sừng lao tới 2 px, về chỗ.
  - Dryad và Dryad cưỡi nai: gom quả cầu lá, quả cầu to nhất (sinh đạn lá), hạ tay. Đứng yên tại chỗ.
  - Gobrider: heo lùi 1 px, húc tới 2 px, về chỗ cũ.
  - Elite và thủ lĩnh: đồng thời giơ chuỳ, nện xuống phía trước, rồi hạ chuỳ.
- **Hurt:** bật lùi 1 px. Frame 0 loé trắng 60%, frame 1 ửng đỏ nhẹ. Nếu code đã có hiệu ứng flash bằng shader thì có thể chỉ dùng frame 1.
- **Death:**
  - Frame 0: loé trắng. Frame 1: khuỵu xuống 2 px.
  - Frame 2: quái đi bộ (Goblin, Goblin Archer, Satyr, Dryad) ngã nghiêng 90°; heo đổ bụng xuống đất, người cưỡi gục trên lưng; Dryad cưỡi nai khuỵu 4 chân xuống đất.
  - Frame 3: như frame 2 nhưng mờ 50% (giữ frame này rồi huỷ object hoặc cho mờ dần thêm bằng code).

## 5. Gợi ý gameplay (art không quyết, chỉ để Cursor tham khảo)

| Quái | Vai trò gợi ý | Gợi ý va chạm (tính từ chân) |
|---|---|---|
| Goblin | quái thường, đánh cận chiến tầm ngắn | hộp ≈ 10×6 px quanh chân |
| Goblin Archer | bắn xa, đứng yên ngắm (có tia telegraph), giữ khoảng cách | ≈ 10×6 px |
| Satyr | quái rừng cận chiến, húc lao ngắn | ≈ 10×6 px |
| Dryad | quái rừng bắn phép tầm trung (đạn lá chậm, bay thẳng) | ≈ 10×6 px |
| Dryad cưỡi nai | bản chính trong video: bắn đạn lá, di chuyển nhanh hơn, hay đi theo nhóm | ≈ 16×8 px |
| Gobrider | quái lao húc (charge) | ≈ 16×8 px |
| Gobrider tinh nhuệ | phiên bản trâu hơn, có chuỳ, heo đeo mặt nạ sắt | ≈ 16×8 px |
| Gobrider thủ lĩnh | boss | ≈ 20×10 px |

## 6. Nguồn và cách xuất lại

- Code sinh art: `tools/MonsterGen.cs` (Goblin), `tools/ArcherGen.cs` (Goblin Archer + mũi tên), `tools/CreatureGen.cs` (Satyr, Dryad, Dryad cưỡi nai + đạn lá), `tools/RiderGen.cs` (người cưỡi và heo rừng đen), `tools/MonsterAnim.cs` (anim và xuất file).
- Chạy `tools/RenderMonsterExport.ps1` để xuất lại, rồi `tools/RenderMonsterReview.ps1` để tạo bảng duyệt ở `monsters/review/`.
- Mẫu đã duyệt: `monsters/GOBLIN_CANDIDATE_v3.png`, `monsters/ARCHER_CANDIDATE_v2.png`, `monsters/FOREST_CANDIDATE_v1.png`, `monsters/DRYAD_RIDER_CANDIDATE_v1.png`, `monsters/RIDERS_CANDIDATE_v7.png`.
- Pixel vẽ mới hoàn toàn, chỉ bám dáng và tỉ lệ theo video tham khảo, không đồ lại.
