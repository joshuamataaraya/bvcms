﻿using CmsData;
using CmsData.Codes;
using System;

namespace SharedTestFixtures
{
    public class FinanceTestBase
    {
        protected BundleHeader CreateBundle(CMSDataContext db, DateTime? contributionDate = null)
        {
            var bundleHeader = new BundleHeader
            {
                BundleHeaderTypeId = BundleTypeCode.ChecksAndCash,
                BundleStatusId = BundleStatusCode.Open,
                ContributionDate = contributionDate ?? DateTime.Now,
                CreatedDate = contributionDate ?? DateTime.Now,
                CreatedBy = 1,
            };
            db.BundleHeaders.InsertOnSubmit(bundleHeader);
            db.SubmitChanges();

            return bundleHeader;
        }

        protected Contribution CreateContribution(CMSDataContext db, BundleHeader bundleHeader,
            DateTime date,
            decimal amount,
            int? peopleId = null,
            int contributionType = ContributionTypeCode.CheckCash,
            int fundId = 1,
            int statusId = ContributionStatusCode.Recorded)
        {
            var contribution = new Contribution
            {
                ContributionAmount = amount,
                ContributionDate = date,
                ContributionStatusId = statusId,
                ContributionTypeId = contributionType,
                CreatedDate = date,
                PledgeFlag = contributionType == ContributionTypeCode.Pledge,
                FundId = fundId,
                PeopleId = peopleId,
            };
            db.Contributions.InsertOnSubmit(contribution);
            db.SubmitChanges();

            db.BundleDetails.InsertOnSubmit(new BundleDetail
            {
                BundleHeader = bundleHeader,
                Contribution = contribution,
                CreatedDate = DateTime.Now,
            });
            db.SubmitChanges();

            return contribution;
        }

        protected void DeleteContribution(CMSDataContext db, int contributionId)
        {
            db.ExecuteCommand("DELETE FROM [Contribution] WHERE [ContributionId] = {0}", contributionId);
            db.SubmitChanges();
        }

        protected void DeleteBundleDetail(CMSDataContext db, int bundleHeaderId, int contributionId)
        {
            db.ExecuteCommand("DELETE FROM [BundleDetail] WHERE [BundleHeaderId] = {0} AND [ContributionId] = {1}", bundleHeaderId, contributionId);
            db.SubmitChanges();
        }

        protected void DeleteBundleHeader(CMSDataContext db, int bundleHeaderId)
        {
            db.ExecuteCommand("DELETE FROM [BundleHeader] WHERE [BundleHeaderId] = {0}", bundleHeaderId);
            db.SubmitChanges();
        }

        protected void DeleteAllFromBundle(CMSDataContext db, BundleHeader bundleHeader)
        {
            var bundleDetails = db.BundleDetails.Where(b => b.BundleHeaderId == bundleHeader.BundleHeaderId);
            foreach (var item in bundleDetails)
            {
                var contribution = db.Contributions.SingleOrDefault(c => c.ContributionId == item.ContributionId);
                db.Contributions.DeleteOnSubmit(contribution);    
            }
            db.BundleDetails.DeleteAllOnSubmit(bundleDetails);
            db.BundleHeaders.DeleteOnSubmit(bundleHeader);
            db.SubmitChanges();
        }
    }
}