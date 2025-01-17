﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using GenderPayGap.Core.Classes.ErrorMessages;
using GenderPayGap.Extensions;
using GenderPayGap.WebUI.Classes.Attributes;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace GenderPayGap.WebUI.Classes
{
    public static class HtmlHelpers
    {

        public static async Task<IHtmlContent> PartialModelAsync<T>(this IHtmlHelper htmlHelper, T viewModel)
        {
            // extract the parial path from the model class attr
            string partialPath = viewModel.GetAttribute<PartialAttribute>().PartialPath;
            return await htmlHelper.PartialAsync(partialPath, viewModel);
        }

        public static HtmlString SetErrorClass<TModel, TProperty>(
            this IHtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression,
            string errorClassName,
            string noErrorClassName = null)
        {
            string expressionText = GetModelExpressionProvider(htmlHelper).GetExpressionText(expression);
            string fullHtmlFieldName = htmlHelper.ViewContext.ViewData
                .TemplateInfo.GetFullHtmlFieldName(expressionText);

            return SetErrorClass(htmlHelper, fullHtmlFieldName, errorClassName, noErrorClassName);
        }

        public static HtmlString SetErrorClass<TModel>(
            this IHtmlHelper<TModel> htmlHelper,
            string fullHtmlFieldName,
            string errorClassName,
            string noErrorClassName = null)
        {
            ModelStateEntry state = htmlHelper.ViewData.ModelState[fullHtmlFieldName];

            if (!string.IsNullOrWhiteSpace(noErrorClassName))
            {
                return state == null || state.Errors.Count == 0 ? new HtmlString(noErrorClassName) : new HtmlString(errorClassName);
            }

            return state == null || state.Errors.Count == 0 ? HtmlString.Empty : new HtmlString(errorClassName);
        }


        public static string WithQuery(this IUrlHelper helper, string actionName, object routeValues)
        {
            var newRoute = new NameValueCollection();

            if (helper.ActionContext.HttpContext.Request.QueryString.HasValue)
            {
                var existingRoute = HttpUtility.ParseQueryString(helper.ActionContext.HttpContext.Request.QueryString.Value);
                newRoute.Add(existingRoute);
            }

            foreach (KeyValuePair<string, object> item in new RouteValueDictionary(routeValues))
            {
                newRoute[item.Key] = item.Value.ToString();
            }

            string querystring = null;
            var keys = new SortedSet<string>(newRoute.AllKeys);
            foreach (string key in keys)
            {
                foreach (string value in newRoute.GetValues(key))
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(querystring))
                    {
                        querystring += "&";
                    }

                    querystring += $"{key}={value}";
                }
            }

            return helper.Action(actionName) + "?" + querystring;
        }
        
        #region Asset Bundles

        public static HtmlString UnpackBundle(this IHtmlHelper htmlHelper, string bundlePath, string media = "")
        {
            Bundle bundle = Bundle.ReadBundleFile("bundleconfig.json", bundlePath);
            if (bundle == null)
            {
                return null;
            }

            IEnumerable<string> outputString = bundlePath.EndsWith(".js")
                ? bundle.InputFiles.Select(inputFile => $"<script src='/{inputFile}' type='text/javascript'></script>")
                : bundle.InputFiles.Select(inputFile => $"<link rel='stylesheet' type='text/css' media='{media}' href='/{inputFile}' />");

            return new HtmlString(string.Join("\n", outputString));
        }

        #endregion

        #region Validation messages

        public static async Task<IHtmlContent> CustomValidationSummaryAsync(this IHtmlHelper helper,
            bool excludePropertyErrors = true,
            string validationSummaryMessage = "The following errors were detected",
            object htmlAttributes = null)
        {
            helper.ViewBag.ValidationSummaryMessage = validationSummaryMessage;
            helper.ViewBag.ExcludePropertyErrors = excludePropertyErrors;

            return await helper.PartialAsync("_ValidationSummary");
        }

        private static Dictionary<string, object> CustomAttributesFor<TModel, TProperty>(IHtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression,
            object htmlAttributes = null)
        {
            Type containerType = typeof(TModel);

            string propertyName = GetModelExpressionProvider(htmlHelper).GetExpressionText(expression);
            PropertyInfo propertyInfo = containerType.GetPropertyInfo(propertyName);

            var displayNameAttribute =
                propertyInfo?.GetCustomAttributes(typeof(DisplayNameAttribute), false).FirstOrDefault() as DisplayNameAttribute;
            var displayAttribute = propertyInfo?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            string displayName = displayNameAttribute != null ? displayNameAttribute.DisplayName :
                displayAttribute != null ? displayAttribute.Name : propertyName;

            string par1 = null;
            string par2 = null;

            Dictionary<string, object> htmlAttr = htmlAttributes.ToPropertyDictionary();
            if (propertyInfo != null)
            {
                foreach (ValidationAttribute attribute in propertyInfo.GetCustomAttributes(typeof(ValidationAttribute), false))
                {
                    string validatorKey = $"{containerType.Name}.{propertyName}:{attribute.GetType().Name.TrimSuffix("Attribute")}";
                    CustomErrorMessage customError = CustomErrorMessages.GetValidationError(validatorKey);
                    if (customError == null)
                    {
                        continue;
                    }

                    //Set the message from the description
                    if (attribute.ErrorMessage != customError.Description)
                    {
                        attribute.ErrorMessage = customError.Description;
                    }

                    //Set the inline error message
                    var errorMessageString = Misc.GetPropertyValue(attribute, "ErrorMessageString") as string;
                    if (string.IsNullOrWhiteSpace(errorMessageString))
                    {
                        errorMessageString = attribute.ErrorMessage;
                    }

                    //Set the summary error message
                    if (customError.Title != errorMessageString)
                    {
                        errorMessageString = customError.Title;
                    }

                    //Set the display name
                    if (!string.IsNullOrWhiteSpace(customError.DisplayName) && customError.DisplayName != displayName)
                    {
                        if (displayAttribute != null)
                        {
                            Misc.SetPropertyValue(displayAttribute, "Name", customError.DisplayName);
                        }

                        displayName = customError.DisplayName;
                    }

                    string altAttr = null;
                    if (attribute is RequiredAttribute)
                    {
                        altAttr = "data-val-required-alt";
                    }
                    else if (attribute is CompareAttribute)
                    {
                        altAttr = "data-val-equalto-alt";
                    }
                    else if (attribute is RegularExpressionAttribute)
                    {
                        altAttr = "data-val-regex-alt";
                    }
                    else if (attribute is RangeAttribute)
                    {
                        altAttr = "data-val-range-alt";
                        par1 = ((RangeAttribute) attribute).Minimum.ToString();
                        par2 = ((RangeAttribute) attribute).Maximum.ToString();
                    }
                    else if (attribute is DataTypeAttribute)
                    {
                        string type = ((DataTypeAttribute) attribute).DataType.ToString().ToLower();
                        switch (type)
                        {
                            case "password":
                                continue;
                            case "emailaddress":
                                type = "email";
                                break;
                            case "phonenumber":
                                type = "phone";
                                break;
                        }

                        altAttr = $"data-val-{type}-alt";
                    }
                    else if (attribute is MinLengthAttribute)
                    {
                        altAttr = "data-val-minlength-alt";
                        par1 = ((MinLengthAttribute) attribute).Length.ToString();
                    }
                    else if (attribute is MaxLengthAttribute)
                    {
                        altAttr = "data-val-maxlength-alt";
                        par1 = ((MaxLengthAttribute) attribute).Length.ToString();
                    }
                    else if (attribute is StringLengthAttribute)
                    {
                        altAttr = "data-val-length-alt";
                        par1 = ((StringLengthAttribute) attribute).MinimumLength.ToString();
                        par2 = ((StringLengthAttribute) attribute).MaximumLength.ToString();
                    }

                    htmlAttr[altAttr.TrimSuffix("-alt")] = string.Format(attribute.ErrorMessage, displayName, par1, par2);
                    htmlAttr[altAttr] = string.Format(errorMessageString, displayName, par1, par2);
                }
            }

            return htmlAttr;
        }

        private static ModelExpressionProvider GetModelExpressionProvider<TModel>(IHtmlHelper<TModel> htmlHelper)
        {
            return htmlHelper.ViewContext.HttpContext.RequestServices.GetService(typeof(ModelExpressionProvider)) as ModelExpressionProvider;
        }

        public static IHtmlContent CustomEditorFor<TModel, TProperty>(this IHtmlHelper<TModel> helper,
            Expression<Func<TModel, TProperty>> expression,
            object htmlAttributes = null)
        {
            Dictionary<string, object> htmlAttr = CustomAttributesFor(helper, expression, htmlAttributes);

            // By default, decimals are truncated to 2 decimal places in edit mode
            // On error, this can be confusing as the value displayed is not the user input
            // As a workaround, we use the String template in order to display the exact same input
            if (typeof(TProperty).FullName == typeof(decimal).FullName || typeof(TProperty).FullName == typeof(decimal?).FullName)
            {
                return helper.EditorFor(expression, "String", new {htmlAttributes = htmlAttr});
            }
            return helper.EditorFor(expression, null, new {htmlAttributes = htmlAttr});
        }

        public static IHtmlContent CustomRadioButtonFor<TModel, TProperty>(this IHtmlHelper<TModel> helper,
            Expression<Func<TModel, TProperty>> expression,
            object value,
            object htmlAttributes = null)
        {
            Dictionary<string, object> htmlAttr = CustomAttributesFor(helper, expression, htmlAttributes);

            return helper.RadioButtonFor(expression, value, htmlAttr);
        }

        #endregion

    }
}
