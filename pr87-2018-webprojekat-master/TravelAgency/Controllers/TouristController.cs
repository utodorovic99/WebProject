using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TravelAgency.Controllers
{
    public class TouristController : Controller
    {
        // GET: Tourist
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Reservations()
        {
            return View();
        }
    }
}