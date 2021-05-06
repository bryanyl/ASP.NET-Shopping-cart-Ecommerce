﻿using System.Threading.Tasks;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.DataExchange.Export
{
    public partial interface IExportProvider : IProvider, IUserEditable
    {
        /// <summary>
        /// Gets the exported entity type.
        /// </summary>
        ExportEntityType EntityType { get; }

        /// <summary>
        /// Gets the file extension of the export files (without dot). Return <c>null</c> for a non file based, on-the-fly export.
        /// </summary>
        string FileExtension { get; }

        /// <summary>
        /// Gets provider specific configuration information. Return <c>null</c> when no provider specific configuration is required.
        /// </summary>
        ExportConfigurationInfo ConfigurationInfo { get; }

        /// <summary>
        /// Starts exporting data to a file.
        /// </summary>
        /// <param name="context">Export execution context.</param>
        Task ExecuteAsync(ExportExecuteContext context); // TODO: (mg) (core) It's better to pass CancellationToken to an async method as parameter.

        /// <summary>
        /// Called once per store when the export execution ended.
        /// </summary>
        /// <param name="context">Export execution context.</param>
        Task OnExecutedAsync(ExportExecuteContext context); // TODO: (mg) (core) It's better to pass CancellationToken to an async method as parameter.
    }
}