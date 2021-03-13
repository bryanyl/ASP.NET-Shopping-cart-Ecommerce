﻿using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Theming;
using Smartstore.Web.Theming;

namespace Smartstore.Web.Controllers
{
    public class CommonController : SmartController
    {
        private readonly SmartDbContext _db;
        private readonly Lazy<IMediaService> _mediaService;
        private readonly IThemeContext _themeContext;
        private readonly IThemeRegistry _themeRegistry;
        private readonly ThemeSettings _themeSettings;

        public CommonController(
            SmartDbContext db,
            Lazy<IMediaService> mediaService,
            IThemeContext themeContext, 
            IThemeRegistry themeRegistry, 
            ThemeSettings themeSettings)
        {
            _db = db;
            _mediaService = mediaService;
            _themeContext = themeContext;
            _themeRegistry = themeRegistry;
            _themeSettings = themeSettings;
        }

        [Route("browserconfig.xml")]
        public async Task<IActionResult> BrowserConfigXmlFile()
        {
            var store = Services.StoreContext.CurrentStore;

            if (store.MsTileImageMediaFileId == 0 || store.MsTileColor.IsEmpty())
                return new EmptyResult();

            var mediaService = _mediaService.Value;
            var msTileImage = await mediaService.GetFileByIdAsync(Convert.ToInt32(store.MsTileImageMediaFileId), MediaLoadFlags.AsNoTracking);
            if (msTileImage == null)
                return new EmptyResult();

            XElement root = new (
                "browserconfig",
                new XElement
                (
                    "msapplication",
                    new XElement
                    (
                        "tile",
                        new XElement("square70x70logo", new XAttribute("src", mediaService.GetUrl(msTileImage, MediaSettings.ThumbnailSizeSm, host: string.Empty))),
                        new XElement("square150x150logo", new XAttribute("src", mediaService.GetUrl(msTileImage, MediaSettings.ThumbnailSizeMd, host: string.Empty))),
                        new XElement("square310x310logo", new XAttribute("src", mediaService.GetUrl(msTileImage, MediaSettings.ThumbnailSizeLg, host: string.Empty))),
                        new XElement("wide310x150logo", new XAttribute("src", mediaService.GetUrl(msTileImage, MediaSettings.ThumbnailSizeMd, host: string.Empty))),
                        new XElement("TileColor", store.MsTileColor)
                    )
                )
            );

            var doc = new XDocument(root);
            var xml = doc.ToString(SaveOptions.DisableFormatting);
            return Content(xml, "text/xml");
        }

        [HttpPost]
        public IActionResult ChangeTheme(string themeName, string returnUrl = null)
        {
            if (!_themeSettings.AllowCustomerToSelectTheme || (themeName.HasValue() && !_themeRegistry.ThemeManifestExists(themeName)))
            {
                return NotFound();
            }

            _themeContext.WorkingThemeName = themeName;

            if (HttpContext.Request.IsAjaxRequest())
            {
                return Json(new { Success = true });
            }

            return RedirectToReferrer(returnUrl);
        }

        [LocalizedRoute("/currency-selected/{customerCurrency:int}", Name = "ChangeCurrency")]
        public async Task<IActionResult> CurrencySelected(int customerCurrency, string returnUrl = null)
        {
            var currency = await _db.Currencies.FindByIdAsync(customerCurrency);
            if (currency != null)
            {
                Services.WorkContext.WorkingCurrency = currency;
            }

            return RedirectToReferrer(returnUrl);
        }
    }
}