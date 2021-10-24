using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using TravelAgency.Models;

namespace TravelAgency.Handlers
{
    public class AccommodationHandler
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
                    var usedKeys = xmlDoc.SelectNodes("/ACCOMMODATIONS/ACCOMMODATION[ @deleted = '0']/AC_ID");
                    foreach (XmlNode aID in usedKeys)
                        keyList.Add(ulong.Parse(aID.InnerText));

                    var deletedKeys = xmlDoc.SelectNodes("/ACCOMMODATIONS/ACCOMMODATION[ @deleted = '1']/AC_ID");
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

        private static AccommodationHandler singletoneInstance = null;
        private static readonly string xmlPath = HttpContext.Current.Server.MapPath("~/App_Data/Files/Accommodations.xml");
        private static XmlDocument xmlDoc = new XmlDocument();
        private static object accommodationMutex = new object();

        private static ReservationHandler reservationHandler = ReservationHandler.GetInstance();
        private static ArrangementsHandler arrangementHandler = ArrangementsHandler.GetInstance();


        private AccommodationHandler()
        {
            InitKeyLoad();
        }

        public static AccommodationHandler GetInstance()
        {
            if (singletoneInstance == null) singletoneInstance = new AccommodationHandler();
            return singletoneInstance;
        }

        public Accommodation GetByName(string key)
        {
            lock (accommodationMutex)
            {
                xmlDoc.Load(xmlPath);
                XmlNode accommodation = xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[ NAME = '{0}' and @deleted = '0']", key));
                if (accommodation == null) throw new Exception("Not found");
                return ParseXMLNode(accommodation);
            }
        }

        public string GetNameByID(string key)
        {
            lock (accommodationMutex)
            {
                xmlDoc.Load(xmlPath);
                XmlNode tagetNode = xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[ AC_ID = '{0}' and @deleted = '0']/NAME", key));
                return tagetNode.InnerText;
            }
        }

        public string GetIDByName(string key)
        {
            lock (accommodationMutex)
            {
                xmlDoc.Load(xmlPath);
                XmlNode accommodation = xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[ NAME = '{0}' and @deleted = '0']/AC_ID", key));
                if (accommodation == null) throw new Exception("Not found");
                return accommodation.InnerText;
            }
        }

        private AccommodationUnit ParseXMLNodeUnit(XmlNode unit)
        {
                AccommodationUnit newUnit = new AccommodationUnit();
                foreach (XmlNode unitParam in unit.ChildNodes)
                {
                    if (unitParam.NodeType == XmlNodeType.Element)
                    {
                        switch (unitParam.Name)
                        {
                            case "UNIT_ID": { newUnit.UID = unitParam.InnerText; break; }
                            case "MAX_GUESSTS": { newUnit.MaxGuessts = Int32.Parse(unitParam.InnerText); break; }
                            case "PETALLOWED": { newUnit.PetAllowed = unitParam.InnerText.Equals("y") ? true : false; break; }
                            case "PRICE": { newUnit.Price = Int32.Parse(unitParam.InnerText); break; }
                        }
                    }
                }
            return newUnit;
        }

