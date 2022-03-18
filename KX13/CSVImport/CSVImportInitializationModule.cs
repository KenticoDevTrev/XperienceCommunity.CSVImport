using CMS;
using CMS.DataEngine;
using CMS.Modules;
using NuGet;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using System.Collections.Generic;
using System.Web;
using XperienceCommunity.CSVImport;

[assembly: RegisterModule(typeof(CSVImportInitializationModule))]

namespace XperienceCommunity.CSVImport
{
    public class CSVImportInitializationModule : Module
    {
        public CSVImportInitializationModule()
            : base("CSVImportInitializationModule")
        {
        }

        // Contains initialization code that is executed when the application starts
        protected override void OnInit()
        {
            base.OnInit();

            ModulePackagingEvents.Instance.BuildNuSpecManifest.After += BuildNuSpecManifest_After;
        }

        private void BuildNuSpecManifest_After(object sender, BuildNuSpecManifestEventArgs e)
        {
            if (e.ResourceName.Equals("CSVImport", System.StringComparison.InvariantCultureIgnoreCase))
            {
                // Change the name
                e.Manifest.Metadata.Title = "Xperience CSV Import Tool";
                e.Manifest.Metadata.SetProjectUrl("https://github.com/KenticoDevTrev/XperienceCommunity.CSVImport");
                e.Manifest.Metadata.SetIconUrl("https://www.hbs.net/HBS/media/Favicon/favicon-96x96.png");
                e.Manifest.Metadata.Tags = "Kentico Xperience CSV Import Importer";
                e.Manifest.Metadata.Id = "XperienceCommunity.CSVImport.Admin";
                e.Manifest.Metadata.ReleaseNotes = "Updated for Kentico Xperience version 13 (Previous HBS_CSVImport)";
                // Add nuget dependencies

                // Add dependencies
                List<PackageDependency> NetStandardDependencies = new List<PackageDependency>()
                {
                    new PackageDependency("Kentico.Xperience.Libraries", new VersionRange(new NuGetVersion("13.0.0")), new string[] { }, new string[] {"Build","Analyzers"}),
                    new PackageDependency("CsvHelper", new VersionRange(new NuGetVersion("27.2.1")), new string[] { }, new string[] {"Build","Analyzers"})
                };
                PackageDependencyGroup PackageGroup = new PackageDependencyGroup(new NuGet.Frameworks.NuGetFramework(".NETStandard2.0"), NetStandardDependencies);
                e.Manifest.Metadata.DependencyGroups = new PackageDependencyGroup[] { PackageGroup };
                // Add in Designer.cs and .cs files since really hard to include these in class library due to depenencies
                string BaseDir = HttpContext.Current.Server.MapPath("~").Trim('\\');
                
            }
        }
    }
}
