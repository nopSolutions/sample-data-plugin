using Nop.Web.Framework.Models;

namespace Nop.Plugin.Misc.FillDBWithRandomData.Models
{
    /// <summary>
    /// Represents a configuration model
    /// </summary>
    public record ConfigurationModel : BaseNopModel
    {
        #region Properties

        public int CountCategories { get; set; }

        public int CountProducts { get; set; }

        public int CountOrders { get; set; }

        public int CountCustomers { get; set; }
        
        #endregion
    }
}