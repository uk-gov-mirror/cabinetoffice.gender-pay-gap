using System;
using System.ComponentModel.DataAnnotations;
using GenderPayGap.Core;

namespace GenderPayGap.WebUI.BusinessLogic.Models.Submit
{
    [Serializable]
    public class ReturnViewModel
    {

        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Enter the difference in mean hourly rate")]
        [Range(-499.99, 100, ErrorMessage = "Value must be between -499.99 and 100")]
        [RegularExpression(@"^[-]?\d+(\.{0,1}\d)?$", ErrorMessage = "Value can't have more than 1 decimal place")]
        [DisplayFormat(DataFormatString = "{0:0.#}", ApplyFormatInEditMode = true)]
        public decimal? DiffMeanHourlyPayPercent { get; set; }

        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Enter the difference in median hourly rate")]
        [Range(-499.99, 100, ErrorMessage = "Value must be between -499.99 and 100")]
        [RegularExpression(@"^[-]?\d+(\.{0,1}\d)?$", ErrorMessage = "Value can't have more than 1 decimal place")]
        [DisplayFormat(DataFormatString = "{0:0.#}", ApplyFormatInEditMode = true)]
        public decimal? DiffMedianHourlyPercent { get; set; }

        [Display(Name = "Enter the difference in mean bonus pay, calculated from the mean")]
        [Range((double) decimal.MinValue, 100, ErrorMessage = "Value must be lower than 100")]
        [RegularExpression(@"^[-]?\d+(\.{0,1}\d)?$", ErrorMessage = "Value can't have more than 1 decimal place")]
        [DisplayFormat(DataFormatString = "{0:0.#}", ApplyFormatInEditMode = true)]
        public decimal? DiffMeanBonusPercent { get; set; }

        [Display(Name = "Enter the difference in median bonus pay, calculated from the median")]
        [Range((double) decimal.MinValue, 100, ErrorMessage = "Value must be lower than 100")]
        [RegularExpression(@"^[-]?\d+(\.{0,1}\d)?$", ErrorMessage = "Value can't have more than 1 decimal place")]
        [DisplayFormat(DataFormatString = "{0:0.#}", ApplyFormatInEditMode = true)]
        public decimal? DiffMedianBonusPercent { get; set; }

        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Males who received bonus pay")]
        [Range(0, 100, ErrorMessage = "Value must be between 0 and 100")]
        [RegularExpression(@"^\d+(\.{0,1}\d)?$", ErrorMessage = "Value can't have more than 1 decimal place")]
        [DisplayFormat(DataFormatString = "{0:0.#}", ApplyFormatInEditMode = true)]
        public decimal? MaleMedianBonusPayPercent { get; set; }

        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Females who received bonus pay")]
        [Range(0, 100, ErrorMessage = "Value must be between 0 and 100")]
        [RegularExpression(@"^\d+(\.{0,1}\d)?$", ErrorMessage = "Value can't have more than 1 decimal place")]
        [DisplayFormat(DataFormatString = "{0:0.#}", ApplyFormatInEditMode = true)]
        public decimal? FemaleMedianBonusPayPercent { get; set; }

        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Male")]
        [Range(0, 200.9, ErrorMessage = "Value must be between 0 and 200.9")]
        [RegularExpression(@"^\d+(\.{0,1}\d)?$", ErrorMessage = "Value can't have more than 1 decimal place")]
        [DisplayFormat(DataFormatString = "{0:0.#}", ApplyFormatInEditMode = true)]
        public decimal? MaleLowerPayBand { get; set; }

        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Female")]
        [Range(0, 200.9, ErrorMessage = "Value must be between 0 and 200.9")]
        [RegularExpression(@"^\d+(\.{0,1}\d)?$", ErrorMessage = "Value can't have more than 1 decimal place")]
        [DisplayFormat(DataFormatString = "{0:0.#}", ApplyFormatInEditMode = true)]
        public decimal? FemaleLowerPayBand { get; set; }

        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Male")]
        [Range(0, 200.9, ErrorMessage = "Value must be between 0 and 200.9")]
        [RegularExpression(@"^\d+(\.{0,1}\d)?$", ErrorMessage = "Value can't have more than 1 decimal place")]
        [DisplayFormat(DataFormatString = "{0:0.#}", ApplyFormatInEditMode = true)]
        public decimal? MaleMiddlePayBand { get; set; }

        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Female")]
        [Range(0, 200.9, ErrorMessage = "Value must be between 0 and 200.9")]
        [RegularExpression(@"^\d+(\.{0,1}\d)?$", ErrorMessage = "Value can't have more than 1 decimal place")]
        [DisplayFormat(DataFormatString = "{0:0.#}", ApplyFormatInEditMode = true)]
        public decimal? FemaleMiddlePayBand { get; set; }

        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Male")]
        [Range(0, 200.9, ErrorMessage = "Value must be between 0 and 200.9")]
        [RegularExpression(@"^\d+(\.{0,1}\d)?$", ErrorMessage = "Value can't have more than 1 decimal place")]
        [DisplayFormat(DataFormatString = "{0:0.#}", ApplyFormatInEditMode = true)]
        public decimal? MaleUpperPayBand { get; set; }

        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Female")]
        [Range(0, 200.9, ErrorMessage = "Value must be between 0 and 200.9")]
        [RegularExpression(@"^\d+(\.{0,1}\d)?$", ErrorMessage = "Value can't have more than 1 decimal place")]
        [DisplayFormat(DataFormatString = "{0:0.#}", ApplyFormatInEditMode = true)]
        public decimal? FemaleUpperPayBand { get; set; }

        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Male")]
        [Range(0, 200.9, ErrorMessage = "Value must be between 0 and 200.9")]
        [RegularExpression(@"^\d+(\.{0,1}\d)?$", ErrorMessage = "Value can't have more than 1 decimal place")]
        [DisplayFormat(DataFormatString = "{0:0.#}", ApplyFormatInEditMode = true)]
        public decimal? MaleUpperQuartilePayBand { get; set; }

        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Female")]
        [Range(0, 200.9, ErrorMessage = "Value must be between 0 and 200.9")]
        [RegularExpression(@"^\d+(\.{0,1}\d)?$", ErrorMessage = "Value can't have more than 1 decimal place")]
        [DisplayFormat(DataFormatString = "{0:0.#}", ApplyFormatInEditMode = true)]
        public decimal? FemaleUpperQuartilePayBand { get; set; }

