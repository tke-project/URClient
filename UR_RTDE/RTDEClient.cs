using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace UR_RTDE
{
    public class RTDEClient
    {
        private string _ip;
        private int _port;
        private int _freq;
        private Task _task;

        private string _inputKeyString = string.Empty;
        private string _outputKeyString = string.Empty;

        private byte _inputRecipe;
        private byte _outputRecipe;

        private new Dictionary<string, string> _inputKeyTypes = new();
        private new Dictionary<string, string> _outputKeyTypes = new();

        public Func<object[]>? OnSendData { get; set; }
        public Action<object[]>? OnReceiveData { get; set; }

        public RTDEClient(string ip, int port, int freq)
        {
            _ip = ip;
            _port = port;
            _freq = freq;
        }


        public async Task StartExchangingDataAsync()
        {
            while (true)
            {
                Socket? client = null;

                try
                {
                    var ipHostInfo = await Dns.GetHostAddressesAsync(_ip);
                    var iPEndPoint = new IPEndPoint(ipHostInfo[0], _port);
                    client = new Socket(iPEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    await client.ConnectAsync(iPEndPoint);

                    try
                    {
                        //プロトコルバージョン確認
                        if (await RequestProtocolVersionAsync(client))
                        {
                            //出力データ登録
                            if (_outputKeyString != string.Empty)
                                _outputRecipe = await SetupOutputsAsync(client, _freq);

                            //入力データ登録
                            if (_inputKeyString != string.Empty)
                                _inputRecipe = await SetupInputs(client);

                            //RTDEデータ通信スタート
                            if (!await StartReceivingAsync(client))
                            {
                                throw new Exception("RTDE Starting is Failed");
                            }

                            while (true)
                            {
                                await Task.Delay(1);

                                if (_outputKeyString != string.Empty)
                                    await ReceiveDataAsync(client);

                                if (_inputKeyString != string.Empty)
                                    await SendDataAsync(client);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                    }

                }
                catch (Exception ex)
                {
                    client?.Shutdown(SocketShutdown.Both);
                    client?.Dispose();

                }
                finally
                {

                }
            }
        }

        public void AddInput(RTDEInput input)
        {
            var sb = new StringBuilder(_inputKeyString);

            if (_inputKeyString == string.Empty)
                sb.Append(input.ToString());
            else
            {
                sb.Append(",");
                sb.Append(input.ToString());

            }

            _inputKeyString = sb.ToString();

        }

        public void AddOutput(RTDEOutput ouput)
        {
            var sb = new StringBuilder(_outputKeyString);

            if (_outputKeyString == string.Empty)
                sb.Append(ouput.ToString());
            else
            {
                sb.Append(",");
                sb.Append(ouput.ToString());

            }

            _outputKeyString = sb.ToString();
        }

        private async Task<byte> SetupOutputsAsync(Socket client, int freq)
        {
            var command = RTDECommand.RTDE_CONTROL_PACKAGE_SETUP_OUTPUTS;

            byte[] hd_freq = BitConverter.GetBytes((double)freq).Reverse().ToArray();
            byte[] hd_key = Encoding.UTF8.GetBytes(_outputKeyString);
            byte[] payload = hd_freq.Concat(hd_key).ToArray();

            await SendCommandAsync(client, command, payload);

            byte[] buf = await ReceiveBytesAsync(client);

            if (buf == null)
                throw new IOException("Setup output error");

            byte[] typesBuf = new byte[buf.Length - 4];
            Array.Copy(buf, 4, typesBuf, 0, typesBuf.Length);

            string[] outkeyArray = _outputKeyString.Split(",");
            string[] types = Encoding.UTF8.GetString(typesBuf).Split(",");

            _outputKeyTypes.Clear();
            for (int i = 0; i < outkeyArray.Length; i++)
            {
                _outputKeyTypes.Add(outkeyArray[i], types[i]);
            }

            return buf[3];
        }

        private async Task<byte> SetupInputs(Socket client)
        {
            var command = RTDECommand.RTDE_CONTROL_PACKAGE_SETUP_INPUTS;

            byte[] payload = Encoding.UTF8.GetBytes(_inputKeyString);

            await SendCommandAsync(client, command, payload);

            byte[] buf = await ReceiveBytesAsync(client);

            if (buf == null)
                throw new IOException("Setup input error");

            byte[] typesBuf = new byte[buf.Length - 4];
            Array.Copy(buf, 4, typesBuf, 0, typesBuf.Length);

            string[] inkeyArray = _inputKeyString.Split(",");
            string[] types = Encoding.UTF8.GetString(typesBuf).Split(",");

            _inputKeyTypes.Clear();
            for (int i = 0; i < inkeyArray.Length; i++)
            {
                _inputKeyTypes.Add(inkeyArray[i], types[i]);
            }

            return buf[3];
        }

        private async Task<bool> StartReceivingAsync(Socket client)
        {
            var command = RTDECommand.RTDE_CONTROL_PACKAGE_START;

            await SendCommandAsync(client, command, null);

            byte[] buf = await ReceiveBytesAsync(client);

            if (buf != null)
                return buf[3] == 1;
            else
                return false;
        }

        private async Task<bool> RequestProtocolVersionAsync(Socket client)
        {
            var cmd = RTDECommand.RTDE_REQUEST_PROTOCOL_VERSION;

            byte version = 2;
            byte[] payload = [0, version];

            await SendCommandAsync(client, cmd, payload);
            byte[] buf = await ReceiveBytesAsync(client);

            if (buf != null)
            {
                return buf[3] == 1 ? true : false;
            }
            else
                return false;
        }

        private async Task SendDataAsync(Socket client)
        {

            object[] values = OnSendData?.Invoke();

            byte[] buf = SetValueToBuffer(values);
            byte[] payload = new byte[buf.Length + 1];

            payload[0] = _inputRecipe;

            Array.Copy(buf, 0, payload, 1, buf.Length);

            await SendCommandAsync(client, RTDECommand.RTDE_DATA_PACKAGE, payload);
        }

        private async Task ReceiveDataAsync(Socket client)
        {
            byte[] buf = await ReceiveBytesAsync(client);

            if (buf[2] == (byte)RTDECommand.RTDE_DATA_PACKAGE)
            {
                byte[] dataBuf = new byte[buf.Length - 4];
                Array.Copy(buf, 4, dataBuf, 0, buf.Length - 4);

                object[] values = GetValuesFromBuffer(dataBuf);

                OnReceiveData?.Invoke(values);

            }
            else if (buf[2] == (byte)RTDECommand.RTDE_TEXT_MESSAGE)
            {

                byte[] dataDuf = new byte[buf.Length - 3];

                int size_m = (int)buf[3];
                int size_s = (int)buf[3 + size_m + 1];

                byte[] buf_m = new byte[size_m];
                byte[] buf_s = new byte[size_s];

                Array.Copy(dataDuf, 4, buf_m, 0, size_m);
                Array.Copy(dataDuf, 4 + size_m + 1, buf_s, 0, size_s);

                byte wlevel = dataDuf[dataDuf.Length - 1];

                string message = Encoding.UTF8.GetString(buf_m);
                string source = Encoding.UTF8.GetString(buf_s);

                throw new Exception(message + "/" + source + "/Level:" + wlevel.ToString());
            }
            else
                throw new Exception("Recieve Error");
        }

        private async Task SendCommandAsync(Socket client, RTDECommand command, byte[]? payload)
        {

            short bufLen = (short)((payload != null ? payload.Length : 0) + 3);

            byte[] sizeBuf = BitConverter.GetBytes(bufLen).Reverse().ToArray();
            byte[] commandBuf = [(byte)command];

            byte[] buf = sizeBuf.Concat(commandBuf).ToArray();

            if (payload != null)
                buf = buf.Concat(payload).ToArray();

            await client.SendAsync(buf, SocketFlags.None);

        }

        private async Task<byte[]> ReceiveBytesAsync(Socket client)
        {
            byte[] buf = new byte[1024];

            int size = await client.ReceiveAsync(buf, SocketFlags.None);

            byte[] res = new byte[size];

            Array.Copy(buf, res, res.Length);

            return res;

        }

        private object[] GetValuesFromBuffer(byte[] dataBuf)
        {
            int current = 0;
            int k = 0;

            List<object> values = new();

            foreach (var keyType in _outputKeyTypes)
            {
                string type = keyType.Value;
                int size = RTDEBuffer.ByteSize(type);

                byte[]? _buf = new byte[size];

                Array.Copy(dataBuf, current, _buf, 0, size);

                object? value = RTDEBuffer.Decode(type, _buf);
                if (value != null)
                    values.Add(value);

                current = current + size;
            }

            return values.ToArray();
        }

        private byte[] SetValueToBuffer(object[] values)
        {
            int k = 0;
            byte[] buf = new byte[] { };

            foreach (var keyType in _inputKeyTypes)
            {
                string type = keyType.Value;
                object value = values[k];

                byte[]? _buf = RTDEBuffer.Encode(type, value);

                if (_buf != null)
                    buf = buf.Concat(_buf).ToArray();

                k++;
            }

            return buf;
        }
    }
}
