using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GameplayPrototype
{
    /// <summary>
    /// Housing text that code writes as plain (unaccented) Vietnamese capitals, e.g. "XAY DUNG".
    /// VI gets the tone marks ("XÂY DỰNG"). EN gets real English ("BUILD"). Exact phrases only,
    /// because many plain words are ambiguous on their own (DA = đá / đã, DAT = đất / đặt).
    /// Names such as Goblin, Gobrider and Cyclops stay names.
    /// </summary>
    public static partial class Loc
    {
        static readonly (string plain, string en, string vi)[] AccentVi =
        {
            // Build mode: cards, tabs, header, filters
            ("NHA GO", "Wood house", "NHÀ GỖ"), ("CHUONG GA", "Chicken coop", "CHUỒNG GÀ"), ("KHO THOC", "Granary", "KHO THÓC"), ("XUONG MOC", "Workshop", "XƯỞNG MỘC"),
            ("CONG TRINH", "Build", "CÔNG TRÌNH"), ("TRANG TRI", "Decor", "TRANG TRÍ"), ("GO BO", "Remove", "GỠ BỎ"), ("DAT", "Land", "ĐẤT"),
            ("XAY DUNG", "Build", "XÂY DỰNG"), ("XAY DUNG [ESC]", "Build [Esc]", "XÂY DỰNG [ESC]"), ("XONG", "Done", "XONG"), ("XONG NGAY", "Finish now", "XONG NGAY"),
            ("DONG", "Close", "ĐÓNG"), ("TAT CA", "All", "TẤT CẢ"), ("CAY", "Trees", "CÂY"), ("RAO", "Fences", "RÀO"), ("VAT", "Props", "VẬT"), ("HET", "Out", "HẾT"),
            ("MOI O", "Per tile", "MỖI Ô"), ("MIEN PHI", "Free", "MIỄN PHÍ"), ("XAC NHAN", "Confirm", "XÁC NHẬN"), ("HUY", "Cancel", "HỦY"), ("XOAY", "Rotate", "XOAY"),
            ("TICK", "OK", "ĐẶT"), ("LUI", "Undo", "LÙI"), ("DOI", "Move", "DỜI"), ("CAT", "Store", "CẤT"),
            ("2.0x TOC DO", "2.0x speed", "2.0x TỐC ĐỘ"),
            ("Open XAY DUNG", "Open Build", "Mở Xây dựng"),
            // Build mode: reasons and hints
            ("KHONG DOI", "No change", "KHÔNG ĐỔI"), ("KHONG CO CONG TRINH", "No building", "KHÔNG CÓ CÔNG TRÌNH"), ("NHA GO TREN MONG", "The wood house stays on its foundation", "NHÀ GỖ TRÊN MÓNG"),
            ("DA DOI", "Already changed", "ĐÃ ĐỔI"), ("CHON CONG TRINH", "Pick a building", "CHỌN CÔNG TRÌNH"), ("THIEU XU", "Not enough coins", "THIẾU XU"), ("DA CO", "Already placed", "ĐÃ CÓ"),
            ("DON TRUOC", "Clear it first", "DỌN TRƯỚC"), ("VUONG DO KHAC", "Something is in the way", "VƯỚNG ĐỒ KHÁC"), ("VE DUONG DAT TRUOC", "Draw a path first", "VẼ ĐƯỜNG ĐẤT TRƯỚC"),
            ("CAN NEN DAT", "Need soil", "CẦN NỀN ĐẤT"), ("CAN MO RONG DAT", "Expand the land first", "CẦN MỞ RỘNG ĐẤT"), ("DA CO NUOC", "Water is already there", "ĐÃ CÓ NƯỚC"),
            ("DA VE", "Already drawn", "ĐÃ VẼ"), ("SUONG MU", "Fog", "SƯƠNG MÙ"), ("VUONG DUONG", "Path in the way", "VƯỚNG ĐƯỜNG"), ("VUONG NHA", "House in the way", "VƯỚNG NHÀ"),
            ("VUONG NUOC", "Water in the way", "VƯỚNG NƯỚC"), ("CHON DO", "Pick an item", "CHỌN ĐỒ"), ("KHONG CO DO", "Nothing there", "KHÔNG CÓ ĐỒ"),
            ("KIEM GO, DA O KHU TAI NGUYEN", "Get wood and ore in the Resource Field", "KIẾM GỖ, ĐÁ Ở KHU TÀI NGUYÊN"),
            ("CHAM V DE XAY NHA", "Tap V to build a house", "CHẠM V ĐỂ XÂY NHÀ"), ("NHAP V / E DE XAY NHA", "Press V or E to build a house", "NHẤP V / E ĐỂ XÂY NHÀ"),
            ("CHAM / KEO DE DOI CHO", "Tap or drag to move", "CHẠM / KÉO ĐỂ ĐỔI CHỖ"), ("NHAP / E DAT   R XOAY   ESC HUY", "Press E to place   R rotate   Esc cancel", "NHẤP / E ĐẶT   R XOAY   ESC HỦY"),
            ("CHAM O DE DAO", "Tap a tile to dig", "CHẠM Ô ĐỂ ĐÀO"), ("NHAP O DE DAO   ESC HUY", "Press a tile to dig   Esc cancel", "NHẤP Ô ĐỂ ĐÀO   ESC HỦY"),
            ("KEO MOT NET   2 NGON DE NHIN", "Drag a stroke   pinch to look", "KÉO MỘT NÉT   2 NGÓN ĐỂ NHÌN"), ("GIU CHUOT TRAI VE   Z LUI", "Hold left mouse to draw   Z undo", "GIỮ CHUỘT TRÁI VẼ   Z LÙI"),
            ("CHAM CHO TRONG DE BO CHON", "Tap empty ground to deselect", "CHẠM CHỖ TRỐNG ĐỂ BỎ CHỌN"), ("NHAP CHO TRONG DE BO CHON", "Press empty ground to deselect", "NHẤP CHỖ TRỐNG ĐỂ BỎ CHỌN"),
            ("THA TAY TREN DO DE CAT", "Release on an item to store it", "THẢ TAY TRÊN ĐỒ ĐỂ CẤT"), ("NHAP DO DE GO   ESC HUY", "Press an item to remove   Esc cancel", "NHẤP ĐỒ ĐỂ GỠ   ESC HỦY"),
            ("KEO DE NHIN   CHAM DO DE CHON", "Drag to look   tap an item to select", "KÉO ĐỂ NHÌN   CHẠM ĐỒ ĐỂ CHỌN"),
            ("NHAP DO DE CHON   CHUOT PHAI KEO", "Press an item to select   right-drag", "NHẤP ĐỒ ĐỂ CHỌN   CHUỘT PHẢI KÉO"),
            ("DANG DOI   TICK DAT   HUY TRA LAI", "Moving   OK to place   Cancel to put back", "ĐANG DỜI   ĐẶT   HỦY TRẢ LẠI"),
            ("DA CHON   X DE GO", "Selected   X to remove", "ĐÃ CHỌN   X ĐỂ GỠ"),
            ("CHAM DE DANH DAU", "Tap to mark", "CHẠM ĐỂ ĐÁNH DẤU"),
            ("KEO VUNG   TICK DAT", "Drag an area   OK to place", "KÉO VÙNG   ĐẶT"),
            ("CHAM O   TICK ROI KEO", "Tap a tile   OK then drag", "CHẠM Ô   ĐẶT RỒI KÉO"),
            ("KEO MOT NET   LUI DE TRA", "Drag a stroke   Undo to refund", "KÉO MỘT NÉT   LÙI ĐỂ TRẢ"),
            ("CONG TRINH   DOI   XONG", "Build   Move   Done", "CÔNG TRÌNH   DỜI   XONG"),
            ("DOI   CAT   XONG", "Move   Store   Done", "DỜI   CẤT   XONG"),
            // Land brushes and decor names
            ("DUONG DAT", "Dirt path", "ĐƯỜNG ĐẤT"), ("LUONG RAU", "Crop bed", "LUỐNG RAU"), ("DAO AO", "Dig pond", "ĐÀO AO"), ("LAP CO", "Fill grass", "LẤP CỎ"), ("VUON RAU", "Vegetable garden", "VƯỜN RAU"),
            ("COT DEN", "Lamp post", "CỘT ĐÈN"), ("HOP THU", "Mailbox", "HỘP THƯ"), ("CAY LA", "Leaf tree", "CÂY LÁ"), ("CAY XANH", "Green tree", "CÂY XANH"), ("CAY RUNG", "Forest tree", "CÂY RỪNG"),
            ("CAY NON LA", "Leaf sapling", "CÂY NON LÁ"), ("CAY NON XANH", "Green sapling", "CÂY NON XANH"), ("BU NHIN", "Scarecrow", "BÙ NHÌN"), ("BUI LA", "Leaf bush", "BỤI LÁ"),
            ("BUI XANH", "Green bush", "BỤI XANH"), ("CAU DA", "Stone bridge", "CẦU ĐÁ"), ("CHAU CAY", "Potted plant", "CHẬU CÂY"), ("CONG VUON", "Garden gate", "CỔNG VƯỜN"), ("DA LAT", "Paving", "ĐÁ LÁT"),
            ("DEN LONG", "Lantern", "ĐÈN LỒNG"), ("GOC CAY", "Tree stump", "GỐC CÂY"), ("HANG RAO", "Fence", "HÀNG RÀO"), ("HANG RAO TRANG", "White fence", "HÀNG RÀO TRẮNG"),
            ("KE HOA", "Flower shelf", "KỆ HOA"), ("RAO THAP", "Low fence", "RÀO THẤP"), ("THAM HOA", "Flower bed", "THẢM HOA"), ("TUONG", "Statue", "TƯỢNG"), ("XICH DU", "Swing", "XÍCH ĐU"),
            ("DA", "Ore", "ĐÁ"),
            // Expand sign
            ("MO RONG", "Expand", "MỞ RỘNG"), ("DA MO", "Already open", "ĐÃ MỞ"), ("DAT MOI: 4 PHIA", "New land: 4 sides", "ĐẤT MỚI: 4 PHÍA"), ("THIEU GO", "Need wood", "THIẾU GỖ"), ("THIEU DA", "Need ore", "THIẾU ĐÁ"),
            ("DU O", "No plots left", "ĐỦ Ô"),
            ("PHIA TAY", "West side", "PHÍA TÂY"), ("PHIA DONG", "East side", "PHÍA ĐÔNG"), ("PHIA NAM", "South side", "PHÍA NAM"), ("PHIA BAC", "North side", "PHÍA BẮC"),
            ("PHIA NAM TAY", "Southwest", "PHÍA TÂY NAM"), ("PHIA NAM DONG", "Southeast", "PHÍA ĐÔNG NAM"),
            ("PHIA BAC TAY", "Northwest", "PHÍA TÂY BẮC"), ("PHIA BAC DONG", "Northeast", "PHÍA ĐÔNG BẮC"),
            // Farm
            ("NONG TRAI", "Farm", "NÔNG TRẠI"), ("NONG TRAI [ESC]", "Farm [Esc]", "NÔNG TRẠI [ESC]"), ("GIEO", "Sow", "GIEO"),
            // Mailbox. Place names that are roles stay translated; villager names are not in this list.
            ("DOC THU", "Read mail", "ĐỌC THƯ"), ("DA DOC", "Read", "ĐÃ ĐỌC"), ("MOI", "New", "MỚI"), ("CO QUA", "Gift", "CÓ QUÀ"), ("DA NHAN", "Claimed", "ĐÃ NHẬN"), ("NHAN", "Claim", "NHẬN"),
            ("KHONG CO QUA", "No gift", "KHÔNG CÓ QUÀ"), ("CHUA XONG NHIEM VU", "Quest not done", "CHƯA XONG NHIỆM VỤ"), ("KHONG CO THU", "No mail", "KHÔNG CÓ THƯ"),
            ("THUONG:", "Reward:", "THƯỞNG:"), ("LANG", "Village", "LÀNG"), ("BIA RUNG", "Forest edge", "BÌA RỪNG"), ("LO REN", "Forge", "LÒ RÈN"), ("CONG LANG", "Village gate", "CỔNG LÀNG"),
            ("GAC CONG", "Gate watch", "GÁC CỔNG"), ("CHO", "Market", "CHỢ"),
            ("CHAO MUNG VE NHA!", "Welcome home!", "CHÀO MỪNG VỀ NHÀ!"), ("VUON DA SAN SANG.", "The garden is ready.", "VƯỜN ĐÃ SẴN SÀNG."), ("GUI BAN IT HAT GIONG.", "Here are a few seeds.", "GỬI BẠN ÍT HẠT GIỐNG."),
            ("BAN GIUP TOI NHE?", "Will you help me?", "BẠN GIÚP TÔI NHÉ?"), ("TOI SE TRA ON.", "I will repay you.", "TÔI SẼ TRẢ ƠN."), ("CHAO HANG XOM!", "Hello, neighbor!", "CHÀO HÀNG XÓM!"),
            ("TANG BAN IT QUA NHO.", "A small gift for you.", "TẶNG BẠN ÍT QUÀ NHỎ."), ("CHUC MUA MANG TOT.", "Have a good harvest.", "CHÚC MÙA MÀNG TỐT."),
            ("MANG 3 BAP CAI", "Bring 3 cabbages", "MANG 3 BẮP CẢI"), ("MANG 5 CA ROT", "Bring 5 carrots", "MANG 5 CÀ RỐT"), ("MANG 3 TRUNG GA", "Bring 3 eggs", "MANG 3 TRỨNG GÀ"),
            ("MANG 2 TRUNG VIT", "Bring 2 duck eggs", "MANG 2 TRỨNG VỊT"),
            // Combat: keep Goblin, Gobrider and Cyclops as names. VI only translates the role.
            ("Goblin Archer", "Goblin Archer", "Goblin Cung Thủ"), ("Elite Gobrider", "Elite Gobrider", "Gobrider Tinh Anh"), ("Gobrider Warlord", "Gobrider Warlord", "Thủ Lĩnh Gobrider"),
            ("Cyclops is in the arena", "Cyclops is in the arena", "Cyclops đang ở đấu trường"), ("Spawn failed", "Spawn failed", "Không gọi được quái"),
            ("XOA", "Erase", "XÓA"), ("EXP PHIEN", "Session EXP", "EXP PHIÊN"), ("< VE LANG", "< To village", "< VỀ LÀNG"),
        };

        /// <summary>Plain-capital lines that carry numbers or names.</summary>
        static IEnumerable<(Regex, Func<Match, string>)> AccentPatterns() => new[]
        {
            // "THIEU 3 GO 2 DA" / "THIEU 20 XU"
            P(@"^THIEU((?: \d+ (?:GO|DA|XU))+)$", m => "THIẾU" + m.Groups[1].Value.Replace(" GO", " GỖ").Replace(" DA", " ĐÁ")),
            P(@"^DANG XAY (\d+)%$", m => $"ĐANG XÂY {G(m, 1)}%"),
            P(@"^DANG XAY (.+)$", m => "ĐANG XÂY " + W(m, 1)),
            P(@"^CAN CAP (\d+)$", m => $"CẦN CẤP {G(m, 1)}"),
            P(@"^XAC NHAN (\d+) XU$", m => $"XÁC NHẬN {G(m, 1)} XU"),
            P(@"^XAC NHAN (.+)  (\d+) XU$", m => "XÁC NHẬN " + W(m, 1) + "  " + G(m, 2) + " XU"),
            P(@"^XONG NGAY (\d+) XU$", m => $"XONG NGAY {G(m, 1)} XU"),
            P(@"^XAC NHAN DAO  DA (\d+)$", m => $"XÁC NHẬN ĐÀO  ĐÁ {G(m, 1)}"),
            P(@"^VUNG (\d+) O  -(\d+) DA   TICK$", m => $"VÙNG {G(m, 1)} Ô  -{G(m, 2)} ĐÁ   ĐẶT"),
            P(@"^DANG CAM (.+)   TICK DAT$", m => "ĐANG CẦM " + W(m, 1) + "   ĐẶT"),
            P(@"^HET (.+)$", m => "HẾT " + W(m, 1)),
            P(@"^DEP \+(.+)$", m => "ĐẸP +" + G(m, 1)),
            P(@"^MO RONG 4 PHIA  GO (\d+)/(\d+)  DA (\d+)/(\d+)$", m => $"MỞ RỘNG 4 PHÍA  GỖ {G(m, 1)}/{G(m, 2)}  ĐÁ {G(m, 3)}/{G(m, 4)}"),
            P(@"^XAC NHAN MO 4 PHIA  GO (\d+)  DA (\d+)(.*)$", m => $"XÁC NHẬN MỞ 4 PHÍA  GỖ {G(m, 1)}  ĐÁ {G(m, 2)}{G(m, 3)}"),
            P(@"^TU: (.+) - (.+)$", m => "TỪ: " + W(m, 1) + " - " + W(m, 2)),
            P(@"^NHIEM VU: (.+)$", m => "NHIỆM VỤ: " + W(m, 1)),
            P(@"^DA DAT (.+)$", m => "ĐÃ ĐẶT " + W(m, 1)),
            P(@"^DA CAT (.+)$", m => "ĐÃ CẤT " + W(m, 1)),
            P(@"^DA GO (.+)$", m => "ĐÃ GỠ " + W(m, 1)),
            P(@"^HOAN THANH: (.+)$", m => "HOÀN THÀNH: " + W(m, 1)),
            P(@"^GO (.+)\? HOAN GO (\d+) DA (\d+)$", m => "GỠ " + W(m, 1) + "? HOÀN GỖ " + G(m, 2) + " ĐÁ " + G(m, 3)),
            P(@"^QUAI (\d+)/(\d+)$", m => $"QUÁI {G(m, 1)}/{G(m, 2)}"),
            P(@"^HOI (\d+)S$", m => $"HỒI {G(m, 1)}S"),
            P(@"^(.+)   E XAC NHAN   ESC HUY$", m => W(m, 1) + "   E XÁC NHẬN   ESC HỦY"),
            P(@"^(.+): chua co bo ky nang$", m => G(m, 1) + ": chưa có bộ kỹ năng"),
            // "Goblin Archer Lv.3": translate the name, keep the level tag
            P(@"^(.+) (Lv\.\d+)$", m => W(m, 1) + " " + G(m, 2)),
        };

        /// <summary>Same plain lines, in English. Proper names are not rewritten.</summary>
        static IEnumerable<(Regex, Func<Match, string>)> EnAccentPatterns() => new[]
        {
            P(@"^THIEU((?: \d+ (?:GO|DA|XU))+)$", m => "Need" + m.Groups[1].Value.Replace(" GO", " wood").Replace(" DA", " ore").Replace(" XU", " coin")),
            P(@"^DANG XAY (\d+)%$", m => $"Building {G(m, 1)}%"),
            P(@"^DANG XAY (.+)$", m => "Building " + EnglishWord(G(m, 1))),
            P(@"^CAN CAP (\d+)$", m => $"Need level {G(m, 1)}"),
            P(@"^XAC NHAN (\d+) XU$", m => $"Confirm {G(m, 1)} coins"),
            P(@"^XAC NHAN (.+)  (\d+) XU$", m => "Confirm " + EnglishWord(G(m, 1)) + "  " + G(m, 2) + " coins"),
            P(@"^XONG NGAY (\d+) XU$", m => $"Finish now {G(m, 1)} coins"),
            P(@"^XAC NHAN DAO  DA (\d+)$", m => $"Confirm dig  {G(m, 1)} ore"),
            P(@"^VUNG (\d+) O  -(\d+) DA   TICK$", m => $"Area {G(m, 1)} tiles  -{G(m, 2)} ore   OK"),
            P(@"^DANG CAM (.+)   TICK DAT$", m => "Holding " + EnglishWord(G(m, 1)) + "   OK to place"),
            P(@"^HET (.+)$", m => "Out of " + EnglishWord(G(m, 1))),
            P(@"^DEP \+(.+)$", m => "Nice +" + G(m, 1)),
            P(@"^MO RONG 4 PHIA  GO (\d+)/(\d+)  DA (\d+)/(\d+)$", m => $"Expand 4 sides  wood {G(m, 1)}/{G(m, 2)}  ore {G(m, 3)}/{G(m, 4)}"),
            P(@"^XAC NHAN MO 4 PHIA  GO (\d+)  DA (\d+)(.*)$", m => $"Confirm expand 4 sides  wood {G(m, 1)}  ore {G(m, 2)}{G(m, 3)}"),
            P(@"^TU: (.+) - (.+)$", m => "From: " + EnglishWord(G(m, 1)) + " - " + EnglishWord(G(m, 2))),
            P(@"^NHIEM VU: (.+)$", m => "Quest: " + EnglishWord(G(m, 1))),
            P(@"^DA DAT (.+)$", m => "Placed " + EnglishWord(G(m, 1))),
            P(@"^DA CAT (.+)$", m => "Stored " + EnglishWord(G(m, 1))),
            P(@"^DA GO (.+)$", m => "Removed " + EnglishWord(G(m, 1))),
            P(@"^HOAN THANH: (.+)$", m => "Done: " + EnglishWord(G(m, 1))),
            P(@"^GO (.+)\? HOAN GO (\d+) DA (\d+)$", m => "Remove " + EnglishWord(G(m, 1)) + "? Refund " + G(m, 2) + " wood " + G(m, 3) + " ore"),
            P(@"^QUAI (\d+)/(\d+)$", m => $"Foes {G(m, 1)}/{G(m, 2)}"),
            P(@"^HOI (\d+)S$", m => $"Respawn {G(m, 1)}s"),
            P(@"^(.+)   E XAC NHAN   ESC HUY$", m => EnglishWord(G(m, 1)) + "   E confirm   Esc cancel"),
            P(@"^(.+): chua co bo ky nang$", m => G(m, 1) + ": no skill kit yet"),
        };
    }
}
