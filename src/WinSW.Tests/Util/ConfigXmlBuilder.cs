﻿using System.Collections.Generic;
using System.Text;
using WinSW.Plugins.RunawayProcessKiller;
using WinSW.Tests.Extensions;
using Xunit.Abstractions;

namespace WinSW.Tests.Util
{
    /// <summary>
    /// Configuration XML builder, which simplifies testing of WinSW Configuration file.
    /// </summary>
    internal class ConfigXmlBuilder
    {
        public string Name { get; set; }

        public string Id { get; set; }

        public string Description { get; set; }

        public string Executable { get; set; }

        public bool PrintXmlVersion { get; set; }

        public string XmlComment { get; set; }

        public List<string> ExtensionXmls { get; } = new List<string>();

        private readonly List<string> configEntries = new List<string>();

        private readonly ITestOutputHelper output;

        private ConfigXmlBuilder(ITestOutputHelper output)
        {
            this.output = output;
        }

        public static ConfigXmlBuilder Create(
            ITestOutputHelper output,
            string id = null,
            string name = null,
            string description = null,
            string executable = null,
            bool printXmlVersion = true,
            string xmlComment = "")
        {
            return new ConfigXmlBuilder(output)
            {
                Id = id ?? "myapp",
                Name = name ?? "MyApp Service",
                Description = description ?? "MyApp Service (powered by WinSW)",
                Executable = executable ?? "%BASE%\\myExecutable.exe",
                PrintXmlVersion = printXmlVersion,
                XmlComment = (xmlComment != null && xmlComment.Length == 0)
                    ? "Just a sample configuration file generated by the test suite"
                    : xmlComment,
            };
        }

        public string ToXmlString(bool dumpConfig = false)
        {
            StringBuilder str = new StringBuilder();
            if (this.PrintXmlVersion)
            {
                // TODO: The encoding is generally wrong
                str.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n");
            }

            if (this.XmlComment != null)
            {
                str.AppendFormat("<!--{0}-->\n", this.XmlComment);
            }

            str.Append("<service>\n");
            str.AppendFormat("  <id>{0}</id>\n", this.Id);
            str.AppendFormat("  <name>{0}</name>\n", this.Name);
            str.AppendFormat("  <description>{0}</description>\n", this.Description);
            str.AppendFormat("  <executable>{0}</executable>\n", this.Executable);
            foreach (string entry in this.configEntries)
            {
                // We do not care much about pretty formatting here
                str.AppendFormat("  {0}\n", entry);
            }

            // Extensions
            if (this.ExtensionXmls.Count > 0)
            {
                str.Append("  <extensions>\n");
                foreach (string xml in this.ExtensionXmls)
                {
                    str.Append(xml);
                }

                str.Append("  </extensions>\n");
            }

            str.Append("</service>\n");
            string res = str.ToString();
            if (dumpConfig)
            {
                this.output.WriteLine("Produced config:");
                this.output.WriteLine(res);
            }

            return res;
        }

        public ServiceDescriptor ToServiceDescriptor(bool dumpConfig = false)
        {
            return ServiceDescriptor.FromXml(this.ToXmlString(dumpConfig));
        }

        public ConfigXmlBuilder WithRawEntry(string entry)
        {
            this.configEntries.Add(entry);
            return this;
        }

        public ConfigXmlBuilder WithTag(string tagName, string value)
        {
            return this.WithRawEntry(string.Format("<{0}>{1}</{0}>", tagName, value));
        }

        public ConfigXmlBuilder WithRunawayProcessKiller(RunawayProcessKillerExtension ext, string extensionId = "killRunawayProcess", bool enabled = true)
        {
            var fullyQualifiedExtensionName = ExtensionTestBase.GetExtensionClassNameWithAssembly(typeof(RunawayProcessKillerExtension));
            StringBuilder str = new StringBuilder();
            str.AppendFormat("    <extension enabled=\"{0}\" className=\"{1}\" id=\"{2}\">\n", new object[] { enabled, fullyQualifiedExtensionName, extensionId });
            str.AppendFormat("      <pidfile>{0}</pidfile>\n", ext.Pidfile);
            str.AppendFormat("      <stopTimeout>{0}</stopTimeout>\n", ext.StopTimeout.TotalMilliseconds);
            str.AppendFormat("      <checkWinSWEnvironmentVariable>{0}</checkWinSWEnvironmentVariable>\n", ext.CheckWinSWEnvironmentVariable);
            str.Append("    </extension>\n");
            this.ExtensionXmls.Add(str.ToString());

            return this;
        }

        public ConfigXmlBuilder WithDownload(Download download)
        {
            StringBuilder xml = new StringBuilder();
            xml.Append($"<download from=\"{download.From}\" to=\"{download.To}\" failOnError=\"{download.FailOnError}\"");

            // Authentication
            if (download.Auth != Download.AuthType.None)
            {
                xml.Append($" auth=\"{download.Auth}\"");
                if (download.Auth == Download.AuthType.Basic)
                {
                    string username = download.Username;
                    if (username != null)
                    {
                        xml.Append($" user=\"{username}\"");
                    }

                    string password = download.Password;
                    if (password != null)
                    {
                        xml.Append($" password=\"{password}\"");
                    }
                }

                if (download.UnsecureAuth)
                {
                    xml.Append(" unsecureAuth=\"true\"");
                }
            }

            xml.Append("/>");

            return this.WithRawEntry(xml.ToString());
        }

        public ConfigXmlBuilder WithDelayedAutoStart()
        {
            return this.WithRawEntry("<delayedAutoStart>true</delayedAutoStart>");
        }
    }
}
