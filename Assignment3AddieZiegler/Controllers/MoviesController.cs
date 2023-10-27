using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Assignment3AddieZiegler.Data;
using Assignment3AddieZiegler.Models;
using System.Numerics;
using System.Net;
using System.Text.Json;
using System.Web;
using VaderSharp2;

namespace Assignment3AddieZiegler.Controllers
{
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MoviesController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> GetMoviePoster(int id)
        {
            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                return NotFound();
            }
            var imageData = movie.Poster;
            return File(imageData, "image/jpg");
        }
        // GET: Movies
        public async Task<IActionResult> Index()
        {
              return _context.Movie != null ? 
                          View(await _context.Movie.ToListAsync()) :
                          Problem("Entity set 'ApplicationDbContext.Movie'  is null.");
        }

        // GET: Movies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Movie == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                return NotFound();
            }
            var queryText = movie.Title;
            var posts = RedditResults(queryText);

            var Sentiment = CalculateOverallSentiment(posts, queryText);

            var viewModel = new RedditPostVM
            {
                Movie = movie,
                Sentiment = Sentiment,
                RedditResults = posts
            };

            return View(movie);
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


        // GET: Movies/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Movies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,IMDBUrl,Genre,YearOfRelease,Poster")] Movie movie, IFormFile Poster)
        {
            if (ModelState.IsValid)
            {
                if (Poster != null && Poster.Length > 0)
                {
                    var memoryStream = new MemoryStream();
                    await Poster.CopyToAsync(memoryStream);
                    movie.Poster = memoryStream.ToArray();
                }
                _context.Add(movie);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        // GET: Movies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Movie == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie.FindAsync(id);
            if (movie == null)
            {
                return NotFound();
            }
            return View(movie);
        }

        // POST: Movies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,IMDBUrl,Genre,YearOfRelease,Poster")] Movie movie)
        {
            if (id != movie.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(movie);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MovieExists(movie.Id))
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
            return View(movie);
        }

        // GET: Movies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Movie == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        // POST: Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Movie == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Movie'  is null.");
            }
            var movie = await _context.Movie.FindAsync(id);
            if (movie != null)
            {
                _context.Movie.Remove(movie);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MovieExists(int id)
        {
          return (_context.Movie?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
