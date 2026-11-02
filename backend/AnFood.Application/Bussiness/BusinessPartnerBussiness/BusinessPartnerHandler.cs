using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using AnFood.Application;
using AnFood.Application.Bussiness.AppLogger;
using AnFood.Application.Bussiness.AuditLogger;
using AnFood.Domain.Datatables;
using AnFood.Infrastructure;
using AnFood.Persistence;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace AnFood.Application.Bussiness.BusinessPartnerBussiness
{
    public class BusinessPartnerHandler : IBusinessPartnerHandler
    {
        public async Task<Response> ImportHoAsync(System.IO.Stream fileStream, string? createdByUserCode, string? createdByUserName, bool isCsv, CancellationToken ct)
        {
            try
            {
                using var workbook = isCsv ? new XLWorkbook() : new XLWorkbook(fileStream);
                if (isCsv)
                {
                    var csvSheet = workbook.Worksheets.Add("HO");
                    using var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(fileStream, System.Text.Encoding.UTF8, true);
                    parser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                    parser.SetDelimiters(",");
                    var rowNumber = 1;
                    while (!parser.EndOfData)
                    {
                        ct.ThrowIfCancellationRequested();
                        var fields = parser.ReadFields() ?? Array.Empty<string>();
                        for (var column = 0; column < fields.Length; column++)
                            csvSheet.Cell(rowNumber, column + 1).Value = fields[column];
                        rowNumber++;
                    }
                }
                var sheet = workbook.Worksheets.FirstOrDefault();
                if (sheet == null || sheet.LastRowUsed() == null) return new Response(Code.BadRequest, "File Excel không có dữ liệu.");
                var headers = sheet.Row(1).CellsUsed()
                    .GroupBy(c => NormalizeHeader(c.GetString()))
                    .ToDictionary(g => g.Key, g => g.First().Address.ColumnNumber);
                int Find(params string[] names) => names.Select(NormalizeHeader).Where(headers.ContainsKey).Select(n => headers[n]).FirstOrDefault();
                var codeCol = Find("Mã KH", "Mã khách hàng", "Mã nhân viên", "Mã NV", "Code");
                var nameCol = Find("Họ và tên", "Tên KH", "Tên khách hàng", "Họ tên", "Tên nhân viên", "Name");
                if (codeCol == 0 || nameCol == 0) return new Response(Code.BadRequest, "File cần có cột Mã KH/Mã nhân viên và Tên KH/Họ tên.");
                var repo = _unit.GetRepository<BusinessPartner>();
                var groupCol = Find("Nhóm KH (Mã - tên nhóm)", "Nhóm KH", "Nhóm khách hàng");
                var groups = groupCol == 0 ? new List<BusinessPartnerGroup>() : await _unit.GetRepository<BusinessPartnerGroup>()
                    .Get(x => x.Status == 1 && (x.GroupTypes == 1 || x.GroupTypes == 3)).ToListAsync(ct);
                var imported = 0;
                var skipped = 0;
                var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var dataRows = sheet.RowsUsed().Skip(1).ToList();
                var fileCodes = dataRows.Select(r => r.Cell(codeCol).GetFormattedString().Trim())
                    .Where(c => c.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var existingCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var batch in fileCodes.Chunk(1000))
                {
                    var codes = batch.ToArray();
                    var matches = await repo.Get(x => x.Code != null && x.Status != 0 && codes.Contains(x.Code))
                        .Select(x => x.Code!).ToListAsync(ct);
                    existingCodes.UnionWith(matches);
                }
                foreach (var row in dataRows)
                {
                    ct.ThrowIfCancellationRequested();
                    var code = row.Cell(codeCol).GetFormattedString().Trim();
                    var name = row.Cell(nameCol).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || !seenCodes.Add(code)) { skipped++; continue; }
                    if (existingCodes.Contains(code)) { skipped++; continue; }
                    string? Value(params string[] keys) { var col = Find(keys); return col == 0 ? null : row.Cell(col).GetString().Trim() is { Length: > 0 } value ? value : null; }
                    DateTime? DateValue(params string[] keys) { var col = Find(keys); if (col == 0) return null; var cell = row.Cell(col); if (cell.TryGetValue<DateTime>(out var date)) return date; return DateTime.TryParse(cell.GetString().Trim(), new System.Globalization.CultureInfo("vi-VN"), System.Globalization.DateTimeStyles.None, out date) ? date : null; }
                    int? GenderValue() { var value = Value("Giới tính", "Gender"); if (int.TryParse(value, out var n)) return n; return value?.ToLowerInvariant() switch { "nam" => 1, "nữ" => 2, "khác" => 3, _ => null }; }
                    var typeText = Value("Loại đối tác", "PartnerType");
                    var partnerType = string.IsNullOrEmpty(typeText) ? 1 : int.TryParse(typeText, out var parsedType) ? parsedType : 0;
                    if (partnerType != 1 && partnerType != 3)
                        return new Response(Code.BadRequest, $"Dòng {row.RowNumber()}: Loại đối tác phải là 1 hoặc 3.");
                    Guid? groupId = null;
                    var groupText = groupCol == 0 ? null : row.Cell(groupCol).GetString().Trim();
                    if (!string.IsNullOrWhiteSpace(groupText))
                    {
                        var group = groups.FirstOrDefault(g =>
                            g.Id.ToString().Equals(groupText, StringComparison.OrdinalIgnoreCase) ||
                            g.Code.Equals(groupText, StringComparison.OrdinalIgnoreCase) ||
                            g.Name.Equals(groupText, StringComparison.OrdinalIgnoreCase) ||
                            groupText.StartsWith(g.Code + " - ", StringComparison.OrdinalIgnoreCase));
                        if (group == null)
                            return new Response(Code.BadRequest, $"Dòng {row.RowNumber()}: Không tìm thấy Nhóm KH '{groupText}'.");
                        groupId = group.Id;
                    }
                    repo.Add(new BusinessPartner { Id = Guid.NewGuid(), PartnerTypes = partnerType, PartnerGroupId = groupId, Code = code, Name = name,
                        MobilePhone = Value("Điện thoại", "SĐT", "Số điện thoại", "MobilePhone", "Phone"),
                        IdentityNumber = Value("CCCD", "CMND", "Số CCCD", "IdentityNumber"), DateOfBirth = DateValue("Ngày sinh", "DateOfBirth"),
                        Gender = GenderValue(), CompanyName = Value("Tên công ty"), Email = Value("Email"), Address = Value("Địa chỉ", "Address"),
                        TaxCode = Value("Mã số thuế", "TaxCode"), Note = Value("Ghi chú", "Note"),
                        Status = 1, CreatedOnDate = DateTime.Now, CreatedByUserCode = createdByUserCode, CreatedByUserName = createdByUserName });
                    imported++;
                }
                await _unit.SaveAsync();
                return new ResponseObject<object>(new { Imported = imported, Skipped = skipped }, "Success", Code.Success);
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Import HO business partners error", ex);
                return new Response(Code.ServerError, "Không thể nhập file: " + ex.Message);
            }
        }

        private static string NormalizeHeader(string value) => string.Concat(value.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit));
        private readonly IUnitOfWork _unit;
        private readonly IAppLogger _logsapp;
        private readonly IAuditLogger _logsaudit;

        public BusinessPartnerHandler(IUnitOfWork unit, IAppLogger logsapp, IAuditLogger logsaudit)
        {
            _unit = unit;
            _logsapp = logsapp;
            _logsaudit = logsaudit;
        }

        public async Task<Response> Create(BusinessPartnerCreateModel model)
        {
            try
            {
                if (model.PartnerTypes <= 0 || model.PartnerTypes > 3)
                {
                    return new Response(Code.ServerError, "Vui lÃ²ng chá»n loáº¡i Ä‘á»‘i tÃ¡c há»£p lá»‡.");
                }

                if (string.IsNullOrWhiteSpace(model.Name) && string.IsNullOrWhiteSpace(model.CompanyName))
                {
                    return new Response(Code.ServerError, "Há» tÃªn hoáº·c TÃªn cÃ´ng ty khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
                }

                var repo = _unit.GetRepository<BusinessPartner>();

                // Auto-generate code if empty
                if (string.IsNullOrWhiteSpace(model.Code))
                {
                    var prefix = model.PartnerTypes == 2 ? "NCC" : "KH";
                    var lastCodes = await repo.Get(x => x.Code != null && x.Code.StartsWith(prefix))
                        .Select(x => x.Code)
                        .ToListAsync();
                    var max = lastCodes
                        .Select(code =>
                        {
                            var suffix = code.Length > prefix.Length ? code[prefix.Length..] : string.Empty;
                            return int.TryParse(suffix, out var number) ? number : 0;
                        })
                        .DefaultIfEmpty(0)
                        .Max();
                    model.Code = $"{prefix}{max + 1:D6}";
                }
                else
                {
                    var existing = await repo.Get(x => x.Code == model.Code.Trim() && x.Status != 0).FirstOrDefaultAsync();
                    if (existing != null)
                    {
                        return new Response(Code.Conflict, "MÃ£ Ä‘á»‘i tÃ¡c Ä‘Ã£ tá»“n táº¡i.");
                    }
                }

                // Uniqueness checks for IdentityNumber and TaxCode
                if (!string.IsNullOrWhiteSpace(model.IdentityNumber))
                {
                    var existingIdentity = await repo.Get(x => x.IdentityNumber == model.IdentityNumber.Trim() && x.Status != 0).FirstOrDefaultAsync();
                    if (existingIdentity != null)
                    {
                        return new Response(Code.Conflict, "Sá»‘ CCCD Ä‘Ã£ tá»“n táº¡i.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(model.TaxCode))
                {
                    var existingTax = await repo.Get(x => x.TaxCode == model.TaxCode.Trim() && x.Status != 0).FirstOrDefaultAsync();
                    if (existingTax != null)
                    {
                        return new Response(Code.Conflict, "MÃ£ sá»‘ thuáº¿ Ä‘Ã£ tá»“n táº¡i.");
                    }
                }

                // Group compatibility check
                if (model.PartnerGroupId.HasValue)
                {
                    var groupRepo = _unit.GetRepository<BusinessPartnerGroup>();
                    var group = await groupRepo.Get(x => x.Id == model.PartnerGroupId.Value && x.Status == 1).FirstOrDefaultAsync();
                    if (group == null)
                    {
                        return new Response(Code.NotFound, "NhÃ³m Ä‘á»‘i tÃ¡c khÃ´ng tá»“n táº¡i hoáº·c Ä‘Ã£ bá»‹ ngá»«ng sá»­ dá»¥ng.");
                    }

                    // group types: 1 = CustomerGroup, 2 = SupplierGroup, 3 = Both
                    // partner types: 1 = Customer, 2 = Supplier, 3 = Both
                    bool isCompatible = false;
                    if (group.GroupTypes == 3) isCompatible = true;
                    else if (group.GroupTypes == 1 && (model.PartnerTypes == 1 || model.PartnerTypes == 3)) isCompatible = true;
                    else if (group.GroupTypes == 2 && (model.PartnerTypes == 2 || model.PartnerTypes == 3)) isCompatible = true;

                    if (!isCompatible)
                    {
                        return new Response(Code.Conflict, "Loáº¡i nhÃ³m Ä‘á»‘i tÃ¡c khÃ´ng tÆ°Æ¡ng thÃ­ch vá»›i loáº¡i Ä‘á»‘i tÃ¡c Ä‘Ã£ chá»n.");
                    }
                }

                var item = new BusinessPartner
                {
                    Id = Guid.NewGuid(),
                    PartnerTypes = model.PartnerTypes,
                    Code = model.Code.Trim(),
                    Name = model.Name?.Trim(),
                    PartnerGroupId = model.PartnerGroupId,
                    IdentityNumber = model.IdentityNumber?.Trim(),
                    MobilePhone = model.MobilePhone?.Trim(),
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender,
                    CompanyName = model.CompanyName?.Trim(),
                    TaxCode = model.TaxCode?.Trim(),
                    Email = model.Email?.Trim(),
                    Address = model.Address?.Trim(),
                    Note = model.Note,
                    Status = 1,
                    CreatedOnDate = DateTime.Now,
                    CreatedByUserCode = model.CreatedByUserCode,
                    CreatedByUserName = model.CreatedByUserName
                };

                repo.Add(item);
                await _unit.SaveAsync();

                await _logsaudit.LogCreateAsync("BusinessPartner", item.Id.ToString(), item, model.CreatedByUserCode, model.CreatedByUserName);
                return new Response(Code.Success, "ThÃªm má»›i Ä‘á»‘i tÃ¡c thÃ nh cÃ´ng.");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Create BusinessPartner Error", ex, model);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Update(BusinessPartnerUpdateModel model)
        {
            try
            {
                if (model.PartnerTypes <= 0 || model.PartnerTypes > 3)
                {
                    return new Response(Code.ServerError, "Vui lÃ²ng chá»n loáº¡i Ä‘á»‘i tÃ¡c há»£p lá»‡.");
                }

                if (string.IsNullOrWhiteSpace(model.Name) && string.IsNullOrWhiteSpace(model.CompanyName))
                {
                    return new Response(Code.ServerError, "Há» tÃªn hoáº·c TÃªn cÃ´ng ty khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
                }

                if (string.IsNullOrWhiteSpace(model.Code))
                {
                    return new Response(Code.ServerError, "MÃ£ Ä‘á»‘i tÃ¡c lÃ  báº¯t buá»™c.");
                }

                var repo = _unit.GetRepository<BusinessPartner>();
                var item = await repo.Get(x => x.Id == model.Id && x.Status != 0).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y Ä‘á»‘i tÃ¡c.");
                }

                var requestedCode = model.Code.Trim();

                if (!string.Equals(item.Code, requestedCode, StringComparison.OrdinalIgnoreCase))

                {

                    return new Response(Code.BadRequest, "Ma doi tac khong duoc phep chinh sua sau khi tao.");

                }

                // CCCD checks
                if (!string.IsNullOrWhiteSpace(model.IdentityNumber))
                {
                    var existingIdentity = await repo.Get(x => x.IdentityNumber == model.IdentityNumber.Trim() && x.Id != model.Id && x.Status != 0).FirstOrDefaultAsync();
                    if (existingIdentity != null)
                    {
                        return new Response(Code.Conflict, "Sá»‘ CCCD Ä‘Ã£ tá»“n táº¡i.");
                    }
                }

                // Tax checks
                if (!string.IsNullOrWhiteSpace(model.TaxCode))
                {
                    var existingTax = await repo.Get(x => x.TaxCode == model.TaxCode.Trim() && x.Id != model.Id && x.Status != 0).FirstOrDefaultAsync();
                    if (existingTax != null)
                    {
                        return new Response(Code.Conflict, "MÃ£ sá»‘ thuáº¿ Ä‘Ã£ tá»“n táº¡i.");
                    }
                }

                // Group compatibility check
                if (model.PartnerGroupId.HasValue)
                {
                    var groupRepo = _unit.GetRepository<BusinessPartnerGroup>();
                    var group = await groupRepo.Get(x => x.Id == model.PartnerGroupId.Value && (x.Status == 1 || x.Id == item.PartnerGroupId)).FirstOrDefaultAsync();
                    if (group == null)
                    {
                        return new Response(Code.NotFound, "NhÃ³m Ä‘á»‘i tÃ¡c khÃ´ng tá»“n táº¡i hoáº·c Ä‘Ã£ bá»‹ ngá»«ng sá»­ dá»¥ng.");
                    }

                    bool isCompatible = false;
                    if (group.GroupTypes == 3) isCompatible = true;
                    else if (group.GroupTypes == 1 && (model.PartnerTypes == 1 || model.PartnerTypes == 3)) isCompatible = true;
                    else if (group.GroupTypes == 2 && (model.PartnerTypes == 2 || model.PartnerTypes == 3)) isCompatible = true;

                    if (!isCompatible)
                    {
                        return new Response(Code.Conflict, "Loáº¡i nhÃ³m Ä‘á»‘i tÃ¡c khÃ´ng tÆ°Æ¡ng thÃ­ch vá»›i loáº¡i Ä‘á»‘i tÃ¡c Ä‘Ã£ chá»n.");
                    }
                }

                var old = new BusinessPartner
                {
                    PartnerTypes = item.PartnerTypes,
                    Code = item.Code,
                    Name = item.Name,
                    PartnerGroupId = item.PartnerGroupId,
                    IdentityNumber = item.IdentityNumber,
                    MobilePhone = item.MobilePhone,
                    DateOfBirth = item.DateOfBirth,
                    Gender = item.Gender,
                    CompanyName = item.CompanyName,
                    TaxCode = item.TaxCode,
                    Email = item.Email,
                    Address = item.Address,
                    Note = item.Note,
                    Status = item.Status
                };

                item.PartnerTypes = model.PartnerTypes;
                item.Name = model.Name?.Trim();
                item.PartnerGroupId = model.PartnerGroupId;
                item.IdentityNumber = model.IdentityNumber?.Trim();
                item.MobilePhone = model.MobilePhone?.Trim();
                item.DateOfBirth = model.DateOfBirth;
                item.Gender = model.Gender;
                item.CompanyName = model.CompanyName?.Trim();
                item.TaxCode = model.TaxCode?.Trim();
                item.Email = model.Email?.Trim();
                item.Address = model.Address?.Trim();
                item.Note = model.Note;
                item.Status = model.Status;
                item.UpdatedOnDate = DateTime.Now;
                item.UpdatedByUserCode = model.UpdatedByUserCode;

                repo.Update(item);
                await _unit.SaveAsync();

                await _logsaudit.LogUpdateAsync("BusinessPartner", item.Id.ToString(), old, item, model.UpdatedByUserCode, model.UpdatedByUserName);
                return new Response(Code.Success, "Cáº­p nháº­t Ä‘á»‘i tÃ¡c thÃ nh cÃ´ng.");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Update BusinessPartner Error", ex, model);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Delete(Guid id, string? userCode, string? userName)
        {
            try
            {
                var repo = _unit.GetRepository<BusinessPartner>();
                var groupRepo = _unit.GetRepository<BusinessPartnerGroup>();
                var item = await repo.Get(x => x.Id == id && x.Status != 0).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y Ä‘á»‘i tÃ¡c.");
                }

                var old = new BusinessPartner
                {
                    PartnerTypes = item.PartnerTypes,
                    Code = item.Code,
                    Name = item.Name,
                    PartnerGroupId = item.PartnerGroupId,
                    IdentityNumber = item.IdentityNumber,
                    MobilePhone = item.MobilePhone,
                    DateOfBirth = item.DateOfBirth,
                    Gender = item.Gender,
                    CompanyName = item.CompanyName,
                    TaxCode = item.TaxCode,
                    Email = item.Email,
                    Address = item.Address,
                    Note = item.Note,
                    Status = item.Status
                };

                item.Status = 0; // Soft delete
                item.UpdatedOnDate = DateTime.Now;
                item.UpdatedByUserCode = userCode;

                repo.Update(item);
                await _unit.SaveAsync();

                await _logsaudit.LogDeleteAsync("BusinessPartner", item.Id.ToString(), old, userCode, userName);
                return new Response(Code.Success, "XÃ³a Ä‘á»‘i tÃ¡c thÃ nh cÃ´ng.");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Delete BusinessPartner Error", ex, id);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetById(Guid id)
        {
            try
            {
                var repo = _unit.GetRepository<BusinessPartner>();
                var groupRepo = _unit.GetRepository<BusinessPartnerGroup>();
                var item = await repo.Get(x => x.Id == id && x.Status != 0).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y Ä‘á»‘i tÃ¡c.");
                }

                var groupName = item.PartnerGroupId.HasValue ?
                    (await groupRepo.Get(x => x.Id == item.PartnerGroupId.Value).Select(x => x.Name).FirstOrDefaultAsync()) : null;

                var res = new BusinessPartnerModel
                {
                    Id = item.Id,
                    PartnerTypes = item.PartnerTypes,
                    Code = item.Code,
                    Name = item.Name,
                    PartnerGroupId = item.PartnerGroupId,
                    PartnerGroupName = groupName,
                    IdentityNumber = item.IdentityNumber,
                    MobilePhone = item.MobilePhone,
                    DateOfBirth = item.DateOfBirth,
                    Gender = item.Gender,
                    CompanyName = item.CompanyName,
                    TaxCode = item.TaxCode,
                    Email = item.Email,
                    Address = item.Address,
                    Note = item.Note,
                    Status = item.Status
                };

                return new ResponseObject<BusinessPartnerModel>(res, "Success", Code.Success);
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetByFilter(BusinessPartnerFilterModel model)
        {
            try
            {
                var repo = _unit.GetRepository<BusinessPartner>();
                var groupRepo = _unit.GetRepository<BusinessPartnerGroup>();
                var query = repo.Get(x => x.Status != 0);

                if (!string.IsNullOrEmpty(model.TextSearch))
                {
                    var textLower = model.TextSearch.ToLower().Trim();
                    query = query.Where(x =>
                        (x.Code != null && x.Code.ToLower().Contains(textLower)) ||
                        (x.Name != null && x.Name.ToLower().Contains(textLower)) ||
                        (x.CompanyName != null && x.CompanyName.ToLower().Contains(textLower)) ||
                        (x.MobilePhone != null && x.MobilePhone.Contains(textLower)) ||
                        (x.IdentityNumber != null && x.IdentityNumber.Contains(textLower)) ||
                        (x.TaxCode != null && x.TaxCode.Contains(textLower)) ||
                        (x.Email != null && x.Email.ToLower().Contains(textLower))
                    );
                }

                if (model.PartnerTypes.HasValue)
                {
                    // If filter is Customer (1), load: 1 (Customer) and 3 (Both)
                    // If filter is Supplier (2), load: 2 (Supplier) and 3 (Both)
                    // If filter is Both (3), load: 3 (Both)
                    if (model.PartnerTypes.Value == 3)
                    {
                        query = query.Where(x => x.PartnerTypes == 3);
                    }
                    else
                    {
                        query = query.Where(x => x.PartnerTypes == model.PartnerTypes.Value || x.PartnerTypes == 3);
                    }
                }

                if (model.PartnerGroupId.HasValue)
                {
                    query = query.Where(x => x.PartnerGroupId == model.PartnerGroupId.Value);
                }

                if (model.Gender.HasValue)
                {
                    query = query.Where(x => x.Gender == model.Gender.Value);
                }

                if (model.Status.HasValue)
                {
                    query = query.Where(x => x.Status == model.Status.Value);
                }

                model.Size ??= 20;
                model.Page = (model.Page ?? 1) - 1;
                var excludedRows = Math.Max((model.Page ?? 0) * (model.Size ?? 0), 0);
                var totalCount = query.Count();

                var groupQuery = _unit.GetRepository<BusinessPartnerGroup>().GetAll()
                    .Select(x => new { x.Id, x.Name })
                    .AsNoTracking();

                var data = from d in query.OrderBy(x => x.Code).Skip(excludedRows).Take(model.Size.Value)
                           join g in groupQuery on d.PartnerGroupId equals g.Id into gj
                           from g in gj.DefaultIfEmpty()
                           select new BusinessPartnerModel
                           {
                               Id = d.Id,
                               PartnerTypes = d.PartnerTypes,
                               Code = d.Code,
                               Name = d.Name,
                               PartnerGroupId = d.PartnerGroupId,
                               PartnerGroupName = g == null ? null : g.Name,
                               IdentityNumber = d.IdentityNumber,
                               MobilePhone = d.MobilePhone,
                               DateOfBirth = d.DateOfBirth,
                               Gender = d.Gender,
                               CompanyName = d.CompanyName,
                               TaxCode = d.TaxCode,
                               Email = d.Email,
                               Address = d.Address,
                               Note = d.Note,
                               Status = d.Status
                           };

                var datas = await data.ToListAsync();

                var response = new Pagination<BusinessPartnerModel>
                {
                    Content = datas,
                    NumberOfElements = datas.Count,
                    Page = (model.Page ?? 0) + 1,
                    Size = model.Size ?? 0,
                    TotalElements = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / (model.Size ?? 1))
                };

                return new ResponsePagination<BusinessPartnerModel>(response);
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("GetByFilter BusinessPartner Error", ex, model);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetNextCode(int partnerType)
        {
            try
            {
                var repo = _unit.GetRepository<BusinessPartner>();
                var groupRepo = _unit.GetRepository<BusinessPartnerGroup>();
                var prefix = partnerType == 2 ? "NCC" : "KH";
                var lastCodes = await repo.Get(x => x.Code != null && x.Code.StartsWith(prefix))
                    .Select(x => x.Code!)
                    .ToListAsync();
                var max = lastCodes
                    .Select(code =>
                    {
                        var suffix = code.Length > prefix.Length ? code[prefix.Length..] : string.Empty;
                        return int.TryParse(suffix, out var number) ? number : 0;
                    })
                    .DefaultIfEmpty(0)
                    .Max();
                var nextCode = $"{prefix}{(max + 1):D6}";
                return new ResponseObject<string>(nextCode, "Success", Code.Success);
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("GetNextCode BusinessPartner Error", ex, partnerType);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }
    }
}


