﻿using GenderPayGap.Core;
using GenderPayGap.Core.Interfaces;
using GenderPayGap.WebUI.Helpers;
using GenderPayGap.WebUI.Models.AddOrganisation;
using GovUkDesignSystem.Parsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GenderPayGap.WebUI.Controllers.AddOrganisation
{
    [Authorize(Roles = LoginRoles.GpgEmployer)]
    [Route("add-employer")]
    public class AddOrganisationChooseSectorController : Controller
    {
        
        private readonly IDataRepository dataRepository;

        public AddOrganisationChooseSectorController(IDataRepository dataRepository)
        {
            this.dataRepository = dataRepository;
        }


        [HttpGet("choose-employer-type")]
        public IActionResult ChooseSector(AddOrganisationChooseSectorViewModel viewModel)
        {
            ControllerHelper.ThrowIfUserAccountRetiredOrEmailNotVerified(User, dataRepository);

            if (viewModel.Validate == true)
            {
                viewModel.ParseAndValidateParameters(Request, m => m.Sector);

                if (viewModel.HasAnyErrors())
                {
                    return View("ChooseSector", viewModel);
                }

                return RedirectToAction("Search", "AddOrganisationSearch",
                    new { Sector = viewModel.Sector.ToString().ToLower() });
            }

            return View("ChooseSector", viewModel);
        }

    }
}
