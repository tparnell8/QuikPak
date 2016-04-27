﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json;
using WixSharp;

namespace QuikPak
{
	class Program
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
					addresses.Add(new WebSite.WebAddress() {
						Attributes = new Attributes()
								{
									{ "Port", Endpoint.Port },
									{"Header", Endpoint.DnsName },
									{ "Secure", Endpoint.Secure? "yes": "no" }
								}
					});
				}

			var project = new Project(config.Name)
			{
				Dirs = new[]
				{
				new Dir(new Id("IISMain"), config.Name + "_" +config.Version.ToString() +"_Web",
				new Files(System.IO.Path.Combine(options.Path, "**")),
				new File(options.Config,
					new IISVirtualDir
					{
						Name = config.Name + "_Web_VDIR",
						WebSite = new WebSite(config.Name)
						{
							InstallWebSite = true,
							Description = config.Name,
							Addresses = addresses.ToArray(),
						},
						WebAppPool = new WebAppPool(config.Name)
					})
				)

			},
				Version = new Version(config.Version) { },
				GUID = new Guid(config.Id),
				UI = WUI.WixUI_ProgressOnly,
				OutFileName = config.Name,
				PreserveTempFiles = true,
				UpgradeCode = new Guid(config.UpgradeCode),
				 
			};
			project.Properties.Add(new Property("REINSTALLMODE", "dmus"));
			project.MajorUpgrade = new MajorUpgrade() { AllowDowngrades = true};
			project.MajorUpgradeStrategy = new MajorUpgradeStrategy() {

				UpgradeVersions = new VersionRange() {
					IncludeMinimum = true,
					IncludeMaximum = false,
					Minimum = "0.0.0.1",
					Maximum = "99.0.0.0"
				} };
			Compiler.BuildMsi(project);
		}
	}
}