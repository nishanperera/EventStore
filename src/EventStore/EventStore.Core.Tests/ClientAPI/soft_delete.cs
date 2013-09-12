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
using System.Linq;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using EventStore.Core.Tests.ClientAPI.Helpers;
using EventStore.Core.Tests.Helpers;
using NUnit.Framework;

namespace EventStore.Core.Tests.ClientAPI
{
    [TestFixture, Category("LongRunning")]
    public class soft_delete : SpecificationWithDirectoryPerTestFixture
    {
        private MiniNode _node;
        private IEventStoreConnection _conn;

        [TestFixtureSetUp]
        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();
            _node = new MiniNode(PathName);
            _node.Start();

            _conn = EventStoreConnection.Create(_node.TcpEndPoint);
            _conn.Connect();
        }

        [TestFixtureTearDown]
        public override void TestFixtureTearDown()
        {
            _conn.Close();
            _node.Shutdown();
            base.TestFixtureTearDown();
        }

        [Test, Category("LongRunning"), Category("Network")]
        public void soft_deleted_stream_returns_no_stream_and_no_events_on_read()
        {
            const string stream = "soft_deleted_stream_returns_no_stream_and_no_events_on_read";

            Assert.AreEqual(1, _conn.AppendToStream(stream, ExpectedVersion.NoStream, TestEvent.NewTestEvent(), TestEvent.NewTestEvent()).NextExpectedVersion);

            Assert.AreEqual(0, _conn.SetStreamMetadata(stream, ExpectedVersion.NoStream, StreamMetadata.Build().SetTruncateBefore(int.MaxValue)).NextExpectedVersion);

            var res = _conn.ReadStreamEventsForward(stream, 0, 100, false);
            Assert.AreEqual(SliceReadStatus.StreamNotFound, res.Status);
            Assert.AreEqual(0, res.Events.Length);
            Assert.AreEqual(1, res.LastEventNumber);
        }

        [Test, Category("LongRunning"), Category("Network")]
        public void soft_deleted_stream_allows_recreation_when_expver_any()
        {
            const string stream = "soft_deleted_stream_allows_recreation_when_expver_any";

            Assert.AreEqual(1, _conn.AppendToStream(stream, ExpectedVersion.NoStream, TestEvent.NewTestEvent(), TestEvent.NewTestEvent()).NextExpectedVersion);

            Assert.AreEqual(0, _conn.SetStreamMetadata(stream, ExpectedVersion.NoStream, StreamMetadata.Build().SetTruncateBefore(int.MaxValue)).NextExpectedVersion);

            var events = new[] {TestEvent.NewTestEvent(), TestEvent.NewTestEvent(), TestEvent.NewTestEvent()};
            Assert.AreEqual(4, _conn.AppendToStream(stream, ExpectedVersion.Any, events).NextExpectedVersion);

            var res = _conn.ReadStreamEventsForward(stream, 0, 100, false);
            Assert.AreEqual(SliceReadStatus.Success, res.Status);
            Assert.AreEqual(4, res.LastEventNumber);
            Assert.AreEqual(3, res.Events.Length);
            Assert.AreEqual(events.Select(x => x.EventId), res.Events.Select(x => x.OriginalEvent.EventId));
            Assert.AreEqual(new[]{2, 3, 4}, res.Events.Select(x => x.OriginalEvent.EventNumber));

            var meta = _conn.GetStreamMetadata(stream);
            Assert.AreEqual(2, meta.StreamMetadata.TruncateBefore);
            Assert.AreEqual(1, meta.MetastreamVersion);
        }

        [Test, Category("LongRunning"), Category("Network")]
        public void soft_deleted_stream_allows_recreation_when_expver_no_stream()
        {
            const string stream = "soft_deleted_stream_allows_recreation_when_expver_no_stream";

            Assert.AreEqual(1, _conn.AppendToStream(stream, ExpectedVersion.NoStream, TestEvent.NewTestEvent(), TestEvent.NewTestEvent()).NextExpectedVersion);

            Assert.AreEqual(0, _conn.SetStreamMetadata(stream, ExpectedVersion.NoStream, StreamMetadata.Build().SetTruncateBefore(int.MaxValue)).NextExpectedVersion);

            var events = new[] { TestEvent.NewTestEvent(), TestEvent.NewTestEvent(), TestEvent.NewTestEvent() };
            Assert.AreEqual(4, _conn.AppendToStream(stream, ExpectedVersion.NoStream, events).NextExpectedVersion);

            var res = _conn.ReadStreamEventsForward(stream, 0, 100, false);
            Assert.AreEqual(SliceReadStatus.Success, res.Status);
            Assert.AreEqual(4, res.LastEventNumber);
            Assert.AreEqual(3, res.Events.Length);
            Assert.AreEqual(events.Select(x => x.EventId), res.Events.Select(x => x.OriginalEvent.EventId));
            Assert.AreEqual(new[] { 2, 3, 4 }, res.Events.Select(x => x.OriginalEvent.EventNumber));

            var meta = _conn.GetStreamMetadata(stream);
            Assert.AreEqual(2, meta.StreamMetadata.TruncateBefore);
            Assert.AreEqual(1, meta.MetastreamVersion);
        }