        private Accommodation ParseXMLNode(XmlNode accommodation, string arrangement)
        {
            Accommodation newAccommodation = new Accommodation();
            foreach (XmlNode childNode in accommodation.ChildNodes)
            {
                if (childNode.NodeType == XmlNodeType.Element)
                {
                    switch (childNode.Name)
                    {
                        case "AC_ID": { newAccommodation.AcID = ulong.Parse(childNode.InnerText); break; }
                        case "LOCATION": { newAccommodation.Location = childNode.InnerText; break; }
                        case "TYPE": { newAccommodation.Type = childNode.InnerText; break; }
                        case "NAME": { newAccommodation.Name = childNode.InnerText; break; }
                        case "STARS": { newAccommodation.Stars = childNode.InnerText; break; }
                        case "POOL": { newAccommodation.Pool = childNode.InnerText.Equals("y") ? true : false; break; }
                        case "SPA": { newAccommodation.Spa = childNode.InnerText.Equals("y") ? true : false; break; }
                        case "DISABLEDCOMPATIBLE": { newAccommodation.DisabledCompatible = childNode.InnerText.Equals("y") ? true : false; break; }
                        case "WIFI": { newAccommodation.WiFi = childNode.InnerText.Equals("y") ? true : false; break; }
                        case "ACCOMODATIONUNITS":
                            {
                                foreach (XmlNode unit in childNode.ChildNodes)
                                {
                                    if (unit.NodeType == XmlNodeType.Element && unit.Name == "UNIT" && unit.Attributes["deleted"].Value == "0")
                                    {
                                        AccommodationUnit newUnit = new AccommodationUnit();
                                        foreach (XmlNode unitParam in unit.ChildNodes)
                                        {
                                            if (unitParam.NodeType == XmlNodeType.Element)
                                            {
                                                switch (unitParam.Name)
                                                {
                                                    case "UNIT_ID": { newUnit.UID = unitParam.InnerText; break; }
                                                    case "MAX_GUESSTS": { newUnit.MaxGuessts = Int32.Parse(unitParam.InnerText); break; }
                                                    case "PETALLOWED": { newUnit.PetAllowed = unitParam.InnerText.Equals("y") ? true : false; break; }
                                                    case "PRICE": { newUnit.Price = Int32.Parse(unitParam.InnerText); break; }
                                                }
                                            }
                                        }
                                        newUnit.Available = reservationHandler.IsAvailable(arrangementHandler.GetArrangementByName(arrangement), newAccommodation.AcID.ToString(), newUnit.UID);
                                        newAccommodation.Units.Add(newUnit);
                                    }
                                }
                                break;
                            }
                    }
                }

            }
            return newAccommodation;
        }

        private Accommodation ParseXMLNode(XmlNode accommodation)
        {
            Accommodation newAccommodation = new Accommodation();
            string elemContent = "";
            foreach (XmlNode childNode in accommodation.ChildNodes)
            {
                if (childNode.NodeType == XmlNodeType.Element)
                {
                    elemContent = childNode.InnerText;
                    switch (childNode.Name)
                    {
                        case "AC_ID": { newAccommodation.AcID = ulong.Parse(elemContent); break; }
                        case "LOCATION": { newAccommodation.Location = elemContent; break; }
                        case "TYPE": { newAccommodation.Type = elemContent; break; }
                        case "NAME": { newAccommodation.Name = elemContent; break; }
                        case "STARS": { newAccommodation.Stars = elemContent; break; }
                        case "POOL": { newAccommodation.Pool = elemContent == "y" ? true : false; break; }
                        case "SPA": { newAccommodation.Spa = elemContent == "y" ? true : false; break; }
                        case "DISABLEDCOMPATIBLE": { newAccommodation.DisabledCompatible = elemContent == "y" ? true : false; break; }
                        case "WIFI": { newAccommodation.WiFi = elemContent == "y" ? true : false; break; }
                        case "ACCOMODATIONUNITS":
                            {
                                List<AccommodationUnit> units = new List<AccommodationUnit>();
                                foreach (XmlNode accommodationUnit in accommodation.ChildNodes)
                                {
                                    AccommodationUnit newUnit = null;
                                    if (accommodationUnit.NodeType == XmlNodeType.Element && accommodationUnit.Name == "UNIT" &&
                                        accommodationUnit.Attributes["deleted"].Value == "0")
                                    {
                                        foreach (XmlNode unitChildNode in accommodationUnit.ChildNodes)
                                        {
                                            newUnit = new AccommodationUnit();
                                            if (unitChildNode.NodeType == XmlNodeType.Element)
                                            {
                                                elemContent = unitChildNode.InnerText;
                                                switch (unitChildNode.Name)
                                                {
                                                    case "UNIT_ID": { newUnit.UID = elemContent; break; }
                                                    case "MAX_GUESSTS": { newUnit.MaxGuessts = int.Parse(elemContent); break; }
                                                    case "PETALLOWED": { newUnit.PetAllowed = elemContent == "y" ? true : false; break; }
                                                    case "PRICE": { newUnit.Price = int.Parse(elemContent); break; }
                                                }
                                            }
                                        }
                                        units.Add(newUnit);
                                    }
                                }
                                newAccommodation.Units = units;
                                break;
                            }
                    }
                }
            }
            return newAccommodation;
        }

