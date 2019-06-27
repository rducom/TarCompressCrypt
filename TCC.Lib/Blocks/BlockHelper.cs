﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TCC.Lib.Database;
using TCC.Lib.Helpers;
using TCC.Lib.Options;

namespace TCC.Lib.Blocks
{
    public static class BlockHelper
    {
        public static IEnumerable<CompressionBlock> GenerateCompressBlocks(this CompressOption compressOption)
        {
            IEnumerable<CompressionBlock> blocks;
            string extension = ExtensionFromAlgo(compressOption.Algo, compressOption.PasswordOption.PasswordMode != PasswordMode.None);

            DirectoryInfo srcDir;
            if (File.Exists(compressOption.SourceDirOrFile))
            {
                srcDir = new FileInfo(compressOption.SourceDirOrFile).Directory;
            }
            else
            {
                srcDir = new DirectoryInfo(compressOption.SourceDirOrFile);
            }

            var compFolder = new CompressionFolderProvider(new DirectoryInfo(compressOption.DestinationDir));

            switch (compressOption.BlockMode)
            {
                case BlockMode.Individual:
                    blocks = PrepareCompressBlockIndividual(srcDir);
                    break;
                case BlockMode.Explicit:
                    blocks = PrepareCompressBlockExplicit(compressOption.SourceDirOrFile);
                    break;
                default:
                    throw new NotImplementedException();
            }

            foreach (var block in blocks)
            {
                if (compressOption.BackupMode == BackupMode.Full)
                {
                    block.BackupMode = BackupMode.Full;
                }

                block.DestinationArchiveExtension = extension;
                block.FolderProvider = compFolder;
                block.SourceOperationFolder = srcDir;
                yield return block;
            }
        }

        private static string[] _extensions = { ".tarlz4aes", ".tarlz4", ".tarbraes", ".tarbr", ".tarzstdaes", ".tarzstd" };

