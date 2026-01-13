using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;

namespace PayRollManagementSystem.Services
{
    public class HolidayService
    {
        private readonly ApplicationDbContext _context;

        public HolidayService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedBangladeshiHolidays(int year)
        {
            // Check if holidays already exist for this year
            var existingHolidays = _context.Holidays.Any(h => h.Date.Year == year);
            if (existingHolidays)
            {
                return; // Holidays already seeded for this year
            }

            var holidays = new List<Holiday>();

            // Fixed Bangladeshi Holidays (these dates are fixed every year)
            holidays.Add(new Holiday
            {
                Name = "Shaheed Day & International Mother Language Day",
                Date = new DateTime(year, 2, 21),
                Type = HolidayType.National,
                Description = "Martyrs' Day commemorating the language movement of 1952",
                IsActive = true,
                CreatedAt = DateTime.Now
            });

            holidays.Add(new Holiday
            {
                Name = "Birthday of Bangabandhu Sheikh Mujibur Rahman & National Children's Day",
                Date = new DateTime(year, 3, 17),
                Type = HolidayType.National,
                Description = "Birthday of the Father of the Nation",
                IsActive = true,
                CreatedAt = DateTime.Now
            });

            holidays.Add(new Holiday
            {
                Name = "Independence Day",
                Date = new DateTime(year, 3, 26),
                Type = HolidayType.National,
                Description = "Independence Day of Bangladesh",
                IsActive = true,
                CreatedAt = DateTime.Now
            });

            holidays.Add(new Holiday
            {
                Name = "Bengali New Year (Pahela Baishakh)",
                Date = new DateTime(year, 4, 14),
                Type = HolidayType.National,
                Description = "First day of Bengali calendar",
                IsActive = true,
                CreatedAt = DateTime.Now
            });

            holidays.Add(new Holiday
            {
                Name = "May Day",
                Date = new DateTime(year, 5, 1),
                Type = HolidayType.National,
                Description = "International Workers' Day",
                IsActive = true,
                CreatedAt = DateTime.Now
            });

            holidays.Add(new Holiday
            {
                Name = "National Mourning Day",
                Date = new DateTime(year, 8, 15),
                Type = HolidayType.National,
                Description = "Assassination of Bangabandhu Sheikh Mujibur Rahman",
                IsActive = true,
                CreatedAt = DateTime.Now
            });

            holidays.Add(new Holiday
            {
                Name = "Victory Day",
                Date = new DateTime(year, 12, 16),
                Type = HolidayType.National,
                Description = "Victory Day of Bangladesh",
                IsActive = true,
                CreatedAt = DateTime.Now
            });

            holidays.Add(new Holiday
            {
                Name = "Christmas Day",
                Date = new DateTime(year, 12, 25),
                Type = HolidayType.Religious,
                Description = "Christmas celebration",
                IsActive = true,
                CreatedAt = DateTime.Now
            });

            // Variable holidays (Islamic holidays - these dates change based on lunar calendar)
            // You'll need to update these dates annually based on moon sighting
            // For 2025, approximate dates:
            if (year == 2025)
            {
                // Shab-e-Barat
                holidays.Add(new Holiday
                {
                    Name = "Shab-e-Barat",
                    Date = new DateTime(2025, 2, 14),
                    Type = HolidayType.Religious,
                    Description = "Night of Fortune",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });

                // Shab-e-Qadr
                holidays.Add(new Holiday
                {
                    Name = "Shab-e-Qadr",
                    Date = new DateTime(2025, 3, 27),
                    Type = HolidayType.Religious,
                    Description = "Night of Destiny",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });

                // Eid-ul-Fitr (3 days)
                holidays.Add(new Holiday
                {
                    Name = "Eid-ul-Fitr",
                    Date = new DateTime(2025, 3, 31),
                    Type = HolidayType.Religious,
                    Description = "Festival of Breaking the Fast - Day 1",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });

                holidays.Add(new Holiday
                {
                    Name = "Eid-ul-Fitr",
                    Date = new DateTime(2025, 4, 1),
                    Type = HolidayType.Religious,
                    Description = "Festival of Breaking the Fast - Day 2",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });

                holidays.Add(new Holiday
                {
                    Name = "Eid-ul-Fitr",
                    Date = new DateTime(2025, 4, 2),
                    Type = HolidayType.Religious,
                    Description = "Festival of Breaking the Fast - Day 3",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });

                // Eid-ul-Azha (3 days)
                holidays.Add(new Holiday
                {
                    Name = "Eid-ul-Azha",
                    Date = new DateTime(2025, 6, 7),
                    Type = HolidayType.Religious,
                    Description = "Festival of Sacrifice - Day 1",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });

                holidays.Add(new Holiday
                {
                    Name = "Eid-ul-Azha",
                    Date = new DateTime(2025, 6, 8),
                    Type = HolidayType.Religious,
                    Description = "Festival of Sacrifice - Day 2",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });

                holidays.Add(new Holiday
                {
                    Name = "Eid-ul-Azha",
                    Date = new DateTime(2025, 6, 9),
                    Type = HolidayType.Religious,
                    Description = "Festival of Sacrifice - Day 3",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });

                // Ashura
                holidays.Add(new Holiday
                {
                    Name = "Ashura",
                    Date = new DateTime(2025, 7, 5),
                    Type = HolidayType.Religious,
                    Description = "Day of Ashura",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });

                // Eid-e-Milad-un-Nabi
                holidays.Add(new Holiday
                {
                    Name = "Eid-e-Milad-un-Nabi",
                    Date = new DateTime(2025, 9, 5),
                    Type = HolidayType.Religious,
                    Description = "Birthday of Prophet Muhammad (PBUH)",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });

                // Durga Puja (1 day from the 5-day festival)
                holidays.Add(new Holiday
                {
                    Name = "Durga Puja",
                    Date = new DateTime(2025, 10, 1),
                    Type = HolidayType.Religious,
                    Description = "Hindu festival celebrating Goddess Durga",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });
            }
            else if (year == 2026)
            {
                // For 2026, you'll need to update with accurate Islamic calendar dates
                // Add similar holidays with updated dates
            }

            await _context.Holidays.AddRangeAsync(holidays);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Holiday>> GetUpcomingHolidays(int count = 5)
        {
            return await Task.Run(() => 
                _context.Holidays
                    .Where(h => h.IsActive && h.Date >= DateTime.Today)
                    .OrderBy(h => h.Date)
                    .Take(count)
                    .ToList()
            );
        }

        public async Task<List<Holiday>> GetHolidaysForMonth(int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            return await Task.Run(() => 
                _context.Holidays
                    .Where(h => h.IsActive && h.Date >= startDate && h.Date <= endDate)
                    .OrderBy(h => h.Date)
                    .ToList()
            );
        }

        public async Task<bool> IsHoliday(DateTime date)
        {
            return await Task.Run(() =>
                _context.Holidays.Any(h => h.IsActive && h.Date.Date == date.Date)
            );
        }

        public async Task<Holiday?> GetHolidayByDate(DateTime date)
        {
            return await Task.Run(() =>
                _context.Holidays.FirstOrDefault(h => h.IsActive && h.Date.Date == date.Date)
            );
        }
    }
}
