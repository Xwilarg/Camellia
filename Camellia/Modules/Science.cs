using Dangl.Calculator;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Camellia.Modules
{
    public class Science : ModuleBase
    {
        public static async Task DurationAsync(IServiceProvider _, SocketSlashCommand cmd)
        {
            var d1Str = (string)cmd.Data.Options.First(x => x.Name == "startdate").Value;
            var d2Str = (string)cmd.Data.Options.First(x => x.Name == "enddate").Value;

            if (!DateTime.TryParse(d1Str, out var d1))
            {
                await cmd.RespondAsync("Starting date isn't in a valid format", ephemeral: true);
                return;
            }
            if (!DateTime.TryParse(d2Str, out var d2))
            {
                await cmd.RespondAsync("Ending date isn't in a valid format", ephemeral: true);
                return;
            }
            if (d1 > d2)
            {
                await cmd.RespondAsync("Starting date must be anterior to ending date", ephemeral: true);
                return;
            }

            var duration = d2 - d1;
            await cmd.RespondAsync(embed: new EmbedBuilder
            {
                Color = Color.Blue,
                Fields = [
                    new()
                    {
                        Name = "Total days",
                        Value = duration.TotalDays
                    },
                    new()
                    {
                        Name = "Total hours",
                        Value = duration.TotalHours
                    }
                ]
            }.Build());
        }

        /*public async Task CalcAsync([Remainder]string str)
        {
            await ReplyAsync(Calculator.Calculate(str).Result.ToString());
        }*/

        public async Task JsonAsync([Remainder]string str = null)
        {
            str = await GetCleanInputCodeAsync("json", str);
            if (str == null)
            {
                await ReplyAsync("You must provide the text to parse, either as an argument, as an attachment or as the previous message by giving \"^\" as a parameter.");
                return;
            }
            try
            {
                JsonConvert.DeserializeObject(str);
                await ReplyAsync("Your JSON is valid");
            }
            catch (JsonReaderException e)
            {
                await ReplyAsync("Your JSON is **not** valid:\n```" + e.Message + "\n```");
            }
        }

        public async Task XmlAsync([Remainder] string str = null)
        {
            str = await GetCleanInputCodeAsync("xml", str);
            if (str == null)
            {
                await ReplyAsync("You must provide the text to parse, either as an argument, as an attachment or as the previous message by giving \"^\" as a parameter.");
                return;
            }
            var xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.LoadXml(str);
                await ReplyAsync("Your XML is valid");
            }
            catch (XmlException e)
            {
                await ReplyAsync("Your XML is **not** valid:\n```" + e.Message + "\n```");
            }
        }

        private IAttachment GetAttachment()
        {
            // Gets the attached file, or if it's a reply the file attached to the reply, or null
            return Context.Message.Attachments.FirstOrDefault() ?? Context.Message.ReferencedMessage?.Attachments?.FirstOrDefault();
        }

        private async Task<string> GetCleanInputCodeAsync(string currentLanguage, string str)
        {
            if (str == null)
            {
                if (Context.Message.Attachments.Any()) // Empty message but contains an attachment
                {
                    return await StaticObjects.HttpClient.GetStringAsync(Context.Message.Attachments.ElementAt(0).Url);
                }
                return null;
            }
            if (str == "^") // We need to check previous message
            {
                var msg = await GetLastMessageAsync();
                if (msg.Attachments.Any()) // Previous message has an attachment
                {
                    return await StaticObjects.HttpClient.GetStringAsync(msg.Attachments.ElementAt(0).Url);
                }
                else if (string.IsNullOrWhiteSpace(msg.Content)) // Previous message is empty
                {
                    return null;
                }
                str = msg.Content;
            }

            // Check for code tags
            if (str.StartsWith("```" + currentLanguage, StringComparison.InvariantCultureIgnoreCase) && str.EndsWith("```"))
            {
                return str[(3 + currentLanguage.Length)..^3].Trim();
            }
            if (str.StartsWith("```") && str.EndsWith("```"))
            {
                return str[3..^3].Trim();
            }
            return str.Trim();
        }

        private async Task<IMessage> GetLastMessageAsync()
        {
            var msgs = await Context.Channel.GetMessagesAsync(2).FlattenAsync();
            if (msgs.Count() == 2)
            {
                return msgs.ElementAt(1);
            }
            return null;
        }

        private async Task<IMessage> ReplyFileAsync(string contents, string name)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(contents));
            return await Context.Channel.SendFileAsync(stream, name);
        }

        public static async Task LengthAsync(IServiceProvider _, SocketSlashCommand cmd)
        {
            var str = (string)cmd.Data.Options.First(x => x.Name == "text").Value;

            await cmd.RespondAsync(embed: new EmbedBuilder
            {
                Color = Color.Blue,
                Fields = [
                    new()
                    {
                        Name = "Length",
                        Value = str.Length
                    },
                    new()
                    {
                        Name = "Alphanumeric only",
                        Value = str.Count(x => char.IsLetterOrDigit(x))
                    }
                ]
            }.Build());
        }

        public async Task HexAsync(params int[] numbers)
        {
            await ReplyAsync(string.Join(" ", numbers.Select(x => x.ToString("X"))));
        }

        public async Task DecAsync(params Hex[] numbers)
        {
            await ReplyAsync(string.Join(" ", numbers.Select(x => x.Value)));
        }

        public static async Task RandomBytesAsync(IServiceProvider _, SocketSlashCommand cmd)
        {
            long length = ((long?)cmd.Data.Options.FirstOrDefault(x => x.Name == "length")?.Value) ?? 16;

            if (length <= 0 || length > 512)
            {
                await cmd.RespondAsync("You must specify a length between 1 and 512 bytes.", ephemeral: true);
                return;
            }

            var bytes = new byte[length];
            RandomNumberGenerator.Fill(bytes);

            await cmd.RespondAsync(Convert.ToHexString(bytes));
        }

        public async Task HexDump()
        {
            var attachment = GetAttachment();

            if (attachment == null)
            {
                await ReplyAsync("You must provide a file.");
                return;
            }

            // Download the file and take the first 0x2000 bytes
            var contents = await StaticObjects.HttpClient.GetByteArrayAsync(attachment.Url);
            var data = contents.Take(0x2000).ToArray();

            var str = Utils.ToHexdump(data);

            if (str.Length < 1000)
            {
                await ReplyAsync("```\n" + str + "\n```");
            }
            else
            {
                await ReplyFileAsync(str, "hexdump.txt");
            }
        }
    }
}
