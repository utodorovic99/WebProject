using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Xml;
using TravelAgency.Models;

namespace TravelAgency.Handlers
{
    public enum EReservationResult
    {
        Success, NotAvailable
    }

    public class ReservationHandler
    {
        #region Keys

        private static ulong keyCounter = 0;
        private static List<ulong> forReuse = new List<ulong>();
        private static bool keyLimitReached = false;

        public static ulong TakeKey()
        {
            ulong outKey;
            if (forReuse.Count != 0)
            {
                outKey = forReuse[forReuse.Count - 1];
                forReuse.RemoveAt(forReuse.Count - 1);
                return outKey;
            }
            else if (forReuse.Count == 0 && keyLimitReached) throw new Exception("Key range used");

            outKey = keyCounter;
            ++keyCounter;
            if (keyCounter == ulong.MaxValue) keyLimitReached = true;
            return outKey;
        }

        public static void ReturnKey(ulong key)
        {
            if (forReuse.Count == 1)                                       //Keep list Asc sorted (case 1 elem)
            {
                if (key > forReuse[0])
                {
                    forReuse.Insert(forReuse.Count, key);
                }
                else if (key < forReuse[0])
                {
                    forReuse.Insert(0, key);
                }
                else throw new Exception("Key conflict");
            }
            else
            {
                bool triggered = false;
                for (int loc = 0; loc < forReuse.Count; ++loc)               // Find place to insert
                {
                    if (key < forReuse[loc])
                    {
                        forReuse.Insert(loc, key);
                        triggered = true;
                        break;
                    }
                }

                if (!triggered) forReuse.Add(key);             // Not inserted? Insert at the end

            }

            if (keyCounter > 0 && forReuse.Count > 0 && forReuse.Contains(keyCounter - 1)) // Transfer from list to key interval
            {
                var rightlim = forReuse.FindIndex(x => x == keyCounter - 1);
                keyCounter = forReuse[0];
                forReuse.RemoveRange(0, rightlim);
            }

        }
        #endregion

        private static ReservationHandler singletoneInstance = null;
        private static readonly string xmlPath = HttpContext.Current.Server.MapPath("~/App_Data/Files/Reservations.xml");
        private static XmlDocument xmlDoc = new XmlDocument();
        private static object reservationsMutex = new object();

        private static ArrangementsHandler arrangementsHandler = ArrangementsHandler.GetInstance();
        private static AccommodationHandler accommodationHandler = AccommodationHandler.GetInstance();
        private static UsersHandler usersHandler = UsersHandler.GetInstance();

        private ReservationHandler(){}

        public static ReservationHandler GetInstance()
        {
            if (singletoneInstance == null) singletoneInstance = new ReservationHandler();
            return singletoneInstance;
        }

        public EReservationResult Reserve(ReservationReq req)
        {
            ulong accommodation = accommodationHandler.GetByName(req.Accommodation).AcID;
            Arrangement arrangement = arrangementsHandler.GetArrangementByName(req.Arrangement);
            if (IsAvailable(arrangementsHandler.GetArrangementByName(req.Arrangement), accommodation.ToString(), req.Unit))
            {
                if (ContainsCanceled(arrangementsHandler.GetArrangementByName(req.Arrangement), accommodation.ToString(), req.Unit))
                    Reactivate(arrangementsHandler.GetArrangementByName(req.Arrangement), accommodation.ToString(), req.Unit);

                else
                    StoreReservation(new Reservation(TakeKey(), req.User, arrangement.AID, "a", accommodationHandler.GetByName(req.Accommodation).AcID, req.Unit));
            }
            return EReservationResult.Success;
        }

        private void Reactivate(Arrangement arrangement, string accommodationID, string accommodationUnitID)
        {
            lock (reservationsMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                var reservations = xmlDoc.SelectNodes(String.Format(
                    "/RESERVATIONS/RESERVATION[ AID = '{0}'  and UNIT_ID = '{1}' and AUNIT_ID = '{2}' and STATUS = 'c' ]",
                    arrangement.AID, accommodationUnitID, accommodationID));

                if (reservations.Count > 1) throw new Exception("Synonims");
                if (reservations.Count == 0) throw new Exception("NoOriginal");

                foreach (XmlNode child in reservations[0].ChildNodes)
                {
                    if (child.NodeType == XmlNodeType.Element && child.Name == "STATUS")
                    { child.InnerText = "a"; break; }
                }

                xmlDoc.Save(xmlPath);

            }
        }

