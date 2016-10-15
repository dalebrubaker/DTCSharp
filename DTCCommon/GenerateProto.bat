echo on
..\packages\Google.Protobuf.Tools.3.1.0\tools\windows_x64\protoc.exe -I.\protos  --csharp_out=.\Generated %1
echo off


