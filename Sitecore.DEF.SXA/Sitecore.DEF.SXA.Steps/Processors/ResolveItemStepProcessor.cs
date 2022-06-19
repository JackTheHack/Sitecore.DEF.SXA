using Sitecore.DataExchange.Attributes;
using Sitecore.DataExchange.Contexts;
using Sitecore.DataExchange.DataAccess;
using Sitecore.DataExchange.Extensions;
using Sitecore.DataExchange.Models;
using Sitecore.DataExchange.Providers.Sc.DataAccess.Readers;
using Sitecore.DataExchange.Providers.Sc.Extensions;
using Sitecore.DataExchange.Providers.Sc.Plugins;
using Sitecore.DataExchange.Providers.Sc.Processors.PipelineSteps;
using Sitecore.DataExchange.Repositories;
using Sitecore.DEF.SXA.Steps.Settings;
using Sitecore.Exceptions;
using Sitecore.Services.Core.Diagnostics;
using Sitecore.Services.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sitecore.DEF.SXA.Steps.Processors
{
    [RequiredPipelineStepPlugins(new Type[] { typeof(ResolveSitecoreItemSettings) })]
    [RequiredEndpointPlugins(new Type[] { typeof(ItemModelRepositorySettings) })]
    public class CustomResolveItemStepProcessor : ResolveSitecoreItemStepProcessor
    {
        public override object FindExistingObject(
     object identifierValue,
     PipelineStep pipelineStep,
     PipelineContext pipelineContext,
     ILogger logger)
        {
            if (identifierValue == null)
                throw new ArgumentException("The value cannot be null.", nameof(identifierValue));
            Endpoint endpoint = this.GetEndpoint(pipelineStep, pipelineContext, logger);
            if (endpoint == null)
                throw new ArgumentNullException("endpoint");
            if (pipelineStep == null)
                throw new ArgumentNullException(nameof(pipelineStep));
            if (pipelineContext == null)
                throw new ArgumentNullException(nameof(pipelineContext));
            IItemModelRepository repositoryFromEndpoint = this.GetItemModelRepositoryFromEndpoint(endpoint);
            if (repositoryFromEndpoint == null)
                return (object)null;
            ResolveSitecoreItemSettings sitecoreItemSettings = pipelineStep.GetResolveSitecoreItemSettings();
            if (sitecoreItemSettings == null)
                return (object)null;
            ItemModel itemModel = this.DoSearch(identifierValue, sitecoreItemSettings, repositoryFromEndpoint, pipelineContext, logger);
            if (itemModel == null)
                return (object)null;
            this.Log(new Action<string>(logger.Debug), pipelineContext, "Item was resolved.", new string[2]
            {
        string.Format("identifier: {0}", identifierValue),
        string.Format("item id: {0}", itemModel["ItemID"])
            });
            this.SetRepositoryStatusSettings(RepositoryObjectStatus.Exists, pipelineContext);
            return (object)itemModel;
        }

        public override object CreateNewObject(
          object identifierValue,
          PipelineStep pipelineStep,
          PipelineContext pipelineContext,
          ILogger logger)
        {
            if (identifierValue == null)
                throw new ArgumentException("The value cannot be null.", nameof(identifierValue));
            Endpoint endpoint = this.GetEndpoint(pipelineStep, pipelineContext, logger);
            if (endpoint == null)
                throw new ArgumentNullException("endpoint");
            if (pipelineStep == null)
                throw new ArgumentNullException(nameof(pipelineStep));
            if (pipelineContext == null)
                throw new ArgumentNullException(nameof(pipelineContext));
            IItemModelRepository repositoryFromEndpoint = this.GetItemModelRepositoryFromEndpoint(endpoint);
            if (repositoryFromEndpoint == null)
                return (object)null;
            ResolveSitecoreItemSettings sitecoreItemSettings = pipelineStep.GetResolveSitecoreItemSettings();
            if (sitecoreItemSettings == null)
                return (object)null;
            ItemModel newItem = this.CreateNewItem(this.GetIdentifierObject(pipelineStep, pipelineContext, logger), repositoryFromEndpoint, sitecoreItemSettings, pipelineContext, logger);
            this.SetRepositoryStatusSettings(RepositoryObjectStatus.DoesNotExist, pipelineContext);
            return (object)newItem;
        }

        private IItemModelRepository GetItemModelRepositoryFromEndpoint(
          Endpoint endpoint)
        {
            return endpoint.GetItemModelRepositorySettings()?.ItemModelRepository;
        }

        protected override ItemModel DoSearch(
          object value,
          ResolveSitecoreItemSettings resolveItemSettings,
          IItemModelRepository repository,
          PipelineContext pipelineContext,
          ILogger logger)
        {
            if (!(this.GetValueReader(resolveItemSettings.MatchingFieldValueAccessor) is SitecoreItemFieldReader valueReader))
            {
                this.Log(new Action<string>(logger.Error), pipelineContext, "The matching field value accessor is not a valid Sitecore item field reader.", Array.Empty<string>());
                return (ItemModel)null;
            }
            string str = this.ConvertValueForSearch(value);
            this.Log(new Action<string>(logger.Debug), pipelineContext, "Value converted for search.", new string[3]
            {
        "field: " + valueReader.FieldName,
        string.Format("original value: {0}", value),
        "converted value: " + str
            });
            this.Log(new Action<string>(logger.Debug), pipelineContext, "Starting search for item.", new string[2]
            {
        "field: " + valueReader.FieldName,
        "value: " + str
            });
            ItemSearchSettings settings = new ItemSearchSettings();
            Guid itemIdForNewItem = this.GetParentItemIdForNewItem(repository, resolveItemSettings, pipelineContext, logger);
            if (itemIdForNewItem != Guid.Empty)
                settings.RootItemIds.Add(itemIdForNewItem);
            settings.SearchFilters.Add(new SearchFilter()
            {
                //!!! CUSTOM IMPLEMENTATION START
                //!!! Search object based on name
                FieldName = "_name",
                Value = str
                //!!! CUSTOM IMPLEMENTATION END
            });
            IEnumerable<ItemModel> source = repository.Search(settings);
            //!!! CUSTOM IMPLEMENTATION START
            //!!! Ensure that the name is exactly the same to avoid duplicates 
            return source == null ? (ItemModel)null : source.FirstOrDefault(i => (string)i[ItemModel.ItemName] == str);
            //!!! CUSTOM IMPLEMENTATION END
        }

        protected override string ConvertValueForSearch(object value) => value == null ? string.Empty : value.ToString();

        protected override Guid GetParentItemIdForNewItem(
          IItemModelRepository repository,
          ResolveSitecoreItemSettings settings,
          PipelineContext pipelineContext,
          ILogger logger)
        {
            Guid parentForItemLocation = settings.ParentForItemLocation;
            if (settings.ParentForItemLocation != Guid.Empty && this.GetObjectFromPipelineContext(settings.ParentForItemLocation, pipelineContext, logger) is ItemModel fromPipelineContext)
            {
                Guid itemId = fromPipelineContext.GetItemId();
                if (itemId != Guid.Empty)
                    return itemId;
            }
            return settings.ParentItemIdItem;
        }

        private ItemModel CreateNewItem(
          object identifierObject,
          IItemModelRepository repository,
          ResolveSitecoreItemSettings settings,
          PipelineContext pipelineContext,
          ILogger logger)
        {
            var itemModel = identifierObject as ItemModel;
            IValueReader valueReader = this.GetValueReader(settings.ItemNameValueAccessor);
            if (valueReader == null)
                return (ItemModel)null;
            DataAccessContext context = new DataAccessContext();

            string validItemName = this.ConvertValueToValidItemName(this.ReadValue(identifierObject, valueReader, context), pipelineContext, logger);
            if (validItemName == null)
                return (ItemModel)null;

            Guid itemIdForNewItem = this.GetParentItemIdForNewItem(repository, settings, pipelineContext, logger);

            if (settings.DoNotCreateItemIfDoesNotExist)
            {
                ItemModel newItem = new ItemModel();
                newItem.Add("ItemName", (object)validItemName);
                newItem.Add("TemplateID", (object)settings.TemplateForNewItem);
                newItem.Add("ParentID", (object)itemIdForNewItem);
                return newItem;
            }

            //!!! CUSTOM IMPLEMENTATION START
            //!!! Handle exception if item exists, but index is not updated yet
            try
            {
                Guid id = repository.Create(validItemName, settings.TemplateForNewItem, itemIdForNewItem);
                return repository.Get(id);
            }
            catch (DuplicateItemNameException e)
            {
                var children = repository.GetChildren(itemModel.GetItemId());
                return children.FirstOrDefault(i => (string)i[ItemModel.ItemName] == validItemName);
            }
            //!!! CUSTOM IMPLEMENTATION END
        }

        private string ConvertValueToValidItemName(
          object value,
          PipelineContext pipelineContext,
          ILogger logger)
        {


            if (value == null)
                return (string)null;
            string str = value.ToString();

            var indexSettings = pipelineContext.GetPlugin<PipelineIndexerSettings>();
            if (indexSettings != null)
            {
                str = str.Replace("{INDEX}", indexSettings.Index.ToString());
            }

            SitecoreItemUtilities plugin = Sitecore.DataExchange.Context.GetPlugin<SitecoreItemUtilities>();
            if (plugin == null)
            {
                this.Log(new Action<string>(logger.Error), pipelineContext, "No plugin is specified on the context to determine whether or not the specified value is a valid item name. The original value will be used.", new string[1]
                {
          "missing plugin: " + typeof (SitecoreItemUtilities).FullName
                });
                return str;
            }
            if (plugin.IsItemNameValid == null)
            {
                this.Log(new Action<string>(logger.Error), pipelineContext, "No delegate is specified on the plugin that can determine whether or not the specified value is a valid item name. The original value will be used.", new string[3]
                {
          "plugin: " + typeof (SitecoreItemUtilities).FullName,
          "delegate: IsItemNameValid",
          "original value: " + str
                });
                return str;
            }
            if (plugin.IsItemNameValid(str))
                return str;
            if (plugin.ProposeValidItemName != null)
                return plugin.ProposeValidItemName(str);
            logger.Error("No delegate is specified on the plugin that can propose a valid item name. The original value will be used. (plugin: {0}, delegate: {1}, original value: {2})", (object)typeof(SitecoreItemUtilities).FullName, (object)"ProposeValidItemName", (object)str);



            return str;
        }

        private IValueReader GetValueReader(IValueAccessor config) => config?.ValueReader;

        private object ReadValue(object source, IValueReader reader, DataAccessContext context)
        {
            if (reader == null)
                return (object)null;
            ReadResult readResult = reader.Read(source, context);
            return !readResult.WasValueRead ? (object)null : readResult.ReadValue;
        }

        protected override object ConvertValueToIdentifier(
          object identifierValue,
          PipelineStep pipelineStep,
          PipelineContext pipelineContext,
          ILogger logger)
        {
            return identifierValue;
        }
    }
}
