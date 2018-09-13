using System;
using System.IO;
using System.Net.Sockets;

namespace localshare
{
    class Utils
    {
       
        public static byte[] ReceiveExactly(Socket socket, int n)
        {
            byte[] buffer = new Byte[n];
            int receivedBytes = 0;
            int receivedNow;

            while (receivedBytes < n)
            {
                receivedNow = socket.Receive(buffer, receivedBytes, n - receivedBytes, 0);
                receivedBytes += receivedNow;

                if (receivedNow == 0)
                    return null;
            }

            return buffer;
        }

        public static void SendExactly(Socket socket, byte[] buffer)
        {
            int n = buffer.Length;
            int sentBytes = 0;
            int sentNow;

            while (sentBytes < n)
            {
                sentNow = socket.Send(buffer, sentBytes, n - sentBytes, 0);
                sentBytes += sentNow;
            }
        }

        public static void SendExactly(Socket socket, byte[] buffer, int bufferLen)
        {
            int n = bufferLen;
            int sentBytes = 0;
            int sentNow;

            while (sentBytes < n)
            {
                sentNow = socket.Send(buffer, sentBytes, n - sentBytes, 0);
                sentBytes += sentNow;
            }
        }

    }
}
