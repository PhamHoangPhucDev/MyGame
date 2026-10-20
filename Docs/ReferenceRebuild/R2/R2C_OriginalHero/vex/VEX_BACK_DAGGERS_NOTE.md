# Vex — dao găm đeo chéo chữ X sau lưng (Idle / Walk / Hurt): ghi chú cho Cursor

**Ngày:** 2026-10-20 · **Người viết:** Claude (chỉ art + ghi chú, không sửa code, scene hay prefab)
**Đã duyệt:** `vex/VEX_BACK_DAGGERS_v1.png` · **Nguồn:** `tools/VexEquip.cs` (`Rest`, `Peek`, `DaggerSprite`, `HandRest`), chạy `tools/RenderVexEquip.ps1`.

## 1. Thay đổi về hình

- **Idle / Walk / Hurt:** hai dao găm nằm trong vỏ, **chéo chữ X giữa lưng**, chuôi hướng lên, lưỡi hướng xuống. Không cầm dao trên tay.
  - **Up:** thấy trọn chữ X trên lưng, vẽ **trước** thân.
  - **Side:** chữ X nằm sau mép lưng, thấy một nửa, vẽ **sau** thân.
  - **Down:** thân che chữ X, nên chỉ thấy **hai chuôi dao nhô qua hai vai**. Dùng 2 sprite mới (ô 8 và 9), vẽ **sau** thân.
- **Attack không đổi.** F0 rút dao, F1 đâm, F2 thu về vẫn **cầm dao trên tay**: F2 dùng tư thế cầm cũ (`HandRest`). Death không đổi.

## 2. File thay đổi trong `Docs/.../unity_export_vex/` (đã kiểm checksum: chỉ 2 file)

| File | Thay đổi |
|---|---|
| `Gear/Vex_Dagger.png` | **384×48 → 480×48 (10 ô 48×48).** Ô 0–7 giữ nguyên 8 hướng (R, UR, U, UL, L, DL, D, DR). **Ô 8 = chuôi nhô vai trái, ô 9 = chuôi nhô vai phải** (chỉ dùng cho Down). Pivot mọi ô (0.5, 0.5) = tay cầm. Kiểm pixel: 0 pixel ở viền ô, 0 pixel bán trong suốt |
| `Vex_Anchors.csv` | Chỉ đổi **30 dòng**: `Idle_*` (12), `Walk_*` (12), `Hurt_*` (6). Mọi dòng `Attack_*` (kể cả `@step`) và `Death_*` **giữ nguyên** |

Các file khác (`Vex_Sheet.png`, `Extra/*`, `Vex_Slash`, `Vex_Smoke`, `Vex_Knife`, `Vex_ExecuteX`) không đổi.

### Giá trị mới (toạ độ sheet; Side đã lật; Walk F0/F2 và Hurt_*_1 là y +1)

| View | main (x, y, dir, order) | off (x, y, off_dir, order) |
|---|---|---|
| Down | 9, 20, **8**, −2 | 22, 20, **9**, −1 |
| Up | 14, 20, 7, +1 | 18, 20, 5, +2 |
| Side | 23, 19, 5, −2 | 19, 19, 7, −1 |

## 3. Việc cần làm phía Unity (Cursor)

1. Copy 2 file trên sang `Assets/Art/Rebuild/R2C/Vex/` (`Gear/Vex_Dagger.png`, `Vex_Anchors.csv`).
2. **`Assets/Editor/R2CVexSandboxSetup.cs`:** đang cắt 8 ô (`for (int d = 0; d < 8; d++)`, dòng 44) và gán `weaponPoses` theo `Dir8` (dòng 177).
   - Cắt 10 ô; đặt tên ô 8–9 là `Vex_Dagger_PeekL` và `Vex_Dagger_PeekR`, pivot (0.5, 0.5).
   - Gán `weaponPoses` (và `offPoses` nếu là mảng riêng) **dài 10**, theo thứ tự `Dir8` rồi tới PeekL, PeekR.
3. **`EquipmentVisual`** không cần sửa logic:
   - `Pick()` đã kiểm giới hạn mảng. Nếu chưa cắt lại 10 ô, view Down chỉ **ẩn dao** chứ không lỗi.
   - Phần nghiêng theo step (`Wrap8`) chỉ chạy khi `step >= 0`, tức trong Attack, nên không động tới ô 8/9.
   - Ô 8/9 chỉ dùng ở Down, không bao giờ bị flip.
4. Chạy lại setup Vex trong Sandbox. Kiểm: Idle/Walk 3 hướng × quay trái/phải; Hurt; R combo 4 đòn vẫn cầm dao ở cả F0, F1, F2; Death không đổi. Level 1–4 không đổi.

## 4. Ghi chú

- Dao ngắn (lưỡi 4 px). Đặt chữ X đúng giữa lưng thì nhìn từ trước bị thân che hết, nên view Down dùng 2 chuôi nhô vai (anh/chị đã duyệt ở bảng mẫu).
- Bảng combo `vex/Vex_combo_v1_review.png` đã render lại với dao đeo sau lưng ở ô Idle.
