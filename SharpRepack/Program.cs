using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SharpCompress.Common;
using SharpCompress.Reader;
using SharpCompress.Writer;
using SharpCompress.Archive.SevenZip;

namespace sharprepack
{
    class Program
    {
        static void Main(string[] args)
        {
            #region Startup Verification
            // insufficient params to proceed
            if (args.Count() != 2)
            {
                Usage();
                return;
            }

            switch (args[0])
            {
                case "help":
                case "/help":
                case "-help":
                case "--help":
                    Usage();
                    return;
                default:
                    break;
            }

            var newformat = args[0];
            var filename = args[1];

            // ensure that the target file exists
            if (File.Exists($"{filename}") == false)
            {
                WriteError($"Fatal Error: Cannot find file '{filename}'");
                return;
            }
            #endregion

            string newFileExtension = null;

            // TODO: check if formats are supported by the library
            CompressionType? newCompressionType = null;
            ArchiveType? newArchiveType = 
                MatchArchiveType(newformat, out newCompressionType, out newFileExtension);

            if(newArchiveType == null)
            {
                Usage();
                return;
            }


            // create a unique temp folder for file extraction
            var tempfolder = $@"{Path.GetTempPath()}{Guid.NewGuid().ToString()}";
            Directory.CreateDirectory(tempfolder);

            try
            {
                using (var reader = SevenZipArchive.IsSevenZipFile(filename) ? 
                    SevenZipArchive.Open(filename).ExtractAllEntries() : 
                    ReaderFactory.Open(File.OpenRead(filename), Options.LookForHeader))
                {
                    WriteInfo($"Converting file '{filename.ToUpper()}' from {reader.ArchiveType.ToString().ToUpper()} to {newformat.ToUpper()}");

                    // TODO: The following code can be replaced by writing everything to a folder
                    // e.g. reader.WriteAllToDirectory(tempfolder);
                    while (reader.MoveToNextEntry())
                    {
                        if(reader.Entry.IsDirectory)
                        {
                            WriteInfo1($"Extracting directory: {reader.Entry.Key}...");
                            reader.WriteEntryToDirectory(tempfolder);
                        }

                        // create temp folder and extract files
                        //Console.WriteLine($"Extracting file: {reader.Entry.Key}...");
                        WriteInfo2($"Extracting file: {reader.Entry.Key}...");
                        reader.WriteEntryToFile($@"{tempfolder}\{reader.Entry.Key}");
                    }
                }

                var newArchiveName = $@"{Path.GetFileNameWithoutExtension(filename)}{newFileExtension}";

                if (!string.IsNullOrEmpty(Path.GetDirectoryName(filename)))
                    newArchiveName = $@"{Path.GetDirectoryName(filename)}\{newArchiveName}";

                using (var outStream = File.OpenWrite(newArchiveName))
                {
                    using (var archive = WriterFactory.Open(outStream, newArchiveType.Value, newCompressionType.Value))
                    {
                        //Console.WriteLine($"Writing files to new archive: {newArchiveName}");
                        WriteInfo1($"Writing files to new archive: {newArchiveName}");
                        archive.WriteAll(tempfolder, "*", SearchOption.AllDirectories);
                    }
                }
            }
            catch (Exception e)
            {
                WriteError($"Critical Error: {e.ToString()}");
            }
            finally
            {
                Directory.Delete(tempfolder, true);
            }

        }

        private static ArchiveType? MatchArchiveType(string newformat, out CompressionType? compressionType, out string fileext)
        {
            ArchiveType? arctype = null;
            compressionType = null;
            fileext = null;

            newformat = newformat.ToLower();

            switch(newformat)
            {
                case "rar":
                    arctype = ArchiveType.Rar;
                    compressionType = CompressionType.Rar;
                    fileext = ".rar";
                    break;
                case "gzip":
                    arctype = ArchiveType.GZip;
                    compressionType = CompressionType.GZip;
                    fileext = ".gzip";
                    break;
                case "tar":
                    arctype = ArchiveType.Tar;
                    compressionType = CompressionType.BCJ2;
                    fileext = ".tar";
                    break;
                case "zip":
                    arctype = ArchiveType.Zip;
                    compressionType = CompressionType.BZip2;
                    fileext = ".zip";
                    break;
                case "7z":
                case "sevenzip":
                    arctype = ArchiveType.SevenZip;
                    compressionType = CompressionType.LZMA;
                    fileext = ".7z";
                    break;
                default:
                    break;
            }

            return arctype;
        }

        /// <summary>
        /// Output the applicaiton usage information to the console
        /// </summary>
        private static void Usage()
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine("Usage: SharpRepack [-help] OR SharpRepack {new format} {filename}");
            Console.WriteLine($"\tSupported formats are: {string.Join(", ", Enum.GetNames(typeof(SharpCompress.Common.ArchiveType)))}");


            Console.ForegroundColor = oldColor;
        }

        #region Utility Functions

        /// <summary>
        /// The different types of output that the local <code>Write()</code>
        /// funciton supports
        /// </summary>
        private enum OutputType { Standard, Info, Warn, Error }

        /// <summary>
        /// Local function to write output to the screen
        /// </summary>
        /// <param name="output"></param>
        /// <param name="outputType"></param>
        /// <param name="tabCount"></param>
        static void Write(string output, OutputType outputType = OutputType.Standard, int? tabCount = null)
        {
            var previousColor = Console.ForegroundColor;
            var textColor = previousColor;

            switch (outputType)
            {
                case OutputType.Info:
                    textColor = ConsoleColor.Gray;
                    tabCount = tabCount ?? 0;
                    break;
                case OutputType.Warn:
                    textColor = ConsoleColor.Yellow;
                    tabCount = tabCount ?? 1;
                    break;
                case OutputType.Error:
                    textColor = ConsoleColor.Red;
                    tabCount = tabCount ?? 2;
                    break;
                case OutputType.Standard:
                default:
                    tabCount = tabCount ?? 0;
                    break;
            }


            var tabs = new String('\t', tabCount.Value);

            Console.WriteLine($"{tabs}{output}");

            Console.ForegroundColor = previousColor;

        }

        static void WriteError(string output)
        {
            Write(output, OutputType.Error);
        }

        static void WriteInfo(string output)
        {
            Write(output, OutputType.Info);
        }

        static void WriteInfo1(string output)
        {
            Write(output, OutputType.Info, 1);
        }

        static void WriteInfo2(string output)
        {
            Write(output, OutputType.Info, 2);
        }

        #endregion

    }

}