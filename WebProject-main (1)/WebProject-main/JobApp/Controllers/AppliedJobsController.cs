using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using JobApp.Data;
using System.Linq;

namespace JobApp.Controllers
{
    [Authorize(Roles = "Employee")]
    public class AppliedJobsController : Controller
    {
        private readonly JobAppContext _context;
        public AppliedJobsController(JobAppContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> AppliedJobs()
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail)) return Unauthorized();
            var employee = await _context.Employees
                .Include(e => e.AppliedJobs!)
                .ThenInclude((System.Linq.Expressions.Expression<System.Func<JobApp.Models.JobDetails, JobApp.Models.Employer?>>)(j => j.Employer))
                .FirstOrDefaultAsync(e => e.Email == userEmail);
            if (employee == null) return Unauthorized();
            var jobs = employee.AppliedJobs ?? new List<JobApp.Models.JobDetails>();
            return View(jobs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int id)
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail)) return Unauthorized();
            var employee = await _context.Employees.Include(e => e.AppliedJobs).FirstOrDefaultAsync(e => e.Email == userEmail);
            if (employee == null) return Unauthorized();
            var job = await _context.JobDetails.Include(j => j.Employer).FirstOrDefaultAsync(j => j.Id == id);
            if (job == null) return NotFound();
            if (employee.AppliedJobs == null)
                employee.AppliedJobs = new List<JobApp.Models.JobDetails>();
            if (!employee.AppliedJobs.Any(j => j.Id == id))
            {
                employee.AppliedJobs.Add(job);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("AppliedJobs");
        }
    }
}