        public bool ContainsCanceled(Arrangement arrangement, string accommodationID, string accommodationUnitID)
        {
            lock (reservationsMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                var reservations = xmlDoc.SelectNodes(String.Format(
                    "/RESERVATIONS/RESERVATION[ @deleted = '0' and AID = '{0}'  and UNIT_ID = '{1}' and AUNIT_ID = '{2}' and STATUS = 'c']",
                    arrangement.AID, accommodationUnitID, accommodationID));

                if (reservations.Count > 1) throw new Exception("Synonims");
                if (reservations.Count == 0) return false;
                return true;
            }
        }

        public bool IsAvailable(Arrangement arrangement, string accommodationID, string accommodationUnitID)
        {
            lock (reservationsMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                var reservations = xmlDoc.SelectNodes(String.Format(
                    "/RESERVATIONS/RESERVATION[ @deleted = '0' and AID = '{0}'  and UNIT_ID = '{1}' and AUNIT_ID = '{2}' and STATUS = 'a']",
                    arrangement.AID, accommodationUnitID, accommodationID));

                if (reservations.Count !=0) return false;
                return true;
            }
        }

        public void Unreserve(ReservationReq req)
        {
            lock (reservationsMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                var reservation = xmlDoc.SelectSingleNode(String.Format(
                    "/RESERVATIONS/RESERVATION[ @deleted = '0' and AID = '{0}'  and UNIT_ID = '{1}' and AUNIT_ID = '{2}' and UID = '{3}' and STATUS = 'a' ]",
                    arrangementsHandler.GetArrangementByName(req.Arrangement).AID,
                    req.Unit,
                    accommodationHandler.GetByName(req.Accommodation).AcID,
                    req.User));

                foreach (XmlNode child in reservation.ChildNodes)
                {
                    if (child.NodeType == XmlNodeType.Element && child.Name == "STATUS")
                    { child.InnerText = "c"; break; }
                }

                if (Int32.Parse(reservation.Attributes["timesCanceled"].Value) != Int32.MaxValue)
                    reservation.Attributes["timesCanceled"].Value = (Int32.Parse(reservation.Attributes["timesCanceled"].Value) + 1).ToString();

                xmlDoc.Save(xmlPath);
                
            }
        }

        public bool IsReservedByMe(ReservationReq req)
        {
            lock (reservationsMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                var reservations = xmlDoc.SelectNodes(String.Format(
                    "/RESERVATIONS/RESERVATION[ @deleted = '0' and AID = '{0}'  and UNIT_ID = '{1}' and AUNIT_ID = '{2}' and UID = '{3}' and STATUS = 'a']",
                    arrangementsHandler.GetArrangementByName(req.Arrangement).AID, 
                    req.Unit, 
                    accommodationHandler.GetByName(req.Accommodation).AcID,
                    req.User));

                if (reservations.Count == 0) return false;
                if (reservations.Count >1) throw new Exception("Inconsistent");
                return true;
            }
        }

        public ECommentStatus IsReservedByMePassed(string aID, string uID)
        {
            lock (reservationsMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                var reservations = xmlDoc.SelectNodes(String.Format("/RESERVATIONS/RESERVATION[ @deleted = '0' and AID = '{0}'  and UID = '{1}' and STATUS = 'a']", aID, uID));
                if (reservations.Count == 0) return ECommentStatus.NotFound;
                foreach(XmlNode node in reservations)
                {
                    if (TimeCalculator.CheckTimeRelationMine(arrangementsHandler.GetArrangementByID(ulong.Parse(aID)).DateStop) < 0)
                        return ECommentStatus.Success;
                }
                return ECommentStatus.TimeViolation;
            }
        }

