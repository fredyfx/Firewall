using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MaxMind.Db;
using MaxMind.GeoIP2;
using Microsoft.AspNetCore.Http;

namespace Firewall
{
    /// <summary>
    /// A Firewall rule which specifies that only connections from a specific geo-location are allowed to access the web server.
    /// </summary>
    public sealed class CountryRule : IFirewallRule
    {
        private readonly IFirewallRule _nextRule;
        private readonly IList<string> _allowedCountries;
        private readonly DatabaseReader _databaseReader;

        /// <summary>
        /// Initialises a new instance of <see cref="CountryRule"/>.
        /// </summary>
        public CountryRule(
            IFirewallRule nextRule,
            IList<CountryCode> allowedCountries,
            string geoIP2FileName = null)
        {
            _nextRule = nextRule;
            _allowedCountries =
                allowedCountries
                    .Select(isoCode => isoCode.ToString())
                    .ToList();

            var stream =
                geoIP2FileName != null
                ? new FileStream(geoIP2FileName, FileMode.Open)
                : Assembly
                    .GetExecutingAssembly()
                    .GetManifestResourceStream(
                        "Firewall.GeoIP2.GeoLite2-Country.mmdb");

            _databaseReader = new DatabaseReader(stream);
        }

        /// <summary>
        /// Denotes whether a given <see cref="HttpContext"/> is permitted to access the web server.
        /// </summary>
        public bool IsAllowed(HttpContext context)
        {
            var remoteIpAddress = context.Connection.RemoteIpAddress;
            var result = _databaseReader.Country(remoteIpAddress);
            var countryCode = result.Country.IsoCode;
            return _allowedCountries.Contains(countryCode) || _nextRule.IsAllowed(context);

        }
    }
}