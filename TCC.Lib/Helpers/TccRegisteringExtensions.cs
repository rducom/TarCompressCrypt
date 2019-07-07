﻿using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TCC.Lib.Benchmark;
using TCC.Lib.Blocks;
using TCC.Lib.Database;
using TCC.Lib.Dependencies;

namespace TCC.Lib.Helpers
{
    public static class TccRegisteringExtensions
    {
        public static void AddTcc(this IServiceCollection services, string workingPath = null)
        {
            services.TryAddScoped<IBlockListener, GenericBlockListener>();
            services.TryAddScoped(typeof(ILogger<>), typeof(NullLogger<>));
            services.AddScoped<ExternalDependencies>();
            services.AddScoped<TarCompressCrypt>();
            services.AddScoped<EncryptionCommands>();
            services.AddScoped<CompressionCommands>();
            services.AddScoped<BenchmarkRunner>();
            services.AddScoped<BenchmarkOptionHelper>();
            services.AddScoped<BenchmarkIterationGenerator>();
            services.AddScoped(_ => new CancellationTokenSource());
            services.AddScoped<DatabaseSetup>();

            services.RegisterDbContext<TccBackupDbContext>(s => s.BackupConnectionString, workingPath);
            services.RegisterDbContext<TccRestoreDbContext>(s => s.RestoreConnectionString, workingPath);
        }

        private static void RegisterDbContext<TDbContext>(this IServiceCollection services, Func<TccSettings, string> connectionString, string workingPath)
            where TDbContext : DbContext
        {
            services.AddDbContext<TDbContext>((s, options) =>
            {
                var setting = s.GetRequiredService<IOptions<TccSettings>>().Value;

                switch (setting.Provider)
                {
                    case Provider.InMemory:
                        options.UseInMemoryDatabase(Guid.NewGuid().ToString());
                        break;
                    case Provider.SqlServer:
                        {
                            var cs = connectionString(setting);
                            options.UseSqlServer(cs);
                            break;
                        }
                    case Provider.SqLite:
                        {
                            string cs = GetSqLiteConnectionString(connectionString(setting), workingPath);
                            var sqLite = new SqliteConnection(cs);
                            sqLite.Open();
                            options.UseSqlite(sqLite);
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
        }

        private static string GetSqLiteConnectionString(string settingConnectionString, string workingPath)
        {
            if (string.IsNullOrWhiteSpace(settingConnectionString) && !string.IsNullOrWhiteSpace(workingPath))
            {
                // default SqLite path on source & destination targets
                if (!Path.IsPathRooted(workingPath))
                {
                    throw new ArgumentException($"Path {workingPath} isn't absolute");
                }

                var dir = new DirectoryInfo(workingPath);
                return "Data Source=" + Path.Combine(dir.FullName, "tcc.db");
            }
            // fallback
            return string.IsNullOrWhiteSpace(settingConnectionString) ? "Data Source=tcc.db" : settingConnectionString;
        }
    }

    public class TccSettings
    {
        public string BackupConnectionString { get; set; }
        public string RestoreConnectionString { get; set; }
        public Provider Provider { get; set; } = Provider.SqLite;
    }

    [DefaultValue(SqLite)]
    public enum Provider
    {
        SqLite,
        InMemory,
        SqlServer
    }
}
