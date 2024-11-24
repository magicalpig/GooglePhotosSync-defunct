using VideoToolsCore.DataAccess;
using WebHelper.Filters;
using WebHelper.Middleware;
using WebHelper.Services;
using MyHostBuilder = VideoToolsCore.Services.HostBuilder;
using WebHelper;

var app =  MyHostBuilder.CreateWebHost<Settings>(services =>
{
    services.AddEndpointsApiExplorer()
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
        .AddWebApiVideoService()
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