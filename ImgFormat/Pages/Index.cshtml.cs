using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Primitives;
using SixLabors.ImageSharp;

namespace ImgFormat.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public void LogRequest(HttpRequest request)
    {
        foreach (var item in request.Form)
        {
            try
            {
                _logger.LogInformation($"{item.Key}: {item.Value.ToString()}");
            }
            catch
            {
                _logger.LogWarning($"Value of \"{item.Key}\" cannot be displayed.");
            }
        }
        foreach (var file in request.Form.Files)
        {
            _logger.LogInformation(String.Format(
                "FileName: {0} | Name: {1} | Type: {2}",
                file.FileName,
                file.Name,
                file.ContentType
            ));
        }
    }

    public string HandleRequest(HttpRequest request)
    {
        StringValues desc_value;
        request.Form.TryGetValue("desc", out desc_value);
        var desc = desc_value.ToString();
        if (desc.Length > 250)
        {
            return "Description too long";
        }

        var file = request.Form.Files.GetFile("image");
        if (file == null)
        {
            return "File is missing";
        }
        if (file.Length > 10485760)
        {
            return "File is too big";
        }
        if (!Images.IsSupportedType(file.ContentType))
        {
            return "File format is not supported";
        }

        Int64? image_id = Database.CreateImage(desc);
        if (image_id == null)
        {
            return "Upload Failed, Try Again Later";
        }
        ProcessImage((Int64)image_id, file);
        return "Upload Successful";
    }

    public async Task ProcessImage(Int64 id, IFormFile file, string db_name = Database.DB_NAME)
    {
        using (var image = Images.Decode(file))
        {
            var tasks = new List<Task>();
            foreach (var format in Images.Formats.Keys)
            {
                tasks.Add(Task.Run(() => {
                    var res = $"{image.Width}x{image.Height}";
                    Database.StoreImageFile(id, res, format.DefaultMimeType,
                        Images.ResizeAndEncode(image, image.Width, image.Height, format),
                        db_name: db_name);
                }));
                tasks.Add(Task.Run(() => {
                    var res = $"{image.Width/2}x{image.Height/2}";
                    Database.StoreImageFile(id, res, format.DefaultMimeType,
                        Images.ResizeAndEncode(image, image.Width/2, image.Height/2, format),
                        db_name: db_name);
                }));
                // generate a thumbnail such that the largest dimension is 100px
                int thumb_width = (image.Width >= image.Height ? 100 : 100 * (image.Width / image.Height));
                int thumb_height = (image.Width >= image.Height ? 100 * (image.Height / image.Width) : 100);
                tasks.Add(Task.Run(() => {
                    Database.StoreImageFile(id, "thumb", format.DefaultMimeType,
                        Images.ResizeAndEncode(image, thumb_width, thumb_height, format),
                        db_name: db_name);
                }));
            }
            await Task.WhenAll(tasks);
        }
    }
}
