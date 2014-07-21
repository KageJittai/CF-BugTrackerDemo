using System.Web;
using System.Web.Mvc;
using BugTrackerDemo.App_Start;

namespace BugTrackerDemo
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new UserInfoAttribute());
        }
    }
}
