using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Akka.Actor;
using Akka.Util.Internal;

namespace Cluster_Server.Actors
{
    public class ServerActor : ReceiveActor
    {
        private readonly IActorRef _progressPublisher;
        
        public class JobRequested
        {
            public int JobSize { get; set; } = 300;
        }

        public class ShutdownRequested
        {
        }

        public ServerActor(IActorRef progressPublisher)
        {
            _progressPublisher = progressPublisher;
            Receive<JobRequested>(s =>
            {
                Console.WriteLine($"Received package with size {s.JobSize}");

                for (int i = 0; i < s.JobSize; i++)
                {
                    _progressPublisher.Tell(new ProgressPublisherActor.ProgressUpdate { Progress = i.ToString() }, Self);
                    Thread.Sleep(1000);
                    Console.WriteLine($"Processed {i}");
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
        }
    }
}
