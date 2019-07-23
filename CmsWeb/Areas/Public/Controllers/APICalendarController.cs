using CmsWeb.Lifecycle;
using CmsWeb.Services.Calendar;
using System;
using System.Web.Mvc;

namespace CmsWeb.Areas.Public.Controllers
{
    public class APICalendarController : CmsController
    {
        private readonly ICalendarService _calendarService;

        public APICalendarController(IRequestManager requestManager, ICalendarService calendarService) : base(requestManager)
        {
            _calendarService = calendarService;
        }

        private string Authenticate()
        {
            var ret = AuthenticateDeveloper();
            if (ret.StartsWith("!"))
            {
                return ret.Substring(1);
            }

            return "";
        }

        private bool Validate()
        {
            return _calendarService.FeatureEnabled;
        }

        [HttpGet]
        public JsonResult RecentEvents()
        {
            return FetchEvents(DateTime.Now.AddDays(-60).Date, DateTime.Now.Date);
        }

        [HttpGet]
        public JsonResult UpcomingEvents()
        {
            return FetchEvents(DateTime.Now.Date, DateTime.Now.AddDays(60).Date);
        }

        [HttpGet]
        public JsonResult Index()
        {
            return FetchEvents(DateTime.Now.Date.AddDays(-1), DateTime.Now.Date.AddYears(1));
        }

        private JsonResult FetchEvents(DateTime startDate, DateTime endDate)
        {
            Authenticate();
            Validate();

            var results = _calendarService.Get(startDate, endDate);

            return Json(results, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}