        [Test, Category("LongRunning"), Category("Network")]
        public void soft_deleted_stream_allows_recreation_when_expver_is_exact()
        {
            const string stream = "soft_deleted_stream_allows_recreation_when_expver_is_exact";

            Assert.AreEqual(1, _conn.AppendToStream(stream, ExpectedVersion.NoStream, TestEvent.NewTestEvent(), TestEvent.NewTestEvent()).NextExpectedVersion);

            Assert.AreEqual(0, _conn.SetStreamMetadata(stream, ExpectedVersion.NoStream, StreamMetadata.Build().SetTruncateBefore(int.MaxValue)).NextExpectedVersion);

            var events = new[] { TestEvent.NewTestEvent(), TestEvent.NewTestEvent(), TestEvent.NewTestEvent() };
            Assert.AreEqual(4, _conn.AppendToStream(stream, 1, events).NextExpectedVersion);

            var res = _conn.ReadStreamEventsForward(stream, 0, 100, false);
            Assert.AreEqual(SliceReadStatus.Success, res.Status);
            Assert.AreEqual(4, res.LastEventNumber);
            Assert.AreEqual(3, res.Events.Length);
            Assert.AreEqual(events.Select(x => x.EventId), res.Events.Select(x => x.OriginalEvent.EventId));
            Assert.AreEqual(new[] { 2, 3, 4 }, res.Events.Select(x => x.OriginalEvent.EventNumber));

            var meta = _conn.GetStreamMetadata(stream);
            Assert.AreEqual(2, meta.StreamMetadata.TruncateBefore);
            Assert.AreEqual(1, meta.MetastreamVersion);
        }

        [Test, Category("LongRunning"), Category("Network")]
        public void soft_deleted_stream_when_recreated_preserves_metadata_except_truncatebefore()
        {
            const string stream = "soft_deleted_stream_when_recreated_preserves_metadata_except_truncatebefore";

            Assert.AreEqual(1, _conn.AppendToStream(stream, ExpectedVersion.NoStream, TestEvent.NewTestEvent(), TestEvent.NewTestEvent()).NextExpectedVersion);

            Assert.AreEqual(0, _conn.SetStreamMetadata(stream, ExpectedVersion.NoStream,
                                    StreamMetadata.Build().SetTruncateBefore(int.MaxValue)
                                                          .SetMaxCount(100)
                                                          .SetDeleteRole("some-role")
                                                          .SetCustomProperty("key1", true)
                                                          .SetCustomProperty("key2", 17)
                                                          .SetCustomProperty("key3", "some value")).NextExpectedVersion);

            var events = new[] { TestEvent.NewTestEvent(), TestEvent.NewTestEvent(), TestEvent.NewTestEvent() };
            Assert.AreEqual(4, _conn.AppendToStream(stream, 1, events).NextExpectedVersion);

            var res = _conn.ReadStreamEventsForward(stream, 0, 100, false);
            Assert.AreEqual(SliceReadStatus.Success, res.Status);
            Assert.AreEqual(4, res.LastEventNumber);
            Assert.AreEqual(3, res.Events.Length);
            Assert.AreEqual(events.Select(x => x.EventId), res.Events.Select(x => x.OriginalEvent.EventId));
            Assert.AreEqual(new[] { 2, 3, 4 }, res.Events.Select(x => x.OriginalEvent.EventNumber));

            var meta = _conn.GetStreamMetadata(stream);
            Assert.AreEqual(1, meta.MetastreamVersion);
            Assert.AreEqual(2, meta.StreamMetadata.TruncateBefore);
            Assert.AreEqual(100, meta.StreamMetadata.MaxCount);
            Assert.AreEqual("some-role", meta.StreamMetadata.Acl.DeleteRole);
            Assert.AreEqual(true, meta.StreamMetadata.GetValue<bool>("key1"));
            Assert.AreEqual(17, meta.StreamMetadata.GetValue<int>("key2"));
            Assert.AreEqual("some value", meta.StreamMetadata.GetValue<string>("key3"));
        }

