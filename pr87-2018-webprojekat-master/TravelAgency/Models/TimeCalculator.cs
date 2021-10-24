using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravelAgency.Models
{
    public enum EProgressStatus
    {
        PASSED,
        ONGOING,
        INCOMING,
        INVALID
    }

    public class TimeCalculator
    {
        public static int CheckTimeRelation(string pointOfView, string referenceDay)
        {
            string[] stringParts = pointOfView.Split(new char[] { '/' });
            int date = Int32.Parse(stringParts[0]);
            int month = Int32.Parse(stringParts[1]);
            int year = Int32.Parse(stringParts[2]);

            stringParts = referenceDay.Split(new char[] { '/' });
            int dateNow = Int32.Parse(stringParts[0]);
            int monthNow = Int32.Parse(stringParts[1]);
            int yearNow = Int32.Parse(stringParts[2]);

            if (year == yearNow)
            {
                if (month == monthNow)
                {
                    if (date == dateNow) return 0;
                    else if (date < dateNow) return -1;
                    else return 1;//date > dateNow
                }
                else if (month < monthNow) return -1;
                else return 1; //month > monthNow
            }
            else if (year < yearNow) return -1;
            else return 1; // year>yearNow
        }

        public static int CheckTimeRelationMine(string dateString)
        {
            string[] stringParts = dateString.Split(new char[] { '/' });
            int date = Int32.Parse(stringParts[0]);
            int month = Int32.Parse(stringParts[1]);
            int year = Int32.Parse(stringParts[2]);

            int dateNow = DateTime.Now.Day;
            int monthNow = DateTime.Now.Month;
            int yearNow = DateTime.Now.Year;

            if (year == yearNow)
            {
                if (month == monthNow)
                {
                    if (date == dateNow) return 0;
                    else if (date < dateNow) return -1;
                    else return 1;//date > dateNow
                }
                else if (month < monthNow) return -1;
                else return 1; //month > monthNow
            }
            else if (year < yearNow) return -1;
            else return 1; // year>yearNow
        }

        public static EProgressStatus CheckProgressStatus(string starts, string stops)
        {
            var startRel = CheckTimeRelationMine(starts);
            var stopRel = CheckTimeRelationMine(stops);

            if (startRel < 0 && stopRel < 0) return EProgressStatus.PASSED;
            if (startRel > 0 && stopRel > 0) return EProgressStatus.INCOMING;
            if (startRel <= 0 && stopRel >= 0) return EProgressStatus.ONGOING;
            return EProgressStatus.INVALID;
        }
    }
}