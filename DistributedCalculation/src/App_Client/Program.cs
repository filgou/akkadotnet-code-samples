using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net.NetworkInformation;
using System.Threading;
using Actors;
using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Cluster.Tools.Client;
using Akka.Configuration;
using App_Client.Actors;
using Cluster_Server.Actors;

namespace App_Client
{
    class Program
    {
        public static ActorSystem MyActorSystem;
        private const string TcpProtocol = "akka.tcp";

        static void Main(string[] args)
        {
            Console.Title = "Client";
            Console.ForegroundColor = ConsoleColor.DarkMagenta;

            Config config = AkkaDistributedHelper.GetAkkaSettings();
            string routerAddress = $"{AkkaDistributedHelper.GetFullyQualifiedDomainName()}:{Ports.Client}";
            config = config
                .WithFallback(string.Format("akka.remote.dot-netty.tcp.hostname = \"{0}\"",
                    AkkaDistributedHelper.GetFullyQualifiedDomainName()))
                .WithFallback(string.Format("akka.remote.dot-netty.tcp.port = {0}", Ports.Client))
                .WithFallback(string.Format("akka.cluster.roles = [\"{0}\"]", NamesRegistry.Client))
                .WithFallback(string.Format("akka.cluster.seed-nodes = [\"{1}://App-Client@{0}\"]",
                    routerAddress, AkkaDistributedHelper.TcpProtocol));
            MyActorSystem = ActorSystem.Create("App-Client", config);

            
            bool isClusterClientInitialized = false;
            string routerActorPath = string.Format("{3}://App-Cluster@{0}:{1}/user/{2}", AkkaDistributedHelper.GetFullyQualifiedDomainName(), Ports.Router, NamesRegistry.Router, TcpProtocol);
            
            while (true)
            {
                Console.WriteLine("Press any key to trigger evaluation, q to exit");
                if (Console.ReadKey().KeyChar != 'q')
                {
                    if (!isClusterClientInitialized)
                    {
                        InitializeProgressSubscription();

                        isClusterClientInitialized = true;
                    }

                    MyActorSystem.ActorSelection(routerActorPath).ResolveOne(TimeSpan.FromSeconds(30)).Result.Tell(new RouterActor.EvaluationRequested());
                }
                else
                {
                    break;
                }
            }
            
            IActorRef router = MyActorSystem.ActorSelection(routerActorPath).ResolveOne(TimeSpan.FromSeconds(30)).Result;
            router.Tell(new RouterActor.ShutdownRequested());
            Console.WriteLine();
            Console.WriteLine("Exiting, sent shutdown...");
        }

        private static void InitializeProgressSubscription()
        {
            string receptionistActorPath = string.Format("{3}://App-Cluster@{0}:{1}/system/{2}",
                AkkaDistributedHelper.GetFullyQualifiedDomainName(), Ports.Router, "receptionist", TcpProtocol);
            ImmutableHashSet<ActorPath> initialContacts = ImmutableHashSet.Create(ActorPath.Parse(receptionistActorPath));

            var settings = ClusterClientSettings.Create(MyActorSystem).WithInitialContacts(initialContacts);
            IActorRef clusterClient = MyActorSystem.ActorOf(ClusterClient.Props(settings), "Client");
            
            MyActorSystem.ActorOf(Props.Create<ProgressSubscriberActor>(), NamesRegistry.ProgressSubscriber);
            var subscirberAddress = string.Format("{3}://App-Client@{0}:{1}/user/{2}",
                AkkaDistributedHelper.GetFullyQualifiedDomainName(), Ports.Client, NamesRegistry.ProgressSubscriber, TcpProtocol);
            clusterClient.Tell(new ClusterClient.SendToAll($"/user/{NamesRegistry.ProgressPublisher}",
                new ProgressPublisherActor.ProgressUpdateRequest {RespondTo = subscirberAddress}));
            Console.WriteLine($"Created Subscriber for progress updates: {clusterClient.Path} on {MyActorSystem.Name}");
        }
    }
}
