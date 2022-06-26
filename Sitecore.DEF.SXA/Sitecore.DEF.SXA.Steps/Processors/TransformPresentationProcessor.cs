using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.DataExchange.Attributes;
using Sitecore.DataExchange.Contexts;
using Sitecore.DataExchange.Extensions;
using Sitecore.DataExchange.Models;
using Sitecore.DataExchange.Processors.PipelineSteps;
using Sitecore.DataExchange.Providers.Sc.Plugins;
using Sitecore.DEF.SXA.Steps.Constants;
using Sitecore.DEF.SXA.Steps.Settings;
using Sitecore.Layouts;
using Sitecore.Services.Core.Diagnostics;
using Sitecore.Services.Core.Model;
using System;

namespace Sitecore.DEF.SXA.Steps.Processors
{
    [RequiredEndpointPlugins(new Type[] { typeof(ItemModelRepositorySettings) })]
    [RequiredPipelineStepPlugins(new Type[] { typeof(TransformPresentationSettings) })]
    public class TransformPresentationProcessor : BasePipelineStepProcessor
    {
        private TransformPresentationSettings _settings;

        protected override void ProcessPipelineStep(PipelineStep pipelineStep = null, 
            PipelineContext pipelineContext = null, ILogger logger = null)
        {
            _settings = pipelineStep.GetPlugin<TransformPresentationSettings>();

            var sourceDatasourceItem = this.GetObjectFromPipelineContext(_settings.SourceDatasourceObjectLocation, pipelineContext, logger) as ItemModel;
            var targetObject = this.GetObjectFromPipelineContext(_settings.TargetObjectLocation, pipelineContext, logger) as ItemModel;

            var datasource = sourceDatasourceItem != null ? sourceDatasourceItem[ItemModel.ItemPath].ToString() : null;
            
            var presentationValue = GetLayoutWithAddedRendering(targetObject, datasource);

            ApplyPresentationLayout(targetObject, presentationValue);

            Logger.Info($"Added the rendering {_settings.RenderingId} for item {targetObject.GetItemId()}");
        }

        private static void ApplyPresentationLayout(ItemModel targetObject, string presentationValue)
        {
            var targetSitecoreItem = Sitecore.Data.Database.GetDatabase("master").GetItem(ID.Parse(targetObject[ItemModel.ItemID]));

            targetSitecoreItem.Editing.BeginEdit();
            LayoutField.SetFieldValue(targetSitecoreItem.Fields[DataExchangeConstants.FinalRenderingsFieldName], presentationValue);
            targetSitecoreItem.Editing.EndEdit();
        }

        private string GetLayoutWithAddedRendering(ItemModel itemModel, string datasource)
        {
            var item = Sitecore.Data.Database.GetDatabase("master")
                .GetItem(ID.Parse(itemModel[ItemModel.ItemID]));

            LayoutDefinition layout = LayoutDefinition.Parse(
                LayoutField.GetFieldValue(item.Fields[DataExchangeConstants.FinalRenderingsFieldName]));

            var deviceLayout = layout.GetDevice(DataExchangeConstants.DefaultDeviceID);

            deviceLayout.AddRendering(new RenderingDefinition()
            {
                ItemID = _settings.RenderingId.ToString(),
                Placeholder = _settings.PlaceholderName,
                Datasource = datasource
            });

            var resultXml = layout.ToXml();

            return resultXml;
        }
    }
}
