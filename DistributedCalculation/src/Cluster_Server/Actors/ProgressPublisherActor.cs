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
        private string _lastUpdate;
        private IActorRef _subscriber;

        public class ProgressUpdateRequest
        {
            public string RespondTo { get; set; }
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
                if (message is ProgressUpdateRequest progressUpdateRequest)
                {
                    _subscriber = Context.System.ActorSelection(progressUpdateRequest.RespondTo).ResolveOne(TimeSpan.FromSeconds(30))
                        .Result;
                    Console.WriteLine($"{Self.Path}: Progress Publisher received subscriber: {_subscriber.Path}.");
                }
                else if (message is ProgressUpdate progressUpdate)
                {
                    _lastUpdate = progressUpdate.Progress;
                    Console.WriteLine($"{Self.Path}: Progress Publisher forwarding message: {progressUpdate}.");
                    _subscriber.Tell(progressUpdate, Self);
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
           c.RegisterService(Self);
        }
    }
}
