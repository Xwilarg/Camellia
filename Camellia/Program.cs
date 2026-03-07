using Camellia.Modules;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Camellia
{
    class Program
    {
        private ulong? _debugGuild;
        private IServiceProvider _provider;

        public static async Task Main()
        {
            var client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            });
            await new Program(client).MainAsync(client);
        }

        private Program(DiscordSocketClient client)
        {
            client.Log += (msg) =>
            {
                Console.WriteLine(msg);
                return Task.CompletedTask;
            };
            /*
            _commands.Log += async (msg) =>
            {
                Console.WriteLine(msg);
#if DEBUG
                if (msg.Exception is CommandException ce)
                {
                    await ce.Context.Channel.SendMessageAsync(embed: new EmbedBuilder
                    {
                        Color = Color.Red,
                        Title = msg.Exception.InnerException.GetType().ToString(),
                        Description = msg.Exception.InnerException.Message
                    }.Build());
                }
#endif
            };*/

            CultureInfo culture = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            culture.NumberFormat.NumberDecimalSeparator = ".";
            Thread.CurrentThread.CurrentCulture = culture;
        }

        private async Task MainAsync(DiscordSocketClient client)
        {
            //_client.MessageReceived += HandleCommandAsync;

            /*_commands.AddTypeReader<Hex>(new HexReader());

            await _commands.AddModuleAsync<Communication>(null);
            await _commands.AddModuleAsync<Science>(null);*/

            var credentials = JsonSerializer.Deserialize<Credentials>(File.ReadAllText("Keys/Credentials.json"), new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            _debugGuild = credentials.DebugGuild;

            client.Ready += Ready;
            client.SlashCommandExecuted += SlashCommandExecuted;

            if (credentials == null) throw new InvalidOperationException("Credentials not found");
            if (credentials.BotToken == null) throw new InvalidOperationException("Bot token cannot be null");

            _provider = new ServiceCollection()
                .AddSingleton<HttpClient>()
                .AddSingleton(client)
                .BuildServiceProvider();

            await client.LoginAsync(TokenType.Bot, credentials.BotToken);
            await client.StartAsync();

            await Task.Delay(-1);
        }

        private async Task SlashCommandExecuted(SocketSlashCommand arg)
        {
            var target = Commands.FirstOrDefault(x => string.Compare(x.SlashCommand.Name, arg.CommandName, true) == 0);
            if (target == null) throw new NotImplementedException();

            await target.Callback(_provider, arg);
        }

        private CommandInfo[] Commands = [
            new CommandInfo()
            {
                SlashCommand = new SlashCommandBuilder()
                    .WithName("length")
                    .WithDescription("Give the length of a text")
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("text")
                        .WithType(ApplicationCommandOptionType.String)
                        .WithDescription("Text to get the length of")
                        .WithRequired(true)
                    ),
                Callback = Science.LengthAsync
            },
            new CommandInfo()
            {
                SlashCommand = new SlashCommandBuilder()
                    .WithName("invite")
                    .WithDescription("Get the invite link of the bot"),
                Callback = Communication.InviteAsync
            },
            new CommandInfo()
            {
                SlashCommand = new SlashCommandBuilder()
                    .WithName("bytes")
                    .WithDescription("Generate a string of cryptographic random bytes")
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("length")
                        .WithType(ApplicationCommandOptionType.Number)
                        .WithDescription("Number of bytes")
                        .WithRequired(false)
                    ),
                Callback = Science.RandomBytesAsync
            },
            new CommandInfo()
            {
                SlashCommand = new SlashCommandBuilder()
                    .WithName("duration")
                    .WithDescription("Display the duration between 2 dates")
                    .AddOptions([
                        new SlashCommandOptionBuilder()
                        .WithName("startdate")
                        .WithType(ApplicationCommandOptionType.String)
                        .WithDescription("Starting date")
                        .WithRequired(true),
                        new SlashCommandOptionBuilder()
                        .WithName("enddate")
                        .WithType(ApplicationCommandOptionType.String)
                        .WithDescription("Ending date")
                        .WithRequired(true)
                    ]),
                Callback = Science.DurationAsync
            },
            new CommandInfo()
            {
                SlashCommand = new SlashCommandBuilder()
                    .WithName("hexdump")
                    .WithDescription("Displays a hex dump of the contents of the specified file")
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("file")
                        .WithType(ApplicationCommandOptionType.Attachment)
                        .WithDescription("File to check")
                        .WithRequired(true)
                    ),
                Callback = Science.HexDumpAsync
            },
        ];
        private bool _areCommandLoaded;
        private async Task Ready()
        {
            if (_areCommandLoaded)
            {
                return;
            }
            _areCommandLoaded = true;

            _ = Task.Run(async () =>
            {
                try
                {
                    var client = _provider.GetRequiredService<DiscordSocketClient>();

                    SocketGuild debugGuild = null;
                    if (_debugGuild != null) client.GetGuild(_debugGuild.Value);
                    var cmds = Commands.Select(x => x.SlashCommand.Build());
                    foreach (var c in cmds)
                    {
                        if (debugGuild != null)
                        {
                            await debugGuild.CreateApplicationCommandAsync(c);
                        }
                        else
                        {
                            await client.CreateGlobalApplicationCommandAsync(c);
                        }
                    }

                    if (debugGuild != null)
                    {
                        await debugGuild.BulkOverwriteApplicationCommandAsync(cmds.ToArray());
                    }
                    else
                    {
                        await client.BulkOverwriteGlobalApplicationCommandsAsync(cmds.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to preload commands: {ex}");
                }
            });
        }
    }
}
