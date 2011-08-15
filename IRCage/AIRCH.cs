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

        static Random r =new Random();

        String host;
        int port;
        String pass;
        String nspass;
        string nick;
        string username;
        string realname;
        string channel;
        String quitMessage;
        Char commandDelim;
        bool ircColors;

        Thread pingThread;
        Thread mainLoop;
        TcpClient tc;
        NetworkStream stream;
        StreamReader reader;
        StreamWriter writer;

        public AIRCH(String host, int port, String channel = "#terraria", String nick="IRCage", String username="IRCage", String realname="IRC support for TDSM",String nspass="", String pass = "", String quitMessage = "Bye bye!", String commandDelim = "+", bool ircColors=false)
        {
            this.host = host;
            this.port = port;
            this.nick = nick;
            this.username = username;
            this.realname = realname;
            this.channel = channel;
            this.nspass = nspass;
            this.pass = pass;
            this.quitMessage = quitMessage;
            this.commandDelim = commandDelim.First();
            this.ircColors = ircColors;
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

                        parseIRCRaw(inputLine);
                    }
                }
                catch (Exception ex)
                {
                    Program.tConsole.WriteLine("IRCAGE ERROR: " + ex.Message);
                    //mainLoop.Abort();
                }
            }
        }

        private void parseIRCRaw(String raw)
        {
            if (raw.Substring(0, 6) == "PING :")
            {
                String answer = raw.Substring(6);
                sendRaw("PONG :" + answer);
                return;
            }

            String code = raw.Split(' ').ElementAt(1);

            switch (code)
            {
                case "001": sendRaw("JOIN :" + this.channel); break;
                case "433":{ 
                    sendRaw("NICK :"+this.nick+ genRandomNum().ToString());
                    break;
                }
                case "NOTICE":
                {
                    //15-08-2011 14:18:47 ?> [Debug] RAW: :NickServ!NickServ@services.esper.net NOTICE IRCage :This nickname is registered. Please choose a different nickname, or identify via /msg NickServ identify <password>.
                    //Allright?
                    String nick = raw.Split(' ').ElementAt(0).Substring(1, raw.IndexOf('!') - 1);
                    String message = raw.Substring(1).Substring(raw.IndexOf(':', 1));
                    if (nick.ToLower() == "nickserv" && message.Contains("This nickname is registered.") )
                    {
                        sendRaw("PRIVMSG nickserv :id "+this.nspass);
                    }

                    break;
                }
                case "PRIVMSG": 
                {
                    String nick = raw.Split(' ').ElementAt(0).Substring(1, raw.IndexOf('!') - 1);
                    String message = raw.Substring(1).Substring(raw.IndexOf(':', 1));
                    //It's a command!
                    if (message.First() == commandDelim)
                    {
                        String[] argv = message.Split(' ');
                        argv[0] = argv.First().Substring(1);

                        parseIRCCommand(argv);
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
                    break;
                }
                case "JOIN":
                {
                    String nick = raw.Split(' ').ElementAt(0).Substring(1, raw.IndexOf('!') - 1);
                    NetMessage.SendData(25, -1, -1, "[IRC]" + nick + " has joined the channel.", 255, 150, 150, 150);
                    break;
                }
                case "QUIT":
                case "PART":
                {
                    String nick = raw.Split(' ').ElementAt(0).Substring(1, raw.IndexOf('!') - 1);
                    NetMessage.SendData(25, -1, -1, "[IRC]" + nick + " has left the channel.", 255, 150, 150, 150);
                    break;
                }
                case "KICK":
                {
                    //Raw sample, just to make sure. :P <- :AWRyder!~AWRyder@lala KICK #somechan someguy :AWRyder
                    String nick = raw.Split(' ').ElementAt(0).Substring(1, raw.IndexOf('!') - 1);
                    String kicked = raw.Split(' ').ElementAt(3);
                    String message = raw.Substring(1).Substring(raw.IndexOf(':', 1));
                    NetMessage.SendData(25, -1, -1, "[IRC]" + kicked + " was kicked from the channel by "+nick+" ('"+message+"').", 255, 150, 150, 150);
                    break;
                }


            }
        }

        private void parseIRCCommand(String[] argv)
        {
            if (argv[0] == "list")
            {
                var pls = from p in Server.players where p.Active select p.Name;
                String sPlayerList = "";
                foreach (String pl in pls)
                {
                    sPlayerList += pl + ", ";
                }
                if (sPlayerList != "")
                {
                    sendToChan("Online Players: " + sPlayerList.Substring(0, sPlayerList.Length - 2));
                }
                else
                {
                    sendToChan("No online players at the moment.");
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

        public int genRandomNum()
        {
            int nr=r.Next(10);
            for (int i = 0; i < 3; i++)
            {
                nr = nr * 10;
                nr += AIRCH.r.Next(10);
            }
            return nr;
        }
    }
}
