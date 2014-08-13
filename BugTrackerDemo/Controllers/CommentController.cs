using BugTrackerDemo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace BugTrackerDemo.Controllers
{
    public class CommentController : BaseController
    {
        [HttpPost]
        public ActionResult Create(int? id, string Comment)
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
    }
}