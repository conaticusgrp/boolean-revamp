using Boolean.Util;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Boolean;

public partial class ServerConfig
{
    // Channel configuration, welcome messages, starboard, etc
    [SlashCommand("channel", "Marks a channel for a certain purpose")]
    public async Task ChannelSet(SpecialChannelType specialChannelType, SocketTextChannel channelTarget)
    {
        var embed = new EmbedBuilder().WithColor(EmbedColors.Success);
        
        // Ensure bot has permission to talk in the specified channel
        if (channelTarget.Guild.CurrentUser.GetPermissions(channelTarget).SendMessages == false) {
            embed.Description = $"I am unable to view {channelTarget.Mention}";
            embed.Color = EmbedColors.Fail;
            await RespondAsync(embed: embed.Build(), ephemeral: true);
            return;
        }

        var guild = await db.Guilds.FirstOrDefaultAsync(s => s.Snowflake == channelTarget.Guild.Id);
        if (guild == null) {
            guild = new Guild { Snowflake = channelTarget.Guild.Id };
            await db.Guilds.AddAsync(guild);
        }

        var specialChannel = await SpecialChannelTools.GetSpecialChannel(db, Context.Guild.Id, specialChannelType);
        if (specialChannel != null)
            specialChannel.Snowflake = channelTarget.Id;
        else
            await db.SpecialChannels.AddAsync(new SpecialChannel
            {
                Guild = guild,
                Snowflake = channelTarget.Id,
                Type = specialChannelType
            });
        
        await db.SaveChangesAsync();
        
        // Use of ToString() is fine for now, we will want to implement a parser later when we add special channels with multiple words (for upper & lower case)
        embed.Description = $"{specialChannelType.ToString()} channel has been set to {channelTarget.Mention}";
        await RespondAsync(embed: embed.Build(), ephemeral: true);
    }
}
// /get, used to get configuration options
[DefaultMemberPermissions(GuildPermission.Administrator)]
[Group("get", "Get configuration options")]
public class ServerGet(DataContext db)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("channel", "Get the current configuration for channels")]
    public async Task ChannelGet(SpecialChannelType specialChannelType)
    {
        var embed = new EmbedBuilder().WithColor(EmbedColors.Normal);
        
        var channel = await SpecialChannelTools.GetSpecialChannel(db, Context.Guild.Id, specialChannelType);
        var specialChannelName = specialChannelType.ToString().ToLower();
        
        if (channel != null)
            embed.Description = $"The current {specialChannelName} channel is set to <#{channel.Snowflake}>";
        else {
            embed.Description = $"There currently isn't a {specialChannelName} channel setup. To set it up use the `/set channel` command.";
            embed.Color = EmbedColors.Fail;
        }
        
        await RespondAsync(embed: embed.Build(), ephemeral: true);
    }
}
[DefaultMemberPermissions(GuildPermission.Administrator)]
[Group("unset", "Unset configuration options")]
public class ServerUnset(DataContext db)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("channel", "Unmarks a channel for a certain purpose")]
    public async Task ChannelUnset(SpecialChannelType specialChannelType)
    {
        var specialChannel = await SpecialChannelTools.GetSpecialChannel(db, Context.Guild.Id, specialChannelType);
        
        if (specialChannel != null) {
            db.SpecialChannels.Remove(specialChannel);
            await db.SaveChangesAsync();
        }
        
        var embed = new EmbedBuilder
        {
            Description = $"{specialChannelType.ToString()} channel has been unset",
            Color = EmbedColors.Success,
        };
        
        await RespondAsync(embed: embed.Build(), ephemeral: true);
    }
}
