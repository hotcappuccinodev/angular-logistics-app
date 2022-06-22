using Duende.IdentityServer.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Reflection;

namespace Logistics.IdentityServer.Pages.Home;

[AllowAnonymous]
public class Index : PageModel
{
    public string Version;

    public void OnGet()
    {
        Version = typeof(IdentityServerMiddleware).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+').First();
    }
}