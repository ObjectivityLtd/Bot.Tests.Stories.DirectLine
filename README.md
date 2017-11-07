# Bot.Tests.Stories.DirectLine

[![Build status](https://ci.appveyor.com/api/projects/status/v03ykn8gs6lavats?svg=true)](https://ci.appveyor.com/project/ObjectivityAdminsTeam/bot-tests-stories-directline)

Test stories player utilizing Direct Line.

(https://travis-ci.org/Homebrew/install)

## Install 
Use nuget package manager
```powershell
Install-Package Objectivity.Bot.Tests.Stories.DirectLine
```

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

