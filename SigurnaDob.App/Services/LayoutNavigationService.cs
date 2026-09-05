namespace SigurnaDob.App.Services;

public class LayoutNavigationService
{
    public event Action? DrawerCloseRequested;

    public void RequestDrawerClose() => DrawerCloseRequested?.Invoke();
}
