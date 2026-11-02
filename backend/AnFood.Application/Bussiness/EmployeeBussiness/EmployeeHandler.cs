using AnFood.Application.Bussiness.AppLogger;
using AnFood.Application.Bussiness.AuditLogger;
using AnFood.Application.Bussiness.BranchBussiness;
using AnFood.Application.Bussiness.CityBussiness;
using AnFood.Application.Bussiness.DistrictBussiness;
using AnFood.Application.Bussiness.WardBussiness;
using AnFood.Domain.Datatables;
using AnFood.Infrastructure;
using AnFood.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AnFood.Application.Bussiness.EmployeeBussiness
{
    public class EmployeeHandler : IEmployeeHandler
    {
        private sealed class BranchLookup
        {
            public Guid Id { get; set; }
            public string Code { get; set; } = default!;
            public string Name { get; set; } = default!;
            public string CityCode { get; set; } = default!;
            public string? DistrictCode { get; set; }
            public string? WardCode { get; set; }
            public string? Address { get; set; }
            public string? Phone { get; set; }
            public string? Notes { get; set; }
            public string? Email { get; set; }
            public int Status { get; set; }
        }

        private readonly IAppLogger _logsapp;
        private readonly IAuditLogger _logsaudit;
        private readonly IUnitOfWork _unit;

        public EmployeeHandler(IAppLogger logsapp, IAuditLogger logsaudit, IUnitOfWork unit)
        {
            _logsapp = logsapp;
            _logsaudit = logsaudit;
            _unit = unit;
        }

        public async Task<Response> Create(CreateEmployeeModel model)
        {
            try
            {
                var employeeRepo = _unit.GetRepository<Employee>();
                var checkExitCode = await employeeRepo.Get(x => x.Code == model.Code).FirstOrDefaultAsync();
                if (checkExitCode != null)
                {
                    return new Response(Code.Conflict, "Code Exit");
                }

                if (!string.IsNullOrEmpty(model.UserId))
                {
                    var checkUserId = await employeeRepo.Get(x => x.UserId == model.UserId && x.Status != 0).FirstOrDefaultAsync();
                    if (checkUserId != null)
                    {
                        return new Response(Code.Conflict, "UserId already linked to another employee.");
                    }
                }

                if (!string.IsNullOrEmpty(model.Email))
                {
                    var checkEmail = await employeeRepo.Get(x => x.Email == model.Email.Trim() && x.Status != 0).FirstOrDefaultAsync();
                    if (checkEmail != null)
                    {
                        return new Response(Code.Conflict, "Email already exists.");
                    }
                }

                if (!string.IsNullOrEmpty(model.Phone))
                {
                    var checkPhone = await employeeRepo.Get(x => x.Phone == model.Phone.Trim() && x.Status != 0).FirstOrDefaultAsync();
                    if (checkPhone != null)
                    {
                        return new Response(Code.Conflict, "Phone number already exists.");
                    }
                }

                var item = new Employee
                {
                    Id = Guid.NewGuid(),
                    Code = model.Code,
                    Name = model.Name,
                    BranchCode = model.BranchCode,
                    LeaderCode = model.LeaderCode,
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender,
                    CustomerCardId = model.CustomerCardId,
                    Phone = model.Phone,
                    Address = model.Address,
                    Email = model.Email,
                    BankAccount = model.BankAccount,
                    Notes = model.Notes,
                    CityCode = model.CityCode,
                    DistrictCode = model.DistrictCode,
                    WardCode = model.WardCode,
                    CommissionGroupCode = model.CommissionGroupCode,
                    Status = 1,
                    ApplyBranchCodes = model.ApplyBranchCodes,
                    IsCashier = model.IsCashier,
                    IsDelivery = model.IsDelivery,
                    IsSales = model.IsSales,
                    IsTechnician = model.IsTechnician,
                    IsWarehouse = model.IsWarehouse,
                    DutyType = model.DutyType,
                    UserId = model.UserId,
                    IsLoyalCustomer = model.IsCustomer,
                    IsSupplier = model.IsSupplier,
                    IsEmployee = model.IsEmployee,
                    IsCollaborator = model.IsCollaborator,
                    CreatedOnDate = DateTime.Now,
                    CreatedByUserName = model.CreatedByUserName,
                    CreatedByUserCode = model.CreatedByUserId
                };

                employeeRepo.Add(item);
                    var partnerRepo = _unit.GetRepository<BusinessPartner>();
                    var existingPartner = await partnerRepo.Get(x => x.Code == item.Code && x.Status != 0).FirstOrDefaultAsync();
                    if (existingPartner == null)
                    {
                        partnerRepo.Add(new BusinessPartner
                        {
                            Id = Guid.NewGuid(),
                            PartnerTypes = 1,
                            Code = item.Code,
                            Name = item.Name,
                            IdentityNumber = item.CustomerCardId,
                            MobilePhone = item.Phone,
                            DateOfBirth = item.DateOfBirth,
                            Gender = item.Gender,
                            Email = item.Email,
                            Address = item.Address,
                            Note = item.Notes,
                            Status = 1,
                            CreatedOnDate = DateTime.Now,
                            CreatedByUserCode = model.CreatedByUserId,
                            CreatedByUserName = model.CreatedByUserName
                        });
                    }
                if (await _unit.SaveAsync() < 0)
                {
                    return new Response(Code.ServerError, "Save Employee Error");
                }

                await _logsaudit.LogCreateAsync("Employee", item.Id.ToString(), item, model.CreatedByUserId, model.CreatedByUserName);
                return new Response(Code.Success, "Success");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Create Employee Error", ex, model);
                Log.Error("Employee Create Catch: {Time}: {Message}", DateTime.Now, ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> CreateCode(string branchCode, CancellationToken ct)
        {
            try
            {
                var employeeRepo = _unit.GetRepository<Employee>();
                var prefix = branchCode ?? string.Empty;
                var lastCodes = await employeeRepo.Get(x => x.Code.StartsWith(prefix))
                    .Select(x => x.Code)
                    .ToListAsync(ct);

                var max = lastCodes
                    .Select(code =>
                    {
                        var suffix = code.Length > prefix.Length ? code[prefix.Length..] : string.Empty;
                        return int.TryParse(suffix, out var number) ? number : 0;
                    })
                    .DefaultIfEmpty(0)
                    .Max();

                var result = $"{prefix}{max + 1:D5}";
                return new ResponseObject<string>(result, "Create Employee Code Succes", Code.Success);
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Create Employee Code Error", ex, branchCode);
                Log.Error("Create Employee Code Catch: {Time}: {Message}", DateTime.Now, ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> Delete(Guid id, string? deletedByUserId, string? deletedByUserName)
        {
            try
            {
                var employeeRepo = _unit.GetRepository<Employee>();
                var checkExit = await employeeRepo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (checkExit == null)
                {
                    return new Response(Code.NotFound, "Id Not Found");
                }

                employeeRepo.Delete(checkExit);
                if (await _unit.SaveAsync() < 0)
                {
                    return new Response(Code.ServerError, "Delete Employee Error");
                }

                await _logsaudit.LogDeleteAsync("Employee", checkExit.Id.ToString(), checkExit, deletedByUserId, deletedByUserName);
                return new Response(Code.Success, "Success");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Delete Employee Error", ex, id);
                Log.Error("Employee Delete Catch: {Time}: {Message}", DateTime.Now, ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<ResponseObject<EmployeeDetailModel>> GetByCode(string code, CancellationToken ct)
        {
            try
            {
                var employeeRepo = _unit.GetRepository<Employee>();
                var checkExit = await employeeRepo.Get(x => x.Code == code).FirstOrDefaultAsync(ct);
                if (checkExit == null)
                {
                    return new ResponseObject<EmployeeDetailModel>(null, "Id Not Found");
                }

                var res = await MapEmployeeDetailAsync(checkExit, ct);
                return new ResponseObject<EmployeeDetailModel>(res, "Success");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("GetByCode Employee Error", ex, code);
                Log.Error("Employee GetByCode Catch: {Time}: {Message}", DateTime.Now, ex.Message);
                return new ResponseObject<EmployeeDetailModel>(null, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetByFilter(EmployeeFilterModel model)
        {
            try
            {
                var employeeRepo = _unit.GetRepository<Employee>();
                var query = employeeRepo.GetAll();

                if (!string.IsNullOrEmpty(model.TextSearch))
                {
                    var textLower = model.TextSearch.ToLower().Trim();
                    query = query.Where(x =>
                        x.Code.ToLower().Contains(textLower) ||
                        x.Name.ToLower().Contains(textLower) ||
                        (x.Phone != null && x.Phone.Contains(textLower)) ||
                        (x.CustomerCardId != null && x.CustomerCardId.Contains(textLower)));
                }

                if (!string.IsNullOrEmpty(model.BranchCode))
                {
                    var splitBranchCode = model.BranchCode
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    query = query.Where(p =>
                        splitBranchCode.Contains(p.BranchCode ?? string.Empty) ||
                        splitBranchCode.Any(b => ("," + (p.ApplyBranchCodes ?? string.Empty) + ",").Contains("," + b + ",")));
                }

                if (model.DutyType.HasValue)
                {
                    query = query.Where(x => x.DutyType == model.DutyType.Value);
                }

                if (!string.IsNullOrEmpty(model.UserId))
                {
                    query = query.Where(x => x.UserId == model.UserId);
                }

                if (model.IsEmployee.HasValue)
                {
                    query = query.Where(x => x.IsEmployee == model.IsEmployee.Value);
                }
                if (model.IsCustomer.HasValue)
                {
                    query = query.Where(x => x.IsLoyalCustomer == model.IsCustomer.Value);
                }
                if (model.IsSupplier.HasValue)
                {
                    query = query.Where(x => x.IsSupplier == model.IsSupplier.Value);
                }
                if (model.IsCollaborator.HasValue)
                {
                    query = query.Where(x => x.IsCollaborator == model.IsCollaborator.Value);
                }

                model.Size ??= 20;
                model.Page = (model.Page ?? 1) - 1;
                var excludedRows = Math.Max((model.Page ?? 0) * (model.Size ?? 0), 0);
                var totalCount = query.Count();

                var branchQuery = _unit.GetRepository<Branchs>().GetAll()
                    .Select(x => new BranchLookup
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Code = x.Code,
                        CityCode = x.CityCode,
                        DistrictCode = x.DistrictCode,
                        WardCode = x.WardCode,
                        Address = x.Address,
                        Phone = x.Phone,
                        Notes = x.Notes,
                        Email = x.Email,
                        Status = x.Status
                    })
                    .AsNoTracking();

                var branchRepo = _unit.GetRepository<Branchs>().GetAll().AsNoTracking();
                var employeeRepoAll = _unit.GetRepository<Employee>().GetAll().AsNoTracking();

                var rawData = await (from d in query.OrderByDescending(x => x.CreatedOnDate).Skip(excludedRows).Take(model.Size ?? 20)
                                     join b in branchRepo on d.BranchCode equals b.Code into bj
                                     from b in bj.DefaultIfEmpty()
                                     join leader in employeeRepoAll on d.LeaderCode equals leader.Code into lj
                                     from leader in lj.DefaultIfEmpty()
                                     where d.Name != null || d.Name != ""
                                     select new
                                     {
                                         Employee = d,
                                         Branch = b,
                                         Leader = leader
                                     }).ToListAsync();

                var datas = rawData.Select(x => new EmployeeDetailModel
                {
                    Id = x.Employee.Id,
                    Name = x.Employee.Name,
                    Code = x.Employee.Code,
                    DateOfBirth = x.Employee.DateOfBirth,
                    Gender = x.Employee.Gender,
                    CustomerCardId = x.Employee.CustomerCardId,
                    Phone = x.Employee.Phone,
                    Address = x.Employee.Address,
                    Email = x.Employee.Email,
                    BankAccount = x.Employee.BankAccount,
                    Notes = x.Employee.Notes,
                    CityCode = x.Employee.CityCode,
                    DistrictCode = x.Employee.DistrictCode,
                    WardCode = x.Employee.WardCode,
                    CommissionGroupCode = x.Employee.CommissionGroupCode,
                    Status = x.Employee.Status,
                    IsSales = x.Employee.IsSales,
                    IsCashier = x.Employee.IsCashier,
                    IsDelivery = x.Employee.IsDelivery,
                    IsTechnician = x.Employee.IsTechnician,
                    IsWarehouse = x.Employee.IsWarehouse,
                    DutyType = x.Employee.DutyType,
                    UserId = x.Employee.UserId,
                    IsLoyalCustomer = x.Employee.IsLoyalCustomer,
                    IsSupplier = x.Employee.IsSupplier,
                    IsEmployee = x.Employee.IsEmployee,
                    IsCollaborator = x.Employee.IsCollaborator,
                    ApplyListBranchCodesString = x.Employee.ApplyBranchCodes,
                    Branch = x.Branch == null ? null : new BranchModel
                    {
                        Id = x.Branch.Id,
                        Code = x.Branch.Code,
                        Name = x.Branch.Name,
                        CityCode = x.Branch.CityCode,
                        DistrictCode = x.Branch.DistrictCode,
                        WardCode = x.Branch.WardCode,
                        Address = x.Branch.Address,
                        Phone = x.Branch.Phone,
                        Notes = x.Branch.Notes,
                        Email = x.Branch.Email,
                        Status = x.Branch.Status
                    },
                    Leader = x.Leader == null ? null : new EmployeeModel
                    {
                        Id = x.Leader.Id,
                        Code = x.Leader.Code,
                        Name = x.Leader.Name
                    },
                    CreatedByUserCode = x.Employee.CreatedByUserCode,
                    CreatedByUserName = x.Employee.CreatedByUserName,
                    CreatedOnDate = x.Employee.CreatedOnDate
                }).ToList();
                await PopulateApplyBranchCodesAsync(datas, branchQuery, CancellationToken.None);

                var response = new Pagination<EmployeeDetailModel>
                {
                    Content = datas,
                    NumberOfElements = datas.Count,
                    Page = model.Page ?? 0,
                    Size = model.Size ?? 0,
                    TotalElements = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / (model.Size ?? 1))
                };

                return new ResponsePagination<EmployeeDetailModel>(response);
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("GetByFilter Employee Error", ex, model);
                Log.Error("Employee GetByFilter Catch: {Time}: {Message}", DateTime.Now, ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<ResponseObject<EmployeeDetailModel>> GetById(Guid id)
        {
            try
            {
                var employeeRepo = _unit.GetRepository<Employee>();
                var checkExit = await employeeRepo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (checkExit == null)
                {
                    return new ResponseObject<EmployeeDetailModel>(null, "Not found");
                }

                var res = await MapEmployeeDetailAsync(checkExit, CancellationToken.None);
                return new ResponseObject<EmployeeDetailModel>(res, "Success");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("GetById Employee Error", ex, id);
                Log.Error("Employee GetById Catch: {Time}: {Message}", DateTime.Now, ex.Message);
                return new ResponseObject<EmployeeDetailModel>(null, ex.Message);
            }
        }

        public async Task<Response> Update(UpdateEmployeeModel model)
        {
            try
            {
                var employeeRepo = _unit.GetRepository<Employee>();
                var checkExit = await employeeRepo.Get(x => x.Id == model.Id).FirstOrDefaultAsync();
                if (checkExit == null)
                {
                    return new Response(Code.NotFound, "Id Not Found");
                }

                var old = new Employee
                {
                    Code = checkExit.Code,
                    Name = checkExit.Name,
                    BranchCode = checkExit.BranchCode,
                    LeaderCode = checkExit.LeaderCode,
                    DateOfBirth = checkExit.DateOfBirth,
                    Gender = checkExit.Gender,
                    CustomerCardId = checkExit.CustomerCardId,
                    Phone = checkExit.Phone,
                    Email = checkExit.Email,
                    BankAccount = checkExit.BankAccount,
                    Notes = checkExit.Notes,
                    CityCode = checkExit.CityCode,
                    DistrictCode = checkExit.DistrictCode,
                    WardCode = checkExit.WardCode,
                    CommissionGroupCode = checkExit.CommissionGroupCode,
                    Status = checkExit.Status,
                    ApplyBranchCodes = checkExit.ApplyBranchCodes,
                    IsCashier = checkExit.IsCashier,
                    IsDelivery = checkExit.IsDelivery,
                    IsTechnician = checkExit.IsTechnician,
                    IsWarehouse = checkExit.IsWarehouse,
                    IsSales = checkExit.IsSales,
                    Address = checkExit.Address,
                    DutyType = checkExit.DutyType,
                    UserId = checkExit.UserId,
                    IsLoyalCustomer = checkExit.IsLoyalCustomer,
                    IsSupplier = checkExit.IsSupplier,
                    IsEmployee = checkExit.IsEmployee,
                    IsCollaborator = checkExit.IsCollaborator
                };

                if (!string.IsNullOrEmpty(model.UserId))
                {
                    var checkUserId = await employeeRepo.Get(x => x.UserId == model.UserId && x.Id != model.Id && x.Status != 0).FirstOrDefaultAsync();
                    if (checkUserId != null)
                    {
                        return new Response(Code.Conflict, "UserId already linked to another employee.");
                    }
                }

                if (!string.IsNullOrEmpty(model.Email))
                {
                    var checkEmail = await employeeRepo.Get(x => x.Email == model.Email.Trim() && x.Id != model.Id && x.Status != 0).FirstOrDefaultAsync();
                    if (checkEmail != null)
                    {
                        return new Response(Code.Conflict, "Email already exists.");
                    }
                }

                if (!string.IsNullOrEmpty(model.Phone))
                {
                    var checkPhone = await employeeRepo.Get(x => x.Phone == model.Phone.Trim() && x.Id != model.Id && x.Status != 0).FirstOrDefaultAsync();
                    if (checkPhone != null)
                    {
                        return new Response(Code.Conflict, "Phone number already exists.");
                    }
                }

                checkExit.Code = model.Code;
                checkExit.Name = model.Name;
                checkExit.BranchCode = model.BranchCode;
                checkExit.LeaderCode = model.LeaderCode;
                checkExit.DateOfBirth = model.DateOfBirth;
                checkExit.Gender = model.Gender;
                checkExit.CustomerCardId = model.CustomerCardId;
                checkExit.Phone = model.Phone;
                checkExit.Email = model.Email;
                checkExit.BankAccount = model.BankAccount;
                checkExit.Notes = model.Notes;
                checkExit.CityCode = model.CityCode;
                checkExit.DistrictCode = model.DistrictCode;
                checkExit.WardCode = model.WardCode;
                checkExit.CommissionGroupCode = model.CommissionGroupCode;
                checkExit.Status = model.Status;
                checkExit.ApplyBranchCodes = model.ApplyBranchCodes;
                checkExit.IsCashier = model.IsCashier;
                checkExit.IsDelivery = model.IsDelivery;
                checkExit.IsTechnician = model.IsTechnician;
                checkExit.IsWarehouse = model.IsWarehouse;
                checkExit.IsSales = model.IsSales;
                checkExit.Address = model.Address;
                checkExit.DutyType = model.DutyType;
                checkExit.UserId = model.UserId;
                checkExit.IsLoyalCustomer = model.IsCustomer;
                checkExit.IsSupplier = model.IsSupplier;
                checkExit.IsEmployee = model.IsEmployee;
                checkExit.IsCollaborator = model.IsCollaborator;

                employeeRepo.Update(checkExit);
                var partnerRepo = _unit.GetRepository<BusinessPartner>();
                var linkedPartner = await partnerRepo.Get(x => x.Code == checkExit.Code && x.Status != 0).FirstOrDefaultAsync();
                if (checkExit.IsLoyalCustomer == 1)
                {
                    if (linkedPartner == null)
                    {
                        linkedPartner = new BusinessPartner { Id = Guid.NewGuid(), PartnerTypes = 1, Code = checkExit.Code, Status = 1, CreatedOnDate = DateTime.Now };
                        partnerRepo.Add(linkedPartner);
                    }
                    linkedPartner.Name = checkExit.Name;
                    linkedPartner.IdentityNumber = checkExit.CustomerCardId;
                    linkedPartner.MobilePhone = checkExit.Phone;
                    linkedPartner.DateOfBirth = checkExit.DateOfBirth;
                    linkedPartner.Gender = checkExit.Gender;
                    linkedPartner.Email = checkExit.Email;
                    linkedPartner.Address = checkExit.Address;
                    linkedPartner.Note = checkExit.Notes;
                    linkedPartner.PartnerTypes = 1;
                    linkedPartner.Status = 1;
                    linkedPartner.UpdatedOnDate = DateTime.Now;
                    linkedPartner.UpdatedByUserCode = model.UpdatedByUserId;
                    partnerRepo.Update(linkedPartner);
                }
                if (await _unit.SaveAsync() < 0)
                {
                    return new Response(Code.ServerError, "Update DB Error");
                }

                await _logsaudit.LogUpdateAsync("Employee", checkExit.Id.ToString(), old, checkExit, model.UpdatedByUserId, model.UpdatedByUserName);
                return new Response(Code.Success, "Success");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Update Employee Error", ex, model);
                Log.Error("Employee Update Catch: {Time}: {Message}", DateTime.Now, ex.Message);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        private async Task<EmployeeDetailModel> MapEmployeeDetailAsync(Employee employee, CancellationToken ct)
        {
            var branch = await _unit.GetRepository<Branchs>().Get(x => x.Code == employee.BranchCode)
                .Select(x => new BranchModel
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    CityCode = x.CityCode,
                    DistrictCode = x.DistrictCode,
                    WardCode = x.WardCode,
                    Address = x.Address,
                    Phone = x.Phone,
                    Notes = x.Notes,
                    Email = x.Email,
                    Status = x.Status
                })
                .FirstOrDefaultAsync(ct);

            var leader = await _unit.GetRepository<Employee>().Get(x => x.Code == employee.LeaderCode)
                .Select(x => new EmployeeModel
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name
                })
                .FirstOrDefaultAsync(ct);

            var city = await _unit.GetRepository<Cities>().Get(x => x.Code == employee.CityCode)
                .Select(x => new CityModel
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    IsDeleted = x.IsDeleted
                })
                .FirstOrDefaultAsync(ct);

            var district = await _unit.GetRepository<Districts>().Get(x => x.Code == employee.DistrictCode)
                .Select(x => new DistrictModel
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    CityId = x.CityId,
                    IsDelete = x.IsDelete
                })
                .FirstOrDefaultAsync(ct);

            var ward = await _unit.GetRepository<Wards>().Get(x => x.Code == employee.WardCode)
                .Select(x => new WardModel
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    CityId = x.CityId,
                    IsDelete = x.IsDelete
                })
                .FirstOrDefaultAsync(ct);

            var res = new EmployeeDetailModel
            {
                Id = employee.Id,
                Name = employee.Name,
                Code = employee.Code,
                Branch = branch,
                Leader = leader,
                DateOfBirth = employee.DateOfBirth,
                Gender = employee.Gender,
                CustomerCardId = employee.CustomerCardId,
                Phone = employee.Phone,
                Address = employee.Address,
                Email = employee.Email,
                BankAccount = employee.BankAccount,
                Notes = employee.Notes,
                CityCode = employee.CityCode,
                DistrictCode = employee.DistrictCode,
                WardCode = employee.WardCode,
                City = city,
                District = district,
                Ward = ward,
                CommissionGroupCode = employee.CommissionGroupCode,
                Status = employee.Status,
                ApplyListBranchCodesString = employee.ApplyBranchCodes,
                IsSales = employee.IsSales,
                IsCashier = employee.IsCashier,
                IsDelivery = employee.IsDelivery,
                IsTechnician = employee.IsTechnician,
                IsWarehouse = employee.IsWarehouse,
                DutyType = employee.DutyType,
                UserId = employee.UserId,
                IsLoyalCustomer = employee.IsLoyalCustomer,
                IsSupplier = employee.IsSupplier,
                IsEmployee = employee.IsEmployee,
                IsCollaborator = employee.IsCollaborator,
                CreatedByUserCode = employee.CreatedByUserCode,
                CreatedByUserName = employee.CreatedByUserName,
                CreatedOnDate = employee.CreatedOnDate,
                ApplyBranchCodes = new List<BranchModel>()
            };

                await PopulateApplyBranchCodesAsync(
                new List<EmployeeDetailModel> { res },
                _unit.GetRepository<Branchs>().GetAll()
                    .Select(x => new BranchLookup
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Code = x.Code,
                        CityCode = x.CityCode,
                        DistrictCode = x.DistrictCode,
                        WardCode = x.WardCode,
                        Address = x.Address,
                        Phone = x.Phone,
                        Notes = x.Notes,
                        Email = x.Email,
                        Status = x.Status
                    })
                    .AsNoTracking(),
                ct);

            return res;
        }

        private static async Task PopulateApplyBranchCodesAsync(
            List<EmployeeDetailModel> datas,
            IQueryable<BranchLookup> branchQuery,
            CancellationToken ct)
        {
            var allCodes = datas
                .SelectMany(x => (x.ApplyListBranchCodesString ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct()
                .ToList();

            if (allCodes.Count == 0)
            {
                foreach (var item in datas)
                {
                    item.ApplyBranchCodes = new List<BranchModel>();
                }

                return;
            }

            var branches = await branchQuery
                .Where(b => allCodes.Contains(b.Code))
                .Select(b => new BranchModel
                {
                    Id = b.Id,
                    Code = b.Code,
                    Name = b.Name,
                    CityCode = b.CityCode,
                    DistrictCode = b.DistrictCode,
                    WardCode = b.WardCode,
                    Address = b.Address,
                    Phone = b.Phone,
                    Notes = b.Notes,
                    Email = b.Email,
                    Status = b.Status
                })
                .ToListAsync(ct);

            var dict = branches.ToDictionary(x => x.Code);

            foreach (var item in datas)
            {
                var codes = (item.ApplyListBranchCodesString ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                item.ApplyBranchCodes = codes
                    .Where(code => dict.ContainsKey(code))
                    .Select(code => dict[code])
                    .ToList();
            }
        }
    }
}
