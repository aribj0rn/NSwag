//-----------------------------------------------------------------------
// <copyright file="ListTypesCommand.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NConsole;
using NJsonSchema.Infrastructure;
using NSwag.AssemblyLoader.Utilities;
using System.IO;

namespace NSwag.Commands.SwaggerGeneration
{
    [Command(Name = "list-types", Description = "List all types for the given assembly and settings.")]
    public class ListTypesCommand : IsolatedCommandBase<string[]>
    {
        [Argument(Name = "File", IsRequired = false, Description = "The nswag.json configuration file path.")]
        public string File { get; set; }

        public override async Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            if (!string.IsNullOrEmpty(File))
            {
                var document = await NSwagDocument.LoadAsync(File);
                var command = (TypesToSwaggerCommand)document.SelectedSwaggerGenerator;

                AssemblyPaths = command.AssemblyPaths;
                AssemblyConfig = command.AssemblyConfig;
                ReferencePaths = command.ReferencePaths;
            }

            var classNames = await RunIsolatedAsync(!string.IsNullOrEmpty(File) ? Path.GetDirectoryName(File) : null);

            host.WriteMessage("\r\n");
            foreach (var className in classNames)
                host.WriteMessage(className + "\r\n");
            host.WriteMessage("\r\n");

            return classNames;
        }

        protected override async Task<string[]> RunIsolatedAsync(AssemblyLoader.AssemblyLoader assemblyLoader)
        {
#if FullNet
            return PathUtilities.ExpandFileWildcards(AssemblyPaths)
                .Select(Assembly.LoadFrom)
#else
            var currentDirectory = DynamicApis.DirectoryGetCurrentDirectoryAsync().GetAwaiter().GetResult();
            return PathUtilities.ExpandFileWildcards(AssemblyPaths)
                .Select(p => assemblyLoader.Context.LoadFromAssemblyPath(PathUtilities.MakeAbsolutePath(p, currentDirectory)))
#endif
                .SelectMany(a => a.ExportedTypes)
                .Select(t => t.FullName)
                .OrderBy(c => c)
                .ToArray();
        }
    }
}