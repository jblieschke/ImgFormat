using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace ImgFormat.Pages;

public class GalleryModel : PageModel
{
    public class ImageLinks
    {
        public string Description { get; }
        public string Thumbnail { get; set; }
        public ICollection<Tuple<string, string>> Files { get; }

        public ImageLinks(string description)
        {
            Description = description;
            Thumbnail = "";
            Files = new List<Tuple<string, string>>();
        }
    }

    private readonly ILogger<GalleryModel> _logger;

    public GalleryModel(ILogger<GalleryModel> logger)
    {
        _logger = logger;
    }

    public IDictionary<string, ImageLinks> GetImages(string db_name = Database.DB_NAME)
    {
        var result = new Dictionary<string, ImageLinks>();
        var rows = Database.ListFiles(db_name: db_name);
        foreach (var r in rows)
        {
            var name = r.Item1;
            var desc = r.Item2;
            var res = r.Item3;
            var type = r.Item4;
            if (!result.ContainsKey(name))
            {
                result.Add(name, new ImageLinks(desc));
            }
            if (res == "thumb")
            {
                if (type == "image/png")
                {
                    result[name].Thumbnail = Images.FileName(name, res, type);
                }
            }
            else
            {
                result[name].Files.Add(new Tuple<string, string>(res, type));
            }
        }
        return result;
    }
}
