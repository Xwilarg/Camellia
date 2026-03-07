using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Camellia.Modules
{
    public class Communication : ModuleBase
    {
        [Command("Help")]
        public async Task HelpAsync()
        {
            await ReplyAsync(embed: new EmbedBuilder
            {
                Description =
                    "**Hex [decimal number]:** Convert decimal to hexadecimal\n" +
                    "**Dec [hexadecimal number]:** Convert hexadecimal to decimal\n" +
                    "**JSON [text]:** Check if a JSON is valid or not\n" +
                    "**XML [text]:** Check if an XML is valid or not\n" +
                    "**Calc [operation]:** Evaluate a mathematical expression\n" +
                    "**Bytes [number of bytes]:** Generates a string of cryptographic random bytes\n" +
                    "**Duration [date 1] [date 2]:** Display the duration between 2 dates\n" +
                    "**Hexdump:** Displays a hex dump of the contents of the specified file\n" +
                    "**Invite** Get the invite link of the bot",
                Color = Color.Blue,
                Footer = new EmbedFooterBuilder
                {
                    Text = "Any other question? Feel free to open an issue: https://github.com/Xwilarg/Camellia/"
                }
            }.Build());
        }

        public static async Task InviteAsync(IServiceProvider provider, SocketSlashCommand cmd)
        {
            await cmd.RespondAsync($"https://discord.com/api/oauth2/authorize?client_id={provider.GetRequiredService<DiscordSocketClient>().CurrentUser.Id}&scope=applications.commands+bot");
        }
    }
}
