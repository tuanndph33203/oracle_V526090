using AnFood.Domain.Datatables;
using AnFood.Persistence;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.ProductBussiness
{
    public class ProductHandler : IProductHandler
    {
        private readonly IUnitOfWork _unit;

        public ProductHandler(IUnitOfWork unit)
        {
            _unit = unit;
        }

        public async Task<Response> Create(CreateProductModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                {
                    return new Response(Code.ServerError, "Code and Name are required.");
                }

                var productRepo = _unit.GetRepository<Product>();
                var existing = await productRepo.FindAsync(p => p.Code == model.Code.Trim());
                if (existing != null)
                {
                    return new Response(Code.ServerError, $"Product code '{model.Code}' already exists.");
                }

                var product = new Product
                {
                    Id = Guid.NewGuid(),
                    Code = model.Code.Trim(),
                    Name = model.Name.Trim(),
                    UnitCode = model.UnitCode ?? "PCS",
                    BarCode = model.BarCode,
                    Origin = model.Origin,
                    PurchasePrice = model.PurchasePrice,
                    SellingPrice = model.SellingPrice,
                    ApplyListBranchCodes = model.ApplyListBranchCodes ?? "00",
                    BranchCode = model.BranchCode ?? "00",
                    Notes = model.Notes,
                    ProductTypeCode = model.ProductTypeCode,
                    ProductGroupCode = model.ProductGroupCode,
                    ImportTaxCode = model.ImportTaxCode,
                    SpecialTaxCode = model.SpecialTaxCode,
                    ExportTaxCode = model.ExportTaxCode,
                    ExportTaxMoney = model.ExportTaxMoney,
                    Status = model.Status,
                    Type = model.Type ?? 0,
                    CreatedOnDate = DateTime.UtcNow,
                    CreatedByUserCode = model.CreatedByUserCode ?? "system",
                    CreatedByUserName = model.CreatedByUserName ?? "system",
                    Description = model.Description,
                    AvatarUrl = model.AvatarUrl
                };

                productRepo.Add(product);
                await _unit.SaveAsync();

                return new Response(Code.Success, "Product created successfully!");
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch Create Product: " + ex.Message);
            }
        }

        public async Task<Response> Update(UpdateProductModel model)
        {
            try
            {
                var productRepo = _unit.GetRepository<Product>();
                var product = await productRepo.FindAsync(x => x.Id == model.Id);
                if (product == null)
                {
                    return new Response(Code.NotFound, "Product not found.");
                }

                product.Name = model.Name.Trim();
                product.UnitCode = model.UnitCode ?? "PCS";
                product.BarCode = model.BarCode;
                product.Origin = model.Origin;
                product.PurchasePrice = model.PurchasePrice;
                product.SellingPrice = model.SellingPrice;
                product.ApplyListBranchCodes = model.ApplyListBranchCodes ?? "00";
                product.BranchCode = model.BranchCode ?? "00";
                product.Notes = model.Notes;
                product.ProductTypeCode = model.ProductTypeCode;
                product.ProductGroupCode = model.ProductGroupCode;
                product.ImportTaxCode = model.ImportTaxCode;
                product.SpecialTaxCode = model.SpecialTaxCode;
                product.ExportTaxCode = model.ExportTaxCode;
                product.ExportTaxMoney = model.ExportTaxMoney;
                product.Status = model.Status;
                product.Type = model.Type ?? 0;
                product.UpdatedOnDate = DateTime.UtcNow;
                product.UpdatedByUserCode = model.UpdatedByUserCode ?? "system";
                product.UpdatedByUserName = model.UpdatedByUserName ?? "system";
                product.Description = model.Description;
                product.AvatarUrl = model.AvatarUrl;

                productRepo.Update(product);
                await _unit.SaveAsync();

                return new Response(Code.Success, "Product updated successfully!");
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch Update Product: " + ex.Message);
            }
        }

        public async Task<Response> Delete(Guid id)
        {
            try
            {
                var productRepo = _unit.GetRepository<Product>();
                var product = await productRepo.FindAsync(x => x.Id == id);
                if (product == null)
                {
                    return new Response(Code.NotFound, "Product not found.");
                }

                // Soft delete
                product.Status = 0;
                product.UpdatedOnDate = DateTime.UtcNow;
                productRepo.Update(product);

                await _unit.SaveAsync();
                return new Response(Code.Success, "Product deleted successfully!");
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch Delete Product: " + ex.Message);
            }
        }

        public async Task<Response> GetByFilter(ProductFilterModel model)
        {
            try
            {
                var productRepo = _unit.GetRepository<Product>();
                var query = await productRepo.GetListAsync(p => p.Status == 1);

                if (!string.IsNullOrEmpty(model.TextSearch))
                {
                    var text = model.TextSearch.Trim().ToLower();
                    query = query.Where(p => p.Code.ToLower().Contains(text) || p.Name.ToLower().Contains(text)).ToList();
                }

                if (model.Type.HasValue)
                {
                    query = query.Where(p => p.Type == model.Type.Value).ToList();
                }

                if (!string.IsNullOrEmpty(model.BranchCode))
                {
                    query = query.Where(p => p.BranchCode == model.BranchCode || p.ApplyListBranchCodes.Contains(model.BranchCode)).ToList();
                }

                var totalCount = query.Count();
                var page = model.Page ?? 1;
                var size = model.Size ?? 20;
                var pagedQuery = query.Skip((page - 1) * size).Take(size).ToList();

                var productGroupRepo = _unit.GetRepository<ProductGroup>();
                var unitRepo = _unit.GetRepository<Unit>();

                var datas = new List<ProductDetailModel>();
                foreach (var item in pagedQuery)
                {
                    var group = string.IsNullOrEmpty(item.ProductGroupCode) ? null : 
                        (await productGroupRepo.GetListAsync(g => g.Code == item.ProductGroupCode)).FirstOrDefault();
                    
                    var unit = string.IsNullOrEmpty(item.UnitCode) ? null : 
                        (await unitRepo.GetListAsync(u => u.Code == item.UnitCode)).FirstOrDefault();

                    datas.Add(new ProductDetailModel
                    {
                        Id = item.Id,
                        Code = item.Code,
                        Name = item.Name,
                        BarCode = item.BarCode,
                        Origin = item.Origin,
                        PurchasePrice = item.PurchasePrice,
                        SellingPrice = item.SellingPrice,
                        ApplyListBranchCodesString = item.ApplyListBranchCodes,
                        Notes = item.Notes,
                        ImportTaxCode = item.ImportTaxCode,
                        SpecialTaxCode = item.SpecialTaxCode,
                        ExportTaxCode = item.ExportTaxCode,
                        ExportTaxMoney = item.ExportTaxMoney,
                        Status = item.Status,
                        Type = item.Type,
                        CreatedOnDate = item.CreatedOnDate,
                        CreatedByUserCode = item.CreatedByUserCode,
                        CreatedByUserName = item.CreatedByUserName,
                        Description = item.Description,
                        AvatarUrl = item.AvatarUrl,
                        ProductGroupName = group?.Name,
                        UnitName = unit?.Name
                    });
                }

                var res = new Pagination<ProductDetailModel>
                {
                    Content = datas,
                    NumberOfElements = datas.Count,
                    Page = page,
                    Size = size,
                    TotalElements = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / size)
                };

                return new ResponsePagination<ProductDetailModel>(res);
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch Get Products: " + ex.Message);
            }
        }

        public async Task<byte[]> ExportExcelByIdsAsync(List<Guid> ids, CancellationToken ct)
        {
            try
            {
                var productRepo = _unit.GetRepository<Product>();
                var products = await productRepo.GetListAsync(p => ids.Contains(p.Id));

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Products");
                    worksheet.Cell(1, 1).Value = "Mã Sản Phẩm";
                    worksheet.Cell(1, 2).Value = "Tên Sản Phẩm";
                    worksheet.Cell(1, 3).Value = "Đơn Vị Tính";
                    worksheet.Cell(1, 4).Value = "Giá Mua";
                    worksheet.Cell(1, 5).Value = "Giá Bán";
                    worksheet.Cell(1, 6).Value = "Ghi Chú";

                    // Apply basic styling
                    var headerRange = worksheet.Range("A1:F1");
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                    int row = 2;
                    foreach (var item in products)
                    {
                        worksheet.Cell(row, 1).Value = item.Code;
                        worksheet.Cell(row, 2).Value = item.Name;
                        worksheet.Cell(row, 3).Value = item.UnitCode;
                        worksheet.Cell(row, 4).Value = item.PurchasePrice;
                        worksheet.Cell(row, 5).Value = item.SellingPrice;
                        worksheet.Cell(row, 6).Value = item.Notes;
                        row++;
                    }

                    worksheet.Columns().AdjustToContents();

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        return stream.ToArray();
                    }
                }
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }
    }
}
