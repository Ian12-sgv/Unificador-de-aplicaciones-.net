namespace Appinventario.Services
{
    /// <summary>
    /// Lee los colores de Resources/Styles/Colors.xaml para usarlos desde código.
    /// </summary>
    public static class AppColors
    {
        public static Color Get(string key)
        {
            if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color)
                return color;

            return Colors.Transparent;
        }

        public static Color Brand => Get("Brand");
        public static Color Primary => Get("Primary");
        public static Color PrimaryContainer => Get("PrimaryContainer");
        public static Color PrimaryOutline => Get("PrimaryOutline");
        public static Color OnPrimary => Get("OnPrimary");
        public static Color Surface => Get("Surface");
        public static Color SurfaceMuted => Get("SurfaceMuted");
        public static Color Outline => Get("Outline");
        public static Color TextPrimary => Get("TextPrimary");
        public static Color TextSecondary => Get("TextSecondary");
        public static Color Error => Get("Error");
        public static Color ErrorContainer => Get("ErrorContainer");
        public static Color ErrorOutline => Get("ErrorOutline");
        public static Color Warning => Get("Warning");
        public static Color WarningContainer => Get("WarningContainer");
        public static Color Info => Get("Info");
        public static Color InfoContainer => Get("InfoContainer");
    }

    /// <summary>
    /// Lee la escala de Resources/Styles/Typography.xaml para usarla desde código.
    /// </summary>
    public static class AppFonts
    {
        private static double Get(string key, double fallback)
        {
            if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is double size)
                return size;

            return fallback;
        }

        public static double Subtitle => Get("FontSubtitle", 16);
        public static double Body => Get("FontBody", 14);
        public static double Caption => Get("FontCaption", 12);
    }
}
