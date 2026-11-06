# Cursor: phó bản Trại Goblin, đợt 2

Spec: `Docs/superpowers/specs/2026-11-06-dungeon-ui-v2-cursor-handoff.md` (mục 2–3) và `2026-11-06-goblin-camp-stages-design.md` (mục C).
Art nằm trong `Docs/WorldArt/DungeonStages/Stage1..5`. Mỗi thư mục có một `README.md` ghi cách import.

0. **Commit phần đợt 1 trước.** `DungeonCatalog`, `DungeonSelectPanel`, `DungeonFenceMaze`, `DungeonTalk` và `DungeonSelectRulesTests` hiện còn chưa track. Commit chỉ các file đó, không kèm thay đổi của việc khác.
1. **Test (đang BLOCKED).** Mở Editor với project Game01 rồi sao lưu PlayerPrefs qua Unity eval. Chạy `DungeonSelectRulesTests`. Play lần lượt 5 ải và kiểm:
   - mỗi ải ra đúng layout;
   - banner `ẢI N`;
   - ribbon `ĐỢT n/N`;
   - đồng hồ đổi màu khi quá mốc;
   - kỷ lục `dg.best.*` được ghi;
   - nút Ải tiếp / Về làng hoạt động.

   Xong thì khôi phục PlayerPrefs. Báo kết quả theo VERIFIED / FAILED / NOT_RUN / BLOCKED.
2. **Import art** vào `Assets/Art/Dungeon/StageN/` theo đúng README của từng ải: PPU 16, Point, không nén, grid và border như README ghi. Copy file PNG gốc từ `Docs/WorldArt/DungeonStages/`.
3. **Ải 2 `FenceMaze`:**
   - thay ô vuông màu bằng `Stage2_StoneWall_Autotile_4x4.png`. Mask hàng xóm N=1, E=2, S=4, W=8; ô nằm ở cột `mask % 4`, hàng `mask / 4`;
   - giữ collider 1 ô;
   - cổng `Stage2_FenceGate_2f` đặt trên đoạn tường ngang, mở sau đợt cuối;
   - 2 ngõ cụt có rương (+30 Vàng mỗi rương, mỗi lượt mở 1 lần) và cọc nhọn (mất 5% HP, hồi chiêu 1 s);
   - minimap phủ sương, có dấu ✕ ngõ cụt và la bàn chỉ cổng.
4. **UI riêng các ải còn lại:**
   - ải 1: decal đất đỏ, mũi tên chỉ đường, icon cổng ở mép màn;
   - ải 3: mũi tên cảnh báo vây 1.5 s trước khi quái ra, vòng vây khi bị vây từ 3 phía trở lên;
   - ải 4: cầu 3 đoạn, thanh tiến độ cầu thay ObjectivePlate, viền đỏ khi Hero cách mép cầu ≤ 0.5 ô;
   - ải 5: vạch pha trên thanh máu boss (66% / 33%), telegraph vòng nghi lễ, banner Chương hoàn thành.
5. **HUD chung theo spec v2 mục 2:**
   - ô đồng hồ `UI_NamePlate` dưới ribbon tên vùng, có icon sao và mốc 3 sao;
   - nhãn quái nằm sát đầu, thanh máu nhỏ chỉ hiện khi đã mất máu;
   - màu số sát thương: trắng cho đòn thường, vàng cho chí mạng.
6. **Ánh sáng (làm sau cùng):** kiểm tra render pipeline trước. Nếu có 2D Light thì dùng; nếu không, dùng sprite glow additive. Màu tham khảo theo mockup `S1..S5_*_run.png`.
7. **Chờ user xác nhận:** "lần đầu qua ải nhân đôi thưởng" không có trong spec. Tạm giữ, nhưng gom vào 1 hằng số để dễ tắt.
