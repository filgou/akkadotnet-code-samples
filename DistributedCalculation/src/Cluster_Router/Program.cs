using System;
using Actors;
using Akka.Actor;
using Akka.Cluster.Tools.Client;
using Akka.Configuration;
using Cluster_Server.Actors;

namespace Cluster_Router
{
    class Program
    {
        public static ActorSystem MyActorSystem;

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Title = $"{NamesRegistry.Router}:{Ports.Router}";
        
        Config config = AkkaDistributedHelper.GetAkkaSettings();
            string routerAddress = $"{AkkaDistributedHelper.GetFullyQualifiedDomainName()}:{Ports.Router}";
            config = config
                .WithFallback(string.Format("akka.remote.dot-netty.tcp.hostname = \"{0}\"",
                    AkkaDistributedHelper.GetFullyQualifiedDomainName()))
                .WithFallback(string.Format("akka.remote.dot-netty.tcp.port = {0}", Ports.Router))
                .WithFallback(string.Format("akka.cluster.roles = [\"{0}\"]", NamesRegistry.Router))
                .WithFallback(string.Format("akka.cluster.seed-nodes = [\"{1}://App-Cluster@{0}\"]",
                    routerAddress, AkkaDistributedHelper.TcpProtocol));

            MyActorSystem = ActorSystem.Create(NamesRegistry.Cluster, config);

            Props props = Props.Create<RouterActor>(MyActorSystem);
            var routerActor = MyActorSystem.ActorOf(props, NamesRegistry.Router);
            ClusterClientReceptionist.Get(MyActorSystem).RegisterService(routerActor);
            Console.WriteLine("Router running...");

            // This blocks the current thread from exiting until MyActorSystem is shut down
            // The ConsoleReaderActor will shut down the ActorSystem once it receives an 
            // "exit" command from the user
            MyActorSystem.WhenTerminated.Wait();
            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }
    }
}
