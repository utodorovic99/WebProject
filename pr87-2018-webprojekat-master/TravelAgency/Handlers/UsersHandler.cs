using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TravelAgency.Models;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml;
using System.Text.RegularExpressions;
using System.Globalization;

namespace TravelAgency.Handlers
{
    public enum EUserStoreStatus { Success, FailedType, KeyViolation }
    public enum EUserLoginStatus { Success, BadPassword, BadUsername, Fail }

    public class UsersHandler
    {
        private static UsersHandler singletoneInstance = null;
        private static readonly string xmlPath = HttpContext.Current.Server.MapPath("~/App_Data/Files/Users.xml");
        private static XmlDocument xmlDoc = new XmlDocument();
        private static object usersMutex = new object();

        private static ArrangementsHandler arrangementsHandler = null;
        private static ReservationHandler reservationHandler = null;

        private static List<string> registratedTourists = new List<string>();
        private static List<string> registratedAdministrators = new List<string>();
        private static List<string> registratedModerators = new List<string>();

        private static List<string> loggedTourists = new List<string>();
        private static List<string> loggedAdministrators = new List<string>();
        private static List<string> loggedModerators = new List<string>();

        CommentHandler commentsHandler = CommentHandler.GetInstance();

        private UsersHandler()
        {
            arrangementsHandler = ArrangementsHandler.GetInstance();
            reservationHandler = ReservationHandler.GetInstance();
        }

        public static UsersHandler GetInstance()
        {
            if (singletoneInstance == null) singletoneInstance = new UsersHandler();
            return singletoneInstance;
        }

        public bool IsRegistrated(LoginReq login)
        {
            RefreshData();
            return registratedTourists.Contains(login.Username) ||
                    registratedAdministrators.Contains(login.Username) ||
                    registratedModerators.Contains(login.Username);
        }

        public bool LogUserIn(string username, string password)
        {
            return LogUserIn(new LoginReq { Username = username, Password = password }) == EUserLoginStatus.Success;
        }
         
        public EUserLoginStatus LogUserIn(LoginReq login)
        {
            User readUser = null;
            if((readUser = ReadUserByID(login.Username))!=null)
            {
                if (readUser.Password != login.Password) return EUserLoginStatus.BadPassword;

                if (readUser.Role == "a") { loggedAdministrators.Add(login.Username); return EUserLoginStatus.Success; }
                if (readUser.Role == "m") { loggedModerators.Add(login.Username); return EUserLoginStatus.Success; }
                if (readUser.Role == "t") { loggedTourists.Add(login.Username); return EUserLoginStatus.Success; }

                return EUserLoginStatus.Fail;
            }
            return EUserLoginStatus.BadUsername;
        }

        public EUserType CheckLoggedRights(string username)
        {
            if (loggedAdministrators.Contains(username)) return EUserType.Administrator;
            if (loggedTourists.Contains(username)) return EUserType.Tourist;
            if (loggedModerators.Contains(username)) return EUserType.Manager;

            return EUserType.None;
        }

        public void InitialLoad()
        {
            registratedTourists = GetAllTourists().Select(x => x.Username).ToList();
            registratedAdministrators = GetAllAdministartors().Select(x => x.Username).ToList();
            registratedModerators=GetAllMangaers().Select(x => x.Username).ToList();
        }

        public void RefreshData()
        {
            InitialLoad();
        }

