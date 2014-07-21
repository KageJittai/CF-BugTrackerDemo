using BugTrackerDemo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BugTrackerDemo.Controllers
{
    public class BaseController : Controller
    {
        protected BugTrackerEntities db;

        public BaseController() : base()
        {
            db = new BugTrackerEntities();
        }
    }
}