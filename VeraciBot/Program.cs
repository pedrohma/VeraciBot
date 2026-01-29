using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VeraciBot.Data;
using VeraciBotCore.APIs.OpenAI;
using VeraciBotCore.APIs.Twitter;
using VeraciLib.Interfaces;
using VeraciLib.Services;
using VeraciLib.Settings;

namespace VeraciBot
{
    class Program
    {
        private ILogger<Program> _logger;

        static async Task Main(string[] args)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogInformation("Starting application");

            // read configurations from appsettings.json or environment variables as needed
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // map the configurations to AppSettings class
            var appSettings = new AppSettings();
            configuration.GetSection("AppSettings").Bind(appSettings);

            var services = new ServiceCollection();
            services.AddLogging(configure => configure.AddConsole());
            services.AddSingleton(appSettings);
            services.AddDbContext<VeraciDbContext>(options =>
                options.UseSqlServer(appSettings.DatabaseSettings.ConnectionString)
            );
            services.AddSingleton<ITwitterActions, TwitterServices>();
            services.AddSingleton<IOpenAiActions, OpenAiServices>();
            services.AddTransient<DbConfig>();
            services.AddTransient<Phrases>();
            var serviceProvider = services.BuildServiceProvider();

            logger.LogInformation("Initializing database context");
            var dbContext = serviceProvider.GetRequiredService<VeraciDbContext>();
            // Cria o banco e a tabela automaticamente se não existirem
            dbContext.Database.EnsureCreated();
            logger.LogInformation("Database context initialized");

            // Cria a tarefa e espera ela
            Task tarefa1 = ThreadCicloTwitterChatGpt(appSettings, services);

            Console.WriteLine("As tarefas foram iniciadas...");

            // Aguarda as duas tarefas terminarem
            await Task.WhenAll(tarefa1);

            Console.WriteLine("Programa finalizado.");
        }