        public void StoreReservation(Reservation reservation)
        {
            xmlDoc.Load(xmlPath);
            XmlElement newReservation = xmlDoc.CreateElement("RESERVATION");
            XmlElement newResID = xmlDoc.CreateElement("RESID");
            XmlElement newUID = xmlDoc.CreateElement("UID");
            XmlElement newAID = xmlDoc.CreateElement("AID");
            XmlElement newStatus = xmlDoc.CreateElement("STATUS");
            XmlElement newAunitID = xmlDoc.CreateElement("AUNIT_ID");
            XmlElement newUnitID = xmlDoc.CreateElement("UNIT_ID");

            newReservation.Attributes.Append(xmlDoc.CreateAttribute("deleted"));
            newReservation.Attributes[0].Value = "0";

            newReservation.Attributes.Append(xmlDoc.CreateAttribute("timesCanceled"));
            newReservation.Attributes[1].Value = "0";

            newResID.InnerText = reservation.ResID.ToString();
            newUID.InnerText = reservation.UID;
            newAID.InnerText = reservation.AID.ToString();
            newStatus.InnerText = reservation.Status;
            newAunitID.InnerText = reservation.AUnitID.ToString();
            newUnitID.InnerText = reservation.UnitID.ToString();

            newReservation.AppendChild(newResID);
            newReservation.AppendChild(newUID);
            newReservation.AppendChild(newAID);
            newReservation.AppendChild(newStatus);
            newReservation.AppendChild(newAunitID);
            newReservation.AppendChild(newUnitID);
            xmlDoc.SelectSingleNode("/RESERVATIONS").AppendChild(newReservation);
            xmlDoc.Save(xmlPath);

        }

        private Reservation ParseXMLNode(XmlNode reservation)
        {
            Reservation newReservation = new Reservation();
            foreach (XmlNode childNode in reservation.ChildNodes)
            {
                if (childNode.NodeType == XmlNodeType.Element)
                {
                    switch ((childNode as XmlNode).Name)
                    {
                        case "RESID": { newReservation.ResID = ulong.Parse(childNode.InnerText); break; }
                        case "UID": { newReservation.UID = childNode.InnerText; break; }
                        case "AID": { newReservation.AID = ulong.Parse(childNode.InnerText); break; }
                        case "STATUS": { newReservation.Status = childNode.InnerText; break; }
                        case "AUNIT_ID": { newReservation.AUnitID = ulong.Parse(childNode.InnerText); break; }
                        case "UNIT_ID": { newReservation.UnitID = childNode.InnerText; break; }
                    }


                }
            }
            return newReservation;
        }

        public List<Reservation> GetReservationsByIDs(List<ulong> keys)
        {
            List<Reservation> outList = new List<Reservation>();
            if (keys.Count == 0) return outList;

            lock (reservationsMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                string groupStr = "";
                foreach (var key in keys) groupStr += String.Format(" or RESID = '{0}'", key);
                groupStr=groupStr.TrimStart(new char[] { ' ', 'o', 'r' });
                
                var reservations=xmlDoc.SelectNodes(String.Format("/RESERVATIONS/RESERVATION[ ({0}) and @deleted = '0' and STATUS = 'a']", groupStr));

                foreach (XmlNode reservation in reservations)
                {

                    if (reservation.NodeType == XmlNodeType.Element)
                    {
                        var newReservation = ParseXMLNode(reservation);
                        if (!newReservation.IsEmpty()) outList.Add(newReservation);
                    }
                }
            }
            return outList;
        }

        public ReservedResp ActiveReservationsFor(string arrangement, string user)
        {
            lock (reservationsMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                var reservations = xmlDoc.SelectNodes(String.Format("/RESERVATIONS/RESERVATION[ @deleted = '0' and AID = '{0}' and STATUS = 'a' and UID = '{1}']",
                    arrangementsHandler.GetAIDByName(arrangement), user));

                ReservedResp resp = new ReservedResp();
                string accommodation = "";
                string unit="";
                foreach(XmlNode reservation in reservations)
                {
                    if (reservation.NodeType == XmlNodeType.Element)
                    {
                        foreach (XmlNode param in reservation.ChildNodes)
                        {
                            if (param.NodeType == XmlNodeType.Element)
                            {
                                if (param.Name == "AUNIT_ID")
                                {
                                    accommodation = accommodationHandler.ReadNameByID(param.InnerText, arrangement);
                                }
                                if (param.Name == "UNIT_ID") unit = param.InnerText;
                            }
                        }

                        if(accommodation !="" && unit !="") resp.AddReservation(accommodation, unit);
                        accommodation = ""; unit = "";
                    }
                }
                return resp;
            }
        }

