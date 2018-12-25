using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using log4net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using PlacardAnalyser.Analyser;
using PlacardAnalyser.Configuration;
using PlacardAPI;

namespace PlacardAnalyser
{
    static class Program
    {
        static readonly ILog logger = log4net.LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            // load Logger
            Stopwatch stopwatch = Stopwatch.StartNew();
            var logRepository = log4net.LogManager.GetRepository(System.Reflection.Assembly.GetEntryAssembly());

            using (var stream = Environment.ExpandEnvironmentVariables(File.ReadAllText(
                new FileInfo("log4net.config").FullName)).ToStream())
            {
                log4net.Config.XmlConfigurator.Configure(logRepository, stream);
            }

            // load configuration
            logger.Info("Starting service...");

            logger.Info("Load configuration file");

            Settings settings = new Settings();

            try
            {
                var info = Environment.ExpandEnvironmentVariables(File.ReadAllText(new FileInfo("appsettings.json").FullName));
                var memoryJsonFile = new MemoryFileInfo("config.json",Encoding.UTF8.GetBytes(info),DateTimeOffset.Now);
                var memoryFileProvider = new MockFileProvider(memoryJsonFile);
                var builder = new ConfigurationBuilder()
                    .AddJsonFile(memoryFileProvider, "config.json",false,false);
                    // .AddJsonFile("appsettings.json")
                    // .AddEnvironmentVariables();
                var configuration = builder.Build();

                ConfigurationBinder.Bind(configuration, settings);
            }
            catch (Exception ex)
            {
                logger.Error("Cannot parse configuration file.", ex);
                Environment.Exit(1);
            }

            // start processor
            var processor = new AnalyserProcessor(settings);
            processor.Start();

            // Ending service
            logger.Info(string.Format("Elapsed time: {0:00}:{1:00}:{2:00}.{3:000}",
                                      stopwatch.Elapsed.Hours, stopwatch.Elapsed.Minutes,
                                      stopwatch.Elapsed.Seconds, stopwatch.Elapsed.Milliseconds));
            logger.Info("Service finish.");

            var client = new APIClient();
            // client.GetFullSportsBook("FullSportsBook.json");
            // client.GetNextEvents("NextEvents.json");
            // client.GetInfo("Info.json");
            client.GetFaq("Faq.json");
        }

        static Stream ToStream(this string str)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(str);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }

    public class MockFileProvider : IFileProvider
    {
        private IEnumerable<IFileInfo> _files;
        private Dictionary<string, IChangeToken> _changeTokens;

        public MockFileProvider()
        {}

        public MockFileProvider(params IFileInfo[] files)
        {
            _files = files;
        }

        public MockFileProvider(params KeyValuePair<string, IChangeToken>[] changeTokens)
        {
            _changeTokens = changeTokens.ToDictionary(
                changeToken => changeToken.Key,
                changeToken => changeToken.Value,
                StringComparer.Ordinal);
        }

        public IDirectoryContents GetDirectoryContents(string subpath) => NotFoundDirectoryContents.Singleton;

        public IFileInfo GetFileInfo(string subpath)
        {
            var file = _files.FirstOrDefault(f => f.Name == subpath);
            return file ?? new NotFoundFileInfo(subpath);
        }

        public IChangeToken Watch(string filter)
        {
            if (_changeTokens != null && _changeTokens.ContainsKey(filter))
            {
                return _changeTokens[filter];
            }
            return NullChangeToken.Singleton;
        }
    }

    public class MemoryFileInfo : IFileInfo
    {
        readonly byte[] _content;

        public MemoryFileInfo(string name, byte[] content, DateTimeOffset timestamp)
        {
            Name = name;
            _content = content;
            LastModified = timestamp;
        }

        public bool Exists => true;

        long IFileInfo.Length => _content.LongLength;

        public string PhysicalPath => null;

        public string Name { get; }

        public DateTimeOffset LastModified { get; }

        public bool IsDirectory => false;

        public Stream CreateReadStream()
        {
            return new MemoryStream(_content);
        }
    }
}
