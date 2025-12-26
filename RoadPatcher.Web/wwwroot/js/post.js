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
        const postId = $btn.data('post-id');
        
        const { value: formValues } = await Swal.fire({
            title: 'Donate',
            html:
                '<div style="max-width:500px">' +
                '<label for="swal-amount" class="form-label">Amount (PKR)</label>' +
                '<input id="swal-amount" type="number" step="1" class="swal2-input" placeholder="Enter amount">' +
                '<div id="swal-card-element" class="mt-3"></div>' +
                '</div>',
            showCancelButton: true,
            confirmButtonText: 'Donate',
            showLoaderOnConfirm: true,
            focusConfirm: false,
            showCloseButton: true,
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
                    return { clientSecret: res.data.client_secret, paymentIntentId: res.data.payment_intent_id, amount };
                }
                
                catch (err) {
                    
                    console.error('create payment intent failed', err);

                    // Extract a useful error message from axios/server response to show to the user.
                    let msg = 'Failed to initialize payment. ';

                    const data = err.response.data;

                    if (data.message) {
                        msg += data.message;
                    } else if (data.error) {
                        msg += data.error;
                    } else if (data.title) {
                        msg += data.title;

                        if(data.errors) {
                            if (Array.isArray(data.errors)) {
                                msg += data.errors.join(', ');
                            } else if (typeof data.errors === 'object') {
                                const vals = Object.values(data.errors).flatMap(v => Array.isArray(v) ? v : [v]);
                                msg += vals.join(', ');
                            }
                        }

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

        if (!formValues) return; // canceled or failed validation

        // Confirm card payment
        const popup = Swal.getPopup();
        const stripe = popup.stripe;
        const card = popup.card;

        try {
            const result = await stripe.confirmCardPayment(formValues.clientSecret, {
                payment_method: {
                    card: card
                }
            });

            if (result.error) {
                toastr.error(result.error.message || 'Payment could not be completed.');
                return;
            }

            if (result.paymentIntent && result.paymentIntent.status === 'succeeded') {
                // Persist donation on server
                try {
                    const resp = await axios.post('/Donations/ConfirmDonation', { payment_intent_id: result.paymentIntent.id });
                    const data = resp.data;

                    if (data && data.totalRaised !== undefined) {
                        // Close donation popup, update UI and show success modal
                        Swal.close();
                        const container = $btn.closest('.card.mx-auto.road-post');
                        updatePostSummaryUI(container, data);
                        Swal.fire('Thank you', `We appreciate your Rs. ${formValues.amount} donation — your contribution helps patch roads and keep our community safe.`, 'success');
                    } else if (data && data.success === true) {
                        Swal.close();
                        Swal.fire('Notice', 'Donation recorded.', 'success');
                    } else {
                        toastr.error('Payment succeeded but server returned an unexpected response. Please contact support.');
                    }
                } catch (err) {
                    console.error('confirm donation server call failed', err);
                    toastr.error('Payment succeeded but we failed to record the donation on server. Please contact support.');
                }
            } else {
                const statusText = result.paymentIntent ? result.paymentIntent.status : 'unknown';
                toastr.error('Payment not completed. Status: ' + statusText);
            }
        } catch (err) {
            console.error('confirmCardPayment exception', err);
            toastr.error('Payment confirmation failed. Please try again.');
        }
    });


    // Helper: format PKR for display
    function formatPkr(amount) {
        if (amount === null || amount === undefined) return 'Rs. 0';
        const num = Number(amount);
        const fraction = (Math.round((num - Math.floor(num)) * 100) !== 0) ? 2 : 0;
        return 'Rs. ' + num.toLocaleString('en-US', { minimumFractionDigits: fraction, maximumFractionDigits: 2 });
    }

    // Helper: update post UI given the card container and server summary
    function updatePostSummaryUI(container, data) {
        if (!container || !data) return;

        // people donated
        container.find('.people_donated_count').each(function () {
            $(this).text(data.peopleDonated);
            // update text color class 'text-*'
            $(this).removeClass(function (index, className) {
                return (className.match(/(^|\s)text-\S+/g) || []).join(' ');
            }).addClass('text-' + data.progressClass);
        });

        // remaining amount display
        container.find('.remaining_amount').text(formatPkr(data.remainingAmount));

        // tooltip raised amount
        container.find('.tooltip-content .col-6.p-2 .fw-bold').text(formatPkr(data.totalRaised));

        // achieved percentage inside tooltip
        container.find('.achieved-box .fw-bold').text(data.raisedPercentage + '%');

        // progress bar width and color
        const $progress = container.find('.progress-bar');
        $progress.css('width', data.raisedPercentage + '%');
        $progress.removeClass(function (index, className) {
            return (className.match(/(^|\s)bg-\S+/g) || []).join(' ');
        }).addClass('bg-' + data.progressClass);

        // progress circle
        const $circle = container.find('.progress-circle');
        $circle.css('left', data.raisedPercentage + '%');
        $circle.removeClass(function (index, className) {
            return (className.match(/(^|\s)bg-\S+/g) || []).join(' ');
        }).addClass('bg-' + data.progressClass);

        // detached arrow border class
        const $arrow = container.find('.detached-arrow');
        $arrow.css('left', data.raisedPercentage + '%');
        $arrow.removeClass(function (index, className) {
            return (className.match(/(^|\s)border-\S+/g) || []).join(' ');
        }).addClass('border-' + data.progressClass);

        // tooltip position and classes
        const $tooltip = container.find('.custom-tooltip');
        $tooltip.css('left', data.tooltipBoxPercentage + '%');
        const $tooltipContent = $tooltip.find('.tooltip-content');
        $tooltipContent.removeClass(function (index, className) {
            return (className.match(/(^|\s)bg-light-\S+/g) || []).join(' ');
        });
        $tooltipContent.removeClass(function (index, className) {
            return (className.match(/(^|\s)border-\S+/g) || []).join(' ');
        });
        $tooltipContent.addClass('bg-light-' + data.progressClass);
        $tooltipContent.addClass('border-' + data.progressClass);
    }

});

// End of file

