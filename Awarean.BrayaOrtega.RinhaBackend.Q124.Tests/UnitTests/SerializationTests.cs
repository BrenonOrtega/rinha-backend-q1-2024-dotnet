using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using NATS.Client.Core;
using NATS.Client.Serializers.Json;
using Npgsql.Internal;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Tests.UnitTests
{
    public class SerializationTests
    {
        private readonly JsonSerializerOptions options;

        public SerializationTests()
        {
            options = new JsonSerializerOptions();
            options.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
        }

        [Fact]
        public void Transaction_Should_Work()
        {
            var transaction = new Transaction(1000, 'c', "aaaaa", 1, 10_000, 20_000, DateTime.Now);


            var @string = JsonSerializer.Serialize(transaction, options);

            var actual = JsonSerializer.Deserialize<Transaction>(@string, options);

            actual.Should().BeEquivalentTo(transaction);
        }

        [Fact]
        public void Nats_Serialization_Should_Work()
        {
            var transaction = new Transaction(1000, 'c', "aaaaa", 1, 10_000, 20_000, DateTime.Now);

            var serializer = new NatsJsonSerializer<Transaction>(options);

            // var buffer = new NatsBufferWriter<Transaction>();
            //serializer.Serialize(buffer, transaction);

            // var actual = JsonSerializer.Deserialize<Transaction>(@string);

            //actual.Should().BeEquivalentTo(transaction);
        }
    }
}