using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using TravelAgency.Handlers;
using TravelAgency.Models;

namespace TravelAgency.Controllers
{
    public class AdminController : ApiController
    {
        private ReservationHandler reservationHandler = ReservationHandler.GetInstance();
        private UsersHandler usersHandler = UsersHandler.GetInstance();

        [HttpGet]
        [Authentificaton]
        [Route("api/admin/getToBan")]
        public List<string> GetNames()
        {
            var username = RetriveUsername(Request);
            if (usersHandler.CheckLoggedRights(username) != EUserType.Administrator)
                return null;

            return reservationHandler.GetToBanUserKeys();
        }


        [HttpPut]
        [Authentificaton]
        [Route("api/admin/ban/{toBanUsername}")]
        public void BanUser([FromUri] string toBanUsername)
        {
            var username = RetriveUsername(Request);
            if (usersHandler.CheckLoggedRights(username) != EUserType.Administrator)
                return;

            usersHandler.BanUser(toBanUsername);
        }

        private static string RetriveUsername(HttpRequestMessage req)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(req.Headers.Authorization.Parameter)).Split(':')[0];
        }
    }
}
