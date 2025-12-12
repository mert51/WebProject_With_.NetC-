
namespace JobApp.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.AspNetCore.Authorization;
    using JobApp.Data;
    using JobApp.Models;
        
    
    public class JobPostController : Controller
    {
        private readonly JobAppContext _context;

        public JobPostController(JobAppContext context)
        {
            _context = context;
        }

        // GET: JobPost
        public async Task<IActionResult> Index(string jobSearchString, string jobType, string location, int? year, int page = 1, int pageSize = 5)
        {
            var jobsQuery = _context.JobDetails
                .Include(j => j.Employer)
                .AsQueryable();

            if (!string.IsNullOrEmpty(jobSearchString))
            {
                jobsQuery = jobsQuery.Where(j => j.Title!.Contains(jobSearchString) || j.Description!.Contains(jobSearchString) || j.Company!.Contains(jobSearchString));
            }

            ViewData["JobSearchString"] = jobSearchString;

            // years for dropdown
            ViewData["Years"] = await _context.JobDetails
                .Select(j => j.PostedDate.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();
            ViewData["Year"] = year;
            if (year.HasValue)
                jobsQuery = jobsQuery.Where(j => j.PostedDate.Year == year.Value);

            // job types and locations for dropdowns
            ViewData["JobTypes"] = await _context.JobDetails.Select(j => j.JobType).Distinct().OrderBy(t => t).ToListAsync();
            ViewData["Locations"] = await _context.JobDetails.Select(j => j.Location).Distinct().OrderBy(l => l).ToListAsync();
            ViewData["JobType"] = jobType;
            ViewData["Location"] = location;

            if (!string.IsNullOrEmpty(jobType))
                jobsQuery = jobsQuery.Where(j => j.JobType == jobType);

            if (!string.IsNullOrEmpty(location))
                jobsQuery = jobsQuery.Where(j => j.Location == location);

            var totalNumberOfJobs = await jobsQuery.CountAsync();
            var jobs = await jobsQuery
                .OrderBy(j => j.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling(totalNumberOfJobs / (double)pageSize);

            return View(jobs);
        }

        // GET: JobPost/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var job = await _context.JobDetails
                .Include(j => j.Employer)
                .Include(j => j.Employees)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null) return NotFound();

            return View(job);
        }


        // GET: JobPost/Post
        [Authorize(Roles = "Employer")]
        public IActionResult Post()
        {
            ViewData["EmployerId"] = new SelectList(_context.Employers.OrderBy(e => e.CompanyName), "Id", "CompanyName");
            return View(); // Post.cshtml view'ı döner
        }

        // GET: JobPost/Create
        [Authorize(Roles = "Employer")]
        public IActionResult Create()
        {
            ViewData["EmployerId"] = new SelectList(_context.Employers.OrderBy(e => e.CompanyName), "Id", "CompanyName");
            return View();
        }

        // POST: JobPost/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employer")]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,Company,Location,Salary,JobType,PostedDate")] JobDetails job, int? employerId)
        {
            if (ModelState.IsValid)
            {
                if (employerId.HasValue)
                {
                    var emp = await _context.Employers.FindAsync(employerId.Value);
                    job.Employer = emp;
                }
                job.PostedDate = job.PostedDate == default ? DateTime.Now : job.PostedDate;
                _context.Add(job);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["EmployerId"] = new SelectList(_context.Employers.OrderBy(e => e.CompanyName), "Id", "CompanyName", employerId);
            return View(job);
        }

        // GET: JobPost/Edit/5
        [Authorize(Roles = "Employer")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var job = await _context.JobDetails.Include(j => j.Employer).FirstOrDefaultAsync(j => j.Id == id);
            if (job == null) return NotFound();

            ViewData["EmployerId"] = new SelectList(_context.Employers.OrderBy(e => e.CompanyName), "Id", "CompanyName", job.Employer?.Id);
            return View(job);
        }

        // POST: JobPost/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employer")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Company,Location,Salary,JobType,PostedDate")] JobDetails job, int? employerId)
        {
            if (id != job.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (employerId.HasValue)
                    {
                        var emp = await _context.Employers.FindAsync(employerId.Value);
                        job.Employer = emp;
                    }
                    _context.Update(job);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!JobExists(job.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["EmployerId"] = new SelectList(_context.Employers.OrderBy(e => e.CompanyName), "Id", "CompanyName", employerId);
            return View(job);
        }

        // GET: JobPost/Delete/5
        [Authorize(Roles = "Employer")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var job = await _context.JobDetails
                .Include(j => j.Employer)
                .FirstOrDefaultAsync(j => j.Id == id);
            if (job == null) return NotFound();

            return View(job);
        }

        // POST: JobPost/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employer")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var job = await _context.JobDetails.FindAsync(id);
            if (job != null)
            {
                _context.JobDetails.Remove(job);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool JobExists(int id)
        {
            return _context.JobDetails.Any(e => e.Id == id);
        }
    }
}