using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Actors;
using Akka.Actor;
using Akka.Cluster.Tools.Client;
using Akka.Cluster.Tools.PublishSubscribe;

namespace Cluster_Server.Actors
{
    public class ProgressPublisherActor : UntypedActor
    {
        private string _lastUpdate = "not started";
        private IActorRef _subscriber;

        public class ProgressUpdateRequest
        {
        }

        public class ProgressUpdate
        {
            public string Progress { get; set; }
        }

        public ProgressPublisherActor()
        {
        }

        protected override void OnReceive(object message)
        {
            try
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Cyan;
               
                if (message is ProgressUpdate progressUpdate)
                {
                    _lastUpdate = progressUpdate.Progress;
                    Console.WriteLine($"{Self.Path}: Progress Publisher acknowledged progress update message: {progressUpdate.Progress}.");
                }
                else if (message is ProgressUpdateRequest progressUpdateRequest)
                {
                    Sender.Tell(new ProgressUpdate {Progress = _lastUpdate});
                    Console.WriteLine($"{Self.Path}: Progress Publisher replying message: {Sender.Path}.");
                }

                Console.ForegroundColor = originalColor;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
           
        }

        protected override void PreStart()
        {
           var c = ClusterClientReceptionist.Get(Context.System);
           //c.RegisterService(Self);
           c.RegisterSubscriber(NamesRegistry.ProgressTopic, Self);
        }
    }
}
