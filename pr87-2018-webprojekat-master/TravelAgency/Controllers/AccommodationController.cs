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
    public class AccommodationController : ApiController
    {
        private AccommodationHandler accommodationHander = AccommodationHandler.GetInstance();
        private UsersHandler usersHandler = UsersHandler.GetInstance();
        private ArrangementsHandler arrangementHandler = ArrangementsHandler.GetInstance();
        private ReservationHandler reservationHandler = ReservationHandler.GetInstance();

        [Route("api/accommodations/{id}/{arrangement}")]
        public List<Accommodation> Get([FromUri] string id, [FromUri] string arrangement)
        {
            return accommodationHander.ReadByKeys(id.Split(new char[] { ',' }).ToList<string>(), arrangement);
        }

        [HttpGet]
        [Route("api/accommodations/getNames")]
        public List<string> GetNames()
        {
            return accommodationHander.GetAllNames();
        }

        [Route("api/accommodationsByNames/{id}/{arrangement}")]
        public List<Accommodation> GetByNames([FromUri] string id, [FromUri] string arrangement)
        {
            return accommodationHander.ReadByNames(id.Split(new char[] { ',' }).ToList<string>(), arrangement);
        }

        [HttpGet]
        [Authentificaton]
        [Route("api/accommodations/GetAll")]
        public List<Accommodation> GetAll()
        {
            return accommodationHander.ReadAll();
        }

        [Route("api/accommodations/getSingle/{id}/{accommodation}")]
        public Accommodation GetSingle([FromUri] string id, [FromUri] string accommodation)
        {
            return accommodationHander.ReadByKey(accommodationHander.GetIDByName(accommodation), arrangementHandler.GetAIDByName(id));
        }

        [HttpPost]
        [Authentificaton]
        [Route("api/accommodation/addNew/{id}")]
        public HttpResponseMessage AddNew([FromUri] string id, [FromBody] Accommodation accommodation)
        {
            var username = RetriveUsername(Request);
            if (usersHandler.CheckLoggedRights(username) != EUserType.Manager)
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Manager privilege");

           if(!usersHandler.IsCreatedBy(id,username))
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Arrangement not made by you");

           var arrangement = arrangementHandler.GetArrangementByName(id);
           if (TimeCalculator.CheckTimeRelationMine(arrangement.DateStop)<=0)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Arrangement already started (or starts today)");

            if (accommodationHander.ReadByKeys(arrangement.Accommodations.ConvertAll<string>(x=>x.ToString()), id).Select(x=>x.Name).Contains(accommodation.Name))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Name already used");

            if (accommodation.Units == null) accommodation.Units = new List<AccommodationUnit>();
            string errMess;
            if((errMess=accommodationHander.CheckAccommodationStatus(accommodation))!="")
                return Request.CreateResponse(HttpStatusCode.BadRequest, errMess);

            accommodationHander.StoreAccommodation(accommodation, id);
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPost]
        [Authentificaton]
        [Route("api/accommodation/addNewFree")]
        public HttpResponseMessage AddNewFree([FromBody] Accommodation accommodation)
        {
            var username = RetriveUsername(Request);
            if (usersHandler.CheckLoggedRights(username) != EUserType.Manager)
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Manager privilege");

            if (accommodationHander.GetAllNames().Contains(accommodation.Name))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Name already used");

            if (accommodation.Units == null) accommodation.Units = new List<AccommodationUnit>();
            string errMess;
            if ((errMess = accommodationHander.CheckAccommodationStatus(accommodation)) != "")
                return Request.CreateResponse(HttpStatusCode.BadRequest, errMess);

            accommodationHander.StoreAccommodation(accommodation);
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPut]
        [Authentificaton]
        [Route("api/accommodation/assignToArrangement/{arrangementName}/{accommodationName}")]
        public HttpResponseMessage AssignToArrangement([FromUri] string arrangementName, [FromUri] string accommodationName)
        {
            var username = RetriveUsername(Request);
            if (usersHandler.CheckLoggedRights(username) != EUserType.Manager)
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Manager privilege");

            if (!accommodationHander.ContainsByName(accommodationName))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Accommodation name not exists");

            if (!arrangementHandler.ContainsByName(arrangementName))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Arrangement name not exists");

            var arrangement = arrangementHandler.GetArrangementByName(arrangementName);
            if (TimeCalculator.CheckTimeRelationMine(arrangement.DateStart)<=0)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Arrangement passed or ongoing");

            if (!accommodationHander.IsFreeBetween(arrangement.DateStart, arrangement.DateStop, accommodationName))
                return Request.CreateResponse(HttpStatusCode.BadRequest, string.Format("Accommodation is not free between {0} and {1}", arrangement.DateStart, arrangement.DateStop));

            arrangementHandler.AssignAccommodationToAarrangement(arrangementName,accommodationName);
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPut]
        [Authentificaton]
        [Route("api/accommodation/delete/{id}/{accName}")]
        public HttpResponseMessage Delete([FromUri] string id, [FromUri] string accName)
        {
            var username = RetriveUsername(Request);
            if (usersHandler.CheckLoggedRights(username) != EUserType.Manager)
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Manager privilege");

            if(!arrangementHandler.ContainsByName(id))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Original arrangement not found");

            var arrangement = arrangementHandler.GetArrangementByName(id);
            if (!accommodationHander.ReadByKeys(arrangement.Accommodations.ConvertAll<string>(x => x.ToString()), id).Select(x => x.Name).Contains(accName))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Original accommodation not found");

            if (!usersHandler.IsCreatedBy(id,username))
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Arrangement not made by you");

           if (TimeCalculator.CheckTimeRelationMine(arrangement.DateStop)<=0)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Arrangement already started (or starts today)");

           //Ako postoji aranzman u buducnosti(ili trenutni) sa tim smjestajem nema brisanja
            if(arrangementHandler.IsAccommodationUsedInCurrentOrOngoing(accommodationHander.GetIDByName(accName)))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Accommodation is used in future/current arrangements, delete forbidden ");

            accommodationHander.Remove(id,accName);
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPut]
        [Authentificaton]
        [Route("api/accommodation/deleteSmp/{accName}")]
        public HttpResponseMessage DeleteSmp([FromUri] string accName)
        {
            var username = RetriveUsername(Request);
            if (usersHandler.CheckLoggedRights(username) != EUserType.Manager)
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Manager privilege");

            if (!accommodationHander.ContainsByName(accName))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Original accommodation not found");

            //Ako postoji aranzman u buducnosti(ili trenutni) sa tim smjestajem nema brisanja (proslost)
            if (arrangementHandler.IsAccommodationUsedInCurrentOrOngoing(accommodationHander.GetIDByName(accName)))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Accommodation is used in future/current arrangements, delete forbidden ");

            accommodationHander.Remove(accName);
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPost]
        [Authentificaton]
        [Route("api/accommodation/update/{id}/{oldAccName}")]
        public HttpResponseMessage Update([FromUri] string id,[FromUri] string oldAccName, [FromBody] Accommodation accommodation)
        {
            var username = RetriveUsername(Request);
            if (usersHandler.CheckLoggedRights(username) != EUserType.Manager)
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Manager privilege");

            if (!usersHandler.IsCreatedBy(id, username))
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Arrangement not made by you");

            var arrangement = arrangementHandler.GetArrangementByName(id);
            if (TimeCalculator.CheckTimeRelationMine(arrangement.DateStop) <= 0)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Arrangement already started (or starts today)");

            if (oldAccName != accommodation.Name)
            {

                if (accommodationHander.ReadByKeys(arrangement.Accommodations.ConvertAll<string>(x => x.ToString()), id).Select(x => x.Name).Contains(accommodation.Name))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "New Name already used");
            }

            string errMess;
            if ((errMess = accommodationHander.CheckAccommodationStatus(accommodation)) != "")
                return Request.CreateResponse(HttpStatusCode.BadRequest, errMess);

            return accommodationHander.UpdateAccommodation(oldAccName,accommodation) ?  Request.CreateResponse(HttpStatusCode.OK): 
                                                                                        Request.CreateResponse(HttpStatusCode.BadRequest, "Update request failed");
        }

        [HttpPost]
        [Authentificaton]
        [Route("api/accommodation/addNewACUnit/{accommodation}")]
        public HttpResponseMessage AddNewACUnit([FromUri] string accommodation, [FromBody] AccommodationUnit accommodationUnit)
        {
            var username = RetriveUsername(Request);
            if (usersHandler.CheckLoggedRights(username) != EUserType.Manager)
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Manager privilege");

            if (!accommodationHander.ContainsByName(accommodation))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Accommodation not found");

            string errStr;
            if((errStr = accommodationHander.CheckAccUnitStatus(accommodationUnit))!="")
                return Request.CreateResponse(HttpStatusCode.BadRequest, errStr);

            if(accommodationHander.AccommodationContainsUnit(accommodation, accommodationUnit.UID))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Unit name already used in chosen accommodation");

            accommodationHander.AddAccUnitToAccommodation(accommodation, accommodationUnit);
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPut]
        [Authentificaton]
        [Route("api/accommodation/deleteACUnit/{accommodation}/{accUnitName}")]
        public HttpResponseMessage DeleteACUnit([FromUri] string accommodation, [FromUri] string accUnitName)
        {
            var username = RetriveUsername(Request);
            if (usersHandler.CheckLoggedRights(username) != EUserType.Manager)
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Manager privilege");

            if (!accommodationHander.ContainsByName(accommodation))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Accommodation not found");

           if (reservationHandler.ContainsFutureReservationForAccommodationAndUnit(accUnitName))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Acc. Unit is used in future reservations, delete forbidden");

            accommodationHander.RemoveUnit(accommodation, accUnitName);
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpGet]
        [Route("api/accommodation/getACUnit/{accommodation}/{accUnitName}")]
        public AccommodationUnit GetACUnit([FromUri] string accommodation, [FromUri] string accUnitName)
        {
            return accommodationHander.ReadUnit(accommodation, accUnitName);
        }


        [HttpPost]
        [Authentificaton]
        [Route("api/accommodation/updateACUnit/{accommodation}/{accUnitName}")]
        public HttpResponseMessage UpdateACUnit([FromUri] string accommodation, [FromUri] string accUnitName, [FromBody] AccommodationUnit accommodationUnit)
        {
            var username = RetriveUsername(Request);
            if (usersHandler.CheckLoggedRights(username) != EUserType.Manager)
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Manager privilege");

            if (!accommodationHander.ContainsByName(accommodation))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Accommodation not found");

            string errStr;
            if ((errStr = accommodationHander.CheckAccUnitStatus(accommodationUnit)) != "")
                return Request.CreateResponse(HttpStatusCode.BadRequest, errStr);

            if(accommodationHander.ReadUnit(accommodation, accommodationUnit.UID) !=null)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "New name already used");

            if (reservationHandler.ContainsFutureReservationForAccommodationAndUnit(accUnitName) && accommodationHander.ReadUnit(accommodation, accUnitName).MaxGuessts!=accommodationUnit.MaxGuessts)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Acc. Unit is used in future reservations, capacity modification is forbidden");

            accommodationHander.UpdateAccommodationUnit(accommodation, accUnitName, accommodationUnit);
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        private static string RetriveUsername(HttpRequestMessage req)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(req.Headers.Authorization.Parameter)).Split(':')[0];
        }
    }
}
