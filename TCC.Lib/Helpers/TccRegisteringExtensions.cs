﻿using System;
using System.ComponentModel;
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
        public static void AddTcc(this IServiceCollection services)
        {
            services.Configure<TccSettings>(i => { i.BackupConnectionString = "Data Source=tcc.db"; });
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
            services.AddSingleton<Database.Database>();

            services.RegisterDbContext<TccBackupDbContext>(s => s.BackupConnectionString);
            services.RegisterDbContext<TccRestoreDbContext>(s => s.RestoreConnectionString);
        }

        private static void RegisterDbContext<TDbContext>(this IServiceCollection services, Func<TccSettings, string> connectionString)
            where TDbContext : DbContext
        {
            services.AddDbContext<TDbContext>((s, options) =>
            {
                var setting = s.GetRequiredService<IOptions<TccSettings>>().Value;
                var cs = connectionString(setting);
                switch (setting.Provider)
                {
                    case Provider.InMemory:
                        options.UseInMemoryDatabase(Guid.NewGuid().ToString());
                        break;
                    case Provider.SqlServer:
                        options.UseSqlServer(cs);
                        break;
                    case Provider.SqLite:
                        var sqLite = new SqliteConnection(cs);
                        sqLite.Open();
                        options.UseSqlite(sqLite);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
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