        private User ParseXMLNode(XmlNode user)
        {
            User usr = new User();
            Tourist tourist = null;
            Manager manager = null;
            EUserType typeTriggered = EUserType.None;
            List<ulong> keys = new List<ulong>();
            foreach (XmlNode childNode in user)
            {
                if (childNode.NodeType == XmlNodeType.Element)
                {
                    switch ((childNode as XmlNode).Name)
                    {
                        case "USERNAME": { usr.Username = childNode.InnerText; break; }
                        case "PASSWORD": { usr.Password = childNode.InnerText; break; }
                        case "NAME": { usr.Name = childNode.InnerText; break; }
                        case "SURNAME": { usr.Surname = childNode.InnerText; break; }
                        case "GENDER": { usr.Gender = childNode.InnerText; break; }
                        case "EMAIL": { usr.Email = childNode.InnerText; break; }
                        case "BIRTHDATE": { usr.BirthDate = childNode.InnerText; break; }
                        case "ROLE": { usr.Role = childNode.InnerText; break; }
                        case "ARRANGEMENTS":
                            {
                                foreach (XmlNode arrangeKey in (childNode as XmlNode).ChildNodes)
                                {
                                    if (arrangeKey.NodeType == XmlNodeType.Element && arrangeKey.Attributes["deleted"].Value == "0")
                                        keys.Add(ulong.Parse(childNode.InnerText));
                                }


                                if (usr.Role.Equals("m"))
                                {
                                    manager = new Manager(usr);
                                    manager.AddArrangement(arrangementsHandler.GetArrangementsByIDs(keys));
                                    typeTriggered = EUserType.Manager;
                                }
                                else if (usr.Role.Equals("t"))
                                {
                                    tourist = new Tourist(usr);
                                    tourist.AddReservation(reservationHandler.GetReservationsByIDs(keys));
                                    typeTriggered = EUserType.Tourist;
                                }

                                break;
                            }
                    }
                }
            }
            if (typeTriggered == EUserType.Manager) return manager as User;
            if (typeTriggered == EUserType.Tourist) return tourist as User;
            return usr;
        }

        public User ReadUserByID(string key)
        {
            lock (usersMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                XmlNode targetUser = xmlDoc.SelectSingleNode(String.Format("/USERS/USER[USERNAME = '{0}' and @deleted = '0']", key));
                if (targetUser == null) return null;
                if (targetUser.Attributes["banned"].Value == "1") return null;
                return ParseXMLNode(targetUser);
            }
        }

        public List<User> ReadUsers() 
        {

            List<User> outList = new List<User>();
            lock(usersMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                var usersList = xmlDoc.SelectNodes("/USERS/USER[@deleted = '0']");
                foreach (XmlNode xmlNode in usersList)
                {
                    if (xmlNode.NodeType == XmlNodeType.Element)
                        outList.Add(ParseXMLNode(xmlNode));  
                }
            }
            return outList;
            /*Napomena: U XML je bitno da se prije ARRANGEMENTS-a definise ROLE sto ce biti programski rjeseno*/
        }

        public List<Manager> GetAllMangaers()
        {
            List<Manager> outList = new List<Manager>();
            lock (usersMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                var managersList = xmlDoc.SelectNodes("/USERS/USER[ROLE = 'm' and @deleted ='0']");

                foreach (XmlNode xmlNode in managersList)
                {

                    if (xmlNode.NodeType == XmlNodeType.Element)
                        outList.Add(ParseXMLNode(xmlNode) as Manager); 
                }
            }
            return outList;
        }

        public List<Tourist> GetAllTourists()
        {
            List<Tourist> outList = new List<Tourist>();
            lock (usersMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                var touristsList = xmlDoc.SelectNodes("/USERS/USER[ROLE = 't' and @deleted ='0']");

                List<ulong> keys = new List<ulong>();

                foreach (XmlNode xmlNode in touristsList)
                {
                    if (xmlNode.NodeType == XmlNodeType.Element)
                        outList.Add(ParseXMLNode(xmlNode) as Tourist);   
                }
            }
            return outList;
        }

        public List<User> GetAllAdministartors()
        {
            List<User> outList = new List<User>();
            lock (usersMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                var adminList = xmlDoc.SelectNodes("/USERS/USER[ROLE = 'a' and @deleted ='0']");

                foreach (XmlNode xmlNode in adminList)
                {

                    if (xmlNode.NodeType == XmlNodeType.Element)
                       outList.Add(ParseXMLNode(xmlNode));
                    
                }
            }
            return outList;
        }

        private bool ContainsKey(string username)
        {
            lock (usersMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");
                return xmlDoc.SelectSingleNode(String.Format("/USERS/USER[USERNAME = '{0}' and @deleted ='0']", username)) != null;
            }
        }

