var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseStaticFiles();

app.MapGet("/", (IWebHostEnvironment env) => {
    var htmlPath = System.IO.Path.Combine(env.ContentRootPath, "wwwroot", "index.html");
    return Results.File(htmlPath, "text/html");
});

app.MapGet("/admin.html", (IWebHostEnvironment env) => {
    var htmlPath = System.IO.Path.Combine(env.ContentRootPath, "wwwroot", "admin.html");
    return Results.File(htmlPath, "text/html");
});

app.UseCors("AllowAll");
app.MapReverseProxy();

app.Run();