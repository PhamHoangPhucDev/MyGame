# Lệnh làm việc cho Cursor: đưa art R2C vào game

**Ngày:** 2026-10-21 · **Người giao:** Claude, thay mặt chủ dự án · **Nhánh:** `reference_rebuild_r2c`

Cursor đọc file này rồi làm **lần lượt từng giai đoạn**. Hết mỗi giai đoạn thì dừng lại, báo cáo theo mẫu ở cuối file, chờ chủ dự án duyệt rồi mới sang giai đoạn sau.

Tài liệu gốc: `HANDOFF_INDEX.md` (mục lục), các ghi chú `*_NOTE.md` và `*_SPEC.md` được nhắc trong từng bước. Art đã duyệt hết, **không vẽ lại hay sửa PNG**. Thấy art sai thì ghi vào báo cáo để Claude sửa.

## Luật chung

- Mọi thử nghiệm làm trong **Sandbox** trước. **Không đụng Level 1–4** cho tới Giai đoạn 6.
- Trước khi sửa scene nào thì sao lưu scene đó vào `IncomingAssets/backup_before_<giai đoạn>/`.
- Import sprite:
  - PPU 16, Filter Point, Compression None, không mipmap.
  - Cắt ô và đặt pivot đúng như file manifest của từng thư mục.
  - **Không scale transform**, kể cả boss.
- Hàng Side của mọi sheet vẽ quay trái, dùng `flipX` để quay phải. Khi lật thì chỉ số hướng trang bị đổi theo `MirrorDir = {4,3,2,1,0,7,6,5}`.
- Mỗi giai đoạn xong thì commit riêng, ghi rõ giai đoạn trong tin nhắn commit. Chưa push nếu chủ dự án chưa bảo.

## Các quyết định đã chốt (mặc định)

Chủ dự án giao Claude chốt các điểm sau. Muốn đổi thì chủ dự án sửa thẳng vào bảng này.

| # | Nội dung | Quyết định |
|---|---|---|
| 1 | Choáng | Một trạng thái choáng dùng chung: quái đứng yên, không đánh, có `Aren_StunStars` trên đầu. Boss chỉ bị choáng 50% thời gian |
| 2 | Khiêu khích (Brom) | Quái thường trong vùng đổi mục tiêu sang Brom 3 s. Boss miễn khiêu khích |
| 3 | Làm chậm (Lyra, Selene) | Không cộng dồn: nhiều hiệu ứng cùng lúc thì lấy cái mạnh nhất |
| 4 | Né đòn / khiên hấp thụ | "Né đòn kế tiếp" mất sau 1 lần né. Khiên hấp thụ đỡ trước khi trừ máu, tự hết sau 4 s |
| 5 | Brom giảm 15% sát thương | Chỉ số riêng của Brom, tách khỏi nút Đỡ (Guard) của bản demo |
| 6 | Chỉ số quái | Lấy goblin hiện có làm gốc. Goblin Archer, Satyr, Dryad ×1; Dryad cưỡi nai ×1.3; Gobrider ×1.5; Elite ×2; Warlord trùm phụ; Cyclops boss. Số cụ thể Cursor đề xuất trong báo cáo |
| 7 | Boss Cyclops | Chuyển mặt hung dữ ngay lần đầu bị trúng đòn. Lấy đà 1.2 s rồi nện. Vòng đỏ đặt trước mặt 1.5 ô, bán kính khoảng 3.9 ô (đúng cỡ art) |
| 8 | Dryad | Giữ cả 2. Dryad đi bộ là bản yếu, xuất hiện sớm hơn Dryad cưỡi nai |
| 9 | Quái theo màn | Màn 1: Goblin, Goblin Archer · Màn 2: thêm Gobrider · Màn 3: Satyr, 2 loại Dryad · Màn 4: Elite, Warlord (trùm phụ), cuối màn là boss Cyclops |

## Giai đoạn 1: cập nhật Aren và Vex đang có trong game

