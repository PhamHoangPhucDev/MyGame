# Sửa 2 lỗi dual-aspect R1 — báo cáo

**Ngày:** 2026-10-19 · **Nhánh:** `reference_rebuild_r1` · **Chưa commit**
**Sao lưu trước khi sửa:** `IncomingAssets/backup_before_aspect_fix/` (3 file)

## Nguyên nhân và cách sửa

| # | Lỗi | Nguyên nhân | Sửa |
|---|---|---|---|
| 1 | `AspectGameViewHelper.SetSize` không đổi được Game View | (a) So khớp mọi ô theo số `width/height`, kể cả ô kiểu tỉ lệ, nên dễ trả sai chỉ số hoặc tạo ô trùng. (b) Chọn ô bằng cách gán thẳng `selectedSizeIndex`, cách này **không** áp dụng kích thước. (c) Lấy `GameViewSizes.instance` sai chỗ (property nằm ở lớp generic `ScriptableSingleton<>`) | Chỉ khớp ô `FixedResolution`. Chọn ô bằng `GameView.SizeSelectionCallback` (đúng hàm mà dropdown của Game View gọi). Thêm `TargetSize()`. `SetSize` giờ trả `bool` và cảnh báo nếu kích thước thực tế không khớp |
| 2 | Màn hình dọc vẫn chạy profile Landscape | `AspectProfileService.Awake()` luôn áp dụng `activeId` lưu trong scene (Landscape) | Thêm `autoDetectFromScreen` (mặc định bật): chọn Portrait khi `Screen.height > Screen.width`, và kiểm tra lại trong `Update` khi độ phân giải đổi (xoay máy, đổi cửa sổ). `Apply()` / `Toggle()` (menu, F8) được tính là **chọn tay** và tắt tự động trong phiên đó. `ApplyDetected()` bật lại tự động |
| 2b | (tìm thêm) | `ReferenceSandboxController.Start()` gọi `Apply(startProfile = Landscape)`, ghi đè lựa chọn tự động | Thêm `forceStartProfile` (mặc định tắt). Khi tắt, gọi `ApplyDetected()` |

**File sửa:**
- `Assets/Editor/AspectGameViewHelper.cs`
- `Assets/Scripts/Camera/AspectProfileService.cs`
- `Assets/Scripts/Level/ReferenceSandboxController.cs`

**Không đổi:** scene (Sandbox vẫn lưu lúc 16:13), profile asset, gameplay, combat, label.

## Kiểm chứng

Ảnh: `aspect_fix/A1_portrait_auto.png`, `A2_landscape_auto_switch.png`, tổng hợp `_aspect_fix_overview.png`.

| Test | Kết quả |
|---|---|
| Biên dịch | ✔ 0 lỗi (còn 1 cảnh báo cũ `OverlapBoxNonAlloc` trong `TreeCanopyOcclusion`, không liên quan) |
| Helper 576×1024 → 1920×1080 → 576×1024 | ✔ Cả 3 lần trả `true`, kích thước thực tế khớp. Số ô Game View **11 → 11** (không tạo ô trùng) |
| Play ở 576×1024, **không** thao tác tay (scene vẫn lưu Landscape + PPC 480×270) | ✔ `Screen` 576×1024 → **Portrait tự động**, PPC 144×256 ×4, ortho 8, Hero thân = 0.086 chiều cao màn hình. Log: `Applied Portrait (auto 576x1024)` |
| Đổi Game View sang 1920×1080 trong lúc chơi | ✔ **Tự chuyển Landscape**, PPC 480×270 ×4 |
| Toggle (tương đương F8) | ✔ Chuyển tay sang Portrait và giữ nguyên (không bị tự động ghi đè) |
| `ApplyDetected()` | ✔ Quay về chọn theo màn hình |
| Console | ✔ 0 lỗi |

## Lưu ý

- **Scene chiến dịch `GameplayPrototype_Phase4Test` không có `AspectProfileService`**, nên Level 1–4 vẫn chỉ chạy landscape như trước. Bản sửa này chỉ có tác dụng ở Sandbox. Đưa dual-aspect vào Level 1–4 là một bước riêng, cần duyệt (R1 ghi "không rollout trước khi duyệt").
- Joystick ở profile Portrait báo `false`: trong Sandbox không tìm thấy `JoystickPad` đang bật. Đây là hành vi có sẵn của R1, không do bản sửa này. Cần kiểm tra khi làm HUD portrait (R4).
- Game View của Editor đã được trả về 1920×1080, scene chiến dịch đang mở, không có thay đổi chưa lưu.
