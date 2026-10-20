# R2C — ClassKit + ClassCombat (bộ của Aren, phím đổi class): báo cáo

**Ngày:** 2026-10-20 · **Nhánh:** `reference_rebuild_r2c` · **Chưa commit** · Chỉ Sandbox dùng
**Sao lưu scene:** `IncomingAssets/backup_before_R2C_Aren/ReferenceCombatSandbox_beforeClassKit.unity`

## 1. Kiến trúc đã làm

| Phần | File | Vai trò |
|---|---|---|
| **ClassKit** | `Assets/Scripts/Combat/ClassKit.cs` (mới) | ScriptableObject cho 1 class: chỉ số gốc (hệ số máu, tốc chạy), `ComboDef` (thời gian hồi, bán kính, sát thương, đẩy lùi, tầm tự nhắm, thời điểm impact, `ComboStep[]` cho từng đòn: hướng vung, hệ số, biến thể VFX, đòn kết, hitstop, rung) và 5 `SkillDef` (phím 2/3/4/V/C: kiểu hành vi, thời gian hồi, sát thương, bán kính, tầm, tốc độ, thời lượng, số đợt, hệ số, hitstop, rung, màu số sát thương) |
| **SkillBehaviour** | trong `ClassKit.cs` | 5 hành vi hiện có: `BurstAhead`, `DashStrike`, `Whirlwind`, `Guard`, `CrescentStrike`. Class mới sẽ thêm kiểu (Projectile, GroundAoE, Teleport…) |
| **PlayerCombat** | sửa | **Vẫn là đường combat duy nhất** (phím, thời gian hồi, sự kiện cho hotbar, hướng cho `PlayerVisual`). Thêm `SetKit(kit)`: có kit thì combo, skill, hitstop đọc từ kit; `kit == null` thì đọc đúng các field và hằng số cũ |
| **ClassCombat** | `Assets/Scripts/Combat/ClassCombat.cs` (mới) | Chỉ có trong Sandbox. **F1–F5** = Aren / Brom / Lyra / Selene / Vex. Đưa kit vào `PlayerCombat`, áp hệ số máu và tốc chạy, hiện nhãn "Class: …" góc trên trái. Ô chưa có kit thì báo "chưa có bộ kỹ năng" và giữ class hiện tại |
| **PlayerMovement** | sửa | Thêm `SpeedMultiplier` (mặc định 1) |
| **Kit Aren** | `Assets/Art/Rebuild/R2C/ClassKits/ClassKit_Aren.asset` | Dựng bằng menu *Tools → Gameplay Prototype → R2C → Setup Sandbox ClassKit (Aren)*. **Lấy số liệu từ chính `PlayerCombat` đã serialize trong scene Sandbox** cộng các hằng số trong code |

**Vì sao không tách hẳn một `ClassCombat` thứ hai để chạy combat?** `PlayerVisual`, hotbar UI và `Health` đều tham chiếu `PlayerCombat` (hướng, thời gian hồi, sự kiện). Thay component sẽ làm đứt các liên kết đó và tạo hai bản logic combat song song. Cách này giữ một đường chạy duy nhất; dữ liệu class nằm trọn trong `ClassKit`.

**Phát hiện khi dựng:** scene Sandbox đặt **Dash Slash = 28 sát thương**, trong khi giá trị mặc định trong code là 26. Kit lấy 28 theo scene. Nếu dựng từ giá trị mặc định, hành vi đã bị đổi mà không ai biết.

## 2. Kiểm chứng "trùng hành vi hiện tại"

Một kịch bản đóng băng thời gian chạy **cùng chuỗi thao tác hai lần** trên cùng một quái (hồi đầy máu trước mỗi lần thử): lần 1 có kit Aren (KIT), lần 2 không kit (LEGACY).
- **Đo:** sát thương thực trúng, bán kính, đẩy lùi, hướng vung so với hướng mặt, biến thể VFX, thời điểm impact, thời gian hồi, độ dài hitstop, số đợt lốc, thời gian thủ và hệ số giảm sát thương, thời gian vung trước của Crescent.
- **Kết quả đầy đủ:** `kit_vs_legacy_compare.txt`.

| Mục | KIT | LEGACY |
|---|---|---|
| R1 / R2 / R3 | 18 / 18 / 18 sát thương · bán kính 1.35 / 1.35 / 1.8225 · đẩy lùi 2.8 / 2.8 / 3.5 · vung +40.4° / −40.4° / 0° · VFX 0 / 1 / 2 · impact 0.12 s · hồi 0.35 s · hitstop **0.048 / 0.048 / 0.058** | giống hệt |
| 2 Flame Burst | 30 · hồi 5 s · hitstop 0.050 | giống hệt |
| 3 Dash Slash | **28** · hồi 4 s · hitstop 0.050 | giống hệt |
| 4 Whirlwind | 20 mỗi đợt · 3 đợt · hồi 7 s · hitstop 0.050 | giống hệt |
| V Guard | 1.4 s · nhận ×0.35 sát thương · hồi 8 s | giống hệt |
| C Crescent | 42 · vung trước 0.12 s · hồi 10 s · hitstop 0.060 | giống hệt |
| Chỉ số | Máu 400, tốc chạy ×1 | — |

**Kết luận: 16/16 mục khớp tuyệt đối.**

| Kiểm thử khác | Kết quả |
|---|---|
| F2 (ô Brom còn trống) | Trả về false, vẫn giữ Aren, hiện thông báo |
| **Level 1–4** (`GameplayPrototype_Phase4Test`, Play) | Không có kit, `ClassCombat` hay `EquipmentVisual`. Bấm R chạy bình thường, `SpeedMultiplier = 1`, **0 lỗi** |
| Biên dịch và console Sandbox | 0 lỗi |

## 3. File

- **Mới:**
  - `Assets/Scripts/Combat/ClassKit.cs`, `Assets/Scripts/Combat/ClassCombat.cs`
  - `Assets/Art/Rebuild/R2C/ClassKits/ClassKit_Aren.asset`
- **Sửa:**
  - `PlayerCombat.cs`: thêm nhánh kit; mọi nhánh không có kit giữ nguyên code cũ.
  - `PlayerMovement.cs`: +2 dòng.
  - `R2CArenSandboxSetup.cs`: thêm menu dựng kit.
  - Scene Sandbox: `Player` có thêm `ClassCombat`.
- **Không đổi:** scene Level 1–4, `PlayerVisual`, `CombatVfx`, `HitStop`, `Health`, Enemy AI, LevelFlow, GameProgress, hotbar.

## 4. Chưa làm / bước sau

1. **Bộ thứ hai (đề xuất: Vex)**, gồm:
   - Kiểu hành vi mới: combo 4 đòn nhanh, `Teleport` (Bước Bóng), `ProjectileFan` (Phi Dao), `Stealth` (Bom Khói), `Execute` (Đoạt Mệnh).
   - Thân, frame Attack, VFX đỏ thẫm, trang bị song đao.
   - Khi đổi class, `ClassCombat` cũng phải đổi controller của thân và bộ trang bị của `EquipmentVisual`. Đã chừa chỗ; hiện chỉ có Aren nên chưa nối.
2. **Giảm sát thương gốc theo class** (Brom 15%): cần tách khỏi hệ số của Guard. Hiện `EndGuard` đặt hệ số về 1, nên chưa áp được. Sẽ sửa khi làm Brom.
3. **Hotbar** đọc tên skill từ `SkillDef.displayName`. Hiện hotbar vẫn hiện tên cũ; với Aren thì trùng.
