using CmsData;
using CmsData.Codes;
using SharedTestFixtures;
using Shouldly;
using System;
using System.Linq;
using UtilityExtensions;
using Xunit;

namespace CmsDataTests
{
    [Collection(Collections.Database)]
    public class CMSDataContextFunctionTests : FinanceTestBase
    {
        [Fact]
        public void GetTotalContributionsDonorTest()
        {
            var fromDate = new DateTime(2019, 1, 1);
            var toDate = new DateTime(2019, 7, 31);
            using (var db = CMSDataContext.Create(DatabaseFixture.Host))
            {
                var TotalAmmountContributions = db.Contributions
                    .Where(x => x.ContributionTypeId == ContributionTypeCode.CheckCash)
                    .Where(x => x.ContributionDate >= fromDate)
                    .Where(x => x.ContributionDate < toDate.AddDays(1))
                    .Sum(x => x.ContributionAmount) ?? 0;
                var TotalPledgeAmountContributions = db.Contributions
                    .Where(x => x.ContributionTypeId == ContributionTypeCode.Pledge)
                    .Where(x => x.ContributionDate >= fromDate)
                    .Where(x => x.ContributionDate < toDate.AddDays(1))
                    .Sum(x => x.ContributionAmount) ?? 0;

                var bundleHeader = MockContributions.CreateSaveBundle(db);
                var FirstContribution = MockContributions.CreateSaveContribution(db, bundleHeader, fromDate, 120, peopleId: 1);
                var SecondContribution = MockContributions.CreateSaveContribution(db, bundleHeader, fromDate, 500, peopleId: 1, contributionType: ContributionTypeCode.Pledge);

                var results = db.GetTotalContributionsDonor(fromDate, toDate, null, null, true, null, null, true).ToList();
                var actualContributionsAmount = results.Where(x => x.ContributionTypeId == ContributionTypeCode.CheckCash).Sum(x => x.Amount);
                var actualPledgesAmount = results.Where(x => x.ContributionTypeId == ContributionTypeCode.Pledge).Sum(x => x.PledgeAmount);

                actualContributionsAmount.ShouldBe(TotalAmmountContributions + 120);
                actualPledgesAmount.ShouldBe(TotalPledgeAmountContributions + 500);

                MockContributions.DeleteAllFromBundle(db, bundleHeader);
            }
        }

        [Fact]
        public void PledgesSummaryTest()
        {
            var fromDate = new DateTime(2019, 1, 1);
            using (var db = CMSDataContext.Create(DatabaseFixture.Host))
            {
                var bundleHeader = MockContributions.CreateSaveBundle(db);
                var FirstContribution = MockContributions.CreateSaveContribution(db, bundleHeader, fromDate, 100, peopleId: 1);
                var SecondContribution = MockContributions.CreateSaveContribution(db, bundleHeader, fromDate, 20, peopleId: 1);
                var Pledges = MockContributions.CreateSaveContribution(db, bundleHeader, fromDate, 500, peopleId: 1, contributionType: ContributionTypeCode.Pledge);

                //Get amount contributed to the pledge
                var TotalAmmountContributions = db.Contributions
                    .Where(x => x.FundId == 1)
                    .Where(x => x.PeopleId == 1)
                    .Where(x => x.ContributionTypeId != ContributionTypeCode.Pledge)
                    .Sum(x => x.ContributionAmount) ?? 0;

                //Get Pledge amount
                var TotalPledgeAmount = db.Contributions
                    .Where(x => x.ContributionTypeId == ContributionTypeCode.Pledge && x.PeopleId == 1 && x.FundId == 1)
                    .Sum(x => x.ContributionAmount) ?? 0;

                var results = db.PledgesSummary(1);
                var actual = results.ToList().First();

                actual.AmountContributed.ShouldBe(TotalAmmountContributions);
                actual.AmountPledged.ShouldBe(TotalPledgeAmount);
                actual.Balance.ShouldBe((TotalPledgeAmount) - (TotalAmmountContributions) < 0 ? 0 : (TotalPledgeAmount) - (TotalAmmountContributions));

                MockContributions.DeleteAllFromBundle(db, bundleHeader);      
            }
        }

