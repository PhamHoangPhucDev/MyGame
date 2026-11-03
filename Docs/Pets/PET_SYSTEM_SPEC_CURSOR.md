# Pet System v2: spec cho Cursor

Ngày: 2026-11-03 · Phạm vi: chỉ logic gameplay. Không đổi UI layout, art, prefab, scene.
File chính: `Assets/Scripts/Progression/Pets.cs` (đang chưa commit ở main checkout).
API có sẵn: `Health.TakeDamage(int, Vector2, Vector2)`, `Health.Died`, `CombatTargeting.FindNearestEnemy / LockedOrNearest`,
`WorldTx.Begin()...Commit()`, `WorldSave.GetQty / KeyItem`, `PlayerMovement.MoveSpeed`, `GameplayClock.CheckOutOfCombat()`.

## 0. Quyết định mặc định (chủ dự án có thể đổi trước khi code)

| # | Quyết định | Mặc định |
|---|---|---|
| D1 | "Pet cùi" là gì | Độ hiếm roll khi nở: Common 60% / Rare 30% / Epic 10%. "Cùi" = Common |
| D2 | Pet nào hiến tế được | Chỉ Common, không active |
| D3 | Mảnh / pet | 1 mảnh mỗi pet Common. 10 mảnh = 1 Mystery Pet Egg |
| D4 | Pet có bị đánh/chết không | v2: không. Kẻ địch không nhắm pet, pet không có HP |

Mọi con số bên dưới là hằng số tuning, gom vào một khối `const` ở đầu `Pets`.

## 1. Sửa di chuyển (ưu tiên 1, làm trước)

Lỗi đã xác định khi đọc code:

| # | Nguyên nhân gốc | Vị trí | Sửa |
|---|---|---|---|
| M1 | Đứng sai bên: `HeelPoint` đọc `hero.localScale.x`, nhưng hero quay mặt bằng `SpriteRenderer.flipX` (`PlayerVisual.cs:228/412/416`). Giá trị luôn dương nên pet luôn nhắm bên trái hero, đi xuyên qua hero khi hero đi sang trái | `Pets.cs:356` | Thêm getter chỉ đọc `public Vector2 Facing` vào `PlayerVisual` (hoặc dùng hướng di chuyển cuối của `PlayerMovement`). Điểm heel = **phía sau** hero: `hero - facing.normalized * Heel` |
| M2 | Walk 4.5 = tốc độ hero 4.5, nên pet không bao giờ thu hẹp khoảng cách. Run 5.1 chỉ nhanh hơn 13% | `Pets.cs:33` | Tốc độ theo hero: `follow = heroSpeed * 1.15`, `catch = heroSpeed * 1.6` (`heroSpeed = MoveSpeed * SpeedMultiplier` lúc chạy) |
| M3 | Không có vùng trễ: khoảng cách lắc quanh `Stop = 0.85`, nên pet dừng/đi luân phiên từng frame | `Pets.cs:270` | Đo khoảng cách tới **điểm heel**: bắt đầu đi khi `> 0.9`, dừng khi `< 0.35` |
| M4 | Tăng/giảm tốc tức thời | `Pets.cs:277` | Giữ `_vel`; `_vel = MoveTowards(_vel, desired, Accel*dt)`, `Accel = 25`, `Decel = 35` |
| M5 | Khựng hoạt ảnh: 1 frame dừng cũng đặt col = 0 | `PetCompanion.Animate` | Chỉ chuyển sang idle sau khi đứng yên `>= 0.12s`. FPS đi = `8 * speed / heroSpeed`, kẹp trong khoảng 6–12 |
| M6 | Đi lùi: khóa mặt 0.25s, kể cả khi đổi trái↔phải | `Animate`, `_faceLock` | Đổi trái/phải tức thì. Chỉ khóa 0.15s khi đổi hàng (dọc↔ngang) |
| M7 | Giật hình: `Legal()` gọi `GameObject.Find("zone_housing")` tới 64 lần/frame; `FindGameObjectWithTag` mỗi frame | `Pets.cs:358,386,397` | Cache transform hero và zone housing; làm mới khi `Appear()` / đổi zone |
| M8 | Điểm heel bị chặn: chọn lại `SafeSpot` mỗi frame nên mục tiêu nhảy lung tung | `Pets.cs:273` | Giữ điểm hợp lệ cuối nếu nó cách điểm heel mới `< 0.5` |

Thêm: dịch chuyển tức thì về heel nếu khoảng cách `> 12` (kẹt hoặc bị tụt lại), dùng lại `SafeSpot`.

Nghiệm thu M:
- Hero đi 4 hướng và chéo 10s: pet luôn ở sau hero, không vượt hoặc đi xuyên qua hero, không đổi walk↔idle quá 1 lần/giây khi hero đi đều.
- Hero chạy rồi dừng: pet giảm tốc và dừng trong ≤ 0.5s ở sau lưng hero.
- Hero quay đầu 180°: pet vòng ra sau, không đi lùi (hướng mặt khớp hướng đi trong ≤ 1 frame khi đổi trái/phải).
- Profiler: `Pets.Tick` không tạo GC alloc và không gọi `GameObject.Find` mỗi frame.

## 2. Chỉ số và cấp độ

Dòng save mở rộng (tương thích ngược, các cột thiếu dùng mặc định):
`id,species,follow,owner,rarity,level,xp`. Thiếu `rarity` → Common, thiếu `level` → 1, thiếu `xp` → 0.

