# R2B — Import Hero_Sheet_R2b vào Unity: báo cáo

**Ngày:** 2026-10-19 · **Nhánh:** `reference_rebuild_r1` · **Chưa commit**

## 1. Import

| Bước | Kết quả |
|---|---|
| Sao lưu | `IncomingAssets/backup_before_R2B/Hero_Sheet.png` + `.meta` + `meta.md5` |
| Ghi đè pixel | `Assets/Art/GoblinCampPhase4/Hero_Sheet.png` ← `Hero_Sheet_R2b.png` |
| `.meta` | **Không đổi** (md5 khớp; thời gian sửa vẫn 10:39) |
| Reimport | 128×288, Point, PPU 16, **33 sprite** giữ tên. `Walk_Side_0` rect (0, 96, 32, 32), pivot (0.500, 0.063) |
| Animator | 53 key sprite trong mọi clip của `Hero.controller`: **0 thiếu** |
| File khác | Chỉ `Hero_Sheet.png` thay đổi trong `Assets/`. Sandbox scene không bị lưu |

## 2. Sandbox 16:9 (1920×1080, PPC 480×270)

Ảnh: `unity_test/L_*.png`, cắt phóng to quanh Hero: `unity_test/_L_hero_crops.png`.

| Kiểm tra | Kết quả |
|---|---|
| Idle | ✔ Sprite mới hiển thị |
| Chạy trái | ✔ `Hero_Walk_Side_1`, `flipX=False` (sprite gốc vẽ hướng trái) |
| Chạy phải | ✔ `flipX=True` |
| Attack Side | ✔ Animator đặt tại 0.09 / 0.13 / 0.20 s cho đúng `Attack_Side_0` / `1` / `2`. Frame strike (khung gây sát thương 0.12 s) là dáng khom sâu + áo xoè |

Để chụp rõ sprite, tôi ẩn quái tạm thời và dàn cảnh frame Attack bằng cách tạm dừng Editor. Chỉ làm trong Play Mode, không lưu.

## 3. Sandbox 9:16 — lần đầu có bằng chứng thật

Ảnh: `unity_test/P_*.png`, tổng hợp: `unity_test/_P_overview.png`.

| Kiểm tra | Kết quả |
|---|---|
| `Screen` khi Play | **576×1024** ✔ |
| PPC | 144×256, pixelRatio ×4, ortho 8 ✔ (đúng giá trị đã chốt) |
| Hero / chiều cao màn hình | Thân ≈ 88 px / 1024 = **0.086** (tham chiếu ≈ 0.072) |
| Hình | Cây và nền đúng tỉ lệ, không còn bị ép hẹp như ảnh R1 |
| Idle / chạy trái / chạy phải / strike | ✔ Sprite đúng, lật đúng |

### Hai lỗi R1 tìm thấy khi làm (chưa sửa — ngoài phạm vi import)

1. **`AspectGameViewHelper.SetSize` chọn sai ô:** chọn ô số 10 (1920×1080) thay vì ô 576×1024. Đây là lý do ảnh portrait R1 thực chất là ảnh ngang bị ép. Tôi đã chọn đúng ô bằng `GameView.SizeSelectionCallback`, sau khi test đã trả Game View về 1920×1080.
2. **`AspectProfileService.Awake()` không tự nhận tỉ lệ màn hình:** luôn áp dụng `activeId` lưu trong scene (Landscape). Khi màn hình là 576×1024, game vẫn chạy PPC 480×270 (ortho 32, Hero bé như hạt). Phải bấm F8 hoặc gọi `Apply(Portrait)` mới đúng. Trên thiết bị dọc thật, lỗi này sẽ xảy ra.
   - **Đề xuất:** trong `Awake`, chọn Portrait khi `Screen.height > Screen.width`, và kiểm tra lại khi độ phân giải thay đổi.

## 4. Test 60 giây (tiêu chí Plan Pack: "0 frame Hero mất thân")

- **Cách làm:** bật lại 6 quái, chạy `SoakTestRunner` (bot di chuyển, giữ R, dùng skill), lấy mẫu trạng thái Hero qua CLI. Ở profile Portrait.
- **Dữ liệu:** `unity_test/hero_visibility_samples.txt`.

| Chỉ số | Kết quả |
|---|---|
| Số mẫu / thời gian game | **120 mẫu**, 86.5 → 147.4 s (**60.9 s**) |
| Mẫu Hero mất thân (renderer tắt, sprite null, inactive, hoặc alpha < 1 ngoài lúc bất tử) | **0 / 120** ✔ |
| State gặp | Walk_Side 72, Walk_Up 21, Walk_Down 18, Attack_Up 7, Attack_Side 2 |
| Console | 0 lỗi, 0 cảnh báo ✔ |

**Giới hạn của test:**
- Bot không bị trúng đòn (HP luôn 400), nên **Hurt và Death chưa được thử** với sheet mới.
- `Attack_Down` và các Idle không xuất hiện trong mẫu (bot luôn di chuyển). Hai loại này đã được xác nhận riêng ở mục 2 và 3 qua ảnh chụp.
- Khoảng cách mẫu không đều (~0.5 s game/mẫu) vì Editor chạy chậm khi không có focus.

## 5. Nhận xét hình ảnh trong game

- Bản mới trông rõ là Hero tóc trắng áo teal giống tham chiếu. Dáng chạy ngả và cú chém khom đọc được ở cả hai tỉ lệ.
- **Kiếm nghỉ placeholder 6×16** (`PlayerVisual`) vẫn vẽ chéo sau lưng khi chạy và chém, trông như một vệt tối. Cần HR04 / HR05 thay thế.
- Frame windup (`HeroExtra/Hero_Windup_Side`) và Hurt/Death của R1 vẫn **palette cũ** (áo tím navy). Có lệch màu ngắn ở đầu mỗi đòn. Cần HR02 / HR03.

## 6. Khôi phục nếu cần

Chép `IncomingAssets/backup_before_R2B/Hero_Sheet.png` đè lại `Assets/Art/GoblinCampPhase4/Hero_Sheet.png`. Không cần đụng `.meta`.
