TO DO NEXT TIME
Use ConcurrentQueues like DTC-Client does
Implement protocol buffers, use them
Don't send login request every time
Don't do encoding request, SC handles it automatically?


Documentation
=============

-   DTCSharp requires .Net 4.5. It uses Async methods to communicate between client and server.

Client
=============
-   Responses from the server to the client are exposed as events, and the SendRequest() method is also exposed, to allow full low-level access.
-   But most request/response activities can be more easily accomplished using async methods that issue the request and return the response(s).

Server
=============
-   Requests from the client to the server are exposed as callbacks from each ClientHandler (which handles each client connected to the server), and the SendResponse() method is also exposed, to allow full low-level access.
-   A Service (e.g. see ExampleService) is passed through the server to each ClientHandler.
-   Events on ExampleService show a way for other server components to hook into client requests.
-   But most request/response activities can be more easily accomplished using async methods that handle the request and return the response(s).

Warnings
=============
-   If you request historical data using HistoricalDataIntervalEnum.IntervalTick you will only receive ticks if the Sierra Chart server Data/Trade Services Settings
        have Intraday Data Storage Time Unit set to 1 Tick
- 	Request callback and event handlers must not block the thread for long; further requests can't be received until you return from this method.

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