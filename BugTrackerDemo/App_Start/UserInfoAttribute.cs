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

namespace BugTrackerDemo.App_Start
{
    class UserInfoAttribute : FilterAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            var ViewBag = filterContext.Controller.ViewBag;

            ViewBag.page = null;
            ViewBag.isLoggedIn = false;

            if (HttpContext.Current.User.Identity.IsAuthenticated)
            {
                ViewBag.isLoggedIn = true;

                // Add a accessor so things don't get so wordy
                var Session = HttpContext.Current.Session;

                PageDataModel page = new PageDataModel();

                var btdb = new BugTrackerEntities();
                string email = HttpContext.Current.User.Identity.Name;

                /**********************
                 *   User Data Setup  *
                 **********************/

                // Get custom user data from AspUsers
                var CurrentUser = btdb.UserModels.Where(m => m.Email == email).FirstOrDefault();

                if (CurrentUser == null)
                    page.UserDisplayName = HttpContext.Current.User.Identity.Name;
                else
                    page.UserDisplayName = CurrentUser.FirstName + " " + CurrentUser.LastName;

                if (Session["User"] == null)
                {
                    Session["User"] = CurrentUser.Id;
                }

                page.UserId = (int)Session["User"];

                /*************************
                 *   Project Data Setup  *
                 *************************/

                // find all projects CurrentUser is a member of
                var query = from l in CurrentUser.UserProjectRoles
                            from p in btdb.Projects
                            where l.ProjectId == p.Id
                            select new { Project = p };

                var projectList = query.ToList();

                // Build a projectmodel list
                foreach (var project in projectList)
                {
                    // Sometimes a user might have more then one role in a project, so don't add the user to the project list twice
                    var listItem = new PageDataModel.ProjectListItem{ id = project.Project.Id, name = project.Project.Name };
                    if (!page.UserProjectList.Contains(listItem))
                        page.UserProjectList.Add(new PageDataModel.ProjectListItem{ id = project.Project.Id, name = project.Project.Name });
                }

                if (Session["Project"] == null && projectList.Count > 0)
                {
                    Session["Project"] = projectList.FirstOrDefault().Project.Id;
                }

                if (Session["Project"] != null)
                    page.ProjectId = (int)Session["Project"];
                else
                    page.ProjectId = null;

                if (page.ProjectId != null)
                {
                    // Determine the user's roles for his current project
                    if (CurrentUser.UserProjectRoles.FirstOrDefault(x => x.ProjectId == page.ProjectId && x.Role.Role1 == "Manager") != null)
                        page.IsManager = true;

                    if (CurrentUser.UserProjectRoles.FirstOrDefault(x => x.ProjectId == page.ProjectId && x.Role.Role1 == "Developer") != null)
                        page.IsDeveloper = true;

                    if (CurrentUser.UserProjectRoles.FirstOrDefault(x => x.ProjectId == page.ProjectId && x.Role.Role1 == "Submitter") != null)
                        page.IsSubmitter = true;
                }

                // Assign a display name to the current project.
                page.ProjectName = Session["Project"] == null ? "No Projects" : projectList.Find(m => m.Project.Id == ((int)Session["Project"])).Project.Name;

                ViewBag.page = page;
            }
        }
    }
}
