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

namespace BugTrackerDemo.Controllers
{

    public class TicketController : BaseController
    {

        [HttpPost]
        public ActionResult SubmitComment(int? id, string Comment)
        {
            if (id == null || CurrentUser.ProjectId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Make sure this ticket is inside the current project
            var ticket = db.Tickets.Where(u => u.Id == id && u.ProjectId == CurrentUser.ProjectId).First();

            var newComment = new TicketComment
            {
                PosterId = (int)CurrentUser.UserId,
                PostTime = DateTimeOffset.Now,
                Message = Comment,
                TicketId = (int)id
            };

            db.TicketComments.Add(newComment);
            db.SaveChanges();

            return RedirectToAction("Details", "Ticket", new { id = id });
        }
        
        
        // GET: Ticket
        public ActionResult Index(string filter)
        {
            if (CurrentUser.ProjectId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            int id = (int)CurrentUser.ProjectId;
            var tickets = db.Tickets.Include(t => t.TicketPriority).Include(t => t.TicketStatus).Include(t => t.TicketType).Include(t => t.Owner).Include(t => t.Assignee).Include(t => t.Project);
            switch(filter)
            {
                case "Open":
                    tickets = tickets.Where(t => t.TicketStatus.Status == "Open");
                    break;
                case "Assigned":
                    tickets = tickets.Where(t => t.TicketStatus.Status == "Assigned" && t.AssigneeId == CurrentUser.UserId);
                    break;
                case "Resolved":
                    tickets = tickets.Where(t => t.TicketStatus.Status == "Resolved");
                    break;
                default:
                    break;
            }

            tickets = tickets.Where(t => t.ProjectId == id).OrderByDescending(t => t.UpdatedTime);
            return View(tickets.ToList());
        }

        // GET: Ticket/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null || CurrentUser.ProjectId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            // We search for both id and project id to ensure users can't access tickets
            // On projects they are not part of.
            Ticket ticket = db.Tickets.Include("Owner").Include("Assignee")
                            .Include("TicketComments").Include("TicketPriority").Include("TicketType")
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
        
        // POST: Ticket/Upload
        [HttpPost]
        public ActionResult Upload(int? id, HttpPostedFileBase file)
        {
            if (id == null || file == null || CurrentUser.ProjectId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ticket ticket = db.Tickets.Where(m => m.Id == id && m.ProjectId == CurrentUser.ProjectId).First();

            if (file.ContentLength > 0)
            {
                TicketAttachment attachment = new TicketAttachment();
                attachment.UploadDate = DateTimeOffset.UtcNow;
                attachment.TicketId = ticket.Id;
                attachment.UploaderId = (int)CurrentUser.UserId;
                attachment.FileName = Path.GetFileName(file.FileName);
 
                var md5 = MD5.Create();
                attachment.FileHash = string.Join("", md5.ComputeHash(file.InputStream).Select(x => x.ToString("x2")));

                var path = Path.Combine(Server.MapPath("~/App_Data/uploads"), attachment.FileHash);

                file.SaveAs(path);

                db.Entry(attachment).State = EntityState.Added;
                db.SaveChanges();
            }
            return RedirectToAction("Details", "Ticket", new { id = (int)id });
        }

        // GET: Ticket/Download/id
        public ActionResult Download(string id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            TicketAttachment attachment = db.TicketAttachments.Where(m => m.FileHash == id).First();

            return File("~/App_Data/uploads/" + attachment.FileHash, "application/force-download", attachment.FileName);
        }
        
        // GET: Ticket/Create
        [SubmitterRequired]
        public ActionResult Create()
        {
            ViewBag.PriorityId = new SelectList(db.TicketPriorities, "Id", "Priority");
            ViewBag.TypeId = new SelectList(db.TicketTypes, "Id", "Type");
            return View();
        }

        // POST: Ticket/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SubmitterRequired]
        public ActionResult Create([Bind(Include = "Id,ProjectId,OwnerId,TypeId,PriorityId,StatusId,AssigneeId,Description,CreationTime,UpdatedTime,Title")] Ticket ticket)
        {
            ticket.CreationTime = DateTimeOffset.UtcNow;
            ticket.UpdatedTime = DateTimeOffset.UtcNow;
            ticket.OwnerId = (int)CurrentUser.UserId;
            ticket.ProjectId = (int)CurrentUser.ProjectId;
            ticket.StatusId = db.TicketStatusList.Where(m => m.Status == "Open").ToList().First().Id;

            ModelState.Clear();
            TryValidateModel(ticket);

            if (ModelState.IsValid)
            {
                db.Tickets.Add(ticket);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                var errors = ModelState.Select(x => x.Value.Errors)
                                       .Where(g => g.Count > 0).ToList();
                if (errors.Count > 0)
                {
                    ViewBag.PriorityId = new SelectList(db.TicketPriorities, "Id", "Priority", ticket.PriorityId);
                    ViewBag.TypeId = new SelectList(db.TicketTypes, "Id", "Type", ticket.TypeId);
                }

                return View(ticket);
            }

        }

        // GET: Ticket/Delete/5
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

        // POST: Ticket/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [ManagerRequired]
        public ActionResult DeleteConfirmed(int id)
        {
            Ticket ticket = db.Tickets.Find(id);
            db.Tickets.Remove(ticket);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
