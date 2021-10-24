using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using TravelAgency.Handlers;
using System.Diagnostics;

namespace TravelAgency
{
    public class RouteConfig
    {

        public static void RegisterRoutes(RouteCollection routes)
        {
            //Service configuration
            UsersHandler userHandeler = UsersHandler.GetInstance();
            userHandeler.InitialLoad();

            #region TestBLocks

            //Trace.TraceInformation("----------- USERS ---------------");
            //foreach(var user in userHandeler.ReadUsers())
            //{
            //    Trace.TraceInformation(user.ToString());
            //}
            //Trace.TraceInformation("---------------------------------");

            //Trace.TraceInformation("----------- ADMINS ---------------");
            //foreach (var user in userHandeler.GetAllAdministartors())
            //{
            //    Trace.TraceInformation(user.ToString());
            //}
            //Trace.TraceInformation("---------------------------------");

            //Trace.TraceInformation("----------- TOURISTS ---------------");
            //foreach (var user in userHandeler.GetAllTourists())
            //{
            //    Trace.TraceInformation(user.ToString());
            //}
            //Trace.TraceInformation("---------------------------------");

            //Trace.TraceInformation("----------- MANAGERS ---------------");
            //foreach (var user in userHandeler.GetAllMangaers())
            //{
            //    Trace.TraceInformation(user.ToString());
            //}
            //Trace.TraceInformation("---------------------------------");

            #endregion

            ///////////////////////////////////////////////////////////

            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
