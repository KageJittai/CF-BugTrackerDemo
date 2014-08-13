using BugTrackerDemo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Security;
using System.Security.Principal;
using System.Web;
using Microsoft.AspNet.Identity;
using System.Web.UI;
using System.Web.Mvc.Filters;
using System.Net;
using BugTrackerDemo.Controllers;

namespace BugTrackerDemo.Common
{
    class AdminRequired : FilterAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            BaseController Context = (BaseController)filterContext.Controller;

            if (!(Context.IsLoggedIn && Context.CurrentUser.IsAdmin))
            {
                filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }
        }
    }

    class SubmitterRequired : FilterAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            BaseController Context = (BaseController)filterContext.Controller;

            if (!(Context.IsLoggedIn && Context.CurrentUser.IsSubmitter))
            {
                filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }
        }
    }

    class DeveloperRequired : FilterAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            BaseController Context = (BaseController)filterContext.Controller;

            if (!(Context.IsLoggedIn && Context.CurrentUser.IsDeveloper))
            {
                filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }
        }
    }

    class ManagerRequired : FilterAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            BaseController Context = (BaseController)filterContext.Controller;

            if (!(Context.IsLoggedIn && Context.CurrentUser.IsManager))
            {
                filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }
        }
    }

    class UserInfoAttribute : FilterAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            BaseController Context = (BaseController)filterContext.Controller;

            if (HttpContext.Current.User.Identity.IsAuthenticated)
            {
                Context.createUserData(HttpContext.Current.User.Identity.Name);
            }
        }
    }
}
