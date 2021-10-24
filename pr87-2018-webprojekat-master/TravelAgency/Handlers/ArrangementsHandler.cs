using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using TravelAgency.Models;

namespace TravelAgency.Handlers
{
    public class ArrangementsHandler
    {
        #region Keys
        private static ulong keyCounter = 0;
        private static List<ulong> forReuse = new List<ulong>();
        private static bool keyLimitReached = false;
        private static readonly object keyMutex = new object();

        public static ulong TakeKey()
        {
            lock (keyMutex)
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
        }

        public static void ReturnKey(ulong key)
        {
            lock (keyMutex)
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

        }

        private void InitKeyLoad()
        {
            lock (keyMutex)
            {
                xmlDoc.Load(xmlPath);
                List<ulong> keyList = new List<ulong>();
                {
                    var usedKeys = xmlDoc.SelectNodes("/ARRANGEMENTS/ARRANGEMENT[ @deleted = '0']/AID");
                    foreach (XmlNode aID in usedKeys)
                        keyList.Add(ulong.Parse(aID.InnerText));

                    var deletedKeys = xmlDoc.SelectNodes("/ARRANGEMENTS/ARRANGEMENT[ @deleted = '1']/AID");
                    foreach (XmlNode aID in deletedKeys)
                        forReuse.Add(ulong.Parse(aID.InnerText));
                    forReuse.Sort();
                }
                keyCounter = keyList.Max();
                if (keyCounter == ulong.MaxValue) keyLimitReached = true;
                else ++keyCounter;
            }
            
        }
        #endregion

        private static ArrangementsHandler singletoneInstance = null;
        private static readonly string xmlPath = HttpContext.Current.Server.MapPath("~/App_Data/Files/Arrangements.xml");
        private static XmlDocument xmlDoc = new XmlDocument();
        private static object arrangementsMutex = new object();
        private static AccommodationHandler acommodationHandler= AccommodationHandler.GetInstance();
        private static UsersHandler usersHandler = UsersHandler.GetInstance();
        private static ReservationHandler reservationsHandler = ReservationHandler.GetInstance();


        private ArrangementsHandler()
        {
                InitKeyLoad();
        }

        public static ArrangementsHandler GetInstance()
        {
            if (singletoneInstance == null)
            {
                singletoneInstance = new ArrangementsHandler();
            }
            return singletoneInstance;
        }

