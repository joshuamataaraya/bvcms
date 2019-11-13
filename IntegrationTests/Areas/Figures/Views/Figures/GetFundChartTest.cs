using CmsData;
using CmsWeb.Charts;
using CmsWeb.Models;
using IntegrationTests.Support;
using SharedTestFixtures;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using UtilityExtensions;
using Xunit;

namespace IntegrationTests.Areas.Figures.Views.Figures
{
    [Collection(Collections.Webapp)]
    public class GetFundChartTest : AccountTestBase
    {
        [Theory]
        [ClassData(typeof(GetFundChartTestData))]
        public void GetFundChartData_Should_Match_TotalsByFund(int[] fundIds, int? year)
        {
            username = RandomString();
            password = RandomString();
            var user = CreateUser(username, password, roles: new string[] { "Access", "Edit", "Admin" });

            using (var db = CMSDataContext.Create(DatabaseFixture.Host))
            {
                db.CurrentUser = user;
                var data = new GoogleChartsData(db);
                var result = data.GetFundChartData(fundIds, year);
                TotalsByFundModel m = new TotalsByFundModel(db);
                for (int i=1; i<=12; i++)
                {
                    var firstDayOfMonth = new DateTime((int)year, i, 1);
                    var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                    m.Dt1 = firstDayOfMonth;
                    m.Dt2 = lastDayOfMonth;
                    m.FundSet = string.Join(",", fundIds);
                    foreach (var t in m.TotalsByFund())
                    {
                        result = data.GetFundChartData(t.FundId.ToString().Select(o => Convert.ToInt32(o)).ToArray(), firstDayOfMonth.Year);
                    }
                }
            }            
        }
    }
}
