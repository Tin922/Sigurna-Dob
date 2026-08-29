using MudBlazor;

namespace SigurnaDob.App.Theme;

public static class SigurnaDobTheme
{
    public static MudTheme Theme { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#2D6A6A",
            PrimaryDarken = "#1E4545",
            PrimaryLighten = "#4A8F8F",
            Secondary = "#C8956C",
            SecondaryDarken = "#A67548",
            SecondaryLighten = "#E0B896",
            Tertiary = "#7BA387",
            Background = "#FAF7F2",
            Surface = "#FFFFFF",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#1E4545",
            DrawerBackground = "#1E4545",
            DrawerText = "#E8F0EF",
            DrawerIcon = "#A8CFC9",
            TextPrimary = "#2C3333",
            TextSecondary = "#5C6B6B",
            ActionDefault = "#5C6B6B",
            Divider = "#E5DDD3",
            TableLines = "#EDE6DC",
            LinesDefault = "#E5DDD3",
            Success = "#5A9E6F",
            Warning = "#D4A04A",
            Error = "#C45C5C",
            Info = "#4A8F8F"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Plus Jakarta Sans", "Segoe UI", "sans-serif"]
            },
            H4 = new H4Typography
            {
                FontWeight = "600",
                LetterSpacing = "-0.02em"
            },
            H5 = new H5Typography
            {
                FontWeight = "600"
            },
            H6 = new H6Typography
            {
                FontWeight = "600"
            },
            Button = new ButtonTypography
            {
                TextTransform = "none",
                FontWeight = "600"
            }
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px"
        }
    };
}
