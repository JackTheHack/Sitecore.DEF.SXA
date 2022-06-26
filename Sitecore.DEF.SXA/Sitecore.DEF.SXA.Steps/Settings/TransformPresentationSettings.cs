using Sitecore.DataExchange;
using System;

namespace Sitecore.DEF.SXA.Steps.Settings
{
    public class TransformPresentationSettings : IPlugin
    {
        public Guid SourceObjectLocation { get; set; }
        public Guid SourceDatasourceObjectLocation { get; set; }
        public Guid TargetObjectLocation { get; set; }
        public Guid RenderingId { get; set; }
        public string PlaceholderName { get; set; }
    }
}
