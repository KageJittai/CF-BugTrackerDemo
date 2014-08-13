using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BugTrackerDemo.Common
{
    public class UserData
    {
        public UserData()
        {
            UserId = null;
            FirstName = "";
            LastName = "";
            Email = "";

            IsAdmin = false;

            IsManager = false;
            IsDeveloper = false;
            IsSubmitter = false;

            ProjectId = null;
            ProjectName = "No Projects";

            UserProjectList = new Dictionary<int, string>();
        }

        public int? UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }

        public bool IsAdmin { get; set; }

        public bool IsManager { get; set; }
        public bool IsDeveloper { get; set; }
        public bool IsSubmitter { get; set; }

        public int? ProjectId { get; set; }
        public string ProjectName { get; set; }

        public Dictionary<int, string> UserProjectList;
    }
}