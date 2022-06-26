using Sitecore.Data;
using Sitecore.DataExchange.Attributes;
using Sitecore.DataExchange.Contexts;
using Sitecore.DataExchange.Extensions;
using Sitecore.DataExchange.Models;
using Sitecore.DataExchange.Processors.PipelineSteps;
using Sitecore.DataExchange.Providers.Sc.Plugins;
using Sitecore.DEF.SXA.Steps.Constants;
using Sitecore.DEF.SXA.Steps.Settings;
using Sitecore.Services.Core.Diagnostics;
using Sitecore.Services.Core.Model;
using System;

namespace Sitecore.DEF.SXA.Steps.Processors
{
    [RequiredEndpointPlugins(new Type[] { typeof(ItemModelRepositorySettings) })]
    [RequiredPipelineStepPlugins(new Type[] { typeof(ResetPresentationSettings) })]
    public class ResetPresentationProcessor : BasePipelineStepProcessor
    {
        protected override void ProcessPipelineStep(PipelineStep pipelineStep = null, 
            PipelineContext pipelineContext = null, ILogger logger = null)
        {
            var settings = pipelineStep.GetPlugin<ResetPresentationSettings>();

            var targetObject = this.GetObjectFromPipelineContext(settings.TargetObjectLocation, pipelineContext, logger) as ItemModel;

            ResetPresentationLayout(targetObject);

            Logger.Info($"Reset the presentation details for item {targetObject.GetItemId()}");
        }

        private static void ResetPresentationLayout(ItemModel targetObject)
        {
            System.Diagnostics.Debug.Assert(targetObject != null, "targetObject can't be null.");

            var targetSitecoreItem = Sitecore.Data.Database.GetDatabase("master").GetItem(ID.Parse(targetObject[ItemModel.ItemID]));
            targetSitecoreItem.Editing.BeginEdit();
            targetSitecoreItem.Fields[DataExchangeConstants.FinalRenderingsFieldName].Reset();
            targetSitecoreItem.Editing.EndEdit();
        }
    }
}
