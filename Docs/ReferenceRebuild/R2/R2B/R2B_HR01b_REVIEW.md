# R2B — HR01b Walk_Side + Attack_Side (chờ duyệt, CHƯA import Unity)

## File

| File | Nội dung |
|---|---|
| `Hero_Sheet_R2b.png` | **Bản ứng viên cuối:** `Hero_Sheet_R2.png` (HR01, đã duyệt làm nền) + 7 frame vẽ lại. 128×288, 33 ô, cùng thứ tự slice |
| `Hero_Sheet_R2.png` | HR01 gốc, giữ nguyên để so sánh và quay lại |
| `HR01b_compare.png` | So sánh trước/sau (×6) + tham chiếu C17 và C25 |
| `tools/HeroSheetR2bGen.cs` | Bộ vẽ 7 frame mới: dáng riêng cho từng frame, không ghép từ khối thân đứng |

## Kiểm tra kỹ thuật (đã đo)

| Tiêu chí | Kết quả |
|---|---|
| Chỉ đổi Walk_Side (hàng 5) và Attack_Side (hàng 8) | **0 pixel thay đổi** ở 26 frame còn lại ✔ |
| Palette | 0 pixel ngoài HR06, không có pixel bán trong suốt ✔ |
| Chân chạm hàng 29 (pivot 0.0625) | 7/7 ✔ |
| Nằm trong ô 32×32 | Rộng nhất 26 px (strike, x 4–29) ✔ |
| Hướng | Tất cả vẽ hướng **trái** (Unity lật khi sang phải) ✔ |

## Walk_Side (theo C17 0:36.45)

| Frame | Dáng | Khung |
|---|---|---|
| 0 | Chạm đất A: chân gần (`#3C3C48`) bước trước, chân xa (`#1C1A26`) đạp sau, lộ bàn chân màu kem | 20×19 |
| 1 | Chuyển: hai chân thu dưới hông, thân nhấc 1 px, tóc ngắn lại | 19×20 |
| 2 | Chạm đất B: đổi chân trước/sau (đảo màu chân gần/xa), đuôi tóc lệch khác frame 0 | 20×19 |
| 3 | = frame 1 | 19×20 |

**Thay đổi so với HR01:**
- Thân ngả khoảng 30°: đầu thấp, ở phía trước; hông lùi về sau.
- Vạt áo bay về sau.
- Tóc thành khối kéo ra sau. Đã rút ngắn 3 px sau lần thử đầu vì quá nhọn so với C17.
- Bước chân dang rõ, thấy bàn chân.

## Attack_Side (theo C25 0:02.53)

| Frame | Dáng | Thời điểm trong clip R1 |
|---|---|---|
| 0 lunge | Khom 1 bậc, ngả trước, vạt áo dài ra sau, tư thế bước tới | 0.08 s |
| 1 strike | **Khom sâu nhất (cao 18 px)**, áo choàng xoè thành vệt ngang dài 26 px, chân trước lao tới, chân sau duỗi | **0.12 s** = khung gây sát thương |
| 2 recover | Hồi về khom nhẹ, chân thu lại, tóc ngắn lại | 0.19 s |

## Lưu ý cần duyệt

1. **Kiếm:** frame body không có kiếm. Ở C17 và C25, kiếm là thứ tạo cảm giác lực lớn nhất (kéo sau khi chạy, cầm chéo khi chém). Thiếu HR05 (`Hero_Sword_Swing`) thì Attack_Side trong game vẫn chỉ có body + lưỡi liềm VFX.
2. **Chuôi kiếm sau lưng** không còn ở Walk_Side và Attack_Side, vì theo tham chiếu kiếm đang cầm tay. Khi chưa có HR05, `PlayerVisual` vẫn vẽ kiếm nghỉ sau lưng như hiện tại, nên sẽ không bị mất kiếm.
3. **Bob 1 px** ở frame chuyển (1, 3): thân nhấc lên khi hai chân thu dưới hông, giống chu kỳ chạy trong tham chiếu.
4. **Giới hạn:** dáng được vẽ bằng lưới ký tự. Bản này đạt silhouette, tỉ lệ và palette. Nếu sau khi xem trong game vẫn muốn mượt hơn (ví dụ tóc có lọn, áo có nếp), có thể cleanup tay trong Aseprite trên chính file này mà không phải đổi khung.

## Bước import (chờ duyệt, chưa làm)

1. Sao lưu `Assets/Art/GoblinCampPhase4/Hero_Sheet.png` vào `IncomingAssets/backup_before_R2B/`.
2. Ghi đè pixel bằng `Hero_Sheet_R2b.png`, **giữ `.meta`**. 33 slice và mọi clip Animator vẫn trỏ đúng.
3. Chụp Sandbox ở 16:9 và 9:16: idle, chạy trái/phải, chém R. Kiểm tra đòn vẫn trúng ở 0.12 s, Hero không mất frame trong 60 s (tiêu chí Plan Pack).
