using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BugTrackerDemo.Models
{
    public class PageDataModel
    {
        public PageDataModel()
        {
            UserId = null;
            UserDisplayName = "";

            IsManager = false;
            IsDeveloper = false;
            IsSubmitter = false;

            ProjectId = null;
            ProjectName = "No Projects";

            UserProjectList = new Dictionary<int, string>();
        }

        public int? UserId { get; set; }
        public string UserDisplayName { get; set; }

        public bool IsManager { get; set; }
        public bool IsDeveloper { get; set; }
        public bool IsSubmitter { get; set; }

        public int? ProjectId { get; set; }
        public string ProjectName { get; set; }

        public Dictionary<int, string> UserProjectList;

    }
}
