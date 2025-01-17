﻿using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using GenderPayGap.Core.Models;
using GenderPayGap.Extensions;
using GenderPayGap.Extensions.AspNetCore;
using Newtonsoft.Json;

namespace GenderPayGap.Core
{
    public static class Global
    {

        public static IContainer ContainerIoC;


        #region Secrets / connection strings / API keys

        public static string DatabaseConnectionString
        {
            get
            {
                if (Global.VcapServices != null && Global.VcapServices.Postgres != null)
                {
                    VcapPostgres postgresConfiguration = Global.VcapServices.Postgres.First(b => b.Name.EndsWith("-db"));
                    return postgresConfiguration.GetConnectionString();
                }
                else
                {
                    return Config.GetConnectionString("GpgDatabase");
                }
            }
        }

        public static string CompaniesHouseApiKey => Config.GetAppSetting("CompaniesHouseApiKey");
        public static string GovUkNotifyApiKey => Config.GetAppSetting("GovUkNotifyApiKey");
        public static int ObfuscationSeed => Config.GetAppSetting("ObfuscationSeed").ToInt32(127);
        public static string DefaultEncryptionKey => Config.GetAppSetting("DefaultEncryptionKey");
        public static string AdminEmails => Config.GetAppSetting("AdminEmails");
        public static VcapServices VcapServices =>
            Config.GetAppSetting("VCAP_SERVICES") != null
                ? JsonConvert.DeserializeObject<VcapServices>(Config.GetAppSetting("VCAP_SERVICES"))
                : null;
        public static string EhrcIPRange => Config.GetAppSetting("EhrcIPRange");
        public static string GpgAnalysisAppApiPassword => Config.GetAppSetting("GpgAnalysisAppApiPassword");
        public static string DataMigrationPassword => Config.GetAppSetting("DataMigrationPassword");
        public static string BasicAuthUsername => Config.GetAppSetting("BasicAuthUsername");
        public static string BasicAuthPassword => Config.GetAppSetting("BasicAuthPassword");

        #endregion



        #region Settings that we expect to want to update at short notice

        public static bool MaintenanceMode => Config.GetAppSetting("MaintenanceMode").ToBoolean(false);
        public static DateTime? MaintenanceModeUpAgainTime => Config.GetAppSetting("MaintenanceModeUpAgainTime")?.ToDateTime();
        public static List<int> ReportingStartYearsToExcludeFromLateFlagEnforcement =>
            JsonConvert.DeserializeObject<List<int>>(Config.GetAppSetting("ReportingStartYearsToExcludeFromLateFlagEnforcement", "[]"));
        public static string ReminderEmailDays => Config.GetAppSetting("ReminderEmailDays");
        public static bool EnableSubmitAlerts
        {
            get => Config.GetAppSetting("EnableSubmitAlerts").ToBoolean(false);
            set => Config.SetAppSetting("EnableSubmitAlerts", value.ToString());
        }
        public static bool DisableSearchCache => Config.GetAppSetting("DisableSearchCache").ToBoolean();

        #endregion



        #region Settings that only change per environment
        public static bool SkipSpamProtection
        {
            get => Config.GetAppSetting("TESTING-SkipSpamProtection").ToBoolean();
            set => Config.SetAppSetting("TESTING-SkipSpamProtection", value.ToString());
        }
        public static bool SendGoogleAnalyticsDataToGovUk => Config.GetAppSetting("SendGoogleAnalyticsDataToGovUk").ToBoolean();
        public static int MaxNumCallsCompaniesHouseApiPerFiveMins => Config.GetAppSetting("MaxNumCallsCompaniesHouseApiPerFiveMins").ToInt32(10);

        public static string GoogleAnalyticsAccountId => Config.GetAppSetting("GoogleAnalyticsAccountId");
        public static List<string> GeoDistributionList => Config.GetAppSetting("GEODistributionList").Split(";", StringSplitOptions.RemoveEmptyEntries).ToList();
        public static TimeSpan TimeToKeepBackupFiles => TimeSpan.FromDays(Config.GetAppSetting("DaysToKeepBackupFiles").ToInt32(35));
        #endregion

