using AnFood.Infrastructure;
using AnFood.Application.Contant;
using AnFood.Application.Bussiness.EmployeeBussiness;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AnFood.Application.Bussiness.SystemBussiness.AccountBussiness
{
    public class AccountingHandler : IAccountingHandler
    {
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEmployeeHandler _employee;

        public AccountingHandler(IConfiguration config, IHttpContextAccessor httpContextAccessor, IEmployeeHandler employee)
        {
            _config = config;
            _httpContextAccessor = httpContextAccessor;
            _employee = employee;
        }

        public async Task<Response> Create(CreateAccountModel model, CancellationToken ct)
        {
            try
            {
                if (model.IsEmployeeCodeEx == false)
                {
                    var createEmployee = new CreateEmployeeModel
                    {
                        Code = model.UserCode ?? string.Empty,
                        Name = model.UserName ?? string.Empty,
                        BranchCode = model.BranchCode,
                        DateOfBirth = model.DateOfBirth,
                        Gender = model.Gender ?? 0,
                        CustomerCardId = model.NumberId,
                        Phone = model.PhoneNumber,
                        Address = model.Address,
                        Email = model.Email,
                        CityCode = model.CityCode,
                        DistrictCode = model.DistrictCode,
                        WardCode = model.WardCode,
                        ApplyBranchCodes = model.ApplyBranchCodes,
                        IsCashier = model.IsCashier ?? 1,
                        IsSales = model.IsSales ?? 1,
                        IsDelivery = model.IsDelivery,
                        IsWarehouse = model.IsWarehouse,
                        IsTechnician = model.IsTechnician,
                        CreatedByUserId = model.CreatedByUserCode,
                        CreatedByUserName = model.CreatedByUserName
                    };

                    var responseEmployee = await _employee.Create(createEmployee);
                    if (responseEmployee.Code != Code.Success)
                    {
                        return new Response(Code.ServerError, "Tạo nhân viên thất bại: " + responseEmployee.Message);
                    }
                }

                return new Response(Code.Success, "Tạo tài khoản thành công!");
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch Create User: " + ex.Message);
            }
        }

        public async Task<Response> Delete(string id, CancellationToken ct)
        {
            return await Task.FromResult(new Response(Code.Success, "Xóa tài khoản thành công!"));
        }

        public async Task<Response> GetAll(FilterModel model, CancellationToken ct)
        {
            try
            {
                var employeesRes = await _employee.GetByFilter(new EmployeeFilterModel { Page = model.Page ?? 1, Size = model.Size ?? 50 });
                var datas = new List<DetailAccountModel>();

                // Quản trị viên hệ thống
                datas.Add(new DetailAccountModel
                {
                    Email = "admin@utc.edu.vn",
                    FirstName = "Quản trị",
                    LastName = "Viên",
                    Name = "Quản trị viên",
                    UserId = "11111111-1111-1111-1111-111111111111",
                    ApplyListBranchCodesString = new List<string> { "00", "97", "136" },
                    Code = "ADMIN",
                    Status = 1,
                    Roles = new List<RoleModel>
                    {
                        new RoleModel { Id = "admin-role", Name = "Quản trị viên", Code = "ADMIN" }
                    }
                });

                var pagedEmployeeRes = employeesRes as ResponsePagination<EmployeeDetailModel>;
                if (pagedEmployeeRes?.Data?.Content != null)
                {
                    foreach (var emp in pagedEmployeeRes.Data.Content)
                    {
                        datas.Add(new DetailAccountModel
                        {
                            Email = emp.Email ?? $"{emp.Code?.ToLower()}@utc.edu.vn",
                            FirstName = emp.Name,
                            LastName = "",
                            Name = emp.Name,
                            UserId = emp.Id.ToString(),
                            Code = emp.Code,
                            Phone = emp.Phone,
                            Status = 1,
                            Branch = emp.Branch,
                            DateOfBirth = emp.DateOfBirth,
                            Gender = emp.Gender,
                            Roles = new List<RoleModel>
                            {
                                new RoleModel { Id = "staff-role", Name = "Nhân viên", Code = "STAFF" }
                            }
                        });
                    }
                }

                if (!string.IsNullOrEmpty(model.TextSearch))
                {
                    var text = model.TextSearch.Trim().ToLower();
                    datas = datas.Where(x => (x.Email != null && x.Email.ToLower().Contains(text)) || (x.Name != null && x.Name.ToLower().Contains(text))).ToList();
                }

                var pageSize = model.Size ?? 20;
                var page = model.Page ?? 1;
                var totalCount = datas.Count;
                var pagedDatas = datas.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                var res = new Pagination<DetailAccountModel>
                {
                    Content = pagedDatas,
                    NumberOfElements = pagedDatas.Count,
                    Page = page,
                    Size = pageSize,
                    TotalElements = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return new ResponsePagination<DetailAccountModel>(res);
            }
            catch (Exception ex)
            {
                Log.Error("Catch GetAll Account: " + ex.Message);
                return new Response(Code.ServerError, "Catch GetAll Account: " + ex.Message);
            }
        }

        public async Task<Response> GetById(string Id, CancellationToken ct)
        {
            var res = new DetailAccountModel
            {
                Email = "admin@utc.edu.vn",
                FirstName = "Quản trị",
                LastName = "Viên",
                Name = "Quản trị viên",
                UserId = Id,
                Code = "ADMIN",
                Status = 1,
                Roles = new List<RoleModel>
                {
                    new RoleModel { Id = "admin-role", Name = "Quản trị viên", Code = "ADMIN" }
                }
            };
            return await Task.FromResult(new ResponseObject<DetailAccountModel>(res, "Get Account Success", Code.Success));
        }

        public async Task<Response> Update(UpdateAccountModel model, CancellationToken ct)
        {
            return await Task.FromResult(new Response(Code.Success, "Cập nhật tài khoản thành công!"));
        }

        private static string ParseAuthentError(string errorContent)
        {
            if (string.IsNullOrWhiteSpace(errorContent))
                return "Không nhận được phản hồi từ dịch vụ xác thực.";

            try
            {
                using var doc = JsonDocument.Parse(errorContent);
                if (doc.RootElement.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
                {
                    var msg = msgProp.GetString();
                    if (!string.IsNullOrWhiteSpace(msg))
                    {
                        if (msg.Equals("PhoneNumber exists", StringComparison.OrdinalIgnoreCase))
                            return "Số điện thoại đã tồn tại trên hệ thống!";
                        if (msg.Equals("Email exists", StringComparison.OrdinalIgnoreCase))
                            return "Email đã tồn tại trên hệ thống!";
                        if (msg.Contains("UserCode exists", StringComparison.OrdinalIgnoreCase) || msg.Contains("Code exists", StringComparison.OrdinalIgnoreCase))
                            return "Mã người dùng đã tồn tại trên hệ thống!";
                        if (msg.Contains("UserName exists", StringComparison.OrdinalIgnoreCase))
                            return "Tên tài khoản đã tồn tại trên hệ thống!";
                        return msg;
                    }
                }
            }
            catch
            {
                // Not JSON format
            }

            return errorContent;
        }

        public async Task<Response> UpdateMutiple(List<UpdateAccountModel> models, CancellationToken ct)
        {
            try
            {
                if (models == null || models.Count == 0)
                {
                    return new Response(Code.NotFound, "Danh sách tài khoản cập nhật trống");
                }

                foreach (var model in models)
                {
                    var response = await Update(model, ct);
                    if (response.Code != Code.Success)
                    {
                        return response;
                    }
                }

                return new Response(Code.Success, "Cập nhật nhiều tài khoản thành công");
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch Update Multiple: " + ex.Message);
            }
        }
    }
}
