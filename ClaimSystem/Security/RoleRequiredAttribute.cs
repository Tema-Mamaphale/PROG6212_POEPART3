using System;
using System.Linq;
using ClaimSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;

namespace ClaimSystem.Security
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class RoleRequiredAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _allowed;

        public RoleRequiredAttribute(params string[] allowedRoles)
        {
            _allowed = allowedRoles ?? Array.Empty<string>();
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (_allowed.Length == 0)
                return;

            var httpContext = context.HttpContext;
            var role = httpContext.Session.GetString("UserRole");

            if (string.IsNullOrWhiteSpace(role))
            {
                context.Result = new RedirectToActionResult(
                    actionName: "SelectRole",
                    controllerName: "Home",
                    routeValues: null
                );
                return;
            }

            var match = _allowed.Contains(role, StringComparer.OrdinalIgnoreCase);
            if (!match)
            {
                httpContext.Items["RequiredRoles"] = string.Join(", ", _allowed);

                context.Result = new RedirectToActionResult(
                    actionName: "AccessDenied",
                    controllerName: "Home",
                    routeValues: null
                );
            }
        }
    }
}