        [Theory]
        [InlineData("", null,null,null,null,null,null,null, null, null)]
        [InlineData("", null, null, -8, null, null, null, null, null, null)]
        [InlineData("", null, null, -7, null, null, null, null, null, null)]
        public void OrgSearchTest(string name, int? prog, int? div, int? type, int? campus, int? sched, int? status, int? onlinereg, int? userId, int? targetDiv)
        {
            /* All TypeIds
             *      NULL => ALL orgs
             *      -8 => Orgs without fees
             *      -7 => Orgs with fees      
             *      -1 => Orgs without type
             *      -6 => Child org
             *      -5 => Parent Org
             *      -2 => Not main fellowship
             *      -3 => Main fellowship
             *      -4 => Suspended Checkin
             *    
             */


            using (var db = CMSDataContext.Create(DatabaseFixture.Host))
            {                
                var funcToTest = db.OrgSearch(name, prog, div, type, campus, sched, status, onlinereg, userId, targetDiv);
                var filterRoles = from o1 in db.Organizations
                                  join r in db.Roles on o1.LimitToRole equals r.RoleName
                                  join ur in db.UserRoles on r.RoleId equals ur.RoleId
                                  join u in db.Users on ur.UserId equals u.UserId
                                  where userId == u.UserId || userId == null
                                  select new { oid = o1.OrganizationId};

                var filterLeaders1 = from o2 in db.Organizations
                                     join m in db.OrganizationMembers on o2.OrganizationId equals m.OrganizationId                                    
                                     join u in db.Users on new { m.PeopleId, userId = userId.ToInt() } equals new { PeopleId = u.PeopleId.ToInt(), userId = u.UserId }
                                     join mt in db.MemberTypes on m.MemberTypeId equals mt.Id                                     
                                     select new { oid = o2.OrganizationId, o2.ParentOrgId };

                var filterLeaders2 = from o3 in db.Organizations
                                     where filterLeaders1.Select(x=>x.oid).ToList().Contains(o3.ParentOrgId.ToInt())
                                     select new { oid = o3.OrganizationId, o3.ParentOrgId };

                var filterLeaders3 = from o4 in db.Organizations
                                     where filterLeaders2.Select(x => x.oid).ToList().Contains(o4.ParentOrgId.ToInt())
                                     select new { oid = o4.OrganizationId, o4.ParentOrgId };

                var filterLeaders = (from o5 in filterLeaders1 select new { o5.oid, o5.ParentOrgId })
                                    .Union(from o5 in filterLeaders2 select new { o5.oid, o5.ParentOrgId })
                                    .Union(from o5 in filterLeaders3 select new { o5.oid, o5.ParentOrgId });

                var filterProg = from o6 in db.Organizations
                                 where (from d in db.DivOrgs
                                        join p in db.ProgDivs on d.DivId equals p.DivId
                                        where d.OrgId == o6.OrganizationId && p.ProgId == prog
                                        select d
                                        ).Any(x => x != null)
                                 select new { oid = o6.OrganizationId};
                //AS(
                //       SELECT o.OrganizationId oid
                //       FROM dbo.Organizations o
                //       WHERE EXISTS(
                //           SELECT NULL
                //           FROM dbo.DivOrg di
                //           JOIN dbo.ProgDiv pp ON pp.DivId = di.DivId
                //           WHERE di.OrgId = o.OrganizationId
                //           AND pp.ProgId = @prog
                //       )
                //   ),

                var filterDiv = from o7 in db.Organizations
                                where (from d in db.DivOrgs                                       
                                       where d.OrgId == o7.OrganizationId && d.DivId == div
                                       select d
                                        ).Any(x => x != null)
                                select new { oid = o7.OrganizationId };
                //filterDiv AS(
                //       SELECT o.OrganizationId oid
                //       FROM dbo.Organizations o
                //       WHERE EXISTS(
                //           SELECT NULL
                //           FROM dbo.DivOrg dd
                //           WHERE dd.OrgId = o.OrganizationId AND dd.DivId = @div
                //       )
                //),
                var filterSched = from o8 in db.Organizations
                                  where (sched == -1 &&
                                      !(from os in db.OrgSchedules 
                                       where os.OrganizationId == o8.OrganizationId
                                       select os
                                        ).Any(x => x != null)
                                  ) || (from os in db.OrgSchedules
                                        where os.OrganizationId == o8.OrganizationId && os.ScheduleId == sched
                                        select os
                                        ).Any(x => x != null)
                                  select new { oid = o8.OrganizationId };
                //filterSched AS(
                //       SELECT o.OrganizationId oid
                //       FROM dbo.Organizations o
                //       WHERE (@sched = -1
                //           AND NOT EXISTS(
                //               SELECT NULL
                //               FROM dbo.OrgSchedule
                //               WHERE OrganizationId = o.OrganizationId
                //           )
                //	)
                //	OR EXISTS(
                //           SELECT NULL
                //           FROM dbo.OrgSchedule
                //           WHERE OrganizationId = o.OrganizationId
                //           AND ScheduleId = @sched
                //       )
                //),

                /*
                    Registration issues 2 tickets


                 */
                var filterType = from o9 in db.Organizations
                                 where (type > 0 && o9.OrganizationTypeId == type)
                                 || (type.Equals(-1) && o9.OrganizationTypeId.Equals(null))
                                 || (type.Equals(-2) && o9.IsBibleFellowshipOrg.Equals(0))
                                 || (type.Equals(-3) && o9.IsBibleFellowshipOrg.Equals(1))
                                 || (type.Equals(-4) && o9.SuspendCheckin.Equals(1))
                                 || (type.Equals(-5) && (from o2 in db.Organizations
                                                         where o2.ParentOrgId == o9.OrganizationId
                                                         select o2).Any(x => x != null))
                                 || (type.Equals(-6) && o9.ParentOrgId > 0)
                                 || (type.Equals(-7) && (from f in db.ViewOrgsWithFees 
                                                         where f.OrganizationId == o9.OrganizationId
                                                         select f).Any(x => x != null))
                                 || (type.Equals(-8) && (from f in db.ViewOrgsWithoutFees
                                                         where f.OrganizationId == o9.OrganizationId
                                                         select f).Any(x => x != null))
                                 select new { oid = o9.OrganizationId };

                var schedules = from o10 in db.OrgSchedules
                                where (sched.Equals(0) && o10.Id == 1) || o10.ScheduleId == sched
                                select new { OrganizationId = o10.OrganizationId, o10.ScheduleId, o10.SchedTime, ScheduleDescription = db.GetScheduleDesc(o10.MeetingTime) };

                //schedules AS(
                //       SELECT
                //           OrganizationId,
                //           ScheduleId,
                //           SchedTime,
                //           dbo.GetScheduleDesc(MeetingTime) ScheduleDescription
                //       FROM dbo.OrgSchedule
                //       WHERE ((ISNULL(@sched, 0) = 0 AND Id = 1) OR ScheduleId = @sched)
                //),

                var divisions = from o11 in db.Organizations
                                select new { o11.OrganizationId, Divisions = $@"{o11.DivisionName} ({o11.OrganizationId})" };
                //divisions AS(
                //       SELECT
                //           OrganizationId,
                //           Divisions = (SELECT d.Name + ' (' + CONVERT(VARCHAR(10), d.Id) + ')'
                //          FROM dbo.DivOrg dd
                //                 JOIN dbo.Division d ON d.Id = dd.DivId
                //                 WHERE dd.OrgId = o.OrganizationId),
                //(
                //	FROM dbo.Organizations o
                //),

                var filterName = from n in db.FilterOrgSearchName(name) select new { oid = n.Oid };

                //filterName AS(
                //       SELECT oid
                //       FROM dbo.FilterOrgSearchName(@name)
                //   ),

                var filterReg = from n in db.FilterOnlineReg(onlinereg) select new { oid = n.Oid };
                //filterReg AS(
                //       SELECT oid
                //       FROM dbo.FilterOnlineReg(@onlinereg)
                //   )



                /*
                 * FROM dbo.Organizations o
                JOIN divisions ds ON ds.OrganizationId = o.OrganizationId
                LEFT JOIN lookup.MemberType mmt ON mmt.Id = o.LeaderMemberTypeId
                LEFT JOIN dbo.Division d ON d.Id = o.DivisionId
                LEFT JOIN dbo.Program p ON p.Id = d.ProgId
                LEFT JOIN schedules sc ON sc.OrganizationId = o.OrganizationId
                LEFT JOIN lookup.Campus c ON c.Id = o.CampusId
                 */
                var comparator = from o in db.Organizations
                                 join ds in divisions on o.OrganizationId equals ds.OrganizationId
                                 join mmt in db.MemberTypes on o.LeaderMemberTypeId equals mmt.Id into mmt1
                                 from mmt2 in mmt1.DefaultIfEmpty()
                                 join d in db.Divisions on o.DivisionId equals d.Id into d1
                                 from d3 in d1.DefaultIfEmpty()
                                 join p in db.Programs on d3.ProgId equals p.Id into p1
                                 from p2 in p1.DefaultIfEmpty()
                                 join sc in schedules on o.OrganizationId equals sc.OrganizationId into sc1
                                 from sc2 in sc1.DefaultIfEmpty()
                                 join c in db.Campus on o.CampusId equals c.Id
                                 where (o.LimitToRole == null || filterRoles.Select(x=>x.oid).Contains(o.OrganizationId))
                                 && (name ?? "" ) == "" || filterName.Select(x => x.oid).Contains( o.OrganizationId)
                                 && (status ?? 0) == 0 || o.OrganizationStatusId == status
                                 && (campus ?? 0) == 0 || o.CampusId == campus
                                 && (type ?? 0) == 0 || filterType.Select(x => x.oid).Contains(o.OrganizationId)
                                 && (prog ?? 0) == 0 || filterProg.Select(x => x.oid).Contains(o.OrganizationId)
                                 && (div ?? 0) == 0 || filterDiv.Select(x => x.oid).Contains(o.OrganizationId)
                                 && (sched ?? 0) == 0 || filterSched.Select(x => x.oid).Contains(o.OrganizationId)
                                 && (onlinereg ?? -1) == -1 || filterReg.Select(x => x.oid).Contains(o.OrganizationId)
                                 && ((from r in db.Roles
                                     join ur in db.UserRoles on r.RoleId equals ur.RoleId
                                     where ur.UserId == userId && r.RoleName == "OrgLeadersOnly"
                                     select r).Any(x => x != null).IsNotNull() ? 1 : 0) == 0 || filterLeaders.Select(x => x.oid).Contains(o.OrganizationId)
                                 select new
                                 {
                                     o.OrganizationId,
                                     o.OrganizationName,
                                     o.OrganizationStatusId,
                                     Program = p2.Name,
                                     ProgramId = p2.Id
                                ,
                                     Division = ds.Divisions,
                                     ds.Divisions,
                                     sc2.ScheduleDescription,
                                     sc2.ScheduleId,
                                     sc2.SchedTime
                                ,
                                     Campus = c.Description,
                                     LeaderType = mmt2.Description,
                                     o.LeaderName,
                                     o.Location
                                ,
                                     o.ClassFilled,
                                     o.RegistrationClosed,
                                     o.AppCategory,
                                     o.RegStart,
                                     o.RegEnd
                                ,
                                     o.PublicSortOrder,
                                     o.FirstMeetingDate,
                                     o.LastMeetingDate,
                                     o.MemberCount
                                ,
                                     o.RegistrationTypeId,
                                     o.CanSelfCheckin,
                                     o.LeaderId,
                                     o.PrevMemberCount,
                                     o.ProspectCount,
                                     o.Description
                                ,
                                     o.UseRegisterLink2,
                                     o.DivisionId,
                                     o.BirthDayStart,
                                     o.BirthDayEnd
                                ,
                                     Tag = (targetDiv ?? 0) == 0 ? "" : (from dd in db.DivOrgs
                                                                         where o.OrganizationId == dd.OrgId && dd.DivId == targetDiv
                                                                         select dd).Any(x => x != null).IsNotNull() ? "Remove" : "Add",
                                     ChangeMain = (o.DivisionId == null || o.DivisionId != targetDiv) && (from dd in db.DivOrgs
                                                                                                          where o.OrganizationId == dd.OrgId && dd.DivId == targetDiv
                                                                                                          select dd).Any(x => x != null).IsNotNull()
                                                                                                          ? 1 : 0
                                 };
                funcToTest.Count().ShouldBe(comparator.Count());

                //MockContributions.DeleteAllFromBundle(db, bundleHeader);
            }
        }
    }
}
