using Discord;
using Discord.WebSocket;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Camellia.Modules
{
    public class Science
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
        /*
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
        */

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

        public static async Task HexDumpAsync(IServiceProvider _, SocketSlashCommand cmd)
        {
            var file = (IAttachment)cmd.Data.Options.First(x => x.Name == "file").Value;

            // Download the file and take the first 0x2000 bytes
            var contents = await StaticObjects.HttpClient.GetByteArrayAsync(file.Url);
            var data = contents.Take(0x2000).ToArray();

            var str = Utils.ToHexdump(data);

            if (str.Length < 1000)
            {
                await cmd.RespondAsync("```\n" + str + "\n```");
            }
            else
            {
                using MemoryStream ms = new();
                using StreamWriter writer = new(ms);
                writer.Write(str);
                writer.Flush();
                ms.Position = 0;
                await cmd.RespondWithFileAsync(ms, "hexdump.txt");
            }
        }
    }
}
