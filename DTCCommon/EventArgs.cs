using System;

namespace DTCCommon
{
    public sealed class EventArgs<T> : EventArgs
    {
        public EventArgs(T data)
        {
            Data = data;
        }

        public T Data { get; }
    }

    public class EventArgs<T1, T2> : EventArgs
    {

        public EventArgs(T1 data1, T2 data2)
        {
            Data1 = data1;
            Data2 = data2;
        }

        public T1 Data1 { get; }
        public T2 Data2 { get; }
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

    public class EventArgs<T1, T2, T3, T4> : EventArgs
    {

        public EventArgs(T1 data1, T2 data2, T3 data3, T4 data4)
        {
            Data1 = data1;
            Data2 = data2;
            Data3 = data3;
            Data4 = data4;
        }

        public T1 Data1 { get; }
        public T2 Data2 { get; }
        public T3 Data3 { get; }
        public T4 Data4 { get; }
    }


    public class EventArgs<T1, T2, T3, T4, T5> : EventArgs
    {

        public EventArgs(T1 data1, T2 data2, T3 data3, T4 data4, T5 data5)
        {
            Data1 = data1;
            Data2 = data2;
            Data3 = data3;
            Data4 = data4;
            Data5 = data5;
        }

        public T1 Data1 { get; }
        public T2 Data2 { get; }
        public T3 Data3 { get; }
        public T4 Data4 { get; }
        public T5 Data5 { get; }
    }

    public class EventArgs<T1, T2, T3, T4, T5, T6> : EventArgs
    {

        public EventArgs(T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6)
        {
            Data1 = data1;
            Data2 = data2;
            Data3 = data3;
            Data4 = data4;
            Data5 = data5;
            Data6 = data6;
        }

        public T1 Data1 { get; }
        public T2 Data2 { get; }
        public T3 Data3 { get; }
        public T4 Data4 { get; }
        public T5 Data5 { get; }
        public T6 Data6 { get; }
    }

    public class EventArgs<T1, T2, T3, T4, T5, T6, T7> : EventArgs
    {

        public EventArgs(T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6, T7 data7)
        {
            Data1 = data1;
            Data2 = data2;
            Data3 = data3;
            Data4 = data4;
            Data5 = data5;
            Data6 = data6;
            Data7 = data7;
        }

        public T1 Data1 { get; }
        public T2 Data2 { get; }
        public T3 Data3 { get; }
        public T4 Data4 { get; }
        public T5 Data5 { get; }
        public T6 Data6 { get; }
        public T7 Data7 { get; }
    }

}
