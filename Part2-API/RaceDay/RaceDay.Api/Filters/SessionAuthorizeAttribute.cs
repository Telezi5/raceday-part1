using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Filters;

/// <summary>
/// Protects an endpoint. Returns 401 if the caller is not logged in,
/// and 403 if a Role is specified and the caller's role does not match.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public string? Role { get; set; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var userId = context.HttpContext.Session.GetInt32("UserId");
        var role = context.HttpContext.Session.GetString("Role");

        if (userId == null)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Please log in first." });
            return;
        }

        if (Role != null && role != Role)
        {
            context.Result = new ObjectResult(new { message = $"This action is for {Role}s only." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}