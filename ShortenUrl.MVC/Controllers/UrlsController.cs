using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shortenUrl.MVC.Data;
using shortenUrl.MVC.Data.Entities;
using shortenUrl.MVC.Models;
using QRCoder;
using System.Drawing;
using System.IO;

namespace shortenUrl.MVC.Controllers
{
    public class UrlsController : Controller
    {
        private readonly ShortenUrlDbContext _context;
        private readonly string _baseUrl;

        public UrlsController(ShortenUrlDbContext context, IConfiguration config)
        {
            _context = context;
            _baseUrl = config["AppSettings:BaseUrl"] ?? "https://localhost:5001/";
        }

        // GET: Urls
        public async Task<IActionResult> Index()
        {
            var urls = await _context.Urls.ToListAsync();
            return View(urls);
        }

        // GET: Urls/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var url = await _context.Urls.FirstOrDefaultAsync(m => m.Id == id);
            if (url == null)
                return NotFound();

            //Generate QR code from OriginalUrl
            var qrTargetUrl = url.OriginalUrl;

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrTargetUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCode(qrCodeData);
            using var bitmap = qrCode.GetGraphic(20);

            using var stream = new MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            var qrImageBase64 = Convert.ToBase64String(stream.ToArray());

            ViewBag.QrCode = $"data:image/png;base64,{qrImageBase64}";

            return View(url);
        }

        // GET: Urls/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Urls/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UrlVM urlVM)
        {
            if (!ModelState.IsValid)
                return View(urlVM);

            string shortCode;

            if (!string.IsNullOrWhiteSpace(urlVM.ShortenedUrl))
            {
                shortCode = urlVM.ShortenedUrl.Trim();

                if (await _context.Urls.AnyAsync(u => u.ShortenedUrl == shortCode))
                {
                    ModelState.AddModelError("ShortenedUrl", "The shortened Url code is already exist. Please choose another one.");
                    return View(urlVM);
                }
            }
            else
            {
                shortCode = GenerateShortCode();
                while (await _context.Urls.AnyAsync(u => u.ShortenedUrl == shortCode))
                {
                    shortCode = GenerateShortCode();
                }
            }

            var url = new Url
            {
                OriginalUrl = urlVM.OriginalUrl,
                ShortenedUrl = shortCode
            };

            _context.Add(url);
            await _context.SaveChangesAsync();

            return RedirectToAction("Success", new { shortCode });
        }

        
        public async Task<IActionResult> Success(string shortCode)
        {
            if (string.IsNullOrEmpty(shortCode))
                return NotFound();

            var url = await _context.Urls.FirstOrDefaultAsync(u => u.ShortenedUrl == shortCode);
            if (url == null)
                return NotFound();

            var fullUrl = url.OriginalUrl;
            var shortUrl = $"{Request.Scheme}://{Request.Host}/u/{shortCode}";

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(fullUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCode(qrCodeData);
            using var bitmap = qrCode.GetGraphic(20);

            using var stream = new MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            var qrImageBase64 = Convert.ToBase64String(stream.ToArray());

            ViewBag.ShortUrl = shortUrl;
            ViewBag.QrCode = $"data:image/png;base64,{qrImageBase64}";

            return View(url);
        }

        // GET: Urls/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var url = await _context.Urls.FindAsync(id);
            if (url == null)
                return NotFound();

            return View(url);
        }

        // POST: Urls/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UrlVM urlVM)
        {
            if (id != urlVM.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                var model = await _context.Urls.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
                if (model == null) return NotFound();
                model.OriginalUrl = urlVM.OriginalUrl;
                model.ShortenedUrl = urlVM.ShortenedUrl;
                return View(model);
            }

            var existing = await _context.Urls.FirstOrDefaultAsync(u => u.Id == id);
            if (existing == null)
                return NotFound();

            existing.OriginalUrl = urlVM.OriginalUrl;
            existing.ShortenedUrl = urlVM.ShortenedUrl;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UrlExists(id))
                    return NotFound();
                else
                    throw;
            }

            return RedirectToAction(nameof(Details), new { id = existing.Id });
        }

        // GET: Urls/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var url = await _context.Urls.FirstOrDefaultAsync(m => m.Id == id);
            if (url == null)
                return NotFound();

            return View(url);
        }

        // POST: Urls/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var url = await _context.Urls.FindAsync(id);
            if (url != null)
            {
                _context.Urls.Remove(url);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: u/{shortCode}
        [HttpGet("u/{shortCode}")]
        public async Task<IActionResult> RedirectToOriginal(string shortCode)
        {
            if (string.IsNullOrEmpty(shortCode))
                return NotFound();

            var url = await _context.Urls.FirstOrDefaultAsync(u => u.ShortenedUrl == shortCode);
            if (url == null)
                return NotFound();

            return Redirect(url.OriginalUrl);
        }

        private string GenerateShortCode()
        {
            var chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 5)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private bool UrlExists(int id)
        {
            return _context.Urls.Any(e => e.Id == id);
        }
    }
}