        [Test, Category("LongRunning"), Category("Network")]
        public void soft_deleted_stream_can_be_deleted()
        {
            const string stream = "soft_deleted_stream_can_be_deleted";

            Assert.AreEqual(1, _conn.AppendToStream(stream, ExpectedVersion.NoStream, TestEvent.NewTestEvent(), TestEvent.NewTestEvent()).NextExpectedVersion);
            Assert.AreEqual(0, _conn.SetStreamMetadata(stream, ExpectedVersion.NoStream, StreamMetadata.Build().SetTruncateBefore(int.MaxValue)).NextExpectedVersion);

            _conn.DeleteStream(stream, ExpectedVersion.Any);

            var res = _conn.ReadStreamEventsForward(stream, 0, 100, false);
            Assert.AreEqual(SliceReadStatus.StreamDeleted, res.Status);
            var meta = _conn.GetStreamMetadata(stream);
            Assert.AreEqual(true, meta.IsStreamDeleted);

            Assert.That(() => _conn.AppendToStream(stream, ExpectedVersion.Any, TestEvent.NewTestEvent()),
                        Throws.Exception.InstanceOf<AggregateException>()
                            .With.InnerException.InstanceOf<StreamDeletedException>());
        }

        [Test, Category("LongRunning"), Category("Network")]
        public void soft_deleted_stream_allows_recreation_only_for_first_write()
        {
            const string stream = "soft_deleted_stream_allows_recreation_only_for_first_write";

            Assert.AreEqual(1, _conn.AppendToStream(stream, ExpectedVersion.NoStream, TestEvent.NewTestEvent(), TestEvent.NewTestEvent()).NextExpectedVersion);
            Assert.AreEqual(0, _conn.SetStreamMetadata(stream, ExpectedVersion.NoStream, StreamMetadata.Build().SetTruncateBefore(int.MaxValue)).NextExpectedVersion);

            var events = new[] { TestEvent.NewTestEvent(), TestEvent.NewTestEvent(), TestEvent.NewTestEvent() };
            Assert.AreEqual(4, _conn.AppendToStream(stream, ExpectedVersion.NoStream, events).NextExpectedVersion);
            Assert.That(() => _conn.AppendToStream(stream, ExpectedVersion.NoStream, TestEvent.NewTestEvent()),
                        Throws.Exception.InstanceOf<AggregateException>()
                            .With.InnerException.InstanceOf<WrongExpectedVersionException>());

            var res = _conn.ReadStreamEventsForward(stream, 0, 100, false);
            Assert.AreEqual(SliceReadStatus.Success, res.Status);
            Assert.AreEqual(4, res.LastEventNumber);
            Assert.AreEqual(3, res.Events.Length);
            Assert.AreEqual(events.Select(x => x.EventId), res.Events.Select(x => x.OriginalEvent.EventId));
            Assert.AreEqual(new[] { 2, 3, 4 }, res.Events.Select(x => x.OriginalEvent.EventNumber));

            var meta = _conn.GetStreamMetadata(stream);
            Assert.AreEqual(2, meta.StreamMetadata.TruncateBefore);
            Assert.AreEqual(1, meta.MetastreamVersion);
        }

        [Test, Category("LongRunning"), Category("Network")]
        public void soft_deleted_stream_appends_both_writes_when_expver_any()
        {
            const string stream = "soft_deleted_stream_appends_both_concurrent_writes_when_expver_any";

            Assert.AreEqual(1, _conn.AppendToStream(stream, ExpectedVersion.NoStream, TestEvent.NewTestEvent(), TestEvent.NewTestEvent()).NextExpectedVersion);
            Assert.AreEqual(0, _conn.SetStreamMetadata(stream, ExpectedVersion.NoStream, StreamMetadata.Build().SetTruncateBefore(int.MaxValue)).NextExpectedVersion);

            var events1 = new[] { TestEvent.NewTestEvent(), TestEvent.NewTestEvent(), TestEvent.NewTestEvent() };
            var events2 = new[] { TestEvent.NewTestEvent(), TestEvent.NewTestEvent() };
            Assert.AreEqual(4, _conn.AppendToStream(stream, ExpectedVersion.Any, events1).NextExpectedVersion);
            Assert.AreEqual(6, _conn.AppendToStream(stream, ExpectedVersion.Any, events2).NextExpectedVersion);

            var res = _conn.ReadStreamEventsForward(stream, 0, 100, false);
            Assert.AreEqual(SliceReadStatus.Success, res.Status);
            Assert.AreEqual(6, res.LastEventNumber);
            Assert.AreEqual(5, res.Events.Length);
            Assert.AreEqual(events1.Concat(events2).Select(x => x.EventId), res.Events.Select(x => x.OriginalEvent.EventId));
            Assert.AreEqual(new[] { 2, 3, 4, 5, 6 }, res.Events.Select(x => x.OriginalEvent.EventNumber));

            var meta = _conn.GetStreamMetadata(stream);
            Assert.AreEqual(2, meta.StreamMetadata.TruncateBefore);
            Assert.AreEqual(1, meta.MetastreamVersion);
        }
    }
}