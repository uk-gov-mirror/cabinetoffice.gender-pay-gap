﻿using GenderPayGap.Database;
using GovUkDesignSystem;
using GovUkDesignSystem.Attributes.ValidationAttributes;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GenderPayGap.WebUI.Models.Admin
{
    public class AdminResendVerificationEmailViewModel : GovUkViewModel
    {
        [BindNever /* Output Only - only used for sending data from the Controller to the View */]
        public User User { get; set; }

        [GovUkValidateRequired(ErrorMessageIfMissing = "Please enter a reason for this change.")]
        public string Reason { get; set; }

        [BindNever /* Output Only - only used for sending data from the Controller to the View */]
        public object OtherErrorMessagePlaceholder { get; set; }
    }
}
