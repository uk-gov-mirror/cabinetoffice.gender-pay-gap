using System;
using GenderPayGap.Core;
using GenderPayGap.Core.Classes;
using GenderPayGap.Database;
using Moq;

namespace GenderPayGap.Tests.Common.TestHelpers
{
    public static class ReturnHelper
    {

        /// <summary>
        ///     Creates a new Empty Return
        /// </summary>
        /// <param name="userOrganisation"></param>
        /// <param name="snapshotYear"></param>
        public static Return GetNewReturnForOrganisationAndYear(UserOrganisation userOrganisation, int snapshotYear)
        {
            var newReturn = new Return();
            newReturn.Organisation = userOrganisation.Organisation;
            newReturn.OrganisationId = userOrganisation.OrganisationId;
            newReturn.AccountingDate = userOrganisation.Organisation.SectorType.GetAccountingStartDate(snapshotYear);

            return newReturn;
        }

        public static Return CreateTestReturnWithNoBonus(Organisation organisation, int testYear = 2017)
        {
            return CreateBonusTestReturn(
                organisation,
                default,
                default,
                default,
                default,
                testYear);
        }

        public static Return CreateBonusTestReturn(Organisation organisation,
            decimal femaleMedianBonusPayPercent,
            decimal maleMedianBonusPayPercent,
            decimal diffMeanBonusPercent,
            decimal diffMedianBonusPercent,
            int testYear = 2017)
        {
            return new Return {
                ReturnId = organisation.OrganisationId + 100,
                Organisation = organisation,
                OrganisationId = organisation.OrganisationId,
                AccountingDate = organisation.SectorType.GetAccountingStartDate(testYear),
                Status = ReturnStatuses.Submitted,
                DiffMeanHourlyPayPercent = 99,
                DiffMedianHourlyPercent = 97,
                FemaleLowerPayBand = 96,
                FemaleMiddlePayBand = 94,
                FemaleUpperPayBand = 93,
                FemaleUpperQuartilePayBand = 92,
                MaleLowerPayBand = 91,
                MaleUpperQuartilePayBand = 89,
                MaleMiddlePayBand = 88,
                MaleUpperPayBand = 87,
                FirstName = $"Firstname{organisation.OrganisationId:000}",
                LastName = $"Lastname{organisation.OrganisationId:000}",
                JobTitle = $"Job title {organisation.OrganisationId:000}",
                CompanyLinkToGPGInfo = $"http://WebOrg{organisation.OrganisationId:000}",
                MinEmployees = 250,
                MaxEmployees = 499,

                /* fill bonus information */
                FemaleMedianBonusPayPercent = femaleMedianBonusPayPercent,
                MaleMedianBonusPayPercent = maleMedianBonusPayPercent,
                DiffMeanBonusPercent = diffMeanBonusPercent,
                DiffMedianBonusPercent = diffMedianBonusPercent
            };
        }

        public static Return CreateTestReturn(Organisation organisation, int testYear = 2017)
        {
            return CreateBonusTestReturn(
                organisation,
                95,
                90,
                100,
                98,
                testYear);
        }

        public static Return CreateLateReturn(Organisation organisation, DateTime snapshotDate, DateTime modifiedDate, OrganisationScope scope)
        {
            var lateReturn = new Return {
                Organisation = organisation,
                ReturnId = organisation.OrganisationId + 100,
                AccountingDate = snapshotDate,
                MinEmployees = 250,
                MaxEmployees = 499,
                Created = modifiedDate,
                Modified = modifiedDate
            };

            OrganisationHelper.LinkOrganisationAndReturn(organisation, lateReturn);
            OrganisationHelper.LinkOrganisationAndScope(organisation, scope, true);

            lateReturn.IsLateSubmission = lateReturn.CalculateIsLateSubmission();
            return lateReturn;
        }

    }
}
