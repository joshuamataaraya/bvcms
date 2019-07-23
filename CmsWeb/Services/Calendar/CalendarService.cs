using CmsWeb.Lifecycle;
using CmsWeb.Services.Calendar.Models;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;

namespace CmsWeb.Services.Calendar
{
    public class CalendarService : ICalendarService
    {
        private readonly IRequestManager _requestManager;

        public CalendarService(IRequestManager requestManager)
        {
            _requestManager = requestManager;
        }

        public bool FeatureEnabled => true;

        public IEnumerable<CalendarItem> Get(DateTime startDate, DateTime endDate)
        {
            return _requestManager.CurrentDatabase.Connection.Query<CalendarItem>("API_Calendar_FetchItems", new { startDate, endDate }, commandType: CommandType.StoredProcedure);
        }
    }
}
