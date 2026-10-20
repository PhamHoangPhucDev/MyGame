# Aren — Idle Down v1 (4 frame): chờ duyệt

**Ngày:** 2026-10-20 · **Không import Unity**
**Tạo lại:** chạy `tools/RenderArenIdle.ps1` (dùng `HeroConceptGen.cs` + `ArenIdleGen.cs`; frame gốc lấy từ Aren v3 trong roster đã duyệt).

## File

| File | Nội dung |
|---|---|
| `Aren_IdleDown_v1.png` | 128×32: 4 ô 32×32, nền trong suốt. **File giao nộp** |
| `Aren_IdleDown_v1_review.png` | 4 frame phóng ×8, ô đỏ đánh dấu px khác F0. Bên dưới: 4 frame ở cỡ thật trong khung Portrait 144×256 và Landscape 480×270 |

## Chuyển động

| Frame | Thay đổi | Px khác F0 | Thời gian |
|---|---|---|---|
| F0 | Tư thế gốc (Aren v3) | 0 | 0.25 s |
| F1 | Chỏm lông bay sang phải, đuôi lông nhấc lên, gấu áo choàng xòe ra 1 px | 7 | 0.15 s |
| F2 | **Thở:** mũ (hàng 0–9) gật xuống 1 px, đè lên cổ giáp; đuôi chỏm lông buông thấp hơn | 65 | 0.25 s |
| F3 | Chỏm lông bay sang trái, đuôi lông rủ xuống, gấu áo choàng thu lại | 5 | 0.15 s |

- **Vòng lặp 0.80 s.** Chậm hơn nhịp 0.60 s của Hero cũ, để hợp với một hiệp sĩ mặc giáp nặng.
- **Kích thước:** F0, F1, F3 là 14×21. F2 là 14×20, vì đỉnh mũ hạ 1 px khi thở. Đế giày luôn ở hàng 29, pivot (0.5, 0.0625) giữ nguyên, nên trong game nhân vật không bị trượt.
- **Kiếm:** không vẽ, vẫn là sprite riêng (thiết kế kiếm làm sau).

## Điểm cần quyết khi duyệt

1. **Nhịp thở ở F2:** mũ gật 1 px nhìn rõ ở cỡ ×4 (Portrait trên máy). Nếu thấy quá mạnh, có thể chỉ dùng chỏm lông và áo choàng, không gật mũ.
2. **Thời gian:** 0.25 / 0.15 / 0.25 / 0.15 s. Có thể đổi thành 4 × 0.20 s cho đều.

## Sau khi duyệt

Dùng cùng kỹ thuật cho Idle Up và Idle Side (3/4). Sau đó vẽ Walk 4 hướng, rồi Attack (kiếm là sprite riêng, có điểm grip theo từng frame). Mỗi bước đều dừng để duyệt.
