using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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
public class DonationsController(ApplicationDbContext db, IConfiguration config) : ControllerBase
{

    [HttpPost("CreatePaymentIntent")]
    [Authorize]
    public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
    {
        if (request.Amount <= 0)
            return BadRequest(new { error = "Invalid amount" });
        
        // Enforce server-side minimum donation amount: Rs.200
        const decimal minPkr = 200m;
        if (request.Amount < minPkr)
            return BadRequest(new { error = $"Minimum donation amount is Rs. {minPkr:0}. Please increase the amount." });

        var post = await db.Posts.FirstOrDefaultAsync(p => p.Id == request.PostId);
        if (post == null)
            return NotFound(new { error = "Post not found" });
        
        var remainingAmount = PostHelper.GetRemaining(post.Cost, post.Raised);
        
        if (request.Amount > remainingAmount)
	        return BadRequest(new { error = $"Maximum donation amount is Rs. {remainingAmount:0} to achieve the goal amount" });
        
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
                { "User", ProfileHelper.GetFullName(userData!) + " - " + userData!.User!.Email},
                { "UserId", userId}
            }
        };

        var intent = await service.CreateAsync(options);

        return Ok(new CreatePaymentIntentResponse
        {
            ClientSecret = intent.ClientSecret,
            PaymentIntentId = intent.Id,
            Amount = request.Amount
        });
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
	        var raisedPercentageExisting = PostHelper.GetRaisedPercentage(post.Cost, post.Raised);
	        var remainingAmountExisting = PostHelper.GetRemaining(post.Cost, post.Raised);
	        var progressClassExisting = PostHelper.GetProgressClass(raisedPercentageExisting);
	        var tooltipBoxPercentageExisting = PostHelper.GetTooltipBoxPercentage(raisedPercentageExisting);

	        return Ok(new
	        {
		        peopleDonated = post.PeopleDonated,
		        totalRaised = post.Raised,
		        raisedPercentage = raisedPercentageExisting,
		        remainingAmount = remainingAmountExisting,
		        progressClass = progressClassExisting,
		        tooltipBoxPercentage = tooltipBoxPercentageExisting
	        });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        var amountDecimal = (decimal)intent.AmountReceived / 100m;

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
                ReceiptUrl = null,
                CreatedAt = DateTime.UtcNow,
                SucceededAt = DateTime.UtcNow
            };

            db.Donations.Add(donation);

            // Update Post.Raised
            post.Raised += amountDecimal;

            // Increase PeopleDonated only if this user hasn't donated successfully before for this post
            var hasDonated = await db.Donations
                .AnyAsync(d => d.PostId == postId && d.UserId == donation.UserId && d.Status == DonationStatus.Completed);

            if (!hasDonated)
            {
                post.PeopleDonated += 1;
            }

            db.Posts.Update(post);
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            // Prepare summary to return
            var raisedPercentage = PostHelper.GetRaisedPercentage(post.Cost, post.Raised);
            var remainingAmount = PostHelper.GetRemaining(post.Cost, post.Raised);
            var progressClass = PostHelper.GetProgressClass(raisedPercentage);
            var tooltipBoxPercentage = PostHelper.GetTooltipBoxPercentage(raisedPercentage);

            return Ok(new
            {
                peopleDonated = post.PeopleDonated,
                totalRaised = post.Raised,
                raisedPercentage,
                remainingAmount,
                progressClass,
                tooltipBoxPercentage
            });
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, new { error = ex.Message });
        }
        
    }

    [HttpPost("Webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var webhookSecret = config["Stripe:WebhookSecret"];

        var signature = Request.Headers["Stripe-Signature"];
        
        var stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);

        switch (stripeEvent.Type)
        {
	        case "payment_intent.succeeded":
	        {
		        if (stripeEvent.Data.Object is not PaymentIntent intent)
			        return BadRequest();

		        // Idempotent handling
		        var existing = await db.Donations.FirstOrDefaultAsync(d => d.PaymentIntentId == intent.Id);
		        if (existing is { Status: DonationStatus.Completed })
			        return Ok();

		        // Try to read metadata
		        intent.Metadata.TryGetValue("Post", out var postIdMeta);
		        intent.Metadata.TryGetValue("UserId", out var userIdMeta);

		        var postId = int.Parse(postIdMeta?.Replace("#", "")!);

		        var post = await db.Posts.AsTracking().FirstOrDefaultAsync(p => p.Id == postId);
		        if (post == null)
			        return NotFound();

		        var amountDecimal = intent.AmountReceived / 100m;

		        await using var tx = await db.Database.BeginTransactionAsync();
		        try
		        {
			        if (existing == null)
			        {
				        var donation = new Donation
				        {
					        PostId = postId,
					        UserId = userIdMeta,
					        Amount = amountDecimal,
					        PaymentIntentId = intent.Id,
					        PaymentMethodId = intent.PaymentMethodId,
					        Status = DonationStatus.Completed,
					        ReceiptUrl = null,
					        CreatedAt = DateTime.UtcNow,
					        SucceededAt = DateTime.UtcNow
				        };

				        db.Donations.Add(donation);

				        post.Raised += amountDecimal;

				        var hasDonated = await db.Donations
					        .AnyAsync(d => d.PostId == postId && d.UserId == donation.UserId && d.Status == DonationStatus.Completed);

				        if (!hasDonated)
				        {
					        post.PeopleDonated += 1;
				        }

				        db.Posts.Update(post);
				        await db.SaveChangesAsync();
			        }

			        await tx.CommitAsync();
		        }
		        catch
		        {
			        await tx.RollbackAsync();
		        }

		        return Ok();
	        }
	        case "payment_intent.payment_failed":
	        {
		        if (stripeEvent.Data.Object is not PaymentIntent intent)
			        return BadRequest();

		        var existing = await db.Donations.FirstOrDefaultAsync(d => d.PaymentIntentId == intent.Id);
		        if (existing == null) return Ok();
		        
		        existing.Status = DonationStatus.Failed;
		        existing.UpdatedAt = DateTime.UtcNow;
		        db.Donations.Update(existing);
		        await db.SaveChangesAsync();

		        return Ok();
	        }
	        default:
		        return Ok();
        }
    }
}
