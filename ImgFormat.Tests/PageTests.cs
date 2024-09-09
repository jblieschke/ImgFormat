using System;
using Xunit;
using Xunit.Abstractions;
using ImgFormat;
using ImgFormat.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Testing;

namespace ImgFormat.Tests;

public class PageTests
{
    private class TestFile : IFormFile
    {
        private FileStream stream;

        // IFormFile
        public string ContentDisposition { get; }
        public string ContentType { get; }
        public string FileName { get; }
        public IHeaderDictionary Headers { get; }
        public long Length { get; }
        public string Name { get; }

        public TestFile(string file)
        {
            var options = new FileStreamOptions();
            options.Access = FileAccess.Read;
            options.Mode = FileMode.Open;
            stream = new FileStream(file, options);

            ContentDisposition = "";
            ContentType = "application/octet-stream";  // could this be better?
            FileName = file;
            Headers = new HeaderDictionary();
            Length = stream.Length;
            Name = "";
        }

        public void CopyTo(Stream target)
        {
            stream.CopyTo(target);
        }

        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        {
            return stream.CopyToAsync(target, cancellationToken);
        }

        public Stream OpenReadStream()
        {
            return stream;
        }
    }

    private readonly ITestOutputHelper Output;

    public PageTests(ITestOutputHelper output)
    {
        Output = output;
    }

    [Fact]
    public async Task IndexProcessImage()
    {
        var db_name = "PageTest_IndexProcessImage";
        var test_image = new TestFile("./Resources/phone-mosaic-grey.png");
        var test_page = new IndexModel(new FakeLogger<IndexModel>());
        using (var db = Database.InitDB(db_name: db_name))
        {
            var img_id = (Int64)Database.CreateImage("test", db_name: db_name);
            await test_page.ProcessImage(img_id, test_image, db_name: db_name);
            var files = Database.ListFiles(db_name: db_name);
            var count = files.Count();

            foreach (var f in files)
            {
                Output.WriteLine($"{f.ToString()}");
            }
            Assert.Equal(6, count);
        }
    }

    [Fact]
    public async Task GalleryGetImages()
    {
        var db_name = "PageTest_GalleryGetImages";
        var test_image = new TestFile("./Resources/phone-mosaic-grey.png");
        var test_index = new IndexModel(new FakeLogger<IndexModel>());
        var test_gallery = new GalleryModel(new FakeLogger<GalleryModel>());
        using (var db = Database.InitDB(db_name))
        {
            var img_id = (Int64)Database.CreateImage("test", db_name: db_name);
            await test_index.ProcessImage(img_id, test_image, db_name: db_name);
            var images = test_gallery.GetImages(db_name: db_name);

            var image_count = images.Count;
            var image_obj = images.Values.First();
            var thumbnail_path = image_obj.Thumbnail;
            var file_count = image_obj.Files.Count;
            foreach (var i in images)
            {
                Output.WriteLine($"{i.Key}");
                foreach (var f in i.Value.Files)
                {
                    Output.WriteLine($"{i.Key}:{f.ToString()}");
                }
            }
            Assert.Equal(1, image_count);
            Assert.NotEqual("", thumbnail_path);
            Assert.Equal(4, file_count);
        }
    }
}
