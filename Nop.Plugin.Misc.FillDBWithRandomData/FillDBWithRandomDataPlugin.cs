using Nop.Core;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Plugins;

namespace Nop.Plugin.Misc.FillDBWithRandomData
{
    /// <summary>
    /// Represents the FillDBWithRandomData plugin
    /// </summary>
    public class FillDBWithRandomDataPlugin : BasePlugin, IMiscPlugin
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;

        #endregion

        #region Ctor

        public FillDBWithRandomDataPlugin(ISettingService settingService, 
            IWebHelper webHelper)
        {
            _settingService = settingService;
            _webHelper = webHelper;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/FillDBWithRandomData/Configure";
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override void Install()
        {
            //settings
            _settingService.SaveSetting(new FillDBWithRandomDataSettings
            {
                CountCategories = FillDBWithRandomDataDefaults.CountCategories,
                CountProduct = FillDBWithRandomDataDefaults.CountProduct,
                CountOrders = FillDBWithRandomDataDefaults.CountOrders,
                CountCustomers = FillDBWithRandomDataDefaults.CountCustomers
            });

            //locales

            base.Install();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<FillDBWithRandomDataSettings>();

            //locales

            base.Uninstall();
        }

        #endregion
    }
}