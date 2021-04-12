using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Seo;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Stores;
using Nop.Data;
using Nop.Plugin.Misc.FillDBWithRandomData.Models;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.FillDBWithRandomData.Controllers
{
    [AutoValidateAntiforgeryToken]
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    public class FillDBWithRandomDataController : BasePluginController
    {
        #region Fields

        private readonly IAddressService _addressService;
        private readonly IEncryptionService _encryptionService;
        private readonly ICategoryService _categoryService;
        private readonly ICustomerService _customerService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly INopDataProvider _dataProvider;
        private readonly INotificationService _notificationService;
        private readonly IProductService _productService;
        private readonly IRepository<Address> _addressRepository;
        private readonly IRepository<Country> _countryRepository;
        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<CustomerPassword> _customerPasswordRepository;
        private readonly IRepository<CustomerRole> _customerRoleRepository;
        private readonly IRepository<GiftCard> _giftCardRepository;
        private readonly IRepository<Language> _languageRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<OrderItem> _orderItemRepository;
        private readonly IRepository<OrderNote> _orderNoteRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<ProductCategory> _productCategoryRepository;
        private readonly IRepository<StateProvince> _stateProvinceRepository;
        private readonly IRepository<Store> _storeRepository;
        private readonly IRepository<UrlRecord> _urlRecordRepository;
        private readonly ISettingService _settingService;
        private readonly Random _random;

        #endregion

        #region Ctor

        public FillDBWithRandomDataController(IAddressService addressService,
            IEncryptionService encryptionService,
            ICategoryService categoryService,
            ICustomerService customerService,
            IGenericAttributeService genericAttributeService,
            INopDataProvider dataProvider,
            INotificationService notificationService,
            IProductService productService,
            IRepository<Address> addressRepository,
            IRepository<Country> countryRepository,
            IRepository<Customer> customerRepository,
            IRepository<CustomerPassword> customerPasswordRepository,
            IRepository<CustomerRole> customerRoleRepository,
            IRepository<GiftCard> giftCardRepository,
            IRepository<Language> languageRepository,
            IRepository<Order> orderRepository,
            IRepository<OrderItem> orderItemRepository,
            IRepository<OrderNote> orderNoteRepository,
            IRepository<Product> productRepository,
            IRepository<ProductCategory> productCategoryRepository,
            IRepository<StateProvince> stateProvinceRepository,
            IRepository<Store> storeRepository,
            IRepository<UrlRecord> urlRecordRepository,
            ISettingService settingService)
        {
            _addressService = addressService;
            _encryptionService = encryptionService;
            _categoryService = categoryService;
            _customerService = customerService;
            _genericAttributeService = genericAttributeService;
            _dataProvider = dataProvider;
            _notificationService = notificationService;
            _productService = productService;
            _addressRepository = addressRepository;
            _countryRepository = countryRepository;
            _customerRepository = customerRepository;
            _customerPasswordRepository = customerPasswordRepository;
            _customerRoleRepository = customerRoleRepository;
            _giftCardRepository = giftCardRepository;
            _languageRepository = languageRepository;
            _orderRepository = orderRepository;
            _orderItemRepository = orderItemRepository;
            _orderNoteRepository = orderNoteRepository;
            _productRepository = productRepository;
            _productCategoryRepository = productCategoryRepository;
            _stateProvinceRepository = stateProvinceRepository;
            _storeRepository = storeRepository;
            _urlRecordRepository = urlRecordRepository;
            _settingService = settingService;

            _random = new Random();
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Prepare configuration model
        /// </summary>
        /// <param name="model">Model</param>
        private async Task PrepareModel(ConfigurationModel model)
        {
            var fillDBWithRandomDataSettings = await _settingService.LoadSettingAsync<FillDBWithRandomDataSettings>();

            model.CountCategories = fillDBWithRandomDataSettings.CountCategories;
            model.CountProducts = fillDBWithRandomDataSettings.CountProduct;
            model.CountOrders = fillDBWithRandomDataSettings.CountOrders;
            model.CountCustomers = fillDBWithRandomDataSettings.CountCustomers;
        }

        protected virtual async Task<T> InsertInstallationData<T>(T entity) where T : BaseEntity
        {
            return await _dataProvider.InsertEntityAsync(entity);
        }

        protected virtual async Task InsertInstallationData<T>(params T[] entities) where T : BaseEntity
        {
            foreach (var entity in entities)
            {
                await InsertInstallationData(entity);
            }
        }

        #endregion

        #region Methods

        public async Task<IActionResult> Configure()
        {
            var model = new ConfigurationModel();
            await PrepareModel(model);

            return View("~/Plugins/Misc.FillDBWithRandomData/Views/Configure.cshtml", model);
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("create_data")]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("Configure");

            var customers = await (await _customerService.GetAllCustomersAsync())
                .WhereAwait(async customer => await _customerService.IsRegisteredAsync(customer)).ToListAsync();

            var categories = await _categoryService.GetAllCategoriesAsync();
            
            //default store
            var defaultStore = _storeRepository.Table.FirstOrDefault();
            if (defaultStore == null)
                throw new Exception("No default store could be loaded");

            #region Insert category

            try
            {
                for (var i = 0; i < model.CountCategories; i++)
                {
                    //_encryptionService.CreateSaltKey(20)
                    var category = new Category
                    {
                        Name = $"Sample_Category_{i}",
                        CategoryTemplateId = 1,
                        PageSize = 6,
                        AllowCustomersToSelectPageSize = true,
                        PageSizeOptions = "6, 3, 9",
                        IncludeInTopMenu = true,
                        Published = true,
                        DisplayOrder = 1,
                        CreatedOnUtc = DateTime.UtcNow,
                        UpdatedOnUtc = DateTime.UtcNow
                    };
                                       
                    category.ParentCategoryId = categories[_random.Next(categories.Count)].Id;

                    await _categoryService.InsertCategoryAsync(category);

                    //search engine names for category
                    await _urlRecordRepository.InsertAsync(new UrlRecord
                    {
                        EntityId = category.Id,
                        EntityName = nameof(Category),
                        LanguageId = 0,
                        IsActive = true,
                        Slug = $"Sample_Category_{i}".ToLower()
                    });
                }
            }
            catch (Exception exc)
            {
                await _notificationService.ErrorNotificationAsync(exc);
                return RedirectToAction("Configure");
            }

            #endregion

            #region Insert products

            try
            {
                //_encryptionService.CreateSaltKey(20),
                for (var i = 0; i < model.CountProducts; i++)
                {
                    var product = new Product
                    {
                        ProductType = ProductType.SimpleProduct,
                        VisibleIndividually = true,
                        Name = $"Sample_Product_{i}",
                        Sku = _encryptionService.CreateSaltKey(10),
                        ShortDescription = _encryptionService.CreateSaltKey(50),
                        FullDescription = $"<p>{_encryptionService.CreateSaltKey(300)}</p>",
                        ProductTemplateId = 1,
                        AllowCustomerReviews = true,
                        Price = new decimal(_random.Next(1000, 1000000) / 10),
                        IsShipEnabled = _random.Next(2) == 1,
                        IsFreeShipping = _random.Next(2) == 1,
                        Weight = _random.Next(1, 20),
                        Length = _random.Next(1, 20),
                        Width = _random.Next(1, 20),
                        Height = _random.Next(1, 20),
                        TaxCategoryId = 0,
                        ManageInventoryMethod = ManageInventoryMethod.ManageStock,
                        StockQuantity = _random.Next(1000),
                        NotifyAdminForQuantityBelow = 1,
                        AllowBackInStockSubscriptions = false,
                        DisplayStockAvailability = true,
                        LowStockActivity = LowStockActivity.DisableBuyButton,
                        BackorderMode = BackorderMode.NoBackorders,
                        OrderMinimumQuantity = 1,
                        OrderMaximumQuantity = _random.Next(10000),
                        Published = true,
                        ShowOnHomepage = false,
                        MarkAsNew = true,
                        CreatedOnUtc = DateTime.UtcNow,
                        UpdatedOnUtc = DateTime.UtcNow
                    };

                    await _productService.InsertProductAsync(product);

                    //search engine names for product
                    await _urlRecordRepository.InsertAsync(new UrlRecord
                    {
                        EntityId = product.Id,
                        EntityName = nameof(Product),
                        LanguageId = 0,
                        IsActive = true,
                        Slug = $"Sample_Product_{i}".ToLower()
                    });

                    for (var j = 0; j < _random.Next(5); j++)
                    {
                        var categoryId = categories[_random.Next(categories.Count)].Id;

                        await _productCategoryRepository.InsertAsync(new ProductCategory
                        {
                            ProductId = product.Id,
                            CategoryId = categoryId,
                            DisplayOrder = _random.Next(1, 1000)
                        });
                    }
                }
            }
            catch (Exception exc)
            {
                await _notificationService.ErrorNotificationAsync(exc);
                return RedirectToAction("Configure");
            }
            #endregion

            #region Insert customers

            try
            {
                var crRegistered = _customerRoleRepository.Table.FirstOrDefault(customerRole =>
                    customerRole.SystemName == NopCustomerDefaults.RegisteredRoleName);

                if (crRegistered == null)
                    throw new ArgumentNullException(nameof(crRegistered));

                for (var i = 0; i < model.CountCustomers; i++)
                {
                    var userEmail = $"sample_user_{i}@nopCommerce.com";

                    var curUser = new Customer
                    {
                        CustomerGuid = Guid.NewGuid(),
                        Email = userEmail,
                        Username = userEmail,
                        Active = true,
                        CreatedOnUtc = DateTime.UtcNow,
                        LastActivityDateUtc = DateTime.UtcNow,
                        RegisteredInStoreId = defaultStore.Id
                    };

                    var defaultUserAddress = await InsertInstallationData(
                        new Address
                        {
                            FirstName = $"FirstName_{i}",
                            LastName = $"LastName_{i}",
                            PhoneNumber = "87654321",
                            Email = userEmail,
                            FaxNumber = string.Empty,
                            Company = _encryptionService.CreateSaltKey(10),
                            Address1 = "750 Bel Air Rd.",
                            Address2 = string.Empty,
                            City = "Los Angeles",
                            StateProvinceId = _stateProvinceRepository.Table.FirstOrDefault(sp => sp.Name == "California")?.Id,
                            CountryId = _countryRepository.Table.FirstOrDefault(c => c.ThreeLetterIsoCode == "USA")?.Id,
                            ZipPostalCode = "90077",
                            CreatedOnUtc = DateTime.UtcNow
                        });

                    curUser.BillingAddressId = defaultUserAddress.Id;
                    curUser.ShippingAddressId = defaultUserAddress.Id;

                    await _customerRepository.InsertAsync(curUser);

                    await InsertInstallationData(new CustomerAddressMapping { CustomerId = curUser.Id, AddressId = defaultUserAddress.Id });
                    await InsertInstallationData(new CustomerCustomerRoleMapping { CustomerId = curUser.Id, CustomerRoleId = crRegistered.Id });

                    //set default customer name
                    await _genericAttributeService.SaveAttributeAsync(curUser, NopCustomerDefaults.FirstNameAttribute, defaultUserAddress.FirstName);
                    await _genericAttributeService.SaveAttributeAsync(curUser, NopCustomerDefaults.LastNameAttribute, defaultUserAddress.LastName);

                    //set customer password
                    await _customerPasswordRepository.InsertAsync(new CustomerPassword
                    {
                        CustomerId = curUser.Id,
                        Password = "123456",
                        PasswordFormat = PasswordFormat.Clear,
                        PasswordSalt = string.Empty,
                        CreatedOnUtc = DateTime.UtcNow
                    });
                }
            }
            catch (Exception exc)
            {
                await _notificationService.ErrorNotificationAsync(exc);
                return RedirectToAction("Configure");
            }
            #endregion

            #region Insert orders

            try
            {
                var allCustomers = await _customerService.GetAllCustomersAsync();
                var languageId = _languageRepository.Table.First().Id;
                var allProducts = _productRepository.Table.Count();

                for (var i = 0; i < model.CountOrders; i++)
                {
                    var randomCustomer = allCustomers[_random.Next(allCustomers.Count)];

                    var randomCustomerBillingAddress = randomCustomer.BillingAddressId;
                    var randomCustomerShippingAddress = randomCustomer.ShippingAddressId;

                    var order = new Order
                    {
                        StoreId = defaultStore.Id,
                        OrderGuid = Guid.NewGuid(),
                        CustomerId = randomCustomer.Id,
                        CustomerLanguageId = languageId,
                        CustomerIp = "127.0.0.1",
                        OrderSubtotalInclTax = 1855M,
                        OrderSubtotalExclTax = 1855M,
                        OrderSubTotalDiscountInclTax = decimal.Zero,
                        OrderSubTotalDiscountExclTax = decimal.Zero,
                        OrderShippingInclTax = decimal.Zero,
                        OrderShippingExclTax = decimal.Zero,
                        PaymentMethodAdditionalFeeInclTax = decimal.Zero,
                        PaymentMethodAdditionalFeeExclTax = decimal.Zero,
                        TaxRates = "0:0;",
                        OrderTax = decimal.Zero,
                        OrderTotal = 1855M,
                        RefundedAmount = decimal.Zero,
                        OrderDiscount = decimal.Zero,
                        CheckoutAttributeDescription = string.Empty,
                        CheckoutAttributesXml = string.Empty,
                        CustomerCurrencyCode = "USD",
                        CurrencyRate = 1M,
                        AffiliateId = 0,
                        OrderStatus = OrderStatus.Complete,
                        AllowStoringCreditCardNumber = false,
                        CardType = string.Empty,
                        CardName = string.Empty,
                        CardNumber = string.Empty,
                        MaskedCreditCardNumber = string.Empty,
                        CardCvv2 = string.Empty,
                        CardExpirationMonth = string.Empty,
                        CardExpirationYear = string.Empty,
                        PaymentMethodSystemName = "Payments.CheckMoneyOrder",
                        AuthorizationTransactionId = string.Empty,
                        AuthorizationTransactionCode = string.Empty,
                        AuthorizationTransactionResult = string.Empty,
                        CaptureTransactionId = string.Empty,
                        CaptureTransactionResult = string.Empty,
                        SubscriptionTransactionId = string.Empty,
                        PaymentStatus = PaymentStatus.Paid,
                        PaidDateUtc = DateTime.UtcNow,
                        BillingAddressId = (randomCustomerBillingAddress ?? randomCustomerShippingAddress) ?? _addressRepository.Table.First().Id,
                        ShippingAddressId = (randomCustomerShippingAddress ?? randomCustomerBillingAddress) ?? _addressRepository.Table.First().Id,
                        ShippingStatus = ShippingStatus.NotYetShipped,
                        ShippingMethod = "Ground",
                        PickupInStore = false,
                        ShippingRateComputationMethodSystemName = "Shipping.FixedByWeightByTotal",
                        CustomValuesXml = string.Empty,
                        VatNumber = string.Empty,
                        CreatedOnUtc = DateTime.UtcNow,
                        CustomOrderNumber = string.Empty
                    };

                    await _orderRepository.InsertAsync(order);
                    order.CustomOrderNumber = order.Id.ToString();
                    await _orderRepository.UpdateAsync(order);

                    //Add order items
                    for (var j = 0; j < _random.Next(1, 5); j++)
                    {
                        var curTax = (decimal)_random.Next(1, 20);

                        var orderItem = new OrderItem
                        {
                            OrderItemGuid = Guid.NewGuid(),
                            OrderId = order.Id,
                            ProductId = _productRepository.Table.First(p => p.Id == _random.Next(1, allProducts)).Id,
                            UnitPriceInclTax = curTax,
                            UnitPriceExclTax = curTax,
                            PriceInclTax = curTax,
                            PriceExclTax = curTax,
                            OriginalProductCost = decimal.Zero,
                            AttributeDescription = string.Empty,
                            AttributesXml = string.Empty,
                            Quantity = _random.Next(1, 20),
                            DiscountAmountInclTax = decimal.Zero,
                            DiscountAmountExclTax = decimal.Zero,
                            DownloadCount = 0,
                            IsDownloadActivated = false,
                            LicenseDownloadId = 0,
                            ItemWeight = null,
                            RentalStartDateUtc = null,
                            RentalEndDateUtc = null
                        };
                        await _orderItemRepository.InsertAsync(orderItem);

                        //Add gift card
                        if (_random.Next(3) == 1)
                        {
                            var giftCard = new GiftCard
                            {
                                GiftCardType = GiftCardType.Virtual,
                                PurchasedWithOrderItemId = orderItem.Id,
                                Amount = _random.Next(50),
                                IsGiftCardActivated = false,
                                GiftCardCouponCode = string.Empty,
                                RecipientName = "Brenda Lindgren",
                                RecipientEmail = "brenda_lindgren@nopCommerce.com",
                                SenderName = "Steve Gates",
                                SenderEmail = "steve_gates@nopCommerce.com",
                                Message = _encryptionService.CreateSaltKey(50),
                                IsRecipientNotified = false,
                                CreatedOnUtc = DateTime.UtcNow
                            };
                            await _giftCardRepository.InsertAsync(giftCard);
                        }
                    }

                    //Add order notes
                    for (var k = 0; k < 4; k++)
                    {
                        var curNote = string.Empty;

                        _ = k switch
                        {
                            0 => curNote = "Order placed",
                            1 => curNote = "Order paid",
                            2 => curNote = "Order shipped",
                            3 => curNote = "Order delivered",
                            _ => throw new NotImplementedException()
                        };

                        await _orderNoteRepository.InsertAsync(new OrderNote
                        {
                            CreatedOnUtc = DateTime.UtcNow,
                            Note = curNote,
                            OrderId = order.Id
                        });
                    }
                }
            }
            catch (Exception exc)
            {
                await _notificationService.ErrorNotificationAsync(exc);
                return RedirectToAction("Configure");
            }
            #endregion

            _notificationService.SuccessNotification("Generate complete!");

            var fillDBWithRandomDataSettings = await _settingService.LoadSettingAsync<FillDBWithRandomDataSettings>();

            fillDBWithRandomDataSettings.CountCategories = model.CountCategories;
            fillDBWithRandomDataSettings.CountProduct = model.CountProducts;
            fillDBWithRandomDataSettings.CountOrders = model.CountOrders;
            fillDBWithRandomDataSettings.CountCustomers = model.CountCustomers;

            await _settingService.SaveSettingAsync(fillDBWithRandomDataSettings);

            return RedirectToAction("Configure");
        }

        #endregion
    }
}