        public ReservationsResp ReservationsFor(string user)
        {
            lock(reservationsMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");
                var reservations = xmlDoc.SelectNodes(String.Format("/RESERVATIONS/RESERVATION[ @deleted = '0' and UID = '{0}' ]", user));
                ReservationsResp resp = new ReservationsResp();
                if (reservations.Count == 0) return resp;

                List<Reservation> parsedNodes = new List<Reservation>();
                foreach(XmlNode reservation in reservations)
                {
                    parsedNodes.Add(ParseXMLNode(reservation));
                }

                Dictionary<string, Arrangement> arrangementsCollection = null;
                {
                    List<string> allArrangementKeys = new List<string>();
                    foreach(var reservation in parsedNodes)
                    {
                        if (!allArrangementKeys.Contains(reservation.AID.ToString()))
                            allArrangementKeys.Add(reservation.AID.ToString()); 
                    }
                    arrangementsCollection = arrangementsHandler.GetAIDArrangementsKVPairs(allArrangementKeys);
                }

                Dictionary<string, List<Accommodation>> incoming = new Dictionary<string, List<Accommodation>>();
                Dictionary<string, List<Accommodation>> passed = new Dictionary<string, List<Accommodation>>();
                Dictionary<string, List<Accommodation>> ongoing = new Dictionary<string, List<Accommodation>>();
                Dictionary<string, List<Accommodation>> canceled = new Dictionary<string, List<Accommodation>>();
                foreach (var reservation in parsedNodes)
                {
                    if(reservation.Status.Equals("a"))
                    {
                        switch (TimeCalculator.CheckProgressStatus(arrangementsCollection[reservation.AID.ToString()].DateStart,
                                                                 arrangementsCollection[reservation.AID.ToString()].DateStop))
                        {
                            case EProgressStatus.INCOMING:
                                {
                                    // First time accommodation
                                    if (!incoming.ContainsKey(arrangementsCollection[reservation.AID.ToString()].Name))
                                    {
                                        incoming.Add(arrangementsCollection[reservation.AID.ToString()].Name, new List<Accommodation>());
                                    }

                                    //First time unit
                                    if (!incoming[arrangementsCollection[reservation.AID.ToString()].Name].Any(x => x.AcID == reservation.AUnitID))
                                    {
                                        Accommodation tmpAccommodation = accommodationHandler.ReadByKey(reservation.AUnitID.ToString(), arrangementsHandler.GetNameByID(reservation.AID));
                                        tmpAccommodation.Units = new List<AccommodationUnit>();
                                        incoming[arrangementsCollection[reservation.AID.ToString()].Name].Add(tmpAccommodation);
                                    }

                                    if (!incoming[arrangementsCollection[reservation.AID.ToString()].Name].Any(x => x.AcID == reservation.AUnitID && x.Units.Any(y => y!=null && y.UID == reservation.UnitID)))
                                    {
                                        incoming[arrangementsCollection[reservation.AID.ToString()].Name].Find(x => x.AcID == reservation.AUnitID).Units.Add
                                            (
                                                accommodationHandler.ReadByKey(reservation.AUnitID.ToString(), arrangementsHandler.GetNameByID(reservation.AID)).Units.Find(x => x.UID == reservation.UnitID)
                                            );
                                    }
                                    break;
                                }
                            case EProgressStatus.ONGOING:
                                {
                                    if (!ongoing.ContainsKey(arrangementsCollection[reservation.AID.ToString()].Name))
                                    {
                                        ongoing.Add(arrangementsCollection[reservation.AID.ToString()].Name,new List<Accommodation>());
                                    }

                                    //First time unit
                                    if (!ongoing[arrangementsCollection[reservation.AID.ToString()].Name].Any(x => x.AcID == reservation.AUnitID))
                                    {
                                        Accommodation tmpAccommodation = accommodationHandler.ReadByKey(reservation.AUnitID.ToString(), arrangementsHandler.GetNameByID(reservation.AID));
                                        tmpAccommodation.Units = new List<AccommodationUnit>();
                                        ongoing[arrangementsCollection[reservation.AID.ToString()].Name].Add(tmpAccommodation);
                                    }

                                    if (!ongoing[arrangementsCollection[reservation.AID.ToString()].Name].Any(x => x.AcID == reservation.AUnitID && x.Units.Any(y => y != null && y.UID == reservation.UnitID)))
                                    {
                                        ongoing[arrangementsCollection[reservation.AID.ToString()].Name].Find(x => x.AcID == reservation.AUnitID).Units.Add
                                            (
                                                accommodationHandler.ReadByKey(reservation.AUnitID.ToString(), arrangementsHandler.GetNameByID(reservation.AID)).Units.Find(x => x.UID == reservation.UnitID)
                                            );
                                    }
                                    break;
                                }
                            case EProgressStatus.PASSED:
                                {
                                    if (!passed.ContainsKey(arrangementsCollection[reservation.AID.ToString()].Name))
                                    {
                                        passed.Add(arrangementsCollection[reservation.AID.ToString()].Name,  new List<Accommodation>());
                                    }

                                    //First time unit
                                    if (!passed[arrangementsCollection[reservation.AID.ToString()].Name].Any(x => x.AcID == reservation.AUnitID))
                                    {
                                        Accommodation tmpAccommodation = accommodationHandler.ReadByKey(reservation.AUnitID.ToString(), arrangementsHandler.GetNameByID(reservation.AID));
                                        tmpAccommodation.Units = new List<AccommodationUnit>();
                                        passed[arrangementsCollection[reservation.AID.ToString()].Name].Add(tmpAccommodation);
                                    }

                                    if (!passed[arrangementsCollection[reservation.AID.ToString()].Name].Any(x => x.AcID == reservation.AUnitID && x.Units.Any(y => y != null && y.UID == reservation.UnitID)))
                                    {
                                        passed[arrangementsCollection[reservation.AID.ToString()].Name].Find(x => x.AcID == reservation.AUnitID).Units.Add
                                            (
                                                accommodationHandler.ReadByKey(reservation.AUnitID.ToString(), arrangementsHandler.GetNameByID(reservation.AID)).Units.Find(x => x.UID == reservation.UnitID)
                                            );
                                    }
                                    break;
                                }
                        }

                    }
                    else if(reservation.Status.Equals("c"))
                    {
                        if (!canceled.ContainsKey(arrangementsCollection[reservation.AID.ToString()].Name))
                        {
                            canceled.Add(arrangementsCollection[reservation.AID.ToString()].Name, new List<Accommodation>());
                        }

                        //First time unit
                        if (!canceled[arrangementsCollection[reservation.AID.ToString()].Name].Any(x => x.AcID == reservation.AUnitID))
                        {
                            Accommodation tmpAccommodation = accommodationHandler.ReadByKey(reservation.AUnitID.ToString(), arrangementsHandler.GetNameByID(reservation.AID));
                            tmpAccommodation.Units = new List<AccommodationUnit>();
                            canceled[arrangementsCollection[reservation.AID.ToString()].Name].Add(tmpAccommodation);
                        }

                        if (!canceled[arrangementsCollection[reservation.AID.ToString()].Name].Any(x => x.AcID == reservation.AUnitID && x.Units.Any(y => y != null && y.UID == reservation.UnitID)))
                        {
                            canceled[arrangementsCollection[reservation.AID.ToString()].Name].Find(x => x.AcID == reservation.AUnitID).Units.Add
                                (
                                    accommodationHandler.ReadByKey(reservation.AUnitID.ToString(), arrangementsHandler.GetNameByID(reservation.AID)).Units.Find(x => x.UID == reservation.UnitID)
                                );
                        }
                    }
                }
                resp.Canceled = canceled;
                resp.Incoming = incoming;
                resp.Ongoing = ongoing;
                resp.Passed = passed;
                return resp;
            }
        }

