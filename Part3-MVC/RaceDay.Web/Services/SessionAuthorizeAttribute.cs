using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Services;

public class SessionAuthorizeAttribute : ActionFilterAttribute
{
    public string? Role { get; set; }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        var role = session.GetString("Role");

        if (string.IsNullOrEmpty(role))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        if (Role != null && !string.Equals(role, Role, StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
    }
}