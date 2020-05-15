using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Autofac.Features.AttributeFilters;
using CsvHelper;
using GenderPayGap.BusinessLogic;
using GenderPayGap.BusinessLogic.Account.Abstractions;
using GenderPayGap.BusinessLogic.Services;
using GenderPayGap.Core;
using GenderPayGap.Core.Classes;
using GenderPayGap.Core.Classes.Logger;
using GenderPayGap.Core.Interfaces;
using GenderPayGap.Core.Models;
using GenderPayGap.Core.Models.HttpResultModels;
using GenderPayGap.Database;
using GenderPayGap.Database.Models;
using GenderPayGap.Extensions;
using GenderPayGap.Extensions.AspNetCore;
using GenderPayGap.WebUI.Classes;
using GenderPayGap.WebUI.Classes.Services;
using GenderPayGap.WebUI.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GenderPayGap.WebUI.Controllers.Administration
{
    [Authorize(Roles = "GPGadmin")]
    [Route("admin")]
    public partial class AdminController : BaseController
    {

        #region Constructors

        public AdminController(
            IHttpCache cache,
            IHttpSession session,
            IHostingEnvironment hostingEnvironment,
            IAdminService adminService,
            IOrganisationBusinessLogic organisationBusinessLogic,
            ISearchBusinessLogic searchBusinessLogic,
            IUserRepository userRepository,
            IDataRepository dataRepository,
            IWebTracker webTracker,
            [KeyFilter("Private")] IPagedRepository<EmployerRecord> privateSectorRepository,
            [KeyFilter("Public")] IPagedRepository<EmployerRecord> publicSectorRepository,
            AuditLogger auditLogger
        ) : base(cache, session, dataRepository, webTracker)
        {
            HostingEnvironment = hostingEnvironment;
            AdminService = adminService;
            OrganisationBusinessLogic = organisationBusinessLogic;
            SearchBusinessLogic = searchBusinessLogic;
            UserRepository = userRepository;
            PrivateSectorRepository = privateSectorRepository;
            PublicSectorRepository = publicSectorRepository;
            this.auditLogger = auditLogger;
        }

        #endregion

        #region Initialisation

        /// <summary>
        ///     This action is only used to warm up this controller on initialisation
        /// </summary>
        /// <returns></returns>
        [HttpGet("Init")]
        public IActionResult Init()
        {
            return new EmptyResult();
        }

        #endregion

        #region Home Action

        [HttpGet]
        public IActionResult Home()
        {
            var viewModel = new AdminHomepageViewModel {
                IsSuperAdministrator = CurrentUser.IsSuperAdministrator(),
                IsDatabaseAdministrator = CurrentUser.IsDatabaseAdministrator(),
                IsDowngradedDueToIpRestrictions =
                    !IsTrustedIP && (CurrentUser.IsDatabaseAdministrator() || CurrentUser.IsSuperAdministrator()),
                FeedbackCount = DataRepository.GetAll<Feedback>().Count(),
                NewFeedbackCount = DataRepository.GetAll<Feedback>().Count(f => f.FeedbackStatus == FeedbackStatus.New),
                LatestFeedbackDate = DataRepository.GetAll<Feedback>()
                    .OrderByDescending(feedback => feedback.CreatedDate)
                    .FirstOrDefault()
                    ?.CreatedDate
            };

            return View("Home", viewModel);
        }

        #endregion

        #region Download Action

        [HttpGet("download")]
        public async Task<IActionResult> Download(string filePath)
        {
            //Ensure the file exists
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return new HttpNotFoundResult("Missing file path");
            }

            if (filePath.StartsWithI("http:", "https:"))
            {
                return new RedirectResult(filePath);
            }

            if (!await Global.FileRepository.GetFileExistsAsync(filePath))
            {
                return new HttpNotFoundResult($"File '{filePath}' does not exist");
            }

            //Setup the http response
            var contentDisposition = new ContentDisposition {FileName = Path.GetFileName(filePath), Inline = true};
            HttpContext.SetResponseHeader("Content-Disposition", contentDisposition.ToString());

            /* No Longer required as AspNetCore has response buffering on by default
            // Buffer response so that page is sent after processing is complete.
            Response.BufferOutput = true;
            */

            return Content(await Global.FileRepository.ReadAsync(filePath), "text/csv");
        }

        #endregion

        #region PendingRegistration Action

        [HttpGet("pending-registrations")]
        public async Task<IActionResult> PendingRegistrations()
        {
            List<UserOrganisation> nonUkAddressUserOrganisations =
                DataRepository
                    .GetAll<UserOrganisation>()
                    .Where(uo => uo.User.Status == UserStatuses.Active)
                    .Where(
                        uo => uo.PINConfirmedDate == null
                              && uo.Method == RegistrationMethods.Manual
                              && uo.Address.IsUkAddress.HasValue
                              && uo.Address.IsUkAddress.Value == false)
                    .OrderBy(uo => uo.Modified)
                    .ToList();

            List<UserOrganisation> publicSectorUserOrganisations =
                DataRepository
                    .GetAll<UserOrganisation>()
                    .Where(uo => uo.User.Status == UserStatuses.Active)
                    .Where(
                        uo => uo.PINConfirmedDate == null
                              && uo.Method == RegistrationMethods.Manual
                              && uo.Organisation.SectorType == SectorTypes.Public)
                    .OrderBy(uo => uo.Modified)
                    .ToList();

            List<UserOrganisation> allManuallyRegisteredUserOrganisations =
                DataRepository
                    .GetAll<UserOrganisation>()
                    .Where(uo => uo.User.Status == UserStatuses.Active)
                    .Where(uo => uo.PINConfirmedDate == null && uo.Method == RegistrationMethods.Manual)
                    .OrderBy(uo => uo.Modified)
                    .ToList();

            List<UserOrganisation> remainingManuallyRegisteredUserOrganisations =
                allManuallyRegisteredUserOrganisations
                    .Except(publicSectorUserOrganisations)
                    .Except(nonUkAddressUserOrganisations)
                    .ToList();

            var model = new PendingRegistrationsViewModel {
                PublicSectorUserOrganisations = publicSectorUserOrganisations,
                NonUkAddressUserOrganisations = nonUkAddressUserOrganisations,
                ManuallyRegisteredUserOrganisations = remainingManuallyRegisteredUserOrganisations
            };

            return View("PendingRegistrations", model);
        }

        #endregion

        #region Dependencies

        private readonly IHostingEnvironment HostingEnvironment;
        private readonly AuditLogger auditLogger;

        public IAdminService AdminService { get; }
        public IOrganisationBusinessLogic OrganisationBusinessLogic { get; set; }
        public ISearchBusinessLogic SearchBusinessLogic { get; set; }
        public IUserRepository UserRepository { get; }
        public IPagedRepository<EmployerRecord> PrivateSectorRepository { get; }
        public IPagedRepository<EmployerRecord> PublicSectorRepository { get; }

        #endregion
        
        #region Uploads Action

        [HttpGet("uploads")]
        public async Task<IActionResult> Uploads()
        {
            var model = new UploadViewModel();

            int sicSectionsCount = await DataRepository.GetAll<SicSection>().CountAsync();
            var upload = new UploadViewModel.Upload {
                Type = "SicSection",
                Filepath = Path.Combine(Global.DataPath, Filenames.SicSections),
                Title = "SIC Sections",
                Description = "Standard Industrial Classification (SIC) sector titles.",
                Count = sicSectionsCount.ToString()
            };
            upload.Modified = await Global.FileRepository.GetFileExistsAsync(upload.Filepath)
                ? await Global.FileRepository.GetLastWriteTimeAsync(upload.Filepath)
                : DateTime.MinValue;
            model.Uploads.Add(upload);

            int sicCodesCount = await DataRepository.GetAll<SicCode>().CountAsync();
            upload = new UploadViewModel.Upload {
                Type = "SicCode",
                Filepath = Path.Combine(Global.DataPath, Filenames.SicCodes),
                Title = "SIC Codes",
                Description = "Standard Industrial Classification (SIC) codes and titles.",
                Count = sicCodesCount.ToString()
            };
            upload.Modified = await Global.FileRepository.GetFileExistsAsync(upload.Filepath)
                ? await Global.FileRepository.GetLastWriteTimeAsync(upload.Filepath)
                : DateTime.MinValue;
            model.Uploads.Add(upload);

            this.StashModel(model);
            return View("Uploads", model);
        }

        [ValidateAntiForgeryToken]
        [PreventDuplicatePost]
        [HttpPost("uploads")]
        [RequestSizeLimit(52428800)]
        public async Task<IActionResult> Uploads(List<IFormFile> files, string command)
        {
            //Throw error if the user is not a super administrator
            if (!IsSuperAdministrator)
            {
                return new HttpUnauthorizedResult($"User {CurrentUser?.EmailAddress} is not a super administrator");
            }

            var model = this.UnstashModel<UploadViewModel>();
            if (model == null)
            {
                return View("CustomError", new ErrorViewModel(1138));
            }

            string filepath = command.AfterFirst(":");
            command = command.BeforeFirst(":");

            if (command.EqualsI("Recheck"))
            {
                await RecheckCompaniesAsync();
            }
            else if (command.EqualsI("Upload"))
            {
                IFormFile file = files.FirstOrDefault();
                if (file == null)
                {
                    ModelState.AddModelError("", "No file uploaded");
                    return View("Uploads", model);
                }

                UploadViewModel.Upload upload = model.Uploads.FirstOrDefault(u => u.Filename.EqualsI(file.FileName));
                if (upload == null)
                {
                    ModelState.AddModelError("", $@"Invalid filename '{file.FileName}'");
                    return View("Uploads", model);
                }

                if (file.Length == 0)
                {
                    ModelState.AddModelError("", $@"No content found in '{file.FileName}'");
                    return View("Uploads", model);
                }

                try
                {
                    using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
                    {
                        var csvReader = new CsvReader(reader);
                        csvReader.Configuration.WillThrowOnMissingField = false;
                        csvReader.Configuration.TrimFields = true;
                        csvReader.Configuration.IgnoreQuotes = false;
                        List<object> records;
                        switch (upload.Type)
                        {
                            case "SicSection":
                                records = csvReader.GetRecords<SicSection>().Cast<object>().ToList();
                                break;
                            case "SicCode":
                                records = csvReader.GetRecords<SicCode>().Cast<object>().ToList();
                                break;
                            default:
                                throw new Exception($"Invalid upload type '{upload.Type}'");
                        }

                        if (records.Count < 1)
                        {
                            ModelState.AddModelError("", $@"No records found in '{upload.Filename}'");
                            return View("Uploads", model);
                        }

                        //Core.Classes.Extensions
                        await Global.FileRepository.SaveCSVAsync(records, upload.Filepath);
                        DateTime updateTime = VirtualDateTime.Now.AddMinutes(-2);
                        switch (upload.Type)
                        {
                            case "SicSection":
                                await DataMigrations.Update_SICSectionsAsync(
                                    DataRepository,
                                    Global.FileRepository,
                                    Global.DataPath,
                                    true);
                                break;
                            case "SicCode":
                                await DataMigrations.Update_SICCodesAsync(
                                    DataRepository,
                                    Global.FileRepository,
                                    Global.DataPath,
                                    true);
                                //TODO Recheck remaining companies with no Sic against new SicCodes and then CoHo
                                await UpdateCompanySicCodesAsync(updateTime);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $@"Error reading file '{upload.Filename}': {ex.Message}");
                }
            }

            //Return any errors
            if (!ModelState.IsValid)
            {
                return View("Uploads", model);
            }

            return RedirectToAction("Uploads");
        }

        private async Task UpdateCompanySicCodesAsync(DateTime updateTime)
        {
            //Get all the bad sic records
            IEnumerable<string> files = await Global.FileRepository.GetFilesAsync(Global.LogPath, "BadSicLog*.csv", true);
            var fileRecords = new Dictionary<string, List<BadSicLogModel>>();
            var changedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string file in files)
            {
                List<BadSicLogModel> records = await Global.FileRepository.ReadCSVAsync<BadSicLogModel>(file);
                fileRecords[file] = records;
            }

            //Get all the new Sic Codes
            IQueryable<int> newSicCodes = DataRepository.GetAll<SicCode>().Where(s => s.Created >= updateTime).Select(s => s.SicCodeId);

            foreach (int newSicCode in newSicCodes)
            {
                foreach (string key in fileRecords.Keys)
                {
                    List<BadSicLogModel> file = fileRecords[key];
                    IEnumerable<BadSicLogModel> records = file.Where(r => r.SicCode == newSicCode);
                    foreach (BadSicLogModel record in records)
                    {
                        IQueryable<OrganisationSicCode> orgSics = DataRepository.GetAll<OrganisationSicCode>()
                            .Where(o => o.OrganisationId == record.OrganisationId);
                        if (await orgSics.AnyAsync())
                        {
                            if (await orgSics.AnyAsync(o => o.SicCodeId == newSicCode))
                            {
                                continue;
                            }

                            DataRepository.Insert(new OrganisationSicCode {OrganisationId = record.OrganisationId, SicCodeId = newSicCode});
                        }

                        file.Remove(record);
                        changedFiles.Add(key);
                    }
                }
            }

            await DataRepository.SaveChangesAsync();

            //Update or delete the changed log files
            foreach (string changedFile in changedFiles)
            {
                List<BadSicLogModel> records = fileRecords[changedFile];
                if (records.Any())
                {
                    await Global.FileRepository.SaveCSVAsync(fileRecords[changedFile], changedFile);
                }
                else
                {
                    await Global.FileRepository.DeleteFileAsync(changedFile);
                }
            }
        }

        private async Task RecheckCompaniesAsync()
        {
            //Get all the bad sic records
            IEnumerable<string> files = await Global.FileRepository.GetFilesAsync(Global.LogPath, "BadSicLog*.csv", true);
            var badSicCodes = new HashSet<string>();
            foreach (string file in files)
            {
                List<BadSicLogModel> records = await Global.FileRepository.ReadCSVAsync<BadSicLogModel>(file);
                badSicCodes.AddRange(records.ToList().Select(s => $"{s.OrganisationId}:{s.SicCode}").Distinct());
            }

            //Get all the private organisations with no sic codes
            IQueryable<Organisation> orgs = DataRepository.GetAll<Organisation>()
                .Where(
                    o => o.SectorType == SectorTypes.Private
                         && o.CompanyNumber != null
                         && !o.OrganisationSicCodes.Where(s => s.Retired == null).Any());
            List<SicCode> allSicCodes = await DataRepository.GetAll<SicCode>().ToListAsync();
            foreach (Organisation org in orgs)
            {
                try
                {
                    //Lookup the sic codes from companies house
                    string sicCodeResults = await PrivateSectorRepository.GetSicCodesAsync(org.CompanyNumber);
                    IEnumerable<int> sicCodes = sicCodeResults.SplitI().Select(s => s.ToInt32());
                    foreach (int code in sicCodes)
                    {
                        if (code <= 0)
                        {
                            continue;
                        }

                        SicCode sicCode = allSicCodes.FirstOrDefault(sic => sic.SicCodeId == code);
                        if (sicCode != null)
                        {
                            org.OrganisationSicCodes.Add(new OrganisationSicCode {Organisation = org, SicCode = sicCode});
                            continue;
                        }

                        if (badSicCodes.Contains($"{org.OrganisationId}:{code}"))
                        {
                            continue;
                        }

                        CustomLogger.Warning("Bad SIC code",
                            new
                            {
                                OrganisationId = org.OrganisationId,
                                OrganisationName = org.OrganisationName,
                                SicCode = code
                            });
                    }
                }
                catch (HttpException hex)
                {
                    int httpCode = hex.StatusCode;
                    if (httpCode.IsAny(429, (int) HttpStatusCode.NotFound))
                    {
                        CustomLogger.Error(hex.Message, hex);
                    }
                }
                catch (Exception ex)
                {
                    CustomLogger.Error(ex.Message, ex);
                }
            }

            await DataRepository.SaveChangesAsync();
        }

        #endregion

        #region Action Impersonate

        [HttpGet("impersonate")]
        public async Task<IActionResult> Impersonate(string emailAddress)
        {
            if (!string.IsNullOrWhiteSpace(emailAddress))
            {
                return await ImpersonatePost(emailAddress);
            }

            return View("Impersonate");
        }

        [HttpPost("impersonate")]
        [PreventDuplicatePost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImpersonatePost(string emailAddress)
        {
            //Ignore case of email address
            emailAddress = emailAddress?.ToLower();

            //Throw error if the user is not a super administrator of a test admin
            if (!IsSuperAdministrator && (!IsAdministrator || !IsTestUser || !emailAddress.StartsWithI(Global.TestPrefix)))
            {
                return new HttpUnauthorizedResult($"User {CurrentUser?.EmailAddress} is not a super administrator");
            }

            if (string.IsNullOrWhiteSpace(emailAddress) || !emailAddress.IsEmailAddress())
            {
                ModelState.AddModelError("", "You must enter a valid email address");
                return View("Impersonate");
            }

            //Ensure we get a valid user from the database
            User currentUser = DataRepository.FindUser(User);
            if (currentUser == null || !currentUser.IsAdministrator())
            {
                throw new IdentityNotMappedException();
            }

            if (currentUser.EmailAddress.StartsWithI(Global.TestPrefix) && !emailAddress.StartsWithI(Global.TestPrefix))
            {
                ModelState.AddModelError(
                    "",
                    "Test administrators are only permitted to impersonate other test users");
                return View("Impersonate");
            }

            // find the latest active user by email
            User impersonatedUser = await UserRepository.FindByEmailAsync(emailAddress, UserStatuses.Active);
            if (impersonatedUser == null)
            {
                ModelState.AddModelError("", "This user does not exist");
                return View("Impersonate");
            }

            if (impersonatedUser.IsAdministrator())
            {
                ModelState.AddModelError("", "Impersonating other administrators is not permitted");
                return View("Impersonate");
            }

            ImpersonatedUserId = impersonatedUser.UserId;
            OriginalUser = currentUser;

            //Refresh page to ensure identity is passed in cookie
            return RedirectToAction(nameof(OrganisationController.ManageOrganisations), "Organisation");
        }

        #endregion

    }
}
