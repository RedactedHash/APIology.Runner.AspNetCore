namespace APIology.Runner.AspNetCore
{
	using Runner.Core;
	using Autofac;
	using System.Diagnostics.CodeAnalysis;
	using Topshelf;
	using Microsoft.AspNetCore.Hosting;
	using Microsoft.AspNetCore.Hosting.Server.Features;
	using Microsoft.Extensions.DependencyInjection;
	using Microsoft.AspNetCore.Builder;
	using Microsoft.Extensions.Logging;
	using Autofac.Extensions.DependencyInjection;
	using System;
	using Serilog;
	using System.Linq;
	using System.IO;

	[SuppressMessage("ReSharper", "MemberCanBeProtected.Global")]
	public abstract class ServiceBase<TAPIBase, TConfiguration> : ServiceBase<TConfiguration>
		where TAPIBase : ServiceBase<TConfiguration>
		where TConfiguration : Configuration, new()
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
				.UseContentRoot(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\.."))
				.UseStartup<Startup>()
				.Start(Config.Bindings.Select(bc => bc.BoundUri).ToArray());

			var registeredAddress = _instance.ServerFeatures.Get<IServerAddressesFeature>();
			Logger.Information("Web server has been bound to {Addresses}", registeredAddress.Addresses);

			return true;
		}

		public class Startup
		{
			internal static ServiceBase<TAPIBase, TConfiguration> Service;
			private ILifetimeScope Scope { get; set; }

			public Startup(IHostingEnvironment env)
			{
				EnvironmentName = env.EnvironmentName;
			}

			public IServiceProvider ConfigureServices(IServiceCollection services)
			{
				services.AddLogging();
				Service.ConfigureAspNetCore(services);

				Scope = Service.LazyContainer.Value.BeginLifetimeScope(builder => {
					builder.Populate(services);

					Service.BuildAspNetCoreDependencySubcontainer(builder);
					/* builder.RegisterType<SerilogLoggerProvider>()
						.As<ILoggerProvider>()
						.SingleInstance(); */
				});

				return new AutofacServiceProvider(Scope);
			}

			public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
			{
				Service.BuildAspNetCoreApp(app, env);
				appLifetime.ApplicationStopped.Register(() => Scope.Dispose());
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
