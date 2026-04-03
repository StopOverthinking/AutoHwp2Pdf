namespace AutoHwp2Pdf;

public enum OutputFormat
{
    Pdf = 0,
    Docx = 1,
    Png = 2,
}

public static class OutputFormatExtensions
{
    public static string GetExtension(this OutputFormat format)
    {
        return format switch
        {
            OutputFormat.Docx => "docx",
            OutputFormat.Png => "png",
            _ => "pdf",
        };
    }

    public static string GetDefaultSubfolderName(this OutputFormat format)
    {
        return format.GetExtension();
    }

    public static string[] GetSaveFormatCodes(this OutputFormat format)
    {
        return format switch
        {
            OutputFormat.Docx => ["DOCX", "OOXML"],
            OutputFormat.Png => ["PNG"],
            _ => ["PDF"],
        };
    }
}
