using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using YAOJ.Data;
using YAOJ.Models;

namespace YAOJ.Controllers
{
    public class ProblemsController : Controller
    {
        static string GetMd5Hash(byte[] data)
        {
            var sb = new StringBuilder();
            using (var md5Hash = MD5.Create())
            {
                var hashData = md5Hash.ComputeHash(data);
                for (var i = 0; i < hashData.Length; ++i)
                {
                    sb.Append(hashData[i].ToString("x2"));
                }
            }
            return sb.ToString();
        }

        private readonly DataContext _context;
        private readonly IConfiguration _configuration;

        public ProblemsController(DataContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: Problems
        public async Task<IActionResult> Index(int? page)
        {
            int pageSize = 10;

            return View(await PaginatedList<Problem>.CreateAsync(_context.Problems.AsNoTracking(),
                page ?? 1, pageSize));
        }

        // GET: Problems/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var problem = await _context.Problems
                .FirstOrDefaultAsync(m => m.ProblemID == id);
            if (problem == null)
            {
                return NotFound();
            }

            return View(problem);
        }

        // GET: Problems/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Problems/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Description,Format,SampleInput,SampleOutput,Note,Limitation")] Problem problem,
            IFormFile data)
        {
            if (data == null)
            {
                ModelState.AddModelError("Data", "Problem data can not be empty.");
            }
            if (ModelState.IsValid)
            {
                string defaultProblemID = "P1000";
                if (!_context.Problems.Any())
                {
                    problem.ProblemID = defaultProblemID;
                }
                else
                {
                    var lastProblemID = _context.Problems.OrderByDescending(p => p.ProblemID)
                        .First().ProblemID.Substring(1);
                    var thisProblemID = int.Parse(lastProblemID) + 1;
                    problem.ProblemID = "P" + thisProblemID.ToString();
                }
                using (var ms = new MemoryStream())
                {
                    var ct = data.CopyToAsync(ms);
                    await ct;
                    problem.Data = ms.ToArray();
                }
                problem.DataHash = GetMd5Hash(problem.Data);
                _context.Add(problem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(problem);
        }

        // GET: Problems/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var problem = await _context.Problems.FindAsync(id);
            if (problem == null)
            {
                return NotFound();
            }
            return View(problem);
        }

        // POST: Problems/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id,
            [Bind("ProblemID,Name,Description,Format,SampleInput,SampleOutput,Note,Limitation")] Problem problem,
            IFormFile data)
        {
            if (id != problem.ProblemID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (data != null)
                    {
                        using (var ms = new MemoryStream())
                        {
                            var ct = data.CopyToAsync(ms);
                            await ct;
                            problem.Data = ms.ToArray();
                            problem.DataHash = GetMd5Hash(problem.Data);
                        }
                    }
                    else
                    {
                        _context.Problems.Attach(problem);
                        var entry = _context.Entry(problem);
                        entry.Property(e => e.Data).IsModified = false;
                    }
                    _context.Update(problem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProblemExists(problem.ProblemID))
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
            return View(problem);
        }

        // GET: Problems/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var problem = await _context.Problems
                .FirstOrDefaultAsync(m => m.ProblemID == id);
            if (problem == null)
            {
                return NotFound();
            }

            return View(problem);
        }

        // POST: Problems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var problem = await _context.Problems.FindAsync(id);
            _context.Problems.Remove(problem);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProblemExists(string id)
        {
            return _context.Problems.Any(e => e.ProblemID == id);
        }

        // GET: Problems/DownloadDataset/5
        public async Task<IActionResult> DownloadDataset(string id, string key)
        {
            var judgeSettings = _configuration.GetSection("Judge").GetChildren();
            foreach (var section in judgeSettings)
            {
                var judgeKey = section.GetValue<string>("Key");
                if (judgeKey == key)
                {
                    var problem = await _context.Problems.FindAsync(id);
                    return File(problem.Data, System.Net.Mime.MediaTypeNames.Application.Zip, $"{id}.zip");
                }
            }
            return Unauthorized();
        }
    }
}
