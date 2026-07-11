using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace EventDrivenArchitecturePlayground.API.Controllers;

public abstract class BaseController<T> : Controller
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool IsUserAuth()
    {
        if (User is null || User.Identity is null)
        {
            return false;
        }

        return User.Identity.IsAuthenticated;
    }
}