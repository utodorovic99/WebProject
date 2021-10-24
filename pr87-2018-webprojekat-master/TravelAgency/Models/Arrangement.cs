using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravelAgency.Models
{
    public class Arrangement
    {
        public ulong AID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Transport { get; set; }
        public string Locationn { get; set; }
        public string DateStart { get; set; }
        public string DateStop { get; set; }
        public Location MeetingSpot { get; set; }
        public string MeetingTime { get; set; }
        public uint MaxPassengers { get; set; }
        public string Desc { get; set; }
        public string Program { get; set; }
        public string PosterURL { get; set; }
        public List<ulong> Accommodations { get; set; }

        public Arrangement()
        {
            Accommodations = new List<ulong>();
            MeetingSpot = new Location();
        }
        public Arrangement(ulong aID, string name, string type, string transport, string location, string dateStart, string dateStop, Location meetingSpot, string meetingTime, uint maxPassengers, string desc, string program, string poseterURL, List<ulong> accommodations)
        {
            AID = aID;
            Name = name;
            Type = type;
            Transport = transport;
            Locationn = location;
            DateStart = dateStart;
            DateStop = dateStop;
            MeetingSpot = meetingSpot;
            MeetingTime = meetingTime;
            MaxPassengers = maxPassengers;
            Desc = desc;
            Program = program;
            PosterURL = poseterURL;
            Accommodations = accommodations;
            MeetingSpot = meetingSpot;
        }

        public override string ToString()
        {
            string acommodations = "";
            foreach (var acommodation in Accommodations) acommodations += acommodation + "\n";
            acommodations += "\n";

            return String.Format("AID: {0}\nName: {1}\nType: {2}\nTransport: {3}\nLocation: {4}\n\nDateStart: {5}\nDateStop: {6}\n",
                                  AID, Name, this.Type, Transport, Locationn, DateStart, DateStop) +
             String.Format("MeetingSpot: {0}\nMeetingTime: {1}\nMaxPassengers: {2}\nDesc: {3}\nProgram: {4}\nPosterURL: {5}\nAcommodations:\n{6}",
                           MeetingSpot.ToString(), MeetingTime, MaxPassengers, Desc, Program, PosterURL, acommodations);
        }

    }
}