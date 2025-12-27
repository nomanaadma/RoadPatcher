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


    // $('.post-cost-field').on('focusout', function() {
    //     let value = parseFloat($(this).val());
    //
    //     // If empty, NaN, or less than 200, set to 200
    //     if (isNaN(value) || value < 200) {
    //         $(this).val(200).trigger('input');
    //     }
    // });

    // Get current URL params
    const urlParams = new URLSearchParams(window.location.search);

    // Iterate over all pagination links
    $(".pagination a.page-link").each(function () {
        let href = $(this).attr("href");

        if (!href) return; // skip if no href

        // Extract the page number from href
        const pageMatch = href.match(/page=(\d+)/);
        const page = pageMatch ? pageMatch[1] : null;

        if (!page) return; // skip if no page number

        // Set the current params including page
        urlParams.set("page", page);

        // Update the href with current parameters
        $(this).attr("href", window.location.pathname + "?" + urlParams.toString());
    });

    $('.search_posts').on('click', function () {

        Swal.fire({
            title: 'Search',
            width: '900px',
            customClass: {
                popup: 'no-padding-swal allow-scroll-swal'
            },
            heightAuto: false,
            allowOutsideClick: false,
            allowEscapeKey: false,
            html: `
                <div id="swal-search-container" style="min-height:300px;">
                
                    <div id="swal-search-container" style="min-height:300px;">
                        <nav>
                            <div class="nav nav-tabs nav-fill" id="nav-tab" role="tablist">
                                <button class="nav-item nav-link active" id="search-tab" data-bs-toggle="tab" data-bs-target="#search-tab-ct" type="button" role="tab" aria-controls="search-tab-ct" aria-selected="true">Search</button>
                                
                                <button class="nav-item nav-link" id="advanced-search-tab" data-bs-toggle="tab" data-bs-target="#advanced-search-tab-ct" type="button" role="tab" aria-controls="advanced-search-tab-ct" aria-selected="true">Advanced Search</button>
                                
                                <button class="nav-item nav-link" id="map-search-tab" data-bs-toggle="tab" data-bs-target="#map-search-tab-ct" type="button" role="tab" aria-controls="map-search-tab-ct" aria-selected="true">Map Search</button>
                              
                            </div>
                        </nav>
                        <div class="tab-content mt-10" id="nav-tabContent">
                            
                            <div class="tab-pane fade show active" id="search-tab-ct" role="tabpanel" aria-labelledby="search-tab">
                                
                                 <form action="/Posts/Search" method="get" class="search-form p-4 border rounded bg-light">

                                  <div class="row g-3">
                                
                                    <!-- Address / Text Search -->
                                    <div class="mt-4 col-md-12">
                                      <label for="searchText" class="form-label">Search Text</label>
                                      <input type="text" name="s" class="form-control" id="searchText" placeholder="Address, description..." autofocus>
                                    </div>
                                    </div>
                                    <div class="mt-4">
                                        <button type="submit" class="btn btn-primary">Search</button>
                                        <button type="reset" class="btn btn-secondary ms-2">Reset</button>
                                      </div>
                                  </form>
                            </div>
                            
                            <div class="tab-pane fade show" id="advanced-search-tab-ct" role="tabpanel" aria-labelledby="advanced-search-tab">
                               
                               <form action="/Posts/AdvancedSearch" method="get" class="search-form p-4 border rounded bg-light">

                                  <div class="row g-3">
                                
                                    <!-- Address / Text Search -->
                                    <div class="mt-4 col-md-6">
                                      <label for="searchText" class="form-label">Search Text</label>
                                      <input type="text" name="as" class="form-control" id="searchText" placeholder="Address, description...">
                                    </div>
                                
                                    <!-- Area (Google Autocomplete) -->
                                    <div class="mt-4 col-md-3">
                                      <label for="areaInput" class="form-label">Area</label>
                                      <input type="text" name="area" id="areaInput" class="form-control" placeholder="Select area...">
                                      <input type="hidden" name="lat" id="areaLat">
                                      <input type="hidden" name="lng" id="areaLng">
                                    </div>
                                    
                                    <div class="mt-4 col-md-3">
                                      <label for="radius" class="form-label">Radius</label>
                                      <input type="number" id="radius" name="radius" class="form-control" placeholder="Find in Radius" min="1" max="100">
                                    </div>
                                
                                    <!-- Goal / Cost -->
                                    <div class="mt-4 col-md-3">
                                      <label for="goalStart" class="form-label">Goal From</label>
                                      <input type="text" name="goal_start" id="goalStart" class="form-control number-separator" placeholder="Min">
                                    </div>
                                    <div class="mt-4 col-md-3">
                                      <label for="goalEnd" class="form-label">Goal To</label>
                                      <input type="text" name="goal_end" id="goalEnd" class="form-control number-separator" placeholder="Max">
                                    </div>
                                
                                    <!-- Raised -->
                                    <div class="mt-4 col-md-3">
                                      <label for="raisedStart" class="form-label">Raised From</label>
                                      <input type="text" name="raised_start" id="raisedStart" class="form-control number-separator" placeholder="Min">
                                    </div>
                                    <div class="mt-4 col-md-3">
                                      <label for="raisedEnd" class="form-label">Raised To</label>
                                      <input type="text" name="raised_end" id="raisedEnd" class="form-control number-separator" placeholder="Max">
                                    </div>
                                
                                    <!-- Remaining -->
                                    <div class="mt-4 col-md-3">
                                      <label for="remainingStart" class="form-label">Remaining From</label>
                                      <input type="text" name="remaining_start" id="remainingStart" class="form-control number-separator" placeholder="Min">
                                    </div>
                                    <div class="mt-4 col-md-3">
                                      <label for="remainingEnd" class="form-label">Remaining To</label>
                                      <input type="text" name="remaining_end" id="remainingEnd" class="form-control number-separator" placeholder="Max">
                                    </div>
                                    
                                    <!-- Achieved % -->
                                    <div class="mt-4 col-md-3">
                                      <label for="achievedStart" class="form-label">Achieved % From</label>
                                      <input type="number" name="achieved_start" id="achievedStart" class="form-control" placeholder="Min" step="0.01" min="0" max="100">
                                    </div>
                                    <div class="mt-4 col-md-3">
                                      <label for="achievedEnd" class="form-label">Achieved % To</label>
                                      <input type="number" name="achieved_end" id="achievedEnd" class="form-control" placeholder="Max" step="0.01" min="0" max="100">
                                    </div>
                                
                                    <!-- Donated By -->
                                    <div class="mt-4 col-md-3">
                                      <label for="donatedByStart" class="form-label">People Donated From</label>
                                      <input type="number" name="donated_by_start" id="donatedByStart" class="form-control" placeholder="Min">
                                    </div>
                                    <div class="mt-4 col-md-3">
                                      <label for="donatedByEnd" class="form-label">People Donated To</label>
                                      <input type="number" name="donated_by_end" id="donatedByEnd" class="form-control" placeholder="Max">
                                    </div>
                                
                                    <!-- Status -->
                                    <div class="mt-4 col-md-3">
                                      <label for="status" class="form-label">Status</label>
                                      <select name="status" id="status" class="form-select">
                                        <option value="">Any</option>
                                        <option value="Pending">Pending</option>
                                        <option value="InProcess">In Process</option>
                                        <option value="Completed">Completed</option>
                                      </select>
                                    </div>
                                
                                  </div>
                                
                                  <!-- Submit Button -->
                                  <div class="mt-4">
                                    <button type="submit" class="btn btn-primary">Search</button>
                                    <button type="reset" class="btn btn-secondary ms-2">Reset</button>
                                  </div>
                                </form>
                               
                               
                            </div>
                            
                            <div class="tab-pane fade show" id="map-search-tab-ct" role="tabpanel" aria-labelledby="map-search-tab">
                                <div class="row">
                                  <div class="col-md-12 mb-4 ">
                                    <h5 class="mb-2">Filter Posts</h5>
                                
                                    <div class="btn-group-horizontal w-100" role="group" id="statusFilters">
                                       
                                        <label class="btn shadow-hover bg-light-danger border-light-danger" for="filterPending" data-class="bg-light-danger">
                                             <input type="checkbox" class="status-filter" id="filterPending" value="Pending" checked autocomplete="off"> Pending
                                        </label>
                                        
                                        <label class="btn shadow-hover bg-light-warning border-light-warning" for="filterInProcess" data-class="bg-light-warning">
                                            <input type="checkbox" class="status-filter" id="filterInProcess" value="InProcess" checked autocomplete="off"> In Process
                                        </label>
                                        
                                        <label class="btn shadow-hover bg-light-success border-light-success" for="filterCompleted" data-class="bg-light-success">
                                            <input type="checkbox" class="status-filter" id="filterCompleted" value="Completed" checked autocomplete="off"> Completed
                                        </label>
                                    </div>
                                </div>

                                  <div class="col-md-12">
                                    <div id="map" style="width: 100%; height: 400px;"></div>
                                  </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            `,
            showCloseButton: true,
            showConfirmButton: false,
            didOpen: () => {

                initMap();

                $(".search-form input, .search-form select").each(function () {
                    const name = $(this).attr("name");
                    if (name && urlParams.has(name)) {
                        console.log( urlParams.get(name) )
                        $(this).val(urlParams.get(name)).trigger('input');
                    }
                });
                
                const input = document.getElementById("areaInput");
                const latInput = document.getElementById("areaLat");
                const lngInput = document.getElementById("areaLng");

                const autocomplete = new google.maps.places.Autocomplete(input, {
                    types: ["(regions)"], // Only show areas/cities
                    componentRestrictions: { country: "pk" } // Optional
                });

                autocomplete.addListener("place_changed", () => {
                    const place = autocomplete.getPlace();
                    if (!place.geometry) return;

                    latInput.value = place.geometry.location.lat();
                    lngInput.value = place.geometry.location.lng();
                });

                input.addEventListener("input", () => {
                    latInput.value = "";
                    lngInput.value = "";
                });
                
            }
        });

    });

    let map;
    let markers = [];
    let controller = null; // AbortController for canceling previous requests
    
    const userProfile = $('.user-profile-nav');
    
    async function initMap() {

        const { PlaceAutocompleteElement } = await google.maps.importLibrary("places");

        const lat = parseFloat(userProfile.data('lat'));
        const lng = parseFloat(userProfile.data('lng'));

        const initialPos = {
            lat: isNaN(lat) ? 30.4188959 : lat,
            lng: isNaN(lng) ? 69.9951495 : lng
        };

        map = new google.maps.Map(document.getElementById("map"), {
            center: initialPos,
            zoom: 13,
            mapTypeControl: true,
            streetViewControl: false,
            fullscreenControlOptions: {
                position: google.maps.ControlPosition.LEFT_BOTTOM
            }
        });

        // 3. Create the New Autocomplete Component
        const autocomplete = new PlaceAutocompleteElement();
        autocomplete.id = "place-autocomplete-input";

        // restrict to Pakistan
        autocomplete.includedRegionCodes = ['pk'];

        // EVENT 1: User selects a place from New Autocomplete
        autocomplete.addEventListener("gmp-select", async ({ placePrediction }) => {

            const place = placePrediction.toPlace();

            // 1. Fetch the geometry fields explicitly (this is required!)
            await place.fetchFields({ fields: ["location", "viewport"] });

            // 2. Check if viewport exists (some places like specific addresses might only have location)
            if (place.viewport) {
                map.fitBounds(place.viewport);
            } else if (place.location) {
                map.setCenter(place.location);
                map.setZoom(13); // Zoom in close for a specific point
            } else {
                console.error("No geometry information available for this place.");
            }
            
        });
        
        // Push it inside the map controls
        map.controls[google.maps.ControlPosition.TOP_LEFT].push(autocomplete);
        
        
        // Initial load
        google.maps.event.addListenerOnce(map, 'idle', () => {
            fetchPosts();
        });

        // Update posts on drag end or zoom change
        map.addListener("dragend", fetchPosts);
        map.addListener("zoom_changed", fetchPosts);

    }

    function fetchPosts() {
        const bounds = map.getBounds();
        
        if (!bounds) return;

        const ne = bounds.getNorthEast();
        const sw = bounds.getSouthWest();

        const selectedStatuses = $(".status-filter:checked")
            .map(function() { return this.value; })
            .get();

        // Cancel previous request if still running
        if (controller) controller.abort();
        controller = new AbortController();

        axios.get('/Posts/MapPosts', {
            params: {
                neLat: ne.lat(),
                neLng: ne.lng(),
                swLat: sw.lat(),
                swLng: sw.lng(),
                statuses: selectedStatuses.join(',')
            },
            signal: controller.signal
        })
            .then(response => {
                clearMarkers();
                response.data.forEach(addMarker);
            })
            .catch(error => {
                if (axios.isCancel(error)) {
                    console.log('Previous request canceled');
                } else {
                    console.error(error);
                }
            });
    }

    function getMarkerIcon(status) {
        switch(status) {
            case "Pending": return "https://maps.google.com/mapfiles/ms/icons/red-dot.png";
            case "InProcess": return "https://maps.google.com/mapfiles/ms/icons/orange-dot.png";
            case "Completed": return "https://maps.google.com/mapfiles/ms/icons/green-dot.png";
            default: return "https://maps.google.com/mapfiles/ms/icons/blue-dot.png";
        }
    }

    function addMarker(post) {
        const marker = new google.maps.Marker({
            position: { lat: post.lat, lng: post.lng },
            map: map,
            icon: getMarkerIcon(post.status),
            title: `#${post.id}`,
        });

        // Assuming 'post' is the object from your loop corresponding to the C# projection
        const infowindow = new google.maps.InfoWindow({
            content: `
        <div class="map-info-card">
            
            <div class="map-info-header">
                <h6 class="text-center">Post #${post.id}</h6>
            </div>

            <div class="map-info-desc">
                ${post.description}
            </div>
            
            <div class="stats-grid">
            
                <div class="stat-item">
                    <span class="stat-label">Goal</span>
                    <span class="stat-value" style="color: grey;">Rs. ${post.cost}</span>
                </div>
                <div class="stat-item">
                    <span class="stat-label">Raised</span>
                    <span class="stat-value" style="color: #4CAF50;">Rs. ${post.total_raised} : ${post.raised_percentage}%</span>
                </div>
                <div class="stat-item">
                    <span class="stat-label">Remaining</span>
                    <span class="stat-value" style="color: #e74c3c;">Rs. ${post.remaining_amount}</span>
                </div>
            </div>

            <div class="donors-count text-center">
                <i class="fas fa-users"></i> <span><strong>${post.people_donated}</strong> people have donated</span>
            </div>

        </div>
    `
        });

        marker.addListener('click', () => infowindow.open(map, marker));
        markers.push(marker);
    }

    function clearMarkers() {
        markers.forEach(m => m.setMap(null));
        markers = [];
    }

    $(document).on("change", ".status-filter", function () {
        
        const ethis = $(this);
        const activeClass = ethis.data('class');
        const label = ethis.parent('label');


        const checkedCount = $(".status-filter:checked").length;

        if (checkedCount === 0) {
            // Prevent unchecking the last one
            $(this).prop("checked", true);
            return;
        }

        // Toggle active class
        if (this.checked) {
            label.addClass(activeClass).removeClass(activeClass);
        } else {
            label.removeClass(activeClass).addClass(activeClass);
        }

        // Trigger map refresh
        fetchPosts();

    });


});
