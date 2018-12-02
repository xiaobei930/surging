﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using Surging.Core.Protocol.Mqtt.Internal.Channel;
using Surging.Core.Protocol.Mqtt.Internal.Enums;
using Surging.Core.Protocol.Mqtt.Internal.Messages;
using System.Collections;
using System.Linq;
using DotNetty.Codecs.Mqtt.Packets;
using System.Threading.Tasks;

namespace Surging.Core.Protocol.Mqtt.Internal.Services
{
    public abstract class AbstractChannelService : IChannelService
    {
        private readonly AttributeKey<string> _loginAttrKey = AttributeKey<string>.ValueOf("login");
        private readonly AttributeKey<string> _deviceIdAttrKey = AttributeKey<string>.ValueOf("deviceId");
        private readonly IMessagePushService _messagePushService;
        private readonly ConcurrentDictionary<string, IEnumerable<MqttChannel>> _topics = new ConcurrentDictionary<string, IEnumerable<MqttChannel>>();
        private readonly ConcurrentDictionary<string, MqttChannel> _mqttChannels = new ConcurrentDictionary<String, MqttChannel>();
        protected readonly  ConcurrentDictionary<String, ConcurrentQueue<RetainMessage>> _retain = new ConcurrentDictionary<String, ConcurrentQueue<RetainMessage>>();
        public AbstractChannelService(IMessagePushService messagePushService)
        {
            _messagePushService = messagePushService;
        }

        public ConcurrentDictionary<string, MqttChannel> MqttChannels { get {
                return _mqttChannels;
            }
        }

        public AttributeKey<string> DeviceIdAttrKey
        {
            get
            {
                return _deviceIdAttrKey;
            }
        }

        public AttributeKey<string> LoginAttrKey
        {
            get
            {
                return _loginAttrKey;
            }
        }

        public ConcurrentDictionary<string, IEnumerable<MqttChannel>> Topics
        {
            get
            {
                return _topics;
            }
        }

        public ConcurrentDictionary<String, ConcurrentQueue<RetainMessage>> Retain
        {
            get
            {
                return _retain;
            }
        }

        public abstract Task Close(string deviceId, bool isDisconnect);

        public abstract bool Connect(string deviceId, MqttChannel build);

        public bool RemoveChannel(string topic, MqttChannel mqttChannel)
        {
            var result = false;
            if (!string.IsNullOrEmpty(topic) && mqttChannel != null)
            {
                _topics.TryGetValue(topic, out IEnumerable<MqttChannel> mqttChannels);
                var channels = mqttChannels == null ? new List<MqttChannel>() : mqttChannels.ToList();
                channels.Remove(mqttChannel);
                _topics.AddOrUpdate(topic, channels, (key, value) => channels);
                result = true;
            }
            return result;
        }

        public async ValueTask<string> GetDeviceId(IChannel channel)
        {
            string deviceId = null;
            if (channel != null)
            {
                AttributeKey<string> deviceIdAttrKey = AttributeKey<string>.ValueOf("deviceId");
                deviceId = channel.GetAttribute<string>(deviceIdAttrKey).Get();
            }
            return await new ValueTask<string>(deviceId);
        }

        public bool AddChannel(string topic, MqttChannel mqttChannel)
        {
            var result = false;
            if (!string.IsNullOrEmpty(topic) && mqttChannel != null)
            {
                _topics.TryGetValue(topic, out IEnumerable<MqttChannel> mqttChannels);
                var channels = mqttChannels==null ? new List<MqttChannel>(): mqttChannels.ToList();
                channels.Add(mqttChannel);
                _topics.AddOrUpdate(topic, channels, (key, value) => channels);
                result = true; 
            }
            return result;
        }

        public MqttChannel GetMqttChannel(string deviceId)
        {
            MqttChannel channel = null;
            if (!string.IsNullOrEmpty(deviceId))
            {
                _mqttChannels.TryGetValue(deviceId, out channel);
            }
            return channel;
        }

        public abstract Task Login(IChannel channel, string deviceId, ConnectMessage mqttConnectMessage);

        public abstract Task Publish(IChannel channel, PublishPacket mqttPublishMessage);

        public abstract Task Pubrec(MqttChannel channel, int messageId);

        public abstract Task Pubrel(IChannel channel, int messageId);

        public abstract Task SendWillMsg(MqttWillMessage willMeaasge);
        public abstract Task Suscribe(string deviceId, params string[] topics);

        public abstract ValueTask UnSubscribe(string deviceId, params string[] topics);

        public abstract Task Publish(string deviceId, MqttWillMessage willMessage);
       
    }
}
