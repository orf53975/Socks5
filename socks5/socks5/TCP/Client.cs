﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace socks5.TCP
{
    public class Client
    {
        public event EventHandler<ClientEventArgs> onClientDisconnected;

        public event EventHandler<DataEventArgs> onDataReceived = delegate { };
        public event EventHandler<DataEventArgs> onDataSent = delegate { };
        

        public Socket Sock { get; set; }
        private byte[] buffer = new byte[2048];
        public Client(Socket sock)
        {
            //start the data exchange.
            Sock = sock;
            onClientDisconnected = delegate { };
        }

        private void DataReceived(IAsyncResult res)
        {
            try
            {
                int received = ((Socket)res.AsyncState).EndReceive(res);
                if (received <= 0)
                {
                    //disconnected.
                    onClientDisconnected(this, new ClientEventArgs(this));
                    this.Disconnect();
                    return;
                }
                DataEventArgs data = new DataEventArgs(this, buffer, received);
                this.onDataReceived(this, data);
            }
            catch
            {
                this.Disconnect();
            }
        }

        public int Receive(byte[] data, int offset, int count)
        {
            try
            {
                int received = this.Sock.Receive(data, offset, count, SocketFlags.None);
                if (received <= 0)
                {
                    this.Disconnect();
                    return -1;
                }
                return received;
            }
            catch
            {
                this.Disconnect();
                return -1;
            }
        }

        public void ReceiveAsync()
        {
            Sock.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(DataReceived), Sock);
        }


        public void Disconnect()
        {
            try
            {
                if (this.Sock != null)
                {
                    onClientDisconnected(this, new ClientEventArgs(this));
                    this.Sock.Shutdown(SocketShutdown.Both);
                    this.Sock.Close();
                    return;
                }
            }
            catch { }
        }

        private void DataSent(IAsyncResult res)
        {
            try
            {
                int sent = ((Socket)res.AsyncState).EndSend(res);
                if (sent < 0)
                {
                    this.Sock.Shutdown(SocketShutdown.Both);
                    this.Sock.Close();
                    return;
                }
            }
            catch { this.Disconnect(); }
        }

        public bool Send(byte[] buff)
        {
            return Send(buff, 0, buff.Length);
        }

        public void SendAsync(byte[] buff, int offset, int count)
        {
            try
            {
                if (this.Sock != null)
                {
                    DataEventArgs data = new DataEventArgs(this, buff, count);
                    this.onDataSent(this, data);
                    this.Sock.BeginSend(data.Buffer, data.Offset, data.Count, SocketFlags.None, new AsyncCallback(DataSent), this.Sock);
                }
            }
            catch
            {
                this.Disconnect();
            }
        }

        public bool Send(byte[] buff, int offset, int count)
        {
            if(this.Sock != null)
            {
                if(this.Sock.Send(buff, offset, count, SocketFlags.None) <= 0)
                {
                    this.Disconnect();
                    return false;
                }
                return true;
            }
            return false;
        }
    }
}