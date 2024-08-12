using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordPlexRichPresence
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                });
    }

    public class Worker(ILogger<Worker> logger) : BackgroundService
    {
        private readonly ILogger<Worker> _logger = logger;

        private Process? _pythonProcess = null;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var plexProcess = Process.GetProcessesByName("Plex");
                if (plexProcess.Length > 0)
                {
                    if (_pythonProcess == null || _pythonProcess.HasExited)
                    {
                        _logger.LogInformation("Plex is running. Starting Python script.");
                        StartPythonScript();
                    }
                }
                else
                {
                    if (_pythonProcess != null && !_pythonProcess.HasExited)
                    {
                        _logger.LogInformation("Plex is not running. Stopping Python script.");
                        _pythonProcess.Kill();
                        _pythonProcess = null;
                    }
                }

                await Task.Delay(5000, stoppingToken); // Check every 5 seconds
            }
        }

        private void StartPythonScript()
        {
            ProcessStartInfo start = new()
            {
                FileName = "python.exe", // Use the actual path to python.exe
                Arguments = "main.py",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = @"C:\Program Files (x86)\discord-rich-presence-plex"
            };

            _pythonProcess = new Process { StartInfo = start };
            _pythonProcess.Start();

            _pythonProcess.BeginOutputReadLine();
            _pythonProcess.BeginErrorReadLine();

            _pythonProcess.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
            _pythonProcess.ErrorDataReceived += (sender, args) => Console.WriteLine(args.Data);

        }



    }
}
