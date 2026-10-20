# Aren — Vệt chém + Hurt + Death v1: chờ duyệt

**Ngày:** 2026-10-20 · **Không import Unity**
**Tạo lại:** `tools/RenderFx.ps1` (dùng `FxGen.cs` cùng các generator trước).

## File

| File | Nội dung |
|---|---|
| `Aren_SlashVFX_{Down,Up,Side}_v1.png` | 96×48: 2 frame vệt chém, ô 48×48, căn theo canvas ghép (ô thân 32×32 đặt tại (8,8)) |
| `Aren_Attack_VFX_v1_review.png` | Attack 3 hướng × 3 frame, có vệt chém |
| `Aren_Hurt_Death_v1_review.png` | Hurt 3 hướng × 2 frame, Death 3 frame |

## Vệt chém

- **Hình dạng:** trăng lưỡi liềm **đi qua đúng mũi kiếm ở F1**. Đầu sáng và dày nhất (3 px, lõi trắng) nằm ở mũi kiếm, thon dần (1 px, xanh nhạt) về phía kiếm vừa quét qua.
- **Màu:** trắng, xanh băng nhạt, xanh vừa, viền xanh đậm. Đọc rõ trên nền cỏ.

| Hướng | Tâm cung | Bán kính | Góc quét | Lớp vẽ |
|---|---|---|---|---|
| Down | (16,24) | 10.3 px | 140°, từ trái vòng xuống dưới tới mũi kiếm | Trước thân |
| Up | (16,18) | 9.1 px | 110°, từ vai phải vòng lên qua mũ | **Sau thân** (kiếm chém ra xa camera) |
| Side | (16,21) | 12.4 px | 150°, từ sau lưng vòng qua đỉnh ra trước | Trước thân |

- **VFX0** (cùng F1, 0.10 s): cả lưỡi liềm.
- **VFX1** (cùng F2, 0.08 s): chỉ còn nửa trước, mỏng hơn, kéo quá mũi kiếm 20°.
- **Gắn trong Unity:** đặt tâm cung tại toạ độ trên (px ÷ 16) so với pivot thân. Nhờ vậy vệt chém luôn dính vào mũi kiếm, sửa được lỗi vệt chém tách rời khỏi Hero trong Sandbox cũ (`SlashPullBack`). Khi đổi kiếm dài hoặc ngắn hơn, bán kính tự tính lại từ độ dài lưỡi.

## Hurt (2 frame, 0.18 s)

| Frame | Nội dung | Thời gian |
|---|---|---|
| H0 | Toàn bộ nhân vật (thân + kiếm + khiên) loé **trắng** | 0.06 s |
| H1 | Nhuộm **đỏ** khoảng 35%, thân giật xuống 1 px (dùng thân tiếp đất của Walk), kiếm và khiên hạ theo | 0.12 s |

Trong Unity, nên làm tint bằng material/shader áp lên mọi SpriteRenderer (thân, kiếm, khiên), để không phải vẽ lại từng món trang bị. Ảnh ở đây là mẫu màu đích.

## Death (3 frame, chung cho mọi hướng)

| Frame | Nội dung | Thời gian |
|---|---|---|
| D0 quỳ | Thân hạ 2 px, chân gập còn 1 hàng, mũi kiếm chống đất, khiên hạ | 0.15 s |
| D1 ngã | Thân **xoay 90° chính xác** (không méo pixel), nằm ngang 21×14, đầu bên trái, sát mặt đất (hàng 29). Kiếm và khiên ẩn khi đang ngã | 0.15 s |
| D2 nằm | Nằm như D1, **kiếm rơi nằm ngang** trước người, **khiên ngửa** cạnh chân | giữ nguyên |

- Kiếm và khiên rơi lấy theo trang bị đang dùng, nên đổi skin vũ khí vẫn khớp.
- Hạt vỡ hoặc khói khi chết (như clip tham chiếu) là VFX riêng, chưa làm.

## Điểm cần quyết

1. Màu vệt chém: xanh băng (hợp kiếm thép). Có thể cho mỗi vũ khí một màu (ví dụ kiếm hàn dùng vệt đỏ), đã chừa trường `màu VFX` trong `WeaponDef`.
2. Death chỉ có 1 bộ chung cho mọi hướng. Nếu muốn ngã theo hướng đang quay mặt, cần thêm 2 bộ nằm.
