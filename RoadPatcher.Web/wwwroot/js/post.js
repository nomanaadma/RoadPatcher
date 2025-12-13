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
            


});