using System;

namespace Sohba.Application.DTOs.Common
{
    public class BaseResponseDto
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        
        public static BaseResponseDto SuccessResponse() => new BaseResponseDto { Success = true, Error = null };
        public static BaseResponseDto FailureResponse(string error) => new BaseResponseDto { Success = false, Error = error };
    }

    public class BaseResponseDto<T> : BaseResponseDto
    {
        public T Data { get; set; }
        
        public static BaseResponseDto<T> SuccessResponse(T data) => new BaseResponseDto<T> { Success = true, Data = data, Error = null };
        public new static BaseResponseDto<T> FailureResponse(string error) => new BaseResponseDto<T> { Success = false, Data = default, Error = error };
    }
}
