using CmsWeb.Lifecycle;
using CmsWeb.Services.Calendar;
using System.Web.Mvc;

namespace CmsWeb.Areas.Setup.Controllers
{
    [RouteArea("Setup", AreaPrefix = "Calendar")]
    public class CalendarController : CMSBaseController
    {
        private readonly ICalendarService _calendarService;

        public CalendarController(IRequestManager requestManager, ICalendarService calendarService)
            : base(requestManager)
        {
            _calendarService = calendarService;
        }

        // GET: Setup/Calendar
        public ActionResult Index()
        {
            return View();
        }
    }
}
