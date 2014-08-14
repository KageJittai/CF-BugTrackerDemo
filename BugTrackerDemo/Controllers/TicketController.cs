using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BugTrackerDemo.Models;
using BugTrackerDemo.Common;
using System.Security.Cryptography;
using System.IO;

using PagedList;

namespace BugTrackerDemo.Controllers
{
    public class TicketController : BaseController
    {
        [HttpGet]
        public ActionResult List(int? page)
        {
            page = page ?? 1;

            if (CurrentUser.ProjectId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            int id = (int)CurrentUser.ProjectId;
            var tickets = db.Tickets.Include(t => t.TicketSeverity).Include(t => t.TicketStatus).Include(t => t.Owner).Include(t => t.Assignee).Include(t => t.Project);

            return View(tickets.Where(t => t.ProjectId == id).OrderByDescending(t => t.UpdatedTime).ToPagedList((int)page, 10));
        }

        [HttpPost, ActionName("List")]
        public ActionResult ListFiltered(int? page)
        {
            page = page ?? 1;
            
            if (CurrentUser.ProjectId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            int id = (int)CurrentUser.ProjectId;
            var tickets = db.Tickets.Include(t => t.TicketSeverity).Include(t => t.TicketStatus).Include(t => t.Owner).Include(t => t.Assignee).Include(t => t.Project);

            return View(tickets.Where(t => t.ProjectId == id).OrderByDescending(t => t.UpdatedTime).ToPagedList((int)page, 10));
        }

        [HttpGet]
        public ActionResult Details(int? id)
        {
            if (id == null || CurrentUser.ProjectId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            // We search for both id and project id to ensure users can't access tickets
            // On projects they are not part of.
            Ticket ticket = db.Tickets.Include("Owner").Include("Assignee")
                            .Include("TicketComments").Include("TicketSeverity")
                            .Include("TicketStatus").Include("TicketComments.Poster")
                            .Where(m => m.Id == id).First();

            // If the user is not in the current project
            if (CurrentUser.ProjectId != ticket.ProjectId)
            {
                // Check to see if the user is a member of this project
                if (CurrentUser.UserProjectList[ticket.ProjectId] != null)
                {
                    // Change their current project to the project this ticket is in
                    CurrentUser.ProjectId = ticket.ProjectId;
                    Session["Project"] = ticket.ProjectId;
                }
                else
                {
                    // Forbid the user from viewing this ticket
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }
            }

            if (CurrentUser.IsManager)
            {
                var list = db.UserProjectRoles.Include("User")
                             .Where(m => m.ProjectId == CurrentUser.ProjectId &&
                                         m.Role.Role1 == "Developer").ToList();

                List<SelectListItem> UserList = new List<SelectListItem>();

                UserList.Add(new SelectListItem { Value = "-1", Text = "Nobody" });
                                
                foreach (var user in list)
                {
                    string name = user.User.FirstName + " " + user.User.LastName + " (" + user.User.Email + ")";
                    var toAdd = new SelectListItem { Value = user.UserId.ToString(), Text = name };

                    if (user.UserId == ticket.AssigneeId)
                        toAdd.Selected = true;

                    UserList.Add(toAdd);
                }

                ViewBag.assignList = UserList;
            }

            var model = new TicketDetailViewModel(ticket);

            return View(model);
        }
        
        [HttpPost]
        [ManagerRequired]
        public ActionResult Assign(int? id, string assignList)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var ticket = db.Tickets.Where(m => m.Id == (int)id && m.ProjectId == CurrentUser.ProjectId).First();

            try
            {
                int newId = Convert.ToInt32(assignList);
                if (newId == -1)
                    ticket.AssigneeId = null;
                else
                {
                    ticket.AssigneeId = newId;
                    ticket.StatusId = db.TicketStatusList.Where(m => m.Status == "Assigned").First().Id;
                }
            }
            catch
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            db.Entry(ticket).State = EntityState.Modified;
            db.SaveChanges();
            
            return RedirectToAction("Details", "Ticket", new { id = id });
        }
       
        [HttpGet]
        [SubmitterRequired]
        public ActionResult Create()
        {
            ViewBag.SeverityId = new SelectList(db.TicketSeverities, "Id", "Type");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [SubmitterRequired]
        public ActionResult Create([Bind(Include = "Id,ProjectId,OwnerId,TypeId,SeverityId,AssigneeId,Description,CreationTime,UpdatedTime,Title")] Ticket ticket)
        {
            ticket.CreationTime = DateTimeOffset.UtcNow;
            ticket.UpdatedTime = DateTimeOffset.UtcNow;
            ticket.OwnerId = (int)CurrentUser.UserId;
            ticket.ProjectId = (int)CurrentUser.ProjectId;
            ticket.StatusId = db.TicketStatusList.Where(m => m.Status == "New").ToList().First().Id;

            ModelState.Clear();
            TryValidateModel(ticket);

            if (ModelState.IsValid)
            {
                db.Tickets.Add(ticket);
                db.SaveChanges();
                return RedirectToAction("List");
            }
            else
            {
                var errors = ModelState.Select(x => x.Value.Errors)
                                       .Where(g => g.Count > 0).ToList();
                if (errors.Count > 0)
                {
                    ViewBag.ServrityId = new SelectList(db.TicketSeverities, "Id", "Type", ticket.SeverityId);
                }

                return View(ticket);
            }

        }

        [HttpGet]
        [ManagerRequired]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ticket ticket = db.Tickets.Find(id);

            return View(ticket);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [ManagerRequired]
        public ActionResult DeleteConfirmed(int id)
        {
            //Ticket ticket = db.Tickets.Find(id);
            //db.Tickets.Remove(ticket);
            //db.SaveChanges();
            return RedirectToAction("List");
        }
    }
}
