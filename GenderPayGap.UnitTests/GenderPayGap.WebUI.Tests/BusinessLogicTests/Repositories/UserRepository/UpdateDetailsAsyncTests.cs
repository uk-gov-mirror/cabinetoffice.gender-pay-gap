﻿using System;
using System.Threading.Tasks;
using AutoMapper;
using GenderPayGap.BusinessLogic.Account.Abstractions;
using GenderPayGap.BusinessLogic.Account.Models;
using GenderPayGap.BusinessLogic.Services;
using GenderPayGap.Core;
using GenderPayGap.Core.Interfaces;
using GenderPayGap.Database;
using GenderPayGap.Extensions.AspNetCore;
using GenderPayGap.Tests.Common.Classes;
using GenderPayGap.Tests.Common.TestHelpers;
using Moq;
using NUnit.Framework;

namespace Repositories.UserRepository
{

    [TestFixture]
    [SetCulture("en-GB")]
    public class UpdateDetailsAsyncTests
    {

        [SetUp]
        public void BeforeEach()
        {
            // init mapper
            Mapper.Reset();
            Mapper.Initialize(config => config.AddMaps(typeof(GenderPayGap.BusinessLogic.Account.Repositories.UserRepository).Assembly));

            // mock data 
            mockDataRepo = new Mock<IDataRepository>().SetupGetAll(UserHelpers.CreateUsers());

            var auditLoggerWithMocks = new AuditLogger(Mock.Of<IDataRepository>(), Mock.Of<IHttpSession>());

            // service under test
            testUserRepo =
                new GenderPayGap.BusinessLogic.Account.Repositories.UserRepository(
                    mockDataRepo.Object,
                    auditLoggerWithMocks);
        }

        private Mock<IDataRepository> mockDataRepo;
        private IUserRepository testUserRepo;

        [TestCase]
        public async Task SavesNewDetails()
        {
            // Arrange
            var saveChangesCalled = false;
            User testUserToUpdate = await testUserRepo.FindByEmailAsync("active1@ad5bda75-e514-491b-b74d-4672542cbd15.com");

            var testUserDetails = new UpdateDetailsModel {
                FirstName = "NewFirstName",
                LastName = "NewLastName",
                JobTitle = "NewJobTitle",
                ContactPhoneNumber = "NewContactPhoneNumber",
                AllowContact = !testUserToUpdate.AllowContact,
                SendUpdates = !testUserToUpdate.SendUpdates
            };

            mockDataRepo.Setup(x => x.SaveChangesAsync())
                .Callback(() => saveChangesCalled = true)
                .Returns(Task.CompletedTask);

            // Act
            testUserRepo.UpdateDetails(testUserToUpdate, testUserDetails);

            // Assert
            Assert.IsTrue(saveChangesCalled, "Expected SaveChangesAsync to be called");
            testUserDetails.Compare(testUserToUpdate);
        }

        [TestCase("active1@ad5bda75-e514-491b-b74d-4672542cbd15.com", UserStatuses.New)]
        [TestCase("active1@ad5bda75-e514-491b-b74d-4672542cbd15.com", UserStatuses.Unknown)]
        [TestCase("active1@ad5bda75-e514-491b-b74d-4672542cbd15.com", UserStatuses.Retired)]
        public async Task ThrowsErrorWhenUserStatusIsNotActive(string testCurrentEmail, UserStatuses testStatus)
        {
            // Arrange
            User testUserToUpdate = await testUserRepo.FindByEmailAsync(testCurrentEmail);
            testUserToUpdate.Status = testStatus;

            // Act
            var actualException = Assert.Throws<ArgumentException>(
                () => testUserRepo.UpdateDetails(testUserToUpdate, new UpdateDetailsModel()));

            // Assert
            Assert.AreEqual(
                "Can only update details for active users. UserId=23322",
                actualException.Message,
                "Expected exception message to match");
            Assert.AreEqual(testStatus, testUserToUpdate.Status, "Expected status to still be the same");
        }

        private static object[] ThrowsErrorWhenArgumentIsNullCases = {
            new object[] {null, null, "Value cannot be null.\r\nParameter name: userToUpdate"},
            new object[] {new User(), null, "Value cannot be null.\r\nParameter name: changeDetails"}
        };

        [TestCaseSource(nameof(ThrowsErrorWhenArgumentIsNullCases))]
        public void ThrowsErrorWhenArgumentIsNull(User testUserToUpdateArg,
            UpdateDetailsModel testChangeDetailsArg,
            string expectedErrorMessage)
        {
            // Act
            var actualException = Assert.Throws<ArgumentNullException>(
                () => testUserRepo.UpdateDetails(testUserToUpdateArg, testChangeDetailsArg));

            // Assert
            Assert.AreEqual(expectedErrorMessage, actualException.Message, "Expected exception message to match");
        }

    }

}