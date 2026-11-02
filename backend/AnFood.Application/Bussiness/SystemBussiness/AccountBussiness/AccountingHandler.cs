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

                var modelCreate = new CreateAccountSsoModel
                {
                    FirstName = string.Empty,
                    LastName = model.UserName ?? string.Empty,
                    Email = model.Email ?? string.Empty,
                    Password = model.Password,
                    ConfirmPassword = model.Password,
                    MailWork = model.Email ?? string.Empty,
                    RoleIds = model.RoleIds,
                    ApplyBranchCodes = model.ApplyBranchCodes ?? "00",
                    UserCode = model.UserCode ?? string.Empty,
                    PhoneNumber = model.PhoneNumber
                };

                using var _httpClient = new HttpClient();
                var json = JsonSerializer.Serialize(modelCreate);
                var url = _config["AuthentService:Url"] + "authentication/register";
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = AuthenticationHeaderValue.Parse(token);
                }

                var response = await _httpClient.SendAsync(request, ct);
                var errorContent = await response.Content.ReadAsStringAsync(ct);

                try
                {
                    using var doc = JsonDocument.Parse(errorContent);
                    if (doc.RootElement.TryGetProperty("code", out var codeProp) && codeProp.GetInt32() != 200)
                    {
                        return new Response(Code.ServerError, ParseAuthentError(errorContent));
                    }
                }
                catch
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        return new Response(Code.ServerError, ParseAuthentError(errorContent));
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
            try
            {
                var url = $"{_config["AuthentService:Url"]}account?userId={id}";
                using var _httpClient = new HttpClient();
                var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();

                var request = new HttpRequestMessage(HttpMethod.Delete, url);

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = AuthenticationHeaderValue.Parse(token);
                }

                var response = await _httpClient.SendAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct);
                    return new Response(Code.ServerError, ParseAuthentError(error));
                }

                return new Response(Code.Success, "Xóa tài khoản thành công!");
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch Delete User: " + ex.Message);
            }
        }

        public async Task<Response> GetAll(FilterModel model, CancellationToken ct)
        {
            try
            {
                using var _httpClient = new HttpClient();
                var url = $"{_config["AuthentService:Url"]}account/getall";
                var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();

                var request = new HttpRequestMessage(HttpMethod.Get, url);

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = AuthenticationHeaderValue.Parse(token);
                }

                var response = await _httpClient.SendAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct);
                    return new Response(Code.ServerError, ParseAuthentError(error));
                }

                var result = await response.Content.ReadAsStringAsync(ct);

                var dataRes = JsonSerializer.Deserialize<GetAllAccountSsoResponse>(
                    result,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                var listAccount = dataRes?.Data ?? new List<AccountSsoDto>();

                if (!string.IsNullOrEmpty(model.TextSearch))
                {
                    var text = model.TextSearch.Trim().ToLower();
                    listAccount = listAccount.Where(x => x.Email != null && x.Email.ToLower().Contains(text)).ToList();
                }

                if (!model.Page.HasValue)
                {
                    model.Page = 1;
                }
                var pageSize = model.Size ?? 20;
                var skip = (model.Page.Value - 1) * pageSize;
                var totalCount = listAccount.Count;

                listAccount = listAccount.Skip(skip).Take(pageSize).ToList();

                var datas = new List<DetailAccountModel>();
                foreach (var item in listAccount)
                {
                    var detail = new DetailAccountModel
                    {
                        Email = item.Email,
                        FirstName = item.FirstName,
                        LastName = item.LastName,
                        Name = item.LastName + " " + item.FirstName,
                        UserId = item.UserId,
                        ApplyListBranchCodesString = item.ApplyBranchCodes,
                        Roles = item.Roles?.Select(x => new RoleModel
                        {
                            Id = x.Id.ToString(),
                            Name = x.Name,
                            Code = x.Code
                        }).ToList() ?? new List<RoleModel>(),
                        Status = item.IsActive == null || item.IsActive == false ? 0 : 1,
                        Phone = item.PhoneNumber,
                        Code = item.UserCode
                    };

                    var getInfoEmployeeRes = await _employee.GetByCode(item.UserCode ?? string.Empty, ct);
                    var getInfoEmployee = getInfoEmployeeRes.Data;

                    detail.Branch = getInfoEmployee?.Branch;
                    detail.Leader = getInfoEmployee?.Leader;
                    detail.DateOfBirth = getInfoEmployee?.DateOfBirth;
                    detail.Gender = getInfoEmployee != null ? getInfoEmployee.Gender : 0;
                    detail.CustomerCardId = getInfoEmployee?.CustomerCardId;
                    detail.Address = getInfoEmployee?.Address;
                    detail.Notes = getInfoEmployee?.Notes;
                    detail.City = getInfoEmployee?.City;
                    detail.District = getInfoEmployee?.District;
                    detail.Ward = getInfoEmployee?.Ward;

                    detail.IsCashier = getInfoEmployee?.IsCashier;
                    detail.IsSales = getInfoEmployee?.IsSales;
                    detail.IsDelivery = getInfoEmployee?.IsDelivery;
                    detail.IsWarehouse = getInfoEmployee?.IsWarehouse;
                    detail.IsTechnician = getInfoEmployee?.IsTechnician;
                    if (getInfoEmployee != null)
                    {
                        detail.Name = getInfoEmployee.Name;
                        detail.Code = getInfoEmployee.Code;
                        detail.FirstName = getInfoEmployee.Name;
                    }

                    datas.Add(detail);
                }

                var res = new Pagination<DetailAccountModel>
                {
                    Content = datas,
                    NumberOfElements = datas.Count,
                    Page = model.Page.Value,
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
            try
            {
                using var _httpClient = new HttpClient();
                var url = $"{_config["AuthentService:Url"]}account/getby-id?Id=" + Id;
                var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();

                var request = new HttpRequestMessage(HttpMethod.Get, url);

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = AuthenticationHeaderValue.Parse(token);
                }

                var response = await _httpClient.SendAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct);
                    return new Response(Code.ServerError, ParseAuthentError(error));
                }

                var result = await response.Content.ReadAsStringAsync(ct);

                var dataRes = JsonSerializer.Deserialize<GetDetailAccountSsoResponse>(
                    result,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                var item = dataRes?.Data;
                var res = new DetailAccountModel();

                if (item != null)
                {
                    res.FirstName = item.FirstName;
                    res.LastName = item.LastName;
                    res.Name = item.LastName + " " + item.FirstName;
                    res.Email = item.Email;
                    res.UserId = item.UserId;
                    res.ApplyListBranchCodesString = item.ApplyBranchCodes;
                    res.Phone = item.PhoneNumber;
                    res.Code = item.UserCode;
                    res.Status = item.IsActive == null || item.IsActive == false ? 0 : 1;
                    res.Roles = item.Roles ?? new List<RoleModel>();

                    var getInfoEmployeeRes = await _employee.GetByCode(item.UserCode ?? string.Empty, ct);
                    var getInfoEmployee = getInfoEmployeeRes.Data;
                    res.Name = getInfoEmployee?.Name;
                    res.Code = getInfoEmployee?.Code;
                    res.Branch = getInfoEmployee?.Branch;
                    res.Leader = getInfoEmployee?.Leader;
                    res.DateOfBirth = getInfoEmployee?.DateOfBirth;
                    res.Gender = getInfoEmployee != null ? getInfoEmployee.Gender : 0;
                    res.CustomerCardId = getInfoEmployee?.CustomerCardId;
                    res.Address = getInfoEmployee?.Address;
                    res.Notes = getInfoEmployee?.Notes;
                    res.City = getInfoEmployee?.City;
                    res.District = getInfoEmployee?.District;
                    res.Ward = getInfoEmployee?.Ward;
                    
                    res.ApplyBranchCodes = getInfoEmployee?.ApplyBranchCodes;
                    res.IsCashier = getInfoEmployee?.IsCashier;
                    res.IsSales = getInfoEmployee?.IsSales;
                    res.IsDelivery = getInfoEmployee?.IsDelivery;
                    res.IsWarehouse = getInfoEmployee?.IsWarehouse;
                    res.IsTechnician = getInfoEmployee?.IsTechnician;
                }

                return new ResponseObject<DetailAccountModel>(res, "Get Account Success", Code.Success);
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch GetById Account: " + ex.Message);
            }
        }

        public async Task<Response> Update(UpdateAccountModel model, CancellationToken ct)
        {
            try
            {
                using var _httpClient = new HttpClient();
                var url = $"{_config["AuthentService:Url"]}account";

                var json = JsonSerializer.Serialize(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();

                var request = new HttpRequestMessage(HttpMethod.Put, url)
                {
                    Content = content
                };

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = AuthenticationHeaderValue.Parse(token);
                }

                var response = await _httpClient.SendAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct);
                    return new Response(Code.ServerError, ParseAuthentError(error));
                }

                var result = await response.Content.ReadAsStringAsync(ct);
                return new Response(Code.Success, result);
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch Update User: " + ex.Message);
            }
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
