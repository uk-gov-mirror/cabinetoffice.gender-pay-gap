﻿using System;
using GenderPayGap.Core;
using GenderPayGap.Core.Interfaces;
using GenderPayGap.Database;
using GenderPayGap.WebUI.BusinessLogic.Abstractions;
using GenderPayGap.WebUI.Classes;
using GenderPayGap.WebUI.Helpers;
using GenderPayGap.WebUI.Models.Admin;
using GovUkDesignSystem;
using GovUkDesignSystem.Parsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GenderPayGap.WebUI.Controllers.Admin
{
    [Authorize(Roles = LoginRoles.GpgAdmin)]
    [Route("admin")]
    public class AdminImpersonateUserController : Controller
    {

        private readonly IUserRepository userRepository;
        private readonly IDataRepository dataRepository;

        public AdminImpersonateUserController(IUserRepository userRepository, IDataRepository dataRepository)
        {
            this.userRepository = userRepository;
            this.dataRepository = dataRepository;
        }

        [HttpGet("impersonate")]
        public IActionResult Impersonate(string emailAddress)
        {

            var viewModel = new AdminImpersonateUserViewModel {EmailAddress = emailAddress};

            return View("ImpersonateUser", viewModel);
        }

        [HttpPost("impersonate")]
        [PreventDuplicatePost]
        [ValidateAntiForgeryToken]
        public IActionResult ImpersonatePost(AdminImpersonateUserViewModel viewModel)
        {
            viewModel.ParseAndValidateParameters(Request, m => m.EmailAddress);
            
            if (viewModel.HasAnyErrors())
            {
                return View("ImpersonateUser", viewModel);
            }

            // find the latest active user by email
            User impersonatedUser = userRepository.FindByEmail(viewModel.EmailAddress, UserStatuses.Active);
            if (impersonatedUser == null)
            {
                viewModel.AddErrorFor(m => m.EmailAddress, "This user does not exist");
                return View("ImpersonateUser");
            }

            if (impersonatedUser.IsAdministrator())
            {
                viewModel.AddErrorFor(m => m.EmailAddress, "Impersonating other administrators is not permitted");
                return View("ImpersonateUser");
            }

            User currentUser = ControllerHelper.GetGpgUserFromAspNetUser(User, dataRepository);

            LoginHelper.LoginWithImpersonation(
                HttpContext,
                impersonatedUser.UserId,
                LoginRoles.GpgEmployer,
                currentUser.UserId);

            return RedirectToAction("ManageOrganisationsGet", "ManageOrganisations");
        }

        [HttpPost("impersonate/{userId}")]
        [PreventDuplicatePost]
        [ValidateAntiForgeryToken]
        public IActionResult ImpersonateDirectPost(long userId)
        {
            User impersonatedUser = dataRepository.Get<User>(userId);
            if (impersonatedUser == null)
            {
                throw new Exception($"Trying to impersonate user ({userId}) but this user does not exist");
            }

            if (impersonatedUser.IsAdministrator())
            {
                throw new Exception($"Trying to impersonate user ({userId}) but this user is an administrator");
            }

            User currentUser = ControllerHelper.GetGpgUserFromAspNetUser(User, dataRepository);

            LoginHelper.LoginWithImpersonation(
                HttpContext,
                impersonatedUser.UserId,
                LoginRoles.GpgEmployer,
                currentUser.UserId);

            return RedirectToAction("ManageOrganisationsGet", "ManageOrganisations");
        }

    }
}
