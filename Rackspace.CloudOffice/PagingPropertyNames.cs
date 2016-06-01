using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rackspace.CloudOffice
{
    public class PagingPropertyNames
    {
        public static PagingPropertyNames Default => new PagingPropertyNames
        {
            ItemsName = "items",
            OffsetName = "offset",
            PageSizeName = "size",
            TotalName = "total",
        };

        public string ItemsName { get; set; }
        public string OffsetName { get; set; }
        public string PageSizeName { get; set; }
        public string TotalName { get; set; }
    }
}
