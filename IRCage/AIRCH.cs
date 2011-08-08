//#define DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using Terraria_Server;
using System.Diagnostics;

namespace IRCage
{
    class AIRCH
    {
        String CODE_ACTION = "" + (char)001;
        String CODE_BOLD = "" + (char)002;
        String CODE_COLOR = "" + (char)003;
        
        String host;
        int port;
        String pass;
        string nick;
        string username;
        string realname;
        string channel;
        String quitMessage;
        Char commandDelim;

        Thread pingThread;
        Thread mainLoop;
        TcpClient tc;
        NetworkStream stream;
        StreamReader reader;
        StreamWriter writer;

        public AIRCH(String host, int port, String channel = "#terraria", String nick="IRCage", String username="IRCage", String realname="IRC support for TDSM", String pass = "", String quitMessage = "Bye bye!", String commandDelim = "+")
        {
            this.host = host;
            this.port = port;
            this.nick = nick;
            this.username = username;
            this.realname = realname;
            this.channel = channel;
            this.pass = pass;
            this.quitMessage = quitMessage;
            this.commandDelim = commandDelim.First();
        }

        public void connect()
        {
            try
            {
                this.tc = new TcpClient(this.host, this.port);
                this.stream = tc.GetStream();
                this.reader = new StreamReader(stream);
                this.writer = new StreamWriter(stream);

                if (this.pass != "")
                {
                    sendRaw("PASS :" + this.pass);
                }
                sendRaw("NICK " + this.nick);
                sendRaw("USER " + this.username + " 0 * :" + this.realname);
                sendRaw("JOIN " + this.channel);
                pingThread = new Thread(this.sendSPing);
                pingThread.Start();

                mainLoop = new Thread(this.loop);
                mainLoop.Start();
            } catch(Exception ex) {
                Program.tConsole.WriteLine("IRCAGE ERROR: " + ex);
                this.close();
            }
        }

        private void sendRaw(string raw)
        {
            try
            {
                this.writer.WriteLine(raw);
                this.writer.Flush(); 
                debug("RAW: -> " + raw);

            }
            catch (Exception)
            {
                
            }
        }


        private void debug(string msg)
        {
#if DEBUG 
            Program.tConsole.WriteLine("[Debug] " + msg);
#endif
        }

        private void sendSPing()
        {
            while (true)
            {
                if (tc.Connected)
                {
                    sendRaw("PING :" + this.host);
                    Thread.Sleep(90000);
                }
            }
        }
        private void loop()
        {
            while (true)
            {
                try
                {
                    if (this.tc == null || this.tc.Connected == false)
                    {
                        this.connect();
                        if (this.tc == null || this.tc.Connected == false)
                        {
                            continue;
                        }
                    }
                    String inputLine;
                    while ((inputLine = reader.ReadLine()) != null)
                    {
                        debug("RAW: " + inputLine);

                        String code = inputLine.Split(' ').ElementAt(1);

                        if (inputLine.Substring(0, 6) == "PING :")
                        {
                            String answer = inputLine.Substring(6);
                            sendRaw("PONG :" + answer);
                        }
                        else if (code == "001")
                        {
                            sendRaw("JOIN :" + this.channel);
                        }
                            //Must add commands here
                        else if (code == "PRIVMSG")
                        {
                            String nick = inputLine.Split(' ').ElementAt(0).Substring(1, inputLine.IndexOf('!') - 1);
                            String message = inputLine.Substring(1).Substring(inputLine.IndexOf(':', 1));
                            //It's a command!
                            if (message.First() == commandDelim)
                            {
                                String[] argv = message.Split(' ');
                                argv[0] = argv.First().Substring(1);

                                if (argv[0] == "list")
                                {
                                    Player[] pls = Program.server.PlayerList;
                                    String sPlayerList = "";
                                    for (int i = 0; i < pls.Length; i++)
                                    {
                                        Player pl = pls[i];
                                        sPlayerList += pl.Name;
                                        if (i < pls.Length - 1) { 
                                            sPlayerList += ", "; 
                                        }
                                    }
                                    sendToChan("Online Players: " + sPlayerList);
                                }
                            }
                            //It's just a msg
                            else
                            {
                                try
                                {
                                    if (message.Length >= 8 && message.Substring(0, 8) == CODE_ACTION + "ACTION " && message.Last() == CODE_ACTION.First<char>())
                                    {
                                        String action = message.Replace(CODE_ACTION, "");
                                        action = action.Substring(7);
                                        Program.server.notifyAll("*" + nick + " " + action);
                                    }
                                    else
                                    {
                                        message = Regex.Replace(message, "[^ a-zA-Z0-9!?-_\"#$%&/()=@'<>.,:;+*\\[\\]{}~\\^|\\\\«»]", "");
                                        NetMessage.SendData(25, -1, -1, "<[IRC]" + nick + "> " + message, 255, 150, 150, 150);
                                    }
                                }
                                catch (Exception)
                                {
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    //mainLoop.Abort();
                }
            }
        }

        public void sendToChan(String msg)
        {
            sendRaw("PRIVMSG " + this.channel + " :" + msg);

        }

        public void close()
        {
            if (this.tc != null)
            {
                sendRaw("QUIT :Bye bye!");
                pingThread.Abort();
                mainLoop.Abort();
                this.tc.Close();
            }
           
        }

    }
}
