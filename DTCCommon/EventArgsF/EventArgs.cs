using System;

namespace DTCCommon.EventArgsF
{
    public sealed class EventArgs<T> : EventArgs
    {
        public EventArgs(T data)
        {
            Data = data;
        }

        public T Data { get; }
    }



    public class EventArgs<T1, T2, T3> : EventArgs
    {
        public EventArgs(T1 data1, T2 data2, T3 data3)
        {
            Data1 = data1;
            Data2 = data2;
            Data3 = data3;
        }

        public T1 Data1 { get; }
        public T2 Data2 { get; }
        public T3 Data3 { get; }
    }





}