        #region Settings that change per deployment slot
        public static bool BackgroundJobsEnabled => Config.GetAppSetting("WEBJOBS_STOPPED").ToInt32(0) == 0;
        #endregion



        #region Settings that change rarely / we only expect to change alongside a code change, but might be used in lots of places (could be constants)

        public static string GpgReportingEmail => "gpg.reporting@cabinetoffice.gov.uk";
        public static string DataControllerEmail => "publiccorrespondence@cabinetoffice.gov.uk";
        public static string DataProtectionOfficerEmail => "dpo@cabinetoffice.gov.uk";
        public static string InformationCommissionerEmail => "casework@ico.org.uk";
        public static string TestEnvironmentEmailRecipient => "genderpaygaptesting@gmail.com";
        public static int StaticCacheSeconds => 86400;
        public static DateTime PrivacyChangedDate => DateTime.Parse("2019-09-23");
        public static DateTime PrivateAccountingDate => DateTime.Parse("2017-04-05");
        public static DateTime PublicAccountingDate => DateTime.Parse("2017-03-31");
        public static int EmailVerificationExpiryDays => 7;
        public static int EmailVerificationMinResendHours => 1;
        public static int LevenshteinDistance => 2;
        public static int LockoutMinutes => 30;
        public static int MaxLoginAttempts => 5;
        public static int MaxPinAttempts => 3;
        public static int MinPasswordResetMinutes => 10;
        public static TimeSpan PasswordResetCodeExpiryDays => TimeSpan.FromDays(1);
        public static int PinInPostExpiryDays => 21;
        public static int PinInPostMinRepostDays => 5;
        public static int PurgeUnusedOrganisationDays => 30;
        public static int PurgeUnconfirmedPinDays => PinInPostExpiryDays + 30;
        public static string DownloadsLocation => "Downloads";
        public static string EmployerCodeChars => "123456789ABCDEFGHKLMNPQRSTUXYZ";
        public static string PINChars => "123456789ABCDEF";
        public static string WhoNeedsToReportGuidanceLink => "https://www.gov.uk/guidance/gender-pay-gap-who-needs-to-report";
        public static int FirstReportingYear => 2017;
        public static int MinIOThreads => 300;
        public static int MinWorkerThreads => 300;
        public static string StartUrl =>
            Config.GetAppSetting("UseStartUrl").ToBoolean()
                ? "https://www.gov.uk/report-gender-pay-gap-data"
                : null;
        public static string DoneUrl => "https://www.gov.uk/done/report-gender-pay-gap-data";
        public static int ShowReportYearCount => 10; // Specifies how many reporting years the public can view or compare
        public static int MaxCompareBasketCount => 500; // Maximum number of employers you can add to the compare basket
        public static int MaxCompareBasketShareCount => 195; // Maximum number of employers you can share in a mailto: protocol
        public static int EditableReportCount => 4; // Specifies how many reports an employer can edit
        public static int EditableScopeCount => 2; // Specifies how many scopes an employer can edit
        public static Dictionary<string, string> SecurityHeaders =>
            new Dictionary<string, string>
            {
                {"X-Content-Type-Options", "nosniff"},
                {"X-Frame-Options", "DENY"},
                {"X-Permitted-Cross-Domain-Policies", "master-only"},
                {"X-Xss-Protection", "1; mode=block;"},
                {"Content-Security-Policy", "frame-ancestors 'none'"},
                {"X-Content-Security-Policy", "$(Content-Security-Policy)"},
                {"Referrer-Policy", "origin-when-cross-origin"},
                {"Strict-Transport-Security", "max-age=31536000; includeSubDomains"},
                {"X-Powered-By", ""},
                {"X-AspNet-Version", ""},
                {"X-AspNetMvc-Version", ""},
                {"Server", ""}
            };

        public static string GpgGuidanceLink = "https://www.gov.uk/government/collections/gender-pay-gap-reporting";

        #endregion

    }
}
