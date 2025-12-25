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
            const totalLikes = response.data.totalLikes;
    
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
            const totalFavourites = response.data.totalFavourites;

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
                            $countBadge.text(response.data.totalShares);
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



});