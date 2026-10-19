# R2 — GOBRIDER SPRITE SPEC

**Trạng thái:** đặc tả, chờ duyệt. Làm **sau Hero** (không thuộc R2B).

## 1. Hiện trạng Unity

| Mục | Giá trị |
|---|---|
| Sheet | `Assets/Art/GoblinCampPhase4/Gobrider_Sheet.png` 120×96 |
| Ô | 30×24 |
| Slice | Idle_0–1, Walk_0–3, Attack_0–2, Hurt_0 (10 sprite) |
| Hình | Lợn rừng nâu nhìn ngang, goblin nhỏ đội mũ nhọn ngồi trên lưng. Silhouette **nằm ngang** |
| Dùng chung | Elite Gobrider và Boss Warlord hiện cũng dùng sheet này (tô màu, phóng to) |
| Nhãn | `Gobrider Lv.5` — **giữ nguyên** (tham chiếu ghi Lv.6; theo yêu cầu giữ cấp Unity) |
| Code | Có `ChargeTelegraph` / `ChargeActive` trong `EnemyController` |

## 2. Tham chiếu

**Nguồn:**
- C22 0:08.0 (`ref_strips/enemy_zoom.png` trái + giữa; `frame_22_8.0.png` vùng phải trên).
- `GameReference/frames/04_enemy_approach_34_0s.jpg`.

| Thuộc tính | Quan sát |
|---|---|
| Silhouette | Khối **đứng, gần vuông**: goblin mặt xanh đội mũ trùm nhọn đỏ sẫm (chóp đỏ tươi) ngồi cao trên một con thú thân dày màu hồng–nâu, chân ngắn to. Một biến thể cầm vũ khí xương/sừng dài màu be chĩa ngang. **Khác hoàn toàn** với lợn nằm ngang của Unity |
| Kích thước | ≈ **23×26 px art** (±2). Cao hơn hero một chút, to rõ hơn Goblin |
| Palette | Thú `#977371`, viền/bóng `#692F3E`, mũ `#543641` + chóp đỏ, mặt goblin `#98A747`, vũ khí be `#D8C8A0` (đề xuất từ ảnh), mắt cam |
| Idle / Walk / Attack | NOT MEASURED (nhóm Gobrider đứng xa, camera di chuyển) |
| Hurt / Death / Knockback | Không có cảnh Gobrider riêng. Giả định giống Goblin (flash trắng, tối dần + khói). **Ghi là giả định** |
| Charge | **NOT OBSERVED** trong các clip đã xem. Mọi hình ảnh charge là **thiết kế bổ sung** |

## 3. Spec đề xuất

| Mục | Giá trị |
|---|---|
| Ô frame | **32×32** (thay 30×24). Cần cập nhật collider (Unity hiện 0.7×0.65 đơn vị) và vị trí nhãn |
| Pivot | (0.5, 0.0625), giống hero |
| Hướng | Vẽ **hướng trái** + lật. Không có bằng chứng cho hướng trước/sau → **không tạo** |
| Idle | 2 frame (giữ số hiện có) |
| Walk | 4 frame (giữ) |
| Attack | 3 frame (giữ). Frame 2 = hit (khớp code hiện tại) |
| Hurt | 1 frame + flash trắng (shader) |
| Charge (thiết kế bổ sung) | 2 frame cúi đầu (telegraph) + 2 frame lao (dùng lại Walk nhanh nếu muốn tiết kiệm) |
| Death | Tint tối + VFX khói dùng chung |

Elite / Boss: **không** tạo bằng cách tô màu sheet Gobrider mới. Boss cần spec riêng (giai đoạn boss).

## 4. Nghiệm thu

- Đặt cạnh `enemy_zoom.png` ở ×4: silhouette đứng, gần vuông, mũ nhọn, thú hồng–nâu.
- Gobrider cao hơn hero 2–6 px.
- Nhãn `Gobrider Lv.5` không chồng lên đầu mũ.
- Charge, nếu làm, được ghi rõ là thiết kế bổ sung trong release note.
