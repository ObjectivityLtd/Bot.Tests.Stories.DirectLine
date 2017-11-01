# Bot.Tests.Stories.DirectLine
Test stories player utilizing Direct Line.

## Example
End-to-end test of interaction with a bot with Direct Line channel example.

```cs
[Fact]
public async Task()
{
    using (var directLineClient = new DirectLineClient(
        new DirectLineClientCredentials("<your_direct_line_secret>")))
    {
        var player = new DirectLineStoryPlayer(directLineClient);
        var story = StoryRecorder
            .Record()
            .User.Says("Hi")
            .Bot.Says("Hello!")
            .Rewind();

        var timeoutSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        await player.Play(story, timeoutSource.Token);
    }
}
```

