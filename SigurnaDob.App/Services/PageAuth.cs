using SigurnaDob.App.Services;
using Microsoft.AspNetCore.Components;

namespace SigurnaDob.App.Services;

public static class PageAuth
{
    public static async Task<bool> EnsureLoggedInAsync(
        CurrentUserService currentUser,
        NavigationManager navigation)
    {
        if (!currentUser.IsInitialized)
            await currentUser.InitializeAsync();

        if (currentUser.IsLoggedIn)
            return true;

        navigation.NavigateTo("/login");
        return false;
    }
}
