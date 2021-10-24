using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using TravelAgency.Handlers;
using TravelAgency.Models;

namespace TravelAgency.Controllers
{
    public class UsersController : ApiController
    {
        private UsersHandler usersHandler = UsersHandler.GetInstance();

        public IHttpActionResult Post([FromBody]User user)
        {
            string tmpStr;
            if ((tmpStr = usersHandler.ChechkUserStatus(user)) != "") return BadRequest(tmpStr);

            if (usersHandler.ReadUserByID(user.Username)!=null) return BadRequest("Username already used; ");

            if (user.Role != "t") return BadRequest("Bad role requested; ");

            usersHandler.StoreUser(new Tourist(user));

            return Ok();
        }

        [Authentificaton]
        [Route("api/users/{username}")]
        public User Get([FromUri] string username)
        {
            return usersHandler.ReadUserByID(username);
        }

        [Authentificaton]
        [HttpPost]
        [Route("api/users/update")]
        public HttpResponseMessage Update([FromBody] UserUpdateReq req)
        {
            if (usersHandler.CheckLoggedRights(req.Username) == EUserType.None) return Request.CreateResponse(HttpStatusCode.BadRequest);
            string errStr;
            if ((errStr=usersHandler.ChechkUserStatus(req)) !="") return Request.CreateResponse(HttpStatusCode.BadRequest, errStr);

            bool result=false;
            try { result =usersHandler.UpdateUser(req); }
            catch(Exception exc)
            {
                if (exc.Message != "Source file not found")
                    Request.CreateResponse(HttpStatusCode.BadRequest, exc.Message);
                else
                    Request.CreateResponse(HttpStatusCode.InternalServerError);
            }
            return result ? Request.CreateResponse(HttpStatusCode.OK) : Request.CreateResponse(HttpStatusCode.BadRequest, "Update failed");
        }

    }
}
