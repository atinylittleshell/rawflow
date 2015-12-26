using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RawFlow
{
    class Program
    {
        private static ILog _logger = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            _logger.Info("RawFlow started");

            foreach(var mlvFile in args.OrderBy(f => f))
            {
                _logger.InfoFormat("Processing {0}", mlvFile);

                if (Path.GetExtension(mlvFile).ToLower() != ".mlv")
                {
                    _logger.InfoFormat("Skipping {0} because it's not an mlv", mlvFile);
                    continue;
                }

                if (File.Exists(mlvFile))
                {
                    ProcessFile(mlvFile);
                }
                else
                {
                    _logger.InfoFormat("Skipping {0} because it doesn't exist", mlvFile);
                    continue;
                }
            }

            _logger.Info("RawFlow ended");
        }

        private static void ProcessFile(string mlvFile)
        {
            var outputDirectory = Settings.OutputDirectory;
            if (string.IsNullOrEmpty(outputDirectory))
            {
                outputDirectory = Path.GetDirectoryName(mlvFile) + "\\";
            }

            var resultDirectory = Path.Combine(outputDirectory, Path.GetFileName(mlvFile) + ".RawFlow") + "\\";
            if (Directory.Exists(resultDirectory))
            {
                _logger.InfoFormat("Skipping {0} because result directory already exists", mlvFile);
                return;
            }
            else
            {
                Directory.CreateDirectory(resultDirectory);
            }

            var temporaryWorkingDirectory = Settings.TemporaryWorkingDirectory;
            if (string.IsNullOrEmpty(temporaryWorkingDirectory))
            {
                temporaryWorkingDirectory = resultDirectory;
            }

            foreach (var existingTempFile in Directory.GetFiles(temporaryWorkingDirectory))
            {
                File.Delete(existingTempFile);
            }

            ExtractDNGs(mlvFile, temporaryWorkingDirectory);

            if (Settings.GenerateProxyVideo)
            {
                GenerateTIFFs(temporaryWorkingDirectory);

                GenerateProxyVideo(mlvFile, temporaryWorkingDirectory);

                Cleanup(temporaryWorkingDirectory);
            }

            if (temporaryWorkingDirectory != resultDirectory)
            {
                _logger.Info("Moving result files from temporary directory into result directory");

                foreach (var file in Directory.GetFiles(temporaryWorkingDirectory))
                {
                    File.Move(file, Path.Combine(resultDirectory, Path.GetFileName(file)));
                }
            }
        }

        private static void Cleanup(string temporaryWorkingDirectory)
        {
            _logger.Info("Cleaning up temporary files");

            foreach (var tiffFile in Directory.GetFiles(temporaryWorkingDirectory).Where(f => f.ToLower().EndsWith(".tiff")))
            {
                File.Delete(tiffFile);
            }
        }

        private static void GenerateProxyVideo(string mlvFile, string temporaryWorkingDirectory)
        {
            _logger.Info("Generating proxy video");

            var startFfmpeg = new ProcessStartInfo();
            startFfmpeg.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
            startFfmpeg.WorkingDirectory = temporaryWorkingDirectory;
            startFfmpeg.UseShellExecute = false;
            startFfmpeg.CreateNoWindow = true;
            startFfmpeg.WindowStyle = ProcessWindowStyle.Hidden;
            startFfmpeg.Arguments = string.Format("-f image2 -framerate 23.976 -i frame_%06d.tiff -vf scale=480:-1 -c libx264 -crf 23 {0}.mp4", Path.GetFileName(mlvFile));

            var ffmpegProcess = Process.Start(startFfmpeg);
            ffmpegProcess.WaitForExit();
        }

        private static void GenerateTIFFs(string temporaryWorkingDirectory)
        {
            _logger.Info("Generating TIFFs for proxy video");

            foreach (var dngFile in Directory.GetFiles(temporaryWorkingDirectory).Where(f => f.ToLower().EndsWith(".dng")))
            {
                var startDcraw = new ProcessStartInfo();
                startDcraw.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dcraw.exe");
                startDcraw.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                startDcraw.UseShellExecute = false;
                startDcraw.CreateNoWindow = true;
                startDcraw.WindowStyle = ProcessWindowStyle.Hidden;
                startDcraw.Arguments = string.Format("-w -o 1 -q 3 -T \"{0}\"", dngFile);

                var dcrawProcess = Process.Start(startDcraw);
                dcrawProcess.WaitForExit();
            }
        }

        private static void ExtractDNGs(string mlvFile, string temporaryWorkingDirectory)
        {
            _logger.Info("Extracting DNGs");

            var startMlvDump = new ProcessStartInfo();
            startMlvDump.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mlv_dump.exe");
            startMlvDump.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            startMlvDump.UseShellExecute = false;
            startMlvDump.CreateNoWindow = true;
            startMlvDump.WindowStyle = ProcessWindowStyle.Hidden;
            startMlvDump.Arguments = string.Format("--dng \"{0}\" -o \"{1}frame_\"", mlvFile, temporaryWorkingDirectory);

            var mlvDumpProcess = Process.Start(startMlvDump);
            mlvDumpProcess.WaitForExit();
        }
    }
}
