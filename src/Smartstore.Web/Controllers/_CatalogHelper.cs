﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Caching;
using Smartstore.Core;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Catalog.Search.Modelling;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.DataExchange.Export;
using Smartstore.Core.Domain.Catalog;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Web.Infrastructure.Hooks;
using Smartstore.Web.Models.Catalog;
using Smartstore.Web.Models.Media;

namespace Smartstore.Web.Controllers
{
    public partial class CatalogHelper
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly ICacheManager _cache;
        private readonly IMenuService _menuService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IProductService _productService;
        //private readonly IProductTemplateService _productTemplateService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly ITaxService _taxService;
        private readonly ICurrencyService _currencyService;
        private readonly IMediaService _mediaService;
        private readonly ILocalizationService _localizationService;
        private readonly IPriceCalculationService _priceCalculationService;
        //private readonly IPriceFormatter _priceFormatter;
        //private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly IDateTimeHelper _dateTimeHelper;
        //private readonly IBackInStockSubscriptionService _backInStockSubscriptionService;
        private readonly IDownloadService _downloadService;
        private readonly MediaSettings _mediaSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly CaptchaSettings _captchaSettings;
        private readonly TaxSettings _taxSettings;
        private readonly PerformanceSettings _performanceSettings;
        private readonly IMeasureService _measureService;
        //private readonly IQuantityUnitService _quantityUnitService;
        private readonly MeasureSettings _measureSettings;
        private readonly IDeliveryTimeService _deliveryTimeService;
        private readonly Lazy<IDataExporter> _dataExporter;
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly ICatalogSearchQueryFactory _catalogSearchQueryFactory;
        private readonly HttpRequest _httpRequest;
        private readonly IUrlHelper _urlHelper;
        private readonly ProductUrlHelper _productUrlHelper;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly IUrlService _urlService;
        private readonly ILinkResolver _linkResolver;

