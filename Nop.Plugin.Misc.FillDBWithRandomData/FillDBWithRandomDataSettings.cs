using Nop.Core.Configuration;

namespace Nop.Plugin.Misc.FillDBWithRandomData
{
    public class FillDBWithRandomDataSettings : ISettings
    {
        public int CountCategories { get; set; }

        public int CountProduct { get; set; }

        public int CountOrders { get; set; }

        public int CountCustomers { get; set; }

    }
}
