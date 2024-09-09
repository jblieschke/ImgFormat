// Create a database connection, and initialise the database.
/*
This service uses an in-memory SQLite database, which allows multiple
connections, and remains active as long as there is one active connection.
This particular connection is only used for initialization during startup,
but it remains open for the entire lifetime of the process, so that the data
persists.
// */
var db = ImgFormat.Database.InitDB();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

// returns images when requested?
app.MapGet("/{name}-{res}.{ext}",
    async (string name, string res, string ext) =>
        await Task.Run(() => {
            var type = ImgFormat.Images.TypeFromExtension(ext);
            return ImgFormat.Database.GetImageFile(name, res, type);
        })
);

app.Run();
