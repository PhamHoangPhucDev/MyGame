# R2 — GOBLIN SPRITE SPEC

**Trạng thái:** đặc tả, chờ duyệt. **Đề xuất: giữ art Goblin hiện tại.** Chỉ chỉnh phần phản hồi khi trúng đòn và khi chết. Chưa sản xuất trong R2B.

## 1. Hiện trạng Unity

| Mục | Giá trị |
|---|---|
| Sheet | `Assets/Art/GoblinCampPhase4/Goblin_Sheet.png` 104×88 |
| Ô | 26×22, pivot (0.5, 0.091) |
| Slice | Idle_0–1, Walk_0–3, Attack_0–2, Hurt_0 (10 sprite) |
| Palette chính | Mũ `#64293F`, viền `#2A2032`, mặt `#84C656`, bóng mặt `#2F5F54`, sáng mũ `#86405A`, mắt cam |
| Nhãn | `Goblin Lv.3` (luôn hiện) + HP bar — **giữ nguyên** |

## 2. So với tham chiếu

**Nguồn:**
- C22 0:04.8 và 0:08.0 (`ref_strips/enemy_zoom.png` phải trên, `frame_22_8.0.png`).
- C22 0:33.0–34.5 (`strip_C22_crowd.png`).

| Thuộc tính | Tham chiếu | Đánh giá |
|---|---|---|
| Silhouette | Thấp, rộng hơn cao, mũ trùm phủ đầu tạo vòm, tai nhọn ló hai bên, mắt cam 1–2 px | **Khớp** |
| Kích thước | ≈20×16 px art; hero ≈18×22 | **Khớp** (Unity thân ≈20×14) |
| Palette | Mũ `#543641` (một nhóm Lv.3 có biến thể ô liu `#555448`), mặt `#5D9C3A`–`#98A747` | Mặt Unity sáng hơn. Đổi tuỳ chọn (P2) |
| Idle | Quái nhỏ, nhấp nhô nhẹ | NOT MEASURED |
| Walk | — | NOT MEASURED (camera chạy, bị che) |
| Attack | — | **Không thấy rõ.** Giữ 3 frame hiện có (thiết kế hiện hữu, không có tham chiếu) |
| Hurt | **Silhouette trắng đặc** trong 1–2 khung video (C22 0:33.4–34.1) | Unity dùng sprite bạc màu → **khác (P1)** |
| Death | Silhouette **tối dần về gần đen**, vài cụm khói trắng tròn bay ra, mất trong khoảng 0.2–0.3 s (C22 0:33.5–33.9) | Unity mất ngay sau 0.25 s, không có hình → **khác (P1)** |
| Knockback | Lùi theo hướng đòn, có bụi trắng nhỏ (C22 0:33.6–34.1) | Unity có lực đẩy, chưa có bụi (P2) |

## 3. Đề xuất (cho giai đoạn sau, không thuộc R2B)

| Hạng mục | Cách làm | Asset cần |
|---|---|---|
| Hurt flash trắng | Shader hoặc tint trắng đặc 2 khung, không cần vẽ thêm | Không |
| Death tối dần | Tint sprite hiện tại về `#1C1A26` trong 3 bước + VFX khói | `VFX_Puff_White` 16×16 × 4 frame (dùng chung với Gobrider) |
| Palette mặt (tuỳ chọn) | Đổi `#84C656` → `#6FAA48`, `#2F5F54` giữ | Sửa pixel trong sheet hiện có |

## 4. Nghiệm thu khi làm

- Chụp cạnh `strip_C22_crowd.png`: thấy được frame trắng đặc khi trúng đòn và silhouette tối khi chết.
- Nhãn `Goblin Lv.3` và HP bar vẫn đi theo quái khi bị đẩy.
- Không đổi kích thước ô 26×22 hay pivot.
