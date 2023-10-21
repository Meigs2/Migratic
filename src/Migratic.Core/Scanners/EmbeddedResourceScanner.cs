using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Migratic.Core.Abstractions;
using Migratic.Core.Configuration;

namespace Migratic.Core.Scanners
{
    public class EmbeddedResourceScanner : IMigrationScanner
    {
        private readonly Assembly _assembly;
        private readonly IMigraticConfiguration _configuration;

        public EmbeddedResourceScanner(Assembly assembly, IMigraticConfiguration configuration)
        {
            _assembly = assembly;
            _configuration = configuration;
        }

        public IEnumerable<IMigration> Scan()
        {
            throw new NotImplementedException();
        }

        public Result<MigrationName, string> Parse(string name)
        {
            throw new NotImplementedException();
        }
    }
}