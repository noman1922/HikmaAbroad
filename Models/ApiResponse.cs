namespace HikmaAbroad.Models;

/// <summary>
/// Standard API response wrapper.
/// </summary>
/// <typeparam name="T">Type of the data payload</typeparam>
public class ApiResponse<T>
{
    /// <summary>Whether the request was successful</summary>
    public bool Success { get; set; }

    /// <summary>Response data payload</summary>
    public T? Data { get; set; }

    /// <summary>Error message if unsuccessful</summary>
    public string? Error { get; set; }

    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };
    public static ApiResponse<T> Fail(string error) => new() { Success = false, Error = error };
}

/// <summary>
/// Paged response wrapper.
/// </summary>
public class PagedResponse<T>
{
    public bool Success { get; set; } = true;
    public List<T> Data { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public long TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public string? Error { get; set; }
}
