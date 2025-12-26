$(document).ready(function() {
    
    new Swiper(".postSlider", {
        // Optional: Loop the slides
        loop: false,

        // Adjust height based on content
        autoHeight: true,

        // Navigation arrows
        navigation: {
            nextEl: ".swiper-button-next",
            prevEl: ".swiper-button-prev",
        },

        pagination: {
            el: ".swiper-pagination",
            type: "progressbar",
        },

        // Prevent video from playing when sliding away (Optional UX improvement)
        on: {
            slideChange: function () {
                document.querySelectorAll('video').forEach(vid => vid.pause());
            }
        }
    });

    $('.zoomBtn').on('click', function() {

        const swiper = $(this).next()[0].swiper;
        const currentSlide = $(swiper.slides[swiper.activeIndex]);
        const currentSliderFullScreenAnchor = currentSlide.prev('a');
        currentSliderFullScreenAnchor.trigger('click');

    });

    $('.tooltip-loading').each(function () {

        const $tooltip = $(this);
        const $inner = $tooltip.find('.tooltip-content-inner');

        $inner.css('width', '306px');
        $tooltip.removeClass('tooltip-loading');

    });


    $(".like-btn").on("click", function () {

        const $btn = $(this);
        const postId = $btn.data("post-id");

        const $icon = $btn.find(".like-icon");
        const originalIconClass = $icon.attr("class"); // store original class

        $btn.addClass("disabled");
        $icon.attr("class", "las la-spinner la-spin p-0"); // la-spin animates spinner

        const $likeText = $btn.find(".like-text");
        const $likeCount = $btn.find(".like-count");

        axios.post('/Posts/ToggleLike', null, {
            params: {
                postId: postId
            }
        })
        .then(function (response) {

            const liked = response.data.liked;
            const totalLikes = response.data.total_likes;
    
            if (liked) {
                $btn.addClass("bg-light-success");
                $likeText.text("Liked");
            } else {
                $btn.removeClass("bg-light-success");
                $likeText.text("Like");
            }
    
            $likeCount.text(totalLikes);
        })
        .catch(function (error) {
            console.error("Like toggle failed", error);
        })
        .finally(function () {
            // Restore original icon and enable button
            $icon.attr("class", originalIconClass);
            $btn.removeClass("disabled");
        });
    });


    $(".favourite-btn").on("click", function () {

        const $btn = $(this);
        const postId = $btn.data("post-id");

        const $icon = $btn.find(".favourite-icon");
        const originalIconClass = $icon.attr("class"); // store original class

        $btn.addClass("disabled");
        $icon.attr("class", "las la-spinner la-spin p-0"); // la-spin animates spinner

        const $favouriteText = $btn.find(".favourite-text");
        const $favouriteCount = $btn.find(".favourite-count");

        axios.post('/Posts/ToggleFavourite', null, {
            params: {
                postId: postId
            }
        })
        .then(function (response) {

            const favourited = response.data.favourited;
            const totalFavourites = response.data.total_favourites;

            if (favourited) {
                $btn.addClass("bg-light-danger");
                $favouriteText.text("Favourited");
            } else {
                $btn.removeClass("bg-light-danger");
                $favouriteText.text("Favourite");
            }

            $favouriteCount.text(totalFavourites);
        })
        .catch(function (error) {
            console.error("Favourite toggle failed", error);
        })
        .finally(function () {
            // Restore original icon and enable button
            $icon.attr("class", originalIconClass);
            $btn.removeClass("disabled");
        });
    });

    
    $('.share-btn').on('click', function() {

        // Cache the button elements for later use
        const $btn = $(this);
        const postId = $btn.data('post-id'); // Gets data-post-id
        const $countBadge = $btn.find('.share-count');

        // Construct the dynamic URL
        const dynamicUrl = window.location.origin + '/' + postId;

        Swal.fire({
            title: 'Share this post',
            html: `
                <div class="shareon" data-url="${dynamicUrl}">
                    <a class="facebook"></a>
                    <a class="linkedin"></a>
                    <a class="twitter"></a>
                    <a class="pinterest"></a>
                    <a class="whatsapp"></a>
                    <a class="copy-url"></a>
                    <a class="email"></a>
                    <a class="print"></a>
                    <a class="web-share"></a>
                </div>
            `,
            showConfirmButton: false,
            showCloseButton: true,
            allowOutsideClick: false,
            allowEscapeKey: false,
            width: '600px',
            didOpen: function (popup) {
                // 'popup' is the raw DOM element, wrap it in jQuery
                const $popup = $(popup);
                const $shareonContainer = $popup.find('.shareon');

                $shareonContainer.attr('data-url', dynamicUrl);

                Shareon.init();

                // 4. Track clicks on the social icons
                $shareonContainer.find('a').on('click', function () {

                    // Send Axios request
                    axios.post('/Posts/IncrementShare', null, {
                        params: { postId: postId }
                    })
                        .then(function (response) {
                            console.log( $countBadge, response.data.totalShares );
                            $countBadge.text(response.data.total_shares);
                        })
                        .catch(function (error) {
                            console.error("Error tracking share:", error);
                        });
                });
            }
        });
        
    });
    
    const userProfile = $('.user-profile-nav');

    $('.discussion-btn').on('click', function () {

        const $btn = $(this);
        const postId = $btn.data('post-id'); // Gets data-post-id
        const countBadge = $btn.find('.discussion-count');


        Swal.fire({
            title: 'Discussion',
            width: '900px',
            customClass: {
                popup: 'no-padding-swal allow-scroll-swal'
            },
            heightAuto: false,
            allowOutsideClick: false,
            allowEscapeKey: false,
            html: `
                <div id="swal-comments-container" style="min-height:300px;">
                    <div id="swal-comments-loader" style="display:flex;align-items:center;justify-content:center;height:200px;">
                        <div class="swal2-loader" style="display:block"></div>
                    </div>
                    <div id="swal-comments-widget" style="display:none; margin-top:10px;"></div>
                </div>
            `,
            showCloseButton: true,
            showConfirmButton: false,
            didOpen: () => {
                // Fetch comments from API and initialize widget
                fetchCommentsAndRender(postId, countBadge);
            }
        });

    });

    // Helper: fetch comments from our API then render the jQuery Comments plugin
    function fetchCommentsAndRender(postId, countBadge) {
        const loader = document.getElementById('swal-comments-loader');
        const widgetContainer = document.getElementById('swal-comments-widget');

        // Show loader
        if (loader) loader.style.display = 'flex';
        if (widgetContainer) widgetContainer.style.display = 'none';

        axios.get(`/comments/post/${postId}`)
            .then(function (response) {
                
                const comments = response.data || [];

                // Initialize widget
                initJQueryComments(widgetContainer, comments, postId, countBadge);
            })
            .catch(function (error) {
                console.error('Failed to load comments', error);
                if (loader) loader.style.display = 'none';
                if (widgetContainer) {
                    widgetContainer.style.display = 'block';
                    widgetContainer.innerHTML = '<div class="text-center text-danger p-3">Failed to load comments.</div>';
                }
            });
    }

    // Initialize jQuery Comments plugin with restricted features
    function initJQueryComments(containerEl, items, postId, countBadge) {
        if (!containerEl) return;

        // Hide loader
        const loader = document.getElementById('swal-comments-loader');
        if (loader) loader.style.display = 'none';

        // Show widget container
        containerEl.style.display = 'block';
        containerEl.innerHTML = ''; // clear

        // Ensure plugin script & css are loaded in your layout. If not, load them here (skipped).

        // Build initial structure for plugin
        const $container = $(containerEl);

        // Provide required callbacks for createReply and createComment
        $container.comments({
            profilePictureURL: userProfile.data('img'),
            getComments: function(success, error) {
                // plugin asks for comments - we already have items
                success(items);
            },
            postComment: function(commentJSON, success, error) {
                // Called when adding a new root comment
                const payload = {
                    postId: postId,
                    parentId: commentJSON.parent ? parseInt(commentJSON.parent) : null,
                    content: commentJSON.content
                };
                
                axios.post('/comments', payload)
                    .then(function(resp) {
                        countBadge.text(resp.data.total_discussion);
                        success(resp.data);
                    })
                    .catch(function(err) { console.error(err); error('Failed'); });
            },
            timeFormatter: function(time) {
                return formatDateWithAgo(time)
            },
            // Disable editing, liking, deleting, attachments and sorting
            enableEditing: false,
            enableDeleting: false,
            enableDeletingCommentWithReplies: false,
            enableAttachments: false,
            enablePinging: false,
            enableNavigation: false,
            postCommentOnEnter: true,
            enableUpvoting: false,
            enableDownvoting: false,
            enableSorting: false,
            textareaPlaceholderText: 'Write a comment...',
            sendText: 'Comment',
        });
    }

    function formatDateWithAgo(dateString) {
        // Normalize timezone (add :00 if missing)
        if (dateString.match(/\+\d{2}$/)) {
            dateString += ":00";
        }

        const date = new Date(dateString);
        const now = new Date();

        // Format date: 20-Dec-2025 05:21 PM
        const formattedDate = date.toLocaleString("en-GB", {
            day: "2-digit",
            month: "short",
            year: "numeric",
            hour: "2-digit",
            minute: "2-digit",
            hour12: true
        });

        // Time difference
        const diffMs = now - date;
        const diffMinutes = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMinutes / 60);
        const diffDays = Math.floor(diffHours / 24);

        let agoText = "";

        if (diffMinutes < 60) {
            agoText = `${diffMinutes} minute${diffMinutes !== 1 ? "s" : ""} ago`;
        }
        else if (diffHours < 24) {
            agoText = `${diffHours} hour${diffHours !== 1 ? "s" : ""} ago`;
        }
        else {
            agoText = `${diffDays} day${diffDays !== 1 ? "s" : ""} ago`;
        }
        
        if(diffMinutes < 1) {
            return "Just Now";
        }

        return `${formattedDate} • ${agoText}`;
    }


    $('.view_on_map').on('click', function () {

        const lat = $(this).data('lat');
        const lng = $(this).data('lng');

        const mapUrl = `https://www.google.com/maps?q=${lat},${lng}&z=15&output=embed`;

        Swal.fire({
            title: 'Location on Map',
            width: '900px',
            customClass: {
                popup: 'no-padding-swal'
            },
            heightAuto: false,
            html: `
        <div style="position:relative;height:410px;">
            
            <!-- Loader -->
            <div id="map-loader"
                 style="
                    position:absolute;
                    inset:0;
                    display:flex;
                    align-items:center;
                    justify-content:center;
                    background:#fff;
                    z-index:10;
                 ">
                <div class="swal2-loader" style="display: block;"></div>
            </div>

            <!-- Map -->
            <iframe
                id="map-iframe"
                src="${mapUrl}"
                width="100%"
                height="100%"
                style="border:0;"
                loading="lazy"
                allowfullscreen>
            </iframe>

        </div>
        `,
            showCloseButton: true,
            showConfirmButton: false,
            didOpen: () => {
                const iframe = document.getElementById('map-iframe');
                const loader = document.getElementById('map-loader');

                iframe.onload = () => {
                    loader.style.display = 'none';
                };
            }
        });

    });



    // Donation flow using Stripe Elements and SweetAlert2
    $(document).on('click', '.donation-btn-button', async function () {
        
        const $btn = $(this);
        const mark_btn = $btn.parent('.mark-btn');
        const $post = $btn.parents('.road-post');
        const postId = $btn.data('post-id');
        
        
        const remaining_amount = $post.find('.remaining_amount');
        const raised_amount = $post.find('.raised_amount');
        const people_donated_count = $post.find('.people_donated_count');
        const raised_percentage = $post.find('.raised_percentage');


        const progress_bar = $post.find('.progress-bar');
        const progress_circle = $post.find('.progress-circle');
        const tooltip_arrow = $post.find('.tooltip-arrow');
        const custom_tooltip = $post.find('.custom-tooltip');
        
        
        await Swal.fire({
            title: 'Donate',
            html:
                '<div style="max-width:500px">' +
                '<label for="swal-amount" class="h3">Amount (PKR)</label>' +
                '<input id="swal-amount" type="number" step="1" class="swal2-input" placeholder="Enter amount">' +
                '<div id="swal-card-element" class="mt-3"></div>' +
                '</div>',
            showCancelButton: true,
            confirmButtonText: 'Donate',
            showLoaderOnConfirm: true,
            focusConfirm: false,
            allowOutsideClick: false,
            allowEscapeKey: false,
            preConfirm: async () => {
                
                const amountStr = document.getElementById('swal-amount').value;
                const amount = parseFloat(amountStr);
                
                if (!amount || isNaN(amount) || amount <= 0) {
                    toastr.error('Please enter a valid amount');
                    return false;
                }
                
                // Create PaymentIntent on server
                try {
                    const res = await axios.post('/Donations/CreatePaymentIntent', { post_id: postId, amount: amount });
                    
                    // Confirm card payment
                    const popup = Swal.getPopup();
                    const stripe = popup.stripe;
                    const card = popup.card;

                    const result = await stripe.confirmCardPayment(res.data.client_secret, {
                        payment_method: { card: card }
                    });

                    if (result.error) {
                        throw new Error(result.error.message || 'Payment could not be completed.');
                    }

                    if (!result.paymentIntent || result.paymentIntent.status !== 'succeeded') {
                        const statusText = result.paymentIntent ? result.paymentIntent.status : 'unknown';
                        throw new Error('Payment not completed. Status: ' + statusText);
                    }

                    // Persist donation on server
                    const resp = await axios.post('/Donations/ConfirmDonation', {
                        payment_intent_id: result.paymentIntent.id,
                        post_id: postId
                    });
                    const data = resp.data;

                    console.log( data );

                    remaining_amount.html(data.remaining_amount);
                    raised_amount.html(data.total_raised);
                    people_donated_count.html(data.people_donated);
                    raised_percentage.html(data.raised_percentage);

                    progress_bar.css('width', data.raised_percentage + '%');
                    progress_circle.css('left', data.raised_percentage + '%');
                    tooltip_arrow.css('left', data.raised_percentage + '%');
                    custom_tooltip.css('left', data.tooltip_box_percentage + '%');

                    // Replace classes only inside this post
                    $post.find('.people_donated_count, .progress-bar, .progress-circle, .detached-arrow, .custom-tooltip, .tooltip-content').each(function() {
                        let $el = $(this);
                        let classes = $el.attr('class').split(/\s+/);

                        classes.forEach(cls => {
                            // Match bg-light-* pattern
                            if (/^bg-light-/.test(cls)) {
                                $el.removeClass(cls).addClass('bg-light-' + data.progress_class);
                            }

                            // Match bg-* pattern
                            else if (/^bg-/.test(cls)) {
                                $el.removeClass(cls).addClass('bg-' + data.progress_class);
                            }

                            // Match border-* pattern
                            else if (/^border-/.test(cls)) {
                                $el.removeClass(cls).addClass('border-' + data.progress_class);
                            }

                            else if (/^text-/.test(cls)) {
                                $el.removeClass(cls).addClass('text-' + data.progress_class);
                            }
                        });
                        
                    });
                    
                    let msg = `<p>We appreciate your <strong>Rs. ${amountStr}</strong> donation — your contribution helps patch roads and keep our community safe.</p>`;
                    
                    if(data.remaining_amount === "0") {
                        
                        $post.find('.donation-btn-button').remove();
                        
                        const goalHtml = `<div class="col-12">
                            <small class="fw-bold text-success">Goal</small>
                            <h2 class="fw-bolder mb-0 fz-20">Rs. ${data.cost}</h2>
                        </div>`;


                        $post.find('.post-costing').html(goalHtml);

                        mark_btn.hide();
                        
                        $(data.btn.value).insertAfter(mark_btn);
                        
                        mark_btn.remove();
                        
                        msg = `
                        <div style="font-size: 1.1em;">
                            <p>Incredible! Your <strong>Rs. ${amountStr}</strong> donation was the final piece of the puzzle.</p>
                            <p class="mt-2"><strong>This project is now fully funded! and now will be moved to Pending list</strong> 🚧✅</p>
                            <p style="color: #666; font-size: 0.9em;">Repair work can now begin immediately thanks to you.</p>
                        </div>
                        `;
                    }
                    
                    // Close the donation popup and show success
                    Swal.close();
                    Swal.fire({
                        title: 'Thank you',
                        html: msg,
                        icon: 'success'
                    });
                
                }
                catch (err) {
                    
                    console.error('create payment intent failed', err);
                    console.log(err)

                    // Extract a useful error message from axios/server response to show to the user.
                    let msg = 'Failed to initialize payment. ';

                    const data = err.response?.data;

                    if (data?.message) {
                        msg += data.message;
                    } else if (data?.error) {
                        msg += data.error;
                    } else if (data?.title) {
                        msg += data.title;

                        if(data.errors) {
                            if (Array.isArray(data.errors)) {
                                msg += data.errors.join(', ');
                            } else if (typeof data.errors === 'object') {
                                const vals = Object.values(data.errors).flatMap(v => Array.isArray(v) ? v : [v]);
                                msg += vals.join(', ');
                            }
                        }

                    } else if (err.message) {
                        msg += err.message;
                    } else {
                        // fallback to JSON stringify if something unexpected
                        msg += JSON.stringify(data);
                    }

                    toastr.error(msg);
                    
                    return false;
                }
            },
            didOpen: () => {
                // initialize stripe element
                const pk = window.stripePublishableKey;

                const stripe = Stripe(pk);
                const elements = stripe.elements();
                const style = {
                    base: {
                        color: '#32325d',
                        fontFamily: 'Arial, sans-serif',
                        fontSmoothing: 'antialiased',
                        fontSize: '16px',
                        '::placeholder': { color: '#a0aec0' }
                    },
                    invalid: { color: '#fa755a', iconColor: '#fa755a' }
                };
                const card = elements.create('card', { style, hidePostalCode: true });
                card.mount('#swal-card-element');

                // Store for later use
                const popup = Swal.getPopup();
                popup.card = card;
                popup.stripe = stripe;

                card.on('change', function (event) {
                    if(event.error) {
                        toastr.error(event.error.message);
                    }
                });
            }
        });
        
    });
    
    $(document).on('click', '.mark-status-btn', async function () {

        const $btn = $(this);
        const mark_btn = $btn.parent('.mark-btn');
        const postId = $btn.data('post-id');
        const currentStatus = $btn.data('status');

        const nextStatus = currentStatus === 'Completed' ? 'InProcess' : 'Completed';

        Swal.fire({
            title: `Marking as ${nextStatus}`,
            html: 'Please wait...',
            allowOutsideClick: false,
            allowEscapeKey: false,
            didOpen: () => Swal.showLoading()
        });

        const response = await axios.post('/Posts/ToggleStatus', null, {
            params: { postId }
        });

        Swal.close();

        mark_btn.hide();

        $(response.data.btn.value).insertAfter(mark_btn);
        
        mark_btn.remove();

        Swal.fire({
            title: `Marked ${nextStatus}`,
            text: `The post status was updated to ${nextStatus}! and now will be moved to ${nextStatus} list`,
            icon: 'success'
        });
        
    });

});