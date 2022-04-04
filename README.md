# DTCSharp
Client and Server using DTC protocol

Client
=============
-   Responses from the server to the client are exposed as events, and the SendRequest() method is also exposed, to allow full low-level access.
    You can get EVERY server response by subscribing to the EveryMessageFromServer event.
-   Many request/response activities can be more easily accomplished using async methods that issue the request and return the response(s).

Server
=============
-   Requests from the client to the server are exposed as callbacks from each ClientHandler (which handles a client connected to the server), and the SendResponse() method is also exposed, to allow full low-level access.
-   A Service (e.g. see ExampleService) derives from ServerBase to support the callbacks.
-   Events on ExampleService demonstrate how other server components can handle client requests.
-   You can get EVERY server response by subscribing to the EveryMessageFromClient event.

Historical Data
================
-   If you request historical data using HistoricalDataIntervalEnum.IntervalTick you will only receive ticks if the Sierra Chart server Data/Trade Services Settings
    have Intraday Data Storage Time Unit set to 1 Tick. Even then some records might be 1 Minute if ticks are not available that far back.
    Zipped transmission is supported for Binary encoding might be faster, but Protocol Buffers (not zipped) can be even faster, even when both ends are on the same machine.
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
would come back in the new protocol.

C++ Definitions
=============
See https://dtcprotocol.org/DTC_Files/DTCProtocol.h for Text String lengths etc.

Protocol Buffers
================
DTCSharp is very protobuf-centric, casting events with protobuf-generated classes and converting
other encodings to and from protobufs for internal use.
The DTCCommon.csproj files includes XML that uses Grpc.Tools to generate the protobuf classes,
which are compiled to the .\obj folder whenever the DTCProtocol.proto file changes.
Some protobuf classes are extended by partial classes in the Partials directory.



