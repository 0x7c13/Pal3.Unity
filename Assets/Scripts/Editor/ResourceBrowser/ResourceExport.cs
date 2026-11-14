// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser
{
    using System;
    using System.IO;
    using System.Text;
    using Core.DataReader.Sce;

    public enum ResourceExportMode
    {
        Original, Script, Dialogue, Json
    }

    public static class ResourceExport
    {
        public static bool Supports(ResourceEntry entry, ResourceExportMode mode) => mode switch
        {
            ResourceExportMode.Original => true,
            ResourceExportMode.Script or ResourceExportMode.Dialogue => entry.Extension == ".sce",
            _ => entry.Category is "Models" or "Scenes" or "Navigation" or "Lights" or "Scripts" or "Database" or "Effects",
        };

        public static string Suffix(ResourceExportMode mode) => mode switch
        {
            ResourceExportMode.Script => ".txt",
            ResourceExportMode.Dialogue => ".dialogue.txt",
            ResourceExportMode.Json => ".json",
            _ => "",
        };

        public static void Write(ResourceCatalog catalog, ResourceEntry entry, ResourceExportMode mode, string destination)
        {
            catalog.CheckExportDestination(destination);
            if (mode == ResourceExportMode.Original)
            {
                catalog.ExportOriginal(entry, destination);
                return;
            }
            object data = ResourceFormats.Read(entry, catalog.Read(entry), catalog.Codepage);
            if (data == null)
                throw new InvalidDataException("No decoder is available for this resource.");
            if (mode == ResourceExportMode.Json)
                ResourceBrowserWindow.WriteJson(data, destination);
            else
                ResourceCatalog.WriteNewFile(destination, stream =>
                {
                    using var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, leaveOpen: true);
                    ResourceScriptListing.Export((SceFile)data, writer, mode == ResourceExportMode.Dialogue);
                });
        }
    }
}
