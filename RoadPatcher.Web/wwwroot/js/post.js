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


});