        public static IEnumerable<DecompressionBatch> GenerateDecompressBlocks(this DecompressOption decompressOption)
        {
            bool yielded = false;
            var dstDir = new DirectoryInfo(decompressOption.DestinationDir);

            // If file -> direct decompress

            if (!Directory.Exists(decompressOption.SourceDirOrFile))
            {
                if (File.Exists(decompressOption.SourceDirOrFile))
                {
                    var file = new FileInfo(decompressOption.SourceDirOrFile);
                    yielded = true;
                    yield return new DecompressionBatch
                    {
                        BackupFull = GenerateDecompressBlock(file, dstDir, AlgoFromExtension(file.Extension))
                    };
                }
                else
                {
                    throw new ArgumentException($"The path provided contains no archive at all : {decompressOption.SourceDirOrFile}");
                }
            }
            else
            {
                // If directory

                var srcDir = new DirectoryInfo(decompressOption.SourceDirOrFile);
                var norecurse = new EnumerationOptions { RecurseSubdirectories = false };
                var archivesFound = _extensions.SelectMany(i => srcDir.EnumerateFiles(i, norecurse));

                if (archivesFound.Any())
                {
                    // -> Multiple archives in directory
                    //    -> Only FULL extensions
                    //    -> FULL and DIFF extensions
                    //    -> Archive extensions 
                    throw new NotImplementedException();
                }
                else
                {
                    var fullBackups = new Dictionary<string, (DateTime fullDate, DecompressionBatch batch, long fullsize)>();

                    // -> Multiple directories in directory
                    //    -> Each directory should contains FULL and DIFF folders
                    foreach (var dir in srcDir.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
                    {
                        var diff = dir.EnumerateDirectories(TccConst.Diff).FirstOrDefault();
                        var full = dir.EnumerateDirectories(TccConst.Full).FirstOrDefault();

                        if (diff == null && full == null)
                        {
                            // Archives in this directory ??
                            throw new NotImplementedException();

                            //foreach (FileInfo fi in srcDir.EnumerateArchives())
                            //{
                            //    yielded = true;
                            //    yield return new DecompressionBatch
                            //    {
                            //        BackupFull = GenerateDecompressBlock(fi, dstDir, AlgoFromExtension(fi.Extension))
                            //    };
                            //}
                        }
                        else
                        {
                            // at least a FULL or a DIFF folder
                            if (full != null)
                            {
                                var lastFull = full.EnumerateArchives().OrderByDescending(i => i.ExtractBackupDateTime()).FirstOrDefault();

                                if (lastFull != null)
                                {
                                    var batch = new DecompressionBatch();
                                    fullBackups.Add(dir.Name, (lastFull.ExtractBackupDateTime(), batch, lastFull.Length));
                                    yielded = true;
                                    batch.BackupFull = GenerateDecompressBlock(lastFull, dstDir,
                                        AlgoFromExtension(lastFull.Extension));
                                }
                            }

                            if (diff != null)
                            {
                                if (fullBackups.TryGetValue(dir.Name, out var datedBatch))
                                {
                                    datedBatch.batch.BackupsDiff = new List<DecompressionBlock>();
                                    foreach (var diffArchive in diff.EnumerateArchives()
                                        .Where(i => i.ExtractBackupDateTime() >= datedBatch.fullDate)
                                        .OrderBy(i => i.ExtractBackupDateTime()))
                                    {
                                        yielded = true;
                                        datedBatch.batch.BackupsDiff.Add(GenerateDecompressBlock(diffArchive, dstDir,
                                            AlgoFromExtension(diffArchive.Extension)));
                                    }
                                }
                                else
                                {
                                    throw new Exception("no full found for diff");
                                }
                            }

                        }
                    }

                    foreach (var found in fullBackups.Values.OrderByDescending(i => i.fullsize))
                    {
                        yield return found.batch;
                    }
                }
            }

            if (yielded && !dstDir.Exists)
                dstDir.Create();
        }


        private static DecompressionBlock GenerateDecompressBlock(FileInfo sourceFile, DirectoryInfo targetDirectory, CompressionAlgo algo)
        {
            return new DecompressionBlock
            {
                OperationFolder = targetDirectory.FullName,
                SourceArchiveFileInfo = sourceFile,
                Algo = algo
            };
        }

        private static IEnumerable<FileInfo> EnumerateArchives(this DirectoryInfo directoryInfo)
        {
            foreach (CompressionAlgo algo in Enum.GetValues(typeof(CompressionAlgo)))
            {
                foreach (FileInfo fi in directoryInfo.EnumerateFiles("*" + ExtensionFromAlgo(algo, true)))
                {
                    yield return fi;
                }

                foreach (FileInfo fi in directoryInfo.EnumerateFiles("*" + ExtensionFromAlgo(algo, false)))
                {
                    yield return fi;
                }
            }
        }

        private static string ExtensionFromAlgo(CompressionAlgo algo, bool crypted)
        {
            switch (algo)
            {
                case CompressionAlgo.Lz4 when crypted:
                    return ".tarlz4aes";
                case CompressionAlgo.Lz4:
                    return ".tarlz4";
                case CompressionAlgo.Brotli when crypted:
                    return ".tarbraes";
                case CompressionAlgo.Brotli:
                    return ".tarbr";
                case CompressionAlgo.Zstd when crypted:
                    return ".tarzstdaes";
                case CompressionAlgo.Zstd:
                    return ".tarzstd";
                default:
                    throw new ArgumentOutOfRangeException(nameof(algo), algo, null);
            }
        }

        private static CompressionAlgo AlgoFromExtension(string extension)
        {
            switch (extension)
            {
                case ".tarlz4":
                case ".tarlz4aes":
                    return CompressionAlgo.Lz4;
                case ".tarbr":
                case ".tarbraes":
                    return CompressionAlgo.Brotli;
                case ".tarzstd":
                case ".tarzstdaes":
                    return CompressionAlgo.Zstd;
                default:
                    throw new ArgumentOutOfRangeException(nameof(extension), extension, null);
            }
        }

        private static IEnumerable<CompressionBlock> PrepareCompressBlockIndividual(DirectoryInfo srcDir)
        {
            // for each directory in sourceDir we create an archive
            foreach (DirectoryInfo di in srcDir.EnumerateDirectories())
            {
                yield return new CompressionBlock
                {
                    SourceFileOrDirectory = new FileOrDirectoryInfo(di),
                };
            }

            // for each file in sourceDir we create an archive
            foreach (FileInfo fi in srcDir.EnumerateFiles())
            {
                yield return new CompressionBlock
                {
                    SourceFileOrDirectory = new FileOrDirectoryInfo(fi),
                };
            }
        }

        private static IEnumerable<CompressionBlock> PrepareCompressBlockExplicit(string sourceDir)
        {
            yield return new CompressionBlock
            {
                SourceFileOrDirectory = new FileOrDirectoryInfo(sourceDir),
            };
        }
    }
}