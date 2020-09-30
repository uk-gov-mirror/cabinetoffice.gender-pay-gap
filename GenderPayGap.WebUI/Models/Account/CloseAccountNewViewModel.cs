﻿using GenderPayGap.Database;
using GovUkDesignSystem;
using GovUkDesignSystem.Attributes.ValidationAttributes;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GenderPayGap.WebUI.Models.Account
{
    public class CloseAccountNewViewModel : GovUkViewModel
    {

        [GovUkValidateRequired(ErrorMessageIfMissing = "Enter your email address")]
        public string Password { get; set; }
        
        [BindNever]
        public User User { get; set; }

    }
}
