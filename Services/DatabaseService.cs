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
                    ActiveDate = DateTime.Today,
                    LastMilestoneDays = (int)Math.Floor((DateTime.Today - soberDate).TotalDays),
                });
            }
            else
            {
                existingRecord.SobrietyDate = soberDate;
                existingRecord.UserName = userName;
                existingRecord.LastMilestoneDays = (int)Math.Floor((DateTime.Today - soberDate).TotalDays);
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

        public void UpdateActiveDate(ulong serverId, ulong userId)
        {
            var sobriety = _context.Sobrieties.FirstOrDefault(s => s.ServerID == serverId && s.UserID == userId);
            if (sobriety != null)
            {
                sobriety.ActiveDate = DateTime.Now;
                _context.Update(sobriety);
                _context.SaveChanges();
            }
        }

        public void PruneInactiveUsers(ulong serverId)
        {
            var pruneDays = _context.Config.FirstOrDefault(c => c.ServerID == serverId)?.PruneDays ?? 30;
            if (pruneDays == 0) pruneDays = 30;
            var inactiveSobrieties = _context.Sobrieties.Where(s => s.ServerID == serverId && s.ActiveDate < DateTime.Now.AddDays(-pruneDays));
            _context.RemoveRange(inactiveSobrieties);
            _context.SaveChanges();
        }

        public void SetPruneDays(ulong serverId, int days)
        {
            var config = _context.Config.FirstOrDefault(c => c.ServerID == serverId);
            if (config == null)
            {
                _context.Config.Add(new Config
                {
                    ServerID = serverId,
                    PruneDays = days,
                });
            }
            else
            {
                config.PruneDays = days;
                _context.Update(config);
            }
            _context.SaveChanges();
        }

        public void BanUser(ulong serverId, ulong userId, string message)
        {
            var ban = _context.Bans.FirstOrDefault(b => b.ServerID == serverId && b.UserID == userId);
            if (ban == null)
            {
                _context.Bans.Add(new Ban
                {
                    ServerID = serverId,
                    UserID = userId,
                    Message = message,
                });
                RemoveSobriety(serverId, userId);
            }
            else
            {
                ban.Message = message;
                _context.Update(ban);
            }
            _context.SaveChanges();
        }

        public void UnbanUser(ulong serverId, ulong userId)
        {
            var ban = _context.Bans.FirstOrDefault(b => b.ServerID == serverId && b.UserID == userId);
            if (ban != null)
            {
                _context.Remove(ban);
                _context.SaveChanges();
            }
        }

        public string GetBanMessage(ulong serverId, ulong userId)
        {
            return _context.Bans.FirstOrDefault(b => b.ServerID == serverId && b.UserID == userId)?.Message;
        }

        public string GetNewMilestoneName(ulong serverId, ulong userId)
        {
            var sobriety = GetSobriety(serverId, userId);

            if (sobriety != null)
            {
                var milestone = _context.Milestones
                    .OrderByDescending(m => m.Days)
                    .FirstOrDefault(m => m.Days > sobriety.LastMilestoneDays
                                      && (DateTime.Today - sobriety.SobrietyDate).TotalDays >= m.Days);

                if (milestone != null)
                {
                    sobriety.LastMilestoneDays = milestone.Days;
                    _context.Update(sobriety);
                    _context.SaveChanges();

                    return milestone.Name;
                }
            }

            return null;
        }

        public void SetMilestoneChannel(ulong serverId, ulong channelId)
        {
            var config = _context.Config.FirstOrDefault(c => c.ServerID == serverId);
            if (config == null)
            {
                _context.Config.Add(new Config
                {
                    ServerID = serverId,
                    MilestoneChannelID = channelId,
                });
            }
            else
            {
                config.MilestoneChannelID = channelId;
                _context.Update(config);
            }
            _context.SaveChanges();
        }

        public ulong? GetMilestoneChannel(ulong serverId)
        {
            return _context.Config.FirstOrDefault(c => c.ServerID == serverId)?.MilestoneChannelID;
        }
    }
}