using CmsData;
using CmsData.Codes;
using CMSWebTests;
using IntegrationTests.Support;
using SharedTestFixtures;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace IntegrationTests.Areas.Main.Views.Email
{
    [Collection(Collections.Webapp)]
    public class SelectTemplateTests : AccountTestBase
    {
        private int OrgId { get; set; }
        private int SpecialContentId { get; set; }

        [Fact]
        public void Empty_Email_Template_Should_Be_Disabled()
        {
            MaximizeWindow();

            CMSDataContext db = CMSDataContext.Create(DatabaseFixture.Host);
            var requestManager = FakeRequestManager.Create();
            var controller = new CmsWeb.Areas.OnlineReg.Controllers.OnlineRegController(requestManager);
            var routeDataValues = new Dictionary<string, string> { { "controller", "OnlineReg" } };
            controller.ControllerContext = ControllerTestUtils.FakeControllerContext(controller, routeDataValues);

            var FakeOrg = FakeOrganizationUtils.MakeFakeOrganization(requestManager, new CmsData.Organization()
            {
                OrganizationName = "MockName",
                RegistrationTitle = "MockTitle",
                Location = "MockLocation",
                RegistrationTypeId = RegistrationTypeCode.JoinOrganization
            });

            OrgId = FakeOrg.org.OrganizationId;

            var htmlEmptyTemplate = SpecialContentUtils.CreateSpecialContent(2, "Empty Template", null);
            SpecialContentId = htmlEmptyTemplate.Id;
            SpecialContentUtils.UpdateSpecialContent(SpecialContentId, htmlEmptyTemplate.Name, htmlEmptyTemplate.Name, "<html>  <head>   <title></title>   <script src='chrome - extension://mooikfkahbdckldjjndioackbalphokd/assets/prompt.js'></script>  </head>  <body>&nbsp;</body>  </html>", null, null, null);

            username = RandomString();
            password = RandomString();
            var user = CreateUser(username, password, roles: new string[] { "Edit", "Access", "Admin" });
            Login();

            Open($"{rootUrl}Org/{OrgId}#tab-Registrations-tab");

            WaitForElementToDisappear(loadingUI);

            Find(css: "#bluebar-menu > div:nth-child(1) > button").Click();
            Find(css: "#bluebar-menu > div.btn-group.btn-group-lg.dropdown-large.open > ul > li > ul > li:nth-child(2) > a").Click();

            string currentURL = driver.Url;

            Find(css: "#emailTemplates > div > div > div > div:nth-child(1) > div > div.col-xs-8 > a").Click();

            // URL After click "Empty Template" doesn't have to change
            currentURL.ShouldBe(driver.Url);
        }

        public override void Dispose()
        {
            FakeOrganizationUtils.DeleteOrg(OrgId);
            SpecialContentUtils.DeleteSpecialContent(SpecialContentId);
        }
    }
}