        public void AssignAccommodationToAarrangement(string arrangementName, string accommodationName)
        {
            lock (arrangementsMutex)
            {
                xmlDoc.Load(xmlPath);
                XmlNode target = xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted = '0']", arrangementName));
                var acID = acommodationHandler.GetByName(accommodationName).AcID;
                if (xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted = '0']/ACCOMMODATIONS/AC_ID [@deleted = '0' and text()= '{1}']",
                    arrangementName, accommodationName)) != null)
                    throw new Exception("Aready connected");

                XmlNode newAcID = xmlDoc.CreateElement("AC_ID");
                newAcID.InnerText = acID.ToString();

                newAcID.Attributes.Append(xmlDoc.CreateAttribute("deleted"));
                newAcID.Attributes[0].Value = "0";

                (xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted = '0']/ACCOMMODATIONS",
                    arrangementName, accommodationName))).AppendChild(newAcID);
                xmlDoc.Save(xmlPath);
            }
        }

        public List<Arrangement> ReadAll()   
        {
            List<Arrangement> outList = new List<Arrangement>();
            lock (arrangementsMutex)
            {
                xmlDoc.Load(xmlPath);
                var arrangements = xmlDoc.SelectNodes("/ARRANGEMENTS/ARRANGEMENT[ @deleted = '0']");
                foreach (XmlNode arrangement in arrangements)
                    outList.Add(ParseXMLNode(arrangement));
            }

            return outList;
        }    

        private Arrangement ParseXMLNode(XmlNode arrangement)
        {
            Arrangement newArrangement = new Arrangement();
            foreach (XmlNode childNode in arrangement.ChildNodes)
            {
                if (childNode.NodeType == XmlNodeType.Element)
                { 
                    switch (childNode.Name)
                    {
                        case "AID": { newArrangement.AID = ulong.Parse(childNode.InnerText); break; }
                        case "NAME": { newArrangement.Name = childNode.InnerText; break; }
                        case "TYPE": { newArrangement.Type = childNode.InnerText; break; }
                        case "TRANSPORT": { newArrangement.Transport = childNode.InnerText; break; }
                        case "LOCATION": { newArrangement.Locationn = childNode.InnerText; break; }
                        case "DATESTART": { newArrangement.DateStart = childNode.InnerText; break; }
                        case "DATESTOP": { newArrangement.DateStop = childNode.InnerText; break; }
                        case "MEETINGSPOT":
                            {
                                Location meetingSpot = new Location();
                                foreach (XmlNode meetingChild in childNode.ChildNodes)
                                {
                                    switch (meetingChild.Name)
                                    {
                                        case "ADDRESS":
                                            { meetingSpot.Address = meetingChild.InnerText; break; }
                                        case "GEOLONGITUDE":
                                            { meetingSpot.GeoLongitude = double.Parse(meetingChild.InnerText); break; }
                                        case "GEOLATITUDE":
                                            { meetingSpot.GeoLatitude = double.Parse(meetingChild.InnerText); break; }
                                    }
                                }
                                newArrangement.MeetingSpot = meetingSpot;
                                break;
                            }
                        case "MAXPASSENGERS": { newArrangement.MaxPassengers = uint.Parse(childNode.InnerText); break; }
                        case "MEETINGTIME": { newArrangement.MeetingTime = childNode.InnerText; break; }
                        case "DESC": { newArrangement.Desc = childNode.InnerText; break; }
                        case "PROGRAM": { newArrangement.Program = childNode.InnerText; break; }
                        case "POSTERURL": { newArrangement.PosterURL = childNode.InnerText; break; }
                        case "ACCOMMODATIONS":
                            {
                                List<ulong> acommodations = new List<ulong>();
                                foreach (XmlNode accommodation in childNode.ChildNodes)
                                {
                                    if (accommodation.NodeType == XmlNodeType.Element && accommodation.Name == "AC_ID" &&
                                       accommodation.Attributes["deleted"].Value == "0")
                                        acommodations.Add(ulong.Parse(accommodation.InnerText));
                                }
                                newArrangement.Accommodations = acommodations;
                                break;
                            }
                    }
                }
            }
            return newArrangement;
        }

        private ArrangementNSC ParseXMLNodeNSC(XmlNode arrangement)
        {
            ArrangementNSC newArrangement = new ArrangementNSC();
            foreach (XmlNode childNode in arrangement.ChildNodes)
            {
                if (childNode.NodeType == XmlNodeType.Element)
                {
                    switch (childNode.Name)
                    {
                        case "NAME": { newArrangement.Name = childNode.InnerText; break; }
                        case "DATESTART": { newArrangement.DateStart = childNode.InnerText; break; }
                        case "DATESTOP": { newArrangement.DateStop = childNode.InnerText; break; }
                        case "POSTERURL": { newArrangement.PosterURL = childNode.InnerText; break; }
                        case "TRANSPORT": { newArrangement.Transport = childNode.InnerText; break; }
                        case "TYPE": { newArrangement.Type = childNode.InnerText; break; }
                        case "ACCOMMODATIONS":
                            {
                                List<ulong> acommodations = new List<ulong>();
                                foreach (XmlNode accommodation in childNode)
                                    acommodations.Add(ulong.Parse(accommodation.InnerText));

                                uint minOffer = uint.MaxValue;
                                uint tmpVar;
                                foreach (var acommodation in acommodations)
                                {
                                    if ((tmpVar = acommodationHandler.GetCheapestUnit(acommodation)) < minOffer)
                                        minOffer = tmpVar;
                                }
                                if(minOffer == uint.MaxValue)
                                    newArrangement.MinPrice="NO OFFERS";
                                else
                                    newArrangement.MinPrice = minOffer.ToString();
                                break;
                            }
                    }
                }

                
            }
            var start = DateTime.ParseExact(newArrangement.DateStart, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            var stop = DateTime.ParseExact(newArrangement.DateStop, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            newArrangement.Days = ushort.Parse(stop.Subtract(start).Days.ToString());

            return newArrangement;
        }

        public List<Arrangement> GetArrangementsByIDs(List<ulong> keys)
        {
            List<Arrangement> outList = new List<Arrangement>();
            lock(arrangementsMutex)
            {
                xmlDoc.Load(xmlPath);
                string groupStr = "";
                foreach (var key in keys) groupStr += String.Format(" or AID = '{0}'", key);
                groupStr = groupStr.TrimStart(new char[] { ' ', 'o', 'r' });

                var arrangements  = xmlDoc.SelectNodes(String.Format("/ARRANGEMENTS/ARRANGEMENT[ ({0}) and @deleted = '0']", groupStr));

                foreach (XmlNode arrangement in arrangements)
                {
                    if (arrangement.NodeType == XmlNodeType.Element)
                        outList.Add(ParseXMLNode(arrangement));
                }                 
            }

            return outList;
        }

        public List<ArrangementNSC> GetArrangementsByIDsNSC(List<ulong> keys)
        {
            List<ArrangementNSC> outList = new List<ArrangementNSC>();
            lock (arrangementsMutex)
            {
                xmlDoc.Load(xmlPath);
                string groupStr = "";
                foreach (var key in keys) groupStr += String.Format(" or AID = '{0}'", key);
                groupStr = groupStr.TrimStart(new char[] { ' ', 'o', 'r' });

                var arrangements = xmlDoc.SelectNodes(String.Format("/ARRANGEMENTS/ARRANGEMENT[ ({0}) and @deleted = '0']", groupStr));

                foreach (XmlNode arrangement in arrangements)
                {
                    if (arrangement.NodeType == XmlNodeType.Element)
                        outList.Add(ParseXMLNodeNSC(arrangement));
                }
            }

            return outList;
        }

        public Arrangement GetArrangementByID(ulong key)
        {
            List<Arrangement> outList = new List<Arrangement>();
            lock (arrangementsMutex)
            {
                xmlDoc.Load(xmlPath);

                var arrangement = xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[ AID = '{0}' and @deleted = '0']", key));
                return ParseXMLNode(arrangement);
            }
        }

        public string GetNameByID(ulong key)
        {
            List<Arrangement> outList = new List<Arrangement>();
            lock (arrangementsMutex)
            {
                xmlDoc.Load(xmlPath);

                var arrangement = xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[ AID = '{0}' and @deleted = '0']/NAME", key));
                return arrangement.InnerText;
            }
        }

        public bool ContainsByName(string name)
        {
            lock (arrangementsMutex)
            {
                xmlDoc.Load(xmlPath);

                var arrangement = xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[ @deleted = '0' and NAME = '{0}' ]", name));
                return arrangement != null ? true : false;
            }
        }

        public List<string> GetIncomingNames()
        {
            return ReadAll().Where(x => TimeCalculator.CheckTimeRelationMine(x.DateStart) > 0).Select(x => x.Name).ToList<string>();
        }

        public List<string> GetCurrentAndFutureIDs()
        {
            return ReadAll().Where(x => TimeCalculator.CheckTimeRelationMine(x.DateStart) >= 0).Select(x => x.AID.ToString()).ToList<string>();
        }

        public Arrangement GetArrangementByName(string key)
        {
            lock (arrangementsMutex)
            {
                xmlDoc.Load(xmlPath);
                return ParseXMLNode(xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted='0']", key))); 
            }
        }

        public ArrangementNSC GetArrangementByIDNSC(string key)
        {
            lock (arrangementsMutex)
            {
                xmlDoc.Load(xmlPath);
                return ParseXMLNodeNSC(xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted='0']", key)));
            }
        }

        public List<ArrangementNSC> GetArrangementsNSC()
        {

            List<ArrangementNSC> outList = new List<ArrangementNSC>();
            lock (arrangementsMutex)
            {
                xmlDoc.Load(xmlPath);
                XmlNode targetNode=xmlDoc.SelectSingleNode("/ARRANGEMENTS");
                if (targetNode == null) return outList;

                foreach(XmlNode arrangement in targetNode.ChildNodes)
                {
                    if(arrangement.NodeType==XmlNodeType.Element && arrangement.Name== "ARRANGEMENT" &&
                         arrangement.Attributes["deleted"].Value == "0")
                    { 
                        outList.Add(ParseXMLNodeNSC(arrangement));
                    }
                }
            }
            return outList;
        }

        public List<string> GetActiveArrangementsNSCCreatedBy(string manager)
        {
            List<string> outList = new List<string>();
            var keys = usersHandler.GetArrangementsForManager(manager);
            lock (arrangementsMutex)
            {
                xmlDoc.Load(xmlPath);
                string groupStr = "";
                foreach (var key in keys) groupStr += String.Format(" or AID = '{0}'", key);
                groupStr = groupStr.TrimStart(new char[] { ' ', 'o', 'r' });

                var arrangements = xmlDoc.SelectNodes(String.Format("/ARRANGEMENTS/ARRANGEMENT[ ({0}) and @deleted = '0']/NAME", groupStr));

                foreach (XmlNode arrangement in arrangements)
                {
                    outList.Add(arrangement.InnerText);
                }
            }

            return outList;
        }

        public List<ArrangementNSC> GetActiveArrangementsNSC()
        {

            List<ArrangementNSC> outList = new List<ArrangementNSC>();
            lock (arrangementsMutex)
            {
                xmlDoc.Load(xmlPath);
                XmlNode targetNode = xmlDoc.SelectSingleNode("/ARRANGEMENTS");
                if (targetNode == null) return outList;

                foreach (XmlNode arrangement in targetNode.ChildNodes)
                {
                    if (arrangement.NodeType == XmlNodeType.Element && arrangement.Name == "ARRANGEMENT" && 
                        arrangement.Attributes["deleted"].Value=="0")
                    {
                        ArrangementNSC newArrangement = ParseXMLNodeNSC(arrangement);
                        if(TimeCalculator.CheckTimeRelationMine(newArrangement.DateStart)>0) outList.Add(newArrangement);  
                    }
                }
            }
            return SortByDateStartAsc(outList);
        }

        public List<ArrangementNSC> GetHistoryAndOngoingArrangementsNSC()
        {

            List<ArrangementNSC> outList = new List<ArrangementNSC>();
            lock (arrangementsMutex)
            {
                xmlDoc.Load(xmlPath);
                XmlNode targetNode = xmlDoc.SelectSingleNode("/ARRANGEMENTS");
                if (targetNode == null) return outList;

                foreach (XmlNode arrangement in targetNode.ChildNodes)
                {
                    if (arrangement.NodeType == XmlNodeType.Element && arrangement.Name == "ARRANGEMENT" &&
                        arrangement.Attributes["deleted"].Value == "0")
                    {
                        ArrangementNSC newArrangement = ParseXMLNodeNSC(arrangement);

                        if (TimeCalculator.CheckTimeRelationMine(newArrangement.DateStop)  < 0  ||
                           (TimeCalculator.CheckTimeRelationMine(newArrangement.DateStart) <= 0 &&
                            TimeCalculator.CheckTimeRelationMine(newArrangement.DateStop)  >= 0))
                                  outList.Add(newArrangement); 
                    }
                }
            }
            return SortByDateStopDesc(outList);
        }

        public List<string> GetCurrentAndFutureAssignedAccommodations()
        {
            return ReadAll().Where(x => TimeCalculator.CheckTimeRelationMine(x.DateStop) >= 0).Select(x => x.AID).Select(x=>x.ToString()).ToList(); 
        }


        private List<ArrangementNSC> SortByDateStartAsc(List<ArrangementNSC> unorderedList)
        {
            int minimum;
            for(int swLoc=0; swLoc<unorderedList.Count-1; ++swLoc)
            {
                minimum = swLoc;
                for(int toCompare=swLoc+1; toCompare< unorderedList.Count; ++toCompare)
                {
                    // curr loc is gt curr max
                    if (TimeCalculator.CheckTimeRelation(unorderedList[toCompare].DateStart, unorderedList[minimum].DateStart) < 0)
                        minimum = toCompare;
                }
                if(swLoc!= minimum)
                { unorderedList.Insert(swLoc, unorderedList[minimum]); unorderedList.RemoveAt(minimum + 1); }
            }
            return unorderedList;
        }

        private List<ArrangementNSC> SortByDateStopDesc(List<ArrangementNSC> unorderedList)
        {
            int minimum;
            for (int swLoc = 0; swLoc < unorderedList.Count - 1; ++swLoc)
            {
                minimum = swLoc;
                for (int toCompare = swLoc + 1; toCompare < unorderedList.Count; ++toCompare)
                {
                    // curr loc is gt curr max
                    if (TimeCalculator.CheckTimeRelation(unorderedList[toCompare].DateStop, unorderedList[minimum].DateStop) < 0)
                        minimum = toCompare;
                }
                if (swLoc != minimum)
                { unorderedList.Insert(swLoc, unorderedList[minimum]); unorderedList.RemoveAt(minimum + 1); }
            }
            return unorderedList;
        }
    
        public bool ContainsArrangement(Arrangement arrangement)
        {
            lock (arrangementsMutex)
            {

                xmlDoc.Load(xmlPath);
                var synonims = xmlDoc.SelectNodes(
                    String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted = '0']", arrangement.Name));
                if (synonims.Count != 0) return true;
                return false;

            }
        }

        public bool ContainsArrangement(string name)
        {
            lock (arrangementsMutex)
            {

                xmlDoc.Load(xmlPath);
                var synonims = xmlDoc.SelectNodes(
                    String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted = '0']", name));
                if (synonims.Count != 0) return true;
                return false;

            }
        }

        public string GetAIDByName(string name)
        {
            lock (arrangementsMutex)
            {
                xmlDoc.Load(xmlPath);
                XmlNode target =xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted = '0']/AID", name));
                return target.InnerText;
            }
        }

        public List<Arrangement> GetAllBetween(string dateStart, string dateStop)
        {
            return ReadAll().Where(x => TimeCalculator.CheckTimeRelation(x.DateStart, dateStart) >= 0 && TimeCalculator.CheckTimeRelation(x.DateStop, dateStop) <= 0).ToList();
        }

        public bool StoreArrangement(Arrangement arrangement, string username)
        {
            if (ContainsArrangement(arrangement)) return false;
            InitKeyLoad();

            lock (arrangementsMutex)
            {
                string[] parts;
                string imgName="";
                if (arrangement.PosterURL != "")
                {
                    parts = arrangement.PosterURL.Split(new char[] { '>' }, StringSplitOptions.RemoveEmptyEntries);
                    imgName = parts[0];
                    arrangement.PosterURL = parts[1];
                }

                arrangement.AID = TakeKey();
                xmlDoc.Load(xmlPath);
                XmlNode newArrangement = xmlDoc.CreateElement("ARRANGEMENT");
                newArrangement.Attributes.Append(xmlDoc.CreateAttribute("deleted"));
                newArrangement.Attributes[0].Value = "0";

                XmlNode newAID = xmlDoc.CreateElement("AID");
                XmlNode newName = xmlDoc.CreateElement("NAME");
                XmlNode newType = xmlDoc.CreateElement("TYPE");
                XmlNode newTransport = xmlDoc.CreateElement("TRANSPORT");
                XmlNode newLocation = xmlDoc.CreateElement("LOCATION");
                XmlNode newDateStart = xmlDoc.CreateElement("DATESTART");
                XmlNode newDateStop = xmlDoc.CreateElement("DATESTOP");

                newAID.InnerText = arrangement.AID.ToString();
                newName.InnerText = arrangement.Name;
                newType.InnerText = arrangement.Type;
                newTransport.InnerText = arrangement.Transport;
                newLocation.InnerText = arrangement.Locationn;
                newDateStart.InnerText = arrangement.DateStart;
                newDateStop.InnerText = arrangement.DateStop;

                newArrangement.AppendChild(newAID);
                newArrangement.AppendChild(newName);
                newArrangement.AppendChild(newType);
                newArrangement.AppendChild(newTransport);
                newArrangement.AppendChild(newLocation);
                newArrangement.AppendChild(newDateStart);
                newArrangement.AppendChild(newDateStop);

                XmlNode newMeetingSpot = xmlDoc.CreateElement("MEETINGSPOT");
                    XmlNode newAddress      = xmlDoc.CreateElement("ADDRESS");
                    XmlNode newGeoLongitude = xmlDoc.CreateElement("GEOLONGITUDE");
                    XmlNode newGeoLatitude = xmlDoc.CreateElement("GEOLATITUDE");

                    newAddress.InnerText = arrangement.MeetingSpot.Address;
                    newGeoLongitude.InnerText = arrangement.MeetingSpot.GeoLongitude.ToString();
                    newGeoLatitude.InnerText = arrangement.MeetingSpot.GeoLatitude.ToString();

                    newMeetingSpot.AppendChild(newAddress);
                    newMeetingSpot.AppendChild(newGeoLatitude);
                    newMeetingSpot.AppendChild(newGeoLongitude);
                newArrangement.AppendChild(newMeetingSpot);

                XmlNode newMeetingTime = xmlDoc.CreateElement("MEETINGTIME");
                XmlNode newMaxPassengers = xmlDoc.CreateElement("MAXPASSENGERS");
                XmlNode newDesc = xmlDoc.CreateElement("DESC");
                XmlNode newProgramm = xmlDoc.CreateElement("PROGRAM");
                XmlNode newPosterURL = xmlDoc.CreateElement("POSTERURL");

                newMeetingTime.InnerText = arrangement.MeetingTime;
                newMaxPassengers.InnerText = arrangement.MaxPassengers.ToString();
                newDesc.InnerText = arrangement.Desc;
                newProgramm.InnerText = arrangement.Program;

                if(imgName != "")
                {
                    try { newPosterURL.InnerText = "http://localhost:62468/Content/Images/" + HandleUserImage(arrangement.PosterURL, imgName); }   // HERE
                    catch (Exception exc)
                    {
                        ReturnKey(arrangement.AID);
                        throw exc;
                    }

                }
                else newPosterURL.InnerText = "http://localhost:62468/Content/Images/default.jpg";

                newArrangement.AppendChild(newMeetingTime);
                newArrangement.AppendChild(newMaxPassengers);
                newArrangement.AppendChild(newDesc);
                newArrangement.AppendChild(newProgramm);
                newArrangement.AppendChild(newPosterURL);

                XmlNode newAccommodations = xmlDoc.CreateElement("ACCOMMODATIONS");
                foreach(var acID in arrangement.Accommodations)
                {
                    XmlNode newAcID = xmlDoc.CreateElement("AC_ID");
                    newAcID.InnerText=acID.ToString();
                    newAcID.Attributes.Append(xmlDoc.CreateAttribute("deleted"));
                    newAcID.Attributes[0].Value = "0";

                    newAccommodations.AppendChild(newAcID);

                }
                newArrangement.AppendChild(newAccommodations);

                xmlDoc.DocumentElement.AppendChild(newArrangement);
                xmlDoc.Save(xmlPath);

                if (username!=null)
                    usersHandler.AppendArrangementToManager(arrangement.AID.ToString(), username);
            }

            return true;
        }

        private string HandleUserImage(string base64String, string imgName)
        {
            string path = System.Web.HttpContext.Current.Server.MapPath("~/Content/Images/");
            ImageFormat format = ImageFormat.Bmp;

            string extension="";
            switch (imgName.Split(new char[] { '.' })[1])
            {
                case "jpg": { format = ImageFormat.Jpeg; extension = ".jpg"; break; }
                case "png": { format = ImageFormat.Png;  extension = ".png"; break; }
                default: { throw new Exception("Image format not suported"); }
            }
            string outPath = path + imgName;

            ulong counter = 0;
            string[] parts;
            //Initial pass to format
            parts = Regex.Split(outPath, @"(?<=[.])");
            parts[parts.Length - 2]=parts[parts.Length-2].Substring(0, parts[parts.Length - 2].Length-1);
            parts = parts.Take(parts.Count() - 1).ToArray();
            outPath = String.Join("",parts)+"_"+counter+extension;  // To format imgName_0.extension

            char[] trimCriteria = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '_' };
            while (File.Exists(outPath))
            {
                if(counter==ulong.MaxValue)
                {
                    outPath = string.Format(@"{0}.txt", Guid.NewGuid());
                    break;
                }

                counter++;
                parts = Regex.Split(outPath, @"(?<=[.])");
                parts[parts.Length - 2] = parts[parts.Length - 2].Substring(0, parts[parts.Length - 2].Length - 1);
                parts = parts.Take(parts.Count() - 1).ToArray();              // Extract name
                parts[0] = parts[0].TrimEnd(trimCriteria);
                outPath = String.Join("", parts) + "_" + counter + extension; // Form new name
            }

            byte[] bytes = Convert.FromBase64String(base64String);
            Image image;
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                image = Image.FromStream(ms);
                image.Save(path+imgName, format);
                return imgName;
            }
        } 

        public Dictionary<string, string> GetNameAIDKVPairs(List<string> keys)
        {
            lock (arrangementsMutex)
            {
                xmlDoc.Load(xmlPath);
                string groupStr = "";
                foreach (var key in keys) groupStr += String.Format(" or AID = '{0}'", key);
                groupStr = groupStr.TrimStart(new char[] { ' ', 'o', 'r' });

                var target = xmlDoc.SelectNodes(String.Format("/ARRANGEMENTS/ARRANGEMENT[ ({0}) and @deleted = '0']", groupStr));
                string AID = "";
                string name = "";
                Dictionary<string, string> outCollection = new Dictionary<string, string>();
                foreach(XmlNode arrangement in target)
                {
                    if (arrangement.NodeType == XmlNodeType.Element)
                    {
                        foreach (XmlNode param in arrangement.ChildNodes)
                        {
                            if (param.NodeType == XmlNodeType.Element)
                            {
                                if (param.Name == "AID") AID = param.InnerText;
                                else if (param.Name == "NAME") name = param.InnerText;
                            }
                        }
                    }

                    if (AID != "" && name != "") outCollection.Add(AID, name);
                    AID = "";
                    name = "";
                }
                return outCollection;         
            }
        }

        public Dictionary<string, Arrangement> GetAIDArrangementsKVPairs(List<string> keys)
        {
            lock (arrangementsMutex)
            {
                xmlDoc.Load(xmlPath);
                string groupStr = "";
                foreach (var key in keys) groupStr += String.Format(" or AID = '{0}'", key);
                groupStr = groupStr.TrimStart(new char[] { ' ', 'o', 'r' });

                var target = xmlDoc.SelectNodes(String.Format("/ARRANGEMENTS/ARRANGEMENT[ ({0}) and @deleted = '0']", groupStr));
                Dictionary<string, Arrangement> outCollection = new Dictionary<string, Arrangement>();
                Arrangement tmpArrangment = null;
                foreach (XmlNode arrangement in target)
                {
                    if (arrangement.NodeType == XmlNodeType.Element)
                    {
                        tmpArrangment = ParseXMLNode(arrangement);
                        outCollection.Add(tmpArrangment.AID.ToString(), tmpArrangment);
                    }
                }
                return outCollection;
            }
        }

        public string CheckArrangementStatus(Arrangement arrangment)
        {
            string outStr = "";         
            if (arrangment.Locationn == null) outStr += "Empty location field;\n";
            if (arrangment.MeetingSpot == null) outStr += "Empty meeting spot fields;\n";
            else if (arrangment.MeetingSpot.Address == "") outStr += "Empty address field;\n";         

            if (arrangment.DateStart == "") outStr += "Starting date not defined;\n";
            if (arrangment.DateStop == "") outStr += "Ending date not defined;\n";

            return outStr;
        }

        public bool IsDeletable(string name)
        {
            return !reservationsHandler.ContainsReservationForArrangement(name) ;
        }

        public void Remove(string name)
        {
            lock (arrangementsMutex)
            {
                xmlDoc.Load(xmlPath);
                var target=xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted='0']", name));
                target.Attributes["deleted"].Value = "1";
                xmlDoc.Save(xmlPath);
            }
        }

        public void UpdateArrangement(string id, Arrangement arrangement)
        {
            lock (arrangementsMutex)
            {

                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                var node = xmlDoc.SelectSingleNode
                    ( String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted = '0']", id));

                if (node == null) throw new Exception("Original not found");
  
                xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted = '0']/TYPE", id)).InnerText                     = arrangement.Type;
                xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted = '0']/TRANSPORT", id)).InnerText                = arrangement.Transport;
                xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted = '0']/LOCATION", id)).InnerText                 = arrangement.Locationn;
                xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted = '0']/DATESTART", id)).InnerText                = arrangement.DateStart;
                xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted = '0']/DATESTOP", id)).InnerText                 = arrangement.DateStop;
                xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted = '0']/MEETINGSPOT/ADDRESS", id)).InnerText      = arrangement.MeetingSpot.Address;
                xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted = '0']/MEETINGSPOT/GEOLONGITUDE", id)).InnerText = arrangement.MeetingSpot.GeoLongitude.ToString();
                xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted = '0']/MEETINGSPOT/GEOLATITUDE", id)).InnerText  = arrangement.MeetingSpot.GeoLatitude.ToString();
                xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted = '0']/MEETINGTIME", id)).InnerText              = arrangement.MeetingTime;
                xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted = '0']/MAXPASSENGERS", id)).InnerText            = arrangement.MaxPassengers.ToString();
                xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted = '0']/DESC", id)).InnerText                     = arrangement.Desc;
                xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted = '0']/PROGRAM", id)).InnerText                  = arrangement.Program;

                string[] parts;
                string imgName = "";
                if (arrangement.PosterURL != "")
                {
                    parts = arrangement.PosterURL.Split(new char[] { '>' }, StringSplitOptions.RemoveEmptyEntries);
                    imgName = parts[0];
                    arrangement.PosterURL = parts[1];
                }
                if (imgName != "")
                {
                    try
                    {
                        xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted = '0']/POSTERURL", id)).InnerText
                      = "http://localhost:62468/Content/Images/" + HandleUserImage(arrangement.PosterURL, imgName);
                    }   
                    catch (Exception exc)
                    { throw exc; }
               }
                

                xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted = '0']/NAME", id)).InnerText                     = arrangement.Name;

                xmlDoc.Save(xmlPath);
            }
        }

        public void RemoveAccommodationFromArrangement(string arrangementName, string accommodationName)
        {
            lock (arrangementsMutex)
            {
                xmlDoc.Load(xmlPath);
                var target = xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted='0']/ACCOMMODATIONS/AC_ID[ @deleted = '0' and text() = '{1}' ]", 
                    arrangementName, acommodationHandler.GetIDByName(accommodationName)));
                if (target == null) throw new Exception("Internal error, consistency violated ");
                
                target.Attributes["deleted"].Value = "1";
                xmlDoc.Save(xmlPath);
            }
        }

        public void AddAccommodationToArrangement(string arrangementName, string acID)
        {
            lock (arrangementsMutex)
            {
                xmlDoc.Load(xmlPath);
                var targetArrangement = xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted='0']/ACCOMMODATIONS",
                    arrangementName, acID));
                if (targetArrangement == null) throw new Exception("Internal error, arrangement not found ");

                var duplicate = xmlDoc.SelectSingleNode(String.Format("/ARRANGEMENTS/ARRANGEMENT[NAME = '{0}' and @deleted='0']/ACCOMMODATIONS/AC_ID[ @deleted = '0' and text() = '{1}' ]",
                    arrangementName, acID));

                if(duplicate!=null) throw new Exception("Internal error, consistency violated ");

                XmlNode newAccommodation = xmlDoc.CreateElement("AC_ID");
                newAccommodation.Attributes.Append(xmlDoc.CreateAttribute("deleted"));
                newAccommodation.Attributes[0].Value = "0";
                newAccommodation.InnerText = acID;
                targetArrangement.AppendChild(newAccommodation);

                xmlDoc.Save(xmlPath);
            }
        }

        public bool IsAccommodationUsedInAny(string acID)
        {
            lock (arrangementsMutex)
            {
                xmlDoc.Load(xmlPath);
                var result = xmlDoc.SelectNodes(String.Format("/ARRANGEMENTS/ARRANGEMENT[@deleted='0']/ACCOMMODATIONS/AC_ID[@deleted='0' and text() = '0']", acID));
                if (result == null) return false;
                return result.Count > 0 ? true : false;
            }
        }

        public bool IsAccommodationUsedInCurrentOrOngoing(string acID)
        {
            lock (arrangementsMutex)
            {
                xmlDoc.Load(xmlPath);
                var results = xmlDoc.SelectNodes(String.Format("/ARRANGEMENTS/ARRANGEMENT[@deleted='0']/ACCOMMODATIONS/AC_ID[@deleted='0' and text() = '{0}']", acID));
                if (results == null) return false;

                foreach (XmlNode result in results)
                    if (TimeCalculator.CheckTimeRelationMine(ParseXMLNode(result.ParentNode.ParentNode).DateStart) <= 0) return true; // Vec poceo
                return false;
            }
        }

        public bool IsAccommodationUsedInHistory(string acID)
        {
            lock (arrangementsMutex)
            {
                xmlDoc.Load(xmlPath);
                var results = xmlDoc.SelectNodes(String.Format("/ARRANGEMENTS/ARRANGEMENT[@deleted='0']/ACCOMMODATIONS/AC_ID[@deleted='0' and text() = '0']", acID));
                if (results == null) return false;

                foreach (XmlNode result in results)
                    if (TimeCalculator.CheckTimeRelationMine(ParseXMLNode(result.ParentNode.ParentNode).DateStart) < 0) return true;

                return false;
            }
        }

    }
}