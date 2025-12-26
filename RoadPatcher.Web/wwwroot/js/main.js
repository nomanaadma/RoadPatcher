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



    function numberSeparator(value, $input) {
        let commaCounter = $input.data('commaCounter') || 0;

        value += '';

        for (let i = 0; i < commaCounter; i++) {
            value = value.replace(',', '');
        }

        let x = value.split('.');
        let y = x[0];
        let z = x.length > 1 ? '.' + x[1] : '';
        let rgx = /(\d+)(\d{3})/;

        while (rgx.test(y)) {
            y = y.replace(rgx, '$1' + ',' + '$2');
        }

        $input.data('commaCounter', commaCounter + 1);

        return y + z;
    }

    $(document).on('focus', '.number-separator', function () {
        const $input = $(this);

        if (!$input.data('initialized')) {
            const name = $input.attr('name');

            if (name) {
                $input.removeAttr('name');

                const hiddenInput = $('<input>', {
                    type: 'hidden',
                    name: name,
                    id: name,
                    class: 'number-hidden'
                });

                $input.after(hiddenInput);
                $input.data('initialized', true);
                $input.data('commaCounter', 0);
            }
        }
    });
    
    
    $(document).on('input', '.number-separator', function () {

        let raw = this.value.replace(/,/g, '').replace(/[^\d]/g, '');
        let formatted = raw.replace(/\B(?=(\d{3})+(?!\d))/g, ",");

        this.value = formatted;

        // Update hidden input
        $(this).next('.number-hidden').val(raw);
    });
    

    $(document).on('input', '.number-separator', function () {
        const $input = $(this);

        let rawValue = this.value.replace(/,/g, '').replace(/[^\d]/g, '');

        // Apply formatting
        this.value = numberSeparator(rawValue, $input);

        // Update matching hidden input
        $input.next('.number-hidden').val(rawValue);
    });


    $('.post-cost-field').on('focusout', function() {
        let value = parseFloat($(this).val());

        // If empty, NaN, or less than 200, set to 200
        if (isNaN(value) || value < 200) {
            $(this).val(200).trigger('input');
        }
    });

});
