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
        var userData = await db.UserData.FirstOrDefaultAsync(x => x.UserId == userId);

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
                { "User", ProfileHelper.GetFullName(userData!) + " - " + userId }
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

        // If succeeded, persist donation
        if (intent.Status == "succeeded")
        {
            // Idempotency: check if donation with this PaymentIntentId already exists
            var existing = await db.Donations.FirstOrDefaultAsync(d => d.PaymentIntentId == intent.Id);
            if (existing != null)
            {
                // Return current post summary if possible
                var existingDonation = existing;
                var postForExisting = await db.Posts.FirstOrDefaultAsync(p => p.Id == existingDonation.PostId);
                if (postForExisting != null)
                {
                    var raisedPercentageExisting = PostHelper.GetRaisedPercentage(postForExisting.Cost, postForExisting.Raised);
                    var remainingAmountExisting = PostHelper.GetRemaining(postForExisting.Cost, postForExisting.Raised);
                    var progressClassExisting = PostHelper.GetProgressClass(raisedPercentageExisting);
                    var tooltipBoxPercentageExisting = PostHelper.GetTooltipBoxPercentage(raisedPercentageExisting);

                    return Ok(new
                    {
                        peopleDonated = postForExisting.PeopleDonated,
                        totalRaised = postForExisting.Raised,
                        raisedPercentage = raisedPercentageExisting,
                        remainingAmount = remainingAmountExisting,
                        progressClass = progressClassExisting,
                        tooltipBoxPercentage = tooltipBoxPercentageExisting
                    });
                }

                return Ok(new { success = true, message = "Already recorded" });
            }

            // Read metadata
            intent.Metadata.TryGetValue("postId", out var postIdMeta);
            intent.Metadata.TryGetValue("userId", out var userIdMeta);

            if (!int.TryParse(postIdMeta, out var postId))
            {
                return BadRequest(new { error = "PaymentIntent missing postId metadata" });
            }

            var post = await db.Posts.FirstOrDefaultAsync(p => p.Id == postId);
            if (post == null)
                return NotFound(new { error = "Post not found2" });

            var amountDecimal = (decimal)intent.AmountReceived / 100m;

            // Persist donation and update post atomically
            using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                var donation = new Donation
                {
                    PostId = postId,
                    UserId = string.IsNullOrWhiteSpace(userIdMeta) ? null : userIdMeta,
                    Amount = amountDecimal,
                    PaymentIntentId = intent.Id,
                    PaymentMethodId = intent.PaymentMethodId,
                    Status = DonationStatus.Completed,
                    CardLast4 = null,
                    ReceiptUrl = null,
                    CreatedAt = DateTime.UtcNow,
                    SucceededAt = DateTime.UtcNow
                };

                db.Donations.Add(donation);

                // Update Post.Raised
                post.Raised += amountDecimal;

                // Increase PeopleDonated only if this user hasn't donated successfully before for this post
                if (!string.IsNullOrWhiteSpace(donation.UserId))
                {
                    var hasDonated = await db.Donations
                        .AnyAsync(d => d.PostId == postId && d.UserId == donation.UserId && d.Status == DonationStatus.Completed);

                    if (!hasDonated)
                    {
                        post.PeopleDonated += 1;
                    }
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

        return Ok(new { success = false, status = intent.Status });
    }

    [HttpPost("Webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var webhookSecret = config["Stripe:WebhookSecret"];

        Event stripeEvent;

        if (!string.IsNullOrEmpty(webhookSecret))
        {
            var signature = Request.Headers["Stripe-Signature"];
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }
        else
        {
            stripeEvent = EventUtility.ParseEvent(json);
        }

        if (stripeEvent.Type == "payment_intent.succeeded")
        {
            var intent = stripeEvent.Data.Object as PaymentIntent;
            if (intent is null)
                return BadRequest();

            // Idempotent handling
            var existing = await db.Donations.FirstOrDefaultAsync(d => d.PaymentIntentId == intent.Id);
            if (existing != null && existing.Status == DonationStatus.Completed)
                return Ok();

            // Try to read metadata
            intent.Metadata.TryGetValue("postId", out var postIdMeta);
            intent.Metadata.TryGetValue("userId", out var userIdMeta);

            if (!int.TryParse(postIdMeta, out var postId))
            {
                // Can't associate to post - ignore
                return Ok();
            }

            var post = await db.Posts.FirstOrDefaultAsync(p => p.Id == postId);
            if (post == null)
                return Ok();

            var amountDecimal = (decimal)intent.AmountReceived / 100m;

            using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                // Re-check idempotency inside tx
                var existing2 = await db.Donations.FirstOrDefaultAsync(d => d.PaymentIntentId == intent.Id);
                if (existing2 == null)
                {
                    var donation = new Donation
                    {
                        PostId = postId,
                        UserId = string.IsNullOrWhiteSpace(userIdMeta) ? null : userIdMeta,
                        Amount = amountDecimal,
                        PaymentIntentId = intent.Id,
                        PaymentMethodId = intent.PaymentMethodId,
                        Status = DonationStatus.Completed,
                        CardLast4 = null,
                        ReceiptUrl = null,
                        CreatedAt = DateTime.UtcNow,
                        SucceededAt = DateTime.UtcNow
                    };

                    db.Donations.Add(donation);

                    post.Raised += amountDecimal;

                    if (!string.IsNullOrWhiteSpace(donation.UserId))
                    {
                        var hasDonated = await db.Donations
                            .AnyAsync(d => d.PostId == postId && d.UserId == donation.UserId && d.Status == DonationStatus.Completed);

                        if (!hasDonated)
                        {
                            post.PeopleDonated += 1;
                        }
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
        else if (stripeEvent.Type == "payment_intent.payment_failed")
        {
            var intent = stripeEvent.Data.Object as PaymentIntent;
            if (intent is null)
                return BadRequest();

            var existing = await db.Donations.FirstOrDefaultAsync(d => d.PaymentIntentId == intent.Id);
            if (existing != null)
            {
                existing.Status = DonationStatus.Failed;
                existing.UpdatedAt = DateTime.UtcNow;
                db.Donations.Update(existing);
                await db.SaveChangesAsync();
            }

            return Ok();
        }

        return Ok();
    }
}
