using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace BV.Web.Services;

public sealed class AuthSessionStore(ProtectedLocalStorage storage)
{
    private const string StorageKey = "bvportal.auth.session";

    public async Task SaveAsync(AuthSession session)
    {
        var snapshot = session.Export();
        if (snapshot is null)
        {
            await ClearAsync();
            return;
        }

        await storage.SetAsync(StorageKey, snapshot);
    }

    public async Task<bool> RestoreAsync(AuthSession session)
    {
        try
        {
            var result = await storage.GetAsync<AuthSessionSnapshot>(StorageKey);
            if (!result.Success || result.Value is null)
                return false;

            session.Restore(result.Value);
            return session.IsAuthenticated || session.CanRefresh;
        }
        catch (InvalidOperationException)
        {
            // Browser storage is unavailable during server-side prerendering.
            return false;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            await ClearAsync();
            return false;
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            await storage.DeleteAsync(StorageKey);
        }
        catch (InvalidOperationException)
        {
            // Browser storage is unavailable during server-side prerendering.
        }
    }
}
