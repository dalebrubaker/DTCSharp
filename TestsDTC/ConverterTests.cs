﻿// unset
using System;
using System.Linq;
using DTCCommon;
using DTCCommon.Codecs;
using DTCPB;
using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Xunit;

namespace TestsDTC
{
    public class ConverterTests
    {
        private readonly CodecProtobufConverter _protobufConverter;
        private readonly CodecBinaryConverter _binaryConverter;

        public ConverterTests()
        {
            _protobufConverter = new CodecProtobufConverter();
            _binaryConverter = new CodecBinaryConverter();
        }

        [Fact]
        public void ProtoToProtoRoundTrip()
        {
            var messageTypes = Enum.GetValues(typeof(DTCMessageType));
            foreach (DTCMessageType messageType in messageTypes)
            {
                if (messageType == DTCMessageType.MessageTypeUnset)
                {
                    continue;
                }
                var protobufTest = CreateTestProtobuf(messageType);
                var bytes = _protobufConverter.ConvertToBuffer(messageType, protobufTest);
                var protobufDuplicate = _protobufConverter.ConvertToProtobuf(messageType, bytes);
                protobufDuplicate.Should().Be(protobufTest, "Conversion should be exact");
            }
        }

        [Fact]
        public void ProtoToBinaryRoundTrip()
        {
            var messageTypes = Enum.GetValues(typeof(DTCMessageType));
            foreach (DTCMessageType messageType in messageTypes)
            {
                if (messageType == DTCMessageType.MessageTypeUnset)
                {
                    continue;
                }
                try
                {
                    var protobufTest = CreateTestProtobuf(messageType);
                    var bytes = _binaryConverter.ConvertToBuffer(messageType, protobufTest);
                    var protobufDuplicate = _binaryConverter.ConvertToProtobuf(messageType, bytes);
                    protobufDuplicate.Should().Be(protobufTest, $"Conversion of {messageType} must be exact");
                }
                catch (NotImplementedException)
                {
                    // We only test what has been implemented
                }
                catch (NotSupportedException)
                {
                    // We only test what is supported
                }
                catch (Exception ex)
                {
                    var tmp = ex.Message;
                    throw;
                }
            }
        }

        private IMessage CreateTestProtobuf(DTCMessageType messageType)
        {
            if (messageType == DTCMessageType.MessageTypeUnset)
            {
                return null;
            }
            var result = EmptyProtobufs.GetEmptyProtobuf(messageType);
            var descriptor = result.Descriptor;
            int count = 1;
            var properties = result.GetType().GetProperties();
            var fields = descriptor.Fields.InFieldNumberOrder();
            foreach (var field in fields)
            {
                var property = properties.FirstOrDefault(x => x.Name.Replace("_", "''") == field.JsonName);
                if (property == null)
                {
                    property = properties.FirstOrDefault(x => x.Name == field.JsonName + "_");
                    if (property == null)
                    {
                        throw new NotSupportedException("Why?");
                    }
                }
                var value = property.GetValue(result);
                switch (field.FieldType)
                {
                    case FieldType.Double:
                        value = ((double)value + count);
                        break;
                    case FieldType.Float:
                        value = ((float)value + count);
                        break;
                    case FieldType.Int64:
                        value = ((long)value + count);
                        break;
                    case FieldType.UInt64:
                        value = (ulong)value + (ulong)count;
                        break;
                    case FieldType.Int32:
                        value = ((int)value + count);
                        break;
                    case FieldType.Bool:
                        value = !(bool)value;
                        break;
                    case FieldType.String:
                        value = ((string)value + count);
                        break;
                    case FieldType.UInt32:
                        value = (uint)value + (uint)count;
                        break;
                    case FieldType.SFixed32:
                        value = ((int)value + count);
                        break;
                    case FieldType.SFixed64:
                        value = ((long)value + count);
                        break;
                    case FieldType.Enum:
                        // Set it to the 2nd enum value
                        value = ((int)value + 1);
                        break;
                    case FieldType.Fixed64:
                    case FieldType.Fixed32:
                    case FieldType.Group:
                    case FieldType.Message:
                    case FieldType.Bytes:
                    case FieldType.SInt32:
                    case FieldType.SInt64:
                        throw new NotSupportedException("Must be a new proto file.");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                property.SetValue(result, value);
                count++;
            }
            return result;
        }
    }
}