        public void HandleUsernameUpdate(string oldUsername, string newUsername)
        {
            lock (reservationsMutex)
            {
                xmlDoc.Load(xmlPath);
                var reservations = xmlDoc.SelectNodes(String.Format("/RESERVATIONS/RESERVATION[ @deleted = '0' and UID = '{0}' ]/UID", oldUsername));
                foreach (XmlNode reservation in reservations)
                    reservation.InnerText = newUsername;
                xmlDoc.Save(xmlPath);
            }
        }

        public bool ContainsReservationForArrangement(string arrangementName)
        {
            lock(reservationsMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");
                //U postavci pise neka rezervacija sto znaci prosla, buduca ili otkazana
                var reservations = xmlDoc.SelectNodes(String.Format("/RESERVATIONS/RESERVATION[ @deleted = '0' and AID = '{0}']", arrangementsHandler.GetAIDByName(arrangementName)));
                if (reservations == null) return false;
                return reservations.Count > 0 ? true : false;
            }
        }

        public bool ContainsReservationForArrangementAndAccommodation(string arrangementName, string accommodationName)
        {
            lock (reservationsMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");
                //U postavci pise neka rezervacija sto znaci prosla, buduca ili otkazana
                var reservations = xmlDoc.SelectNodes(String.Format("/RESERVATIONS/RESERVATION[ @deleted = '0' and AID = '{0}' and AUNIT_ID = '{1}' ]", 
                    arrangementsHandler.GetAIDByName(arrangementName),accommodationHandler.GetIDByName(accommodationName)));
                if (reservations == null) return false;
                return reservations.Count > 0 ? true : false;
            }
        }

