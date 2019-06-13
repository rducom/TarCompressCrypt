﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TCC.Lib.Database;

namespace TCC.Lib.Migrations.TccRestoreDb
{
    [DbContext(typeof(TccRestoreDbContext))]
    partial class TccRestoreDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.4-servicing-10062");

            modelBuilder.Entity("TCC.Lib.Database.RestoreBlockJob", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("BackupMode");

                    b.Property<TimeSpan>("Duration");

                    b.Property<string>("Exception");

                    b.Property<string>("FullSourcePath");

                    b.Property<int>("JobId");

                    b.Property<int?>("JobId1");

                    b.Property<long>("Size");

                    b.Property<DateTime>("StartTime");

                    b.Property<bool>("Success");

                    b.HasKey("Id");

                    b.HasIndex("JobId");

                    b.HasIndex("JobId1");

                    b.HasIndex("StartTime");

                    b.ToTable("RestoreBlockJobs");
                });

            modelBuilder.Entity("TCC.Lib.Database.RestoreJob", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<TimeSpan>("Duration");

                    b.Property<DateTime>("StartTime");

                    b.HasKey("Id");

                    b.ToTable("RestoreJobs");
                });

            modelBuilder.Entity("TCC.Lib.Database.RestoreBlockJob", b =>
                {
                    b.HasOne("TCC.Lib.Database.RestoreJob")
                        .WithMany("BlockJobs")
                        .HasForeignKey("JobId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("TCC.Lib.Database.RestoreJob", "Job")
                        .WithMany()
                        .HasForeignKey("JobId1");
                });
#pragma warning restore 612, 618
        }
    }
}