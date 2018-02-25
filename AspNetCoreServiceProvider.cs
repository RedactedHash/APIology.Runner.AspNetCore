// ReSharper disable once CheckNamespace
namespace APIology.ServiceProvider
{
	using Autofac;
	using Autofac.Extensions.DependencyInjection;
	using Configuration;
	using Topshelf;
	using Microsoft.AspNetCore.Builder;
	using Microsoft.AspNetCore.Hosting;
	using Microsoft.AspNetCore.Hosting.Server.Features;
	using Microsoft.Extensions.DependencyInjection;
	using Microsoft.Extensions.Logging;
	using Serilog;
	using System;
	using System.Diagnostics.CodeAnalysis;
	using System.Linq;
	using System.IO;
	using Core;

	[SuppressMessage("ReSharper", "MemberCanBeProtected.Global")]
	public abstract class AspNetCoreServiceProvider<TAPIBase, TConfiguration> : BaseServiceProvider<TConfiguration>
		where TAPIBase : BaseServiceProvider<TConfiguration>
		where TConfiguration : AspNetCoreConfiguration, new()
	{
		private IWebHost _instance;
		public IHostingEnvironment Environment { get; private set; }

		public override bool Start(HostControl hostControl)
		{
			Logger.Debug("Building web server configuration");

			Logger.Debug("Starting web server");

			Startup.Service = this;

			_instance = new WebHostBuilder()
				.UseKestrel()
				.UseContentRoot(Directory.GetCurrentDirectory())
				.UseStartup<Startup>()
				.Start(Config.Bindings.Select(bc => bc.BoundUri).ToArray());

			var registeredAddress = _instance.ServerFeatures.Get<IServerAddressesFeature>();
			Logger.Information("Web server has been bound to {Addresses}", registeredAddress.Addresses);

			return true;
		}

		public class Startup
		{
			internal static AspNetCoreServiceProvider<TAPIBase, TConfiguration> Service;
			private static ILifetimeScope Container { get; set; }

			public Startup(IHostingEnvironment env)
			{
			}

			public IServiceProvider ConfigureServices(IServiceCollection services)
			{
				Service.ConfigureAspNetCore(services);
				services.AddLogging();

				var builder = new ContainerBuilder();
				builder.Populate(services);
				Service.BuildAspNetCoreDependencySubcontainer(builder);

				Container = builder.Build();

				return new AutofacServiceProvider(Container);
			}

			public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
			{
				Service.BuildAspNetCoreApp(app, env);
				appLifetime.ApplicationStopped.Register(() => Container.Dispose());
			}
		}

		public override bool Stop(HostControl hostControl)
		{
			Logger.Debug("Web server stopping");
			if (ReferenceEquals(_instance, null))
				return false;

			_instance.Dispose();
			_instance = null;
			return true;
		}

		public override void BuildDependencyContainer(ContainerBuilder builder)
		{
			builder.RegisterType<LoggerFactory>()
				.As<ILoggerFactory>()
				.OnActivating(args => {
					args.Instance.AddSerilog();
				})
				.SingleInstance();
		}

		public virtual void BuildAspNetCoreDependencySubcontainer(ContainerBuilder builder)
		{
		}

		public virtual void AspNetCoreStartupHandler(IHostingEnvironment env)
		{
			throw new NotImplementedException();
		}

		public virtual void ConfigureAspNetCore(IServiceCollection services)
		{
			throw new NotImplementedException();
		}

		public abstract void BuildAspNetCoreApp(IApplicationBuilder app, IHostingEnvironment env);
	}
}