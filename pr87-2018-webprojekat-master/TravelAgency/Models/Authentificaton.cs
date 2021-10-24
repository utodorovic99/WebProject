using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using TravelAgency.Handlers;
 
namespace TravelAgency.Models
{
    public class Authentificaton: AuthorizationFilterAttribute
    {
        private static UsersHandler usersHandler = UsersHandler.GetInstance();
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            if(actionContext.Request.Headers.Authorization==null)
            {
                actionContext.Response = actionContext.Request.CreateResponse(System.Net.HttpStatusCode.Unauthorized);
            }
            else
            {
                string token = actionContext.Request.Headers.Authorization.Parameter;
                string formattedToken=Encoding.UTF8.GetString(Convert.FromBase64String(token));
                string[] loginParams=formattedToken.Split(':');

                string username = loginParams[0];
                string password = loginParams[1];

                if(usersHandler.LogUserIn(username, password))
                    Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity(username), null);  
                else
                    actionContext.Response = actionContext.Request.CreateResponse(System.Net.HttpStatusCode.Unauthorized, "Invalid username or password");
            }
        }
    }
}