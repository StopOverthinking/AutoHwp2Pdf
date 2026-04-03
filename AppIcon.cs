using System.Drawing;
using System.Windows.Forms;

namespace AutoHwp2Pdf;

internal static class AppIcon
{
    private static readonly Lazy<Icon> Shared = new(CreateSharedIcon);

    public static Icon Create()
    {
        return (Icon)Shared.Value.Clone();
    }

    private static Icon CreateSharedIcon()
    {
        try
        {
            var executablePath = Application.ExecutablePath;
            if (!string.IsNullOrWhiteSpace(executablePath) && File.Exists(executablePath))
            {
                using var extractedIcon = Icon.ExtractAssociatedIcon(executablePath);
                if (extractedIcon is not null)
                {
                    return (Icon)extractedIcon.Clone();
                }
            }
        }
        catch
        {
        }

        return (Icon)SystemIcons.Application.Clone();
    }
}
