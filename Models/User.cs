using Microsoft.EntityFrameworkCore;
using System;

namespace QuestQuokka.Models
{
    public class User
    {
        public ulong UserId { get; set; }
        public int Score { get; set; } = 0;
        public DateTime LastDaily { get; set; } = DateTime.MinValue;
    }
}
