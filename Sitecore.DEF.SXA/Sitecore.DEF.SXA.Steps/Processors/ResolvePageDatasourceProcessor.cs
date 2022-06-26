using Sitecore.DataExchange.Attributes;
using Sitecore.DataExchange.Contexts;
using Sitecore.DataExchange.Extensions;
using Sitecore.DataExchange.Models;
using Sitecore.DataExchange.Providers.Sc.Extensions;
using Sitecore.DataExchange.Providers.Sc.Plugins;
using Sitecore.DataExchange.Repositories;
using Sitecore.DEF.SXA.Steps.Settings;
using Sitecore.Services.Core.Diagnostics;
using Sitecore.Services.Core.Model;
using System;
using System.Linq;

namespace Sitecore.DEF.SXA.Steps.Processors
{
    [RequiredEndpointPlugins(new Type[] { typeof(ItemModelRepositorySettings) })]
    [RequiredPipelineStepPlugins(new Type[] { typeof(AddLocalDatasourceSettings) })]
    public class ResolvePageDatasourceProcessor : CustomResolveItemStepProcessor
    {
        private readonly string DataFolderName = "Data";
        private readonly Guid PageDataFolderTemplateId = Guid.Parse("{1C82E550-EBCD-4E5D-8ABD-D50D0809541E}");
        private IItemModelRepository _repositoryFromEndpoint;
        private Guid _dataFolderItemId;
        private AddLocalDatasourceSettings _settings;
        private PipelineIndexerSettings _pipelineIndexSettings;

        public ResolvePageDatasourceProcessor() : base()
        {

        }

        protected override void ProcessPipelineStep(PipelineStep pipelineStep = null, PipelineContext pipelineContext = null, ILogger logger = null)
        {
            _settings = pipelineStep.GetPlugin<AddLocalDatasourceSettings>();

            _pipelineIndexSettings = pipelineContext.GetPlugin<PipelineIndexerSettings>();

            var pageItem = this.GetObjectFromPipelineContext(_settings.IdentifierObjectLocation, pipelineContext, logger) as ItemModel;
            var pageItemId = pageItem.GetItemId();

            Endpoint endpoint = this.GetEndpoint(pipelineStep, pipelineContext, logger);
            _repositoryFromEndpoint = this.GetItemModelRepositoryFromEndpoint(endpoint);

            //Get page data folder, or create it if it doesnt exist
            _dataFolderItemId = GetOrCreateDataFolder(pageItemId);

            base.ProcessPipelineStep(pipelineStep, pipelineContext, logger);
        }

        private IItemModelRepository GetItemModelRepositoryFromEndpoint(
         Endpoint endpoint)
        {
            return endpoint.GetItemModelRepositorySettings()?.ItemModelRepository;
        }


        private Guid GetOrCreateDataFolder(Guid pageItemId)
        {
            var pageChildren = _repositoryFromEndpoint.GetChildren(pageItemId);

            var dataFolderItem = pageChildren.FirstOrDefault(i => (string)i[ItemModel.ItemName] == DataFolderName &&
                            i.GetTemplateId() == PageDataFolderTemplateId);

            if (dataFolderItem == null)
            {
                var dataFolder = new ItemModel();
                dataFolder[ItemModel.ItemName] = DataFolderName;
                dataFolder[ItemModel.TemplateID] = PageDataFolderTemplateId;
                dataFolder[ItemModel.ParentID] = pageItemId;

                return _repositoryFromEndpoint.Create(dataFolder);
            }
            else
            {
                return dataFolderItem.GetItemId();
            }
        }

        protected override object ConvertValueToIdentifier(object identifierValue, PipelineStep pipelineStep, PipelineContext pipelineContext, ILogger logger)
        {
            return GetNameValue();
        }

        private string GetNameValue()
        {
            var result = _settings.DatasourceItemName;
            if (_pipelineIndexSettings != null)
            {
                result = result.Replace("{INDEX}", _pipelineIndexSettings.Index.ToString());
            }
            return result;
        }

        protected override string ConvertValueForSearch(object value)
        {
            return GetNameValue();
        }

        protected override Guid GetParentItemIdForNewItem(IItemModelRepository repository, ResolveSitecoreItemSettings settings, PipelineContext pipelineContext, ILogger logger)
        {
            return _dataFolderItemId;
        }
    }
}
