﻿using GenderPayGap.Database.Models;
using GovUkDesignSystem;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GenderPayGap.WebUI.Models.Report
{
    public class ReportReviewAndSubmitViewModel : GovUkViewModel
    {

        [BindNever /* Output Only - only used for sending data from the Controller to the View */]
        public Database.Organisation Organisation { get; set; }

        [BindNever /* Output Only - only used for sending data from the Controller to the View */]
        public int ReportingYear { get; set; }

        [BindNever /* Output Only - only used for sending data from the Controller to the View */]
        public bool IsEditingSubmittedReturn { get; set; }

        [BindNever /* Output Only - only used for sending data from the Controller to the View */]
        public DraftReturn DraftReturn { get; set; }

        [BindNever /* Output Only - only used for sending data from the Controller to the View */]
        public bool WillBeLateSubmission { get; set; }

    }
}
