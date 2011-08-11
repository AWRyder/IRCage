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
            base.Version = "0.5";
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
            String commandDelim = pfile.getValue("command_initial", "+");
            bool ircColors = pfile.getValue("irc-colors", false);
            pfile.Save();

            mircc = new AIRCH(hostname, port, channelName, nick, username, realName,nspass, serverPass,quitMessage, commandDelim,ircColors);
            mircc.connect();

            registerHook(Hooks.PLAYER_CHAT);
            registerHook(Hooks.PLAYER_LOGIN);
            registerHook(Hooks.PLAYER_LOGOUT);
            registerHook(Hooks.PLAYER_DEATH);
            registerHook(Hooks.PLAYER_COMMAND);
        }

        public override void Disable()
        {
            pfile.Save();
            Program.tConsole.WriteLine(base.Name + " has been disabled.");
            isEnabled = false;
            this.mircc.close();
        }

        public override void onPlayerChat(MessageEvent Event)
        {
            mircc.sendToChan("<"+Event.Sender.Name+"> "+Event.Message);
            base.onPlayerChat(Event);
        }
        public override void onPlayerCommand(PlayerCommandEvent Event)
        {
            if (Event.Message.StartsWith("/me "))
            {
                mircc.sendToChan("* "+Event.Sender.Name+" "+Event.Message.Substring(4));
            }
            base.onPlayerCommand(Event);
        }
        public override void  onPlayerDeath(PlayerDeathEvent Event)
        {
            mircc.sendToChan(Event.Sender.Name+""+Event.DeathMessage);
 	         base.onPlayerDeath(Event);
        }
        public override void onPlayerJoin(PlayerLoginEvent Event)
        {
            mircc.sendToChan(Event.Sender.Name + " has joined the server.");
            base.onPlayerJoin(Event);
        }
        public override void onPlayerLogout(PlayerLogoutEvent Event)
        {
            mircc.sendToChan(Event.Sender.Name + " has left the server.");
            base.onPlayerLogout(Event);
        }

    }
}