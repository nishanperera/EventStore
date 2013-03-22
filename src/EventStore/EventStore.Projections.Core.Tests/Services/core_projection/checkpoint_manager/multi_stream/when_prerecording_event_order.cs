﻿// Copyright (c) 2012, Event Store LLP
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
// 
// Redistributions of source code must retain the above copyright notice,
// this list of conditions and the following disclaimer.
// Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
// Neither the name of the Event Store LLP nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EventStore.Core.Messages;
using EventStore.Projections.Core.Services.Processing;
using NUnit.Framework;

namespace EventStore.Projections.Core.Tests.Services.core_projection.checkpoint_manager.multi_stream
{
    [TestFixture]
    public class when_prerecording_event_order : TestFixtureWithMultiStreamCheckpointManager
    {
        private ResolvedEvent _event1;
        private ResolvedEvent _event2;

        protected override void Given()
        {
            base.Given();
            _streams = new[] {"pa", "pb"};
            ExistingEvent("a", "test1", "{}", "{}");
            ExistingEvent("b", "test1", "{}", "{}");

            ExistingEvent("pa", "$>", "1@a", "{$o:\"oa\"}");
            ExistingEvent("pb", "$>", "1@b", "{$o:\"ob\"}");

            _event1 = new ResolvedEvent("pa", 1, "a", 1, true, new EventPosition(200, 150), Guid.NewGuid(), "test1", true, "{}", "{}", "{$o:\"oa\"");
            _event2 = new ResolvedEvent("pb", 1, "b", 1, true, new EventPosition(300, 250), Guid.NewGuid(), "test1", true, "{}", "{}", "{$o:\"ob\"");

            NoStream("$projections-projection-order");


            AllWritesSucceed();
        }

        protected override void When()
        {
            base.When();
            Action noop = () => { };
            _manager.Initialize();
            _manager.BeginLoadState();
            _manager.Start(CheckpointTag.FromStreamPositions(new Dictionary<string, int> { { "pa", -1 }, { "pb", -1 } }));
            _manager.RecordEventOrder(_event1, CheckpointTag.FromStreamPositions(new Dictionary<string, int>{{"pa", 1},{"pb", -1}}), committed: noop);
            _manager.RecordEventOrder(_event2, CheckpointTag.FromStreamPositions(new Dictionary<string, int>{{"pa", 1},{"pb", 1}}), committed: noop);
        }

        [Test]
        public void writes_correct_link_tos()
        {
            var writeEvents = _consumer.HandledMessages.OfType<ClientMessage.WriteEvents>().SelectMany(v => v.Events).ToArray();
            Assert.AreEqual(2, writeEvents.Length);
            Assert.AreEqual("1@pa", Encoding.UTF8.GetString(writeEvents[0].Data));
            Assert.AreEqual("1@pb", Encoding.UTF8.GetString(writeEvents[1].Data));
        }

    }
}