﻿using System.Collections.Generic;
using GenderPayGap.Core.Classes;
using GenderPayGap.Core.Interfaces;
using GenderPayGap.Extensions.AspNetCore;

namespace GenderPayGap.WebJob.Services
{
    public class EmailSendingService
    {

        private readonly IGovNotifyAPI govNotifyApi;

        public EmailSendingService(IGovNotifyAPI govNotifyApi)
        {
            this.govNotifyApi = govNotifyApi;
        }

        public void SendReminderEmail(
            string emailAddress,
            string deadlineDate,
            int daysUntilDeadline,
            string organisationNames,
            bool organisationIsSingular,
            bool organisationIsPlural,
            string sectorType)
        {
            var personalisation = new Dictionary<string, dynamic>
            {
                {"DeadlineDate", deadlineDate},
                {"DaysUntilDeadline", daysUntilDeadline},
                {"OrganisationNames", organisationNames},
                {"OrganisationIsSingular", organisationIsSingular},
                {"OrganisationIsPlural", organisationIsPlural},
                {"SectorType", sectorType},
                {"Environment", Config.IsProduction() ? "" : $"[{Config.EnvironmentName}] "}
            };

            var notifyEmail = new NotifyEmail
            {
                EmailAddress = emailAddress,
                TemplateId = EmailTemplates.ReminderEmail,
                Personalisation = personalisation
            };

            SendEmail(notifyEmail);
        }

        public void SendGeoSiteCertificateSoonToExpireEmail(string emailAddress, string host, string expiryDate, string remainingDays)
        {
            var personalisation = new Dictionary<string, dynamic>
            {
                {"host", host},
                {"expiryDate", expiryDate},
                {"remainingDays", remainingDays},
                {"Environment", Config.IsProduction() ? "" : $"[{Config.EnvironmentName}] "}
            };

            var notifyEmail = new NotifyEmail
            {
                EmailAddress = emailAddress,
                TemplateId = EmailTemplates.GeoSiteCertificateSoonToExpireEmail,
                Personalisation = personalisation
            };

            SendEmail(notifyEmail);
        }

        public void SendGeoSiteCertificateExpiredEmail(string emailAddress, string host, string expiryDate)
        {
            var personalisation = new Dictionary<string, dynamic>
            {
                {"host", host}, {"expiryDate", expiryDate}, {"Environment", Config.IsProduction() ? "" : $"[{Config.EnvironmentName}] "}
            };

            var notifyEmail = new NotifyEmail
            {
                EmailAddress = emailAddress,
                TemplateId = EmailTemplates.GeoSiteCertificateExpiredEmail,
                Personalisation = personalisation
            };

            SendEmail(notifyEmail);
        }

        public void SendEmailFromQueue(NotifyEmail notifyEmail)
        {
            SendEmail(notifyEmail);
        }

        private void SendEmail(NotifyEmail notifyEmail)
        {
            govNotifyApi.SendEmail(notifyEmail);
        }

    }

    public static class EmailTemplates
    {

        public const string ReminderEmail = "db15432c-9eda-4df4-ac67-290c7232c546";
        public const string GeoSiteCertificateSoonToExpireEmail = "f05abb4f-55b3-472c-8c18-b568b6f2b4c8";
        public const string GeoSiteCertificateExpiredEmail = "a928f9e7-962c-447f-a19e-466cfbe61740";

    }

}