﻿namespace PopeAI.Bot.Managers;

public static class DailyTaskManager
{
    public static Random rnd = new();
    public static IdManager idManager = new();

    public static async ValueTask DidTask(DailyTaskType TaskType, long MemberId, CommandContext ctx = null, DBUser user = null)
    {
        bool fromabove = true;
        if (user is null) {
            user = await DBUser.GetAsync(MemberId);
            fromabove = false;
        }
        DailyTask task = user.DailyTasks.FirstOrDefault(x => x.TaskType == TaskType);
        if (task != null)
        {
            if (task.Done < task.Goal)
            {
                task.Done += 1;
                if (task.Done == task.Goal)
                {
                    user.Coins += task.Reward;
                    await StatManager.AddStat(CurrentStatType.Coins, task.Reward, user.PlanetId);
                    if (ctx != null)
                    {
                        string content = $"{ctx.Member.Nickname}, your {task.TaskType.ToString().Replace("_", " ")} daily task is done! You get {task.Reward} coins.";
                        ctx.ReplyWithDelayAsync(4000, content);
                    }
                }
            }
        }
        if (user.FromDB && !fromabove) {
            await user.UpdateDB();
        }
    }

    public static DailyTaskType RandomTask()
    {
        return rnd.Next(0, 5) switch
        {
            0 => DailyTaskType.Dice_Games_Played,
            1 => DailyTaskType.Hourly_Claims,
            2 => DailyTaskType.Gamble_Games_Played,
            3 => DailyTaskType.Messages,
            4 => DailyTaskType.Combined_Elements,
            _ => 0
        };
    }

    public static short Choice(short[] list)
    {
        return list[rnd.Next(0, list.Length)];
    }

    public static IEnumerable<DailyTask> GenerateNewDailyTasks(long Memberid)
    {
        List<DailyTask> toadd = new();

        for (int i = 0; i < 3; i++)
        {
            DailyTaskType tasktype = RandomTask();
            while (toadd.Any(x => x.TaskType == tasktype))
            {
                tasktype = RandomTask();
            }

            DailyTask task = new()
            {
                TaskType = tasktype,
                Id = idManager.Generate(),
                MemberId = Memberid,
                Done = 0
            };
            switch (tasktype)
            {
                case DailyTaskType.Messages:
                    task.Goal = Choice(new short[] { 10, 15, 20, 25, 30, 35, 40, 45, 50 });
                    task.Reward = Choice(new short[] { 50, 75, 100, 125, 150, 175, 200 });
                    break;
                case DailyTaskType.Hourly_Claims:
                    task.Goal = Choice(new short[] { 3, 4, 5 });
                    task.Reward = Choice(new short[] { 50, 75, 100, 125, 150, 175});
                    break;
                case DailyTaskType.Gamble_Games_Played:
                    task.Goal = Choice(new short[] { 5, 6, 7, 8, 9, 10 });
                    task.Reward = Choice(new short[] { 50, 75, 100, 125, 150, 175});
                    break;
                case DailyTaskType.Dice_Games_Played:
                    task.Goal = Choice(new short[] { 5, 6, 7, 8, 9, 10 });
                    task.Reward = Choice(new short[] { 50, 75, 100, 125, 150, 175, 200});
                    break;
                case DailyTaskType.Combined_Elements:
                    task.Goal = Choice(new short[] { 2, 3, 4, 5, 6 });
                    task.Reward = Choice(new short[] { 100, 125, 150, 175, 200, 225 });
                    break;
            }
            toadd.Add(task);
        }
        return toadd;
    }

    public static void UpdateTasks(DBUser user, PopeAIDB dbctx)
    {
        DailyTask task;
        List<DailyTask> tasks = GenerateNewDailyTasks(user.Id).ToList();

        foreach (var oldtask in user.DailyTasks)
        {
            task = tasks[0];
            tasks.RemoveAt(0);
            oldtask.Done = 0;
            oldtask.Reward = task.Reward;
            oldtask.TaskType = task.TaskType;
        }
        if (tasks.Count > 0)
        {
            dbctx.DailyTasks.AddRange(tasks);
        }
    }
    
    public static async Task UpdateDailyTasks()
    {
        // only replace dailytasks if the day is different
        using var dbctx = PopeAIDB.DbFactory.CreateDbContext();

        var bottime = await dbctx.BotTimes.FirstOrDefaultAsync();

        if (bottime is null) {
            bottime = new() {
                Id = idManager.Generate(),
                LastDailyTasksUpdate = DateTime.UtcNow.AddDays(-1)
            };
            dbctx.BotTimes.Add(bottime);
            await dbctx.SaveChangesAsync();
        }

        if (bottime.LastDailyTasksUpdate.AddDays(1) > DateTime.UtcNow)
        {
            return;
        }

        bottime.LastDailyTasksUpdate = DateTime.UtcNow;

        // in future process this in chunks of like 10k because we would run out of memory
        // no sense in updating daily tasks for a user that is inactive
        DateTime time = DateTime.UtcNow.AddDays(-2);
        foreach (DBUser user in dbctx.Users.Where(x => x.LastSentMessage > time).Include(x => x.DailyTasks))
        {
            DailyTaskManager.UpdateTasks(user, dbctx);
        }
        await dbctx.SaveChangesAsync();
    }
}