- **Nguồn:**
  - `unity_export/` và `unity_export_vex/`: có `AREN_MANIFEST.csv`, `VEX_MANIFEST.csv`.
  - Ghi chú: `roster/BACK_GEAR_NOTE.md`, `vex/VEX_BACK_DAGGERS_NOTE.md`, `roster/CLASS_AUDIT_FIX_NOTE.md`.
- **Việc làm:**
  - Thay `Aren_Sword.png` (9 ô) và `Vex_Dagger.png` (10 ô).
  - Thay `Aren_Anchors.csv`, `Vex_Anchors.csv`.
  - Thêm Death hướng Up/Side.
  - Thêm VFX Vex: Bước Bóng, Vũ Điệu Lưỡi Dao, dao trúng đích.
- **Đạt khi:**
  - Idle/Walk/Hurt đeo vũ khí sau lưng; Attack rút vũ khí ra.
  - Chết theo đúng hướng đang nhìn.
  - Hai skill 2 và 4 của Vex dùng sprite mới thay hiệu ứng tạm.

## Giai đoạn 2: icon

- **Nguồn:** `icons/export/` và `icons/status/`; ghi chú `icons/ICONS_NOTE.md`, `icons/STATUS_NOTE.md`.
- **Đạt khi:**
  - Hotbar đổi icon theo class khi bấm F1–F5.
  - Icon trạng thái hiện trên đầu quái và cạnh thanh máu.

## Giai đoạn 3: quái thường

- **Nguồn:** `unity_export_monsters/`; ghi chú `MONSTERS_NOTE.md`, mục 1–6.
- **Việc làm:**
  - Goblin, Goblin Archer (mũi tên + tia ngắm đỏ), Satyr, Dryad, Dryad cưỡi nai (đạn lá).
  - Gobrider, Elite, Warlord.
  - Mỗi quái một Animator đủ Idle/Walk/Attack/Hurt/Death × 3 hướng. Sát thương gây ra đúng frame ghi trong note.
- **Đạt khi:** thả từng loại vào Sandbox đánh thử được, đúng tỉ lệ so với hero, đạn bay đúng 8 hướng.

## Giai đoạn 4: Brom, Lyra, Selene

- **Nguồn:** `unity_export_brom/`, `unity_export_lyra/`, `unity_export_selene/`, mỗi thư mục có manifest, anchor và `Skills/`.
- **Spec:** `brom/BROM_KIT_SPEC.md`, `lyra/LYRA_KIT_SPEC.md`, `selene/SELENE_KIT_SPEC.md`.
- **Việc làm:** làm lần lượt Brom → Lyra → Selene, mỗi class một commit. Áp dụng quyết định 1–5 ở bảng trên.
- **Đạt khi:**
  - Đổi class bằng F1–F5.
  - Combo R và 5 skill chạy đúng số liệu trong spec.
  - VFX gắn đúng chỗ.

## Giai đoạn 5: Aren bộ skill mới và boss Cyclops

- **Aren:**
  - Nguồn: `aren_kit/AREN_KIT_TECH_SPEC.md`, `aren_kit/sheets/`, `unity_export/Skills/`.
  - Thay bộ skill demo bằng bộ đã thiết kế.
- **Cyclops:**
  - Nguồn: `MONSTERS_NOTE.md` mục 7. Sheet 21 hàng (hiền/hung dữ), vòng cảnh báo, vết nứt.
  - Áp dụng quyết định 7.
- **Đạt khi:**
  - Boss hiền lúc đầu, bị đánh thì hung dữ.
  - Vòng đỏ lan đủ 1.2 s rồi nện đúng frame.
  - Vết nứt đất mờ dần.

## Giai đoạn 6: đưa vào Level 1–4

- Xếp quái theo quyết định 9.
- Chạy thử từng màn từ đầu đến cuối.
- Báo các chỗ khó quá hoặc dễ quá.

## Mẫu báo cáo sau mỗi giai đoạn

1. Đã làm gì (file, scene, prefab).
2. Kết quả chạy thử trong Sandbox, có ảnh chụp nếu được.
3. Chỗ chưa đạt hoặc art bị lỗi (ghi tên file và frame để Claude sửa).
4. Việc cần chủ dự án quyết thêm.
