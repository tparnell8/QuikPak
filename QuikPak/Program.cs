﻿using CommandLine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using WixSharp;

namespace QuikPak
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var options = new Options();
            var result = Parser.Default.ParseArguments(args, options);
            if(!result) return;
            if(!System.IO.File.Exists(options.Config))
            {
                Console.Error.WriteLine("Error Config.json does not exist");
            }
            if(!System.IO.Directory.Exists(options.Path))
            {
                Console.Error.WriteLine("path to pack does not exist");
            }
            var config = JsonConvert.DeserializeObject<Config>(System.IO.File.ReadAllText(options.Config));
            var addresses = new List<WebSite.WebAddress>();

            foreach(var Endpoint in config.Endpoints)
            {
                var attr = new Attributes()
                                {
                                    { "Port", Endpoint.Port.ToString() },
                                    { "Secure", Endpoint.Secure? "yes": "no" }
                                };
                if(!string.IsNullOrWhiteSpace(Endpoint.DnsName))
                {
                    attr["Header"] = Endpoint.DnsName;
                }
                addresses.Add(new WebSite.WebAddress()
                {
                    Attributes = attr
                });
            }

            var project = new Project(config.Name)
            {
                Dirs = new[]
                {
                new Dir(new Id("IISMain"), config.Name + "_" +config.Version +"_Web",

                new Files(System.IO.Path.Combine(options.Path, "**")),
                new File(options.Config,
                    new IISVirtualDir
                    {
                        Name = config.Name + "_Web_VDIR",
                        WebSite = new WebSite(config.Name)
                        {
                            InstallWebSite = false,
                            Description = config.Name,
                            Addresses = addresses.ToArray()
                        },
                        WebAppPool = new WebAppPool(config.Name) {
                            Attributes = new Dictionary<string, string>() {
                               ["Identity"] = config.Identity,
                               ["RecycleMinutes"] = config.RecycleMinutes.ToString(),
                               ["IdleTimeout"] = config.IdleTimeout.ToString(),
                               ["ManagedPipelineMode"] = config.ManagedPipelineMode,
                               ["ManagedRuntimeVersion"] = config.ManagedRuntimeVersion
                           }
                        }
                    })
                )
            },
                Version = new Version(config.Version) { },
                GUID = string.IsNullOrWhiteSpace(config.Id) ? Guid.NewGuid() : new Guid(config.Id),
                UI = WUI.WixUI_ProgressOnly,
                OutFileName = config.Name,
                PreserveTempFiles = true,
                UpgradeCode = new Guid(config.UpgradeCode),
            };
            project.Certificates = new Certificate[] { new Certificate() { PFXPassword = "password", CertificatePath = @"c:\bin\a.pfx", Request = false, StoreName = StoreName.personal, StoreLocation = StoreLocation.localMachine, Name = "cert1" } };
            project.Properties.Add(new Property("REINSTALLMODE", "dmus"));
            project.MajorUpgrade = new MajorUpgrade() { AllowDowngrades = true, Schedule = UpgradeSchedule.afterInstallInitialize };
            project.MajorUpgradeStrategy = new MajorUpgradeStrategy()
            {
                UpgradeVersions = new VersionRange()
                {
                    IncludeMinimum = true,
                    IncludeMaximum = false,
                    Minimum = "0.0.0.1",
                    Maximum = "99.0.0.0"
                },
                RemoveExistingProductAfter = Step.InstallInitialize
            };
            Compiler.BuildWxs(project);
            //Compiler.BuildMsi(project);
        }
    }
}