        public CatalogHelper(
            SmartDbContext db,
            ICommonServices services,
            IMenuService menuService,
            IManufacturerService manufacturerService,
            IProductService productService,
            //IProductTemplateService productTemplateService,
            IProductAttributeService productAttributeService,
            IProductAttributeMaterializer productAttributeMaterializer,
            IProductAttributeFormatter productAttributeFormatter,
            ITaxService taxService,
            ICurrencyService currencyService,
            IMediaService mediaService,
            IPriceCalculationService priceCalculationService,
            //IPriceFormatter priceFormatter,
            //ISpecificationAttributeService specificationAttributeService,
            IDateTimeHelper dateTimeHelper,
            //IBackInStockSubscriptionService backInStockSubscriptionService,
            IDownloadService downloadService,
            MediaSettings mediaSettings,
            CatalogSettings catalogSettings,
            CustomerSettings customerSettings,
            CaptchaSettings captchaSettings,
            IMeasureService measureService,
            //IQuantityUnitService quantityUnitService,
            MeasureSettings measureSettings,
            TaxSettings taxSettings,
            PerformanceSettings performanceSettings,
            IDeliveryTimeService deliveryTimeService,
            Lazy<IDataExporter> dataExporter,
            ICatalogSearchService catalogSearchService,
            ICatalogSearchQueryFactory catalogSearchQueryFactory,
            IUrlHelper urlHelper,
            ProductUrlHelper productUrlHelper,
            ILocalizedEntityService localizedEntityService,
            IUrlService urlService,
            ILinkResolver linkResolver)
        {
            _db = db;
            _services = services;
            _workContext = services.WorkContext;
            _storeContext = services.StoreContext;
            _cache = services.Cache;
            _menuService = menuService;
            _manufacturerService = manufacturerService;
            _productService = productService;
            //_productTemplateService = productTemplateService;
            _productAttributeService = productAttributeService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _productAttributeFormatter = productAttributeFormatter;
            _taxService = taxService;
            _currencyService = currencyService;
            _mediaService = mediaService;
            _localizationService = _services.Localization;
            _priceCalculationService = priceCalculationService;
            //_priceFormatter = priceFormatter;
            //_specificationAttributeService = specificationAttributeService;
            _dateTimeHelper = dateTimeHelper;
            //_backInStockSubscriptionService = backInStockSubscriptionService;
            _downloadService = downloadService;
            _measureService = measureService;
            //_quantityUnitService = quantityUnitService;
            _measureSettings = measureSettings;
            _taxSettings = taxSettings;
            _performanceSettings = performanceSettings;
            _deliveryTimeService = deliveryTimeService;
            _mediaSettings = mediaSettings;
            _catalogSettings = catalogSettings;
            _customerSettings = customerSettings;
            _captchaSettings = captchaSettings;
            _dataExporter = dataExporter;
            _catalogSearchService = catalogSearchService;
            _catalogSearchQueryFactory = catalogSearchQueryFactory;
            _urlHelper = urlHelper;
            _productUrlHelper = productUrlHelper;
            _localizedEntityService = localizedEntityService;
            _urlService = urlService;
            _linkResolver = linkResolver;
            _httpRequest = _urlHelper.ActionContext.HttpContext.Request;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
        public ILogger Logger { get; set; } = NullLogger.Instance;
        public DbQuerySettings QuerySettings { get; set; } = DbQuerySettings.Default;

        #region Brand

        public async Task<List<BrandOverviewModel>> PrepareBrandOverviewModelAsync(
            ICollection<ProductManufacturer> brands,
            IDictionary<int, BrandOverviewModel> cachedModels = null,
            bool withPicture = false)
        {
            var model = new List<BrandOverviewModel>();
            cachedModels ??= new Dictionary<int, BrandOverviewModel>();
            IDictionary<int, MediaFileInfo> mediaFileLookup = null;

            if (withPicture)
            {
                mediaFileLookup = brands
                    .Select(x => x.Manufacturer.MediaFile)
                    .Where(x => x != null)
                    .Distinct()
                    .Select(x => _mediaService.ConvertMediaFile(x))
                    .ToDictionarySafe(x => x.Id);
            }

            //var files = _mediaService.GetFilesByIds(fileIds).ToDictionarySafe(x => x.Id);

            foreach (var pm in brands)
            {
                var manufacturer = pm.Manufacturer;

                if (!cachedModels.TryGetValue(manufacturer.Id, out BrandOverviewModel item))
                {
                    item = new BrandOverviewModel
                    {
                        Id = manufacturer.Id,
                        Name = manufacturer.GetLocalized(x => x.Name),
                        Description = manufacturer.GetLocalized(x => x.Description, true),
                        SeName = manufacturer.GetActiveSlug()
                    };

                    if (withPicture)
                    {
                        item.Picture = await PrepareBrandPictureModelAsync(manufacturer, item.Name, mediaFileLookup);
                    }

                    cachedModels.Add(item.Id, item);
                }

                model.Add(item);
            }

            return model;
        }

        public async Task<PictureModel> PrepareBrandPictureModelAsync(Manufacturer brand, string localizedName, IDictionary<int, MediaFileInfo> fileLookup = null)
        {
            MediaFileInfo file;

            if (fileLookup != null)
            {
                fileLookup.TryGetValue(brand.MediaFileId ?? 0, out file);
            }
            else
            {
                file = await _mediaService.GetFileByIdAsync(brand.MediaFileId ?? 0, MediaLoadFlags.AsNoTracking);
            }

            var model = new PictureModel
            {
                File = file,
                ThumbSize = _mediaSettings.ManufacturerThumbPictureSize,
                Title = file?.File?.GetLocalized(x => x.Title)?.Value.NullEmpty() ?? string.Format(T("Media.Manufacturer.ImageLinkTitleFormat"), localizedName),
                Alt = file?.File?.GetLocalized(x => x.Alt)?.Value.NullEmpty() ?? string.Format(T("Media.Manufacturer.ImageAlternateTextFormat"), localizedName),
                NoFallback = _catalogSettings.HideManufacturerDefaultPictures
            };

            _services.DisplayControl.Announce(file?.File);

            return model;
        }

        public async Task<BrandNavigationModel> PrepareBrandNavigationModelAsync(int brandItemsToDisplay)
        {
            var storeId = _services.StoreContext.CurrentStore.Id;
            var storeToken = QuerySettings.IgnoreMultiStore ? "0" : storeId.ToString();
            var rolesToken = QuerySettings.IgnoreAcl ? "0" : _services.WorkContext.CurrentCustomer.GetRolesIdent();

            var settingsKeyPart = string.Join(",",
                _catalogSettings.ShowManufacturersOnHomepage,
                _catalogSettings.ShowManufacturerPictures,
                _catalogSettings.HideManufacturerDefaultPictures,
                _mediaSettings.ManufacturerThumbPictureSize).ToLower();

            var cacheKey = string.Format(ModelCacheInvalidator.MANUFACTURER_NAVIGATION_MODEL_KEY,
                settingsKeyPart,
                _services.WorkContext.WorkingLanguage.Id,
                storeToken,
                rolesToken,
                brandItemsToDisplay);

            var cacheModel = await _services.CacheFactory.GetMemoryCache().GetAsync(cacheKey, async (o) =>
            {
                o.ExpiresIn(TimeSpan.FromHours(6));

                var manufacturers = await _db.Manufacturers
                    .AsNoTracking()
                    .ApplyStandardFilter(false, _workContext.CurrentCustomer.GetRoleIds(), storeId)
                    .Take(brandItemsToDisplay + 1)
                    .ToListAsync();

                var files = new Dictionary<int, MediaFileInfo>();

                if (_catalogSettings.ShowManufacturerPictures)
                {
                    var fileIds = manufacturers
                        .Select(x => x.MediaFileId ?? 0)
                        .Where(x => x != 0)
                        .Distinct()
                        .ToArray();
                    files = (await _mediaService.GetFilesByIdsAsync(fileIds)).ToDictionarySafe(x => x.Id);
                }

                var model = new BrandNavigationModel
                {
                    DisplayBrands = _catalogSettings.ShowManufacturersOnHomepage,
                    DisplayImages = _catalogSettings.ShowManufacturerPictures,
                    DisplayAllBrandsLink = manufacturers.Count > brandItemsToDisplay,
                    HideBrandDefaultPictures = _catalogSettings.HideManufacturerDefaultPictures,
                    BrandThumbPictureSize = _mediaSettings.ManufacturerThumbPictureSize
                };

                if (model.DisplayAllBrandsLink)
                {
                    brandItemsToDisplay -= 1;
                }

                foreach (var manufacturer in manufacturers.Take(brandItemsToDisplay))
                {
                    files.TryGetValue(manufacturer.MediaFileId ?? 0, out var file);

                    var name = manufacturer.GetLocalized(x => x.Name);

                    model.Brands.Add(new BrandBriefInfoModel
                    {
                        Id = manufacturer.Id,
                        Name = name,
                        SeName = manufacturer.GetActiveSlug(),
                        DisplayOrder = manufacturer.DisplayOrder,
                        Picture = new PictureModel
                        {
                            File = file,
                            ThumbSize = _mediaSettings.ManufacturerThumbPictureSize,
                            Alt = file?.File?.GetLocalized(x => x.Alt)?.Value.NullEmpty() ?? name,
                            Title = file?.File?.GetLocalized(x => x.Title)?.Value.NullEmpty() ?? name,
                            NoFallback = _catalogSettings.HideManufacturerDefaultPictures
                        }
                    });
                }

                return model;
            });

            return cacheModel;
        }

        #endregion

        #region Category

        public async Task<List<CategorySummaryModel>> MapCategorySummaryModelAsync(IEnumerable<Category> categories, int thumbSize)
        {
            Guard.NotNull(categories, nameof(categories));

            var fileIds = categories
                .Select(x => x.MediaFileId ?? 0)
                .Where(x => x != 0)
                .Distinct()
                .ToArray();
            var files = (await _mediaService.GetFilesByIdsAsync(fileIds)).ToDictionarySafe(x => x.Id);

            return await categories
                .SelectAsync(async c =>
                {
                    var name = c.GetLocalized(y => y.Name);
                    var model = new CategorySummaryModel
                    {
                        Id = c.Id,
                        Name = name
                    };

                    _services.DisplayControl.Announce(c);

                    // Generate URL.
                    if (c.ExternalLink.HasValue())
                    {
                        var link = await _linkResolver.ResolveAsync(c.ExternalLink);
                        if (link.Status == LinkStatus.Ok)
                        {
                            model.Url = link.Link;
                        }
                    }

                    if (model.Url.IsEmpty())
                    {
                        model.Url = _urlHelper.RouteUrl(nameof(Category), new { SeName = c.GetActiveSlug() });
                    }

                    files.TryGetValue(c.MediaFileId ?? 0, out var file);

                    model.PictureModel = new PictureModel
                    {
                        File = file,
                        ThumbSize = thumbSize,
                        Title = file?.File?.GetLocalized(x => x.Title)?.Value.NullEmpty() ?? string.Format(T("Media.Category.ImageLinkTitleFormat"), name),
                        Alt = file?.File?.GetLocalized(x => x.Alt)?.Value.NullEmpty() ?? string.Format(T("Media.Category.ImageAlternateTextFormat"), name),
                        NoFallback = _catalogSettings.HideCategoryDefaultPictures
                    };

                    _services.DisplayControl.Announce(file?.File);

                    return model;
                })
                .AsyncToList();
        }

        #endregion

        #region Product

        #endregion
    }
}