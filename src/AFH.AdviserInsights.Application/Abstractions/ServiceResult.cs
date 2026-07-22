using System.Net;

namespace AFH.AdviserInsights.Application.Abstractions;

public sealed record ServiceResult<T>(
    bool IsSuccess,
    T? Value,
    HttpStatusCode StatusCode,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    string? ErrorDetail = null)
{
    public static ServiceResult<T> Ok(T value) => new(true, value, HttpStatusCode.OK);

    public static ServiceResult<T> Fail(
        HttpStatusCode statusCode,
        string errorCode,
        string errorMessage,
        string? errorDetail = null)
        => new(false, default, statusCode, errorCode, errorMessage, errorDetail);
}
