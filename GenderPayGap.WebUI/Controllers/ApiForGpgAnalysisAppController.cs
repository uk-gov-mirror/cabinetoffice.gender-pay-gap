﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using GenderPayGap.Core;
using GenderPayGap.Core.Helpers;
using GenderPayGap.Core.Interfaces;
using GenderPayGap.Database;
using GenderPayGap.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GenderPayGap.WebUI.Controllers
{
    /// <summary>
    /// This is the API used by the GPG Analysis App (commonly known as Tom's App)
    /// </summary>
    [Route("api/gpg-analysis-app")]
    public class ApiForGpgAnalysisAppController : Controller
    {

        private readonly IDataRepository dataRepository;

        public ApiForGpgAnalysisAppController(IDataRepository dataRepository)
        {
            this.dataRepository = dataRepository;
        }


        [HttpGet("scopes-and-returns")]
        public IActionResult ScopesAndReturns(string password)
        {
            string expectedPassword = Global.GpgAnalysisAppApiPassword;
            if (string.IsNullOrWhiteSpace(expectedPassword))
            {
                throw new ArgumentException("GpgAnalysisAppApiPassword must be set to enable the API");
            }
            if (password != expectedPassword)
            {
                return Unauthorized();
            }

            // IMPORTANT: This variable isn't used, but running this query makes the next query much faster
            var allOrgsWithScopes = dataRepository.GetAll<Organisation>()
                .Include(o => o.OrganisationScopes)
                .ToList();

            // IMPORTANT: This variable isn't used, but running this query makes the next query much faster
            var allOrgsWithReturns = dataRepository.GetAll<Organisation>()
                .Include(o => o.Returns)
                .ToList();

            // IMPORTANT: This variable isn't used, but running this query makes the next query much faster
            var allOrgsWithPublicSectorTypes = dataRepository.GetAll<Organisation>()
                .Include(o => o.LatestPublicSectorType)
                .ToList();

            List<Organisation> organisations = dataRepository
                .GetAll<Organisation>()
                //.Include(o => o.OrganisationScopes) // Moved into separate pre-load query
                //.Include(o => o.Returns) // Moved into separate pre-load query
                //.Include(o => o.LatestPublicSectorType) // Moved into separate pre-load query
                .Include(o => o.OrganisationSicCodes)
                .ThenInclude(osc => osc.SicCode)
                .ThenInclude(sc => sc.SicSection)
                .ToList();

            List<int> reportingYears = ReportingYearsHelper.GetReportingYears();

            var records = new List<object>();
            foreach (Organisation organisation in organisations)
            {
                foreach (int reportingYear in reportingYears)
                {
                    OrganisationScope scopeForYear = organisation.GetLatestScopeForSnapshotYear(reportingYear);
                    
                    if (scopeForYear != null)
                    {
                        Return returnForYear = organisation.GetReturn(reportingYear);

                        // Organisations are allowed multiple SIC codes
                        // But, for this API, we just want 1 SIC code per organisation
                        // We use this method to keep this API consistent with the database query
                        OrganisationSicCode firstSicCodeForOrganisation = organisation.OrganisationSicCodes
                            .Where(osc => !osc.IsRetired())
                            .OrderBy(osc => osc.SicCodeId)
                            .ThenBy(osc => osc.Source)
                            .FirstOrDefault();

                        var record = new
                        {
                            OrganisationId = organisation.OrganisationId,
                            OrganisationName = organisation.OrganisationName,
                            EmployerReference = organisation.EmployerReference,
                            CompanyNumber = organisation.CompanyNumber,
                            OrganisationStatus = $"{organisation.Status.ToString()} ({(int)organisation.Status})",
                            SectorType = $"{organisation.SectorType.ToString()} ({(int)organisation.SectorType})",
                            ScopeStatus = $"{ScopeStatusToString(scopeForYear.ScopeStatus)} ({(int)scopeForYear.ScopeStatus})",
                            SnapshotDate = reportingYear,
                            SicCodeSectionDescription = firstSicCodeForOrganisation?.SicCode?.SicSection?.Description,
                            ReturnId = returnForYear?.ReturnId,
                            PublicSectorDescription = organisation.LatestPublicSectorType?.PublicSectorType?.Description,
                            OrganisationSize = returnForYear?.OrganisationSize.GetAttribute<DisplayAttribute>().Name
                        };

                        records.Add(record);
                    }
                }
            }
            
            return Json(records);
        }

        private static string ScopeStatusToString(ScopeStatuses scope)
        {
            switch (scope)
            {
                case ScopeStatuses.InScope:
                    return "In scope";
                case ScopeStatuses.OutOfScope:
                    return "Out of scope";
                case ScopeStatuses.PresumedInScope:
                    return "Presumed in scope";
                case ScopeStatuses.PresumedOutOfScope:
                    return "Presumed out of scope";
                case ScopeStatuses.Unknown:
                default:
                    return "Unknown";
            }
        }


        [HttpGet("submissions")]
        public IActionResult Submissions(string password)
        {
            string expectedPassword = Global.GpgAnalysisAppApiPassword;
            if (string.IsNullOrWhiteSpace(expectedPassword))
            {
                throw new ArgumentException("GpgAnalysisAppApiPassword must be set to enable the API");
            }
            if (password != expectedPassword)
            {
                return Unauthorized();
            }

            List<Return> returns = dataRepository
                .GetAll<Return>()
                .Include(r => r.ReturnStatuses)
                .Include(r => r.Organisation)
                .ThenInclude(o => o.Returns)
                .ToList();

            var records = new List<object>();

            foreach (Return ret in returns)
            {
                Return firstReturnThisYear = ret.Organisation.Returns
                    .Where(r => r.AccountingDate == ret.AccountingDate)
                    .OrderBy(r => r.Modified)
                    .First();

                // Note: It seems a little odd to produce a record for each Return AND for each ReturnStatus
                // Only a small number of Returns have more than one ReturnStatus, but this means that if a Return was Retired or Deleted,
                // we will produce two records for it. I'm not sure if Tom's App deals with this correctly (there's a risk of double-counting)
                foreach (ReturnStatus returnStatus in ret.ReturnStatuses)
                {
                    var record = new
                    {
                        OrganisationId = ret.OrganisationId,
                        latestReturnAccountingDate = ret.AccountingDate.ToString("yyyy-MM-dd"),
                        StatusId = $"{returnStatus.Status.ToString()} ({(int)returnStatus.Status})",

                        // Note: These four fields are quite confusing and it could be good to check that Tom's App deals with them correctly
                        // StatusDate and StatusDetails are from the ReturnStatus
                        // latestReturnStatus and latestReturnStatusDate are from the Return
                        StatusDate = returnStatus.StatusDate.ToString("yyyy-MM-dd HH:mm:ss.fffffff"),
                        StatusDetails = returnStatus.StatusDetails,
                        latestReturnStatus = $"{ret.Status.ToString()} ({(int)ret.Status})",
                        latestReturnStatusDate = ret.StatusDate.ToString("yyyy-MM-dd HH:mm:ss.fffffff"),
                        
                        dateFirstReportedInYear = firstReturnThisYear.Modified.ToString("yyyy-MM-dd HH:mm:ss.fffffff"),
                        SubmittedBy = (ret.FirstName != null && ret.LastName != null && ret.JobTitle != null) ? $"{ret.FirstName} {ret.LastName} [{ret.JobTitle}]" : null,
                        LatestReturnLateReason = ret.LateReason,
                        ReturnModifiedFields = ret.Modifications,
                        EHRCResponse = ret.EHRCResponse.ToString().ToLower(),
                        DiffMeanBonusPercent = ret.DiffMeanBonusPercent,
                        DiffMeanHourlyPayPercent = ret.DiffMeanHourlyPayPercent,
                        DiffMedianBonusPercent = ret.DiffMedianBonusPercent,
                        DiffMedianHourlyPercent = ret.DiffMedianHourlyPercent,
                        FemaleLowerPayBand = ret.FemaleLowerPayBand,
                        FemaleMedianBonusPayPercent = ret.FemaleMedianBonusPayPercent,
                        FemaleMiddlePayBand = ret.FemaleMiddlePayBand,
                        FemaleUpperPayBand = ret.FemaleUpperPayBand,
                        FemaleUpperQuartilePayBand = ret.FemaleUpperQuartilePayBand,
                        MaleLowerPayBand = ret.MaleLowerPayBand,
                        MaleMedianBonusPayPercent = ret.MaleMedianBonusPayPercent,
                        MaleMiddlePayBand = ret.MaleMiddlePayBand,
                        MaleUpperPayBand = ret.MaleUpperPayBand,
                        MaleUpperQuartilePayBand = ret.MaleUpperQuartilePayBand
                    };

                    records.Add(record);
                }
            }

            return Json(records);
        }

    }
}
