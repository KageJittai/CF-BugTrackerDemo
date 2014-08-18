using BugTrackerDemo.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;

namespace BugTrackerDemo.Controllers
{
    public class AttachmentController : BaseController
    {
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
                UpdateTicket(ticket, "File Attached");
            }

            return RedirectToAction("Details", "Ticket", new { id = (int)id });
        }

        // GET: Ticket/Download/id
        [HttpGet]
        public ActionResult Download(string id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            TicketAttachment attachment = db.TicketAttachments.Where(m => m.FileHash == id).First();

            return File("~/App_Data/uploads/" + attachment.FileHash, "application/force-download", attachment.FileName);
        }
    }
}