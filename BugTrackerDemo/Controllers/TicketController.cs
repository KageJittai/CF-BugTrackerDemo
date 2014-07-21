using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BugTrackerDemo.Models;

namespace BugTrackerDemo.Controllers
{
    public class TicketController : Controller
    {
        private BugTrackerEntities db = new BugTrackerEntities();

        // GET: Ticket
        public ActionResult Index(string filter)
        {
            if (Session["Project"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            int id = (int)Session["Project"];
            var tickets = db.Tickets.Include(t => t.TicketPriority).Include(t => t.TicketStatus).Include(t => t.TicketType).Include(t => t.Owner).Include(t => t.Assignee).Include(t => t.Project);
            switch(filter)
            {
                case "Open":
                    tickets = tickets.Where(t => t.TicketStatus.Status == "Open");
                    break;
                case "Assigned":
                    int userid = (int)Session["User"];
                    tickets = tickets.Where(t => t.TicketStatus.Status == "Assigned" && t.AssigneeId == userid);
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
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ticket ticket = db.Tickets.Find(id);
            if (ticket == null)
            {
                return HttpNotFound();
            }
            return View(ticket);
        }

        // GET: Ticket/Create
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
        public ActionResult Create([Bind(Include = "Id,ProjectId,OwnerId,TypeId,PriorityId,StatusId,AssigneeId,Description,CreationTime,UpdatedTime,Title")] Ticket ticket)
        {
            ticket.CreationTime = DateTimeOffset.UtcNow;
            ticket.UpdatedTime = DateTimeOffset.UtcNow;
            ticket.OwnerId = (int)Session["User"];
            ticket.ProjectId = (int)Session["Project"];
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

        // GET: Ticket/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ticket ticket = db.Tickets.Find(id);
            if (ticket == null)
            {
                return HttpNotFound();
            }

            ViewBag.PriorityId = new SelectList(db.TicketPriorities, "Id", "Priority", ticket.PriorityId);
            ViewBag.StatusId = new SelectList(db.TicketStatusList, "Id", "Status", ticket.StatusId);
            ViewBag.TypeId = new SelectList(db.TicketTypes, "Id", "Type", ticket.TypeId);
            ViewBag.OwnerId = new SelectList(db.UserModels, "Id", "FirstName", ticket.OwnerId);
            ViewBag.AssigneeId = new SelectList(db.UserModels, "Id", "FirstName", ticket.AssigneeId);
            ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Name", ticket.ProjectId);
            return View(ticket);
        }

        // POST: Ticket/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,ProjectId,OwnerId,TypeId,PriorityId,StatusId,AssigneeId,Description,Creation,Updated,Title")] Ticket ticket)
        {
            if (ModelState.IsValid)
            {
                db.Entry(ticket).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.PriorityId = new SelectList(db.TicketPriorities, "Id", "Priority", ticket.PriorityId);
            ViewBag.StatusId = new SelectList(db.TicketStatusList, "Id", "Status", ticket.StatusId);
            ViewBag.TypeId = new SelectList(db.TicketTypes, "Id", "Type", ticket.TypeId);
            ViewBag.OwnerId = new SelectList(db.UserModels, "Id", "FirstName", ticket.OwnerId);
            ViewBag.AssigneeId = new SelectList(db.UserModels, "Id", "FirstName", ticket.AssigneeId);
            ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Name", ticket.ProjectId);
            return View(ticket);
        }

        // GET: Ticket/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ticket ticket = db.Tickets.Find(id);
            if (ticket == null)
            {
                return HttpNotFound();
            }
            return View(ticket);
        }

        // POST: Ticket/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Ticket ticket = db.Tickets.Find(id);
            db.Tickets.Remove(ticket);
            db.SaveChanges();
            return RedirectToAction("Index");
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
