using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenWrap.Exports
{
    public class AssemblyReferenceExport : IExport
    {
        public AssemblyReferenceExport(IEnumerable<IExportItem> assemblies)
        {
            Items = (from entry in assemblies
                     let extension = Path.GetExtension(entry.FullPath)
                     where ".dll".Equals(extension, StringComparison.OrdinalIgnoreCase) ||
                           ".exe".Equals(extension, StringComparison.OrdinalIgnoreCase)
                     let assemblyRef = CreateAssemblyRef(entry)
                     where assemblyRef != null
                     select assemblyRef).ToList();
        }

        public IEnumerable<IExportItem> Items { get; private set; }

        public string Name
        {
            get { return "bin"; }
        }

        static IExportItem CreateAssemblyRef(IExportItem item)
        {
            try
            {
                return new AssemblyReferenceExportItem(item);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}