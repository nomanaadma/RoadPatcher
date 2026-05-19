using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoadPatcher.Application.Data;
using RoadPatcher.Application.Entities;
using Stripe;
using RoadPatcher.Helpers;
using RoadPatcher.Models;

namespace RoadPatcher.Controllers;

[Route("[controller]")]
[ApiController]
public class DonationsController(
    ApplicationDbContext db,
    RoadPatcher.Services.INotificationService notificationService
) : Controller
{
	
	[HttpPost("CreatePaymentIntent")]
    [Authorize]
    public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
    {
        if (request.Amount <= 0)
            return BadRequest(new { error = "Invalid amount" });
        
        // Enforce server-side minimum donation amount: Rs.200
        const decimal minAmount = 200m;
        if (request.Amount < minAmount)
        {
	        var minAmountInCurrency = minAmount.ToPkr();
            return BadRequest(new { error = $"Minimum donation amount is {minAmountInCurrency}. Please increase the amount." });
        }

        var post = await db.Posts.FirstOrDefaultAsync(p => p.Id == request.PostId);
        if (post == null)
            return NotFound(new { error = "Post not found" });
        
        var remainingAmount = PostHelper.GetRemaining(post.Cost, post.Raised);

        if (request.Amount > remainingAmount)
        {
	        var remainingAmountInCurrency = remainingAmount.ToPkr();
	        return BadRequest(new { error = $"Maximum donation amount is {remainingAmountInCurrency} to achieve the goal amount" });
        }
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userData = await db.UserData.Include(ud => ud.User).FirstOrDefaultAsync(x => x.UserId == userId);

        var amount = (long)(request.Amount * 100);
        
        var url = $"{Request.Scheme}://{Request.Host}/{request.PostId}";
        
        var service = new PaymentIntentService();
        var options = new PaymentIntentCreateOptions
        {
            Amount = amount,
            Currency = "pkr",
            PaymentMethodTypes = ["card"],
            Description = '#'+request.PostId.ToString(),
            Metadata = new Dictionary<string, string>
            {
	            { "Url", url },
                { "Post", '#'+request.PostId.ToString() },
                { "User", UserHelper.GetFullName(userData!) + " - " + userData!.User!.Email},
                { "UserId", userId}
            }
        };

        try
        {
            var intent = await service.CreateAsync(options);

            return Ok(new CreatePaymentIntentResponse
            {
                ClientSecret = intent.ClientSecret,
                PaymentIntentId = intent.Id,
                Amount = request.Amount
            });
        }
        catch (StripeException sex)
        {
            // Return Stripe error message so frontend can surface it (e.g. amount conversion errors)
            var msg = sex.Message;
            return BadRequest(new { error = msg });
        }
        

    }
    
    [HttpPost("ConfirmDonation")]
    [Authorize]
    public async Task<IActionResult> ConfirmDonation([FromBody] ConfirmDonationRequest request)
    {
        if (string.IsNullOrEmpty(request.PaymentIntentId))
            return BadRequest(new { error = "paymentIntentId is required" });

        var service = new PaymentIntentService();
        var intent = await service.GetAsync(request.PaymentIntentId);

        if (intent == null)
            return NotFound(new { error = "PaymentIntent not found" });

        var postId = request.PostId;
        var post = await db.Posts.AsTracking().FirstOrDefaultAsync(p => p.Id == request.PostId);
        if (post == null)
	        return NotFound(new { error = "Post not found" });

        if (intent.Status != "succeeded")
        {
			return Ok(new { success = false, status = intent.Status });
        }
        
        // Idempotency: check if donation with this PaymentIntentId already exists
        var existing = await db.Donations.FirstOrDefaultAsync(d => d.PaymentIntentId == intent.Id);
        if (existing != null)
        {
	        // render partial for the post and return existing summary + receipt if any
	        var updatedPostValues = UpdatedPostValues(post, existing.Amount);
	        return Ok(updatedPostValues);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        
        var amountDecimal = intent.AmountReceived / 100m;

        var completed = false;

        // Persist donation and update post atomically
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var donation = new Donation
            {
                PostId = postId,
                UserId = userId,
                Amount = amountDecimal,
                PaymentIntentId = intent.Id,
                PaymentMethodId = intent.PaymentMethodId,
                Status = DonationStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                SucceededAt = DateTime.UtcNow
            };

            db.Donations.Add(donation);

            // Update Post.Raised
            post.Raised += amountDecimal;

            if (post.Cost == post.Raised)
            {
	            post.Status = PostStatus.InProcess;
                completed = true;
            }

            // Increase PeopleDonated only if this user hasn't donated successfully before for this post
            var hasDonated = await db.Donations
                .AnyAsync(d => d.PostId == postId && d.UserId == donation.UserId && d.Status == DonationStatus.Completed);

            if (!hasDonated)
            {
                post.PeopleDonated += 1;
            }

            db.Posts.Update(post);
            await db.SaveChangesAsync();

            var donator = await db.Users
                .Include(u => u.UserData)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var donatorName = donator == null
                ? "Someone"
                : UserHelper.GetDisplayName(donator);

            var postViewUrl = $"/Posts/{post.Id}";

            var notificationContent = completed
                ? $"Congratulations! Post #{post.Id} has been fully funded thanks to everyone. A team will now be arranged to start the patch work. <a href='{postViewUrl}'>View Post</a>"
                : $"{donatorName} Donated Rs. {amountDecimal.ToPkr()} to Post #{post.Id} <a href='{postViewUrl}'>View</a>";

            await notificationService.NotifyPostSubscribersAsync(
                postId,
                userId,
                notificationContent,
                "Donation");

            await tx.CommitAsync();
            
            var updatedPostValues = UpdatedPostValues(post, amountDecimal);
            return Ok(updatedPostValues);
            
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, new { error = ex.Message });
        }
        
    }

    private static object UpdatedPostValues(Post post, decimal amountDecimal)
    {
	    var raisedPercentage = PostHelper.GetRaisedPercentage(post.Cost, post.Raised);
	    var remainingAmount = PostHelper.GetRemaining(post.Cost, post.Raised).ToPkr();
	    var progressClass = PostHelper.GetProgressClass(raisedPercentage);
	    var tooltipBoxPercentage = PostHelper.GetTooltipBoxPercentage(raisedPercentage);
	    var cost = post.Cost.ToPkr();
	    var donatedAmount = amountDecimal.ToPkr();
	    
	    if (raisedPercentage % 1 == 0)
		    raisedPercentage = (int)raisedPercentage;

	    IHtmlContent btn = HtmlString.Empty;

	    // ReSharper disable once InvertIf
	    if (post.Status != PostStatus.Pending)
	    {
			var currentStatus = PostHelper.InvertStatus( (PostStatus)post.Status! );
			btn = PostHelper.RenderBtn(currentStatus, post.Id);
	    }
	    
	    return new
	    {
		    peopleDonated = post.PeopleDonated,
		    totalRaised = post.Raised.ToPkr(),
		    raisedPercentage,
		    remainingAmount,
		    progressClass,
		    tooltipBoxPercentage,
		    btn,
		    cost,
		    donatedAmount
	    };
    }
}
