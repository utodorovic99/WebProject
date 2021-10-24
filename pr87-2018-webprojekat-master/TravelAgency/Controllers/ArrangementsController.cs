using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Web.Http;
using TravelAgency.Handlers;
using TravelAgency.Models;

namespace TravelAgency.Controllers
{

    public class ArrangementsController : ApiController
    {
        private ArrangementsHandler arrangementsHandler = ArrangementsHandler.GetInstance();
        private CommentHandler commentsHandler = CommentHandler.GetInstance();
        private UsersHandler usersHandler = UsersHandler.GetInstance();

        [HttpGet]
        [Route("api/arrangements/active")]
        public List<ArrangementNSC> GetActive()
        {
            return arrangementsHandler.GetActiveArrangementsNSC();
        }

        [HttpGet]
        [Route("api/arrangements/incomingNames")]
        public List<string> GetIncomingNames()
        {
            return arrangementsHandler.GetIncomingNames();
        }

        [HttpGet]
        [Authentificaton]
        [Route("api/arrangements/activeCreatedBy/{id}")]
        public List<string> GetActiveCreatedBy([FromUri]string id)
        {
            return arrangementsHandler.GetActiveArrangementsNSCCreatedBy(id);
        }

        [HttpGet]
        [Route("api/arrangements/history_ongoing")]
        public List<ArrangementNSC> GetHistoryAndOngoing()
        {
            return arrangementsHandler.GetHistoryAndOngoingArrangementsNSC();
        }

        [HttpGet]
        [Route("api/arrangements/{id}")]
        public Arrangement Get(string id)
        {
            return arrangementsHandler.GetArrangementByName(id);
        }

        [HttpGet]
        [Route("api/arrangements/readNSC/{id}")]
        public ArrangementNSC ReadNSC([FromUri]string id)
        {
            return arrangementsHandler.GetArrangementByIDNSC(id);
        }

        [HttpGet]
        [Route("api/arrangements/getComments/{id}")]
        public List<Comment> GetCommentsForClient([FromUri]string id)
        {
            return commentsHandler.ReadAllApproved(arrangementsHandler.GetAIDByName(id));
        }

        [HttpGet]
        [Authentificaton]
        [Route("api/arrangements/getCommentsForManager/{id}")]
        public List<Comment> GetCommentsForManager([FromUri]string id)
        {
            if (usersHandler.CheckLoggedRights(RetriveUsername(Request)) != EUserType.Manager)
                return null;
            return commentsHandler.ReadAll(arrangementsHandler.GetAIDByName(id));
        }

        [HttpPost]
        [Authentificaton]
        [Route("api/arrangements/managerCommentApprove")]
        public HttpResponseMessage ManagerApproveComment([FromBody] Comment comment)
        {

            if (usersHandler.CheckLoggedRights(RetriveUsername(Request)) != EUserType.Manager)
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Manager privilege");

            if(!usersHandler.IsCreatedBy(arrangementsHandler.GetNameByID(ulong.Parse(comment.AID)), RetriveUsername(Request)))
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Comment arrangement not made by you");

            var result = "";
            if((result=commentsHandler.ApproveComment(comment))=="")
                return Request.CreateResponse(HttpStatusCode.OK);
            else
                return Request.CreateResponse(HttpStatusCode.BadRequest, result);
        }

        [HttpPost]
        [Authentificaton]
        [Route("api/arrangements/managerCommentDecline")]
        public HttpResponseMessage ManagerDeclineComment([FromBody] Comment comment)
        {

            if (usersHandler.CheckLoggedRights(RetriveUsername(Request)) != EUserType.Manager)
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Manager privilege");

            if (!usersHandler.IsCreatedBy(arrangementsHandler.GetNameByID(ulong.Parse(comment.AID)), RetriveUsername(Request)))
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Comment arrangement not made by you");

            var result = "";
            if ((result = commentsHandler.DeclineComment(comment)) == "")
                return Request.CreateResponse(HttpStatusCode.OK);
            else
                return Request.CreateResponse(HttpStatusCode.BadRequest, result);
        }

        [HttpGet]
        [Authentificaton]
        [Route("api/arrangements/isCreatedByManager/{id}")]
        public bool IsCreatedByManager([FromUri]string id)
        {
            var username = RetriveUsername(Request);
            if (usersHandler.CheckLoggedRights(username) != EUserType.Manager)
                return false;
            return usersHandler.IsCreatedBy(id, username);
        }

        [HttpGet]
        [Route("api/arrangements/isReserved/{id}")]
        public bool IsReservedName([FromUri]string id)
        {
            return arrangementsHandler.ContainsArrangement(id);
        }

        [Authentificaton]
        public HttpResponseMessage Post([FromBody] Arrangement arrangement)
        {
            if (usersHandler.CheckLoggedRights(RetriveUsername(Request)) != EUserType.Manager)
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Manager privilege");

            if (arrangementsHandler.ContainsArrangement(arrangement))
                return Request.CreateResponse(HttpStatusCode.NotAcceptable, "Name already used");

            string errMess;
            if ((errMess = arrangementsHandler.CheckArrangementStatus(arrangement)) != "")
                return Request.CreateResponse(HttpStatusCode.NotAcceptable, errMess);

            if (arrangement.Accommodations == null) arrangement.Accommodations = new List<ulong>();
            arrangementsHandler.StoreArrangement(arrangement, RetriveUsername(Request));
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPut]
        [Authentificaton]
        [Route("api/arrangements/delete/{id}")]
        public HttpResponseMessage Delete([FromUri] string id)
        {
            if(usersHandler.CheckLoggedRights(RetriveUsername(Request))!= EUserType.Manager)
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Manager privilege");

            if(!arrangementsHandler.IsDeletable(id))
                return Request.CreateResponse(HttpStatusCode.NotAcceptable, "Reservation-Related object");

            arrangementsHandler.Remove(id);

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPost]
        [Authentificaton]
        [Route("api/arrangements/update/{id}")]
        public HttpResponseMessage Update([FromUri] string id,[FromBody] Arrangement arrangement)
        {
            if (usersHandler.CheckLoggedRights(RetriveUsername(Request)) != EUserType.Manager)
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Manager privilege");

            if (arrangementsHandler.ContainsArrangement(arrangement.Name))
                return Request.CreateResponse(HttpStatusCode.NotAcceptable, "Name already used");

            if (!arrangementsHandler.ContainsArrangement(id))
                return Request.CreateResponse(HttpStatusCode.NotAcceptable, "Original not found");

            string errMess;
            if ((errMess = arrangementsHandler.CheckArrangementStatus(arrangement)) != "")
                return Request.CreateResponse(HttpStatusCode.NotAcceptable, errMess);

            arrangementsHandler.UpdateArrangement(id, arrangement);

            return Request.CreateResponse(HttpStatusCode.OK);
        }


        private static string RetriveUsername(HttpRequestMessage req)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(req.Headers.Authorization.Parameter)).Split(':')[0];
        }
    }
}
