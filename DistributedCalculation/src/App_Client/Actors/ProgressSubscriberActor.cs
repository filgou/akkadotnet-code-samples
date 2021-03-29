using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Akka.Actor;
using Akka.Cluster.Tools.Client;
using Cluster_Server.Actors;

namespace App_Client.Actors
{
    public class ProgressSubscriberActor : UntypedActor
    {
        protected override void OnReceive(object message)
        {
            if (message is ProgressPublisherActor.ProgressUpdate update)
            {
                Console.WriteLine($"Received Progress Update: {update.Progress}");
            }
            else
            {
                Console.WriteLine($"Received other message : {message}");
            }
        }
    }
}