        public List<Accommodation> ReadAll()
        {
            lock (accommodationMutex)
            {
                List<Accommodation> outList = new List<Accommodation>();
                xmlDoc.Load(xmlPath);
                foreach (XmlNode xmlNode in xmlDoc.DocumentElement)
                {
                    if (xmlNode.NodeType == XmlNodeType.Element &&
                        xmlNode.Name == "ACCOMMODATION" &&
                        xmlNode.Attributes["deleted"].Value == "0")
                             outList.Add(ParseXMLNode(xmlNode)); 
                }
                return outList;
            }
        }

        public List<Accommodation> ReadByKeys(List<string> keys, string arrangement )
        {
            List<Accommodation> outList = new List<Accommodation>();
            lock (accommodationMutex)
            {
                xmlDoc.Load(xmlPath);
                string groupStr = "";
                foreach (var key in keys) groupStr += String.Format(" or AC_ID = '{0}'", key);
                groupStr = groupStr.TrimStart(new char[] { ' ', 'o', 'r' });

                var accommodations = xmlDoc.SelectNodes(String.Format("/ACCOMMODATIONS/ACCOMMODATION[ ({0}) and @deleted = '0']", groupStr));

                foreach (XmlNode accommodation in accommodations)
                {
                    if (accommodation.NodeType == XmlNodeType.Element)
                            outList.Add(ParseXMLNode(accommodation, arrangement));                     
                }
            }

            return outList;
        }

        public Accommodation ReadByKey(string key, string arrangement)
        {
            lock (accommodationMutex)
            {
                xmlDoc.Load(xmlPath);
                var accommodation = xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[ AC_ID = '{0}' and @deleted = '0']", key));

                return ParseXMLNode(accommodation, arrangementHandler.GetNameByID(ulong.Parse(arrangement)));
             
            }
            throw new Exception("Not Found");
        }

        public List<Accommodation> ReadByNames(List<string> keys, string arrangement)
        {
            List<Accommodation> outList = new List<Accommodation>();
            lock (accommodationMutex)
            {
                xmlDoc.Load(xmlPath);
                string groupStr = "";
                foreach (var key in keys) groupStr += String.Format(" or NAME = '{0}'", key);
                groupStr = groupStr.TrimStart(new char[] { ' ', 'o', 'r' });

                var accommodations = xmlDoc.SelectNodes(String.Format("/ACCOMMODATIONS/ACCOMMODATION[ ({0}) and @deleted = '0']", groupStr));

                foreach (XmlNode accommodation in accommodations)
                {
                    if (accommodation.NodeType == XmlNodeType.Element)
                        outList.Add(ParseXMLNode(accommodation, arrangement));
                }
            }

            return outList;
        }

