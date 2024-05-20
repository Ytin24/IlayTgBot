using Autofac;
using Autofac.Builder;
using FuckingDbIlay.Context;
using NLog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace FuckingIlayBot {
    internal partial class Program {
        static async Task Main(string[] args) {
            var Container = RegisterDi();
            var botClient = new TelegramBotClient("6715967257:AAEry5JdGItAExVIFrkvtm5rlnnd6GDtzj8");
            using CancellationTokenSource cts = new();
            ReceiverOptions receiverOptions = new() {
                AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
            };
            var bot = Container.Resolve<ITelegramBot>();
            botClient.StartReceiving(
                updateHandler: bot.HandleUpdateAsync,
                pollingErrorHandler: bot.HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );
            var me = await botClient.GetMeAsync();
            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();
            cts.Cancel();

        }
        private static IContainer RegisterDi() {
            var builder = new ContainerBuilder();
            builder.RegisterInstance<Logger>(LogManager.GetCurrentClassLogger()).As<ILogger>();

            builder.RegisterType<NotesDbContext>()
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<TelegramBot>()
                .As<ITelegramBot>()
                .SingleInstance();

            builder.RegisterType<TelegramBotClient>()
                .WithParameter("token", "6715967257:AAEry5JdGItAExVIFrkvtm5rlnnd6GDtzj8")
                .SingleInstance();
            return builder.Build();
        }

    }
}
