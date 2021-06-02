using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Actors;
using Akka.Actor;
using Akka.Cluster.Tools.Client;
using Akka.Pattern;

namespace Cluster_Server.Actors
{
    public class RouterActor : ReceiveActor
    {
        private readonly ActorSystem _actorSystem;

        public int PackageSize { get; set; } = 100;

        public class EvaluationRequested
        {
        }

        public class ShutdownRequested
        {
        }

        public RouterActor(ActorSystem actorSystem)
        {
            _actorSystem = actorSystem;
            Receive<EvaluationRequested>(s =>
            {
                IActorRef server;

                string serverActorPath = string.Format("{3}://App-Cluster@{0}:{1}/user/{2}",
                    AkkaDistributedHelper.GetFullyQualifiedDomainName(), Ports.Server,
                    NamesRegistry.Server,
                    AkkaDistributedHelper.TcpProtocol);
                if (AkkaDistributedHelper.ActorExists(_actorSystem, serverActorPath, out server))
                {
                    Console.WriteLine("Sending evaluation to server...");
                    server.Tell(new ServerActor.JobRequested(), Self);
                }
                else
                {
                    Console.WriteLine("No server found, retry...");
                }
            });

            Receive<ShutdownRequested>(s =>
            {
                Console.WriteLine("Received shutdown, exiting!");
                // shut down the entire actor system via the ActorContext
                // causes MyActorSystem.AwaitTermination(); to stop blocking the current thread
                // and allows the application to exit.
                Context.System.Terminate();
            });

            Receive<ProgressPublisherActor.ProgressUpdate>(s =>
            {
                Console.WriteLine($"Received progress update, {s.Progress}");
                // shut down the entire actor system via the ActorContext
                // causes MyActorSystem.AwaitTermination(); to stop blocking the current thread
                // and allows the application to exit.
                Context.System.Terminate();
            });
        }

        protected override void PreStart()
        {
            var c = ClusterClientReceptionist.Get(Context.System);
            //c.RegisterService(Self);
            
        }
    }
}
