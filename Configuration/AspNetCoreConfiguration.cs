namespace APIology.ServiceProvider.Configuration
{
	using Newtonsoft.Json;
	// using Security.X509;
	using System;
	using System.IO;
	using System.Linq;
	// using System.Collections.Generic;
	// using System.Security.Cryptography.X509Certificates;

	public class AspNetCoreConfiguration : BaseServiceConfiguration
	{
		[JsonIgnore]
		// ReSharper disable once InconsistentNaming
		public string ASPNETCORE_URLS
		{
			get
			{
				return string.Join(";",
					Bindings.Select(val => val.BoundUri).ToArray()
				);
			}
			set
			{
				if (string.IsNullOrWhiteSpace(value))
					return;

				Bindings = value.Split(';', ',')
					.Select(url => new BindingConfiguration { UrlAcl = url })
					.ToArray();
			}
		}

		public BindingConfiguration[] Bindings { get; set; } = { };

		// public List<CertificateIdentifier> ClientWhitelist { get; set; }
	}

	public class BindingConfiguration
	{
		[JsonIgnore]
		public UriBuilder Builder { get; set; }

		private string _urlAcl;
		
		public string UrlAcl {
			get {
				return _urlAcl;
			}
			set {
				_urlAcl = value;
				Builder = new UriBuilder(Url);
			}
		}

		public string Url {
			get {
				var value = _urlAcl;

				if (value?.StartsWith("://") == true)
				{
					value = $"any{value}";
				}

				return value?.Replace("://+", "://0.0.0.0");
			}
		}

		[JsonProperty("CertHash", NullValueHandling = NullValueHandling.Ignore)]
		public string CertHash { get; set; }

		internal string PreferedHostname { get; set; }

		[JsonIgnore]
		public string BoundUri {
			get {
				if (Builder == null)
					return null;

				var temp = new UriBuilder(Builder.Uri);

				if (CertHash != null && temp.Host == "0.0.0.0")
				{
					// var cert = CertificateStore.LoadFromCertificateStore(CertHash);
					temp.Host = PreferedHostname; //?? cert.GetNameInfo(X509NameType.DnsName, false);
				}

				return temp.ToString().ToLowerInvariant();
			}
		}
	}
}
