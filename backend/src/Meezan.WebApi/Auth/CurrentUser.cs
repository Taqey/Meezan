using System.Security.Claims;
using Meezan.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Meezan.WebApi.Auth;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? UserName;

    public string? UserName =>
        _httpContextAccessor.HttpContext?.User?.Identity?.Name
        ?? (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true ? "admin" : null);

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
