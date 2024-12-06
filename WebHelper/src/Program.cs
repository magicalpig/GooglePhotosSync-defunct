using WebHelper.Filters;
using WebHelper.Middleware;
using WebHelper.Services;
using MyHostBuilder = VideoToolsCore.Services.HostBuilder;
using WebHelper;
using VideoToolsCore.User.Services;

var app =  MyHostBuilder.CreateWebHost<Settings>(services =>
{
    services
        .AddScoped<IGoogleCurrentUserService, WebApiCurrentUserService>()
        .AddScoped<IDualCurrentUserService, DualCurrentUserService>()
        .AddEndpointsApiExplorer()
        .AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "WebHelper", Version = "v1" });
            c.OperationFilter<UserIdHeaderOperationFilter>();
        })
        .AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigin",
                builder =>
                {
                    builder.WithOrigins("https://photos.google.com")
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
        })
        // my own extension method that registers the db context factory, 
        // a current user service, and of course the WebApiVideoService
        .AddWebApiVideoService()
        // give controllers and services access the HttpContext
        // (middleware already has access)
        .AddHttpContextAccessor()
        .AddControllers();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowSpecificOrigin");
app.MapControllers();
app.UseMiddleware<UserIdExtractionMiddleware>();

app.Run();