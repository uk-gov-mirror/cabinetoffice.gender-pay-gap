﻿using System;
using System.Collections.Generic;
using System.Linq;
using GenderPayGap.Core.Classes;
using GenderPayGap.Core.Helpers;
using GenderPayGap.Core.Interfaces;
using GenderPayGap.Database;
using GenderPayGap.Database.Models;
using GenderPayGap.WebUI.Helpers;
using GenderPayGap.WebUI.Models.ManageOrganisations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GenderPayGap.WebUI.Controllers.ManageOrganisations
{
    [Route("account/organisations")]
    public class ManageOrganisationsController : Controller
    {

        private readonly IDataRepository dataRepository;

        public ManageOrganisationsController(IDataRepository dataRepository)
        {
            this.dataRepository = dataRepository;
        }

        [HttpGet]
        [Authorize(Roles = LoginRoles.GpgEmployer + "," + LoginRoles.GpgAdmin)]
        public IActionResult ManageOrganisationsGet()
        {
            // Check for feature flag and redirect if not enabled
            if (!FeatureFlagHelper.IsFeatureEnabled(FeatureFlag.NewManageOrganisationsJourney))
            {
                return RedirectToAction("ManageOrganisationsGet", "ManageOrganisations");
            }

            if (User.IsInRole(LoginRoles.GpgAdmin))
            {
                return RedirectToAction("AdminHomePage", "AdminHomepage");
            }

            User user = ControllerHelper.GetGpgUserFromAspNetUser(User, dataRepository);
            ControllerHelper.ThrowIfUserAccountRetiredOrEmailNotVerified(user);
            ControllerHelper.RedirectIfUserNeedsToReadPrivacyPolicy(User, user, Url);

            var viewModel = new ManageOrganisationsViewModel
            {
                UserOrganisations = user.UserOrganisations.OrderBy(uo => uo.Organisation.OrganisationName)
            };

            return View("ManageOrganisations", viewModel);

        }

        [HttpGet("{encryptedOrganisationId}")]
        [Authorize(Roles = LoginRoles.GpgEmployer)]
        public IActionResult ManageOrganisationGet(string encryptedOrganisationId)
        {
            // Check for feature flag and redirect if not enabled
            if (!FeatureFlagHelper.IsFeatureEnabled(FeatureFlag.NewManageOrganisationsJourney))
            {
                return RedirectToAction("ManageOrganisationGet", "ManageOrganisations", new {encryptedOrganisationId = encryptedOrganisationId});
            }
            
            long organisationId = ControllerHelper.DecryptOrganisationIdOrThrow404(encryptedOrganisationId);
            User user = ControllerHelper.GetGpgUserFromAspNetUser(User, dataRepository);
            ControllerHelper.ThrowIfUserAccountRetiredOrEmailNotVerified(user);
            ControllerHelper.ThrowIfUserDoesNotHavePermissionsForGivenOrganisation(User, dataRepository, organisationId);

            var organisation = dataRepository.Get<Organisation>(organisationId);
            if (OrganisationIsNewThisYearAndHasNotProvidedScopeForLastYear(organisation))
            {
                return RedirectToAction("DeclareScope", "Organisation", new { id = encryptedOrganisationId });
            }

            // build the view model
            List<int> yearsWithDraftReturns =
                dataRepository.GetAll<DraftReturn>()
                    .Where(d => d.OrganisationId == organisationId)
                    .Select(d => d.SnapshotYear)
                    .ToList();

            var viewModel = new ManageOrganisationViewModel(organisation, user, yearsWithDraftReturns);

            return View("ManageOrganisation", viewModel);
        }

        private static bool OrganisationIsNewThisYearAndHasNotProvidedScopeForLastYear(Organisation organisation)
        {
            DateTime currentYearSnapshotDate = organisation.SectorType.GetAccountingStartDate();
            bool organisationCreatedInCurrentReportingYear = organisation.Created >= currentYearSnapshotDate;

            if (organisationCreatedInCurrentReportingYear)
            {
                int previousReportingYear = currentYearSnapshotDate.AddYears(-1).Year;
                OrganisationScope scope = organisation.GetScopeForYear(previousReportingYear);

                if (scope.IsScopePresumed())
                {
                    return true;
                }
            }

            return false;
        }

    }
}