- `rarity` ∈ {0 Common, 1 Rare, 2 Epic}, roll trong `Hatch()` theo D1, ghi trong cùng một `WorldTx`.
- Cấp `1..20`. XP để lên cấp: `need(L) = 20 + 10*L`. Thừa XP thì chuyển sang cấp sau; tới cấp 20 thì không cộng XP nữa.
- Nhận XP: chỉ **pet đang active** nhận, mỗi khi một kẻ địch mà hero hoặc pet gây sát thương bị giết trong zone hiện tại. Lượng XP = exp kẻ địch đó đã có (`WorldActivities` truyền `exp` vào `OnDied`) × `0.5`. Dùng một hook `Pets.OnEnemyKilled(int exp)`.
- Chỉ số (tính ra, không lưu):
  - `ATK = Base[species] * RarityMul * (1 + 0.08*(L-1))`, làm tròn, tối thiểu 1.
  - `RarityMul`: Common 1.0 · Rare 1.3 · Epic 1.7.
  - `Base` (dog, cat, fox, rabbit, wolf, dragon) = 4, 3, 4, 2, 5, 6.
  - `AtkInterval` (giây) = 1.1, 0.8, 1.0, 0.7, 1.2, 1.5. `Range` = 0.9 cho tất cả; riêng dragon 2.5 (bắn từ xa).
- Lưu: thêm XP gộp ghi tối đa 1 lần/2s hoặc khi lên cấp (một `WorldTx`), không ghi mỗi lần giết.
- Getter công khai để UI dùng sau: `RarityAt(i)`, `LevelAt(i)`, `XpAt(i)`, `XpNeedAt(i)`, `AtkAt(i)`.

Nghiệm thu S: save cũ (4 cột) load được, pet thành Common cấp 1; giết quái lên cấp đúng công thức; khởi động lại vẫn giữ cấp/XP.

## 3. Hỗ trợ chiến đấu

Máy trạng thái trên pet active, chỉ khi `follow == true`:

`Follow → Engage → Return → Follow`

- **Follow → Engage**: hero đang trong combat (`GameplayClock.CheckOutOfCombat() != Deny.None`) VÀ có mục tiêu = `LockedOrNearest(mục tiêu hero đang khóa, heroPos, 4.0, enemyMask)` còn sống.
- **Engage**: đi tới mục tiêu bằng cùng code di chuyển (M2–M4, dùng tốc độ catch). Trong `Range`: dừng, quay mặt về mục tiêu, cứ mỗi `AtkInterval` gọi `target.TakeDamage(ATK, targetPos, dir*0.5f)`. Mục tiêu chết → chọn mục tiêu kế trong 4.0 quanh hero, không có thì sang Return.
- **Return** (bỏ mục tiêu): khoảng cách pet↔hero `> 6`, hoặc hero hết combat, hoặc hero chết. Chạy về heel với tốc độ catch, sau đó sang Follow.
- **Stay**: không bao giờ đánh. **Housing zone / vùng an toàn**: không bao giờ đánh.
- Hoạt ảnh tấn công: chưa có art, dùng tạm frame walk col 0 + nhích nhẹ 0.1 về phía mục tiêu trong 0.1s. Không tạo asset.
- Sát thương của pet phải đi qua `Health.TakeDamage` để popup/defense/death chạy đúng như cũ. Pet không được giành aggro (D4).

Nghiệm thu C: cạnh 3 quái, pet active đánh mục tiêu hero đang khóa; quái chết thì đổi mục tiêu; hero chạy xa > 6 thì pet bỏ đánh và theo hero; Stay thì không đánh; trong Home không đánh.

## 4. Hiến tế → mảnh trứng → trứng

Item mới trong `WorldContent`: `pet_egg_shard` ("Pet Egg Shard", Material, stack 999). Icon: dùng tạm `UiKit.ItemIcon` mặc định cho tới khi có art.

`Pets.CanSacrifice(id)` trả lý do từ chối:
- không có pet · pet đang active ("Recall first") · không phải Common (D2) · đang nở trứng (`_lock`) · đang combat.

`Pets.Sacrifice(id)`: **một** `WorldTx` xóa dòng pet + `shard += 1` (D3). Sửa bộ nhớ chỉ sau khi `Commit`, giống `Hatch()`. Chặn bấm 2 lần.

`Pets.CanCombine()` / `Pets.Combine()`: cần `shard >= 10`; **một** `WorldTx` gồm `shard -= 10`, `egg += 1`. Kẹp trứng theo stack 99, đầy thì từ chối ("Egg bag full").

UI: chỉ thêm nút **SACRIFICE** (dùng `Cmd` có sẵn, cùng style) vào cột Details của `PetHud`, có hỏi xác nhận; thêm nút **COMBINE** vào ô chi tiết item mảnh trong Bag, cạnh nút HATCH có sẵn. Không đổi layout khác.

Nghiệm thu E:
- Hiến tế pet Common không active: pet mất, +1 mảnh. Pet Rare/Epic/đang active: nút bị tắt và có lý do.
- 10 mảnh → Combine → 9 mảnh thì sai, đúng là 0 mảnh + 1 trứng; nở được ngay.
- Tắt game giữa giao dịch: không bao giờ có trạng thái nửa vời (mất pet mà không có mảnh).

## 5. Thứ tự làm và test

1. M1–M8 (một commit). 2. Chỉ số/cấp + migrate save. 3. Chiến đấu. 4. Hiến tế/ghép.

EditMode test (hàm thuần, không cần scene): parse dòng save cũ/mới, `need(L)` và chuyển XP thừa, ATK theo cấp/độ hiếm, các lý do từ chối của `CanSacrifice`, toán mảnh của `Combine`.
PlayMode/thủ công: các mục nghiệm thu M và C. Ghi rõ VERIFIED / FAILED / NOT_RUN cho từng mục.

Ngoài phạm vi: pet bị đánh/chết, kỹ năng pet, nhiều pet active, art tấn công, thiết kế lại UI.
