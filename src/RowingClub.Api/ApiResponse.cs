namespace RowingClub.Api;

public sealed record ApiResponse<T>(bool Success, T? Data, string? Message, string? Code)
{
    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new(true, data, message, null);

    public static ApiResponse<T> Fail(string code, string message) =>
        new(false, default, message, code);
}

public sealed record ApiResponse(bool Success, string? Message, string? Code)
{
    public static ApiResponse Ok(string? message = null) =>
        new(true, message, null);

    public static ApiResponse Fail(string code, string message) =>
        new(false, message, code);
}