        public List<string> GetAllAIDs()
        {
            lock (reservationsMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");
                var aIDs = xmlDoc.SelectNodes("/RESERVATIONS/RESERVATION[ @deleted = '0' and AID = '{0}']/AID");
                List<string> outList = new List<string>();
                foreach (XmlNode aID in aIDs)
                    if (!outList.Contains(aID.InnerText)) outList.Add(aID.InnerText);
                return outList;
            }
        }

        public List<Reservation> ReadAll()
        {
            lock (reservationsMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");
                var reservations = xmlDoc.SelectNodes("/RESERVATIONS/RESERVATION[ @deleted = '0' and STATUS = 'a']");
                List<Reservation> outList = new  List<Reservation>();
                foreach (XmlNode reservation in reservations)
                    outList.Add(ParseXMLNode(reservation));
                return outList;
            }
        }

        public bool ContainsFutureReservationForAccommodationAndUnit(string accUnitName)
        {
            return (ReadAll().Where(x => x.UnitID == accUnitName && arrangementsHandler.GetCurrentAndFutureIDs().Contains(x.AID.ToString())).Count()) > 0 ? true : false;
        }

        public void PropagateAccUnitNameChange(string oldName, string newName)
        {
            lock (reservationsMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                var reservations = xmlDoc.SelectNodes(String.Format("/RESERVATIONS/RESERVATION[ @deleted = '0' and UNIT_ID = '{0}']/UNIT_ID", oldName));

                foreach (XmlNode unitName in reservations)
                    unitName.InnerText = newName;

                xmlDoc.Save(xmlPath);
            }
        }

        public List<Reservation> ReadByArrangementKeys(List<ulong> arrangementKeys)
        {
            lock (reservationsMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");
                XmlNodeList reservations;
                Reservation parsedReservation;
                List<Reservation> outList = new List<Reservation>();
                foreach (var aID in arrangementKeys)
                {
                    reservations = xmlDoc.SelectNodes(String.Format("/RESERVATIONS/RESERVATION[ @deleted = '0' and AID = {0}]", aID));
                    foreach(XmlNode reservation in reservations)
                    {
                        parsedReservation = ParseXMLNode(reservation);
                        if (!outList.Contains(parsedReservation))
                            outList.Add(parsedReservation);
                    }
                }
                    
                return outList;
            }
        }

        public List<string> GetToBanUserKeys()
        {
            lock (reservationsMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");
                var users = xmlDoc.SelectNodes("/RESERVATIONS/RESERVATION[ @deleted = '0' and @timesCanceled >= '2']/UID");
                List<string> outList = new List<string>();
                foreach(XmlNode uID in users)
                {
                    if (!outList.Contains(uID.InnerText) && !usersHandler.IsBanned(uID.InnerText)) outList.Add(uID.InnerText);
                }
                return outList;
            }
        }

    }
}