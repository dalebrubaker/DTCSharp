﻿using System.IO;
using DTCPB;
using Google.Protobuf;

namespace DTCCommon.Codecs
{
    public interface ICodecDTC
    {
        /// <summary>
        /// Write the message using binaryWriter
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        /// <param name="binaryWriter"></param>
        void Write<T>(DTCMessageType messageType, T message, BinaryWriter binaryWriter) where T : IMessage;

        /// <summary>
        /// Write the message using binaryWriter.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="binaryWriter"></param>
        void Write<T>(T message, BinaryWriter binaryWriter) where T : IMessage;


        /// <summary>
        /// Load the message represented by bytes into the IMessage
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageType"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        T Load<T>(DTCMessageType messageType, byte[] bytes) where T : IMessage<T>, new();

        /// <summary>
        /// Load the message represented by bytes into the IMessage
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <returns></returns>
        T Load<T>(byte[] bytes) where T : IMessage<T>, new();
    }
}