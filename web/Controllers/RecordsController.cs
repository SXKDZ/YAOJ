using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAOJ.Data;
using YAOJ.Models;

namespace YAOJ.Controllers
{
    public class JudgeRequest
    {
        public int userID;
        public int recordID;
        public string problemID;
        public string language;
        public string sourceCode;
        public string dataHash;

        public static JudgeRequest CreateJudgeRequest(Record record)
        {
            return new JudgeRequest
            {
                recordID = record.RecordID,
                problemID = record.ProblemID,
                language = record.Language,
                userID = record.UserID,
                dataHash = record.Problem.DataHash,
                sourceCode = record.SourceCode
            };
        }
        
    }

    public class RecordsController : Controller
    {
        public void SendJudgeRequest(Record record)
        {
            var factory = new ConnectionFactory()
            {
                HostName = _rabbitMQConfig["Server"],
                UserName = _rabbitMQConfig["Username"],
                Password = _rabbitMQConfig["Password"],
                Port = int.Parse(_rabbitMQConfig["Port"])
            };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "yaoj_queue", durable: true, exclusive: false,
                    autoDelete: false, arguments: null);

                var request = JudgeRequest.CreateJudgeRequest(record);
                var jsonifiedMessage = JsonConvert.SerializeObject(request);
                var body = Encoding.UTF8.GetBytes(jsonifiedMessage);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                channel.BasicPublish(exchange: "", routingKey: "yaoj_queue",
                    basicProperties: properties, body: body);
            }
        }

        private readonly DataContext _context;
        private readonly IConfiguration _configuration;
        private readonly Dictionary<string, string> _rabbitMQConfig;

        public RecordsController(DataContext context, IConfiguration configuration,
             Dictionary<string, string> rabbitMQConfig)
        {
            _context = context;
            _configuration = configuration;
            _rabbitMQConfig = rabbitMQConfig;
        }

        // GET: Records
        public async Task<IActionResult> Index(string searchProblemID,
            string currentFilter, int? page)
        {
            ViewData["CurrentFilter"] = searchProblemID;

            if (searchProblemID != null)
            {
                page = 1;
            }
            else
            {
                searchProblemID = currentFilter;
            }

            var records = from s in _context.Records select s;
            if (!String.IsNullOrEmpty(searchProblemID))
            {
                records = records.Where(s => s.ProblemID == searchProblemID);
            }
            records = records.Include(s => s.Problem).Include(s => s.User)
                .OrderByDescending(i => i.RecordID);

            int pageSize = 10;

            return View(await PaginatedList<Record>.CreateAsync(records.AsNoTracking(), page ?? 1, pageSize));
        }

        // GET: Records/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var record = await _context.Records
                .Include(r => r.Problem)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.RecordID == id);
            if (record == null)
            {
                return NotFound();
            }

            return View(record);
        }

        // GET: Records/Create/P1000
        public IActionResult Create(string id)
        {
            ViewData["ProblemID"] = id;
            return View();
        }

        // POST: Records/Create/P1000
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string id,
            [Bind("Language,SourceCode")] Record record)
        {
            if (ModelState.IsValid)
            {
                record.ProblemID = id;
                record.Problem = _context.Problems.First(i => i.ProblemID == id);
                record.UserID = 1;
                record.Status = Status.NA;
                record.UsedMemory = 0;
                record.UsedTime = 0;
            
                _context.Records.Add(record);
                await _context.SaveChangesAsync();
                SendJudgeRequest(record);
                return RedirectToAction(nameof(Index));
            }
            return View(record);
        }
        
        // GET: Records/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var record = await _context.Records
                .Include(r => r.Problem)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.RecordID == id);
            if (record == null)
            {
                return NotFound();
            }

            return View(record);
        }

        // POST: Records/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var record = await _context.Records.FindAsync(id);
            _context.Records.Remove(record);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Records/Update/5
        [HttpPost]
        public async Task<IActionResult> Update(int id, string text, string status,
            string usedTime, string usedMemory)
        {
            var record = await _context.Records
                .Include(r => r.Problem)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.RecordID == id);
            if (record == null)
            {
                return NotFound();
            }

            record.Status = (Status) Enum.Parse(typeof(Status), status);
            record.JudgeText = text;
            record.UsedTime = double.Parse(usedTime);
            record.UsedMemory = int.Parse(usedMemory);

            _context.Update(record);
            await _context.SaveChangesAsync();
            return Content("Succeeded!");
        }

        private bool RecordExists(int id)
        {
            return _context.Records.Any(e => e.RecordID == id);
        }
    }
}
