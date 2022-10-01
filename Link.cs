﻿using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Text;

namespace NetWolf
{
    public class Link
    {
        private readonly int port = WLServer.Port;
        private readonly Mutex wolfMtx;
        private readonly Socket mathKernel;
        private readonly Process? wlserver;
        private static Mutex wlserverMtx = new Mutex();
        public Engine Engine { get; private set; }

        public Link(string ip = "")
        {
            if (ip == "")
                ip = Dns.GetHostEntry(Dns.GetHostName()).AddressList.ToList().Where(x => x.AddressFamily == AddressFamily.InterNetwork).ToList().First().ToString();

            Engine = new Engine(this);
            wolfMtx = new Mutex();
            if (wlserverMtx.WaitOne(10))
            {
                string args = "";
                File.WriteAllText(WLServer.Name, WLServer.Code);
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = @"python";
                start.Arguments = string.Format("{0} {1}", WLServer.Name, args);
                start.UseShellExecute = false;// Do not use OS shell
                start.CreateNoWindow = true; // We don't need new window
                start.RedirectStandardOutput = true;// Any output, generated by application will be redirected back
                start.RedirectStandardError = true; // Any error in standard output will be redirected back (for example exceptions)
                wlserver = Process.Start(start);
                if (wlserver != null)
                {
                    StreamReader reader = wlserver.StandardOutput;
                    Thread.Sleep(2000);
                    new Thread(() =>
                    {
                        string stderr = wlserver.StandardError.ReadToEnd();
                        wlserverMtx.ReleaseMutex();
                        throw new Exception(stderr);
                    }).Start();
                }
            }

            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = IPAddress.Parse(ip);
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);
            mathKernel = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            mathKernel.Connect(remoteEP);
        }

        public string ToEngine(string input)
        {
            wolfMtx.WaitOne();

            byte[] byteData = Encoding.ASCII.GetBytes(input);
            if (mathKernel.Send(byteData) != byteData.Length)
                throw new Exception();
            byte[] rec = new byte[1000];
            int length = mathKernel.Receive(rec);
            string response = Encoding.ASCII.GetString(rec, 0, length);

            wolfMtx.ReleaseMutex();

            return response;
        }

        public void Dispose()
        {
            mathKernel.Dispose();
        }

        #region DEPRECATED

        /*
        public List<string> RecursiveSimplify(string input)
        {
            string pattern = @"Abs\[([^\[\]A]+|(?<Level>\[)|(?<-Level>\]))+(?(Level)(?!))\]";
            MatchCollection matchList = Regex.Matches(input, pattern);
            List<string> list = matchList.Cast<Match>().Select(match => match.Value).Distinct().ToList();
            List<string> binary = Enumerable.Range(0, (int)Math.Pow(2, list.Count)).Select(x => ToBinary(x, list.Count)).ToList();

            List<string> output = Execute(ReplaceAbs(input, list, binary)).Select(x => x.Text).ToList();
            for (int i = 0; i < output.Count; i++)
            {
                if (output[i].Contains("Abs["))
                {
                    string tmp = output[i];
                    output.RemoveAt(i);
                    output.AddRange(RecursiveSimplify(tmp));
                    i = -1;
                }
            }

            return output;
        }

        private List<string> ReplaceAbs(string input, List<string> abs, List<string> positive)
        {
            List<string> ret = new List<string>();
            List<string> args = abs.Select(x => x.Substring(4, x.Length - 5)).ToList();
            for (int i = 0; i < positive.Count; i++)
            {
                string retEl = input;
                string pos = positive[i];
                for (int p = 0; p < pos.Length; p++)
                {
                    string argsSign = "";
                    if (pos[p] == '1')
                        argsSign = args[p];
                    else if (args[p][0] == 's')
                        argsSign = "-" + string.Concat(args[p].Select(c => c == '-' ? '+' : c == '+' ? '-' : c));
                    else
                        argsSign = string.Concat(args[p].Select(c => c == '-' ? '+' : c == '+' ? '-' : c));

                    retEl = retEl.Replace(abs[p], "(" + argsSign + ")");
                }

                ret.Add(retEl);
            }

            return ret;
        }

        private string ToBinary(int x, int size = 32)
        {
            char[] buff = new char[size];

            for (int i = size - 1; i >= 0; i--)
            {
                int mask = 1 << i;
                buff[size - 1 - i] = (x & mask) != 0 ? '1' : '0';
            }

            return new string(buff);
        }
        */

        #endregion DEPRECATED
    }
}