$(document).ready(function () {
    
    $('.logout-link').click(function () {
        $('#logoutForm').trigger('submit');
    })
    
    const confirmLink = $('#confirm-link');
    
    if(confirmLink.length) {
        
        let count = 7; // Starting value
        const interval = setInterval(function() {

            // Decrement the count
            count--;

            // Update the div text with the new count
            $('.autoConfirmMail').text(count);

            // If the count reaches 1, stop the interval and trigger the click event
            if (count === 0) {
                window.location.href = confirmLink.attr('href'); 
                clearInterval(interval); // Stop the interval
            }
        }, 1000); // 1000 ms (1 second)
    }
    
});
