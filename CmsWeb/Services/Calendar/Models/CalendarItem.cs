using System;

namespace CmsWeb.Services.Calendar.Models
{
    public class CalendarItem
    {
        public int OrganizationId { get; set; }
        public DateTime MeetingDate { get; set; }
        public string Location { get; set; }
        public string OrganizationName { get; set; }
        public string Description { get; set; }
        public string LeaderName { get; set; }
        public int ScheduleId { get; set; }
        public int MeetingId { get; set; }
        public int CampusId { get; set; }
    }
}
