// unset
using System;
using System.Linq;
using DTCCommon;
using DTCCommon.Codecs;
using DTCPB;
using FluentAssertions;
using Google.Protobuf;
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
            var properties = result.GetType().GetProperties();
            int count = 1;
            var skipPropertyNames = new[] {"Parser", "Descriptor", "RuntimePropertyInfo"};
            foreach (var property in properties)
            {
                var propertyName = property.Name;
                if (skipPropertyNames.Contains(propertyName))
                {
                    continue;
                }
                var value = property.GetValue(result);
                var typeName = property.PropertyType.Name;
                var baseType = property.PropertyType.BaseType;
                if (baseType == typeof(Enum))
                {
                    // Set it to the 2nd enum value
                    value = ((int)value + 1);
                    property.SetValue(result, value);
                    continue;
                }
                switch (typeName)
                {
                    case "Boolean":
                        var boolValue = (bool)value;
                        property.SetValue(result, !boolValue);
                        break;
                    case "Int32":
                        value = ((int)value + count);
                        property.SetValue(result, value);
                        break;
                    case "Int64":
                        value = ((long)value + count);
                        property.SetValue(result, value);
                        break;
                    case "DateTime":
                        var dateTime = (DateTime)value;
                        dateTime = dateTime.AddDays(count).AddHours(count).AddMilliseconds(1);
                        property.SetValue(result, dateTime);
                        break;
                    case "Double":
                        value = ((double)value + count);
                        property.SetValue(result, value);
                        break;
                    case "Single":
                        value = ((float)value + count);
                        property.SetValue(result, value);
                        break;
                    case "String":
                        value = ((string)value + count);
                        property.SetValue(result, value);
                        break;
                    case "UInt32":
                        value = (uint)value + (uint)count;
                        property.SetValue(result, value);
                        break;
                    case "UInt64":
                        value = (ulong)value + (ulong)count;
                        property.SetValue(result, value);
                        break;
                    default:
                        throw new NotImplementedException();
                }
                count++;
            }

            return result;
        }
    }
}