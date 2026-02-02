using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Student_Management_System.Pages;

public class ChatModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string RoomId { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? Username { get; set; }

    public IActionResult OnGet()
    {
        if (string.IsNullOrEmpty(RoomId))
        {
            return RedirectToPage("/Index");
        }

        return Page();
    }
}
