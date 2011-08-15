using System;
using System.IO;
using Terraria_Server;
using Terraria_Server.Events;
using Terraria_Server.Misc;
using Terraria_Server.Plugin;

//TODO: 
// Color support for IRC prints
// Show player join/part/kick/nick/quits ingame.
// Commands for the chat
// Show on IRC if the person is OPd

namespace IRCage
{
    public class IRCage : Plugin
    {
        bool isEnabled; 
        AIRCH mircc;
        PropertiesFile pfile;

        public override void Load()
        {
            base.Name = "IRCage";
            base.Description = "IRC integration for TDSM";
            base.Author = "AWRyder";
            base.Version = "0.6";
            base.TDSMBuild = 29;
            this.isEnabled = true;

            string ppfolder = Statics.PluginPath + Path.DirectorySeparatorChar + "IRCage";
            string ppfile = ppfolder + Path.DirectorySeparatorChar + "ircage.pfile";

            if (!Directory.Exists(ppfolder))
                 Directory.CreateDirectory(ppfolder);

            if (!File.Exists(ppfile))
                File.Create(ppfile).Close();

            pfile = new PropertiesFile(ppfile);


            Program.tConsole.WriteLine(base.Name + " has been enabled.");
        }

        public override void Enable()
        {
            pfile.Load();
            String hostname = pfile.getValue("hostname", "apocalypse.esper.net");
            int port = pfile.getValue("port", 6667);
            String channelName = pfile.getValue("channel", "#empire");
            String nick = pfile.getValue("nick", "IRCage");
            String username = pfile.getValue("username", "IRCage");
            String realName = pfile.getValue("realname", "IRC bot plugin for TDSM by AWRyder");
            String nspass = pfile.getValue("nickserv-pass", "");
            String serverPass = pfile.getValue("serverpass", "");
            String quitMessage = pfile.getValue("quitMessage", "Bye Bye!");
            String commandDelim = pfile.getValue("command_prefix", "+");
            bool ircColors = pfile.getValue("irc-colors", false);
            pfile.Save();

            mircc = new AIRCH(hostname, port, channelName, nick, username, realName,nspass, serverPass,quitMessage, commandDelim,ircColors);
            mircc.connect();

            registerHook(Hooks.PLAYER_CHAT);
            registerHook(Hooks.PLAYER_LOGIN);
            registerHook(Hooks.PLAYER_LOGOUT);
            registerHook(Hooks.PLAYER_DEATH);
            registerHook(Hooks.PLAYER_COMMAND);

            AddCommand("irc").WithAccessLevel(Terraria_Server.Commands.AccessLevel.OP).Calls(this.parseCommands);
        }

        public override void Disable()
        {
            pfile.Save();
            Program.tConsole.WriteLine(base.Name + " has been disabled.");
            isEnabled = false;
            this.mircc.close();
        }

        public void parseCommands(Server serv, Terraria_Server.Commands.ISender sender,Terraria_Server.Commands.ArgumentList argv)
        {
            if (sender is Player)
            {
                if (argv.GetString(0) == "colors")
                {
                    if (argv.GetString(1) == "on") { mircc.setIrcColors(true); sender.sendMessage("IRC colors enabled."); }
                    else if (argv.GetString(1) == "off") { mircc.setIrcColors(false); sender.sendMessage("IRC colors disabled."); }
                    else
                    {
                        sender.sendMessage("Syntax is: /irc colors <on/off> ");
                    }
                }
                else if (argv.GetString(0) == "help")
                {
                    sender.sendMessage("Syntax: /irc <option>");
                    sender.sendMessage("Options: help, colors");
                }
                else
                {
                    sender.sendMessage("No such option. ");
                }
            }
        }

        public override void onPlayerChat(MessageEvent Event)
        {
            String msg = "<" + Event.Sender.Name + "> " + Event.Message;
            if (mircc.getIrcColors()) { msg = AIRCH.CODE_COLOR + "1" + msg; }
            mircc.sendToChan(msg);
            base.onPlayerChat(Event);
        }
        public override void onPlayerCommand(PlayerCommandEvent Event)
        {
            if (Event.Message.StartsWith("/me "))
            {
                String msg = "* " + Event.Sender.Name + " " + Event.Message.Substring(4);
                if (mircc.getIrcColors()) { msg = AIRCH.CODE_COLOR + "6" + msg; }
                mircc.sendToChan(msg);
            }
            base.onPlayerCommand(Event);
        }
        public override void  onPlayerDeath(PlayerDeathEvent Event)
        {
            String msg = Event.Sender.Name + Event.DeathMessage;
            if (mircc.getIrcColors()) { msg = AIRCH.CODE_COLOR + "4" + msg; }
            mircc.sendToChan(msg);
 	        base.onPlayerDeath(Event);
        }
        public override void onPlayerJoin(PlayerLoginEvent Event)
        {
            String msg = Event.Sender.Name + " has joined the server.";
            if (mircc.getIrcColors()) { msg = AIRCH.CODE_COLOR + "3" + msg; }
            mircc.sendToChan(msg);
            base.onPlayerJoin(Event);
        }
        public override void onPlayerLogout(PlayerLogoutEvent Event)
        {
            if (Event.Sender.Name.Length > 0)
            {
                String msg = Event.Sender.Name + " has left the server.";
                if (mircc.getIrcColors()) { msg = AIRCH.CODE_COLOR + "3" + msg; }
                mircc.sendToChan(msg);
            }
            base.onPlayerLogout(Event);
        }

    }
}