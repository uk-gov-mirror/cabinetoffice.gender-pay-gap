﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using GenderPayGap.Core;
using GenderPayGap.Core.Interfaces;
using GenderPayGap.Database.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GenderPayGap.WebUI.Controllers
{
    [Authorize(Roles = "GPGadmin")]
    [Route("admin")]
    public class AdminFeedbackController : Controller
    {

        private readonly IDataRepository dataRepository;

        public AdminFeedbackController(IDataRepository dataRepository)
        {
            this.dataRepository = dataRepository;
        }

        [HttpGet("feedback")]
        public IActionResult ViewFeedback()
        {
            List<Feedback> feedback = dataRepository.GetAll<Feedback>().ToList();

            return View("ViewFeedback", feedback);
        }

        [HttpPost("feedback")]
        public IActionResult CategoriseFeedback(long feedbackId, FeedbackStatus status)
        {
            Feedback feedback = dataRepository.Get<Feedback>(feedbackId);

            feedback.FeedbackStatus = status;

            dataRepository.SaveChangesAsync().Wait();

            return RedirectToAction("ViewFeedback", "AdminFeedback");
        }

        [HttpGet("feedback/download")]
        public FileContentResult DownloadFeedback(bool nonSpamOnly = false)
        {
            var feedback = dataRepository.GetAll<Feedback>();
            if (nonSpamOnly)
            {
                feedback = feedback.Where(f => f.FeedbackStatus == FeedbackStatus.NotSpam);
            }

            var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream))
            {
                using (var csv = new CsvWriter(writer))
                {
                    csv.WriteRecords(feedback.ToList());
                }
            }

            var fileContentResult = new FileContentResult(memoryStream.GetBuffer(), "text/csv") {FileDownloadName = "Feedback.csv"};

            return fileContentResult;
        }

    }
}