using WebHelper.Filters;
using WebHelper.Middleware;
using WebHelper.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services
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
  .AddWebApiVideoService()
  .AddHttpContextAccessor() // lets UserIdExtractionMiddleware access the user id header
  .AddControllers();


var app = builder.Build();
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
