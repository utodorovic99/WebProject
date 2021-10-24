using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TravelAgency.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";

            return View();
        }

        public ActionResult HistoryAndOngoing()
        {
            ViewBag.Title = "Arrangemetns";

            return View();
        }


        public ActionResult Accommodations()
        {
            ViewBag.Title = "Accommodations";

            return View();
        }

        public ActionResult Profile()
        {
            ViewBag.Title = "Profile";

            return View();
        }

        public ActionResult Arrangments()
        {
            return RedirectToAction("Arrangments", controllerName: "Manager");
        }
    }
}
