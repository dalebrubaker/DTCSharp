TO DO NEXT TIME
Use ConcurrentQueues like DTC-Client does
Implement protocol buffers, use them
Don't send login request every time
Don't do encoding request, SC handles it automatically?


Documentation
=============

-   DTCSharp targets .Net 4.6.1.

Client
=============
-   Responses from the server to the client are exposed as events, and the SendRequest() method is also exposed, to allow full low-level access.
    You can get EVERY server response by subscribing to the EveryMessageFromServer event.
-   But most request/response activities can be more easily accomplished using async methods that issue the request and return the response(s).

Server
=============
-   Requests from the client to the server are exposed as callbacks from each ClientHandler (which handles a client connected to the server), and the SendResponse() method is also exposed, to allow full low-level access.
-   A Service (e.g. see ExampleService) derives from ServerBase to support the callbacks.
-   Events on ExampleService show a way for other server components to hook into client requests.
-   You can get EVERY server response by subscribing to the EveryMessageFromClient event.
-   But most request/response activities can be more easily accomplished using async methods that handle the request and return the response(s).

Historical Data
================
-   If you request historical data using HistoricalDataIntervalEnum.IntervalTick you will only receive ticks if the Sierra Chart server Data/Trade Services Settings
        have Intraday Data Storage Time Unit set to 1 Tick
    Zipped transmission can be 5 to 10 times faster, even when both ends are on the same machine. The ProtocolBuffers encoding is especially fast. 
    Using the same client or server for mixed use, like zipped historical and all other transmissions, is supported but discouraged. During zipped transmission there are no messages received from the server, including heartbeats.

Future
=============
For a simple JSON implementation, consider conversion to/from Protobuf classes
        See https://medium.com/google-cloud/making-newtonsoft-json-and-protocol-buffers-play-nicely-together-fe92079cc91c

Encoding
=============

See docs at https://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#EncodingRequest

The INITIAL encoding request and response are in binary encoding.
A later encoding request could be in the current protocol. In that case, the encoding response
        would come back in the new protocol. Wait for all messages to be received in the old protocol,
        then switch to reading & writing the new protocol.

C++ Definitions
=============
See https://dtcprotocol.org/DTC_Files/DTCProtocol.h for Text String lengths etc.

Protocol Buffers
================
DTCSharp is very protobuf-centric, casting events with protobuf-generated classes and converting 
        other encodings to and from protobufs for internal use.
The DTCCommon.csproj files includes XML that uses Grpc.Tools to generate the protobuf classes,
        which are compile to the .\obj folder whenever the DTCProtocol.proto file changes.
Some protobuf classes are extended by partial classes in the Partials directory.



