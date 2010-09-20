﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using OpenFileSystem.IO;
using OpenFileSystem.IO.FileSystems;
using OpenWrap.Services;

namespace OpenWrap.Commands.Wrap
{
    [Command(Noun="enable", Verb="wrap")]
    public class EnableWrapCommand : AbstractCommand
    {
        IEnumerable<IFile> _projectsToPatch;
        const string MSBUILD_NS = "http://schemas.microsoft.com/developer/msbuild/2003";
        const string OPENWRAP_BUILD = @"..\..\wraps\openwrap\build\OpenWrap.CSharp.targets";
        [CommandInput]
        public bool AllProjects { get; set; }
        [CommandInput]
        public string Projects { get; set; }
        protected IEnvironment Environment { get { return WrapServices.GetService<IEnvironment>(); } }
        public override IEnumerable<ICommandOutput> Execute()
        {
            return Either(VerifyArguments()).Or(ExecuteCore());
        }

        IEnumerable<IFile> GetAllProjects()
        {
            return Environment.CurrentDirectory.Files("*.*proj", SearchScope.SubFolders);
        }

        IEnumerable<ICommandOutput> ExecuteCore()
        {
            
            foreach(var project in _projectsToPatch)
            {
                var xmlDoc = new XmlDocument();
                var namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
                namespaceManager.AddNamespace("msbuild", MSBUILD_NS);

                using (var projectFileStream = project.OpenRead())
                    xmlDoc.Load(projectFileStream);
                var importNode = xmlDoc.SelectSingleNode(@"//msbuild:Import[@Project='$(MSBuildToolsPath)\Microsoft.CSharp.targets']", namespaceManager);
                if (importNode == null)
                    yield return new GenericMessage("Project '{0}' was not a recognized csharp project file. Ignoring.");
                else
                {
                    // TODO: Detect path of openwrap directory and generate correct relative path from there
                    importNode.Attributes["Project"].Value = OPENWRAP_BUILD;
                    using (var projectFileStream = project.OpenWrite())
                        xmlDoc.Save(projectFileStream);
                }
            }
        }

        IEnumerable<IFile> GetSpecificProjects()
        {
            return Projects.Split(new[]{";"}, StringSplitOptions.RemoveEmptyEntries).Select(x => Environment.CurrentDirectory.GetFile(x));
            
        }

        public IEnumerable<ICommandOutput> VerifyArguments()
        {
            if (AllProjects && !string.IsNullOrEmpty(Projects))
            {
                yield return new GenericError(@"No project was specified. Either specify -AllProjects for all the projects in any folders under the current path, or -Project path\to\project.csproj.");
                yield break;
            }
            _projectsToPatch = AllProjects ? GetAllProjects() : GetSpecificProjects();
            foreach (var proj in _projectsToPatch.Where(x => x.Exists == false))
                yield return new GenericError("The project at path '{0}' does not exist. Check the path and try again.", proj.Path.FullPath);
        }
    }
}
