Documentation
=============

-   DTCSharp requires .Net 4.5. It uses Async methods to communicate between client and server.

Client
=============
-   Responses from the server to the client are exposed as events, and the SendRequest() method is also exposed, to allow full low-level access.
-   But most request/response activities can be more easily accomplished using async methods that issue the request and return the response.

Server
=============
-   Requests from the client to the server are exposed as events, and the SendResponse() method is also exposed, to allow full low-level access.
-   But most request/response activities can be more easily accomplished using async methods that handle the request and return the response.