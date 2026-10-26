using Grpc.Core;
using ProtoBuf.Grpc.Configuration;
using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace protobuf_net.Grpc.Test.Issues
{
    public class Issue408
    {
        [Fact]
        public async Task ConcurrentFirstClientCreationDoesNotThrow()
        {
            const int Rounds = 200;
            int threads = Math.Max(Environment.ProcessorCount, 4);
            var invoker = new NullCallInvoker();
            var cancellationToken = TestContext.Current.CancellationToken;

            for (int round = 0; round < Rounds; round++)
            {
                var factory = ClientFactory.Create(BinderConfiguration.Create([ProtoBufMarshallerFactory.Default]));
                using var barrier = new Barrier(threads);
                var tasks = new Task[threads];
                for (int i = 0; i < threads; i++)
                {
                    tasks[i] = Task.Factory.StartNew(() =>
                    {
                        barrier.SignalAndWait(cancellationToken);
                        factory.CreateClient<IRaceService>(invoker);
                    }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                }
                await Task.WhenAll(tasks);
            }
        }

        [ServiceContract]
        public interface IRaceService
        {
            [OperationContract]
            ValueTask<RaceMessage> Call(RaceMessage value);
        }

        [DataContract]
        public class RaceMessage { }

        private sealed class NullCallInvoker : CallInvoker
        {
            public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) => throw new NotSupportedException();
            public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) => throw new NotSupportedException();
            public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) => throw new NotSupportedException();
            public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options) => throw new NotSupportedException();
            public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options) => throw new NotSupportedException();
        }
    }
}
