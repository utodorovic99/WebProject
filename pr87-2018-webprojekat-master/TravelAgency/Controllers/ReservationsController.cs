using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using TravelAgency.Handlers;
using TravelAgency.Models;

namespace TravelAgency.Controllers
{
    public class ReservationsController : ApiController
    {
        private ReservationHandler reservationsHandler = ReservationHandler.GetInstance();
        private ArrangementsHandler arrangmentHandler = ArrangementsHandler.GetInstance();
        private UsersHandler usersHandler = UsersHandler.GetInstance();
        private CommentHandler commentHandler = CommentHandler.GetInstance();
        private AccommodationHandler accommodationHandler = AccommodationHandler.GetInstance();

        [Authentificaton]
        [HttpPost]
        [Route("api/reservations/reserve")]
        public HttpResponseMessage Reserve([FromBody] ReservationReq req)
        {
            EUserType type = usersHandler.CheckLoggedRights(req.User);
            if (type == EUserType.None) return Request.CreateResponse(HttpStatusCode.MethodNotAllowed, "Please log in");

            if (type != EUserType.Tourist)
                return Request.CreateResponse(HttpStatusCode.MethodNotAllowed);

            var arrangment = arrangmentHandler.GetArrangementByName(req.Arrangement);
            var timeStatus = TimeCalculator.CheckTimeRelationMine(arrangment.DateStart);
            if (timeStatus < 0)
                return Request.CreateResponse(HttpStatusCode.MethodNotAllowed, "Arrangement started");
            else if (timeStatus == 0)
            {
                // Min 3 hours earlier to resere
                if (DateTime.Compare(DateTime.Now, DateTime.ParseExact(arrangment.DateStart, "dd/mm/yyyy", CultureInfo.InvariantCulture).
                                                            AddHours(Double.Parse(arrangment.MeetingTime) + 3)) > 0)
                {
                    Request.CreateResponse(HttpStatusCode.MethodNotAllowed, "Min. 3 hours earlier to reserve");
                }

            }

            reservationsHandler.Reserve(req);
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [Authentificaton]
        [HttpPost]
        [Route("api/reservations/unreserve")]
        public HttpResponseMessage Unreserve([FromBody] ReservationReq req)
        {
            if (usersHandler.CheckLoggedRights(req.User) != EUserType.Tourist)
                return Request.CreateResponse(HttpStatusCode.MethodNotAllowed);

            if (!reservationsHandler.IsReservedByMe(req))
                return Request.CreateResponse(HttpStatusCode.MethodNotAllowed, "Not reserved by you or original not found");

            var arrangment = arrangmentHandler.GetArrangementByName(req.Arrangement);
            var timeStatus = TimeCalculator.CheckTimeRelationMine(arrangment.DateStart);
            if (timeStatus < 0)
                return Request.CreateResponse(HttpStatusCode.MethodNotAllowed, "Arrangement started");
            else if (timeStatus == 0)
            {
                // Min 3 hours earlier to resere
                if (DateTime.Compare(DateTime.Now, DateTime.ParseExact(arrangment.DateStart, "dd/mm/yyyy", CultureInfo.InvariantCulture).
                                                            AddHours(Double.Parse(arrangment.MeetingTime) + 3)) > 0)
                {
                    Request.CreateResponse(HttpStatusCode.MethodNotAllowed, "Min. 3 hours earlier to cancel");
                }

            }

            reservationsHandler.Unreserve(req);
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [Authentificaton]
        [HttpGet]
        [Route("api/reservations/activeReservationsFor/{arrangement}/{user}")]
        public ReservedResp ReservedFor([FromUri] string arrangement, [FromUri] string user)
        {
            return reservationsHandler.ActiveReservationsFor(arrangement, user);
        }

        [Authentificaton]
        [HttpGet]
        [Route("api/reservations/{user}")]
        public ReservationsResp GetAllReservations([FromUri] string user)
        {
            return reservationsHandler.ReservationsFor(user);
        }

        [Authentificaton]
        [HttpPost]
        [Route("api/reservations/comment")]
        public HttpResponseMessage Comment([FromBody] Comment comment)
        {
            if (comment.Txt == null || comment.AID == null || comment.UID== null || comment.Rating == null)
                Request.CreateResponse(HttpStatusCode.NotAcceptable, "Empty parameter detected");
            comment.Status = "na";
            var result =commentHandler.Comment(comment);
            if (result == ECommentStatus.Success) return Request.CreateResponse(HttpStatusCode.OK);
            else
            {
                switch(result)
                {
                    case ECommentStatus.NotFound:
                        {
                            Request.CreateResponse(HttpStatusCode.MethodNotAllowed, "Reservation not found");
                            break;
                        }
                    case ECommentStatus.TimeViolation:
                        {
                            Request.CreateResponse(HttpStatusCode.MethodNotAllowed, "Reservation not completed");
                            break;
                        }
                }
            }
            return null;
        }

        [Authentificaton]
        [HttpGet]
        [Route("api/reservations/getForMannager")]
        public List<Reservation> ReadManagerAll()
        {
            var username = RetriveUsername(Request);
            if (usersHandler.CheckLoggedRights(username) != EUserType.Manager)
                return null;       
            return reservationsHandler.ReadByArrangementKeys(usersHandler.GetArrangementsForManager(username));
        }

        [Authentificaton]
        [HttpGet]
        [Route("api/reservations/managerTranslation/{aid}/{aUnitID}")]
        public ArrangementAccomodationNameTraslationReq ManagerTranslation([FromUri] string aid, [FromUri] string aUnitID)
        {
            var username = RetriveUsername(Request);
            if (usersHandler.CheckLoggedRights(username) != EUserType.Manager)
                return null;
            return new ArrangementAccomodationNameTraslationReq(arrangmentHandler.GetNameByID(ulong.Parse(aid)), accommodationHandler.GetNameByID(aUnitID));
        }

        private static string RetriveUsername(HttpRequestMessage req)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(req.Headers.Authorization.Parameter)).Split(':')[0];
        }

    }
}
