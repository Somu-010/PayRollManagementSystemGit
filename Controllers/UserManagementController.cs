using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace PayRollManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserManagementController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: UserManagement - List all users
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userList = new List<UserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var linkedEmployee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == user.Id);

                userList.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    UserName = user.UserName ?? "",
                    Roles = roles.ToList(),
                    IsEmailConfirmed = user.EmailConfirmed,
                    LinkedEmployeeId = linkedEmployee?.EmployeeId,
                    LinkedEmployeeName = linkedEmployee?.Name,
                    LinkedEmployeeCode = linkedEmployee?.EmployeeCode
                });
            }

            return View(userList);
        }

        // GET: UserManagement/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var linkedEmployee = await _context.Employees
                .Include(e => e.DepartmentNavigation)
                .Include(e => e.DesignationNavigation)
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();

            var viewModel = new UserDetailsViewModel
            {
                Id = user.Id,
                Email = user.Email ?? "",
                UserName = user.UserName ?? "",
                Roles = roles.ToList(),
                AllRoles = allRoles,
                IsEmailConfirmed = user.EmailConfirmed,
                LinkedEmployee = linkedEmployee
            };

            // Get unlinked employees for dropdown
            ViewBag.UnlinkedEmployees = await _context.Employees
                .Where(e => string.IsNullOrEmpty(e.UserId) || e.UserId == user.Id)
                .OrderBy(e => e.Name)
                .ToListAsync();

            return View(viewModel);
        }

        // POST: UserManagement/UpdateRoles
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRoles(string userId, List<string> roles)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Get current roles
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Remove all current roles
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // Add selected roles
            if (roles != null && roles.Any())
            {
                await _userManager.AddToRolesAsync(user, roles);
            }

            TempData["Success"] = $"Roles updated successfully for {user.Email}";
            return RedirectToAction(nameof(Details), new { id = userId });
        }

        // POST: UserManagement/LinkEmployee
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkEmployee(string userId, int? employeeId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // First, unlink any previously linked employee
            var previousEmployee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == userId);
            if (previousEmployee != null)
            {
                previousEmployee.UserId = null;
            }

            // Link new employee if selected
            if (employeeId.HasValue && employeeId.Value > 0)
            {
                var employee = await _context.Employees.FindAsync(employeeId.Value);
                if (employee != null)
                {
                    employee.UserId = userId;

                    // Also ensure user has Employee role
                    if (!await _userManager.IsInRoleAsync(user, "Employee"))
                    {
                        await _userManager.AddToRoleAsync(user, "Employee");
                    }

                    TempData["Success"] = $"Successfully linked {user.Email} to employee {employee.Name}";
                }
            }
            else
            {
                // Remove Employee role if unlinking
                if (await _userManager.IsInRoleAsync(user, "Employee"))
                {
                    await _userManager.RemoveFromRoleAsync(user, "Employee");
                }
                TempData["Success"] = $"Successfully unlinked employee from {user.Email}";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = userId });
        }

        // POST: UserManagement/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Don't allow deleting the main admin
            if (user.Email == "admin@payrollpro.com")
            {
                TempData["Error"] = "Cannot delete the main admin account.";
                return RedirectToAction(nameof(Index));
            }

            // Unlink any associated employee
            var linkedEmployee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == id);
            if (linkedEmployee != null)
            {
                linkedEmployee.UserId = null;
                await _context.SaveChangesAsync();
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = $"User {user.Email} has been deleted.";
            }
            else
            {
                TempData["Error"] = "Failed to delete user.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: UserManagement/UnlinkedEmployees
        public async Task<IActionResult> UnlinkedEmployees()
        {
            var unlinkedEmployees = await _context.Employees
                .Include(e => e.DepartmentNavigation)
                .Include(e => e.DesignationNavigation)
                .Where(e => string.IsNullOrEmpty(e.UserId))
                .OrderBy(e => e.Name)
                .ToListAsync();

            return View(unlinkedEmployees);
        }

        // GET: UserManagement/CreateUserForEmployee/5
        public async Task<IActionResult> CreateUserForEmployee(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.DepartmentNavigation)
                .Include(e => e.DesignationNavigation)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(employee.UserId))
            {
                TempData["Error"] = "This employee already has a linked user account.";
                return RedirectToAction(nameof(UnlinkedEmployees));
            }

            var viewModel = new CreateUserForEmployeeViewModel
            {
                EmployeeId = employee.EmployeeId,
                EmployeeName = employee.Name,
                EmployeeCode = employee.EmployeeCode,
                Email = employee.Email,
                Department = employee.DepartmentNavigation?.Name ?? employee.Department,
                Designation = employee.DesignationNavigation?.Title ?? employee.Designation
            };

            return View(viewModel);
        }

        // POST: UserManagement/CreateUserForEmployee
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUserForEmployee(CreateUserForEmployeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var employee = await _context.Employees.FindAsync(model.EmployeeId);
            if (employee == null)
            {
                return NotFound();
            }

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "A user with this email already exists.");
                return View(model);
            }

            // Create new user
            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Link to employee
                employee.UserId = user.Id;
                await _context.SaveChangesAsync();

                // Add Employee role
                await _userManager.AddToRoleAsync(user, "Employee");

                TempData["Success"] = $"User account created and linked to {employee.Name}. Password: {model.Password}";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }
    }

    // View Models
    public class UserViewModel
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string UserName { get; set; } = "";
        public List<string> Roles { get; set; } = new();
        public bool IsEmailConfirmed { get; set; }
        public int? LinkedEmployeeId { get; set; }
        public string? LinkedEmployeeName { get; set; }
        public string? LinkedEmployeeCode { get; set; }
    }

    public class UserDetailsViewModel
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string UserName { get; set; } = "";
        public List<string> Roles { get; set; } = new();
        public List<string> AllRoles { get; set; } = new();
        public bool IsEmailConfirmed { get; set; }
        public Employee? LinkedEmployee { get; set; }
    }

    public class CreateUserForEmployeeViewModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = "";
        public string EmployeeCode { get; set; } = "";
        public string Department { get; set; } = "";
        public string Designation { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }
}
