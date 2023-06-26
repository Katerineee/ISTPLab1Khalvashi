using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FootBallWebLaba1.Models;
using System.Numerics;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.VariantTypes;

namespace FootBallWebLaba1.Controllers
{
    public class ClubsController : Controller
    {
        private readonly FootBallBdContext _context;

        public ClubsController(FootBallBdContext context)
        {
            _context = context;
        }

        // GET: Clubs
        public async Task<IActionResult> Index(string importSuccess)
        {
            ViewBag.importSuccess = importSuccess;
            return View(await _context.Clubs.ToListAsync());
        }

        // GET: Clubs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Clubs == null)
            {
                return NotFound();
            }

            var club = await _context.Clubs
                .FirstOrDefaultAsync(m => m.ClubId == id);
            if (club == null)
            {
                return NotFound();
            }

            return View(club);
        }

        public async Task<IActionResult> PlayersList(int? id)
        {
            if (id == null || _context.Clubs == null)
            {
                return NotFound();
            }

            var club = await _context.Clubs
                .FirstOrDefaultAsync(m => m.ClubId == id);
            if (club == null)
            {
                return NotFound();
            }

            return RedirectToAction("IndexPlayers", "Players", new { id = club.ClubId, name = club.ClubId });
        }

        // GET: Clubs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Clubs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClubId,ClubName,ClubOrigin,ClubPlayerQuantity,ClubCoachName,ClubEstablishmentDate")] Club club)
        {
            club.ClubPlayerQuantity = 0;
            if (ModelState.IsValid)
            {
                var clubName = _context.Clubs.FirstOrDefault(c => c.ClubName == club.ClubName);
                var clubCoach = _context.Clubs.FirstOrDefault(c => c.ClubCoachName == club.ClubCoachName);

                DateTime curDate = DateTime.Now;
                DateTime clubDate = club.ClubEstablishmentDate;

                if (clubDate > curDate)
                {
                    ModelState.AddModelError("ClubEstablishmentDate", "Дата створення клубу не відповідає дійсності");
                    return View(club);
                }

                if (clubCoach != null)
                {
                    ModelState.AddModelError("ClubCoachName", "Цей тренер уже тренує іншу команду");
                    return View(club);
                }

                if (clubName != null)
                {
                    ModelState.AddModelError("ClubName", "Команда з такою назвою уже існує");
                    return View(club);
                }

                _context.Add(club);
                await _context.SaveChangesAsync();
                return RedirectToAction("Create", "Stadiums", new { clubId = club.ClubId });
            }
            return View(club);
        }

        // GET: Clubs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Clubs == null)
            {
                return NotFound();
            }