        public string EncryptedOrganisationId { get; set; }
        public SectorTypes SectorType { get; set; }

        public DateTime AccountingDate { get; set; }

        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Job title")]
        public string JobTitle { get; set; }

        [Required(AllowEmptyStrings = false)]
        [Display(Name = "First name")]
        public string FirstName { get; set; }

        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Last name")]
        public string LastName { get; set; }

        [Url]
        [MaxLength(255, ErrorMessage = "The web address (URL) cannot be longer than 255 characters.")]
        [Display(Name = "Link to your gender pay gap information")]
        public string CompanyLinkToGPGInfo { get; set; }

        public string Address { get; set; }
        public string LatestAddress { get; set; }
        public string OrganisationName { get; set; }
        public string LatestOrganisationName { get; set; }
        public OrganisationSizes OrganisationSize { get; set; }

        public bool IsVoluntarySubmission { get; set; }
        public bool IsLateSubmission { get; set; }

        #region Hourly Rate Helpers

        public bool FemaleHasLowerMeanHourlyPercent => DiffMeanHourlyPayPercent == null || DiffMeanHourlyPayPercent >= 0;

        public bool FemaleHasLowerMedianHourlyPercent => DiffMedianHourlyPercent == null || DiffMedianHourlyPercent >= 0;

        public decimal FemaleMoneyFromMedianHourlyRate
        {
            get
            {
                if (DiffMedianHourlyPercent == null)
                {
                    return 0;
                }

                return FemaleHasLowerMedianHourlyPercent
                    ? 100 - DiffMedianHourlyPercent.Value
                    : 100 + Math.Abs(DiffMedianHourlyPercent.Value);
            }
        }

        public string FemaleMedianHourlyRateMonitised
        {
            get
            {
                decimal roundedRate = Math.Round(FemaleMoneyFromMedianHourlyRate);
                if (roundedRate < 100)
                {
                    return $"{roundedRate}p";
                }

                if (roundedRate == 100)
                {
                    return "£1";
                }

                return $"£{string.Format("{0:#.00}", roundedRate / 100)}";
            }
        }

        public string MaleMedianHourlyRateMonitised => "£1";

        #endregion

        #region Bonus Pay Helpers

        public decimal FemaleMoneyFromMedianBonusPay
        {
            get
            {
                if (DiffMedianBonusPercent == null)
                {
                    return 0;
                }

                return FemaleHasLowerMedianBonusPay ? 100 - DiffMedianBonusPercent.Value : 100 + Math.Abs(DiffMedianBonusPercent.Value);
            }
        }

        public string FemaleMedianBonusPayMonitised
        {
            get
            {
                decimal roundedRate = Math.Round(FemaleMoneyFromMedianBonusPay);
                if (roundedRate < 100)
                {
                    return $"{roundedRate}p";
                }

                if (roundedRate == 100)
                {
                    return "£1";
                }

                return $"£{string.Format("{0:#.00}", roundedRate / 100)}";
            }
        }

        public string MaleMedianBonusPayMonitised => "£1";

        public bool FemaleHasLowerMeanBonusPay => DiffMeanBonusPercent == null || DiffMeanBonusPercent >= 0;

        public bool FemaleHasLowerMedianBonusPay => DiffMedianBonusPercent == null || DiffMedianBonusPercent >= 0;

        public bool MenReceivedBonuses => MaleMedianBonusPayPercent.HasValue && MaleMedianBonusPayPercent.Value != 0;

        public bool WomenReceivedBonuses => FemaleMedianBonusPayPercent.HasValue && FemaleMedianBonusPayPercent.Value != 0;

        #endregion

    }
}
