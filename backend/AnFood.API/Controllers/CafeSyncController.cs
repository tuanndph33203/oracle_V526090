using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AnFood.Persistence;
using AnFood.Domain.Datatables;
using AnFood.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnFood.API.Controllers
{
    [Route("api/cafe-sync")]
    [ApiController]
    public class CafeSyncController : ControllerBase
    {
        private readonly IUnitOfWork _unit;

        public CafeSyncController(IUnitOfWork unit)
        {
            _unit = unit;
        }

        [HttpPost("full-data")]
        [HttpGet("full-data")]
        public async Task<IActionResult> SyncFullCafeData()
        {
            try
            {
                var branchRepo = _unit.GetRepository<Branchs>();
                var floorRepo = _unit.GetRepository<FloorPlan>();
                var unitRepo = _unit.GetRepository<Unit>();
                var taxRepo = _unit.GetRepository<TaxRate>();
                var groupRepo = _unit.GetRepository<MenuGroup>();
                var itemRepo = _unit.GetRepository<MenuItem>();
                var itemGroupRepo = _unit.GetRepository<MenuItemGroup>();
                var itemBranchRepo = _unit.GetRepository<MenuItemBranch>();

                // 1. Cập nhật tên các chi nhánh thành Quán Café UTC
                var b02 = await branchRepo.Get(x => x.Code == "02").FirstOrDefaultAsync();
                if (b02 != null)
                {
                    b02.Name = "Quán Café UTC – Cơ sở 1 (Cầu Giấy)";
                    b02.Address = "97 Triều Khúc, Thanh Xuân, Hà Nội (ĐH GTVT)";
                    b02.Email = "cafe.utc.cs1@utc.edu.vn";
                    b02.Status = 1;
                    branchRepo.Update(b02);
                }

                var b17 = await branchRepo.Get(x => x.Code == "17A").FirstOrDefaultAsync();
                if (b17 != null)
                {
                    b17.Name = "Quán Café UTC – Cơ sở 2 (Xuân Thủy)";
                    b17.Address = "136 Xuân Thủy, Cầu Giấy, Hà Nội (ĐH GTVT)";
                    b17.Email = "cafe.utc.cs2@utc.edu.vn";
                    b17.Status = 1;
                    branchRepo.Update(b17);
                }

                // Cập nhật tên mặt sàn
                var f02 = await floorRepo.Get(x => x.Code == "TANG1" || x.Code == "Tang1").ToListAsync();
                foreach (var f in f02)
                {
                    if (b02 != null && f.BranchId == b02.Id)
                    {
                        f.Name = "Tầng 1 - Quầy Bar & Phòng Lạnh";
                        floorRepo.Update(f);
                    }
                    else if (b17 != null && f.BranchId == b17.Id)
                    {
                        f.Name = "Tầng 1 - Khu Sân Vườn Thoáng Mát";
                        floorRepo.Update(f);
                    }
                }

                // 2. Đơn vị tính (Units)
                var unitLy = await unitRepo.Get(x => x.Code == "LY" || x.Name.ToLower().Contains("ly")).FirstOrDefaultAsync();
                if (unitLy == null)
                {
                    unitLy = new Unit
                    {
                        Id = Guid.NewGuid(),
                        Code = "LY",
                        Name = "Ly",
                        Status = 1,
                        CreatedOnDate = DateTime.UtcNow
                    };
                    unitRepo.Add(unitLy);
                }

                var unitDia = await unitRepo.Get(x => x.Code == "DIA" || x.Name.ToLower().Contains("đĩa") || x.Name.ToLower().Contains("dia")).FirstOrDefaultAsync();
                if (unitDia == null)
                {
                    unitDia = new Unit
                    {
                        Id = Guid.NewGuid(),
                        Code = "DIA",
                        Name = "Đĩa",
                        Status = 1,
                        CreatedOnDate = DateTime.UtcNow
                    };
                    unitRepo.Add(unitDia);
                }

                var unitPhan = await unitRepo.Get(x => x.Code == "PHAN" || x.Name.ToLower().Contains("phần") || x.Name.ToLower().Contains("suất")).FirstOrDefaultAsync();

                // 3. Thuế suất (TaxRate)
                var taxRate = await taxRepo.Get(x => x.Rate == 8 || x.Rate == 10).FirstOrDefaultAsync();
                var taxRateId = taxRate?.Id ?? Guid.Parse("d240090e-6d53-42b9-a7c8-9e1a2dffc418");
                var taxRatePercent = taxRate?.Rate ?? 8;

                // 4. Nhóm thực đơn Quán Café (MenuGroups)
                var groupsDef = new (string Code, string Name, int Priority, string Desc)[]
                {
                    ("CF_TRAD", "Cà phê Truyền thống", 1, "Cà phê Robusta nguyên chất Đắk Lắk, hương vị đậm đà truyền thống"),
                    ("CF_MACH", "Cà phê Pha máy & Cold Brew", 2, "Espresso, Americano, Latte, Cappuccino chuẩn vị Ý & Cold Brew ủ lạnh"),
                    ("TEA_FRUIT", "Trà Trái Cây & Trà Thảo Mộc", 3, "Trà đào, trà vải, trà hoa quả tươi thanh nhiệt sảng khoái"),
                    ("MILK_TEA", "Trà Sữa & Matcha", 4, "Trà sữa Ô long nướng, trà sữa hoàng gia và matcha Nhật Bản thơm béo"),
                    ("SMOOTHIE", "Sinh Tố & Nước Ép Tươi", 5, "Nước ép nguyên chất từ trái cây tươi 100%, dồi dào vitamin"),
                    ("BAKERY", "Bánh Ngọt & Đồ Ăn Kèm", 6, "Croissant, Tiramisu, Cheesecake và đồ ăn vặt hấp dẫn"),
                    ("TOPPING", "Topping & Thêm", 7, "Các loại trân châu, thạch, kem cheese macchiato thêm vào đồ uống")
                };

                var createdGroups = new Dictionary<string, MenuGroup>();
                foreach (var gDef in groupsDef)
                {
                    var existing = await groupRepo.Get(x => x.Code == gDef.Code).FirstOrDefaultAsync();
                    if (existing == null)
                    {
                        existing = new MenuGroup
                        {
                            Id = Guid.NewGuid(),
                            Code = gDef.Code,
                            Name = gDef.Name,
                            Priority = gDef.Priority,
                            Description = gDef.Desc,
                            Status = 1,
                            Type = 1,
                            CreatedOnDate = DateTime.UtcNow,
                            CreatedByUserCode = "ADMIN",
                            CreatedByUserName = "Quản trị viên"
                        };
                        groupRepo.Add(existing);
                    }
                    else
                    {
                        existing.Name = gDef.Name;
                        existing.Priority = gDef.Priority;
                        existing.Description = gDef.Desc;
                        existing.Status = 1;
                        groupRepo.Update(existing);
                    }
                    createdGroups[gDef.Code] = existing;
                }

                // Cấu hình Option Groups chuẩn cho đồ uống cà phê & trà
                var drinkOptionGroups = new object[]
                {
                    new
                    {
                        Code = "SUGAR",
                        Name = "Mức đường",
                        Required = true,
                        Multiple = false,
                        Values = new object[]
                        {
                            new { Code = "100", Name = "100% đường (Chuẩn)", ExtraCharge = 0, IsDefault = true },
                            new { Code = "70", Name = "70% đường", ExtraCharge = 0, IsDefault = false },
                            new { Code = "50", Name = "50% đường", ExtraCharge = 0, IsDefault = false },
                            new { Code = "30", Name = "30% đường", ExtraCharge = 0, IsDefault = false },
                            new { Code = "0", Name = "Không đường", ExtraCharge = 0, IsDefault = false }
                        }
                    },
                    new
                    {
                        Code = "ICE",
                        Name = "Mức đá",
                        Required = true,
                        Multiple = false,
                        Values = new object[]
                        {
                            new { Code = "100", Name = "100% đá (Chuẩn)", ExtraCharge = 0, IsDefault = true },
                            new { Code = "70", Name = "70% đá", ExtraCharge = 0, IsDefault = false },
                            new { Code = "50", Name = "50% đá", ExtraCharge = 0, IsDefault = false },
                            new { Code = "0", Name = "Không đá", ExtraCharge = 0, IsDefault = false },
                            new { Code = "HOT", Name = "Uống nóng", ExtraCharge = 0, IsDefault = false }
                        }
                    },
                    new
                    {
                        Code = "SIZE",
                        Name = "Kích cỡ",
                        Required = true,
                        Multiple = false,
                        Values = new object[]
                        {
                            new { Code = "M", Name = "Size M (Chuẩn)", ExtraCharge = 0, IsDefault = true },
                            new { Code = "L", Name = "Size L (+8.000đ)", ExtraCharge = 8000, IsDefault = false }
                        }
                    },
                    new
                    {
                        Code = "TOPPING",
                        Name = "Thêm Topping",
                        Required = false,
                        Multiple = true,
                        Values = new object[]
                        {
                            new { Code = "TC_DEN", Name = "Trân châu đen hoàng kim", ExtraCharge = 5000, IsDefault = false },
                            new { Code = "TC_TRANG", Name = "Trân châu trắng 3Q", ExtraCharge = 5000, IsDefault = false },
                            new { Code = "KEM_CHEESE", Name = "Kem cheese Macchiato", ExtraCharge = 10000, IsDefault = false },
                            new { Code = "NHA_DAM", Name = "Thạch nha đam", ExtraCharge = 5000, IsDefault = false },
                            new { Code = "THACH_DAO", Name = "Thạch đào giòn", ExtraCharge = 6000, IsDefault = false }
                        }
                    }
                };

                var drinkOptionJson = JsonSerializer.Serialize(drinkOptionGroups);
                var emptyOptionJson = "[]";

                // 5. Danh sách các món Café UTC đầy đủ
                var products = new (string Code, string Name, string GroupCode, decimal Price, bool HasOptions, bool MustTry, string Desc)[]
                {
                    // Cà phê truyền thống
                    ("CF01", "Cà phê đen đá", "CF_TRAD", 25000, true, true, "Cà phê Robusta Đắk Lắk rang mộc, thơm nồng đượm vị truyền thống"),
                    ("CF02", "Cà phê sữa đá", "CF_TRAD", 30000, true, true, "Cà phê Robusta hòa quyện cùng sữa đặc ngọt béo thơm lừng"),
                    ("CF03", "Bạc xỉu ba tầng", "CF_TRAD", 35000, true, true, "Lớp sữa đặc béo ngậy, sữa tươi thanh mát và lớp bọt cà phê thơm dịu"),
                    ("CF04", "Cà phê trứng Hà Nội", "CF_TRAD", 40000, true, true, "Lớp kem trứng đánh bông mịn vàng óng trên nền cà phê nóng đậm vị"),
                    ("CF05", "Cà phê cốt dừa", "CF_TRAD", 42000, true, true, "Cốt dừa béo ngậy xay tuyết mịn cùng cà phê Robusta hảo hạng"),
                    ("CF06", "Cà phê muối xứ Huế", "CF_TRAD", 35000, true, false, "Lớp kem muối mằn mặn béo ngậy tôn lên hương thơm cà phê đậm đà"),

                    // Cà phê Máy & Cold Brew
                    ("MC01", "Espresso Single", "CF_MACH", 30000, false, false, "Chiết xuất chuẩn Ý từ 100% hạt Arabica Cầu Đất thơm nồng"),
                    ("MC02", "Americano đá", "CF_MACH", 35000, true, true, "Espresso pha loãng thanh nhẹ, hậu vị chua thanh ngọt hậu"),
                    ("MC03", "Café Latte", "CF_MACH", 45000, true, true, "Espresso hòa quyện cùng sữa tươi thanh trùng đánh bọt mịn màng"),
                    ("MC04", "Cappuccino Ý", "CF_MACH", 45000, true, true, "Cân bằng tỉ lệ vàng giữa cà phê, sữa nóng và lớp bọt sữa dày mịn"),
                    ("MC05", "Caramel Macchiato", "CF_MACH", 49000, true, true, "Vani, sữa tươi, espresso và sốt caramel hảo hạng thơm lừng"),
                    ("MC06", "Cold Brew Cam Vàng", "CF_MACH", 45000, true, true, "Cà phê ủ lạnh suốt 16 tiếng kết hợp cùng lát cam vàng tươi mát"),

                    // Trà trái cây & Thảo mộc
                    ("TR01", "Trà đào cam sả", "TEA_FRUIT", 38000, true, true, "Trà đen thơm ngát kết hợp cùng đào giòn ngọt và hương sả sảng khoái"),
                    ("TR02", "Trà vải hoa hồng", "TEA_FRUIT", 42000, true, true, "Hương hoa hồng quý phái kết hợp trái vải mọng nước thanh mát"),
                    ("TR03", "Trà sen vàng Macchiato", "TEA_FRUIT", 45000, true, true, "Trà Ô long hảo hạng, hạt sen bùi ngậy cùng lớp kem cheese mặn béo"),
                    ("TR04", "Trà chanh giã tay", "TEA_FRUIT", 32000, true, false, "Chanh tươi Quảng Đông giã tay thơm nồng tinh dầu giải nhiệt tức thì"),
                    ("TR05", "Trà dâu tằm pha lê", "TEA_FRUIT", 39000, true, false, "Vị chua ngọt thanh mát của dâu tằm tự nhiên cùng thạch pha lê giòn"),
                    ("TR06", "Trà hoa cúc mật ong nóng", "TEA_FRUIT", 35000, false, false, "Trà thảo mộc thanh lọc cơ thể, ấm áp và ngọt dịu tự nhiên"),

                    // Trà sữa & Matcha
                    ("TS01", "Trà sữa Ô long nướng", "MILK_TEA", 40000, true, true, "Trà Ô long sao nhiệt đậm đà kết hợp sữa béo ngậy thơm mùi khói đặc trưng"),
                    ("TS02", "Trà sữa Trân châu hoàng gia", "MILK_TEA", 38000, true, true, "Hương vị trà sữa truyền thống thơm ngon kèm trân châu đen dẻo dai"),
                    ("TS03", "Matcha Latte Uji", "MILK_TEA", 45000, true, true, "Bột trà xanh Uji Kyoto Nhật Bản nguyên chất hòa cùng sữa tươi thanh trùng"),
                    ("TS04", "Trà sữa Lài kem cheese", "MILK_TEA", 42000, true, false, "Hương hoa lài thanh tao ngập tràn vị giác cùng kem cheese béo mặn"),

                    // Sinh tố & Nước ép tươi
                    ("ST01", "Sinh tố bơ dừa Đắk Lắk", "SMOOTHIE", 45000, true, true, "Bơ sáp loại 1 dẻo quánh xay cùng nước cốt dừa và sữa đặc"),
                    ("ST02", "Sinh tố xoài chanh leo", "SMOOTHIE", 42000, true, false, "Sự kết hợp chua ngọt sảng khoái dồi dào vitamin và khoáng chất"),
                    ("EP01", "Nước ép cam sành tươi", "SMOOTHIE", 39000, true, true, "100% cam tươi vắt nguyên chất, không thêm đường nước"),
                    ("EP02", "Nước ép dưa hấu bạc hà", "SMOOTHIE", 35000, true, false, "Dưa hấu ngọt mát kết hợp lá bạc hà tươi xay lạnh giải khát tuyệt đỉnh"),
                    ("EP03", "Nước ép táo cà rốt detox", "SMOOTHIE", 42000, true, false, "Công thức detox thanh lọc cơ thể, đẹp da giữ dáng"),

                    // Bánh ngọt & Ăn nhẹ
                    ("BM01", "Bánh Croissant bơ Pháp", "BAKERY", 32000, false, true, "Bánh sừng bò ngàn lớp thơm lừng mùi bơ Pháp cao cấp"),
                    ("BM02", "Bánh Tiramisu truyền thống", "BAKERY", 45000, false, true, "Bánh kem phô mai mascarpone đậm đà hương cà phê espresso và cacao"),
                    ("BM03", "Bánh Cheesecake việt quất", "BAKERY", 48000, false, true, "Phô mai nướng mềm mịn phủ lớp mứt việt quất chua ngọt hài hòa"),
                    ("AN01", "Hạt hướng dương rang mộc", "BAKERY", 15000, false, false, "Món nhâm nhi trò chuyện không thể thiếu bên ly cà phê"),
                    ("AN02", "Bò khô sợi cay vắt chanh", "BAKERY", 35000, false, false, "Bò khô cay ngọt đậm vị nhắm cùng bia hoặc trà giải khát"),

                    // Topping thêm
                    ("TP01", "Trân châu đen hoàng kim", "TOPPING", 5000, false, false, "Trân châu nấu đường đen dẻo dai giòn ngọt"),
                    ("TP02", "Trân châu trắng 3Q", "TOPPING", 5000, false, false, "Trân châu ngọc trai giòn sần sật vui miệng"),
                    ("TP03", "Kem Cheese Macchiato", "TOPPING", 10000, false, true, "Lớp kem cheese mặn béo mịn màng đỉnh cao"),
                    ("TP04", "Thạch nha đam thanh mát", "TOPPING", 5000, false, false, "Nha đam giòn mát ngọt dịu")
                };

                // Thu thập tất cả ID chi nhánh cần link (cả 02 và 17A)
                var branchIds = new List<Guid>();
                if (b02 != null) branchIds.Add(b02.Id);
                if (b17 != null) branchIds.Add(b17.Id);

                int createdCount = 0;
                int updatedCount = 0;

                foreach (var p in products)
                {
                    if (!createdGroups.TryGetValue(p.GroupCode, out var group))
                        continue;

                    var unitToUse = p.GroupCode == "BAKERY" ? (unitDia?.Id ?? unitLy.Id) : unitLy.Id;
                    var optionJson = p.HasOptions ? drinkOptionJson : emptyOptionJson;

                    var existing = await itemRepo.Get(x => x.Code == p.Code).FirstOrDefaultAsync();
                    if (existing == null)
                    {
                        var newItem = new MenuItem
                        {
                            Id = Guid.NewGuid(),
                            Code = p.Code,
                            Name = p.Name,
                            ItemType = 1,
                            CourseOrder = 1,
                            MenuGroupId = group.Id,
                            UnitId = unitToUse,
                            SalePrice = Math.Round(p.Price / (1 + (taxRatePercent / 100m)), 2),
                            TaxRateId = taxRateId,
                            FixedTaxAmount = 0,
                            Amount = p.Price,
                            CostPrice = Math.Round(p.Price * 0.35m, 2),
                            Description = p.Desc,
                            ImageUrl = "",
                            OptionGroupsJson = optionJson,
                            Status = 1,
                            IsAvailable = true,
                            IsMustTry = p.MustTry,
                            IsTrackStock = false,
                            StockQuantity = null,
                            DailyQuota = null,
                            DisabledAt = null
                        };
                        itemRepo.Add(newItem);

                        // Liên kết Group
                        itemGroupRepo.Add(new MenuItemGroup
                        {
                            Id = Guid.NewGuid(),
                            MenuItemId = newItem.Id,
                            MenuGroupId = group.Id
                        });

                        // Liên kết Branches
                        foreach (var bId in branchIds)
                        {
                            itemBranchRepo.Add(new MenuItemBranch
                            {
                                Id = Guid.NewGuid(),
                                MenuItemId = newItem.Id,
                                BranchId = bId
                            });
                        }

                        createdCount++;
                    }
                    else
                    {
                        existing.Name = p.Name;
                        existing.MenuGroupId = group.Id;
                        existing.UnitId = unitToUse;
                        existing.Amount = p.Price;
                        existing.SalePrice = Math.Round(p.Price / (1 + (taxRatePercent / 100m)), 2);
                        existing.TaxRateId = taxRateId;
                        existing.Description = p.Desc;
                        existing.OptionGroupsJson = optionJson;
                        existing.Status = 1;
                        existing.IsAvailable = true;
                        existing.IsMustTry = p.MustTry;
                        itemRepo.Update(existing);

                        // Kiểm tra MenuItemGroup
                        var hasGroup = await itemGroupRepo.Get(x => x.MenuItemId == existing.Id && x.MenuGroupId == group.Id).AnyAsync();
                        if (!hasGroup)
                        {
                            itemGroupRepo.Add(new MenuItemGroup
                            {
                                Id = Guid.NewGuid(),
                                MenuItemId = existing.Id,
                                MenuGroupId = group.Id
                            });
                        }

                        // Kiểm tra MenuItemBranch
                        foreach (var bId in branchIds)
                        {
                            var hasBranch = await itemBranchRepo.Get(x => x.MenuItemId == existing.Id && x.BranchId == bId).AnyAsync();
                            if (!hasBranch)
                            {
                                itemBranchRepo.Add(new MenuItemBranch
                                {
                                    Id = Guid.NewGuid(),
                                    MenuItemId = existing.Id,
                                    BranchId = bId
                                });
                            }
                        }

                        updatedCount++;
                    }
                }

                await _unit.SaveAsync();

                return Ok(new
                {
                    code = 200,
                    message = "Đồng bộ dữ liệu Quán Café UTC thành công!",
                    data = new
                    {
                        branches = new[]
                        {
                            new { code = "02", name = b02?.Name },
                            new { code = "17A", name = b17?.Name }
                        },
                        groupsCount = groupsDef.Length,
                        productsCreated = createdCount,
                        productsUpdated = updatedCount,
                        totalCoffeeProducts = products.Length
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    code = 500,
                    message = "Lỗi khi đồng bộ dữ liệu: " + ex.Message,
                    detail = ex.StackTrace
                });
            }
        }
    }
}
