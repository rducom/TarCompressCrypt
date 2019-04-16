﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TCC.Lib.Helpers;

namespace TCC.Lib.Migrations
{
    [DbContext(typeof(TccDbContext))]
    [Migration("20190416084947_InitialSchema")]
    partial class InitialSchema
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.8-servicing-32085");

            modelBuilder.Entity("TCC.Lib.Helpers.BlockJob", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("BackupType");

                    b.Property<TimeSpan>("Duration");

                    b.Property<string>("Exception");

                    b.Property<int>("JobId");

                    b.Property<int?>("JobId1");

                    b.Property<long>("Size");

                    b.Property<string>("Source");

                    b.Property<DateTime>("StartTime");

                    b.Property<bool>("Success");

                    b.HasKey("Id");

                    b.HasIndex("JobId");

                    b.HasIndex("JobId1");

                    b.HasIndex("StartTime");

                    b.ToTable("BlockJobs");
                });

            modelBuilder.Entity("TCC.Lib.Helpers.Job", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<TimeSpan>("Duration");

                    b.Property<DateTime>("StartTime");

                    b.HasKey("Id");

                    b.ToTable("Jobs");
                });

            modelBuilder.Entity("TCC.Lib.Helpers.BlockJob", b =>
                {
                    b.HasOne("TCC.Lib.Helpers.Job")
                        .WithMany("BlockJobs")
                        .HasForeignKey("JobId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("TCC.Lib.Helpers.Job", "Job")
                        .WithMany()
                        .HasForeignKey("JobId1");
                });
#pragma warning restore 612, 618
        }
    }
}
