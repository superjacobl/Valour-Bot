﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valour.Api.Items.Users;

namespace PopeAI.Database.Models.Users;

public class DBUser
{
    [Key]
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public ulong PlanetId { get; set; }
    public double Coins { get; set; }
    public ushort CharsThisMinute { get; set; }
    public ushort PointsThisMinute { get; set; }
    public int TotalPoints { get; set; }
    public int TotalChars { get; set; }
    public double MessageXp { get; set; }
    public double ElementalXp { get; set; }
    public int Messages { get; set; }
    public int ActiveMinutes { get; set; }
    public DateTime LastHourly { get; set; }
    public DateTime LastSentMessage { get; set; }

    [NotMapped]
    public double Xp
    {
        get
        {
            return MessageXp + ElementalXp;
        }
    }

    [NotMapped]
    public PlanetMember? _Member { get; set; }

    [NotMapped]
    public PlanetMember Member
    {
        get
        {
            if (_Member == null)
            {
                _Member = PlanetMember.FindAsync(Id).GetAwaiter().GetResult();
            }
            return _Member;
        }
    }

    public DBUser(PlanetMember planetMember)
    {
        MessageXp = 0;
        ElementalXp = 0;
        Coins = 0;
        CharsThisMinute = 0;
        PointsThisMinute = 0;
        TotalPoints = 0;
        TotalChars = 0;
        LastSentMessage = DateTime.UtcNow;
        LastHourly = DateTime.UtcNow.AddHours(-10);
        Id = planetMember.Id;
        UserId = planetMember.UserId;
        PlanetId = planetMember.PlanetId;
    }

    public static string RemoveWhitespace(string input)
    {
        return new string(input.ToCharArray()
            .Where(c => !char.IsWhiteSpace(c))
            .ToArray());
    }

    

    public void NewMessage(PlanetMessage msg)
    {
        if (LastSentMessage.AddSeconds(60) < DateTime.UtcNow)
        {
            double xpgain = (Math.Log10(PointsThisMinute) - 1) * 3;
            xpgain = Math.Max(0.2, xpgain);
            MessageXp += xpgain;
            ActiveMinutes += 1;
            PointsThisMinute = 0;
            LastSentMessage = DateTime.UtcNow;
        }

        string Content = RemoveWhitespace(msg.Content);

        Content = Content.Replace("*", "");

        ushort Points = 0;

        // each char grants 1 point
        Points += (ushort)Content.Length;

        // if there is media then add 100 points
        if (Content.Contains("https://vmps.valour.gg"))
        {
            Points += 100;
        }

        PointsThisMinute += Points;
        TotalChars += Content.Length;
        TotalPoints += Points;

        Messages += 1;
    }
}
