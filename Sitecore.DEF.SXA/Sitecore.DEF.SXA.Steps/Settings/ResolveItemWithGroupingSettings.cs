using Sitecore.DataExchange;
using System;

namespace Sitecore.DEF.SXA.Steps.Settings
{
    public class ResolveItemWithGroupingSettings : IPlugin
    {
        public Guid GroupFolderTemplate { get; set; }
        public string GroupDateFieldName { get; set; }
        public string GroupFieldName { get; set; }
    }
}