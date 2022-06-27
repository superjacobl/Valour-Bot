/*
stats
roll
*/

namespace PopeAI.Commands.Mod
{
    public class Mod : CommandModuleBase
    {
        [Command("purge")]
        public async Task PurgeAsync(CommandContext ctx, int messages)
        {
            if (!(ctx.Member.User_Id == ctx.Planet.Owner_Id)) {
                return;
            }
            //if (!(await ctx.Channel.HasPermissionAsync(ctx.Member.Id, ChatChannelPermissions.ManageMessages))) {
            //    return;
            //}
            if (messages > 64) {
                messages = 64;
            }
            List<PlanetMessage> ChannelMessages = await ctx.Channel.GetLastMessagesAsync(count: messages);
            ChannelMessages.Reverse();
            await ctx.ReplyAsync($"Purging {messages} from this channel!");
            int i = 0;
            foreach(PlanetMessage message in ChannelMessages) {
                if ((await (await message.GetAuthorAsync()).GetUserAsync()).Id == ctx.Planet.Owner_Id) {
                    continue;
                }
                Console.WriteLine(message.Content);
                if (!(await message.DeleteAsync()).Success) {
                    Console.WriteLine(await JsonContent.Create(message).ReadAsStringAsync());
                }
                i += 1;
                if (i >= messages) {
                    return;
                }
            }
        }

        [Group("fliterword")]
        public class FliterWordGroup : CommandModuleBase
        {
        }
    }
}