using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TravelAgency.Controllers
{
    public class ManagerController : Controller
    {
        // GET: Manager
        public ActionResult Arrangements()
        {
            return View();
        }

        public ActionResult Accommodation()
        {
            return View();
        }

        public ActionResult Unit()
        {
            return View();
        }

        public ActionResult Reservations()
        {
            return View();
        }
        
    }
}