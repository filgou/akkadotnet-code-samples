using System;
using System.Threading;
using System.Threading.Tasks;
using Actors;
using Akka.Actor;
using Cluster_Server.Actors;

namespace App_NoPortOpen
{
    class Program
    {

        public static ActorSystem MyActorSystem;
        private const string TcpProtocol = "akka.tcp";

        static void Main(string[] args)
        {
            Console.Title = "Client - No Port";
            Console.ForegroundColor = ConsoleColor.DarkMagenta;

            MyActorSystem = ActorSystem.Create("App-Client");


            bool isClusterClientInitialized = false;
            string routerActorPath = string.Format("{3}://App-Cluster@{0}:{1}/user/{2}", AkkaDistributedHelper.GetFullyQualifiedDomainName(), Ports.Router, NamesRegistry.Router, TcpProtocol);
            string progressActorPath = string.Format("{3}://App-Cluster@{0}:{1}/user/{2}", AkkaDistributedHelper.GetFullyQualifiedDomainName(), Ports.Server, NamesRegistry.ProgressPublisher, TcpProtocol);

            while (true)
            {
                Console.WriteLine("Press any key to trigger evaluation, q to exit");
                if (Console.ReadKey().KeyChar != 'q')
                { 
                    if (!isClusterClientInitialized)
                    {
                        
                        IActorRef progressPublisher = MyActorSystem.ActorSelection(progressActorPath).ResolveOne(TimeSpan.FromSeconds(30)).Result;
                        int delay = 100;
                        var cancellationTokenSource = new CancellationTokenSource();
                        var token = cancellationTokenSource.Token;
                        var listener = Task.Factory.StartNew(async () =>
                        {
                            while (true)
                            {
                                try
                                {
                                    var progressUpdate =
                                        (ProgressPublisherActor.ProgressUpdate)await progressPublisher.Ask(
                                            new ProgressPublisherActor.ProgressUpdateRequest());
                                    Console.WriteLine($"Current Status: {progressUpdate.Progress}");
                                    Thread.Sleep(delay);
                                    if (token.IsCancellationRequested)
                                        break;
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }
                            }

                            // cleanup, e.g. close connection
                        }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                        isClusterClientInitialized = true;
                    }

                    MyActorSystem.ActorSelection(routerActorPath).ResolveOne(TimeSpan.FromSeconds(30)).Result.Tell(new RouterActor.EvaluationRequested());
                }
                else
                {
                    break;
                }
            }

            Console.WriteLine();
            Console.WriteLine("Exiting, sent shutdown...");
        }
    }
}