        public EUserStoreStatus StoreUser(User user)
        {
            lock (usersMutex)
            {
                if (user.Role == "a") return EUserStoreStatus.FailedType;
                if (ContainsKey(user.Username)) return EUserStoreStatus.KeyViolation;

                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                var newUser = xmlDoc.CreateElement("USER");
                newUser.Attributes.Append(xmlDoc.CreateAttribute("deleted"));
                newUser.Attributes[0].Value = "0";
                newUser.Attributes.Append(xmlDoc.CreateAttribute("banned"));
                newUser.Attributes[0].Value = "0";
                var usernameNode = xmlDoc.CreateElement("USERNAME");
                var passwordNode = xmlDoc.CreateElement("PASSWORD");
                var nameNode = xmlDoc.CreateElement("NAME");
                var surnameNode = xmlDoc.CreateElement("SURNAME");
                var genderNode = xmlDoc.CreateElement("GENDER");
                var emailNode = xmlDoc.CreateElement("EMAIL");
                var birthdateNode = xmlDoc.CreateElement("BIRTHDATE");
                var roleNode = xmlDoc.CreateElement("ROLE");


                usernameNode.InnerText = user.Username;
                passwordNode.InnerText = user.Password;
                nameNode.InnerText = user.Name;
                surnameNode.InnerText = user.Surname;
                genderNode.InnerText = user.Gender;
                emailNode.InnerText = user.Email;
                birthdateNode.InnerText = user.BirthDate;
                roleNode.InnerText = user.Role;

                newUser.AppendChild(usernameNode);
                newUser.AppendChild(passwordNode);
                newUser.AppendChild(nameNode);
                newUser.AppendChild(surnameNode);
                newUser.AppendChild(genderNode);
                newUser.AppendChild(emailNode);
                newUser.AppendChild(birthdateNode);
                newUser.AppendChild(roleNode);

                if (user.Role!="a")
                {
                    var arrangementsNode = xmlDoc.CreateElement("ARRANGEMENTS");
                    switch (user.Role)
                    {
                        case "m":
                            {
                                foreach (var arrangement in (user as Manager).Arrangements)
                                {
                                    arrangement.AID = ArrangementsHandler.TakeKey();
                                    var arrangementNode = xmlDoc.CreateElement("AID");
                                    arrangementNode.InnerText = arrangement.AID.ToString();
                                    arrangementsNode.AppendChild(arrangementNode);
                                    arrangementsHandler.StoreArrangement(arrangement, null);
                                }
                                break;
                            }
                        case "t":
                            {
                                foreach (var reservation in (user as Tourist).Reservations)
                                {
                                    reservation.ResID = ReservationHandler.TakeKey();
                                    var reservationNode = xmlDoc.CreateElement("AID");
                                    reservationNode.InnerText = reservation.ResID.ToString();
                                    arrangementsNode.AppendChild(reservationNode);
                                    reservationHandler.StoreReservation(reservation);
                                }
                                break;
                            }

                    }
                    newUser.AppendChild(arrangementsNode);
                }
                xmlDoc.DocumentElement.AppendChild(newUser);
                xmlDoc.Save(xmlPath);
            }

            return EUserStoreStatus.Success;
        }

        //TODO
        public bool DeleteUser(string key)
        {
            if (key == "") return false;

            lock (usersMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                XmlNode targetUser = xmlDoc.SelectSingleNode(String.Format("/USERS/USER[USERNAME = '{0}' and @deleted = '0']", key));
                if (targetUser == null) return false;

                //TODO Cascade delete (Delete arrangements if no active arrangements left)

                targetUser.Attributes["deleted"].Value = "1";
                xmlDoc.Save(xmlPath);
                return true;
            }
        }

