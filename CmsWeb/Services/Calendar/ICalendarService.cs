using CmsWeb.Services.Calendar.Models;
using System;
using System.Collections.Generic;

namespace CmsWeb.Services.Calendar
{
    public interface ICalendarService
    {
        bool FeatureEnabled { get; }

        IEnumerable<CalendarItem> Get(DateTime startDate, DateTime endDate);
    }
}
