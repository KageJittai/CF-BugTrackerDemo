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
using BugTrackerDemo.Common;

namespace BugTrackerDemo.Controllers
{
    [AdminRequired]
    public class UserController : BaseController
    {
        public ActionResult List()
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

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var user = db.UserModels
                         .Include("UserProjectRoles")
                         .Include("UserProjectRoles.Role")
                         .Where(m=>m.Id == id).ToList().First();

            var userProjectsList = user.UserProjectRoles.ToList();
            var projectList = db.Projects.ToList();

            var viewmodel = new UserViewModel();
            viewmodel.Id = user.Id;
            viewmodel.Email = user.Email;
            viewmodel.FirstName = user.FirstName;
            viewmodel.LastName = user.LastName;

            foreach (var item in projectList)
            {
                var toAdd = new ProjectItem();
                toAdd.ProjectId = item.Id;
                toAdd.ProjectName = item.Name;
                toAdd.IsManager = userProjectsList.Where(m => m.ProjectId == item.Id && m.Role.Role1 == "Manager").ToList().Count > 0;
                toAdd.IsDeveloper = userProjectsList.Where(m => m.ProjectId == item.Id && m.Role.Role1 == "Developer").ToList().Count > 0;
                toAdd.IsSubmitter = userProjectsList.Where(m => m.ProjectId == item.Id && m.Role.Role1 == "Submitter").ToList().Count > 0;
                viewmodel.ProjectItems.Add(toAdd);
            }

            return View(viewmodel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(UserViewModel newData, List<ProjectItem> ProjectItems)
        {
            newData.ProjectItems = ProjectItems;
            if (ModelState.IsValid)
            {
                var user = db.UserModels.Where(m => m.Id == newData.Id).ToList().First();

                    user.FirstName = newData.FirstName;
                    user.LastName = newData.LastName;
                    db.UserModels.Attach(user);
                    var entry = db.Entry(user);
                    entry.Property(m => m.FirstName).IsModified = true;
                    entry.Property(m => m.LastName).IsModified = true;

                    db.UserProjectRoles.RemoveRange(db.UserProjectRoles.Where(m => m.UserId == user.Id));
                    foreach (var item in newData.ProjectItems)
                    {

                        if (item.IsManager)
                            db.UserProjectRoles.Add(new UserProjectRole
                            {
                                UserId = user.Id,
                                ProjectId = item.ProjectId,
                                RoleId = db.RoleModels.Where(m => m.Role1 == "Manager").First().Id
                            });

                        if (item.IsDeveloper)
                            db.UserProjectRoles.Add(new UserProjectRole
                            {
                                UserId = user.Id,
                                ProjectId = item.ProjectId,
                                RoleId = db.RoleModels.Where(m => m.Role1 == "Developer").First().Id
                            });

                        if (item.IsSubmitter)
                            db.UserProjectRoles.Add(new UserProjectRole
                            {
                                UserId = user.Id,
                                ProjectId = item.ProjectId,
                                RoleId = db.RoleModels.Where(m => m.Role1 == "Submitter").First().Id
                            });
                    }

                db.SaveChanges();

                return RedirectToAction("List");
            }

            return View(newData);
        }
    }
}