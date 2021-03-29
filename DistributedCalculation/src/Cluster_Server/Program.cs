using System;
using Actors;
using Akka.Actor;
using Akka.Cluster.Tools.Client;
using Akka.Configuration;
using Cluster_Server.Actors;

namespace Cluster_Server
{
    class Program
    {
        public static ActorSystem MyActorSystem { get; set; }
        private static IActorRef _progressPublisherActor;

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Title = $"{NamesRegistry.Server}:{Ports.Server}";
            Config config = AkkaDistributedHelper.GetAkkaSettings();
            string routerAddress = $"{AkkaDistributedHelper.GetFullyQualifiedDomainName()}:{Ports.Router}";
            config = config
                .WithFallback(string.Format("akka.remote.dot-netty.tcp.hostname = \"{0}\"",
                    AkkaDistributedHelper.GetFullyQualifiedDomainName()))
                .WithFallback(string.Format("akka.remote.dot-netty.tcp.port = {0}", Ports.Server))
                .WithFallback(string.Format("akka.cluster.roles = [\"{0}\"]", NamesRegistry.Server))
                .WithFallback(string.Format("akka.cluster.seed-nodes = [\"{1}://App-Cluster@{0}\"]",    
                    routerAddress, AkkaDistributedHelper.TcpProtocol));

            MyActorSystem = ActorSystem.Create(NamesRegistry.Cluster, config);

            CreateProgressPublisher();
            CreateEvaluatorActor();

            // This blocks the current thread from exiting until MyActorSystem is shut down
            // The ConsoleReaderActor will shut down the ActorSystem once it receives an 
            // "exit" command from the user
            MyActorSystem.WhenTerminated.Wait();
        }

        private static void CreateProgressPublisher()
        {
            _progressPublisherActor = MyActorSystem.ActorOf(Props.Create<ProgressPublisherActor>(), NamesRegistry.ProgressPublisher);
            Console.WriteLine($"{_progressPublisherActor.Path} on {MyActorSystem.Name}: Progress Tracker ready...");
        }

        private static void CreateEvaluatorActor()
        {
            Props props = Props.Create<ServerActor>(_progressPublisherActor);
            IActorRef _actor = MyActorSystem.ActorOf(props, NamesRegistry.Server);
            Console.WriteLine($"{_actor.Path} on {MyActorSystem.Name}: Evaluator ready...");
        }
    }
}
