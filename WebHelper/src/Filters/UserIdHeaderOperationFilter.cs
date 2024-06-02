using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebHelper.Filters;

public class UserIdHeaderOperationFilter : IOperationFilter
{
  public void Apply(OpenApiOperation operation, OperationFilterContext context)
  {
    operation.Parameters ??= [];
    operation.Parameters.Add(new OpenApiParameter
    {
      Name = "X-UserId",
      In = ParameterLocation.Header,
      Required = true,
      Schema = new OpenApiSchema
      {
        Type = "string"
      }
    });
  }
}
