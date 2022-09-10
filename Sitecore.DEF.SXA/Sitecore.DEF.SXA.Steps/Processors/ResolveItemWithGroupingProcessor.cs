using System;
using Sitecore.DataExchange.Attributes;
using Sitecore.DataExchange.Contexts;
using Sitecore.DataExchange.Extensions;
using Sitecore.DataExchange.Models;
using Sitecore.DataExchange.Plugins;
using Sitecore.DataExchange.Providers.Sc.Extensions;
using Sitecore.DataExchange.Providers.Sc.Plugins;
using Sitecore.DataExchange.Repositories;
using System.Linq;
using Sitecore.Services.Core.Diagnostics;
using Sitecore.Services.Core.Model;
using System.Collections.Generic;
using Sitecore.DEF.SXA.Steps.Settings;

namespace Sitecore.DEF.SXA.Steps.Processors
{
    [RequiredEndpointPlugins(new Type[] { typeof(ItemModelRepositorySettings) })]
    [RequiredPipelineStepPlugins(new Type[] { typeof(ResolveItemWithGroupingSettings) })]
    public class RBNZResolveItemWithGroupingProcessor : CustomResolveItemStepProcessor
    {
        private IItemModelRepository _repositoryFromEndpoint;
        private Guid _dataFolderItemId;
        private ResolveItemWithGroupingSettings _settings;

        public RBNZResolveItemWithGroupingProcessor() : base()
        {

        }

        protected override void ProcessPipelineStep(PipelineStep pipelineStep = null, PipelineContext pipelineContext = null, ILogger logger = null)
        {
            _settings = pipelineStep.GetPlugin<ResolveItemWithGroupingSettings>();

            var resolveIdSettings = pipelineStep.GetPlugin<ResolveIdentifierSettings>();
            var resolveObjSettings = pipelineStep.GetPlugin<ResolveSitecoreItemSettings>();

            var sourceItem = this.GetObjectFromPipelineContext(resolveIdSettings.IdentifierObjectLocation, pipelineContext, logger) as ItemModel;

            var dateValueString = sourceItem.GetFieldValueAsString(_settings.GroupDateFieldName);

            var dateValue = Sitecore.DateUtil.ParseDateTime(dateValueString, DateTime.Now);
            dateValue = Sitecore.DateUtil.ToServerTime(dateValue);

            var groupingValue = 
                !string.IsNullOrWhiteSpace(_settings.GroupFieldName) ?
                sourceItem.GetFieldValueAsString(_settings.GroupFieldName) : 
                dateValue.Month.ToString();

            var pageItemId = resolveObjSettings.ParentItemIdItem;

            Endpoint endpoint = this.GetEndpoint(pipelineStep, pipelineContext, logger);
            
            _repositoryFromEndpoint = this.GetItemModelRepositoryFromEndpoint(endpoint);

            //Get page data folder, or create it if it doesnt exist
            if(string.IsNullOrWhiteSpace(_settings.GroupFieldName))
            {
                _dataFolderItemId = GetOrCreateGroupingFolder(pageItemId,
                _settings.GroupFolderTemplate,
                groupingValue,
                dateValue.Year.ToString());
            }
            else
            {
                _dataFolderItemId = GetOrCreateGroupingFolder(pageItemId,
                _settings.GroupFolderTemplate,
                dateValue.Year.ToString(),
                groupingValue);
            }

            base.ProcessPipelineStep(pipelineStep, pipelineContext, logger);
        }       

        private IItemModelRepository GetItemModelRepositoryFromEndpoint(
         Endpoint endpoint)
        {
            return endpoint.GetItemModelRepositorySettings()?.ItemModelRepository;
        }


        private Guid GetOrCreateGroupingFolder(Guid pageItemId, Guid folderTemplate, 
            string groupingFolderLvl2, string groupingFolderLvl1)
        {
            var pageChildren = _repositoryFromEndpoint.GetChildren(pageItemId);

            Guid? groupingFolder = pageItemId;
            ItemModel dataFolderItem = null;

            if(!string.IsNullOrWhiteSpace(groupingFolderLvl1))
            {
                dataFolderItem = pageChildren.FirstOrDefault(i =>
                            (string)i[ItemModel.ItemName] == groupingFolderLvl1 &&
                            i.GetTemplateId() == folderTemplate);

                if (dataFolderItem == null)
                {
                    var dataFolder = new ItemModel();
                    dataFolder[ItemModel.ItemName] = groupingFolderLvl1;
                    dataFolder[ItemModel.TemplateID] = folderTemplate;
                    dataFolder[ItemModel.ParentID] = pageItemId;

                    groupingFolder = _repositoryFromEndpoint.Create(dataFolder);
                    pageChildren = new List<ItemModel>();
                }
                else
                {
                    groupingFolder = dataFolderItem.GetItemId();
                    pageChildren = _repositoryFromEndpoint.GetChildren(groupingFolder.Value);
                }                
            }

            dataFolderItem = pageChildren.FirstOrDefault(i =>
                            (string)i[ItemModel.ItemName] == groupingFolderLvl2 &&
                            i.GetTemplateId() == folderTemplate);

            if (dataFolderItem == null)
            {
                var dataFolder = new ItemModel();
                dataFolder[ItemModel.ItemName] = groupingFolderLvl2;
                dataFolder[ItemModel.TemplateID] = folderTemplate;
                dataFolder[ItemModel.ParentID] = groupingFolder;

                groupingFolder = _repositoryFromEndpoint.Create(dataFolder);
            }
            else
            {
                groupingFolder = dataFolderItem.GetItemId();
            }

            return groupingFolder.GetValueOrDefault(Guid.Empty);            
        }        

        protected override Guid GetParentItemIdForNewItem(IItemModelRepository repository, ResolveSitecoreItemSettings settings, PipelineContext pipelineContext, ILogger logger)
        {
            return _dataFolderItemId;
        }
    }
}