            var club = await _context.Clubs.FindAsync(id);
            if (club == null)
            {
                return NotFound();
            }
            return View(club);
        }

        // POST: Clubs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ClubId,ClubName,ClubOrigin,ClubPlayerQuantity,ClubCoachName,ClubEstablishmentDate")] Club club)
        {
            if (id != club.ClubId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {

                var clubName = _context.Clubs.FirstOrDefault(c => c.ClubName == club.ClubName && c.ClubId != club.ClubId);
                var clubCoach = _context.Clubs.FirstOrDefault(c => c.ClubCoachName == club.ClubCoachName && c.ClubId != club.ClubId);

                if (clubCoach != null)
                {
                    ModelState.AddModelError("ClubCoachName", "Цей тренер уже тренує іншу команду");
                    return View(club);
                }

                if (clubName != null)
                {
                    ModelState.AddModelError("ClubName", "Команда з такою назвою уже існує");
                    return View(club);
                }

                try
                {
                    _context.Update(club);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClubExists(club.ClubId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(club);
        }

        // GET: Clubs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Clubs == null)
            {
                return NotFound();
            }

            var club = await _context.Clubs
                .FirstOrDefaultAsync(m => m.ClubId == id);
            if (club == null)
            {
                return NotFound();
            }

            return View(club);
        }

        // POST: Clubs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Clubs == null)
            {
                return Problem("Entity set 'FootBallBdContext.Clubs' is null.");
            }

            var club = await _context.Clubs
                .Include(c => c.Players)
                .Include(c => c.Stadiums)
                .FirstOrDefaultAsync(m => m.ClubId == id);

            if (club == null)
            {
                return NotFound();
            }

            var matches = await _context.Matches
                .Where(m => m.HostClubId == id || m.GuestClubId == id)
                .ToListAsync();

            foreach (var match in matches)
            {
                // Удаление связанных записей в таблице "ScoredGoal"
                var scoredGoals = await _context.ScoredGoals
                    .Where(s => s.MatchId == match.MatchId)
                    .ToListAsync();

                _context.ScoredGoals.RemoveRange(scoredGoals);

                _context.Matches.Remove(match);
            }

            foreach (var stadium in club.Stadiums)
            {
                // Удаление связанных записей в таблице "Stadium"
                var stadiumMatches = await _context.Matches
                    .Where(m => m.StadiumId == stadium.StadiumId)
                    .ToListAsync();

                foreach (var match in stadiumMatches)
                {
                    // Удаление связанных записей в таблице "ScoredGoal" для матчей на этом стадионе
                    var scoredGoals = await _context.ScoredGoals
                        .Where(s => s.MatchId == match.MatchId)
                        .ToListAsync();

                    _context.ScoredGoals.RemoveRange(scoredGoals);

                    _context.Matches.Remove(match);
                }

                _context.Remove(stadium);
            }

            foreach (var player in club.Players)
            {
                _context.Remove(player);
            }

            _context.Clubs.Remove(club);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        private bool ClubExists(int id)
        {
            return _context.Clubs.Any(e => e.ClubId == id);
        }

        public ActionResult ExportToExcel()
        {
            using (XLWorkbook workbook = new XLWorkbook())
            {
                // Отримати дані з бази даних
                var clubs = _context.Clubs
                    .Include(c => c.Players)
                    .ThenInclude(p => p.Position)
                    .Include(c => c.Stadiums).ToList();

                // Створити новий Excel 
                var worksheet = workbook.Worksheets.Add("Matches");

                // Додати заголовки стовпців
                worksheet.Cell(1, 1).Value = "ClubId";
                worksheet.Cell(1, 2).Value = "ClubName";
                worksheet.Cell(1, 3).Value = "ClubOrigin";
                worksheet.Cell(1, 4).Value = "ClubCoachName";
                worksheet.Cell(1, 5).Value = "ClubEstablishmentDate";
                worksheet.Cell(1, 6).Value = "StadiumLocation";
                worksheet.Cell(1, 7).Value = "StadiumCapacity";
                worksheet.Cell(1, 8).Value = "StadiumEstablismentDate";
                worksheet.Cell(1, 9).Value = "PlayerName";
                worksheet.Cell(1, 10).Value = "PlayerNumber";
                worksheet.Cell(1, 11).Value = "PositionName";
                worksheet.Cell(1, 12).Value = "PlayerSalary";
                worksheet.Cell(1, 13).Value = "PlayerBirthDate";

                // Додати дані з бази даних
                for (int i = 0; i < clubs.Count; i++)
                {
                    var club = clubs[i];
                    worksheet.Cell(i + 2, 1).Value = club.ClubId;
                    worksheet.Cell(i + 2, 2).Value = club.ClubName;
                    worksheet.Cell(i + 2, 3).Value = club.ClubOrigin;
                    worksheet.Cell(i + 2, 4).Value = club.ClubCoachName;
                    worksheet.Cell(i + 2, 5).Value = club.ClubEstablishmentDate.ToShortDateString();
                    worksheet.Cell(i + 2, 6).Value = string.Join(",",club.Stadiums.Select(s => s.StadiumLocation));
                    worksheet.Cell(i + 2, 7).Value = string.Join(",", club.Stadiums.Select(s => s.StadiumCapacity));
                    worksheet.Cell(i + 2, 8).Value = string.Join(",",club.Stadiums.Select(s => s.StadiumEstablismentDate.ToShortDateString()));
                    worksheet.Cell(i + 2, 9).Value = string.Join(",", club.Players.Select(p => p.PlayerName));
                    worksheet.Cell(i + 2, 10).Value = string.Join(",", club.Players.Select(p => p.PlayerNumber));
                    worksheet.Cell(i + 2, 11).Value = string.Join(",", club.Players.Select(p => p.Position.PositionName));
                    worksheet.Cell(i + 2, 12).Value = string.Join(";", club.Players.Select(p => p.PlayerSalary));
                    worksheet.Cell(i + 2, 13).Value = string.Join(",", club.Players.Select(p => p.PlayerBirthDate.ToShortDateString()));
                }

                // Зберегти файл Excel
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Flush();
                    return new FileContentResult(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                    {
                        FileDownloadName = $"clubs.xlsx"
                    };
                }
            }
        }


        public async Task<IActionResult> ImportFromExcel(IFormFile fileExel)
        {
            string importSuccess = "Файл завнтажено успішно. ";
            if (fileExel != null && fileExel.Length > 0)
            {
                using (var stream = fileExel.OpenReadStream())
                {
                    try
                    {
                        XLWorkbook workbook = new XLWorkbook(stream);
                    }
                    catch
                    {
                        return RedirectToAction("Index", new { importSuccess = "Формат файлу невірний" });
                    }
                    using (XLWorkbook workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var row = 2;
                        int failedAdd = 0;
                        var clubs = new List<Club>();
                        while (!worksheet.Cell(row, 1).IsEmpty())
                        {
                            var club = new Club();

                            // Отримати дані з рядка
                  
                            club.ClubName = worksheet.Cell(row, 2).GetValue<string>();
                            club.ClubOrigin = worksheet.Cell(row, 3).GetValue<string>();
                            club.ClubCoachName = worksheet.Cell(row, 4).GetValue<string>();
                            club.ClubEstablishmentDate = Convert.ToDateTime(worksheet.Cell(row, 5).GetValue<string>());

                            var clubCheck = _context.Clubs.FirstOrDefault(c => c.ClubName == club.ClubName || c.ClubOrigin == club.ClubOrigin || c.ClubCoachName == club.ClubCoachName);

                            if (clubCheck != null)
                            {
                                failedAdd++;
                                string clubProperty = string.Empty;
                                if (clubCheck.ClubName == club.ClubName) clubProperty += $"назва: {club.ClubName}";
                                if (clubCheck.ClubOrigin == club.ClubOrigin) clubProperty += string.Join(", ", $"походження: {club.ClubOrigin}");
                                if (clubCheck.ClubCoachName == club.ClubCoachName) clubProperty += string.Join(", ", $"тренер: {club.ClubCoachName}");
                                importSuccess += $"Команда з назвою {club.ClubName} не додана, через те що {clubProperty} уже зайняті іншими командами. ";
                            }

                            if(clubCheck == null)
                            clubs.Add(club);
                            row++;
                        }

                        // Зберегти дані в базі даних
                        await _context.Clubs.AddRangeAsync(clubs);
                        await _context.SaveChangesAsync();
                        row = 2 + failedAdd;
                        int clubCount = 0;
                        var stadiums = new List<Stadium>();
                        var players = new List<Player>();
                        while (!worksheet.Cell(row, 1).IsEmpty())
                        {
                            var stadium = new Stadium();

                            stadium.ClubId = clubs[clubCount].ClubId;
                            stadium.StadiumLocation = worksheet.Cell(row, 6).GetValue<string>();
                            stadium.StadiumCapacity = Convert.ToInt32(worksheet.Cell(row, 7).GetValue<string>());
                            stadium.StadiumEstablismentDate = Convert.ToDateTime(worksheet.Cell(row, 8).GetValue<string>());

                            var stadiumCheck = _context.Stadiums.FirstOrDefault(s => s.StadiumLocation == stadium.StadiumLocation);
                            if (stadiumCheck != null)
                            {
                                importSuccess += $"Команда {clubs[clubCount].ClubName} не була додана, тому що локація {stadium.StadiumLocation} зайнята. Змініть її і спробуйте додати команду ще раз. ";
                                _context.Clubs.Remove(clubs[clubCount]);
                                clubs.RemoveAt(clubCount);
                                await _context.SaveChangesAsync();
                                row++;
                                clubCount++;
                                continue;
                            }


                            var playerName = worksheet.Cell(row, 9).GetValue<string>();
                            var playerNumber = worksheet.Cell(row, 10).GetValue<string>();
                            var playerPosition = worksheet.Cell(row, 11).GetValue<string>();
                            var playerSalary = worksheet.Cell(row, 12).GetValue<string>();
                            var playerBirthDate = worksheet.Cell(row, 13).GetValue<string>();

                            var playerNameArray = playerName.Split(',');
                            var playerNumberArray = playerNumber.Split(',');
                            var playerPositionArray = playerPosition.Split(',');
                            var playerSalaryArray = playerSalary.Split(';');
                            var playerBirthDateArray = playerBirthDate.Split(',');

                            for(int i = 0; i< playerNameArray.Length; i++)
                            {
                                var player = new Player();
                                player.ClubId = clubs[clubCount].ClubId;
                                player.PlayerName = playerNameArray[i];
                                player.PlayerNumber = Convert.ToInt32(playerNumberArray[i]);
                                player.PositionId = _context.Positions.First(p => p.PositionName == playerPositionArray[i]).PositionId;
                                player.PlayerSalary = Convert.ToDecimal(playerSalaryArray[i]);
                                player.PlayerBirthDate = Convert.ToDateTime(playerBirthDateArray[i]);

                                var playerCheck = _context.Players.FirstOrDefault(p => p.PlayerName == player.PlayerName);
                                if (playerCheck != null)
                                    importSuccess += $"Гравець {player.PlayerName} команди {clubs[clubCount].ClubName} не був доданий, тому що таке гравець з таким іменем уже існує. ";

                                if (playerCheck == null)
                                {
                                    clubs[clubCount].ClubPlayerQuantity++;
                                    players.Add(player);
                                }
                            }
                            if(stadiumCheck == null)
                            stadiums.Add(stadium);
                            row++;
                            clubCount++;
                        }

                        _context.Clubs.UpdateRange(clubs);
                        await _context.SaveChangesAsync();

                        await _context.Stadiums.AddRangeAsync(stadiums);
                        await _context.SaveChangesAsync();

                        await _context.Players.AddRangeAsync(players);
                        await _context.SaveChangesAsync();

                    }
                }
            }

            if (fileExel == null) return RedirectToAction("Index", new { importSuccess = "Ви не вибрали файл для завантаженн" });
            if (fileExel.Length < 0) return RedirectToAction("Index", new { importSuccess = "Вибраний файл пустий, або містить хибну інформацію" });
            return RedirectToAction("Index", new { importSuccess = importSuccess });
        }
    } 
}

