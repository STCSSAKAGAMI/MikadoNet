using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using MikadoNet.Models;

namespace MikadoNet.Services;

public interface ICurrentUserService
{
    Task<short> GetRequiredTantoCodeAsync(CancellationToken cancellationToken);
}

public class CurrentUserService : ICurrentUserService
{
    private const string TantoClaimType = "tanto_code";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<AppUser> _userManager;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, UserManager<AppUser> userManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    public async Task<short> GetRequiredTantoCodeAsync(CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user is null || user.Identity?.IsAuthenticated != true)
        {
            throw new InvalidOperationException("認証ユーザーが必要です。");
        }

        var claimValue = user.FindFirstValue(TantoClaimType);
        if (short.TryParse(claimValue, out var tantoCode))
        {
            return tantoCode;
        }

        var appUser = await _userManager.GetUserAsync(user);
        if (appUser?.TantoCode is null)
        {
            throw new InvalidOperationException("TantoCodeが設定されていないためJournal操作ができません。");
        }

        return appUser.TantoCode.Value;
    }
}
