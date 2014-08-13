using BugTrackerDemo.Common;
using BugTrackerDemo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BugTrackerDemo.Controllers
{
    public class BaseController : Controller
    {
        protected BugTrackerEntities db;

        public UserData CurrentUser { get; private set; }
        public bool IsLoggedIn { get; private set; }

        public BaseController()
            : base()
        {
            db = new BugTrackerEntities();
            IsLoggedIn = false;
            CurrentUser = new UserData();

            ViewBag.isLoggedIn = false;
            ViewBag.page = CurrentUser;
        }

        public void createUserData(string email)
        {
            IsLoggedIn = true;
            //CurrentUser = new UserData();
                                
            /**********************
             *   User Data Setup  *
             **********************/

            // Get custom user data from AspUsers
            var DbUser = db.UserModels.Include("UserProjectRoles")
                           .Include("UserProjectRoles.Project")
                           .Where(m => m.Email == email).First();

            CurrentUser.UserId = DbUser.Id;
            CurrentUser.FirstName = DbUser.FirstName;
            CurrentUser.LastName = DbUser.LastName;
            CurrentUser.IsAdmin = DbUser.Admin;

            /*************************
             *   Project Data Setup  *
             *************************/

            // Build a projectmodel list
            foreach (var project in DbUser.UserProjectRoles)
            {
                CurrentUser.UserProjectList[project.Project.Id] = project.Project.Name;
            }

            int? projectId = (int?)Session["Project"];

            if (projectId == null && CurrentUser.UserProjectList.Count > 0)
            {
                // If no project is set and the user has projects, set da first one
                projectId = CurrentUser.UserProjectList.First().Key;
                Session["Project"] = projectId;
            }

            if (projectId != null)
            {
                if (!CurrentUser.UserProjectList.ContainsKey((int)projectId))
                {
                    // We have a Project set, but we aren't a member of said project
                    projectId = null;
                    Session["Project"] = null;
                }
                else
                {
                    // Determine the user's roles for his current project
                    if (DbUser.UserProjectRoles.FirstOrDefault(x => x.ProjectId == projectId && x.Role.Role1 == "Manager") != null)
                        CurrentUser.IsManager = true;

                    if (DbUser.UserProjectRoles.FirstOrDefault(x => x.ProjectId == projectId && x.Role.Role1 == "Developer") != null)
                        CurrentUser.IsDeveloper = true;

                    if (DbUser.UserProjectRoles.FirstOrDefault(x => x.ProjectId == projectId && x.Role.Role1 == "Submitter") != null)
                        CurrentUser.IsSubmitter = true;

                    CurrentUser.ProjectName = CurrentUser.UserProjectList[(int)projectId];
                    CurrentUser.ProjectId = projectId;
                }
            }

            ViewBag.isLoggedIn = true;
            ViewBag.page = CurrentUser;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

    }
}