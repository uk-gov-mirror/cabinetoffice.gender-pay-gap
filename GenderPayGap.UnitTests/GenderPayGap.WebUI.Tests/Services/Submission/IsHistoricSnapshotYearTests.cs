﻿using System;
using GenderPayGap.Core;
using GenderPayGap.Core.Classes;
using GenderPayGap.Core.Interfaces;
using GenderPayGap.Tests.Common.Classes;
using GenderPayGap.WebUI.BusinessLogic.Services;
using Moq;
using NUnit.Framework;

namespace GenderPayGap.Tests.Services.SubmissionService
{

    public class IsHistoricSnapshotYearTests
    {

        private Mock<IDataRepository> mockDataRepo;
        private Mock<IDraftFileBusinessLogic> mockDraftFileBL;
        private Mock<IScopeBusinessLogic> mockScopeBL;

        [SetUp]
        public void BeforeEach()
        {
            mockDataRepo = MoqHelpers.CreateMockDataRepository();
            mockScopeBL = new Mock<IScopeBusinessLogic>();
            mockDraftFileBL = new Mock<IDraftFileBusinessLogic>();
        }

        [TestCase(SectorTypes.Private, 2017)]
        [TestCase(SectorTypes.Public, 2017)]
        [TestCase(SectorTypes.Private, 2018)]
        [TestCase(SectorTypes.Public, 2018)]
        public void ReturnsTrueForHistoricYears(SectorTypes testSector, int testHistoricYear)
        {
            // Arrange
            DateTime testSnapshotDate = testSector.GetAccountingStartDate();
            var expectCalledGetSnapshotDate = false;

            // Mocks
            var mockService = new Mock<WebUI.Classes.Services.SubmissionService>(
                mockDataRepo.Object,
                mockScopeBL.Object,
                mockDraftFileBL.Object);
            mockService.CallBase = true;

            // Override GetPreviousReportingStartDate and return expectedYear
            mockService.Setup(ss => ss.GetSnapshotDate(It.IsIn(testSector), It.IsAny<int>()))
                .Returns(
                    () => {
                        expectCalledGetSnapshotDate = true;
                        return testSnapshotDate;
                    });

            // Assert
            WebUI.Classes.Services.SubmissionService testService = mockService.Object;
            bool actual = testService.IsHistoricSnapshotYear(testSector, testHistoricYear);

            Assert.IsTrue(expectCalledGetSnapshotDate, "Expected to call GetSnapshotDate");
            Assert.IsTrue(actual, "Expected IsHistoricSnapshotYear to return true");
        }

        [TestCase(SectorTypes.Private)]
        [TestCase(SectorTypes.Public)]
        public void ReturnsFalseForHistoricYears(SectorTypes testSector)
        {
            // Arrange
            DateTime testSnapshotDate = testSector.GetAccountingStartDate();
            int testHistoricYear = testSnapshotDate.Year;
            var expectCalledGetSnapshotDate = false;

            // Mocks
            var mockService = new Mock<WebUI.Classes.Services.SubmissionService>(
                mockDataRepo.Object,
                mockScopeBL.Object,
                mockDraftFileBL.Object);
            mockService.CallBase = true;

            // Override GetPreviousReportingStartDate and return expectedYear
            mockService.Setup(ss => ss.GetSnapshotDate(It.IsIn(testSector), It.IsAny<int>()))
                .Returns(
                    () => {
                        expectCalledGetSnapshotDate = true;
                        return testSnapshotDate;
                    });

            // Assert
            WebUI.Classes.Services.SubmissionService testService = mockService.Object;
            bool actual = testService.IsHistoricSnapshotYear(testSector, testHistoricYear);

            Assert.IsTrue(expectCalledGetSnapshotDate, "Expected to call GetSnapshotDate");
            Assert.IsFalse(actual, "Expected IsHistoricSnapshotYear to return false");
        }

    }

}
