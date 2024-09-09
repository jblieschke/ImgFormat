using System;
using Xunit;
using Xunit.Abstractions;
using ImgFormat;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;

namespace ImgFormat.Tests;

public class DatabaseTests
{
    private static readonly Byte[] TEST_BLOB = { 0xDE, 0xAD, 0xCA, 0xFE };

    private static HttpContext CreateMockHttpContext() =>
        new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
            Response = {
                Body = new MemoryStream(),
            },
        };

    private readonly ITestOutputHelper Output;

    public DatabaseTests(ITestOutputHelper output)
    {
        Output = output;
    }

    [Fact]
    public void CreateDb()
    {
        using (var db = Database.GetConnection(db_name: "DBTest_GetConnection"))
        {
            Assert.NotNull(db);
        }
    }

    [Fact]
    public void NamedDBs()
    {
        using (var db1 = Database.InitDB(db_name: "InitializedDB"))
        {
            db1.Open();
            var cmd1 = db1.CreateCommand();
            cmd1.CommandText = @"SELECT name, value FROM _Variables";
            using (var reader = cmd1.ExecuteReader())
            {
                Assert.True(reader.HasRows);
                while (reader.Read())
                {
                    var name = reader.GetString(0);
                    var val = reader.GetString(0);
                    Output.WriteLine($"{name}: {val}");
                }
            }
            using (var db2 = Database.GetConnection(db_name: "EmptyDB"))
            {
                db2.Open();
                var cmd2 = db2.CreateCommand();
                cmd2.CommandText = @"SELECT name, value FROM _Variables";
                try
                {
                    using (var reader = cmd2.ExecuteReader())
                    {
                        Assert.False(reader.HasRows);
                    }
                    Assert.Fail("Query should throw SqliteException");
                }
                catch (SqliteException e)
                {
                    Output.WriteLine(e.ToString());
                }
            }
        }
    }

    [Fact]
    public void CreateImage()
    {
        var db_name = "DBTest_CreateImage";
        using (var db = Database.InitDB(db_name: db_name))
        {
            var result = Database.CreateImage("test image", db_name: db_name);
            Assert.NotNull(result);
        }
    }

    [Fact]
    public void StoreImageFile()
    {
        var db_name = "DBTest_StoreImageFile";
        using (var db = Database.InitDB(db_name: db_name))
        {
            var id = (Int64)Database.CreateImage("test image", db_name: db_name);
            Database.StoreImageFile(id, "none", "test", TEST_BLOB, db_name: db_name);
        }
    }

    [Fact]
    public void ListImages()
    {
        var db_name = "DBTest_ListImages";
        using (var db = Database.InitDB(db_name: db_name))
        {
            var id1 = Database.CreateImage("image1", db_name: db_name);
            Assert.NotNull(id1);
            var id2 = Database.CreateImage("image2", db_name: db_name);
            Assert.NotNull(id2);
            var Images = Database.ListImages(db_name: db_name);
            var result = Images.Count();
            Assert.Equal(2, result);
        }
    }

    [Fact]
    public void ListFiles()
    {
        var db_name = "DBTest_ListFiles";
        using (var db = Database.InitDB(db_name: db_name))
        {
            var id = (Int64)Database.CreateImage("test", db_name: db_name);
            Database.StoreImageFile(id, "file1", "test", TEST_BLOB, db_name: db_name);
            Database.StoreImageFile(id, "file2", "test", TEST_BLOB, db_name: db_name);
            var files = Database.ListFiles(db_name: db_name);
            var result = files.Count();
            Assert.Equal(2, result);
        }
    }

    [Fact]
    public async Task GetImageFile()
    {
        var db_name = "DBTest_GetImageFile";
        using (var db = Database.InitDB(db_name: db_name))
        {
            var id = (Int64)Database.CreateImage("test", db_name: db_name);
            Database.StoreImageFile(id, "none", "test", TEST_BLOB, db_name: db_name);
            var files = Database.ListFiles(db_name: db_name);
            var test_file = files.First();
            var name = test_file.Item1;
            var res = test_file.Item3;
            var type = test_file.Item4;
            var response = Database.GetImageFile(name, res, type, db_name: db_name);

            var ctx = CreateMockHttpContext();
            await response.ExecuteAsync(ctx);
            Assert.Equal(200, ctx.Response.StatusCode);
            // TODO: more thorough tests?
        }
    }

    [Fact]
    public async Task NoSuchFile()
    {
        var db_name = "DBTest_NoSuchFile";
        using (var db = Database.InitDB(db_name: db_name))
        {
            var response = Database.GetImageFile("none", "none", "none", db_name: db_name);
            var ctx = CreateMockHttpContext();
            await response.ExecuteAsync(ctx);
            Assert.Equal(404, ctx.Response.StatusCode);
        }
    }

    [Fact]
    public void ImageLimitTrigger()
    {
        var db_name = "DBTest_ImageLimitTrigger";
        int limit = 1;
        using (var db = Database.InitDB(db_name: db_name, limit: limit))
        {
            var id1 = Database.CreateImage("test1", db_name: db_name);
            var id2 = Database.CreateImage("test2", db_name: db_name);
            var id3 = Database.CreateImage("test3", db_name: db_name);
            var images = Database.ListImages(db_name: db_name);
            foreach (var i in images)
            {
                Output.WriteLine($"{i}");
            }
            var count = images.Count();
            var desc = images.First().Item2;
            Assert.Equal(limit, count);
            Assert.Equal("test3", desc);
        }
    }
}