        public string ReadNameByID(string id, string arrangement)
        {
            //Find all accommodations for arrangement
            var targetArrangement =arrangementHandler.GetArrangementByName(arrangement);
            lock (accommodationMutex)
            {
                xmlDoc.Load(xmlPath);  
                foreach (var accommodation in targetArrangement.Accommodations)
                {
                    //Pinpoint
                    if(id==accommodation.ToString())
                    {
                        foreach(XmlNode param in xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[ AC_ID = '{0}' and @deleted = '0']", id)).ChildNodes)
                        {
                            if (param.NodeType == XmlNodeType.Element && param.Name == "NAME") return param.InnerText;
                        }
                    }
                }
            }
            return "";
        }

        public AccommodationUnit ReadUnit(string accommodationName, string unitID)
        {
            lock (accommodationMutex)
            {
                xmlDoc.Load(xmlPath);

                XmlNode targetUnit = xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[NAME = '{0}' and @deleted='0']/ACCOMODATIONUNITS/UNIT[@deleted = '0' and UNIT_ID = '{1}']", 
                    accommodationName, unitID));

                return targetUnit == null ? null : ParseXMLNodeUnit(targetUnit);

            }
           
        }

        public uint GetCheapestUnit(ulong acID)
        {
            uint minVal=uint.MaxValue;
            lock (accommodationMutex)
            {
                xmlDoc.Load(xmlPath);
                XmlNode targetAcommodation = xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[AC_ID = '{0}' and @deleted='0']/ACCOMODATIONUNITS", acID));
                if (targetAcommodation == null) throw new Exception("Unknown acID");

                foreach(XmlElement childNode in targetAcommodation.ChildNodes)
                {
                    if(childNode.NodeType==XmlNodeType.Element && childNode.Name== "UNIT" && childNode.Attributes["deleted"].Value == "0")
                    {         
                        foreach(XmlElement unitChild in childNode.ChildNodes)
                        {
                            if (unitChild.NodeType == XmlNodeType.Element && unitChild.Name == "PRICE" && uint.Parse(unitChild.InnerText) < minVal)
                                minVal = uint.Parse(unitChild.InnerText);
                        }
                    }
                }
            }
            return minVal;
        }

        public List<string> GetAllNames()
        {
            lock (accommodationMutex)
            {
                List<string> outList = new List<string>();
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Soruce not found");
                var targetNodes= xmlDoc.SelectNodes("/ACCOMMODATIONS/ACCOMMODATION[ @deleted='0' ]/NAME");
                foreach (XmlNode xmlNode in targetNodes)
                    outList.Add(xmlNode.InnerText);
                return outList;
            }
        }

        public string CheckAccommodationStatus(Accommodation accomodation)
        {
            var errStr = "";
            if (accomodation.Location == "" || accomodation.Location == null) errStr += "Empty Location field; \n";
            if (accomodation.Type == "" || accomodation.Type == null) errStr += "Empty Type field; \n";
            if (accomodation.Name == "" || accomodation.Name == null) errStr += "Empty Name field; \n";
            if (accomodation.Type != "" && accomodation.Type != null && accomodation.Type == "Hotel")
            {
                if (accomodation.Stars == "" || accomodation.Stars == null) errStr += "Empty Stars field; \n";
                else
                {
                    int parsed;
                    if (!Int32.TryParse(accomodation.Stars, out parsed)) errStr += "Stars field not a number; \n";
                    else if (parsed > 5 || parsed < 1) errStr += "Stars value must be between 1 and 5 \n";
                }
            }

            return errStr;
        }

        public string CheckAccUnitStatus(AccommodationUnit accommodationUnit)
        {
            var errStr = "";
            if (accommodationUnit.UID == "" || accommodationUnit.UID == null) errStr += "Empty Name field; \n";
            if (accommodationUnit.MaxGuessts<1) errStr += "Guesst limit must be g.t. 1; \n";
            if (accommodationUnit.Price < 1) errStr += "Price must be g.t. 0; \n";
            return errStr;
        }

        public bool ContainsByName(string name)
        {
            lock (accommodationMutex)
            {
                List<string> outList = new List<string>();
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Soruce not found");
                var targetNode = xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[ @deleted='0' and NAME = '{0}' ]", name));
                if (targetNode == null) return false;
                return true;
            }
        }

        public string StoreAccommodation(Accommodation accommodation)
        {
            lock (accommodationMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                var newAccommodation = xmlDoc.CreateElement("ACCOMMODATION");
                newAccommodation.Attributes.Append(xmlDoc.CreateAttribute("deleted"));
                newAccommodation.Attributes[0].Value = "0";
                var acIDNode = xmlDoc.CreateElement("AC_ID");
                var locationNode = xmlDoc.CreateElement("LOCATION");
                var typeNode = xmlDoc.CreateElement("TYPE");
                var nameNode = xmlDoc.CreateElement("NAME");

                XmlElement starsNode = null;
                if (accommodation.Type == "Hotel")
                    starsNode = xmlDoc.CreateElement("STARS");

                var poolNode = xmlDoc.CreateElement("POOL");
                var spaNode = xmlDoc.CreateElement("SPA");
                var disabledCompatibleNode = xmlDoc.CreateElement("DISABLEDCOMPATIBLE");
                var wifiNode = xmlDoc.CreateElement("WIFI");
                var accUnitsNode = xmlDoc.CreateElement("ACCOMODATIONUNITS");



                acIDNode.InnerText = TakeKey().ToString();
                locationNode.InnerText = accommodation.Location;
                typeNode.InnerText = accommodation.Type;
                nameNode.InnerText = accommodation.Name;
                if (accommodation.Type == "Hotel")
                    starsNode.InnerText = accommodation.Stars;
                poolNode.InnerText = accommodation.Pool ? "y" : "n";
                spaNode.InnerText = accommodation.Spa ? "y" : "n";
                disabledCompatibleNode.InnerText = accommodation.DisabledCompatible ? "y" : "n";
                wifiNode.InnerText = accommodation.WiFi ? "y" : "n";

                newAccommodation.AppendChild(acIDNode);
                newAccommodation.AppendChild(locationNode);
                newAccommodation.AppendChild(typeNode);
                newAccommodation.AppendChild(nameNode);
                if (accommodation.Type == "Hotel")
                    newAccommodation.AppendChild(starsNode);
                newAccommodation.AppendChild(poolNode);
                newAccommodation.AppendChild(spaNode);
                newAccommodation.AppendChild(disabledCompatibleNode);
                newAccommodation.AppendChild(wifiNode);
                newAccommodation.AppendChild(accUnitsNode);

                try
                {
                    xmlDoc.DocumentElement.AppendChild(newAccommodation);
                    xmlDoc.Save(xmlPath);
                }
                catch (Exception exc)
                {
                    ReturnKey(ulong.Parse(acIDNode.InnerText));
                    throw exc;
                }
                return acIDNode.InnerText;
            }

        }

        public void StoreAccommodation(Accommodation accommodation, string arrangementName)
        {          
            arrangementHandler.AddAccommodationToArrangement(arrangementName, StoreAccommodation(accommodation));
        }

        public void Remove(string arrangementName, string accommodationName)
        {
            lock(arrangementHandler)
            {
                arrangementHandler.RemoveAccommodationFromArrangement(arrangementName, accommodationName);  // Ukloni sa tog aranzmana
                PureDelete(accommodationName);
            }
        }

        public void Remove(string accommodationName)
        {
            PureDelete(accommodationName);
        }

        private void PureDelete(string accommodationName)
        {
            xmlDoc.Load(xmlPath);
            var target = xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[ AC_ID = '{0}' and @deleted='0']", this.GetIDByName(accommodationName)));
            target.Attributes["deleted"].Value = "1";
            xmlDoc.Save(xmlPath);
        }

        public void RemoveUnit(string accommodationName, string accUnitName)
        {
            lock (accommodationMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                var reservation = xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[ AC_ID = '{0}' and @deleted='0' ]/ACCOMODATIONUNITS/UNIT[ @deleted='0' and UNIT_ID = '{1}']", 
                                                                                                            this.GetIDByName(accommodationName), accUnitName));
                reservation.Attributes["deleted"].Value = "1";
                xmlDoc.Save(xmlPath);
            }
        }

        public bool UpdateAccommodation(string oldAccName, Accommodation accommodation)
        {

            lock (accommodationMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                XmlElement starsNode;
                XmlNode originalXML = xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[NAME = '{0}' and @deleted ='0']", oldAccName));
                if (originalXML == null) throw new Exception("Original not found exception");

                xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[NAME = '{0}' and @deleted ='0']/LOCATION", oldAccName)).InnerText = accommodation.Location;
                xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[NAME = '{0}' and @deleted ='0']/TYPE", oldAccName)).InnerText = accommodation.Type;
                if(accommodation.Type=="Hotel")
                {
                    var targetNode = xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[NAME = '{0}' and @deleted ='0']/STARS", oldAccName));
                    if(targetNode!=null) targetNode.InnerText = accommodation.Stars;
                    else
                    {
                        starsNode = xmlDoc.CreateElement("STARS");
                        starsNode.InnerText = accommodation.Stars;
                        xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[NAME = '{0}' and @deleted ='0']", oldAccName)).AppendChild(starsNode);
                    }
                }
                else
                {
                    var targetNode = xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[NAME = '{0}' and @deleted ='0']/STARS", oldAccName));
                    if (targetNode!=null)
                        xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[NAME = '{0}' and @deleted ='0']", oldAccName)).RemoveChild(targetNode);
                }
                
                    
                xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[NAME = '{0}' and @deleted ='0']/POOL", oldAccName)).InnerText = accommodation.Pool? "y" : "n";
                xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[NAME = '{0}' and @deleted ='0']/SPA", oldAccName)).InnerText = accommodation.Spa ? "y" : "n";
                xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[NAME = '{0}' and @deleted ='0']/DISABLEDCOMPATIBLE", oldAccName)).InnerText = accommodation.DisabledCompatible ? "y" : "n";
                xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[NAME = '{0}' and @deleted ='0']/WIFI", oldAccName)).InnerText = accommodation.WiFi ? "y" : "n";
                xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[NAME = '{0}' and @deleted ='0']/NAME", oldAccName)).InnerText = accommodation.Name;
                xmlDoc.Save(xmlPath);
            }
            return true;
        }

        public void UpdateAccommodationUnit(string accommodationName, string accUnitName, AccommodationUnit accommodationUnit)
        {
            lock (accommodationMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                XmlNode originalXML = xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[NAME = '{0}' and @deleted ='0']/ACCOMODATIONUNITS/UNIT[@deleted ='0' and UNIT_ID = '{1}']", 
                                                                                                                                                                accommodationName, accUnitName));
                if (originalXML == null) throw new Exception("Original not found exception");

                xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[NAME = '{0}' and @deleted ='0']/ACCOMODATIONUNITS/UNIT[@deleted ='0' and UNIT_ID = '{1}']/MAX_GUESSTS",
                accommodationName, accUnitName)).InnerText=accommodationUnit.MaxGuessts.ToString();

                xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[NAME = '{0}' and @deleted ='0']/ACCOMODATIONUNITS/UNIT[@deleted ='0' and UNIT_ID = '{1}']/PETALLOWED",
                accommodationName, accUnitName)).InnerText = accommodationUnit.PetAllowed ? "y" : "n";

                xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[NAME = '{0}' and @deleted ='0']/ACCOMODATIONUNITS/UNIT[@deleted ='0' and UNIT_ID = '{1}']/PRICE",
                accommodationName, accUnitName)).InnerText = accommodationUnit.Price.ToString();

                xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[NAME = '{0}' and @deleted ='0']/ACCOMODATIONUNITS/UNIT[@deleted ='0' and UNIT_ID = '{1}']/UNIT_ID",
                accommodationName, accUnitName)).InnerText = accommodationUnit.UID;

                xmlDoc.Save(xmlPath);
                if (accUnitName != accommodationUnit.UID)
                    reservationHandler.PropagateAccUnitNameChange(accUnitName, accommodationUnit.UID);
            }
        }

        public bool IsFreeBetween(string dateStart, string dateStop, string accommodationName)
        {
            var arrangements =arrangementHandler.GetAllBetween(dateStart, dateStop);
            var accommodationID = this.GetIDByName(accommodationName);
            foreach(var arrangement in arrangements)
            {
                foreach (var accomodation in arrangement.Accommodations)
                    if (accomodation.ToString() == accommodationID) return false;
            }
            return true;
        }

        public bool AccommodationContainsUnit(string accommodation, string accommodationUnit)
        {
            var targetAccommodation=this.GetByName(accommodation).Units;
            foreach (var unit in targetAccommodation)
                if (unit.UID == accommodationUnit) return true;
            return false;
        }

        public void AddAccUnitToAccommodation(string accommodation, AccommodationUnit accommodationUnit)
        {
            lock(accommodationMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source not found");

                XmlNode targetNode = xmlDoc.SelectSingleNode(String.Format("/ACCOMMODATIONS/ACCOMMODATION[ NAME = '{0}' and @deleted = '0']/ACCOMODATIONUNITS", accommodation));
                if (targetNode == null) throw new Exception("Not found");
                var newUnitNode = xmlDoc.CreateElement("UNIT");
                newUnitNode.Attributes.Append(xmlDoc.CreateAttribute("deleted"));
                newUnitNode.Attributes[0].Value = "0";

                var newIDNode = xmlDoc.CreateElement("UNIT_ID");
                var newMaxGuesstsNode = xmlDoc.CreateElement("MAX_GUESSTS");
                var newPetAllowedNode= xmlDoc.CreateElement("PETALLOWED");
                var newPriceNode = xmlDoc.CreateElement("PRICE");

                newIDNode.InnerText =accommodationUnit.UID; 
                newMaxGuesstsNode.InnerText = accommodationUnit.MaxGuessts.ToString();
                newPetAllowedNode.InnerText = accommodationUnit.PetAllowed ? "y" : "n";
                newPriceNode.InnerText = accommodationUnit.Price.ToString();

                newUnitNode.AppendChild(newIDNode);
                newUnitNode.AppendChild(newMaxGuesstsNode);
                newUnitNode.AppendChild(newPetAllowedNode);
                newUnitNode.AppendChild(newPriceNode);
                targetNode.AppendChild(newUnitNode);
                xmlDoc.Save(xmlPath);
            }
        }

    }
}