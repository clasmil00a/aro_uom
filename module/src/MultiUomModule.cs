using System;
using Aronium.Modularity;
using Aronium.Modularity.Infrastructure;
using Aronium.Modularity.Infrastructure.Events;
using Aronium.Pos.Core;
using Aronium.Pos.Facade.Services;

namespace Aronium.Pos.MultiUom
{
    [Module(Name = "Aronium.Pos.MultiUom")]
    [ModuleDependency("Aronium.Storage.Local")]
    [MinVersion("1.51.1.0")]
    [LocalizedAssemblyLocation("Modules\\Aronium.Pos.MultiUom\\Lang")]
    public sealed class MultiUomModule : LocalizedModule
    {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(typeof(MultiUomModule));
        private bool initialized;

        public override void Initialize()
        {
            if (initialized) return;
            initialized = true;
            logger.Info("Initializing Aronium.Pos.MultiUom module");

            IGenericDatabaseService db = AroniumContainer.Resolve<IGenericDatabaseService>();
            var repository = new MultiUomRepository(db);
            repository.EnsureSchema();
            MultiUomRuntime.Repository = repository;

            IOrderService orderService = AroniumContainer.Resolve<IOrderService>();
            if (!(orderService is MultiUomOrderService))
                AroniumContainer.Register<IOrderService>(new MultiUomOrderService(orderService, repository));

            EventAggregator.Instance
                .GetEvent<DocumentSavedEvent>()
                .Subscribe(OnDocumentSaved);
        }

        private void OnDocumentSaved(DocumentPayload payload)
        {
            if (payload == null || payload.Document == null) return;
            MultiUomRepository repository = MultiUomRuntime.Repository;
            if (repository != null) repository.SaveDocumentMetadata(payload.Document);
        }
    }
}
