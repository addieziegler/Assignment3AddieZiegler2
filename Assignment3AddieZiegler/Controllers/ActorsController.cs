using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Assignment3AddieZiegler.Data;
using Assignment3AddieZiegler.Models;
using System.Net;
using System.Text.Json;
using System.Web;
using VaderSharp2;
using Reddit;

namespace Assignment3AddieZiegler.Controllers
{
    public class ActorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ActorsController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> GetActorPhoto(int id)
        {
            var actor = await _context.Actor
                .FirstOrDefaultAsync(m => m.Id == id);
            if (actor == null)
            {
                return NotFound();
            }
            var imageData = actor.Photo;
            return File(imageData, "image/jpg");
        }
        // GET: Actors
        public async Task<IActionResult> Index()
        {
              return _context.Actor != null ? 
                          View(await _context.Actor.ToListAsync()) :
                          Problem("Entity set 'ApplicationDbContext.Actor'  is null.");
        }

        // GET: Actors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Actor == null)
            {
                return NotFound();
            }

            var actor = await _context.Actor
                .FirstOrDefaultAsync(m => m.Id == id);
            if (actor == null)
            {
                return NotFound();
            }
            var queryText = actor.Name;
            var posts = RedditResults(queryText);

            var Sentiment = CalculateOverallSentiment(posts, queryText);

            var viewModel = new RedditPostVM
            {
                Actor = actor,
                Sentiment = Sentiment,
                RedditResults = posts
            };

            return View(viewModel);
        }
        public List<RedditPost> RedditResults(string queryText)
        {
            var json = "";

            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                json = wc.DownloadString("https://www.reddit.com/search.json?limit=100&q=" + HttpUtility.UrlEncode(queryText));
            }

            var redditPosts = new List<RedditPost>(); // Initialize the list

            var textToExamine = new List<string>();
            JsonDocument doc = JsonDocument.Parse(json);

            // Navigate to the "data" object
            JsonElement dataElement = doc.RootElement.GetProperty("data");

            // Navigate to the "children" array
            JsonElement childrenElement = dataElement.GetProperty("children");

            var analyzer = new SentimentIntensityAnalyzer();

            Double postsTotal = 0;
            int nonZeroCount = 0;

            foreach (JsonElement child in childrenElement.EnumerateArray())
            {
                if (child.TryGetProperty("data", out JsonElement data))
                {
                    var uploadText = "";

                    if (data.TryGetProperty("selftext", out JsonElement selftext))
                    {
                        string selftextValue = selftext.GetString();
                        if (!string.IsNullOrEmpty(selftextValue))
                        {
                            textToExamine.Add(selftextValue);
                            uploadText = selftextValue;
                        }
                        else if (data.TryGetProperty("title", out JsonElement title)) // use title if text is empty
                        {
                            string titleValue = title.GetString();
                            if (!string.IsNullOrEmpty(titleValue))
                            {
                                textToExamine.Add(titleValue);
                                uploadText = titleValue;
                            }
                        }

                        if (!string.IsNullOrEmpty(uploadText))
                        {
                            var results = analyzer.PolarityScores(uploadText);
                            var score = results.Compound;
                            postsTotal += score;
                            if (score != 0) nonZeroCount++;

                            var PosNegNeu = score < 0 ? "Negative" : (score > 0 ? "Positive" : "Neutral");

                            if (score != 0)
                            {
                                var upload = new RedditPost
                                {
                                    Text = uploadText,
                                    CompoundScore = score,
                                    Sentiment = PosNegNeu,
                                    PercentScore = Math.Round(score * 100)
                                };
                                redditPosts.Add(upload);
                            }

                        }
                    }
                }
            }

            return redditPosts;
        }

        public string CalculateOverallSentiment(List<RedditPost> posts, string queryText)
        {
            double postsTotal = posts.Sum(post => post.CompoundScore);
            int nonZeroCount = posts.Count(post => post.CompoundScore != 0);

            var totalScore = postsTotal / nonZeroCount;
            var totalPercent = Math.Round(totalScore * 100);
            var overallSentiment = totalScore < 0 ? "Negative" : (totalScore > 0 ? "Positive" : "Neutral");

            return "Overall, Reddit shows that " + queryText + " was " + totalPercent + "%" + overallSentiment;
        }

        // GET: Actors/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Actors/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Gender,Age,IMBDUrl,Photo")] Actor actor, IFormFile Photo)
        {
            if (ModelState.IsValid)
            {
                if (Photo != null && Photo.Length > 0)
                {
                    var memoryStream = new MemoryStream();
                    await Photo.CopyToAsync(memoryStream);
                    actor.Photo = memoryStream.ToArray();
                }
                _context.Add(actor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(actor);
        }

        // GET: Actors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Actor == null)
            {
                return NotFound();
            }

            var actor = await _context.Actor.FindAsync(id);
            if (actor == null)
            {
                return NotFound();
            }
            return View(actor);
        }

        // POST: Actors/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Gender,Age,IMBDUrl,Photo")] Actor actor, IFormFile Photo)
        {
            if (id != actor.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(actor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ActorExists(actor.Id))
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
            return View(actor);
        }

        // GET: Actors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Actor == null)
            {
                return NotFound();
            }

            var actor = await _context.Actor
                .FirstOrDefaultAsync(m => m.Id == id);
            if (actor == null)
            {
                return NotFound();
            }

            return View(actor);
        }

        // POST: Actors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Actor == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Actor'  is null.");
            }
            var actor = await _context.Actor.FindAsync(id);
            if (actor != null)
            {
                _context.Actor.Remove(actor);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ActorExists(int id)
        {
          return (_context.Actor?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
