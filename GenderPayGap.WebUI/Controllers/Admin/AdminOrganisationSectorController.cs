﻿using System;
using System.Linq;
using GenderPayGap.Core;
using GenderPayGap.Core.Classes;
using GenderPayGap.Core.Interfaces;
using GenderPayGap.Database;
using GenderPayGap.Database.Models;
using GenderPayGap.Extensions;
using GenderPayGap.WebUI.Classes;
using GenderPayGap.WebUI.Helpers;
using GenderPayGap.WebUI.Models.Admin;
using GenderPayGap.WebUI.Services;
using GovUkDesignSystem;
using GovUkDesignSystem.Parsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GenderPayGap.WebUI.Controllers.Admin
{
    [Authorize(Roles = LoginRoles.GpgAdmin)]
    [Route("admin")]
    public class AdminOrganisationSectorController : Controller
    {
        private readonly IDataRepository dataRepository;
        private readonly AuditLogger auditLogger;

        public AdminOrganisationSectorController(
            IDataRepository dataRepository,
            AuditLogger auditLogger)
        {
            this.dataRepository = dataRepository;
            this.auditLogger = auditLogger;
        }
        
          [HttpGet("organisation/{id}/sector")]
        public IActionResult ViewSectorHistory(long id)
        {
            var viewModel = new AdminSectorHistoryViewModel();
            
            var organisation = dataRepository.Get<Organisation>(id);
            var sectorHistory = dataRepository.GetAll<AuditLog>()
                .Where(al => al.Action == AuditedAction.AdminChangedOrganisationSector)
                .Where(al => al.Organisation.OrganisationId == id)
                .OrderByDescending(al => al.CreatedDate)
                .ToList();

            viewModel.Organisation = organisation;
            viewModel.SectorHistory = sectorHistory;

            return View("ViewOrganisationSector", viewModel);
        }
        
        [HttpGet("organisation/{id}/sector/change")]
        public IActionResult ChangeSectorGet(long id)
        {
            var viewModel = new AdminChangeSectorViewModel();

            UpdateAdminChangeSectorViewModelFromOrganisation(viewModel, id);
            viewModel.NewSector = viewModel.Organisation.SectorType == SectorTypes.Private ? NewSectorTypes.Private : NewSectorTypes.Public;

            return View("ChangeSector", viewModel);
        }
        
        [HttpPost("organisation/{id}/sector/change")]
        [PreventDuplicatePost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangeSectorPost(long id, AdminChangeSectorViewModel viewModel)
        {
            switch (viewModel.Action)
            {
                case ChangeOrganisationSectorViewModelActions.OfferNewSectorAndReason:
                    return OfferNewSectorAndReason(id, viewModel);
            
                case ChangeOrganisationSectorViewModelActions.ConfirmSectorChange:
                    return ConfirmSectorChange(id, viewModel);
                default:
                    throw new ArgumentException("Unknown action in AdminOrganisationSectorController.ChangeSectorPost");
            }
        }

        private IActionResult OfferNewSectorAndReason(long id, AdminChangeSectorViewModel viewModel)
        {
            UpdateAdminChangeSectorViewModelFromOrganisation(viewModel, id);
            ValidateAdminChangeSectorViewModel(viewModel);
                    
            // Check if new sector is same as original organisation sector
            var newSector = viewModel.NewSector == NewSectorTypes.Private ? SectorTypes.Private : SectorTypes.Public;
            if (newSector == viewModel.Organisation.SectorType)
            {
                viewModel.AddErrorFor(
                    m => m.NewSector,
                    "The organisation is already assigned to this sector."
                );
            }
                    
            if (viewModel.HasAnyErrors())
            {
                return View("ChangeSector", viewModel);
            }
            
            return View("ConfirmSectorChange", viewModel);
        }

        private IActionResult ConfirmSectorChange(long id, AdminChangeSectorViewModel viewModel)
        {
            ChangeSector(viewModel, id);
            return RedirectToAction("ViewSectorHistory", "AdminOrganisationSector", new {id});
        }

        [HttpGet("organisation/{id}/change-public-sector-classification")]
        public IActionResult ChangePublicSectorClassificationGet(long id)
        {
            Organisation organisation = dataRepository.Get<Organisation>(id);

            var viewModel = new AdminChangePublicSectorClassificationViewModel
            {
                OrganisationId = organisation.OrganisationId,
                OrganisationName = organisation.OrganisationName,
                PublicSectorTypes = dataRepository.GetAll<PublicSectorType>().ToList(),
                SelectedPublicSectorTypeId = organisation.LatestPublicSectorType?.PublicSectorTypeId
            };

            return View("ChangePublicSectorClassification", viewModel);
        }

        [HttpPost("organisation/{id}/change-public-sector-classification")]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePublicSectorClassificationPost(long id, AdminChangePublicSectorClassificationViewModel viewModel)
        {
            Organisation organisation = dataRepository.Get<Organisation>(id);
            viewModel.OrganisationId = organisation.OrganisationId;
            viewModel.OrganisationName = organisation.OrganisationName;
            viewModel.PublicSectorTypes = dataRepository.GetAll<PublicSectorType>().ToList();

            viewModel.ParseAndValidateParameters(Request, m=> m.Reason);

            if (!viewModel.SelectedPublicSectorTypeId.HasValue)
            {
                viewModel.AddErrorFor(
                    m => m.SelectedPublicSectorTypeId,
                    "Please select a public sector classification");
            }

            if (viewModel.HasAnyErrors())
            {
                return View("ChangePublicSectorClassification", viewModel);
            }

            var newPublicSectorType = dataRepository.GetAll<PublicSectorType>()
                .FirstOrDefault(p => p.PublicSectorTypeId == viewModel.SelectedPublicSectorTypeId.Value);
            if (newPublicSectorType == null)
            {
                throw new ArgumentException($"User selected an invalid PublicSectorType ({viewModel.SelectedPublicSectorTypeId})");
            }

            AuditChange(viewModel, organisation, newPublicSectorType);

            RetireExistingOrganisationPublicSectorTypesForOrganisation(organisation);

            AddNewOrganisationPublicSectorType(organisation, viewModel.SelectedPublicSectorTypeId.Value);

            dataRepository.SaveChanges();

            return RedirectToAction("ViewOrganisation", "AdminViewOrganisation", new {id = organisation.OrganisationId});
        }

        private void AuditChange(
            AdminChangePublicSectorClassificationViewModel viewModel,
            Organisation organisation,
            PublicSectorType newPublicSectorType)
        {
            auditLogger.AuditChangeToOrganisation(
                AuditedAction.AdminChangeOrganisationPublicSectorClassification,
                organisation,
                new
                {
                    OldClassification = organisation.LatestPublicSectorType?.PublicSectorType?.Description,
                    NewClassification = newPublicSectorType.Description,
                    viewModel.Reason
                },
                User);
        }

        private void RetireExistingOrganisationPublicSectorTypesForOrganisation(Organisation organisation)
        {
            var organisationPublicSectorTypes = dataRepository.GetAll<OrganisationPublicSectorType>()
                .Where(opst => opst.OrganisationId == organisation.OrganisationId)
                .ToList();

            foreach (OrganisationPublicSectorType organisationPublicSectorType in organisationPublicSectorTypes)
            {
                organisationPublicSectorType.Retired = VirtualDateTime.Now;
            }
        }

        private void AddNewOrganisationPublicSectorType(Organisation organisation, int publicSectorTypeId)
        {
            var newOrganisationPublicSectorType = new OrganisationPublicSectorType
            {
                OrganisationId = organisation.OrganisationId,
                PublicSectorTypeId = publicSectorTypeId,
                Source = "Service Desk"
            };

            dataRepository.Insert(newOrganisationPublicSectorType);

            organisation.LatestPublicSectorType = newOrganisationPublicSectorType;
        }
        
        private void UpdateAdminChangeSectorViewModelFromOrganisation(AdminChangeSectorViewModel viewModel, long organisationId)
        {
            viewModel.Organisation = dataRepository.Get<Organisation>(organisationId);
        }
        
        private void ValidateAdminChangeSectorViewModel(AdminChangeSectorViewModel viewModel)
        {
            viewModel.ParseAndValidateParameters(Request, m => m.NewSector);
            viewModel.ParseAndValidateParameters(Request, m => m.Reason);
        }
        
        private void ChangeSector(AdminChangeSectorViewModel viewModel, long organisationId)
        {
            var organisation = dataRepository.Get<Organisation>(organisationId);

            SectorTypes previousSector = organisation.SectorType;
            SectorTypes newSector = viewModel.NewSector.Value == NewSectorTypes.Private ? SectorTypes.Private : SectorTypes.Public;

            // Update the sector
            organisation.SectorType = newSector;

            // Remove SIC codes when company changes between sectors
            organisation.OrganisationSicCodes.Clear();
            
            // Change snapshot date for all organisation scopes to match new sector
            organisation.OrganisationScopes.ForEach(
                scope => scope.SnapshotDate = organisation.SectorType.GetAccountingStartDate(scope.SnapshotDate.Year)
            );
            
            // Change accounting date for all returns to match new sector
            organisation.Returns.ForEach(
                returnItem => returnItem.AccountingDate = organisation.SectorType.GetAccountingStartDate(returnItem.AccountingDate.Year)
            );

            dataRepository.SaveChanges();

            // Audit log
            auditLogger.AuditChangeToOrganisation(
                AuditedAction.AdminChangedOrganisationSector,
                organisation,
                new AdminChangeSectorAuditLogDetails {OldSector = previousSector, NewSector = newSector, Reason = viewModel.Reason},
                User);

        }

    }
}
