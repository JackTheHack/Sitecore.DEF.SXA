using Sitecore.DataExchange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sitecore.DEF.SXA.Steps.Settings
{
    public class AddLocalDatasourceSettings : IPlugin
    {
        public string DatasourceItemName { get; set; }
        public Guid IdentifierObjectLocation { get; set; }
    }
}
