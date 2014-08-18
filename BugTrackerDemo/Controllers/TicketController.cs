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
        public ActionResult List(int? page, int? status, int? severity, bool? myTickets)
        {
            page = page ?? 1;
            status = status ?? -1;
            severity = severity ?? -1;
            myTickets = myTickets ?? false;

            if (CurrentUser.ProjectId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            int id = (int)CurrentUser.ProjectId;
            var tickets = db.Tickets.Include(t => t.TicketSeverity).Include(t => t.TicketStatus).Include(t => t.Owner).Include(t => t.Assignee).Include(t => t.Project);

            if (status != -1)
            {
                tickets = tickets.Where(t => t.StatusId == (int)status);
            }
            if (severity != -1)
            {
                tickets = tickets.Where(t => t.SeverityId == (int)severity);
            }
            if ((bool)myTickets)
            {
                tickets = tickets.Where(t => t.OwnerId == CurrentUser.UserId || t.AssigneeId == CurrentUser.UserId);
            }

            ViewBag.statusCurrent = (int)status;
            ViewBag.severityCurrent = (int)severity;
            ViewBag.myTicketsCurrent = (bool)myTickets;


            List<SelectListItem> severityList = new List<SelectListItem>();
            List<SelectListItem> statusList = new List<SelectListItem>();

            severityList.Add(new SelectListItem
             {
                Value = "-1",
                Text = "[Any]",
                Selected = (-1 == severity) ? true : false
             });

            statusList.Add(new SelectListItem
             {
                Value = "-1",
                Text = "[Any]",
                Selected = (-1 == status) ? true : false
             });

            foreach (var item in db.TicketSeverities.ToList())
            {
                severityList.Add(new SelectListItem
                    {
                        Value = item.Id.ToString(),
                        Text = item.Type,
                        Selected = (item.Id == severity) ? true : false
                    });
            }
                
            foreach (var item in db.TicketStatusList.ToList())
            {
                statusList.Add(new SelectListItem
                    {
                        Value = item.Id.ToString(),
                        Text = item.Status,
                        Selected = (item.Id == status) ? true : false
                    });
            }

            ViewBag.severity = severityList;
            ViewBag.status = statusList;

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
                
                List<SelectListItem> SeverityList = new List<SelectListItem>();
                var dbSeverityList = db.TicketSeverities.ToList();

                foreach (var severity in dbSeverityList)
                {
                    SeverityList.Add(new SelectListItem
                    {
                        Value = severity.Id.ToString(),
                        Text = severity.Type
                    });
                }

                ViewBag.severityList = SeverityList;
            }

            // Both a manager or a person assigned to a ticket can update the ticket's status
            if (CurrentUser.IsManager || CurrentUser.UserId == ticket.AssigneeId)
            {
                List<SelectListItem> StatusList = new List<SelectListItem>();
                var dbList = db.TicketStatusList.ToList();

                foreach(var status in dbList)
                {
                    var toAdd = new SelectListItem
                    {
                        Value = status.Id.ToString(),
                        Text = status.Status
                    };

                    if (ticket.StatusId == status.Id)
                        toAdd.Selected = true;

                    StatusList.Add(toAdd);
                }

                ViewBag.statusList = StatusList;

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
                    ticket.StatusId = 2; // Ugly hack, but assigns the Status to the "In progress" status.
                }
            }
            catch
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            UpdateTicket(ticket, "Assigned to User");
            
            return RedirectToAction("Details", "Ticket", new { id = id });
        }

        [HttpPost]
        public ActionResult ChangeStatus(int? id, string statusList)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var ticket = db.Tickets.Where(m => m.Id == (int)id && m.ProjectId == CurrentUser.ProjectId).First();

            // Only people assigned to ticket and managers can change the status
            if (ticket.AssigneeId != CurrentUser.UserId && !CurrentUser.IsManager)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            try
            {
                ticket.StatusId = Int32.Parse(statusList);
            }
            catch
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            UpdateTicket(ticket, "Status Updated");

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
