namespace Objectivity.Bot.Tests.Stories.DirectLine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Connector.DirectLine;
    using Newtonsoft.Json.Linq;
    using Bot.DirectLine.DirectLine;
    using Bot.DirectLine.Messenger;
    using Player;
    using StoryModel;
    using Xunit;

    public class DirectLineStoryPlayer : IStoryPlayer
    {
        private readonly IUserBotMessengerService userBotMessengerService;

        private readonly List<Activity> receivedMessages = new List<Activity>();

        private string[] latestOptions;

        private readonly IDictionary<string, object> outputValues = new Dictionary<string, object>();

        private string userId;
        private string UserId  => this.userId ?? (this.userId = Guid.NewGuid().ToString());

        public DirectLineStoryPlayer(DirectLineClient directLineClient, string userId = null)
        {
            if (directLineClient == null)
            {
                throw new ArgumentNullException(nameof(directLineClient));
            }

            this.userId = userId;

            this.userBotMessengerService = new UserBotMessengerService(new DirectLineConversationService(directLineClient));
        }

        public async Task<IStoryResult> Play(IStory story, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken == default(CancellationToken))
            {
                cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token;
            }

            var steps = story.StoryFrames.Select(sf => new StoryPlayerStep
                    {
                        StoryFrame = sf,
                        Status = StoryPlayerStepStatus.NotDone,
                    })
                .ToArray();

            await this.InitializeMessaging(cancellationToken);

            foreach (var step in steps)
            {
                await this.Process(step, cancellationToken);
            }

            // At the end there should be no unprocessed received messages
            if (steps.Last().StoryFrame.Actor == Actor.Bot)
            {
                Assert.Empty(this.receivedMessages);
            }

            var result = new StoryResult();

            foreach (var pair in this.outputValues)
            {
                result.OutputValues.Add(pair.Key, pair.Value);
            }

            return result;
        }

        private async Task InitializeMessaging(CancellationToken cancellationToken = default(CancellationToken))
        {
            await this.userBotMessengerService.StartMessagingForUser(
                    this.UserId,
                    (sender, args) => { this.receivedMessages.AddRange(args.Messages); },
                    cancellationToken);

            this.userBotMessengerService.GetOrCreateMessengerForUser(this.UserId).MessengerInitialized.Wait(cancellationToken);
        }

        private void RemoveTypingMessages()
        {
            var message = this.receivedMessages.FirstOrDefault();
            while (message != null && message.Type == "typing")
            {
                this.receivedMessages.Remove(message);
                message = this.receivedMessages.FirstOrDefault();
            }
        }

        private async Task Process(StoryPlayerStep step, CancellationToken cancellationToken = default(CancellationToken))
        {
            var storyFrame = step.StoryFrame;

            var isUserFrame = storyFrame.Actor == Actor.User;
            var isBotFrame = storyFrame.Actor == Actor.Bot;

            if (isUserFrame)
            {
                switch (storyFrame.ComparisonType)
                {
                    case ComparisonType.TextExact:
                        await this.ProcessUserFrameTextExact(storyFrame, cancellationToken);
                        break;
                    case ComparisonType.Option:
                        await this.ProcessUserFrameOption(storyFrame, cancellationToken);
                        break;
                }
                
            }
            else if (isBotFrame)
            {
                switch (storyFrame.ComparisonType)
                {
                    case ComparisonType.TextExact:
                        this.ProcessBotFrameTextExact(storyFrame);
                        break;

                    case ComparisonType.AttachmentListPresent:
                        this.ProcessBotFrameListPresent(storyFrame);
                        break;
                }
            }
        }

        private void ProcessBotFrameTextExact(IStoryFrame storyFrame)
        {
            if (storyFrame == null)
            {
                throw new ArgumentNullException(nameof(storyFrame));
            }

            this.RemoveTypingMessages();

            var message = this.receivedMessages.FirstOrDefault();
            Assert.NotNull(message);
            Assert.Equal("message", message.Type);
            Assert.Equal(storyFrame.Text, message.Text);

            this.receivedMessages.Remove(message);
        }

        private void ProcessBotFrameListPresent(IStoryFrame storyFrame)
        {
            this.RemoveTypingMessages();

            var message = this.receivedMessages.FirstOrDefault();
            Assert.NotNull(message);
            Assert.Equal("list", message.AttachmentLayout);
            Assert.Equal(1, message.Attachments.Count);

            var listJson = (JObject)message.Attachments[0].Content;
            if (storyFrame.ListPredicate != null)
            {
                Assert.True(storyFrame.ListPredicate(listJson), "List contains expected item");
            }

            this.latestOptions = listJson.SelectToken("buttons").Select(item => item["value"].ToString()).ToArray();

            this.receivedMessages.Remove(message);
        }

        private async Task ProcessUserFrameTextExact(IStoryFrame storyStoryFrame, CancellationToken cancellationToken = default(CancellationToken))
        {
            // When user frame is being processed there should be no newly received messages from bot
            Assert.Empty(this.receivedMessages);

            await
                this.userBotMessengerService.GetOrCreateMessengerForUser(this.UserId)
                    .SendUserToBotMessageAsync(storyStoryFrame.Text, cancellationToken);
        }

        private async Task ProcessUserFrameOption(IStoryFrame storyFrame, CancellationToken cancellationToken = default(CancellationToken))
        {
            // When user frame is being processed there should be no newly received messages from bot
            Assert.Empty(this.receivedMessages);

            Assert.NotEmpty(this.latestOptions);

            var optionValue = this.latestOptions[storyFrame.OptionIndex];

            if (!string.IsNullOrEmpty(storyFrame.OptionOutputPlaceholder))
            {
                this.outputValues.Add(storyFrame.OptionOutputPlaceholder, optionValue);
            }

            await
                this.userBotMessengerService.GetOrCreateMessengerForUser(this.UserId)
                    .SendUserToBotMessageAsync(optionValue, cancellationToken);
        }
    }
}