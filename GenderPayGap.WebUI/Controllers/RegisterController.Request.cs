﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using GenderPayGap.Core;
using GenderPayGap.Core.Classes.Logger;
using GenderPayGap.Core.Models;
using GenderPayGap.Core.Models.HttpResultModels;
using GenderPayGap.Database;
using GenderPayGap.Extensions;
using GenderPayGap.WebUI.Classes;
using GenderPayGap.WebUI.Helpers;
using GenderPayGap.WebUI.Models.Register;
using GenderPayGap.WebUI.Services;
using GovUkDesignSystem.Parsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GenderPayGap.WebUI.Controllers
{
    public partial class RegisterController : BaseController
    {

        private ActionResult UnwrapRegistrationRequest(OrganisationViewModel model, out UserOrganisation userOrg)
        {
            userOrg = null;

            long userId = 0;
            long orgId = 0;
            try
            {
                string code = Encryption.DecryptQuerystring(model.ReviewCode);
                code = HttpUtility.UrlDecode(code);
                string[] args = code.SplitI(":");
                if (args.Length != 3)
                {
                    throw new ArgumentException("Too few parameters in registration review code");
                }

                userId = args[0].ToLong();
                if (userId == 0)
                {
                    throw new ArgumentException("Invalid user id in registration review code");
                }

                orgId = args[1].ToLong();
                if (orgId == 0)
                {
                    throw new ArgumentException("Invalid organisation id in registration review code");
                }
            }
            catch
            {
                return View("CustomError", new ErrorViewModel(1114));
            }

            //Get the user oganisation
            userOrg = DataRepository.GetAll<UserOrganisation>()
                .Where(uo => uo.UserId == userId)
                .Where(uo => uo.OrganisationId == orgId)
                .FirstOrDefault();

            if (userOrg == null)
            {
                return View("CustomError", new ErrorViewModel(1115));
            }

            //Check this registrations hasnt already completed
            if (userOrg.HasBeenActivated())
            {
                return View("CustomError", new ErrorViewModel(1145));
            }

            switch (userOrg.Organisation.Status)
            {
                case OrganisationStatuses.Active:
                case OrganisationStatuses.Pending:
                    break;
                default:
                    throw new ArgumentException(
                        $"Invalid organisation status {userOrg.Organisation.Status} user {userId} and organisation {orgId} for reviewing registration request");
            }

            if (userOrg.Address == null)
            {
                throw new Exception($"Cannot find address for user {userId} and organisation {orgId} for reviewing registration request");
            }

            //Load view model
            model.ContactFirstName = userOrg.User.ContactFirstName;
            model.ContactLastName = userOrg.User.ContactLastName;
            if (string.IsNullOrWhiteSpace(model.ContactFirstName) && string.IsNullOrWhiteSpace(model.ContactFirstName))
            {
                model.ContactFirstName = userOrg.User.Firstname;
                model.ContactLastName = userOrg.User.Lastname;
            }

            model.ContactJobTitle = userOrg.User.ContactJobTitle.Coalesce(userOrg.User.JobTitle);
            model.ContactEmailAddress = userOrg.User.ContactEmailAddress.Coalesce(userOrg.User.EmailAddress);
            model.EmailAddress = userOrg.User.EmailAddress;
            model.ContactPhoneNumber = userOrg.User.ContactPhoneNumber;

            model.OrganisationName = userOrg.Organisation.OrganisationName;
            model.CompanyNumber = userOrg.Organisation.CompanyNumber;
            model.SectorType = userOrg.Organisation.SectorType;
            
            var sicCodeIds = userOrg.Organisation.GetSicCodes().Select(o => o.SicCode.SicCodeId).ToList();
            model.SicCodes = DataRepository.GetAll<SicCode>().Where(s => sicCodeIds.Contains(s.SicCodeId)).ToList();

            model.Address1 = userOrg.Address.Address1;
            model.Address2 = userOrg.Address.Address2;
            model.Address3 = userOrg.Address.Address3;
            model.Country = userOrg.Address.Country;
            model.Postcode = userOrg.Address.PostCode;
            model.PoBox = userOrg.Address.PoBox;

            model.RegisteredAddress = userOrg.Address.Status == AddressStatuses.Pending
                ? userOrg.Organisation.GetLatestAddress().GetAddressLines()
                : null;

            model.CharityNumber = userOrg.Organisation.OrganisationReferences
                .Where(o => o.ReferenceName.ToLower() == nameof(OrganisationViewModel.CharityNumber).ToLower())
                .Select(or => or.ReferenceValue)
                .FirstOrDefault();

            model.MutualNumber = userOrg.Organisation.OrganisationReferences
                .Where(o => o.ReferenceName.ToLower() == nameof(OrganisationViewModel.MutualNumber).ToLower())
                .Select(or => or.ReferenceValue)
                .FirstOrDefault();

            model.OtherName = userOrg.Organisation.OrganisationReferences.ToList()
                .Where(
                    o => o.ReferenceName.ToLower() != nameof(OrganisationViewModel.CharityNumber).ToLower()
                         && o.ReferenceName.ToLower() != nameof(OrganisationViewModel.MutualNumber).ToLower())
                .Select(or => or.ReferenceName)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(model.OtherName))
            {
                model.OtherValue = userOrg.Organisation.OrganisationReferences
                    .Where(o => o.ReferenceName == model.OtherName)
                    .Select(or => or.ReferenceValue)
                    .FirstOrDefault();
            }

            return null;
        }

        #region ReviewRequest

        [Authorize(Roles = LoginRoles.GpgAdmin)]
        [HttpGet("review-request")]
        public async Task<IActionResult> ReviewRequest(string code)
        {
            //Ensure user has completed the registration process
            User currentUser;
            IActionResult checkResult = CheckUserRegisteredOk(out currentUser);
            if (checkResult != null)
            {
                return checkResult;
            }

            var model = new OrganisationViewModel();

            if (string.IsNullOrWhiteSpace(code))
            {
                //Load the employers from session
                model = this.UnstashModel<OrganisationViewModel>();
                if (model == null)
                {
                    return View("CustomError", new ErrorViewModel(1114));
                }
            }
            else
            {
                model.ReviewCode = code;
            }

            //Unwrap code
            UserOrganisation userOrg;
            ActionResult result = UnwrapRegistrationRequest(model, out userOrg);
            if (result != null)
            {
                return result;
            }

            //Tell reviewer if this org has already been approved
            if (model.ManualRegistration)
            {
                UserOrganisation firstRegistered = userOrg.Organisation.UserOrganisations.OrderByDescending(uo => uo.PINConfirmedDate)
                    .FirstOrDefault(uo => uo.HasBeenActivated());
                if (firstRegistered != null)
                {
                    AddModelError(
                        3017,
                        parameters: new
                        {
                            approvedUser = firstRegistered.User.EmailAddress,
                            approvedDate = firstRegistered.PINConfirmedDate.Value.ToShortDateString(),
                            approvedAddress = firstRegistered.Address?.GetAddressString()
                        });
                }

                //Tell reviewer how many other open regitrations for same organisation
                int requestCount = await DataRepository.GetAll<UserOrganisation>()
                    .CountAsync(
                        uo => uo.UserId != userOrg.UserId
                              && uo.OrganisationId == userOrg.OrganisationId
                              && uo.Organisation.Status == OrganisationStatuses.Pending);
                if (requestCount > 0)
                {
                    AddModelError(3018, parameters: new {requestCount});
                }
            }

            //Get any conflicting or similar organisations
            IEnumerable<long> results;
            var orgIds = new HashSet<long>();

            if (!string.IsNullOrWhiteSpace(model.CompanyNumber))
            {
                results = DataRepository.GetAll<Organisation>()
                    .Where(o => o.OrganisationId != userOrg.OrganisationId)
                    .Where(o => o.SectorType == SectorTypes.Private)
                    .Where(o => o.CompanyNumber == model.CompanyNumber)
                    .Select(o => o.OrganisationId);
                if (results.Any())
                {
                    orgIds.AddRange(results);
                }
            }

            if (!string.IsNullOrWhiteSpace(model.CharityNumber))
            {
                results = DataRepository.GetAll<OrganisationReference>()
                    .Where(r => r.OrganisationId != userOrg.OrganisationId)
                    .Where(r => r.ReferenceName.ToLower() == "charity number")
                    .Where(r => r.ReferenceValue.ToLower() == model.CharityNumber.ToLower())
                    .Select(r => r.OrganisationId);
                if (results.Any())
                {
                    orgIds.AddRange(results);
                }
            }

            if (!string.IsNullOrWhiteSpace(model.MutualNumber))
            {
                results = DataRepository.GetAll<OrganisationReference>()
                    .Where(r => r.OrganisationId != userOrg.OrganisationId)
                    .Where(r => r.ReferenceName.ToLower() == "mutual number")
                    .Where(r => r.ReferenceValue.ToLower() == model.MutualNumber.ToLower())
                    .Select(r => r.OrganisationId);
                if (results.Any())
                {
                    orgIds.AddRange(results);
                }
            }

            if (!string.IsNullOrWhiteSpace(model.OtherName) && !string.IsNullOrWhiteSpace(model.OtherValue))
            {
                results = DataRepository.GetAll<OrganisationReference>()
                    .Where(r => r.OrganisationId != userOrg.OrganisationId)
                    .Where(r => r.ReferenceName.ToLower() == model.OtherName.ToLower())
                    .Where(r => r.ReferenceValue.ToLower() == model.OtherValue.ToLower())
                    .Select(r => r.OrganisationId);
                if (results.Any())
                {
                    orgIds.AddRange(results);
                }
            }

            model.MatchedReferenceCount = orgIds.Count;

            //Only show orgs matching names when none matching references
            if (model.MatchedReferenceCount == 0)
            {
                string orgName = model.OrganisationName.ToLower().ReplaceI("limited", "").ReplaceI("ltd", "");
                results = DataRepository.GetAll<Organisation>()
                    .Where(o => o.OrganisationId != userOrg.OrganisationId)
                    .Where(o => o.OrganisationName.ToLower().Contains(orgName))
                    .Select(o => o.OrganisationId);
                if (results.Any())
                {
                    orgIds.AddRange(results);
                }

                results = Organisation.Search(
                        DataRepository.GetAll<Organisation>().Where(o => o.OrganisationId != userOrg.OrganisationId),
                        model.OrganisationName,
                        50 - results.Count(),
                        Global.LevenshteinDistance)
                    .Select(o => o.OrganisationId);
                if (results.Any())
                {
                    orgIds.AddRange(results);
                }
            }

            if (orgIds.Any())
            {
                //Add the registrations
                List<Organisation> orgs =
                    await DataRepository.GetAll<Organisation>().Where(o => orgIds.Contains(o.OrganisationId)).ToListAsync();
                model.ManualEmployers = orgs.Select(o => o.ToEmployerRecord()).ToList();
            }

            //Ensure exact match shown at top
            if (model.ManualEmployers != null)
            {
                if (model.ManualEmployers.Count > 1)
                {
                    int index = model.ManualEmployers.FindIndex(e => e.OrganisationName.EqualsI(model.OrganisationName));
                    if (index > 0)
                    {
                        model.ManualEmployers.Insert(0, model.ManualEmployers[index]);
                        model.ManualEmployers.RemoveAt(index + 1);
                    }
                }

                //Sort he organisations
                model.ManualEmployers = model.ManualEmployers.OrderBy(o => o.OrganisationName).ToList();
            }

            this.StashModel(model);
            return View("ReviewRequest", model);
        }

        [Authorize(Roles = LoginRoles.GpgAdmin)]
        [PreventDuplicatePost]
        [ValidateAntiForgeryToken]
        [HttpPost("review-request")]
        public IActionResult ReviewRequest(OrganisationViewModel model, string command)
        {
            //Ensure user has completed the registration process
            User currentUser;
            IActionResult checkResult = CheckUserRegisteredOk(out currentUser);
            if (checkResult != null)
            {
                return checkResult;
            }

            //Make sure we can load employers from session
            var m = this.UnstashModel<OrganisationViewModel>();
            if (m == null)
            {
                return View("CustomError", new ErrorViewModel(1112));
            }

            model.ManualEmployers = m.ManualEmployers;

            //Unwrap code
            UserOrganisation userOrg;
            ActionResult result = UnwrapRegistrationRequest(model, out userOrg);
            if (result != null)
            {
                return result;
            }

            //Check model is valid

            //Exclude the address details
            var excludes = new HashSet<string>();
            excludes.AddRange(
                nameof(model.Address1),
                nameof(model.Address2),
                nameof(model.Address3),
                nameof(model.City),
                nameof(model.County),
                nameof(model.Country),
                nameof(model.Postcode),
                nameof(model.PoBox));

            //Exclude the contact details
            excludes.AddRange(
                nameof(model.ContactFirstName),
                nameof(model.ContactLastName),
                nameof(model.ContactJobTitle),
                nameof(model.ContactEmailAddress),
                nameof(model.ContactPhoneNumber));

            //Exclude the SIC Codes
            excludes.Add(nameof(model.SicCodeIds));

            excludes.Add(nameof(model.SearchText));
            excludes.Add(nameof(model.OrganisationName));
            excludes.AddRange(
                nameof(model.CompanyNumber),
                nameof(model.CharityNumber),
                nameof(model.MutualNumber),
                nameof(model.OtherName),
                nameof(model.OtherValue));

            ModelState.Exclude(excludes.ToArray());

            if (!ModelState.IsValid)
            {
                this.CleanModelErrors<OrganisationViewModel>();
                return View(nameof(ReviewRequest), model);
            }

            if (command.EqualsI("decline"))
            {
                result = RedirectToAction("ConfirmCancellation");
            }
            else if (command.EqualsI("approve"))
            {
                Organisation conflictOrg = null;

                //Check for company number conflicts
                if (!string.IsNullOrWhiteSpace(model.CompanyNumber))
                {
                    conflictOrg = DataRepository.GetAll<Organisation>()
                        .FirstOrDefault(
                            o => userOrg.OrganisationId != o.OrganisationId && o.CompanyNumber.ToLower() == model.CompanyNumber.ToLower());
                    if (conflictOrg != null)
                    {
                        ModelState.AddModelError(
                            3031,
                            nameof(model.CompanyNumber),
                            new {organisationName = conflictOrg.OrganisationName, referenceName = "Company number"});
                    }
                }

                //Check for charity number conflicts
                if (!string.IsNullOrWhiteSpace(model.CharityNumber))
                {
                    OrganisationReference orgRef = DataRepository.GetAll<OrganisationReference>()
                        .FirstOrDefault(
                            o => userOrg.OrganisationId != o.OrganisationId
                                 && o.ReferenceName.ToLower() == nameof(model.CharityNumber).ToLower()
                                 && o.ReferenceValue.ToLower() == model.CharityNumber.ToLower());
                    conflictOrg = orgRef?.Organisation;
                    if (conflictOrg != null)
                    {
                        ModelState.AddModelError(
                            3031,
                            nameof(model.CharityNumber),
                            new {organisationName = conflictOrg.OrganisationName, referenceName = "Charity number"});
                    }
                }

                //Check for mutual number conflicts
                if (!string.IsNullOrWhiteSpace(model.MutualNumber))
                {
                    OrganisationReference orgRef = DataRepository.GetAll<OrganisationReference>()
                        .FirstOrDefault(
                            o => userOrg.OrganisationId != o.OrganisationId
                                 && o.ReferenceName.ToLower() == nameof(model.MutualNumber).ToLower()
                                 && o.ReferenceValue.ToLower() == model.MutualNumber.ToLower());
                    conflictOrg = orgRef?.Organisation;
                    if (conflictOrg != null)
                    {
                        ModelState.AddModelError(
                            3031,
                            nameof(model.MutualNumber),
                            new {organisationName = conflictOrg.OrganisationName, referenceName = "Mutual partnership number"});
                    }
                }

                //Check for other reference conflicts
                if (!string.IsNullOrWhiteSpace(model.OtherValue))
                {
                    OrganisationReference orgRef = DataRepository.GetAll<OrganisationReference>()
                        .FirstOrDefault(
                            o => userOrg.OrganisationId != o.OrganisationId
                                 && o.ReferenceName.ToLower() == model.OtherName.ToLower()
                                 && o.ReferenceValue.ToLower() == model.OtherValue.ToLower());
                    conflictOrg = orgRef?.Organisation;
                    if (conflictOrg != null)
                    {
                        ModelState.AddModelError(
                            3031,
                            nameof(model.OtherValue),
                            new {organisationName = conflictOrg.OrganisationName, referenceName = model.OtherValue});
                    }
                }

                if (!ModelState.IsValid)
                {
                    this.CleanModelErrors<OrganisationViewModel>();
                    return View("ReviewRequest", model);
                }

                //Activate the org user
                userOrg.PINConfirmedDate = VirtualDateTime.Now;

                //Activate the organisation
                userOrg.Organisation.SetStatus(
                    OrganisationStatuses.Active,
                    OriginalUser == null ? currentUser.UserId : OriginalUser.UserId,
                    "Manually registered");

                // save any sic codes
                if (!string.IsNullOrEmpty(model.SicCodeIds))
                {
                    IOrderedEnumerable<int> newSicCodes = model.SicCodeIds.Split(',').Cast<int>().OrderBy(sc => sc);
                    foreach (int sc in newSicCodes)
                    {
                        userOrg.Organisation.OrganisationSicCodes.Add(
                            new OrganisationSicCode
                            {
                                SicCodeId = sc, OrganisationId = userOrg.OrganisationId, Created = VirtualDateTime.Now
                            });
                    }
                }

                //Retire the old address 
                OrganisationAddress latestAddress = userOrg.Organisation.GetLatestAddress();
                if (latestAddress != null && latestAddress.AddressId != userOrg.Address.AddressId)
                {
                    latestAddress.SetStatus(
                        AddressStatuses.Retired,
                        OriginalUser == null ? currentUser.UserId : OriginalUser.UserId,
                        "Replaced by Manual registration");
                }

                //Activate the address
                userOrg.Address.SetStatus(
                    AddressStatuses.Active,
                    OriginalUser == null ? currentUser.UserId : OriginalUser.UserId,
                    "Manually registered");

                //Send the approved email to the applicant
                SendRegistrationAccepted(userOrg.User.ContactEmailAddress.Coalesce(userOrg.User.EmailAddress));

                //Log the approval
                auditLogger.AuditChangeToOrganisation(
                    AuditedAction.RegistrationLog,
                    userOrg.Organisation,
                    new
                    {
                        Status = "Manually registered",
                        Sector = userOrg.Organisation.SectorType,
                        Organisation = userOrg.Organisation.OrganisationName,
                        CompanyNo = userOrg.Organisation.CompanyNumber,
                        Address = userOrg?.Address.GetAddressString(),
                        SicCodes = userOrg.Organisation.GetSicCodeIdsString(),
                        UserFirstname = userOrg.User.Firstname,
                        UserLastname = userOrg.User.Lastname,
                        UserJobtitle = userOrg.User.JobTitle,
                        UserEmail = userOrg.User.EmailAddress,
                        userOrg.User.ContactFirstName,
                        userOrg.User.ContactLastName,
                        userOrg.User.ContactJobTitle,
                        userOrg.User.ContactOrganisation,
                        userOrg.User.ContactPhoneNumber
                    },
                    User);

                result = RedirectToAction("RequestAccepted");
            }
            else
            {
                return new HttpBadRequestResult($"Invalid command on '{command}'");
            }

            //Save the changes and redirect
            DataRepository.SaveChanges();

            //Send notification email to existing users 
            EmailSendingServiceHelpers.SendUserAddedEmailToExistingUsers(userOrg.Organisation, userOrg.User, emailSendingService);

            //Save the model for the redirect
            this.StashModel(model);

            return result;
        }

        //Send the registration request
        protected void SendRegistrationAccepted(string emailAddress)
        {
            //Send an acceptance link to the email address
            string returnUrl = Url.Action("ManageOrganisationsGet", "ManageOrganisations", null, "https");
            emailSendingService.SendOrganisationRegistrationApprovedEmail(emailAddress, returnUrl);
        }

        /// <summary>
        ///     ask the reviewer for decline reason and confirmation ///
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = LoginRoles.GpgAdmin)]
        [HttpGet("confirm-cancellation")]
        public IActionResult ConfirmCancellation()
        {
            //Ensure user has completed the registration process
            User currentUser;
            IActionResult checkResult = CheckUserRegisteredOk(out currentUser);
            if (checkResult != null)
            {
                return checkResult;
            }

            //Make sure we can load employers from session
            var model = this.UnstashModel<OrganisationViewModel>();
            if (model == null)
            {
                return View("CustomError", new ErrorViewModel(1112));
            }

            return View("ConfirmCancellation", model);
        }

        /// <summary>
        ///     On confirmation save the organisation
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = LoginRoles.GpgAdmin)]
        [PreventDuplicatePost]
        [ValidateAntiForgeryToken]
        [HttpPost("confirm-cancellation")]
        public async Task<IActionResult> ConfirmCancellation(OrganisationViewModel model, string command)
        {
            //Ensure user has completed the registration process
            User currentUser;
            IActionResult checkResult = CheckUserRegisteredOk(out currentUser);
            if (checkResult != null)
            {
                return checkResult;
            }
            
            // Validate cancellation reason
            model.ParseAndValidateParameters(Request, m => m.CancellationReason);
            if (model.HasAnyErrors())
            {
                View("ConfirmCancellation", model);
            }

            //If cancel button clicked the n return to review page
            if (command.EqualsI("Cancel"))
            {
                return RedirectToAction("ReviewRequest");
            }

            //Unwrap code
            UserOrganisation userOrg;
            ActionResult result = UnwrapRegistrationRequest(model, out userOrg);
            if (result != null)
            {
                return result;
            }

            //Log the rejection
            auditLogger.AuditChangeToOrganisation(
                AuditedAction.RegistrationLog,
                userOrg.Organisation,
                new
                {
                    Status = "Manually Rejected",
                    Sector = userOrg.Organisation.SectorType,
                    Organisation = userOrg.Organisation.OrganisationName,
                    CompanyNo = userOrg.Organisation.CompanyNumber,
                    Address = userOrg?.Address.GetAddressString(),
                    SicCodes = userOrg.Organisation.GetSicCodeIdsString(),
                    UserFirstname = userOrg.User.Firstname,
                    UserLastname = userOrg.User.Lastname,
                    UserJobtitle = userOrg.User.JobTitle,
                    UserEmail = userOrg.User.EmailAddress,
                    userOrg.User.ContactFirstName,
                    userOrg.User.ContactLastName,
                    userOrg.User.ContactJobTitle,
                    userOrg.User.ContactOrganisation,
                    userOrg.User.ContactPhoneNumber
                },
                User);

            //Delete address for this user and organisation
            if (userOrg.Address.Status != AddressStatuses.Active && userOrg.Address.CreatedByUserId == userOrg.UserId)
            {
                DataRepository.Delete(userOrg.Address);
            }

            //Delete the org user
            long orgId = userOrg.OrganisationId;
            string emailAddress = userOrg.User.ContactEmailAddress.Coalesce(userOrg.User.EmailAddress);

            //Delete the organisation if it has no returns, is not in scopes table, and is not registered to another user
            if (userOrg.Organisation != null
                && !userOrg.Organisation.Returns.Any()
                && !userOrg.Organisation.OrganisationScopes.Any()
                && !await DataRepository.GetAll<UserOrganisation>()
                    .AnyAsync(uo => uo.OrganisationId == userOrg.Organisation.OrganisationId && uo.UserId != userOrg.UserId))
            {
                CustomLogger.Information(
                    $"Unused organisation {userOrg.OrganisationId}:'{userOrg.Organisation.OrganisationName}' deleted by {(OriginalUser == null ? currentUser.EmailAddress : OriginalUser.EmailAddress)} when declining manual registration for {userOrg.User.EmailAddress}");
                DataRepository.Delete(userOrg.Organisation);
            }

            EmployerSearchModel searchRecord = userOrg.Organisation.ToEmployerSearchResult(true);
            DataRepository.Delete(userOrg);

            //Send the declined email to the applicant
            SendRegistrationDeclined(
                emailAddress,
                string.IsNullOrWhiteSpace(model.CancellationReason)
                    ? "We haven't been able to verify your employer's identity. So we have declined your application."
                    : model.CancellationReason);

            //Save the changes and redirect
            DataRepository.SaveChanges();

            //Save the model for the redirect
            this.StashModel(model);

            //If private sector then send the pin
            return RedirectToAction("RequestCancelled");
        }


        //Send the registration request
        protected void SendRegistrationDeclined(string emailAddress, string reason)
        {
            //Send a verification link to the email address
            emailSendingService.SendOrganisationRegistrationDeclinedEmail(emailAddress, reason);
        }

        /// <summary>
        ///     Show review accepted confirmation
        ///     <returns></returns>
        [Authorize(Roles = LoginRoles.GpgAdmin)]
        [HttpGet("request-accepted")]
        public IActionResult RequestAccepted()
        {
            //Ensure user has completed the registration process
            User currentUser;
            IActionResult checkResult = CheckUserRegisteredOk(out currentUser);
            if (checkResult != null)
            {
                return checkResult;
            }

            //Make sure we can load model from session
            var model = this.UnstashModel<OrganisationViewModel>();
            if (model == null)
            {
                return View("CustomError", new ErrorViewModel(1112));
            }

            //Clear the stash
            this.ClearStash();

            return View("RequestAccepted", model);
        }

        /// <summary>
        ///     Show review cancel confirmation
        ///     <returns></returns>
        [Authorize(Roles = LoginRoles.GpgAdmin)]
        [HttpGet("request-cancelled")]
        public IActionResult RequestCancelled()
        {
            //Ensure user has completed the registration process
            User currentUser;
            IActionResult checkResult = CheckUserRegisteredOk(out currentUser);
            if (checkResult != null)
            {
                return checkResult;
            }

            //Make sure we can load model from session
            var model = this.UnstashModel<OrganisationViewModel>();
            if (model == null)
            {
                return View("CustomError", new ErrorViewModel(1112));
            }

            //Clear the stash
            this.ClearStash();

            return View("RequestCancelled", model);
        }

        #endregion

    }
}
