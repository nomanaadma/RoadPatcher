$(document).ready(function () {
    
    $('.logout-link').click(function () {
        $('#logoutForm').trigger('submit');
    })
    
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
        
        const $input = $(this);
        const hiddenNumber = $input.next('.number-hidden').val();
        let value = parseFloat(hiddenNumber);
        
        // If empty, NaN, or less than 200, set to 200
        if (isNaN(value) || value < 200) {
            $(this).val(200).trigger('input');
        }
    });


    const $toggle = $('.media_guideline_toggle');
    const $content = $('.media_guideline_content');

    // Click handler
    $toggle.on('click', function () {
        

        $content.stop(true, true).slideToggle(300, function () {
            if ($content.is(':visible')) {
                $toggle.text('Hide');
            } else {
                $toggle.text('Show');
            }
        });

    });


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

    const slimSettings = {settings: {
        allowDeselect: true
    }};

    let userOptions = ``;

    allUsers.forEach(user => {
        userOptions += `<option value="${user.Id}">${user.fullName}</option>`;
    });

    const isUser = $('.user-root').length;

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
                               
                               <form action="/Posts/AdvancedSearch" id="AdvancedSearchForm" method="get" class="search-form p-4 border rounded bg-light">

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
                                      <label for="statuses" class="form-label">Statuses</label>
                                      <select name="statuses" multiple id="statuses" class="form-select">
                                        <option value="Pending">Pending</option>
                                        <option value="InProcess">In Process</option>
                                        <option value="Completed">Completed</option>
                                      </select>
                                    </div>
                                    
                                    <div class="mt-4 col-md-3 ${isUser ? 'd-none' : ''}">
                                        <label for="users" class="form-label">Users</label>
                                        <select name="users" multiple id="users" class="form-select">
                                            ${userOptions}
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
                                        
                                        <!-- User -->
                                        <label for="users2" class="btn form-label map-user-select ${isUser ? 'd-none' : ''}">
                                            <span>Users</span>
                                            <select name="users2" multiple id="users2" class="form-select">
                                            ${userOptions}
                                            </select>
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
                    if (!name) return;

                    const values = urlParams.getAll(name); // <-- get all values
                    if (!values.length) return;

                    if ($(this).is('select[multiple]')) {
                        $(this).val(values).trigger('change'); // multiple select
                    } else {
                        $(this).val(values[0]).trigger('input'); // single input/select
                    }
                });
                
                new SlimSelect({
                    select: '#users',
                    ...slimSettings
                })

                new SlimSelect({
                    select: '#statuses',
                    ...slimSettings
                })

                new SlimSelect({
                    select: '#users2',
                    ...slimSettings,
                    events: {
                        afterChange: () => {
                            fetchPosts();
                        }
                    }
                })
                
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

    $(document).on('reset', '#AdvancedSearchForm', function () {
        const form = $(this);
        form.find('.ss-deselect').trigger('click');
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
            mapId: "d51644aa08ca7cea704f06a2",
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
        
        const selectedUsers = $('#users2').val();

        // Cancel previous request if still running
        if (controller) controller.abort();
        controller = new AbortController();

        axios.get('/Posts/MapPosts', {
            params: {
                neLat: ne.lat(),
                neLng: ne.lng(),
                swLat: sw.lat(),
                swLng: sw.lng(),
                statuses: selectedStatuses.join(','),
                userids: selectedUsers.join(',')
            },
            signal: controller.signal
        })
            .then(response => {
                clearMarkers();
                response.data[0].forEach(addMarker);
                response.data[1].forEach(addUserMarker);
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

    function addUserMarker(user) {
        const marker = new google.maps.Marker({
            position: { lat: user.lat, lng: user.lng },
            map: map,
            icon: getMarkerIcon(),
            title: user.full_name,
        });

        // Assuming 'post' is the object from your loop corresponding to the C# projection
        const infowindow = new google.maps.InfoWindow({
            content: `
        <div class="map-info-card">
            
            <div class="map-info-header">
                <h6 class="text-center">User: ${user.full_name}</h6>
            </div>
        </div>
    `
        });

        marker.addListener('click', () => infowindow.open(map, marker));
        markers.push(marker);
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
                ${post.description ?? ''}
            </div>
            
            <div class="stats-grid">
            
                <div class="stat-item">
                    <span class="stat-label">User</span>
                    <span class="stat-value">${post.cost}</span>
                </div>
                
                <div class="stat-item">
                    <span class="stat-label">Date</span>
                    <span class="stat-value">${post.date}</span>
                </div>
                
                <div class="stat-item">
                    <span class="stat-label">Posted</span>
                    <span class="stat-value">${post.since}</span>
                </div>
            </div>
            
            <div class="stats-grid">
            
                <div class="stat-item">
                    <span class="stat-label">Goal</span>
                    <span class="stat-value" style="color: grey;">${post.cost}</span>
                </div>
                <div class="stat-item">
                    <span class="stat-label">Raised</span>
                    <span class="stat-value" style="color: #4CAF50;">${post.total_raised} : ${post.raised_percentage}%</span>
                </div>
                <div class="stat-item">
                    <span class="stat-label">Remaining</span>
                    <span class="stat-value" style="color: #e74c3c;">${post.remaining_amount}</span>
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

    async function initAdminStatisticsMap() {
        const container = document.getElementById('admin-statistics-map');

        if (!container || typeof google === 'undefined' || !google.maps?.drawing) {
            return;
        }

        const { Map } = await google.maps.importLibrary("maps");
        const { PlaceAutocompleteElement } = await google.maps.importLibrary("places");

        const lat = parseFloat(userProfile.data('lat'));
        const lng = parseFloat(userProfile.data('lng'));

        const initialPos = {
            lat: isNaN(lat) ? 30.4188959 : lat,
            lng: isNaN(lng) ? 69.9951495 : lng
        };

        const statsMap = new Map(container, {
            center: initialPos,
            zoom: 6,
            mapId: "d51644aa08ca7cea704f06a2",
            mapTypeControl: true,
            streetViewControl: false,
            fullscreenControlOptions: {
                position: google.maps.ControlPosition.LEFT_BOTTOM
            }
        });

        const autocomplete = new PlaceAutocompleteElement();
        autocomplete.id = 'admin-statistics-place-autocomplete-input';
        autocomplete.includedRegionCodes = ['pk'];

        autocomplete.addEventListener('gmp-select', async ({ placePrediction }) => {
            const place = placePrediction.toPlace();

            await place.fetchFields({ fields: ['location', 'viewport'] });

            if (place.viewport) {
                statsMap.fitBounds(place.viewport);
            } else if (place.location) {
                statsMap.setCenter(place.location);
                statsMap.setZoom(13);
            }
        });

        statsMap.controls[google.maps.ControlPosition.TOP_LEFT].push(autocomplete);

        const drawingManager = new google.maps.drawing.DrawingManager({
            drawingMode: google.maps.drawing.OverlayType.POLYGON,
            drawingControl: true,
            drawingControlOptions: {
                position: google.maps.ControlPosition.TOP_LEFT,
                drawingModes: [google.maps.drawing.OverlayType.POLYGON]
            },
            polygonOptions: {
                editable: true,
                draggable: false,
                fillColor: '#0d6efd',
                fillOpacity: 0.18,
                strokeColor: '#0d6efd',
                strokeWeight: 2,
                zIndex: 2
            }
        });

        drawingManager.setMap(statsMap);

        const totalCountElement = document.getElementById('boundaryTotalCount');
        const pendingCountElement = document.getElementById('boundaryPendingCount');
        const inProcessCountElement = document.getElementById('boundaryInProcessCount');
        const completedCountElement = document.getElementById('boundaryCompletedCount');
        const goalElement = document.getElementById('boundaryGoal');
        const receivedElement = document.getElementById('boundaryReceived');
        const donatorsBody = document.getElementById('boundaryDonatorsBody');
        const donationsBody = document.getElementById('boundaryDonationsBody');
        const postStatsSection = document.querySelector('.post_stats');
        const donateStatsSection = document.querySelector('.donate_stats');
        const clearButton = document.getElementById('clearBoundaryShape');

        let activeShape = null;
        let activeMarkers = [];
        let fetchController = null;

        function clearMapMarkers() {
            activeMarkers.forEach(marker => marker.setMap(null));
            activeMarkers = [];
        }

        function renderDonators(rows) {
            if (!donatorsBody) {
                return;
            }

            if (!rows.length) {
                donatorsBody.innerHTML = `
                    <tr>
                        <td colspan="3" class="text-center text-muted py-8">No donors found inside the selected boundary.</td>
                    </tr>
                `;
                return;
            }

            donatorsBody.innerHTML = rows.map(row => `
                <tr>
                    <td class="fw-semibold">${row.rank}</td>
                    <td>
                        <div class="fw-semibold">${row.user_name}</div>
                    </td>
                    <td class="text-end fw-semibold">${row.amount}</td>
                </tr>
            `).join('');
        }

        function renderDonations(rows) {
            if (!donationsBody) {
                return;
            }

            if (!rows.length) {
                donationsBody.innerHTML = `
                    <tr>
                        <td colspan="5" class="text-center text-muted py-8">No donations found inside the selected boundary.</td>
                    </tr>
                `;
                return;
            }

            donationsBody.innerHTML = rows.map(row => `
                <tr>
                    <td class="fw-semibold">${row.rank}</td>
                    <td class="fw-semibold">${row.user_name}</td>
                    <td class="text-end notification-td"><a href="/posts/${row.post_id}">#${row.post_id}</a></td>
                    <td class="text-end fw-semibold">${row.amount}</td>
                    <td>${row.date}</td>
                </tr>
            `).join('');
        }

        function renderMarker(post) {
            const marker = new google.maps.Marker({
                position: { lat: post.lat, lng: post.lng },
                map: statsMap,
                icon: getMarkerIcon(post.status),
                title: `#${post.id}`
            });

            const infoWindow = new google.maps.InfoWindow({
                content: `
                    <div class="map-info-card">
                        <div class="map-info-header">
                            <h6 class="text-center">Post #${post.id}</h6>
                        </div>

                        <div class="map-info-desc">${post.description ?? ''}</div>

                        <div class="stats-grid">
                            <div class="stat-item">
                                <span class="stat-label">User</span>
                                <span class="stat-value">${post.user}</span>
                            </div>
                            <div class="stat-item">
                                <span class="stat-label">Date</span>
                                <span class="stat-value">${post.date}</span>
                            </div>
                            <div class="stat-item">
                                <span class="stat-label">Posted</span>
                                <span class="stat-value">${post.since}</span>
                            </div>
                        </div>

                        <div class="stats-grid">
                            <div class="stat-item">
                                <span class="stat-label">Goal</span>
                                <span class="stat-value" style="color: grey;">${post.cost}</span>
                            </div>
                            <div class="stat-item">
                                <span class="stat-label">Raised</span>
                                <span class="stat-value" style="color: #4CAF50;">${post.totalRaised} : ${post.raisedPercentage}%</span>
                            </div>
                            <div class="stat-item">
                                <span class="stat-label">Remaining</span>
                                <span class="stat-value" style="color: #e74c3c;">${post.remainingAmount}</span>
                            </div>
                        </div>

                        <div class="donors-count text-center">
                            <i class="fas fa-users"></i> <span><strong>${post.peopleDonated}</strong> people have donated</span>
                        </div>
                    </div>
                `
            });

            marker.addListener('click', () => infoWindow.open(statsMap, marker));
            activeMarkers.push(marker);
        }

        async function fetchBoundaryPosts() {
            if (!activeShape) {
                return;
            }

            const path = activeShape.getPath().getArray().map(point => ({
                lat: point.lat(),
                lng: point.lng()
            }));

            if (path.length < 3) {
                return;
            }

            if (fetchController) {
                fetchController.abort();
            }

            fetchController = new AbortController();

            try {
                const response = await axios.post('/Admin/Statistics/MapPostsByBoundary', {
                    coordinates: path
                }, {
                    signal: fetchController.signal
                });

                clearMapMarkers();
                (response.data.posts || []).forEach(renderMarker);

                const summary = response.data.summary ?? {};
                const donors = response.data.donors ?? [];
                const donations = response.data.donations ?? [];

                if (totalCountElement) {
                    totalCountElement.textContent = summary.count ?? 0;
                }

                if (pendingCountElement) {
                    pendingCountElement.textContent = summary.pending ?? 0;
                }

                if (inProcessCountElement) {
                    inProcessCountElement.textContent = summary.in_process ?? 0;
                }

                if (completedCountElement) {
                    completedCountElement.textContent = summary.completed ?? 0;
                }

                if (goalElement) {
                    goalElement.textContent = summary.total_goal ?? 'Rs. 0';
                }

                if (receivedElement) {
                    receivedElement.textContent = summary.total_received ?? 'Rs. 0';
                }

                if (postStatsSection) {
                    postStatsSection.classList.remove('d-none');
                }

                if (donateStatsSection) {
                    donateStatsSection.classList.remove('d-none');
                }

                renderDonators(donors);
                renderDonations(donations);
            } catch (error) {
                if (!axios.isCancel(error)) {
                    console.error(error);
                }
            }
        }

        drawingManager.addListener('overlaycomplete', (event) => {
            if (event.type !== google.maps.drawing.OverlayType.POLYGON) {
                event.overlay.setMap(null);
                return;
            }

            if (activeShape) {
                activeShape.setMap(null);
            }

            activeShape = event.overlay;
            drawingManager.setDrawingMode(null);

            if (clearButton) {
                clearButton.classList.remove('d-none');
            }

            const path = activeShape.getPath();
            path.addListener('insert_at', fetchBoundaryPosts);
            path.addListener('set_at', fetchBoundaryPosts);
            path.addListener('remove_at', fetchBoundaryPosts);

            fetchBoundaryPosts();
        });

        if (clearButton) {
            clearButton.addEventListener('click', () => {
                if (activeShape) {
                    activeShape.setMap(null);
                    activeShape = null;
                }

                clearMapMarkers();

                const summary = {
                    count: 0,
                    totalRaised: 'Rs. 0',
                    pending: 0,
                    inProcess: 0,
                    completed: 0,
                    totalGoal: 'Rs. 0',
                    totalReceived: 'Rs. 0'
                };

                if (totalCountElement) totalCountElement.textContent = summary.count;
                if (pendingCountElement) pendingCountElement.textContent = summary.pending;
                if (inProcessCountElement) inProcessCountElement.textContent = summary.inProcess;
                if (completedCountElement) completedCountElement.textContent = summary.completed;
                if (goalElement) goalElement.textContent = summary.totalGoal;
                if (receivedElement) receivedElement.textContent = summary.totalReceived;
                if (donatorsBody) {
                    donatorsBody.innerHTML = `
                        <tr>
                            <td colspan="3" class="text-center text-muted py-8">Draw a shape to load donor statistics.</td>
                        </tr>
                    `;
                }
                if (donationsBody) {
                    donationsBody.innerHTML = `
                        <tr>
                            <td colspan="5" class="text-center text-muted py-8">Draw a shape to load donation history.</td>
                        </tr>
                    `;
                }

                if (postStatsSection) {
                    postStatsSection.classList.add('d-none');
                }

                if (donateStatsSection) {
                    donateStatsSection.classList.add('d-none');
                }

                if (clearButton) {
                    clearButton.classList.add('d-none');
                }

                drawingManager.setDrawingMode(google.maps.drawing.OverlayType.POLYGON);

            });
        }
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

    window.addEventListener('load', function () {
        if (document.getElementById('admin-statistics-map')) {
            initAdminStatisticsMap();
        }
    });    

});
