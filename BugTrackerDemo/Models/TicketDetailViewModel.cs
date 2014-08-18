using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BugTrackerDemo.Models
{
    public class TicketDetailViewModel
    {
        public TicketDetailViewModel(Ticket ticket)
        {
            Id = ticket.Id;

            OwnerName = ticket.Owner.FirstName + " " + ticket.Owner.LastName + " (" + ticket.Owner.Email + ")";
            if (ticket.Assignee != null)
                AssigneeName = ticket.Assignee.FirstName + " " + ticket.Assignee.LastName + " (" + ticket.Assignee.Email + ")";
            else
                AssigneeName = "Nobody";

            CreationDate = ticket.CreationTime.ToString();
            UpdateDate = ticket.UpdatedTime.ToString();
            Title = ticket.Title;
            Description = ticket.Description;
            Severity = ticket.TicketSeverity.Type;
            Status = ticket.TicketStatus.Status;
            AssigneeId = ticket.AssigneeId;

            foreach (var item in ticket.TicketComments)
            {
                Comments.Add(new TicketCommentViewModel
                {
                    PosterName = item.Poster.FirstName + " " + item.Poster.LastName + " (" + item.Poster.Email + ")",
                    Message = item.Message,
                    PostTime = item.PostTime
                });
            }

            foreach (var item in ticket.TicketAttachments)
            {
                Attachments.Add(new TicketAttachmentModelView
                    {
                        FileHash = item.FileHash,
                        FileName = item.FileName,
                        Uploader = item.User.FirstName + " " + item.User.LastName + " (" + item.User.Email + ")",
                        Uploaded = item.UploadDate
                    });
            }

        }

        public int Id;
        public string OwnerName;
        public string AssigneeName;
        public string CreationDate;
        public string UpdateDate;
        public string Title;
        public string Description;
        public string Severity;
        public string Status;
        public int? AssigneeId;

        public List<TicketCommentViewModel> Comments = new List<TicketCommentViewModel>();
        public List<TicketAttachmentModelView> Attachments = new List<TicketAttachmentModelView>();
    }

    public class TicketCommentViewModel
    {
        public string PosterName;
        public string Message;
        public DateTimeOffset PostTime;
    }

    public class TicketAttachmentModelView
    {
        public string FileHash;
        public string FileName;
        public string Uploader;
        public DateTimeOffset Uploaded;
    }

    public class UserListItem
    {
        public int userId;
        public string name;
    }
}