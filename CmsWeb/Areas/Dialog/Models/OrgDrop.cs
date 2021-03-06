using CmsData;
using CmsData.Codes;
using ImageData;
using CmsWeb.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web.Hosting;
using UtilityExtensions;

namespace CmsWeb.Areas.Dialog.Models
{
    public class OrgDrop : LongRunningOperation, IDbBinder
    {
        public const string Op = "orgdrop";

        public string DisplayGroup
        {
            get
            {
                switch (Filter?.GroupSelect)
                {
                    case GroupSelectCode.Member:
                        return "Members";
                    case GroupSelectCode.Inactive:
                        return "Inactive";
                    case GroupSelectCode.Pending:
                        return "Pending";
                    case GroupSelectCode.Prospect:
                        return "Prospects";
                    default:
                        return Filter?.GroupSelect;
                }
            }
        }

        public DateTime? DropDate { get; set; }
        public bool RemoveFromEnrollmentHistory { get; set; }
        public int UserId { get; set; }
        public CMSDataContext CurrentDatabase { get; set; }

        public OrgDrop()
        {
        }

        public OrgDrop(CMSDataContext db)
        {
            Host = db.Host;
            UserId = Util.UserId;
            CurrentDatabase = db;
        }
        public OrgDrop(CMSDataContext db, Guid id)
            : this(db)
        {
            QueryId = id;
        }

        private OrgFilter filter;
        public OrgFilter Filter => filter ?? (filter = CurrentDatabase?.OrgFilter(QueryId));
        public int OrgId => Filter?.Id ?? 0;

        public int DisplayCount => Count ?? (Count = CurrentDatabase?.OrgFilterIds(QueryId).Count()) ?? 0;

        private string orgname;
        public string OrgName => orgname ?? (orgname = CurrentDatabase?.Organizations.Where(vv => vv.OrganizationId == Filter.Id).Select(vv => vv.OrganizationName).Single());

        private List<int> pids;
        private List<int> Pids => pids ?? (pids = CurrentDatabase?.OrgFilterIds(QueryId).Select(p => p.PeopleId.Value).ToList());

        public void Process(CMSDataContext db)
        {
            // running has not started yet, start it on a separate thread
            Started = DateTime.Now;
            var lop = new LongRunningOperation()
            {
                QueryId = QueryId,
                Started = Started,
                Count = Pids.Count,
                Processed = 0,
                Operation = Op,
            };
            db.LongRunningOperations.InsertOnSubmit(lop);
            db.SubmitChanges();
            db.LogActivity($"OrgDrop {lop.Count} records", Filter.Id, uid: UserId);
            HostingEnvironment.QueueBackgroundWorkItem(ct => DoWork(this));
        }

        private static void DoWork(OrgDrop model)
        {
            var db = CMSDataContext.Create(model.Host);
            var idb = CMSImageDataContext.Create(model.Host);
            var cul = db.Setting("Culture", "en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(cul);
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(cul);

            LongRunningOperation lop = null;
            foreach (var pid in model.Pids)
            {
                var om = db.OrganizationMembers.Single(mm => mm.PeopleId == pid && mm.OrganizationId == model.filter.Id);
                if (model.DropDate.HasValue)
                {
                    om.Drop(db, idb, model.DropDate.Value);
                }
                else
                {
                    om.Drop(db, idb);
                }

                db.SubmitChanges();
                if (model.RemoveFromEnrollmentHistory)
                {
                    db.ExecuteCommand("DELETE dbo.EnrollmentTransaction WHERE PeopleId = {0} AND OrganizationId = {1}", pid, model.filter.Id);
                }

                lop = FetchLongRunningOperation(db, Op, model.QueryId);
                Debug.Assert(lop != null, "r != null");
                lop.Processed++;
                db.SubmitChanges();
                db.LogActivity($"Org{model.DisplayGroup} Drop{(model.RemoveFromEnrollmentHistory ? " w/history" : "")}", model.filter.Id, pid, uid: model.UserId);
            }
            // finished
            lop = FetchLongRunningOperation(db, Op, model.QueryId);
            lop.Completed = DateTime.Now;
            db.SubmitChanges();
        }

        public void DropSingleMember(int orgId, int peopleId)
        {
            var org = CurrentDatabase.LoadOrganizationById(orgId);
            var om = org.OrganizationMembers.Single(mm => mm.PeopleId == peopleId);
            om.Drop(CurrentDatabase, CMSImageDataContext.Create(CurrentDatabase.Host));
            CurrentDatabase.SubmitChanges();
        }
    }
}
