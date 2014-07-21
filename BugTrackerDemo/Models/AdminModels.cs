using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace BugTrackerDemo.Models
{
    public class UserViewModel
    {
        [Required]
        public int Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public List<ProjectItem> ProjectItems = new List<ProjectItem>();
    }
    public class ProjectItem
    {
        public string ProjectName { get; set; }

        public int ProjectId { get; set; }

        public bool IsManager { get; set; }
        public bool IsDeveloper { get; set; }
        public bool IsSubmitter { get; set; }

    }
}