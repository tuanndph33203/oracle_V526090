using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnFood.Infrastructure
{
    #region Response
    public class OldResponse
    {
        public int Status { get; set; }

        public string Message { get; set; }

        public OldResponse(int status, string message = null)
        {
            Status = status;
            Message = message;
        }

        public OldResponse() { }
    }

    public class OldResponse<T> : OldResponse
    {
        public T Data { get; set; }

        public int TotalCount { get; set; }

        public int DataCount { get; set; }
        public decimal SalaryCount { get; set; }

        public OldResponse() { }

        public OldResponse(int status, string message = null, T data = default(T))
                : base(status, message)
        {
            Data = data;
            TotalCount = 0;
            DataCount = 0;
        }

        public OldResponse(int status, string message = null, T data = default(T), int dataCount = 0, int totalCount = 0)
            : base(status, message)
        {
            Data = data;
            TotalCount = totalCount;
            DataCount = dataCount;
        }

    }

    /// <summary>
    ///     �?i tu?ng tr? v?
    /// </summary>
    public class Response
    {
        [JsonConstructor]
        public Response(Code code, string message)
        {
            Code = code;
            Message = message;
        }

        public Response(string message)
        {
            Message = message;
        }

        public Response()
        {
        }

        /// <summary>
        ///     M� l?i tr? v?
        /// </summary>
        public Code Code { get; set; } = Code.Success;

        /// <summary>
        ///     Th�ng tin chi ti?t
        /// </summary>
        public string Message { get; set; } = "Th�nh c�ng";

        /// <summary>
        ///     Th�ng tin chi ti?t
        /// </summary>
        public long TotalTime { get; set; } = 0;

        public int? DataCount { get; set; }
        public int? Status { get; set; }
        public int? TotalCount { get; set; }
    }

    /// <summary>
    ///     Tr? v? L?i
    /// </summary>
    public class ResponseError : Response
    {
        [JsonConstructor]
        public ResponseError(Code code, string message, List<Dictionary<string, string>> errorDetail = null) : base(
            code,
            message)
        {
            ErrorDetail = errorDetail;
        }

        public List<Dictionary<string, string>> ErrorDetail { get; set; }
    }

    /// <summary>
    ///     Tr? v? d?ng d?i tu?ng
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ResponseObject<T> : Response
    {
        public ResponseObject()
        {
        }

        //[JsonConstructor]
        public ResponseObject(T data)
        {
            Data = data;
        }
        //[JsonConstructor]
        public ResponseObject(T data, Exception ex)
        {
            Data = data;
        }
        public ResponseObject(T data, string message)
        {
            Data = data;
            Message = message;
        }
        public ResponseObject(T data, string message, Code code)
        {
            Code = code;
            Data = data;
            Message = message;
        }
        public ResponseObject(T data, string message, Code code, int status, int dataCount, int totalCount)
        {
            Code = code;
            Data = data;
            Message = message;
            Status = status;
            DataCount = dataCount;
            TotalCount = totalCount;
        }
        /// <summary>
        ///     D? li?u tr? v?
        /// </summary>
        public T Data { get; set; }
    }

    /// <summary>
    ///     Tr? v? d?ng m?ng
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ResponseList<T> : Response
    {
        [JsonConstructor]
        public ResponseList(List<T> data)
        {
            Data = data;
        }

        public ResponseList(List<T> data, string message)
        {
            Data = data;
            Message = message;
        }

        public ResponseList(List<T> data, string message, Code code)
        {
            Code = code;
            Data = data;
            Message = message;
        }
        /// <summary>
        ///     Danh s�ch d? li?u tr? v?
        /// </summary>
        public List<T> Data { get; set; }
    }

    /// <summary>
    ///     Tr? v? d?ng ph�n trang
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ResponsePagination<T> : Response
    {
        [JsonConstructor]
        public ResponsePagination(Pagination<T> data)
        {
            Data = data;
        }

        /// <summary>
        ///     Danh s�ch d? li?u tr? v?
        /// </summary>
        public Pagination<T> Data { get; set; }
    }

    /// <summary>
    ///     Tr? v? k?t qu? c?p nh?t d? li?u
    /// </summary>
    public class ResponseUpdate : Response
    {
        [JsonConstructor]
        public ResponseUpdate(Guid id)
        {
            Data = new ResponseUpdateModel { Id = id };
        }

        public ResponseUpdate(Guid id, string message) : base(message)
        {
            Data = new ResponseUpdateModel { Id = id };
        }

        public ResponseUpdate(Code code, string message, Guid id) : base(code, message)
        {
            Data = new ResponseUpdateModel { Id = id };
        }

        public ResponseUpdate()
        {
        }

        /// <summary>
        ///     Danh s�ch d? li?u tr? v?
        /// </summary>
        public ResponseUpdateModel Data { get; set; }
    }

    /// <summary>
    ///     Tr? v? k?t qu? c?p nh?t nhi?u d? li?u
    /// </summary>
    public class ResponseUpdateMulti : Response
    {
        [JsonConstructor]
        public ResponseUpdateMulti(List<ResponseUpdate> data)
        {
            Data = data;
        }

        public ResponseUpdateMulti()
        {
        }

        /// <summary>
        ///     Danh s�ch d? li?u tr? v?
        /// </summary>
        public List<ResponseUpdate> Data { get; set; }
    }

    /// <summary>
    ///     Tr? v? k?t qu? x�a d? li?u
    /// </summary>
    public class ResponseDelete : Response
    {
        [JsonConstructor]
        public ResponseDelete(Guid id, string name)
        {
            Data = new ResponseDeleteModel { Id = id, Name = name };
        }

        public ResponseDelete(Code code, string message, Guid id, string name) : base(code, message)
        {
            Data = new ResponseDeleteModel { Id = id, Name = name };
        }

        public ResponseDelete()
        {
        }

        /// <summary>
        ///     Danh s�ch d? li?u tr? v?
        /// </summary>
        public ResponseDeleteModel Data { get; set; }
    }

    /// <summary>
    ///     Tr? v? k?t qu? x�a nhi?u d? li?u
    /// </summary>
    public class ResponseDeleteMulti : Response
    {
        [JsonConstructor]
        public ResponseDeleteMulti(List<ResponseDelete> data)
        {
            Data = data;
        }

        public ResponseDeleteMulti()
        {
        }

        /// <summary>
        ///     Danh s�ch d? li?u tr? v?
        /// </summary>
        public List<ResponseDelete> Data { get; set; }
    }

    /// <summary>
    ///     Tr? v? k?t qu? t�m ki?m cu?n
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ResponseScroll<T> : Response
    {
        [JsonConstructor]
        public ResponseScroll(Pagination<T> data,
            string scrollId, string scrollTime,
            List<string> listHighlightOtherField)
        {
            Data = data;
            ListHighlightOtherField = listHighlightOtherField;
            ScrollId = scrollId;
            ScrollTime = scrollTime;
        }

        public Pagination<T> Data { get; set; }
        public List<string> ListHighlightOtherField { get; set; }
        public string ScrollId { get; set; }
        public string ScrollTime { get; set; }
    }

    #endregion

    /// <summary>
    ///     �?i tu?ng ph�n trang
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Pagination<T>
    {
        public Pagination()
        {
            Size = 20;
            Page = 1;
        }

        /// <summary>
        ///     V? tr� trang hi?n t?i
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        ///     T?ng s? trang to�n h? th?ng
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        ///     S? lu?ng b?n ghi tr�n m?t trang
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        ///     S? lu?ng b?n ghi tr? v?
        /// </summary>
        public int NumberOfElements { get; set; }

        /// <summary>
        ///     T?ng s? b?n ghi t�m ki?m du?c
        /// </summary>
        public int TotalElements { get; set; }

        /// <summary>
        ///     Danh s�ch d? li?u tr? v?
        /// </summary>
        public List<T> Content { get; set; }
    }

    /// <summary>
    ///     �?i tu?ng m� tr? v?
    /// </summary>
    public enum Code
    {
        Success = 200,
        Unauthorized = 401,
        BadRequest = 400,
        Forbidden = 403,
        NotFound = 404,
        MethodNotAllowed = 405,
        ServerError = 500,
        Conflict = 409
    }

    /// <summary>
    ///     �?i tu?ng k?t qu? c?p nh?t
    /// </summary>
    public class ResponseUpdateModel
    {
        public Guid Id { get; set; }
    }

    /// <summary>
    ///     �?i tu?ng k?t qu? x�a
    /// </summary>
    public class ResponseDeleteModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    ///     �?i tu?ng chung
    /// </summary>
    public class BaseObject
    {
        public DateTime Modified { get; set; }
        public string Modifier { get; set; }
    }

    public class PagingViewModel
    {
        public PagingViewModel()
        {
            Size = 20;
            Page = 1;
        }

        /// <summary>
        ///     V? tr� trang hi?n t?i
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        ///     S? trang hi?n th?
        /// </summary>
        public int NumberPageShow { get; set; }

        /// <summary>
        ///     T?ng s? trang to�n h? th?ng
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        ///     S? lu?ng b?n ghi tr�n m?t trang
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        ///     S? lu?ng b?n ghi tr? v?
        /// </summary>
        public int NumberOfElements { get; set; }

        /// <summary>
        ///     T?ng s? b?n ghi t�m ki?m du?c
        /// </summary>
        public int TotalElements { get; set; }

        /// <summary>
        /// loai phan trang
        /// 1- phan trang posts
        /// 2- phan trang comment
        /// </summary>
        public int PagingType { get; set; }

        public string JavascriptFunction { get; set; }
    }
}
