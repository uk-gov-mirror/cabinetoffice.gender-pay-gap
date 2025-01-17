﻿using System;
using System.Collections.Generic;
using GenderPayGap.Core;
using GenderPayGap.Database;
using GenderPayGap.Extensions;
using GenderPayGap.WebUI.Controllers.Account;
using GenderPayGap.WebUI.ExternalServices;
using GenderPayGap.WebUI.Models.AccountCreation;
using GenderPayGap.WebUI.Services;
using GenderPayGap.WebUI.Tests.Builders;
using GenderPayGap.WebUI.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;
using NUnit.Framework;

namespace GenderPayGap.WebUI.Tests.Controllers.Account
{
    [TestFixture]
    [SetCulture("en-GB")]
    public class AccountCreationTests
    {
        
        [Test]
        [Description("POST: Verification email is sent after creating a user account")]
        public void POST_Verification_Email_Is_Sent_After_Creating_User_Account()
        {
            // Arrange
            var requestFormValues = new Dictionary<string, StringValues>();
            requestFormValues.Add("GovUk_Text_EmailAddress", "test@example.com");
            requestFormValues.Add("GovUk_Text_ConfirmEmailAddress", "test@example.com");
            requestFormValues.Add("GovUk_Text_FirstName", "Test");
            requestFormValues.Add("GovUk_Text_LastName", "Example");
            requestFormValues.Add("GovUk_Text_JobTitle", "Tester");
            requestFormValues.Add("GovUk_Text_Password", "Pa55word");
            requestFormValues.Add("GovUk_Text_ConfirmPassword", "Pa55word");
            requestFormValues.Add("GovUk_Checkbox_SendUpdates", "true");
            requestFormValues.Add("GovUk_Checkbox_AllowContact", "false");

            var controllerBuilder = new ControllerBuilder<AccountCreationController>();
            var controller = controllerBuilder
                .WithRequestFormValues(requestFormValues)
                .WithMockUriHelper()
                .Build();

            // Act
            var response = (ViewResult) controller.CreateUserAccountPost(new CreateUserAccountViewModel());

            // Assert
            Assert.AreEqual("ConfirmEmailAddress", response.ViewName);

            Assert.AreEqual(1, controllerBuilder.EmailsSent.Count);
            NotifyEmail emailSent = controllerBuilder.EmailsSent[0];

            Assert.AreEqual("test@example.com", emailSent.EmailAddress);
            Assert.AreEqual(EmailTemplates.AccountVerificationEmail, emailSent.TemplateId);
        }
        
        [Test]
        [Description("GET: Clicking link in verification email confirms user")]
        public void GET_Clicking_Link_In_Verification_Email_Confirms_User()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                EmailAddress = "test@example.com",
                Firstname = "Test",
                Lastname = "Example",
                EmailVerifySendDate = VirtualDateTime.Now,
                EmailVerifyHash = Guid.NewGuid().ToString("N"),
                Status = UserStatuses.New
            };

            var controller = new ControllerBuilder<AccountCreationController>()
                .WithDatabaseObjects(user)
                .Build();

            // Act
            var response = (RedirectToActionResult) controller.VerifyEmail(user.EmailVerifyHash);

            // Assert
            Assert.AreEqual("AccountCreationConfirmation", response.ActionName);
            Assert.AreEqual(user.Status, UserStatuses.Active);
        }

    }

}
