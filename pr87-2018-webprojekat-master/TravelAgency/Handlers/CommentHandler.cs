using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using TravelAgency.Models;

namespace TravelAgency.Handlers
{
    public enum ECommentStatus
    {
        Success,
        TimeViolation,
        NotFound
    }

    public class CommentHandler
    {
        private static CommentHandler singletoneInstance = null;
        private static readonly string xmlPath = HttpContext.Current.Server.MapPath("~/App_Data/Files/Comments.xml");
        private static XmlDocument xmlDoc = new XmlDocument();
        private static object commentsMutex = new object();

        private static ArrangementsHandler arrangementsHandler = ArrangementsHandler.GetInstance();
        private static ReservationHandler reservationHandler = ReservationHandler.GetInstance();

        private CommentHandler(){ }

        public static CommentHandler GetInstance()
        {
            if (singletoneInstance == null) singletoneInstance = new CommentHandler();
            return singletoneInstance;
        }

        private Comment ParseXMLNode(XmlNode comment)
        {
            Comment newComment = new Comment();
            foreach (XmlNode commentParam in comment.ChildNodes)
            {
                if (commentParam.NodeType == XmlNodeType.Element)
                {
                    switch (commentParam.Name)
                    {
                        case "UID": { newComment.UID = commentParam.InnerText; break; }
                        case "AID": { newComment.AID = commentParam.InnerText; break; }
                        case "TXT": { newComment.Txt = commentParam.InnerText; break; }
                        case "RATING": { newComment.Rating = commentParam.InnerText; break; }
                    }
                }
            }
            return newComment;
        }

        private Comment ParseXMLNodeFull(XmlNode comment)
        {
            Comment newComment = new Comment();
            newComment.Status = comment.Attributes["status"].Value;
            foreach (XmlNode commentParam in comment.ChildNodes)
            {
                if (commentParam.NodeType == XmlNodeType.Element)
                {
                    switch (commentParam.Name)
                    {
                        case "UID": { newComment.UID = commentParam.InnerText; break; }
                        case "AID": { newComment.AID = commentParam.InnerText; break; }
                        case "TXT": { newComment.Txt = commentParam.InnerText; break; }
                        case "RATING": { newComment.Rating = commentParam.InnerText; break; }
                    }
                }
            }
            return newComment;
        }

        public List<Comment> ReadAllApproved(string arrangementID)
        {
            List<Comment> outList = new List<Comment>();
            lock(commentsMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                var comments = xmlDoc.SelectNodes(String.Format( "/COMMENTS/COMMENT[ @deleted = '0' and @status='ap' and AID = '{0}' ]", arrangementID));
                foreach (XmlNode comment in comments)
                {
                    if (comment.NodeType == XmlNodeType.Element && comment.Attributes["deleted"].Value == "0")
                        outList.Add(ParseXMLNode(comment));            
                }
            }
            return outList;
        }

        public List<Comment> ReadAll(string arrangementID)
        {
            List<Comment> outList = new List<Comment>();
            lock (commentsMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                var comments = xmlDoc.SelectNodes(String.Format("/COMMENTS/COMMENT[ @deleted = '0' and AID = '{0}' ]", arrangementID));
                foreach (XmlNode comment in comments)
                {
                    if (comment.NodeType == XmlNodeType.Element && comment.Attributes["deleted"].Value == "0")
                        outList.Add(ParseXMLNodeFull(comment));
                }
            }
            return outList;
        }

        public ECommentStatus Comment (Comment comment)
        {
            ECommentStatus tmpVar;
            if ((tmpVar = reservationHandler.IsReservedByMePassed(arrangementsHandler.GetAIDByName(comment.AID), comment.UID)) != ECommentStatus.Success)
                return tmpVar;

            lock (commentsMutex)
            {
                xmlDoc.Load(xmlPath);

                var newComment = xmlDoc.CreateElement("COMMENT");
                newComment.Attributes.Append(xmlDoc.CreateAttribute("deleted"));
                newComment.Attributes[0].Value = "0";
                newComment.Attributes.Append(xmlDoc.CreateAttribute("status"));
                newComment.Attributes[1].Value = comment.Status;

                var usernameNode = xmlDoc.CreateElement("UID");
                var arrangementNode = xmlDoc.CreateElement("AID");
                var txtNode = xmlDoc.CreateElement("TXT");
                var ratingNode = xmlDoc.CreateElement("RATING");

                usernameNode.InnerText = comment.UID;
                arrangementNode.InnerText = comment.AID;
                txtNode.InnerText = comment.Txt;
                ratingNode.InnerText = comment.Rating;

                newComment.AppendChild(usernameNode);
                newComment.AppendChild(arrangementNode);
                newComment.AppendChild(txtNode);
                newComment.AppendChild(ratingNode);

                xmlDoc.DocumentElement.AppendChild(newComment);
                xmlDoc.Save(xmlPath);
                return ECommentStatus.Success;
            }

        }

        private string ChangeCommentState(Comment comment, string state)
        {
            lock (commentsMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");

                var targetComment = xmlDoc.SelectSingleNode(String.Format("/COMMENTS/COMMENT[ @deleted = '0' and @status='{0}' and AID = '{1}' and UID = '{2}' and TXT = '{3}' and RATING = {4} ]",
                                                                                                                           comment.Status, comment.AID, comment.UID, comment.Txt, comment.Rating));
                if (targetComment == null) return "original not found";
                targetComment.Attributes["status"].Value = state;
                xmlDoc.Save(xmlPath);
                return "";
            }
        }

        public string ApproveComment(Comment comment)
        {
            return ChangeCommentState(comment, "ap");
        }

        public string DeclineComment(Comment comment)
        {
            return ChangeCommentState(comment, "dc");
        }

        public void HandleUsernameUpdate(string oldUsername, string newUsername)
        {
            lock (commentsMutex)
            {
                xmlDoc.Load(xmlPath);
                if (xmlDoc == null) throw new Exception("Source file not found");
                var comments = xmlDoc.SelectNodes(String.Format("/COMMENTS/COMMENT[ @deleted = '0' and UID = '{0}' ]/UID", oldUsername));
                foreach (XmlNode comment in comments) comment.InnerText = newUsername;
                xmlDoc.Save(xmlPath);
            }
        }
    }
}