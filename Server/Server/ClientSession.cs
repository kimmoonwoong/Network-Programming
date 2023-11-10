﻿using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public abstract class Packet
    {
        public ushort size;
        public ushort packetId;
        public abstract ArraySegment<byte> Write();
        public abstract void Read(ArraySegment<byte> array);
    }

    class PlayerinfoReq : Packet
    {
        public long playerId;
        public string name;

        public PlayerinfoReq()
        {
            this.packetId = (ushort)PacketID.PlayerinfoReq;
        }
        /*
         * 모든 Packet에서 쓰는 것, 읽는 것 모두 동일하게 작업하므로 클래스의 메서드화
         */
        public override ArraySegment<byte> Write()
        {
            ArraySegment<byte> segment = SendBufferHelper.Open(4096);

            ushort count = 0;
            bool success = true;

            Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

            count += (ushort)Marshal.SizeOf(count);
            /*
             * 여기서 Array.Copy가 아닌 TryWriteBytes를 쓰는 이유
             * Array.Copy를 사용하면 new를 통해 새로운 객체를 만들어서 복사하는 작업을 함
             * 하지만 TryWriteByte는 span 클래스, 즉 어떤 array에 직접 쓰는 것으로 작업을 한다.
             * 그렇기에 TryWriteByte가 더 효율적이다.
             */
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.packetId);
            count += (ushort)Marshal.SizeOf(this.packetId);

            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerId);
            count += (ushort)Marshal.SizeOf(this.playerId);


            /*
             * 패킷을 통해 string 문자열을 보낼 때 먼저 보낼 string의 길이를 넣어줌으로써 어디까지 읽어야하는지 알려준다.
             */
            ushort nameLen = (ushort)Encoding.Unicode.GetBytes(this.name, 0, this.name.Length, segment.Array, segment.Offset + count + sizeof(ushort));
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), nameLen);
            count += nameLen;
            count += sizeof(ushort);
            success &= BitConverter.TryWriteBytes(s, count);



            if (!success) return null;

            return SendBufferHelper.Close(count);

        }
        public override void Read(ArraySegment<byte> buffer)
        {
            ushort count = 0;

            ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(buffer.Array, buffer.Offset, buffer.Count); ;

            count += sizeof(ushort);
            count += sizeof(ushort);


            this.playerId = BitConverter.ToInt64(s.Slice(count, s.Length - count));
            count += sizeof(long);

            ushort nameLen = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
            count += sizeof(ushort);
            this.name = Encoding.Unicode.GetString(s.Slice(count, nameLen));
        }
    }


    public enum PacketID
    {
        PlayerinfoReq = 1,
        PlayerinfoOk = 2,
    }

    class ClientSession : PacketSession
    {
        static int cnt = 0;
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}, Count : {cnt}");


            //Packet packet = new Packet() { size = 100, packetId = 10 };


            /*ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
            byte[] buffer = BitConverter.GetBytes(packet.size);
            byte[] buffer2 = BitConverter.GetBytes(packet.packetId);

            Array.Copy(buffer, 0, openSegment.Array, openSegment.Offset, buffer.Length);
            Array.Copy(buffer2, 0, openSegment.Array, openSegment.Offset + buffer.Length, buffer2.Length);

            ArraySegment<byte> sendbuff = SendBufferHelper.Close(buffer.Length + buffer2.Length);


            Send(sendbuff);
*/
            Thread.Sleep(5000);

            DisConnect();
            cnt++;

        }
        public override void OnRecvPacket(ArraySegment<byte> buffer)
        {
            ushort count = 0;

            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            count += 2;
            ushort packetId = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
            count += 2;

            switch ((PacketID)packetId)
            {
                case PacketID.PlayerinfoReq:

                    PlayerinfoReq p = new PlayerinfoReq();
                    p.Read(buffer);
                    Console.WriteLine($"PlayerId : {p.playerId}, PlayerName : {p.name}");
                    break;
            }
            Console.WriteLine($"PacketSize : {size}, PacketId : {packetId}");

        }


        public override void OnDisConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisConnected : {endPoint}");
        }

        public override void OnSend(int numOfBuffer)
        {
            Console.WriteLine($"Transferred bytes: {numOfBuffer}");
        }
    }
}
