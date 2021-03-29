using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Web;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Configuration;
using Akka.Configuration.Hocon;

namespace Actors
{
    public class AkkaDistributedHelper
    {
        public const string TcpProtocol = "akka.tcp";

        public static bool ActorExists(ActorSystem system, string actorPath, out IActorRef actor)
        {
            try
            {
                actor = system.ActorSelection(actorPath).ResolveOne(TimeSpan.FromSeconds(3)).Result;
            }
            catch (AggregateException)
            {
                actor = null;
            }

            return actor != null;
        }

        public static Config GetAkkaSettings()
        {
            var defaultC = DistributedPubSub.DefaultConfig();
            
            AkkaConfigurationSection configurationSection = (AkkaConfigurationSection)ConfigurationManager.GetSection("akka") ?? new AkkaConfigurationSection();
            Config config = ConfigurationFactory.ParseString(GetHoconForEnvironmentVariables() + configurationSection.Hocon.Content);
            return config;
        }

        private static string GetHoconForEnvironmentVariables()
        {
            Hashtable environmentVariables = (Hashtable)Environment.GetEnvironmentVariables();

            StringBuilder sb = new StringBuilder();

            sb.Append("env {");

            foreach (DictionaryEntry environmentVariable in environmentVariables)
            {
                sb.AppendFormat("\"{0}\" = \"{1}\"", environmentVariable.Key, HttpUtility.JavaScriptStringEncode(environmentVariable.Value.ToString())).AppendLine();
            }

            sb.Append("}");

            return sb.ToString();
        }

        public static Config GetAkkaSettings(int focusPort)
        {
            Config config = GetAkkaSettings()
                .WithFallback(string.Format("akka.remote.dot-netty.tcp.port = {0}", focusPort))
                .WithFallback(string.Format("akka.remote.dot-netty.tcp.hostname = \"{0}\"", GetFullyQualifiedDomainName()));
            return config;
        }

        public static string GetFullyQualifiedDomainName()
        {
            string domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
            domainName = "." + domainName;

            string hostName = Dns.GetHostName();

            if (!hostName.EndsWith(domainName, StringComparison.InvariantCulture)) // if hostname does not already include domain name
            {
                hostName += domainName; // add the domain name part
            }

            return hostName;
        }
    }
}
