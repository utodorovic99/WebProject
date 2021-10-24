using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using TravelAgency.Handlers;
using TravelAgency.Models;

namespace TravelAgency.Controllers
{

    public class LoginController : ApiController
    {
        private UsersHandler usersHandler = UsersHandler.GetInstance();
        [Authentificaton]
        public HttpResponseMessage Post([FromBody]LoginReq login)
        {
            // Potencijalna oprimizacija: Slati samo ERR CODE umjesto cijelog string-a 
            // da se ne opterecuje mreza bez potrebe.
            switch (usersHandler.LogUserIn(login))
            {
                case EUserLoginStatus.BadPassword: { return Request.CreateResponse(HttpStatusCode.NotAcceptable, "Bad Password");  }
                case EUserLoginStatus.BadUsername: { return Request.CreateResponse(HttpStatusCode.NotAcceptable, "Bad Username"); }
                case EUserLoginStatus.Fail: { return Request.CreateResponse(HttpStatusCode.NotAcceptable, "Failed"); }  // Unknown role
                case EUserLoginStatus.Success:
                    {
                        string role = ""; 
                        switch (usersHandler.CheckLoggedRights(login.Username))
                        {
                            case EUserType.Administrator:
                                {
                                    role = "Administrator";                                
                                    break;
                                }

                            case EUserType.Tourist:
                                { role = "Tourist"; break;}

                            case EUserType.Manager:{ role = "Manager"; break;}
                        }
                        if (role != "") return Request.CreateResponse(HttpStatusCode.OK, role);
                        else return Request.CreateResponse(HttpStatusCode.BadRequest);
                    }
            }
            return Request.CreateResponse(HttpStatusCode.InternalServerError, "Call Support");
        }
    }
}