        public string ChechkUserStatus(User toCheckUser)
        {
            string errMess = "";
            string tmpStr = "";

            if (toCheckUser.Username == null || toCheckUser.Username == "") errMess += "Empty Username field; ";
            else
            {

                tmpStr = toCheckUser.Username.Trim();
                if (tmpStr.Length < 8) errMess += "Username at least 8 charchters long;";
                else if (tmpStr.Length > 20) errMess += "Username can be max. 20 charchters long;";

                if (tmpStr.Contains(' ')) errMess += "No spacing in username; ";

            }

            if (toCheckUser.Password == null || toCheckUser.Password == "") errMess += "Empty Password field; ";
            //else
            //{
            //    tmpStr = toCheckUser.Password;
            //    if (tmpStr.Length < 8) errMess += "Password at least 8 charchters long;";
            //    else if (tmpStr.Length > 20) errMess += "Password can be max. 20 charchters long;";
            //}

            string lettersOnlyRegex = "^([a-z] || [A-Z])";
            if (toCheckUser.Name == null || toCheckUser.Name == "") errMess += "Empty Name field; ";
            else if(Regex.Match(toCheckUser.Name, lettersOnlyRegex).Length!=0) errMess += "Only letter allowed in name; ";

            if (toCheckUser.Surname == null || toCheckUser.Surname == "") errMess += "Empty Surname field; ";
            else if (Regex.Match(toCheckUser.Surname, lettersOnlyRegex).Length != 0) errMess += "Only letter allowed in surname; ";

            if (toCheckUser.Gender == null || toCheckUser.Gender == "") errMess += "Empty Gender field; ";
            else if(toCheckUser.Gender!="m" && toCheckUser.Gender != "z") errMess += "Unknown gender; ";

            if (toCheckUser.Email == null || toCheckUser.Email == "") errMess += "Empty Email field; ";
            else if (Regex.Match(toCheckUser.Email, @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$").Length == 0) errMess += "Invalid email format; ";


            if (toCheckUser.BirthDate == null || toCheckUser.BirthDate == "") errMess += "Empty BirthDate field; ";
            else
            {
                DateTime dateTime;
                try
                {
                    dateTime = DateTime.ParseExact(toCheckUser.BirthDate, "dd/mm/yyyy", CultureInfo.InvariantCulture);
                    DateTime maxDate = DateTime.Now.AddYears(-18);
                    DateTime minDate = DateTime.Now.AddYears(-130);

                    if (DateTime.Compare(dateTime, maxDate) > 0) { errMess += "Age minimum is 18; "; }
                    if (DateTime.Compare(dateTime, minDate) < 0) { errMess += "Age maximum is 130; "; }

                }
                catch(System.FormatException)
                { errMess += "Bad date format; ";}

            }

            if (toCheckUser.Role == null || toCheckUser.Role == "") errMess += "Role unknown; ";

            return errMess;
        }

        public string ChechkUserStatus(UserUpdateReq req)
        {
            string outStr = this.ChechkUserStatus(new User(req.Username, req.Password, req.Name, req.Surname,
                req.Gender, req.Email, req.BirthDate, req.Role));
            if (req.PasswordNew1 != req.PasswordNew2) outStr += "New Password missmatch; ";
            return outStr;
        }

        public bool UpdateUser(UserUpdateReq user)
        {
            if (user == null || user.IsEmpty()) return false;
            RefreshData();

            lock (usersMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                XmlNode originalXML =  xmlDoc.SelectSingleNode(String.Format("/USERS/USER[USERNAME = '{0}' and @deleted ='0']", user.Username));
                if (originalXML == null) throw new Exception("Original not found exception");
                var original = ParseXMLNode(originalXML);
                if (original.Password != user.Password) throw new Exception("Bad password");
                
                if((user.Username != user.UsernameNew) && this.ContainsKey(user.UsernameNew))
                    throw new Exception("Username already used");

                if (user.PasswordNew1 != "")
                xmlDoc.SelectSingleNode(String.Format("/USERS/USER[USERNAME = '{0}' and @deleted ='0']/PASSWORD", user.Username)).InnerText = user.PasswordNew1;
                xmlDoc.SelectSingleNode(String.Format("/USERS/USER[USERNAME = '{0}' and @deleted ='0']/NAME", user.Username)).InnerText = user.Name;
                xmlDoc.SelectSingleNode(String.Format("/USERS/USER[USERNAME = '{0}' and @deleted ='0']/SURNAME", user.Username)).InnerText = user.Surname;
                xmlDoc.SelectSingleNode(String.Format("/USERS/USER[USERNAME = '{0}' and @deleted ='0']/GENDER", user.Username)).InnerText = user.Gender;
                xmlDoc.SelectSingleNode(String.Format("/USERS/USER[USERNAME = '{0}' and @deleted ='0']/EMAIL", user.Username)).InnerText= user.Email;
                xmlDoc.SelectSingleNode(String.Format("/USERS/USER[USERNAME = '{0}' and @deleted ='0']/BIRTHDATE", user.Username)).InnerText = user.BirthDate;
                xmlDoc.SelectSingleNode(String.Format("/USERS/USER[USERNAME = '{0}' and @deleted ='0']/USERNAME", user.Username)).InnerText = user.UsernameNew;

                xmlDoc.Save(xmlPath);

                if (original.Username != user.UsernameNew && user.Role == "t")
                {
                    reservationHandler.HandleUsernameUpdate(original.Username, user.UsernameNew);
                    commentsHandler.HandleUsernameUpdate(original.Username, user.UsernameNew);
                }

                switch(original.Role)
                { 
                    case "t":
                        {
                            registratedTourists.Remove(original.Username);
                            registratedTourists.Add(user.Username);
                            loggedTourists.Remove(original.Username);
                            loggedTourists.Add(user.Username);
                            break;
                        }
                    case "a":
                        {
                            registratedAdministrators.Remove(original.Username);
                            registratedAdministrators.Add(user.Username);
                            loggedAdministrators.Remove(original.Username);
                            loggedAdministrators.Add(user.Username);
                            break;
                        }
                    case "m":
                        {
                            registratedModerators.Remove(original.Username);
                            registratedModerators.Add(user.Username);
                            loggedModerators.Remove(original.Username);
                            loggedModerators.Add(user.Username);
                            break;
                        }
                }
            }
            return true;
        }

        public List<ulong> GetArrangementsForManager(string username)
        {
            lock(usersMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");
                var arrangementsList = xmlDoc.SelectNodes(String.Format("/USERS/USER[ROLE = 'm' and USERNAME = '{0}' and @deleted ='0']/ARRANGEMENTS/AID[@deleted = '0']", username));
                List<ulong> outList = new List<ulong>();
                foreach (XmlNode aID in arrangementsList)
                    outList.Add(ulong.Parse(aID.InnerText));
                return outList;
            }
        }

        public void AppendArrangementToManager(string aID, string username)
        {
            lock (usersMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");
                var targetUser = xmlDoc.SelectSingleNode(String.Format("/USERS/USER[ROLE = 'm' and USERNAME = '{0}' and @deleted ='0']/ARRANGEMENTS", username));
                XmlNode newArrangement = xmlDoc.CreateElement("AID");
                newArrangement.Attributes.Append(xmlDoc.CreateAttribute("deleted"));
                newArrangement.Attributes[0].Value = "0";
                newArrangement.InnerText = aID;
                targetUser.AppendChild(newArrangement);
                xmlDoc.Save(xmlPath);
            }
        }

        public bool IsCreatedBy(string arrangementName, string managerName)
        {
            lock (usersMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");
                var targetAID = arrangementsHandler.GetAIDByName(arrangementName);
                return xmlDoc.SelectNodes(String.Format("/USERS/USER[@deleted = '0' and USERNAME = '{0}' and ROLE = 'm' ]/ARRANGEMENTS/AID[@deleted ='0' and text() = '{1}' ] ",
                                                                                                                                managerName, targetAID)) != null ? true : false; 
                
            }
        }

        public void BanUser(string toBanUsername)
        {
            if (toBanUsername == "") return;

            lock (usersMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                XmlNode targetUser = xmlDoc.SelectSingleNode(String.Format("/USERS/USER[USERNAME = '{0}' and @deleted = '0']", toBanUsername));
                if (targetUser == null) return;
                targetUser.Attributes["banned"].Value = "1";
                xmlDoc.Save(xmlPath);

                if (registratedTourists.Contains(toBanUsername)) registratedTourists.Remove(toBanUsername);
                if (loggedTourists.Contains(toBanUsername)) loggedTourists.Remove(toBanUsername);
            }
        }

        public bool IsBanned(string username)
        {
            lock (usersMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                XmlNode targetUser = xmlDoc.SelectSingleNode(String.Format("/USERS/USER[USERNAME = '{0}' and @deleted = '0']", username));
                return targetUser.Attributes["banned"].Value == "1" ? true : false;
            }
        }
    }
}