        static async Task ThreadCicloTwitterChatGpt(
            AppSettings appSettings,
            ServiceCollection services
        )
        {
            Console.WriteLine("TWIT: Connecting VERACIBOT database");

            Console.WriteLine("TWIT: Starting VERACIBOT bot");

            var dbConfig = services.BuildServiceProvider().GetRequiredService<DbConfig>();
            var dbContext = services.BuildServiceProvider().GetRequiredService<VeraciDbContext>();
            var twitterApi = services.BuildServiceProvider().GetRequiredService<ITwitterActions>();
            var openAiServices = services
                .BuildServiceProvider()
                .GetRequiredService<IOpenAiActions>();
            var phrases = services.BuildServiceProvider().GetRequiredService<Phrases>();

            string startTime = dbConfig
                .GetLastDateTimeForTwitterCheck()
                .Result.ToString("yyyy-MM-ddTHH:mm:ssZ");

            Console.WriteLine("TWIT: Checking mentions to @veracibot since " + startTime);

            while (true)
            {
                try
                {
                    var mentions = await twitterApi.GetUserMentions(startTime);

                    // Só a partir de agora

                    if (mentions is null)
                    {
                        Console.WriteLine("No mentions since " + startTime);
                        startTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    }
                    else
                    {
                        Console.WriteLine("Treating mentions since " + startTime);
                        startTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                        DateTime lastTime = DateTime.Parse(startTime);

                        foreach (var tweet in mentions.Tweets)
                        {
                            // Pega infos do tweet

                            if (tweet.Id == null)
                            {
                                Console.WriteLine("Tweet id is null, skipping.");
                                continue;
                            }
                            string tweetId = tweet.Id;

                            if (tweet.AuthorId == null)
                            {
                                Console.WriteLine($"Tweet {tweetId} author_id is null, skipping.");
                                continue;
                            }
                            string authorId = tweet.AuthorId;

                            if (tweet.CreatedAt == null)
                            {
                                Console.WriteLine($"Tweet {tweetId} created_at is null, skipping.");
                                continue;
                            }
                            string tweetDate = tweet.CreatedAt;

                            // Atualiza o último tweet processado

                            lastTime = DateTime.Parse(tweetDate);
                            dbConfig.SetLastDateTimeForTwitterCheck(lastTime).Wait();

                            // Já tratei esse tweet? -> ignora

                            var previousTweet = await dbContext.Tweets.FirstOrDefaultAsync(e =>
                                e.Id == tweetId
                            );
                            if (previousTweet != null)
                            {
                                Console.WriteLine($"Tweet {tweetId} already processed.");
                                continue;
                            }

                            // Usuário tem autorização?

                            var authorization = await dbContext.AuthorizedUsers.FirstOrDefaultAsync(
                                e => e.Id == authorId
                            );
                            if (
                                authorization == null
                                || (
                                    authorization != null
                                    && authorization.Status == AuthorizedUser.STATUS_NOT_AUTHORIZED
                                )
                            )
                            {
                                // Não está autorizado

                                VeraciBot.Data.Tweet notAuthTweet = new Data.Tweet()
                                {
                                    Id = tweetId,
                                    OriginalText = "",
                                    ThreadId = tweetId,
                                    Text = "",
                                    AuthorId = authorId,
                                    OriginalAuthorId = authorId,
                                    Result = 0,
                                };

                                dbContext.Tweets.Add(notAuthTweet);
                                dbContext.SaveChanges();

                                // TODO: Verificar a lingua do usuário e responder na língua correta

                                string notAuthorizedResponseText =
                                    await phrases.GetNotAuthorizedResponseAsync("pt");

                                await twitterApi.PostReplyWithImageAsync(
                                    notAuthorizedResponseText,
                                    Images.notAuthorizedImage,
                                    tweetId
                                );

                                continue;
                            }

                            // Tenho que tratar esse tweet

                            Console.WriteLine($"Getting full thread {tweetId}...");

                            // Pega todo o contexto da thread que o tweet faz parte

                            ThreadContext fullThread = await twitterApi.GetThreadContext(
                                tweetId,
                                authorId
                            );
                            if (
                                fullThread != null
                                && fullThread.AuthorA != appSettings.xSettings.UserId
                            )
                            {
                                // Temos que saber se é thread ou apenas uma chamada simples

                                bool isSingleTweet = true;

                                if (
                                    fullThread != null
                                    && fullThread.AuthorA != appSettings.xSettings.UserId
                                    && fullThread.Tweets.Count > 1
                                )
                                    isSingleTweet = false;

                                // O que o usuário pediu?

                                Console.WriteLine($"Getting command from {tweetId}...");

                                string commandstr = tweet.Text ?? "";

                                // Chama o CHAT GPT para identificar o comando

                                IdentifiedCommand cmd = await openAiServices.CheckCommand(
                                    commandstr,
                                    isSingleTweet
                                );

                                // Checa comando invalido

                                if (
                                    cmd == null
                                    || cmd.Result == OpenAiServices.CMD_UNKNOWN
                                    || (
                                        cmd.Result != OpenAiServices.CMD_HELP
                                        && cmd.Result != OpenAiServices.CMD_SCORE
                                        && cmd.Result != OpenAiServices.CMD_SCOREBOARD
                                        && cmd.Result != OpenAiServices.CMD_INVITE
                                        && cmd.Result != OpenAiServices.CMD_ACCEPT_INVITE
                                        && cmd.Result != OpenAiServices.CMD_REFUSE_INVITE
                                        && cmd.Result != OpenAiServices.CMD_THREAD_FALSE
                                        && cmd.Result != OpenAiServices.CMD_THREAD_ARGUE
                                        && cmd.Result != OpenAiServices.CMD_THREAD_WHOISRIGHT
                                    )
                                )
                                {
                                    // Não entendi o comando

                                    VeraciBot.Data.Tweet failToUnderstandTweet = new Data.Tweet()
                                    {
                                        Id = tweetId,
                                        OriginalText = "",
                                        ThreadId = tweetId,
                                        Text = "",
                                        AuthorId = authorId,
                                        OriginalAuthorId = authorId,
                                        Result = 0,
                                    };

                                    dbContext.Tweets.Add(failToUnderstandTweet);
                                    dbContext.SaveChanges();

                                    string failedToUndesrstadText =
                                        await phrases.GetFailedToUnderstandResponseAsync("pt");

                                    await twitterApi.PostReplyWithImageAsync(
                                        failedToUndesrstadText,
                                        Images.failedToUnderstandImage,
                                        tweetId
                                    );

                                    continue;
                                }

                                // Verficia autorização específica para aceitar (ou não) convite

                                if (
                                    authorization != null
                                    && authorization.Status == AuthorizedUser.STATUS_INVITED
                                    && cmd.Result != OpenAiServices.CMD_ACCEPT_INVITE
                                    && cmd.Result != OpenAiServices.CMD_REFUSE_INVITE
                                )
                                {
                                    // Não entendi o comando, precisa aceitar ou negar

                                    VeraciBot.Data.Tweet failToUnderstandTweet = new Data.Tweet()
                                    {
                                        Id = tweetId,
                                        OriginalText = "",
                                        ThreadId = tweetId,
                                        Text = "",
                                        AuthorId = authorId,
                                        OriginalAuthorId = authorId,
                                        Result = 0,
                                    };

                                    dbContext.Tweets.Add(failToUnderstandTweet);
                                    dbContext.SaveChanges();

                                    string failedToUndesrstadAcceptText =
                                        await phrases.GetFailedToUnderstandAcceptResponseAsync(
                                            "pt"
                                        );

                                    await twitterApi.PostReplyWithImageAsync(
                                        failedToUndesrstadAcceptText,
                                        Images.failedToUnderstandAcceptImage,
                                        tweetId
                                    );

                                    continue;
                                }

                                // Comando de aceitar ou recusar quando autorização já foi feita... não faz sentido... ignorar

                                if (
                                    authorization != null
                                    && authorization.Status == AuthorizedUser.STATUS_AUTHORIZED
                                    && (
                                        cmd.Result == OpenAiServices.CMD_ACCEPT_INVITE
                                        || cmd.Result == OpenAiServices.CMD_REFUSE_INVITE
                                    )
                                )
                                {
                                    Console.WriteLine(
                                        $"Tweet {tweetId} aceitação ou recusa já feita. Ignorado."
                                    );
                                    continue;
                                }

                                // Executa o comando

                                switch (cmd.Result)
                                {
                                    case OpenAiServices.CMD_HELP: // Ajuda

                                        VeraciBot.Data.Tweet helpTweet = new Data.Tweet()
                                        {
                                            Id = tweetId,
                                            OriginalText = "",
                                            ThreadId = fullThread.Id,
                                            Text = "",
                                            AuthorId = authorId,
                                            OriginalAuthorId = fullThread.AuthorA,
                                            Result = 0,
                                        };

                                        dbContext.Tweets.Add(helpTweet);
                                        dbContext.SaveChanges();

                                        string helpResponseText =
                                            await phrases.GetHelpResponseAsync("pt");

                                        await twitterApi.PostReplyWithImageAsync(
                                            helpResponseText,
                                            Images.helpImage,
                                            tweetId
                                        );
                                        break;

                                    case OpenAiServices.CMD_SCORE: // Pontuacao

                                        VeraciBot.Data.Tweet scoreTweet = new Data.Tweet()
                                        {
                                            Id = tweetId,
                                            OriginalText = "",
                                            ThreadId = fullThread.Id,
                                            Text = "",
                                            AuthorId = authorId,
                                            OriginalAuthorId = fullThread.AuthorA,
                                            Result = 0,
                                        };

                                        dbContext.Tweets.Add(scoreTweet);
                                        dbContext.SaveChanges();

                                        TwitterUser author = await twitterApi.GetTwitterUserById(
                                            authorId
                                        );
                                        TweetAuthor authorTweet = await TweetAuthor.GetTweetAuthor(
                                            dbContext,
                                            authorId,
                                            author.Username,
                                            author.Name
                                        );

                                        string finalResponse = await phrases.GetScoreResponseAsync(
                                            "pt"
                                        );
                                        finalResponse += "\r\n\r\n" + authorTweet.GetDescription();

                                        await twitterApi.PostReplyWithImageAsync(
                                            finalResponse,
                                            Images.scoreImage,
                                            tweetId
                                        );
                                        break;

                                    case OpenAiServices.CMD_SCOREBOARD: // Taebela de pontuação

                                        VeraciBot.Data.Tweet scoreBoardTweet = new Data.Tweet()
                                        {
                                            Id = tweetId,
                                            OriginalText = "",
                                            ThreadId = fullThread.Id,
                                            Text = "",
                                            AuthorId = authorId,
                                            OriginalAuthorId = fullThread.AuthorA,
                                            Result = 0,
                                        };

                                        dbContext.Tweets.Add(scoreBoardTweet);
                                        dbContext.SaveChanges();

                                        string boardResponse = await phrases.GetScoreResponseAsync(
                                            "pt"
                                        );
                                        boardResponse +=
                                            "\r\n\r\n" + TweetAuthor.GetFullScoreBoard(10);

                                        await twitterApi.PostReplyWithImageAsync(
                                            boardResponse,
                                            Images.scoreBoardImage,
                                            tweetId
                                        );
                                        break;

                                    case OpenAiServices.CMD_INVITE: // Convidar outra pessoa

                                        VeraciBot.Data.Tweet InviteTweet = new Data.Tweet()
                                        {
                                            Id = tweetId,
                                            OriginalText = "",
                                            ThreadId = fullThread.Id,
                                            Text = "",
                                            AuthorId = authorId,
                                            OriginalAuthorId = fullThread.AuthorA,
                                            Result = 0,
                                        };

                                        dbContext.Tweets.Add(InviteTweet);
                                        dbContext.SaveChanges();

                                        string inviteText = await phrases.GetInviteResponseAsync(
                                            "pt"
                                        );

                                        // Identifica outros usuários no tweet

                                        string[] userNames = TwitterServices.FindUsersReference(
                                            commandstr
                                        );
                                        string inviteUserName = "";

                                        // Pega o último usuário que não seja o veracibot nem que postou

                                        string authorUserName = (
                                            await twitterApi.GetTwitterUserById(authorId)
                                        ).Username.ToLower();

                                        for (int i = userNames.Length - 1; i >= 0; i--)
                                        {
                                            if (
                                                userNames[i].ToLower() != "veracibot"
                                                && userNames[i].ToLower() != authorUserName
                                            )
                                            {
                                                inviteUserName = userNames[i];
                                                break;
                                            }
                                        }

                                        if (inviteUserName != null && inviteUserName != "")
                                        {
                                            // Identifica outros usuários no tweet

                                            TwitterUser userInvite =
                                                await twitterApi.GetTwitterUserByUserName(
                                                    inviteUserName
                                                );

                                            // Usuário já foi convidado?

                                            var inviteAuthorization =
                                                await dbContext.AuthorizedUsers.FirstOrDefaultAsync(
                                                    e => e.Id == userInvite.Id
                                                );
                                            if (
                                                inviteAuthorization != null
                                                && (
                                                    inviteAuthorization.Status
                                                        == AuthorizedUser.STATUS_AUTHORIZED
                                                    || inviteAuthorization.Status
                                                        == AuthorizedUser.STATUS_INVITED
                                                )
                                            )
                                            {
                                                // Já foi convidado ou já está jogando

                                                VeraciBot.Data.Tweet inviteErrorTweet =
                                                    new Data.Tweet()
                                                    {
                                                        Id = tweetId,
                                                        OriginalText = "",
                                                        ThreadId = fullThread.Id,
                                                        Text = "",
                                                        AuthorId = authorId,
                                                        OriginalAuthorId = fullThread.AuthorA,
                                                        Result = 0,
                                                    };

                                                dbContext.Tweets.Add(inviteErrorTweet);
                                                dbContext.SaveChanges();

                                                string inviteErrorText =
                                                    await phrases.GetInviteErrorResponseAsync("pt");

                                                await twitterApi.PostReplyWithImageAsync(
                                                    inviteErrorText,
                                                    Images.inviteErrorImage,
                                                    tweetId
                                                );

                                                break;
                                            }

                                            if (
                                                inviteAuthorization != null
                                                && inviteAuthorization.Status
                                                    == AuthorizedUser.STATUS_NOT_AUTHORIZED
                                            )
                                            {
                                                // Se o cara não estava autorizado, pode ser convidado de novo... Atualiza autorização para convidado

                                                inviteAuthorization.AuthorizedById =
                                                    fullThread.AuthorA;
                                                inviteAuthorization.AuthorizationDate =
                                                    DateTime.UtcNow;
                                                inviteAuthorization.Status =
                                                    AuthorizedUser.STATUS_INVITED;

                                                dbContext.AuthorizedUsers.Update(
                                                    inviteAuthorization
                                                );
                                                dbContext.SaveChanges();
                                            }
                                            else
                                            {
                                                // Cria autorização temporária para o usuário convidado

                                                VeraciBot.Data.AuthorizedUser newTempAuth =
                                                    new Data.AuthorizedUser()
                                                    {
                                                        Id = userInvite.Id,
                                                        AuthorizedById = fullThread.AuthorA,
                                                        AuthorizationDate = DateTime.UtcNow,
                                                        Status = AuthorizedUser.STATUS_INVITED,
                                                    };

                                                dbContext.AuthorizedUsers.Add(newTempAuth);
                                                dbContext.SaveChanges();
                                            }

                                            // Personaliza o convite com o nome do usuário

                                            inviteText = inviteUserName + " " + inviteText;

                                            await twitterApi.PostReplyWithImageAsync(
                                                inviteText,
                                                Images.inviteImage,
                                                tweetId
                                            );
                                        }
                                        else
                                        {
                                            // Não marcou outro usuário. Burro!

                                            VeraciBot.Data.Tweet inviteNoUserTweet =
                                                new Data.Tweet()
                                                {
                                                    Id = tweetId,
                                                    OriginalText = "",
                                                    ThreadId = fullThread.Id,
                                                    Text = "",
                                                    AuthorId = authorId,
                                                    OriginalAuthorId = fullThread.AuthorA,
                                                    Result = 0,
                                                };

                                            dbContext.Tweets.Add(inviteNoUserTweet);
                                            dbContext.SaveChanges();

                                            string inviteNoUserText =
                                                await phrases.GetInviteNoUserResponseAsync("pt");

                                            await twitterApi.PostReplyWithImageAsync(
                                                inviteNoUserText,
                                                Images.inviteNoUserImage,
                                                tweetId
                                            );
                                        }
                                        break;

                                    case OpenAiServices.CMD_ACCEPT_INVITE: // Aceitar convite

                                        VeraciBot.Data.Tweet AcceptInviteTweet = new Data.Tweet()
                                        {
                                            Id = tweetId,
                                            OriginalText = "",
                                            ThreadId = fullThread.Id,
                                            Text = "",
                                            AuthorId = authorId,
                                            OriginalAuthorId = fullThread.AuthorA,
                                            Result = 0,
                                        };

                                        dbContext.Tweets.Add(AcceptInviteTweet);
                                        dbContext.SaveChanges();

                                        // Atualiza autorização para definitiva

                                        if (authorization != null)
                                        {
                                            authorization.Status = AuthorizedUser.STATUS_AUTHORIZED;

                                            dbContext.AuthorizedUsers.Update(authorization);
                                            dbContext.SaveChanges();
                                        }

                                        // Coloca resposta

                                        string acceptText = await phrases.GetAcceptResponseAsync(
                                            "pt"
                                        );

                                        await twitterApi.PostReplyWithImageAsync(
                                            acceptText,
                                            Images.acceptImage,
                                            tweetId
                                        );

                                        break;

                                    case OpenAiServices.CMD_REFUSE_INVITE: // Não Aceitar convite

                                        VeraciBot.Data.Tweet RefuseInviteTweet = new Data.Tweet()
                                        {
                                            Id = tweetId,
                                            OriginalText = "",
                                            ThreadId = fullThread.Id,
                                            Text = "",
                                            AuthorId = authorId,
                                            OriginalAuthorId = fullThread.AuthorA,
                                            Result = 0,
                                        };

                                        dbContext.Tweets.Add(RefuseInviteTweet);
                                        dbContext.SaveChanges();

                                        // Atualiza autorização para definitiva

                                        if (authorization != null)
                                        {
                                            authorization.Status =
                                                AuthorizedUser.STATUS_NOT_AUTHORIZED;

                                            dbContext.AuthorizedUsers.Update(authorization);
                                            dbContext.SaveChanges();
                                        }

                                        // Coloca resposta

                                        string noAcceptText = await phrases.GetRefuseResponseAsync(
                                            "pt"
                                        );

                                        await twitterApi.PostReplyWithImageAsync(
                                            noAcceptText,
                                            Images.refuseImage,
                                            tweetId
                                        );

                                        break;
                                }

                                // Se for tweet simples, não faz mais nada. O resto só se aplica a threads

                                if (isSingleTweet)
                                    continue;

                                // Já tratou essa thread?

                                var previousThread = await dbContext.Tweets.FirstOrDefaultAsync(e =>
                                    e.ThreadId == fullThread.Id
                                );
                                if (previousThread != null)
                                {
                                    Console.WriteLine($"Thread {tweetId} already processed.");
                                    continue;
                                }

                                // Checa se tem crédito

                                TwitterUser userAuthorA = await twitterApi.GetTwitterUserById(
                                    fullThread.AuthorA
                                );
                                TwitterUser userAuthorB = await twitterApi.GetTwitterUserById(
                                    fullThread.AuthorB
                                );

                                TweetAuthor authorA = await TweetAuthor.GetTweetAuthor(
                                    dbContext,
                                    fullThread.AuthorA,
                                    userAuthorA.Username,
                                    userAuthorA.Name
                                );
                                TweetAuthor authorB = await TweetAuthor.GetTweetAuthor(
                                    dbContext,
                                    fullThread.AuthorB,
                                    userAuthorB.Username,
                                    userAuthorB.Name
                                );

                                fullThread.AuthorA = authorA.UserName;
                                fullThread.AuthorB = authorB.UserName;

                                // Executa o comando

                                switch (cmd.Result)
                                {
                                    case OpenAiServices.CMD_THREAD_FALSE: // Contesta informação da thread
                                        break;

                                    case OpenAiServices.CMD_THREAD_WHOISRIGHT: // Quem está certo na thread
                                        break;

                                    case OpenAiServices.CMD_THREAD_ARGUE: // Argumente sobre a thread

                                        // Chama o CHAT GPT

                                        FullEvaluation result = await openAiServices.CheckThread(
                                            fullThread
                                        );
                                        if (result == null)
                                        {
                                            Console.WriteLine(
                                                $"Thread {fullThread.Id} failed to check."
                                            );
                                            continue;
                                        }

                                        // Prepara a resposta

                                        VeraciBot.Data.Tweet fullResponseTweet = new Data.Tweet()
                                        {
                                            Id = tweetId,
                                            ThreadId = fullThread.Id,
                                            Text = fullThread.GetStartB(),
                                            OriginalText = fullThread.GetStartA(),
                                            AuthorId = fullThread.AuthorB,
                                            OriginalAuthorId = fullThread.AuthorA,
                                            Date = DateTime.UtcNow,
                                            Result = result.Result,
                                        };

                                        fullResponseTweet.ComputeAuthors(dbContext).Wait();

                                        dbContext.Tweets.Add(fullResponseTweet);
                                        dbContext.SaveChanges();

                                        string fullResponseImage =
                                            "img/resp" + result.Result + ".jpg";
                                        string fullResponseText = result.Response;

                                        fullResponseText =
                                            "@"
                                            + authorA.UserName
                                            + ": "
                                            + fullResponseText
                                            + "\n\n"
                                            + authorA.GetDescription()
                                            + "\n"
                                            + authorB.GetDescription();

                                        await twitterApi.PostReplyWithImageAsync(
                                            fullResponseText,
                                            fullResponseImage,
                                            tweetId
                                        );

                                        break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

                Thread.Sleep(60000);
            }
        }
    }
}
