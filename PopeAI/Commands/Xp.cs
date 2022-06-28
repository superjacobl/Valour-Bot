using PopeAI.Database.Models.Planets;
using Valour.Shared.Items.Messages.Embeds;

namespace PopeAI.Commands.Xp;

public class Xp : CommandModuleBase
{

    [Event(EventType.AfterCommand)]
    public async Task AfterCommand(CommandContext ctx)
    {
        StatManager.selfstat.TimeTakenTotal += (ulong)(DateTime.UtcNow - ctx.TimeReceived).TotalMilliseconds;
        StatManager.selfstat.Commands += 1;
    }

    [Event(EventType.Message)]
    public async Task OnMessage(CommandContext ctx)
    {
        // put this message in queue for storage
        MessageManager.AddToQueue(ctx.Message);

        if (ctx.Message.AuthorId == ulong.Parse(Client.Config.BotId) || (await ctx.Member.GetUserAsync()).Bot) {
            return;
        }

        await DailyTaskManager.DidTask(DailyTaskType.Messages, ctx.Member.Id, ctx);

        await StatManager.AddStat(CurrentStatType.Message, 1, ctx.Member.PlanetId);

        DBUser user = DBCache.Get<DBUser>(ctx.Member.Id);

        if (user == null)
        {
            user = new(ctx.Member);
        }

        user.NewMessage(ctx.Message);
    }

    [Command("xp")]
    [Summary("Gives the user who sent the command their xp.")]
    public async Task SendXp(CommandContext ctx)
    {
        var user = DBCache.Get<DBUser>(ctx.Member.Id);
        EmbedBuilder embed = new();
        var page = new EmbedPageBuilder()
            .AddText($"{ctx.Member.Nickname}'s Xp")
            .AddText("Message Xp", ((ulong)user.MessageXp).ToString())
            .AddText("Elemental Xp", ((ulong)user.ElementalXp).ToString())
            .AddText("Total Xp", ((ulong)user.Xp).ToString());

        embed.AddPage(page);
    }

    [Command("leaderboard")]
    [Summary("Returns the leaderboard of the users with the most xp.")]
    public async Task Leaderboard(CommandContext ctx)
    {
        List<DBUser> users = DBCache.GetAll<DBUser>()
            .Where(x => x.PlanetId == ctx.Planet.Id)
            .OrderByDescending(x => x.Xp)
            .Take(10)
            .ToList();

        EmbedBuilder embed = new();
        EmbedPageBuilder page = new();
        int i = 1;
        foreach (DBUser user in users)
        {
            PlanetMember member = await PlanetMember.FindAsync(ctx.Planet.Id, user.UserId);
            page.AddText(text:$"({i}) {member.Nickname} - {(ulong)user.Xp}xp");
            i += 1;
            if (page.Items.Count() > 10) {
                embed.AddPage(page);
                page = new();
            }
        }
        embed.AddPage(page);
        await ctx.ReplyAsync(embed);
    }
}