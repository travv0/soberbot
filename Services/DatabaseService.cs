using DiscordBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Services
{
    public class DatabaseService
    {
        private SoberContext _context;

        public void Initialize(SoberContext soberContext)
        {
            _context = soberContext;
            _context.Database.EnsureCreated();
        }

        public void SetDate(ulong serverId, ulong userId, string userName, DateTime soberDate)
        {
            var existingRecord = _context.Sobrieties
                .FirstOrDefault(s => s.ServerID == serverId && s.UserID == userId);

            if (existingRecord == null)
            {
                _context.Sobrieties.Add(new Sobriety
                {
                    ServerID = serverId,
                    UserID = userId,
                    UserName = userName,
                    SobrietyDate = soberDate,
                });
            }
            else
            {
                existingRecord.SobrietyDate = soberDate;
                existingRecord.UserName = userName;
                _context.Sobrieties.Update(existingRecord);
            }

            _context.SaveChanges();
        }

        public List<Sobriety> GetSobrieties(ulong serverId)
        {
            return _context.Sobrieties.Where(s => s.ServerID == serverId).ToList();
        }

        public Sobriety GetSobriety(ulong serverId, ulong userId)
        {
            return _context.Sobrieties
                .FirstOrDefault(s => s.ServerID == serverId && s.UserID == userId);
        }

        public void RemoveSobriety(ulong serverId, ulong userId)
        {
            var sobriety = _context.Sobrieties.FirstOrDefault(s => s.ServerID == serverId && s.UserID == userId);
            if (sobriety != null)
            {
                _context.Remove(sobriety);
                _context.SaveChanges();
            }
        }
    }
}