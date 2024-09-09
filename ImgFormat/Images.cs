using Microsoft.AspNetCore.Http;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace ImgFormat;

public static class Images
{
    // TODO: use ImageSharp's Configuration / ImageFormatManager instead?
    // Depends on how thread-safe it is. This is "good enough".
    public static readonly Dictionary<IImageFormat, IImageEncoder> Formats
        = new Dictionary<IImageFormat, IImageEncoder>
            {
                { PngFormat.Instance, new PngEncoder() },
                { JpegFormat.Instance, new JpegEncoder() }
            };

    public static bool IsSupportedType(string type)
    {
        foreach (var format in Formats.Keys)
        {
            if (format.MimeTypes.Contains(type))
            {
                return true;
            }
        }
        return false;
    }

    public static string TypeFromExtension(string ext)
    {
        foreach (var format in Formats.Keys)
        {
            if (format.FileExtensions.Contains(ext))
            {
                return format.DefaultMimeType;
            }
        }
        return "";
    }

    public static string ExtensionFromType(string type)
    {
        foreach (var format in Formats.Keys)
        {
            if (format.MimeTypes.Contains(type))
            {
                return format.FileExtensions.First();
            }
        }
        return "";
    }

    public static string FileName(string name, string res, string type)
    {
        var ext = ExtensionFromType(type);
        return $"{name}-{res}.{ext}";
    }

    public static Image Decode(IFormFile image)
    {
        return Image.Load(image.OpenReadStream());
    }

    public static Byte[] ResizeAndEncode(Image image, int width, int height, IImageFormat format)
    {
        using (Image result = image.Clone(x => x.Resize(width, height)))
        {
            using (var stream = new MemoryStream())
            {
                result.Save(stream, Formats[format]);
                return stream.ToArray();
            }
        }
    }
}
