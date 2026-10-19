# Prompt Canva — R1 Batch 01 Art Samples only
Thiết kế BỘ ASSET MẪU 2D TOP-DOWN PIXEL ART FANTASY ACTION RPG dựa trên 3 frame reference đã duyệt và `ASSET_MANIFEST_R1.csv` (kích thước/PPU/pivot thực tế do Claude+Cursor chốt). Đây là asset components, không phải scene mockup hay concept poster.

A) FOREST GROUND / DIRT TRANSITIONS: tiles cỏ mịn, độ nhiễu nhẹ, nhiều biến thể tự nhiên, đường đất nâu be có mép pixel organic, đủ edges/corners/inside corners, cỏ/hoa đá nhỏ rải tiết chế; không checkerboard, không diagonal stripe pattern, không seam. Grid 16×16 chỉ dùng khi đã được audit xác nhận.
B) TREE: 1 cây top-down hợp palette ref; export 3 thành phần riêng PNG transparent: TRUNK, CANOPY, GROUND_SHADOW; pivot alignment nhất quán, canopy đủ rõ để runtime fade alpha tới 0.4 mà Hero vẫn thấy được. Không vẽ background cỏ cố định bên dưới.
C) SWORD SLASH: spritesheet/frame sequence crescent xanh lam và trắng; có anticipation, active sweep, fade và impact nếu brief yêu cầu; alpha transparent và frame alignment/pivot đúng specification. Không blur, glow gradient mềm, anti-alias bán trong suốt ngoài pixel-art palette được duyệt.

REQUIRED: PNG từng tile/frame/layer, filenames theo ASSET_MANIFEST, nearest-neighbor pixel crisp, no text, no watermark, no fake UI, transparent đúng nơi cần. Tạo trước 1 preview sheet để duyệt style; sau duyệt mới export toàn bộ components. Canva không đảm bảo pixel-perfect spritesheet: giao PNG preview/source để kỹ thuật viên pixel-cleanup/validation trước khi Unity import. Không sản xuất hàng trăm assets hoặc sao chép nguyên sprite bên thứ ba.
