﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Autofac.Extensions.DependencyInjection;
using GenderPayGap.Core;
using GenderPayGap.Extensions;
using GenderPayGap.Extensions.AspNetCore;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace GenderPayGap.WebUI
{
    public class Program
    {

        public static void Main(string[] args)
        {
            Console.Title = "GenderPayGap.WebUI";

            //Add a handler for unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            //Culture is required so UK dates can be parsed correctly
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB");
            Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture;

            //Create the web host
            IWebHost host = BuildWebHost(args);

            //Set the minumum threads 
            Console.WriteLine(Extensions.AspNetCore.Extensions.SetThreadCount());

            //Show thread availability
            Console.WriteLine(Extensions.AspNetCore.Extensions.GetThreadCount());

            //Run the webhost
            host.Run();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;

            Console.WriteLine($"UNHANDLED EXCEPTION ({Console.Title}): {ex.Message}{Environment.NewLine}{ex.GetDetailsText()}");
            Debug.WriteLine($"UNHANDLED EXCEPTION ({Console.Title}): {ex.Message}{Environment.NewLine}{ex.GetDetailsText()}");

            //Show thread availability
            Console.WriteLine(Extensions.AspNetCore.Extensions.GetThreadCount());

            throw ex;
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            IWebHostBuilder webHostBuilder = WebHost.CreateDefaultBuilder(args);

            webHostBuilder.ConfigureKestrel(
                    options =>
                    {
                        options.AddServerHeader = false;
                        //options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
                    })
                .ConfigureAppConfiguration(ConfigureAppConfiguration)
                .CaptureStartupErrors(true) // Add this line to capture startup errors
                .UseEnvironment(Config.EnvironmentName) //Set the environment name
                .UseSetting(
                    WebHostDefaults.DetailedErrorsKey,
                    "true") //When enabled (or when the Environment is set to Development), the app captures detailed exceptions.
                .ConfigureServices(
                    services => services
                        .AddAutofac()); /// This call allows for ConfigureContainer to be supported in Startup with a strongly-typed ContainerBuilder

            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PORT")))
            {
                webHostBuilder.UseUrls($"http://0.0.0.0:{Environment.GetEnvironmentVariable("PORT")}/");
            }

            webHostBuilder.UseStartup<Startup>();

            SetupSerilogLogger(webHostBuilder);

            return webHostBuilder.Build();
        }

        /// <summary>
        ///     Use the Config extension class for Configuration
        /// </summary>
        /// <param name="builderContext"></param>
        /// <param name="configBuilder"></param>
        private static void ConfigureAppConfiguration(WebHostBuilderContext builderContext, IConfigurationBuilder configBuilder)
        {
            Config.EnvironmentName = builderContext.HostingEnvironment.EnvironmentName;
            Console.WriteLine($"Environment: {Config.EnvironmentName}");

            //Build the configuration
            Config.Configuration = Config.Build(configBuilder);
            Encryption.SetDefaultEncryptionKey(Global.DefaultEncryptionKey);
        }

        public static void SetupSerilogLogger(IWebHostBuilder webHostBuilder)
        {
            if (Config.IsLocal())
            {
                // Log to Console
                Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            }
            else
            {
                // Log to Logit.io
                webHostBuilder.UseSerilog(
                    (ctx, config) =>
                    {
                        config.ReadFrom.Configuration(ctx.Configuration);

                        // Add a property called "cf.app" with the environment name (e.g. DEV)
                        // This allows us to filter the logs per-environment in logit.io
                        config.Enrich.WithProperty("cf.app", Config.EnvironmentName);
                    });
            }

            Log.Information("Serilog logger setup complete");
        }

    }
}
