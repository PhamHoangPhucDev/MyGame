# Prompt Claude — Reference breakdown + R0 technical review
Bạn là Art Director pixel-art + Unity Technical Reviewer. Làm việc READ-ONLY, không đụng code project.

BỘ ĐẦU VÀO: original ZIP 30 clip, 3 contact sheets + clip index, Cursor `PROJECT_AUDIT.md`, `CURRENT_ASSET_INVENTORY.csv`, 12 screenshot baseline game Unity. Tham chiếu video portrait 9:16; project hiện có build landscape 16:9 cần bảo toàn.

1) Chọn canonical clips + timecodes: Ground/tree (04/19/22); Hero/combat (03/06/12); Mob/HP/labels/camp (20/22); optional Boss 18/26 và UI meta14/25. Ghi chính xác timecode sau khi tự xem; không bịa.
2) Tách gameplay art khỏi TikTok caption, watermark, editor overlay, black border.
3) Với mỗi shot: tỷ lệ Hero trên màn, màu cỏ/đất, đường viền đất, số lớp cây, camera, screen-space sprite, slash shapes, damage/hitcount, UI, Mob Label & HP.
4) So với 12 ảnh current game: tạo `REFERENCE_SHOTLIST.csv` và `DIFFERENCE_MATRIX.csv` (P0/P1/P2; clip+timecode; hiện trạng; delta; owner Canva/Cursor; fix; acceptance evidence). Không chấm 16:9 không giống frame 9:16 như là lỗi art: ghi tách Adaptation.
5) Tạo `ASSET_MANIFEST_R1.csv` cho 3 batch mẫu duy nhất: Ground+transition, Tree trunk/canopy, Slash arcs. Có dimensions theo pixel sau audit, grid, PPU, frame count, alpha, pivot, palette, naming và screenshot ref.
6) Rà soát plan dual-aspect đảm bảo giữ shared LevelFlow/save/combat, không ẩn nhãn quái, không cho sword sort độc lập body, không destroy existing Level 1–4.
7) Chấm rubric weighted environment 30%, character 20%, combat 25%, HUD/camera 15%, interaction 10%, dưới dạng baseline uncertain nếu chưa đủ video.
8) Chỉ xuất report, tuyệt đối không nhận đã test Unity nếu không có MCP/Play Mode. Clip 21 hỏng thì ghi NOT REVIEWED.

Đầu ra: `REFERENCE_SHOTLIST.csv`, `DIFFERENCE_MATRIX.csv`, `ASSET_MANIFEST_R1.csv`, `CLAUDE_R0_REVIEW.md`. Chọn 3 reference stills rõ nhất để user phê duyệt. DỪNG trước khi giao Canva.
