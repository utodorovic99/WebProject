using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravelAgency.Models
{
    public class Reservation
    {
        public ulong  ResID     { get; set; }
        public string UID       { get; set; }   // User ID
        public ulong  AID       { get; set; }   // Arrangement ID
        public string Status    { get; set; }   // Active/Canceled
        public ulong  AUnitID   { get; set; }   // Accommodation ID
        public string  UnitID    { get; set; }  // Accommodation unit ID

        public Reservation()
        {
            ResID = ulong.MaxValue;
            UID = null;
            AID = ResID;
            Status = null;
            AUnitID = ResID;
            UnitID = "None";
        }
        public Reservation(ulong resID, string uID, ulong aID, string status, ulong aUnitID, string unitID)
        {
            ResID = resID;
            UID = uID;
            AID = aID;
            Status = status;
            AUnitID = aUnitID;
            UnitID = unitID;
        }

        public override string ToString()
        {
            return String.Format("RESID: {0}\nUID: {1}\nAID: {2}\nStatus: {3}\nAUNIT_ID: {4}\nUNIT_ID: {5}\n", ResID, UID, AID, Status, AUnitID, UnitID);
        }

        public bool IsEmpty()
        {
            return ResID == ulong.MaxValue &&  AID == ulong.MaxValue && 
                   AUnitID == ulong.MaxValue && UnitID == "None" &&
                   UID == null && Status == null;
        }
    }

}