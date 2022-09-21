using System;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Functional.Core;
using Unit = System.ValueTuple;

namespace Migratic.Core
{
    public record Migration
    {
        private string? _checksum;

        public Migration(MigrationType Type, MigrationVersion Version, string Description, string Sql) : this(
            Type,
            Version,
            Description,
            Sql,
            Encoding.UTF8)
        {
        }

        public Migration(MigrationType Type, MigrationVersion Version, string Description, string Sql, Encoding encoding)
        {
            this.Type = Type;
            this.Version = Version;
            this.Description = Description;
            this.Sql = Sql;
            this.Encoding = encoding;
        }

        public MigrationType Type { get; init; }
        public MigrationVersion Version { get; init; }
        public string Description { get; init; }
        public string Sql { get; init; }
        public string Checksum => _checksum ??= GetHash(Sql);
        private Encoding Encoding { get; init; }

        private static string GetHash(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return null;
            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(sql);
            var hashBytes = md5.ComputeHash(inputBytes);
            var sb = new StringBuilder();
            foreach (var t in hashBytes) { sb.Append(t.ToString("X2")); }

            return sb.ToString();
        }

        public async Task<Exceptional<Migration>> Execute(DbCommand command)
        {
            try
            {
                command.CommandText = Sql;
                await command.ExecuteNonQueryAsync();
                return this;
            }
            catch (Exception e) { return e; }
        }

        public void Deconstruct(out MigrationType Type,
            out Option<MigrationVersion> Version,
            out string Description,
            out string Sql,
            out Encoding encoding)
        {
            Type = this.Type;
            Version = this.Version;
            Description = this.Description;
            Sql = this.Sql;
            encoding = this.Encoding;
        }
    }
}
