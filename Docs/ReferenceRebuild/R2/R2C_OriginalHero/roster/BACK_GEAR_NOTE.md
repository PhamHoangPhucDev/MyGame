# Vũ khí đeo sau lưng khi Idle / Walk / Hurt (Aren, Brom, Selene): ghi chú cho Cursor

**Ngày:** 2026-10-21 · **Người viết:** Claude (chỉ art + ghi chú, không sửa code, scene hay prefab)
**Đã duyệt:** `roster/BACK_GEAR_CANDIDATE_v1.png` · **Nguồn:** `tools/BackGearGen.cs`, chạy `tools/RenderBackGearExport.ps1`.
**Quy tắc chung cho mọi class:** Idle và Walk/Run (và Hurt, vì Hurt dùng lại tư thế nghỉ) đeo vũ khí sau lưng, hai tay để trống. Chỉ Attack và skill mới cầm vũ khí. Lyra (`lyra/LYRA_KIT_SPEC.md`) và Vex (`vex/VEX_BACK_DAGGERS_NOTE.md`) đã theo quy tắc này.

## 1. Thay đổi về hình

| Class | Up (sau lưng) | Side | Down (trước mặt) |
|---|---|---|---|
| Aren | khiên huy hiệu giữa lưng (mặt trước khiên), kiếm chéo sau khiên | kiếm và khiên sau mép lưng, chuôi kiếm nhô qua vai | **chuôi kiếm nhô qua vai phải** (ô mới) |
| Brom | khiên tròn giữa lưng, đầu búa nhô qua vai phải | khiên sau mép lưng, đầu búa nhô qua vai sau | đầu búa nhô qua vai phải |
| Selene | gậy chéo giữa lưng, ngọc nhô qua vai phải | gậy sau lưng, ngọc nhô qua vai sau | ngọc nhô cạnh vành mũ, đuôi gậy thò dưới vạt áo |

Attack (kể cả frame thu về F2), Death và frame skill **không đổi**: vẫn cầm vũ khí trên tay.

## 2. File thay đổi (đã đối chiếu checksum và nội dung)

| File | Thay đổi |
|---|---|
| `unity_export/Aren_Anchors.csv` | Chỉ 30 dòng `Idle_*` / `Walk_*` / `Hurt_*`. Số dòng giữ nguyên (43) |
| `unity_export/Gear/Aren_Sword.png` | **384×48 → 432×48 (9 ô)**. Ô 0–7 không đổi pixel nào. **Ô 8 = chuôi kiếm nhô qua vai** (pivot giữa ô, như các ô khác) |
| `unity_export_brom/Brom_Anchors.csv` | Chỉ 30 dòng Idle / Walk / Hurt. Dùng lại ô búa và ô khiên sẵn có |
| `unity_export_selene/Selene_Anchors.csv` | Chỉ 30 dòng Idle / Walk / Hurt. Dùng lại ô gậy sẵn có (gậy dài) |

### Giá trị mới (toạ độ sheet; Side đã lật; Walk F0/F2 và `Hurt_*_1` là y +1)

| Class | View | main (x, y, dir, order) | off (x, y, view, order) |
|---|---|---|---|
| Aren | Down | 21, 16, **8**, −2 | — |
| Aren | Up | 12, 16, 7, +1 | 16, 21, **Down**, +2 (mặt trước khiên quay ra sau) |
| Aren | Side | 25, 15, 5, −2 | 22, 21, Side, −1 |
| Brom | Down | 15, 22, 1, −2 | — |
| Brom | Up | 14, 22, 1, +1 | 16, 22, **Down**, +2 |
| Brom | Side | 14, 24, 1, −2 | 22, 22, Side, −1 |
| Selene | Down | 11, 27, 1, −2 | — |
| Selene | Up | 11, 26, 1, +2 | — |
| Selene | Side | 14, 27, 1, −2 | — |

`hand_x` = −99 ở mọi dòng này, vì không có tay cầm vũ khí.

## 3. Việc cần làm phía Unity (Cursor)

1. Copy 4 file trên sang thư mục art tương ứng trong `Assets/` (Aren: `Assets/Art/Rebuild/R2C/Aren/`).
2. **Aren:** cắt lại `Aren_Sword.png` thành **9 ô**, đặt tên ô 8 là `Aren_Sword_BackHilt`. Gán `weaponPoses` dài 9 (`Assets/Editor/R2CArenSandboxSetup.cs`, và prefab `Player_Aren` nếu dùng bảng riêng).
   - `EquipmentVisual.Pick()` có kiểm giới hạn mảng: nếu chưa cắt lại, Idle hướng Down chỉ **ẩn kiếm**, không lỗi.
   - Phần nghiêng theo step (`Wrap8`) chỉ chạy trong Attack, nên không động tới ô 8.
3. **Brom, Selene:** chỉ cần copy CSV rồi chạy lại setup. Không có ô mới.
4. **Khiên `off_view = Down` ở hướng Up:** cố ý. Khiên đeo sau lưng quay mặt trước ra ngoài, nên nhìn từ sau thấy mặt có huy hiệu. Bảng `shieldViews` đã có ô Down sẵn.
5. **Kiểm tra trong Sandbox (F1–F5):**
   - Idle và Walk ở 3 hướng × quay trái/phải: không có tay cầm vũ khí, vũ khí nằm trên lưng, không giật khi đổi frame.
   - Attack và skill vẫn rút vũ khí ra tay.
   - Hurt dùng tư thế sau lưng.
   - Level 1–4: Aren trong campaign đổi theo nếu dùng chung `Aren_Anchors.csv`. Cần anh/chị xác nhận có áp dụng cho campaign hay chỉ Sandbox.

## 4. Lưu ý khi sinh lại art

Các script export cũ (`RenderBrom.ps1`, `RenderSelene.ps1`, export của Aren) vẫn ghi tư thế cầm tay cho Idle/Walk. Nếu chạy lại chúng, phải chạy **`tools/RenderBackGearExport.ps1` sau cùng** để áp lại tư thế đeo sau lưng.
