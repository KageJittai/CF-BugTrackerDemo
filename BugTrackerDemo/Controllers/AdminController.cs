using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Owin;
using BugTrackerDemo.Models;
using System.Web.Security;
using System.Net;

namespace BugTrackerDemo.Controllers
{
    public class AdminController : BaseController
    {
        public ActionResult UserList()
        {
            List<UserModel> myUserList = db.UserModels.ToList();

            List<UserViewModel> returnList = new List<UserViewModel>();

            foreach(UserModel item in myUserList)
            {
                UserViewModel thisItem = new UserViewModel();
                thisItem.Id = item.Id;
                thisItem.Email = item.Email;
                thisItem.FirstName = item.FirstName;
                thisItem.LastName = item.LastName;
                returnList.Add(thisItem);
            }

            return View(returnList);
        }

        public ActionResult ManageUserProjects(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var user = db.UserModels.Where(m=>m.Id == id).ToList().FirstOrDefault();
            if (user == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var userProjectsList = db.UserProjectRoles.Where(m => m.UserId == id).ToList();
            var projectList = db.Projects.ToList();


            return View();
        }
    }
}