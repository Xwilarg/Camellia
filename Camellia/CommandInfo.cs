using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Camellia;

public class CommandInfo
{
    public Func<IServiceProvider, SocketSlashCommand, Task> Callback { set; get; }
    public SlashCommandBuilder SlashCommand { set; get; }
}