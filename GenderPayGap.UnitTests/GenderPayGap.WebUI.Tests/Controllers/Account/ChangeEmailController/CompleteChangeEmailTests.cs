﻿using System.Threading.Tasks;
using GenderPayGap.Core.Classes;
using GenderPayGap.Core.Interfaces;
using GenderPayGap.Core.Models;
using GenderPayGap.Database;
using GenderPayGap.Extensions;
using GenderPayGap.Tests.TestHelpers;
using GenderPayGap.WebUI;
using GenderPayGap.WebUI.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Moq;
using NUnit.Framework;

namespace Account.Controllers.ChangeEmailController
{

    public class CompleteChangeEmailAsyncTests
    {

        private RouteData mockRouteData;

        [SetUp]
        public void BeforeEach()
        {
            mockRouteData = new RouteData();
            mockRouteData.Values.Add(
                "Action",
                nameof(GenderPayGap.WebUI.Areas.Account.Controllers.ChangeEmailController.CompleteChangeEmailAsync));
            mockRouteData.Values.Add("Controller", "ChangeEmail");
        }

        [Test]
        public async Task FailsWhenTokenExpired()
        {
            // Arrange
            var testNewEmail = "new@testemail.com";
            User verifiedUser = UserHelper.GetNotAdminUserWithVerifiedEmailAddress();
            var controller =
                UiTestHelper.GetController<GenderPayGap.WebUI.Areas.Account.Controllers.ChangeEmailController>(
                    verifiedUser.UserId,
                    mockRouteData,
                    verifiedUser);
            string expectedCurrentEmailAddress = verifiedUser.EmailAddress;

            string code = Encryption.EncryptModel(
                new ChangeEmailVerificationToken {
                    UserId = verifiedUser.UserId, NewEmailAddress = testNewEmail, TokenTimestamp = VirtualDateTime.Now.AddDays(-1)
                });

            // Act
            var viewResult = await controller.CompleteChangeEmailAsync(code) as ViewResult;

            // Assert
            Assert.NotNull(viewResult);
            Assert.AreEqual("ChangeEmailFailed", viewResult.ViewName);
            Assert.AreEqual(1, viewResult.ViewData.ModelState.ErrorCount);
            Assert.AreEqual(
                "Cannot complete the change email process because your verify url has expired.",
                viewResult.ViewData.ModelState[nameof(controller.CompleteChangeEmailAsync)].Errors[0].ErrorMessage);

            Assert.AreEqual(expectedCurrentEmailAddress, verifiedUser.EmailAddress, "Expected the email address not to change");
        }

        [Test]
        public async Task FailsWhenEmailOwnedByNewOrActiveUser()
        {
            // Arrange
            User verifiedUser = UserHelper.GetNotAdminUserWithVerifiedEmailAddress();
            User existingUser = UserHelper.GetNotAdminUserWithVerifiedEmailAddress();
            string testNewEmail = existingUser.EmailAddress;

            var controller = UiTestHelper.GetController<GenderPayGap.WebUI.Areas.Account.Controllers.ChangeEmailController>(
                verifiedUser.UserId,
                mockRouteData,
                verifiedUser,
                existingUser);

            string expectedCurrentEmailAddress = verifiedUser.EmailAddress;

            string code = Encryption.EncryptModel(
                new ChangeEmailVerificationToken {
                    UserId = verifiedUser.UserId, NewEmailAddress = testNewEmail, TokenTimestamp = VirtualDateTime.Now
                });

            // Act
            var viewResult = await controller.CompleteChangeEmailAsync(code) as ViewResult;

            // Assert
            Assert.NotNull(viewResult);
            Assert.AreEqual("ChangeEmailFailed", viewResult.ViewName);
            Assert.AreEqual(1, viewResult.ViewData.ModelState.ErrorCount);
            Assert.AreEqual(
                "Cannot complete the change email process because the new email address has been registered since this change was requested.",
                viewResult.ViewData.ModelState[nameof(controller.CompleteChangeEmailAsync)].Errors[0].ErrorMessage);

            Assert.AreEqual(expectedCurrentEmailAddress, verifiedUser.EmailAddress, "Expected the email address not to change");
        }

        [Test]
        public async Task UpdatesUserEmailAddress()
        {
            // Arrange
            var testNewEmail = "new@testemail.com";
            User verifiedUser = UserHelper.GetNotAdminUserWithVerifiedEmailAddress();
            var controller =
                UiTestHelper.GetController<GenderPayGap.WebUI.Areas.Account.Controllers.ChangeEmailController>(
                    verifiedUser.UserId,
                    mockRouteData,
                    verifiedUser);

            string code = Encryption.EncryptModel(
                new ChangeEmailVerificationToken {
                    UserId = verifiedUser.UserId, NewEmailAddress = testNewEmail, TokenTimestamp = VirtualDateTime.Now
                });

            // Act
            var viewResult = await controller.CompleteChangeEmailAsync(code) as ViewResult;

            // Assert
            Assert.NotNull(viewResult);
            Assert.AreEqual("ChangeEmailCompleted", viewResult.ViewName);

            Assert.AreEqual(testNewEmail, verifiedUser.EmailAddress, "Expected new email address to be saved");
        }

        [Test]
        public async Task SendsChangeEmailToOldAndNewEmailAddresses()
        {
            // Arrange
            var calledSendEmailQueue = false;
            var testNewEmail = "new@testemail.com";
            User verifiedUser = UserHelper.GetNotAdminUserWithVerifiedEmailAddress();
            var controller =
                UiTestHelper.GetController<GenderPayGap.WebUI.Areas.Account.Controllers.ChangeEmailController>(
                    verifiedUser.UserId,
                    mockRouteData,
                    verifiedUser);
            string testOldEmail = verifiedUser.EmailAddress;

            string code = Encryption.EncryptModel(
                new ChangeEmailVerificationToken {
                    UserId = verifiedUser.UserId, NewEmailAddress = testNewEmail, TokenTimestamp = VirtualDateTime.Now
                });

            var mockEmailQueue = new Mock<IQueue>();
            Program.MvcApplication.SendEmailQueue = mockEmailQueue.Object;
            mockEmailQueue
                .Setup(q => q.AddMessageAsync(It.IsAny<QueueWrapper>()))
                .Callback(
                    (QueueWrapper inst) => {
                        calledSendEmailQueue = true;

                        if (inst.Type == typeof(ChangeEmailCompletedNotificationTemplate).FullName)
                        {
                            Assert.IsTrue(
                                inst.Message.Contains(testOldEmail),
                                "Expected the users old email address to be in the email send queue");
                        }
                        else
                        {
                            Assert.Fail("Expected new and old emails to be queued");
                        }
                    });

            // Act
            var viewResult = await controller.CompleteChangeEmailAsync(code) as ViewResult;

            // Assert
            Assert.IsTrue(calledSendEmailQueue, "Expected send email queue to be called");
            Assert.NotNull(viewResult);
            Assert.AreEqual("ChangeEmailCompleted", viewResult.ViewName);
        }

    }

}
