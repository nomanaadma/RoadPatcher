```markdown
# Road Patchers
## Featured Detail Documentation (Final)

### FYP Phase 1
### Community-Driven Road Repair Crowdfunding Platform for Pakistan

---

**Project Title:** Road Patchers  
**Academic Year:** 2024-2025  
**Phase:** FYP Phase 1  
**Technology Stack:** ASP.NET Core 10.0, PostgreSQL, PostGIS, Stripe, SignalR, Python (YOLOv26)

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Project Objectives / Outcomes](#2-project-objectives--outcomes)
3. [Technology Stack](#3-technology-stack)
4. [System Architecture](#4-system-architecture)
5. [Detailed Feature Specifications](#5-detailed-feature-specifications)
6. [Detailed Workflow Descriptions](#6-detailed-workflow-descriptions)
7. [Business Rules & Validations](#7-business-rules--validations)
8. [Helpers & Utility Classes](#8-helpers--utility-classes)
9. [Configuration Settings](#9-configuration-settings)
10. [Test Case Categories](#10-test-case-categories)
11. [Diagrams Preparation Guide](#11-diagrams-preparation-guide)
12. [Conclusion](#12-conclusion)

---

## 1. Introduction

In many developing countries, damaged roads remain unattended for long periods due to slow government response, limited budgets, and lack of transparency in maintenance systems. Citizens who face these issues daily have no structured way to report or collectively fund small repairs.  **Road Patchers** aims to bridge this gap by providing a community-driven web platform where people can report road damages, share verified evidence, and crowdfund their repair through public donations.

After signing up, users can upload a report by adding photos or a short video of the damaged road, along with its street location and brief details. Before going live, reports are reviewed by administrators who manually trigger a custom YOLOv26 AI model to process the uploaded images, calculating pothole dimensions, distances, and areas. Once approved, the required repair amount is set, and anyone can contribute via Stripe. Progress is updated in real time.

Once the required amount is achieved, the campaign is marked as **Funded (In Process)**, and evidence of the completed repair can later be attached by administrators or community representatives.  People can comment on or share posts to raise awareness within their neighbourhoods. 

The project encourages civic responsibility, promotes community participation, and introduces accountability in small-scale infrastructure maintenance. By creating a transparent and donation-based ecosystem, Road Patchers transforms passive citizens into active contributors toward safer and better-maintained roads. 

---

## 2. Project Objectives / Outcomes

1. To provide a digital platform that allows citizens to easily report road damage in their locality with photo, video, and location-based evidence. 

2. To create a transparent, community-funded system for small-scale road repairs through online contributions.

3. To visually present repair projects on a map for better local awareness and participation.

4. To automatically calculate and display funding progress and status for each reported case.

5. To enable discussion and sharing of local repair campaigns, encouraging collective civic action.

6. To foster a sense of shared ownership and responsibility in improving public infrastructure.

7. **To implement geolocation-based sorting using PostGIS spatial queries to prioritize posts closest to the user's registered location.**

8. **To enforce mandatory profile completion before allowing user interaction with posts through middleware.**

9. **To restrict platform usage to Pakistan-based locations for focused regional impact.**

10. **To provide secure payment processing with server-side validation to ensure donation integrity.**

11. **To integrate AI-based pothole detection using a YOLOv26 model for automatic calculation of road damage area and volume.**

12. **To implement a secure administrative portal for reviewing posts, analyzing geographical statistics, and managing users.**

13. **To secure user accounts with OAuth (Google, Facebook, X) and Two-Factor Authentication (2FA).**

14. **To gamify user engagement via a leaderboards system and interactive badges.**

15. **To provide real-time updates through SignalR for live toast notifications.**

---

## 3. Technology Stack

### 3.1 Backend Technologies

- **Framework:** ASP.NET Core 10.0 (MVC + Razor Pages)
- **Database:** PostgreSQL with PostGIS extension (spatial/geographic queries)
- **ORM:** Entity Framework Core 10.0 (Code-First approach)
- **Authentication:** ASP.NET Core Identity (with email confirmation)
- **Payment Processing:** Stripe. NET SDK v50. 1.0
- **Spatial Library:** NetTopologySuite (NTS) v2.6.0 for geometric operations (SRID: 4326 - WGS84)
- **Data Serialization:** Newtonsoft.Json with snake_case naming strategy
- **Real-time Web:** SignalR for live toast notifications
- **AI Integration:** Python, CSnakes runtime, OpenCV, Ultralytics YOLOv26 model

### 3.2 Frontend Technologies

- **HTML/CSS/JavaScript** (CSS:  78.1%, JavaScript: 11.9%, HTML: 2.8%)
- **jQuery** for DOM manipulation and AJAX
- **Bootstrap** for responsive UI
- **Swiper.js** for image/video carousels
- **FSLightbox** for media lightbox viewer
- **jQuery Comments** plugin for threaded comment interface
- **Shareon** for social media sharing
- **Stripe.js** for secure payment UI
- **X. PagedList. Mvc. Core** for pagination
- **Razor Views** for pagination

### 3.3 Third-Party APIs

- **Google Maps JavaScript API** for geocoding, places autocomplete, and interactive maps
- **Stripe API** for payment intent creation and processing

### 3.4 Database Features

- **PostGIS Geography Type** for storing location points
- **GIST Spatial Indexes** for efficient proximity queries
- **ST_Distance** for calculating geographic distances
- **ST_Within** for bounding box queries in map search

---

## 4. System Architecture

### 4.1 Application Layers

**RoadPatcher.Application (Class Library)**
- Entities (Domain Models)
- Data Context (ApplicationDbContext)
- Entity Configurations (Fluent API)
- Database Migrations

**RoadPatcher.Web (Web Application)**
- Controllers (MVC Controllers + API Controllers)
- Views (Razor Views)
- Middlewares (ProfileCompletion, Theme)
- Helpers (PostHelper, ProfileHelper, DateHelper, CommentHelper)
- Models (ViewModels, DTOs)
- wwwroot (Static files, uploads)

### 4.2 Database Schema

**Core Entities:**

#### 1. ApplicationUser (ASP.NET Identity)
- Id (string, PK)
- Email, PasswordHash, EmailConfirmed, etc.
- UserData (1:1 relationship)

#### 2. UserData
- UserId (string, PK, FK to ApplicationUser)
- FirstName (string, max 50, required)
- LastName (string, max 50, required)
- Address (string, max 200, required)
- GoogleAddress (string, max 200, required)
- Latitude (double, required)
- Longitude (double, required)
- Image (string, max 200, optional)
- Badge (string, optional - Bronze, Silver, Gold, Platinum, Emerald, Diamond)
- BadgeAwardedDate (DateTime, optional)

#### 3. Post
- Id (int, PK)
- UserId (string, FK to ApplicationUser)
- Address (string, max 255, required)
- GoogleAddress (string, max 255, required)
- Latitude (double)
- Longitude (double)
- Location (Point - PostGIS geography type with GIST index)
- Description (string, max 2000, **optional**)
- Cost (decimal - goal amount)
- Raised (decimal)
- PeopleDonated (int, default 0)
- Likes (int, default 0)
- Discussions (int, default 0) - comment count
- Shares (int, default 0)
- Favourites (int, default 0)
- Status (PostStatus enum: Pending, InProcess, Completed)
- ReviewStatus (ApprovalStatus enum: InReview, Approved, ChangesRequested, Rejected)
- ReviewerId (string, FK to ApplicationUser - assigned admin)
- ImageId (int, FK to Media, required)
- VideoId (int, FK to Media, required)
- CompletionImageId (int?, FK to Media)
- InferenceImageId (int?, FK to Media - generated by AI)
- InferenceData (string, optional - JSON metrics from AI)
- CameraHeightFromGround (decimal)
- CameraToCenterDistance (decimal)
- AllowEdit (bool, default true)
- CreatedAt (DateTime, auto NOW())
- UpdatedAt (DateTime, auto NOW())
- MediaFiles (List<Media> - additional optional files)

#### 4. Media
- Id (int, PK)
- FileName (string, required)
- FilePath (string, required)
- Type (MediaType enum: Image, Video, Other)
- PostId (int?, FK to Post, optional)
- CreatedAt (DateTime)

#### 5. Donation
- Id (int, PK)
- UserId (string, FK to ApplicationUser)
- PostId (int, FK to Post)
- Amount (decimal, required)
- PaymentIntentId (string, unique index)
- Status (DonationStatus enum: Pending=0, Completed, Failed)
- CreatedAt (DateTime)
- CompletedAt (DateTime?)

#### 6. Comment (Threaded/Nested)
- Id (int, PK)
- UserId (string, FK to ApplicationUser)
- PostId (int, FK to Post)
- ParentId (int?, FK to Comment - for replies)
- Content (string, required)
- CreatedAt (DateTime)
- Replies (List<Comment> - self-referential)

#### 7. Like, Favourite, Share (Inherits from StateBase)
- Id (int, PK)
- UserId (string, FK to ApplicationUser)
- PostId (int, FK to Post)
- CreatedAt (DateTime)

### 4.3 Relationships

- **ApplicationUser → UserData:** One-to-One
- **ApplicationUser → Posts:** One-to-Many
- **ApplicationUser → Donations:** One-to-Many
- **ApplicationUser → Comments:** One-to-Many
- **ApplicationUser → Likes/Favourites/Shares:** One-to-Many
- **Post → Media (Image/Video):** Many-to-One (required)
- **Post → MediaFiles:** One-to-Many (optional, CASCADE)
- **Post → Donations:** One-to-Many
- **Post → Comments:** One-to-Many
- **Comment → Replies (Comments):** One-to-Many (self-referential)

---

## 5. Detailed Feature Specifications

### 5.1 User Authentication & Authorization

**Registration & Login:**
- User registers with email and password.
- Email confirmation required (`RequireConfirmedAccount = true`).
- **OAuth Integration:** Sign-in options using Google, Facebook, and Twitter (X).
- **Two-Factor Authentication (2FA):** Support for authenticator apps and recovery codes.
- Session management via ASP.NET Core Identity cookies.
- "Remember Me" option available.
- User redirected to Profile page after successful registration/login.

**Password Management:**
- Change password functionality in Settings
- Requires old password verification
- Password strength validation enforced

**Authorization:**
- All controllers marked with `[Authorize]` attribute
- Unauthenticated users redirected to login

### 5.2 User Profile Management

**Mandatory Fields:**
- First Name
- Last Name
- Address (Custom Address)
- Google Address (geocoded)
- Latitude
- Longitude

**Optional Fields:**
- Profile Image (JPG/JPEG/PNG only, uploaded to `/uploads/users/`)

**Profile Completion Enforcement:**
- `ProfileCompletionMiddleware` runs after authentication
- Checks if `UserData` record exists for logged-in user
- If missing → redirects to `/Settings/EditProfile`
- Blocks access to all pages except: 
  - `/Settings/EditProfile` (allowed)
  - Static assets (`/css`, `/js`, `/lib`, `/images`, `/uploads`)
  - Identity pages (`/Identity`, `/Account`)
  - Error pages

**Address Input System (5 Methods):**

1. **Fetch Auto:**
   - Uses browser's Geolocation API
   - Auto-fills all address fields and map location

2. **Fetch by Filled Address:**
   - User enters custom address manually
   - Clicks "Fetch by Filled Address Above"
   - System geocodes address via Google Maps API
   - Populates Google Address, Lat/Lng, and updates map

3. **Map Autocomplete Search:**
   - User searches location via map's autocomplete bar
   - Map centers on selected location
   - Clicks "Copy to Full Address" to update custom address field

4. **Fetch by Device Location (in map):**
   - Button inside map component
   - Similar to Fetch Auto but doesn't overwrite custom address

5. **Manual Marker Dragging:**
   - User drags map marker to fine-tune location
   - Lat/Lng updates in real-time

**Address Guidelines:**
- System provides step-by-step instructions for best accuracy
- Recommends Google Address over custom address if more accurate
- "Copy to Full Address" syncs Google Address → Custom Address

**Pakistan Boundary Validation:**
- All locations must be within Pakistan
- Error shown for out-of-bounds selections

### 5.3 Post Creation

**Form Fields:**
- **Description:** Text area, max 2000 characters, **optional**
- **Image:** File upload (JPG/JPEG/PNG), **mandatory**
- **Video:** File upload (MP4), **mandatory field** (can upload placeholder)
- **Other Media:** Additional files, **optional**
- **Cost (Goal):** Decimal input (estimated repair cost), **required**
- **Address Block:** Same 5-method system as profile (mandatory)

**Validation:**
- Image and Video are required (enforced via entity configuration)
- Cost must be greater than 0
- Address must be within Pakistan

**Post Creation Flow:**
1. User fills form
2. Files uploaded to `/uploads/posts/` with sanitized filenames
3. Media records created in database
4. Post record created with: 
   - Status = Pending (default)
   - ReviewStatus = InReview (assigned to Admin)
   - Raised = 0
   - PeopleDonated = 0
   - All counters = 0
   - Location = PostGIS Point(Longitude, Latitude) with SRID 4326
5. **AI Inference:** Admin manually triggers YOLOv26 inference (predict.py) which calculates damage area and creates an annotated bounding box image (`InferenceImageId`).
6. **Approval Workflow:** Post requires Admin approval. Once approved, it appears in the Pending list.
7. User is notified via SignalR once the post is approved.

**File Handling:**
- Filenames sanitized (special chars → `-`, lowercase, timestamped)
- Allowed extensions validated server-side
- Old profile images deleted when new one uploaded

### 5.4 Post Listing & Viewing

**Five List Views:**

1. **Pending List:** Posts with Status = Pending (funding < 100%)
2. **In Process List:** Posts with Status = InProcess (funded, awaiting repair)
3. **Completed List:** Posts with Status = Completed (repair done)
4. **Favourites List:** Posts favourited by current user
5. **Donated List:** Posts donated to by current user (Status = Completed donations only)

**Proximity-Based Sorting:**
- All lists sorted by distance from user's profile location (closest first)
- Uses PostGIS `ST_Distance` function: 
  ```csharp
  . OrderBy(p => p.Location.Distance(userLocation))
  ```
- Calculated on database side for performance

**Post Card Display Fields:**
- User's full name (first + last)
- Profile image
- Full Address
- Google Address with location button (opens map popup)
- Latitude & Longitude
- Date posted (formatted as "dd-MMM-yyyy hh:mm tt")
- Time ago ("2 hours ago", "3 days ago")
- Goal amount (formatted as PKR)
- Raised amount (formatted as PKR)
- Remaining amount (formatted as PKR)
- Raised percentage with color-coded progress bar: 
  - 0%: secondary (gray)
  - ≤20%: danger (red)
  - ≤35%: warning (orange)
  - ≤50%: primary (blue)
  - ≤65%: dark (dark gray)
  - ≤80%: info (cyan)
  - >80%: success (green)
- Number of people donated
- Likes count
- Discussions (comments) count
- Shares count
- Favourites count

**Post Actions:**
- **Donate Button:** Opens donation modal with Stripe payment
- **Like Button:** Toggles like (AJAX, increments counter)
- **Comment Button:** Opens comment section
- **Share Button:** Social media sharing options
- **Favourite Button:** Toggles favourite (AJAX, adds to Favourites list)

**Single Post View:**
- Dedicated page at `/Posts/{id}`
- Displays full post details
- Image/video carousel (Swiper.js)
- FSLightbox for media viewing
- Threaded comment section (jQuery Comments plugin)
- Location map (Google Maps)

**Pagination:**
- Configurable page size (default:  5, set in `appsettings.json`)
- Uses X.PagedList library
- Server-side pagination for performance

### 5.5 Donation & Payment System

**Donation Flow:**

1. **User clicks "Donate" button**
   - Modal opens with amount input
   - Shows remaining amount needed

2. **Frontend calls `/Donations/CreatePaymentIntent`:**
   - POST request with `{ postId, amount }`
   - Server-side validations:
     - Amount > 0
     - Amount ≥ Rs. 200 (minimum enforced)
     - Amount ≤ Remaining amount (can't exceed goal)
   - Creates Stripe PaymentIntent with metadata: 
     - Post URL
     - Post # (ID)
     - User full name + email
     - UserId
   - Returns `{ clientSecret, paymentIntentId, amount }`

3. **Frontend displays Stripe Card Element:**
   - User enters card details
   - Stripe. js handles PCI compliance
   - Frontend confirms payment with Stripe

4. **On success, frontend calls `/Donations/ConfirmDonation`:**
   - POST request with `{ postId, paymentIntentId }`
   - Server verifies PaymentIntent status with Stripe
   - Creates Donation record: 
     - UserId (from claims)
     - PostId
     - Amount (from PaymentIntent)
     - PaymentIntentId (unique)
     - Status = Completed
     - CompletedAt = NOW()
   - Updates Post: 
     - `Post. Raised += donation.Amount`
     - `Post.PeopleDonated += 1`
     - If `Post.Raised >= Post.Cost`: `Post.Status = InProcess`
   - Returns success message

5. **Success message displayed to user**
   - Post card updates in real-time (progress bar, amounts, status)

**Security Measures:**
- UserId taken from server-side claims (not client input)
- PaymentIntent verified with Stripe before confirmation
- Unique constraint on PaymentIntentId (prevents duplicate donations)
- Server-side validation of amount constraints
- HTTPS enforced

**Stripe Configuration:**
- API keys in `appsettings.json`:
  - `Stripe: SecretKey` (server-side)
  - `Stripe:PublishableKey` (client-side)
- Test mode keys used

### 5.6 Search & Filter System

#### A. Basic Search

**Route:** `/Posts/Search? s={keyword}`

**Features:**
- Searches in: 
  - Post Description
  - Full Address
  - Google Address
- Uses SQL `LIKE` with `%keyword%` pattern
- Case-insensitive
- Results sorted by proximity
- Pagination enabled

#### B. Advanced Search

**Route:** `/Posts/AdvancedSearch?goalStart=... &goalEnd=...&statuses=...&userIds=...&page=1`

**Filter Parameters:**
- **goalStart / goalEnd:** Goal amount range (decimal)
- **raisedStart / raisedEnd:** Raised amount range (decimal)
- **remainingStart / remainingEnd:** Remaining amount range (decimal)
- **achievedStart / achievedEnd:** Raised percentage range (0-100)
- **donatedByStart / donatedByEnd:** Donor count range (int)
- **userIds:** Multi-select list of user IDs (List<string>)
- **statuses:** Multi-select list of statuses (List<PostStatus>)

**Implementation:**
- Fetches base query with proximity sorting
- Applies DB-level filters (goal, raised, donor count, user, status)
- Applies in-memory filters for calculated fields (remaining, percentage) using `PostHelper`
- Paginates results
- Returns to `PostList` view

#### C. Map Search

**Route:** `/Posts/MapPosts?neLat=...&neLng=...&swLat=...&swLng=...&statuses=...&userids=...`

**Parameters:**
- **neLat, neLng:** Northeast corner of bounding box
- **swLat, swLng:** Southwest corner of bounding box
- **statuses:** Comma-separated status filter (e.g., "Pending,InProcess")
- **userids:** Comma-separated user IDs filter

**Features:**
- Creates PostGIS Polygon from bounding box coordinates using `GeometryFactory`
- Queries posts where `Post.Location. Within(bbox)` using PostGIS `ST_Within`
- Applies status and user filters
- Returns JSON array of marker data: 
  ```json
  [{
    "id": 1,
    "lat": 31.5204,
    "lng": 74.3587,
    "status": "Pending",
    "raisedPercentage": 45. 5,
    "remainingAmount": "Rs. 5,450",
    "cost": "Rs. 10,000",
    "peopleDonated": 3,
    "totalRaised": "Rs. 4,550",
    "date": "15-Dec-2024 03:30 PM",
    "since": "2 days ago",
    "user": "John Doe"
  }]
  ```

**Map Features:**
- Interactive Google Map
- Marker color coding: 
  - **Red:** Pending
  - **Orange:** In Process
  - **Green:** Completed
  - **Blue:** User profile location (when user filter selected)
- Dynamic loading:  Posts reload when map dragged/zoomed
- Marker click:  Shows post details in info window
- Autocomplete search for navigation
- Pakistan boundary enforcement

### 5.7 Comments System (Threaded)

**API Endpoints:**

**1. GET `/Comments/Post/{postId}`**
- Returns all comments for a post
- Ordered by `CreatedAt` ascending
- Includes parent-child relationships via `ParentId`
- Returns CommentDto: 
  ```json
  {
    "id": 1,
    "post_id": 5,
    "parent":  null,
    "fullname": "John Doe",
    "profile_picture_url": "/uploads/users/john-123.jpg",
    "content":  "This road needs urgent repair! ",
    "created":  "2024-12-15T14:30:00Z",
    "total_discussion":  12
  }
  ```

**2. POST `/Comments`**
- Creates new comment or reply
- Request body:
  ```json
  {
    "postId": 5,
    "parentId": null,
    "content": "Comment text"
  }
  ```
- Server extracts `userId` from claims (security)
- Increments `Post.Discussions` counter
- Returns created CommentDto with updated discussion count

**Frontend:**
- Uses jQuery Comments plugin for threaded UI
- Supports unlimited nesting depth
- Real-time updates via AJAX
- Profile images displayed
- Relative timestamps ("2 hours ago")

### 5.8 Like, Favourite, Share

**Implementation:**
- All use `StateBase` abstract class
- AJAX endpoints for toggle actions
- Increment/decrement counters on Post entity
- User-specific tracking via UserId

**Like:**
- API:  `/Posts/ToggleLike/{postId}`
- Updates `Post.Likes` counter
- Toggle behavior: like/unlike

**Favourite:**
- API:  `/Posts/ToggleFavourite/{postId}`
- Creates/deletes `Favourite` record
- Updates `Post.Favourites` counter
- Favourited posts appear in user's Favourites list

**Share:**
- Uses Shareon library for social media sharing
- Supported platforms: Facebook, Twitter, WhatsApp, LinkedIn, etc.
- Updates `Post.Shares` counter
- Share buttons on post cards

### 5.9 Post Status Management

**Status Enum:**
```csharp
public enum PostStatus
{
    Pending = 1,      // Default, funding < 100%
    InProcess = 2,    // Funded (100%), awaiting repair
    Completed = 3,    // Repair verified
}
```

**Status Transitions:**

1. **Create Post → InReview (Admin review required)**
   - Defaults to `InReview` upon creation.
   - Admin approves, requests changes, or rejects.

2. **InReview → Pending (Manual Approval)**
   - Admin manually approves the post. It is then visible for crowdfunding.

3. **Pending → InProcess (Auto, on 100% funding)**
   - Triggered in `/Donations/ConfirmDonation`
   - When `Post.Raised >= Post.Cost`

4. **InProcess → Completed (Admin Verification)**
   - Admin verifies repair.
   - Admin uploads a `CompletionImage`.
   - Admin manually marks the post as Completed.
     ```csharp
     InProcess → Completed
     Completed → InProcess
     ```
   - Displayed as button on post card (only for these statuses)
   - **Note:** This is a temporary feature for testing

**Status Display:**
- Badge color coding on posts
- Filters in list views
- Map marker colors

### 5.10 Settings & Profile Edit

**Routes:**
- `/Settings/Index` - Settings dashboard
- `/Settings/EditProfile` - Edit profile (GET/POST)
- `/Settings/ChangePassword` - Change password (GET/POST)

**Edit Profile:**
- Pre-fills form with existing UserData
- Shows current profile image
- Same address input system (5 methods)
- Allows image change (old image deleted)
- **Insert or Update logic:**
  - If UserData exists → Update record
  - If missing → Insert new record
- Redirects to Pending page on success

**Change Password:**
- Requires old password
- ASP.NET Identity password validation (strength, etc.)
- Refreshes sign-in on success
- Shows success message

### 5.11 Admin Portal & User Management

- **User Listing (`/Admin/Users`):** SuperAdmins and Admins can view all users, search, and filter by roles.
- **Role Management:** Ability to assign Admin or User roles (SuperAdmin restricted).
- **User Lockout:** Toggle user access to temporarily or permanently ban accounts.

### 5.12 Statistics & Map Analysis

- **Interactive Map (`/Admin/Statistics`):** Admins draw polygons (3+ points) on a Google Map.
- **Spatial Queries:** Uses PostGIS (`Intersects`) to fetch posts within the drawn boundary.
- **Aggregated Stats:** Displays total raised, goals, status counts, top donors (ranked by amount), and recent donations specific to the selected region.

### 5.13 Leaderboard & User Badges

- **Gamification:** Tracks top contributors and ranks them based on their total cumulative donations.
- **Badge Tiers:** Users are automatically awarded unique SVG Badges displayed beside their usernames based on their lifetime donation amounts:
  - **Bronze:** ≥ Rs. 1,000
  - **Silver:** ≥ Rs. 5,000
  - **Gold:** ≥ Rs. 10,000
  - **Platinum:** ≥ Rs. 25,000
  - **Emerald:** ≥ Rs. 50,000
  - **Diamond:** ≥ Rs. 100,000
- **Award Notification:** When a user's total donations push them into a new tier, their `Badge` and `BadgeAwardedDate` fields are updated in `UserData`, and a SignalR notification is sent to congratulate them on their new badge.

### 5.14 Notification System

- **SignalR Real-time Hub:** Delivers toast notifications instantly to users.
- **Events Tracked:** "Post Approved", "Changes Requested", "Donation Received", "Post Completed".
- **Dashboard (`/Notifications`):** Dedicated page listing historical alerts with read/unread statuses.

---

## 6. Detailed Workflow Descriptions

### 6.1 New User Onboarding Workflow

1. User visits site → Redirected to Login page (not authenticated)
2. Clicks "Register" → Registration form
3. Enters email + password → Submits
4. Email confirmation sent (if configured)
5. User confirms email → Redirected to Login
6. Logs in → Authenticated
7. ProfileCompletionMiddleware checks UserData
8. No UserData found → Redirected to `/Settings/EditProfile`
9. User fills mandatory fields (First Name, Last Name, Address)
10. Chooses address input method (Fetch Auto recommended)
11. Optionally uploads profile image
12. Submits → UserData record created
13. Redirected to `/Posts/Pending` (default home page)
14. Middleware allows access to all pages now

### 6.2 Post Creation Workflow

1. User clicks "Create Post" button (navigation)
2. Form displays with fields: 
   - Description (optional text area)
   - Image upload (required)
   - Video upload (required)
   - Other media (optional)
   - Cost input (required)
   - Address block (required)
3. User fills description
4. Uploads image (JPG/PNG) and video (MP4)
5. Enters estimated repair cost (e.g., 10000 PKR)
6. Uses address input method: 
   - Option A: Fetch Auto (device location)
   - Option B: Type address → Fetch by Filled Address
   - Option C: Search on map → Copy to Full Address
   - Option D: Drag marker to fine-tune
7. Reviews location on embedded map
8. Clicks "Submit"
9. Server validation:
   - Image & Video required
   - Cost > 0
   - Address within Pakistan
10. Files uploaded to `/uploads/posts/` with sanitized names
11. Media records created (Image, Video)
12. Post record created:
    - Status = Pending
    - ReviewStatus = InReview
    - Location = PostGIS Point
    - ImageId, VideoId linked
13. Post assigned to least loaded Admin for review.
14. User notified via SignalR once the post is approved.
15. New post appears at top (sorted by proximity to user's location)

### 6.3 Donation Workflow

1. User browses Pending list
2. Finds post with damaged road near their area
3. Clicks "Donate" button on post card
4. Modal opens showing: 
   - Post details (address, goal, raised, remaining)
   - Amount input field
   - Stripe Card Element
5. User enters donation amount (e.g., 500 PKR)
6. Frontend validates: 
   - Amount ≥ 200 (client-side hint)
   - Amount ≤ Remaining
7. Frontend calls `/Donations/CreatePaymentIntent`:
   - POST `{ postId:  5, amount: 500 }`
8. Server validates:
   - Amount ≥ 200 (enforced)
   - Amount ≤ Remaining (enforced)
   - Post exists
9. Server creates Stripe PaymentIntent:
   - Amount:  50000 (in paisas, *100)
   - Currency: PKR
   - Metadata: Post#, User, URL
10. Server returns `{ clientSecret, paymentIntentId }`
11. User enters card details in Stripe UI
12. Stripe validates card
13. User clicks "Pay"
14. Stripe processes payment
15. On success, frontend calls `/Donations/ConfirmDonation`:
    - POST `{ postId: 5, paymentIntentId: "pi_xxx" }`
16. Server verifies PaymentIntent with Stripe API
17. Server creates Donation record:
    - UserId (from claims)
    - PostId: 5
    - Amount: 500
    - PaymentIntentId:  "pi_xxx"
    - Status: Completed
    - CompletedAt: NOW()
18. Server updates Post:
    - `Post.Raised += 500`
    - `Post.PeopleDonated += 1`
19. Server checks if `Post.Raised >= Post.Cost`:
    - If YES: `Post.Status = InProcess`
20. Server returns success response
21. Frontend shows success message
22. Post card updates in real-time: 
    - Progress bar advances
    - Raised amount increases
    - Remaining decreases
    - Donor count increments
    - If 100%: Status badge changes to "In Process"
23. Post appears in user's Donated list

### 6.4 Map Search Workflow

1. User clicks "Map Search" in navigation
2. Google Map loads centered on user's profile location
3. Status filter displayed (Pending/InProcess/Completed)
4. User filter displayed (multi-select of users)
5. Map visible area calculated (NE and SW bounds)
6. Frontend calls `/Posts/MapPosts`:
   - GET `?neLat=31.6&neLng=74.5&swLat=31.4&swLng=74.2&statuses=Pending,InProcess`
7. Server creates bounding box Polygon from coordinates
8. Server queries: `Post.Location.Within(bbox)`
9. Server applies status filter:  `Status IN (Pending, InProcess)`
10. Server returns JSON array of posts
11. Frontend renders markers on map: 
    - Red markers: Pending
    - Orange markers: InProcess
    - Green markers: Completed
12. User drags map to new area
13. New bounds calculated
14. Frontend calls `/Posts/MapPosts` with new bounds
15. Markers update dynamically
16. User clicks on red marker (Pending post)
17. Info window opens with post summary: 
    - Address
    - Goal, Raised, Remaining
    - User name
    - Date
    - "View Details" link
18. User clicks "View Details" → Redirected to `/Posts/{id}`
19. User selects "User Filter" → Adds specific user
20. Blue marker appears at selected user's profile location
21. Only posts by that user shown on map
22. User uses map's autocomplete search
23. Types "Lahore" → Suggestions appear
24. Selects "Lahore, Pakistan"
25. Map centers on Lahore
26. Posts in Lahore area loaded
27. User tries to navigate outside Pakistan
28. Error message:  "Location must be within Pakistan"
29. Map reset to previous valid location

### 6.5 Advanced Search Workflow

1. User clicks "Advanced Search" in navigation
2. Form displays with multiple filter fields: 
   - Goal amount range (Start - End)
   - Raised amount range
   - Remaining amount range
   - Achievement percentage range (0-100)
   - Donor count range
   - Users (multi-select dropdown)
   - Status (multi-select:  Pending/InProcess/Completed)
3. User fills desired filters: 
   - Goal:  5000 - 15000 PKR
   - Achievement: 20% - 60%
   - Status: Pending, InProcess
4.  Clicks "Search"
5. Frontend submits GET request: 
   - `/Posts/AdvancedSearch?goalStart=5000&goalEnd=15000&achievedStart=20&achievedEnd=60&statuses=Pending&statuses=InProcess&page=1`
6. Server fetches base query (proximity-sorted)
7. Server applies DB-level filters:
   - `Post.Cost BETWEEN 5000 AND 15000`
   - `Post.Status IN (Pending, InProcess)`
8. Server loads results into memory
9. Server applies calculated filters:
   - `GetRaisedPercentage(Cost, Raised) BETWEEN 20 AND 60`
10. Server paginates (page 1, size 5)
11. Returns PostList view with results
12. User sees filtered posts sorted by proximity
13. Pagination controls at bottom
14. User clicks "Page 2"
15. Request:  `... &page=2`
16. Next 5 results displayed

### 6.6 Comment & Discussion Workflow

1. User views Single Post page (`/Posts/5`)
2. Comment section loads below post
3. Frontend calls `/Comments/Post/5`
4. Server returns all comments for post (JSON)
5. jQuery Comments plugin renders threaded UI: 
   - Top-level comments
   - Nested replies indented
6. User types comment:  "This road is near my house!"
7. Clicks "Post Comment"
8. Frontend calls POST `/Comments`:
   - `{ postId: 5, parentId: null, content: "This road is near my house!" }`
9. Server extracts userId from claims
10. Server validates:  postId exists, content not empty
11. Server creates Comment record:
    - UserId:  "user123"
    - PostId: 5
    - ParentId: null (top-level)
    - Content: "This road is near my house!"
    - CreatedAt: NOW()
12. Server updates `Post.Discussions += 1`
13. Server returns CommentDto with: 
    - User's full name
    - Profile image
    - Comment content
    - Timestamp
    - Total discussion count
14. Frontend appends new comment to UI
15. Discussion count on post card updates
16. Another user views the post
17. Sees new comment
18. Clicks "Reply" on the comment
19. Types:  "Same here!  Let's donate together."
20. Frontend calls POST `/Comments`:
    - `{ postId: 5, parentId: 12, content: "Same here! Let's donate together." }`
21. Server creates Comment with ParentId:  12
22. Server increments `Post.Discussions`
23. Frontend renders reply nested under parent comment
24. Nested structure visible (indented, with connector lines)
25. User can continue nesting (no depth limit)

---

## 7. Business Rules & Validations

### 7.1 Profile Rules

- First Name, Last Name, Address:  **Mandatory**
- Profile Image: **Optional**
- Address must be **within Pakistan**
- User **cannot access any page** (except EditProfile, static assets, Identity pages) until profile is complete
- ProfileCompletionMiddleware enforces this rule

### 7.2 Post Creation Rules

- **Image:** Mandatory (JPG/JPEG/PNG)
- **Video:** Mandatory field (can be placeholder/empty file)
- **Description:** Optional (max 2000 characters)
- **Cost (Goal):** Mandatory, must be > 0
- **Address:** Mandatory, must be within Pakistan
- **Approval Workflow:** Posts require Admin review (`InReview`). Posts are assigned to an admin upon creation.
- **Status:** Defaults to Pending. `ReviewStatus` defaults to `InReview`.

### 7.3 Donation Rules

- **Minimum amount:** Rs. 200 (enforced server-side)
- **Maximum amount:** Remaining amount to reach goal (can't exceed)
- **No donations** allowed if post is already 100% funded
- **UserId** must be taken from server-side claims (not client input)
- **PaymentIntent** must be verified with Stripe before confirming
- **Unique constraint** on PaymentIntentId (prevents duplicate donations from same Stripe transaction)

### 7.4 Status Transition Rules

- **Create Post:** Status = Pending (default)
- **100% Funded:** Status auto-changes to InProcess
- **Manual toggle:** Only between InProcess ↔ Completed (temporary feature)
- **Pending cannot** be manually changed to InProcess
- **Completed posts** should have evidence attached (future feature)

### 7.5 Search Rules

- **Basic Search:** Searches Description, Address, Google Address (case-insensitive)
- **Advanced Search:** All filters optional (empty = no filter applied)
- **Map Search:** Dynamic bounding box (loads posts in visible area only)
- **All searches:** Results sorted by proximity to user's profile location

### 7.6 Spatial Data Rules

- All locations stored as **PostGIS Point** with **SRID 4326** (WGS84 lat/lng)
- **GIST spatial index** on Post.Location for efficient queries
- **ST_Distance** used for proximity sorting
- **ST_Within** used for bounding box queries
- **Pakistan boundary** enforced (lat/lng validation)

### 7.7 Comment Rules

- **Content:** Required (not empty)
- **Threading:** Unlimited nesting depth via ParentId
- **Discussions counter:** Incremented for every comment (top-level or reply)
- **UserId:** Taken from server-side claims
- **Order:** Oldest first (CreatedAt ASC)

### 7.8 Security Rules

- **Authentication:** Required for all pages (except public assets, Identity, errors)
- **Authorization:** `[Authorize]` attribute on all controllers
- **Antiforgery Tokens:** Required on all POST actions
- **Email Confirmation:** Required for login
- **HTTPS:** Enforced in production
- **Query Tracking:** Disabled by default for read queries (performance)
- **Explicit Tracking:** Enabled only when updating entities (`.AsTracking()`)

---

## 8. Helpers & Utility Classes

### 8.1 PostHelper. cs

- **GetRaisedPercentage(cost, raised):** Calculates % raised, rounded to 2 decimals
- **GetRemaining(cost, raised):** Calculates remaining amount
- **GetProgressClass(percentage):** Returns Bootstrap color class for progress bar
- **GetTooltipBoxPercentage(raisedPercentage):** Adjusts tooltip position (29%-71% range)
- **InvertStatus(status):** Toggles InProcess ↔ Completed
- **RenderBtn(status, postId):** Generates HTML for status toggle button

### 8.2 ProfileHelper.cs

- **GetProfileImage(fileName):** Returns `/uploads/users/{fileName}` or default `/img/profile. png`
- **GetFullName(userData):** Concatenates FirstName + LastName

### 8.3 DateHelper.cs

- **ToPrettyFormat(date):** Formats as "dd-MMM-yyyy hh:mm tt" (e.g., "15-Dec-2024 03:30 PM")
- **ToAgo(date):** Converts to relative time: 
  - < 1 min: "Just now"
  - < 24 hrs: "X hours ago"
  - < 7 days: "X days ago"
  - < 28 days: "X weeks ago"
  - < 365 days: "X months ago"
  - ≥ 365 days:  "X years ago"

### 8.4 CommentHelper.cs

- **ToDto(comment):** Maps Comment entity to CommentDto for API responses
- Includes user full name, profile image, formatted dates

### 8.5 CurrencyHelper.cs (Extension)

- **ToPkr(amount):** Formats decimal as "Rs. X,XXX" with comma separators

---

## 9. Configuration Settings

### 9.1 appsettings.json

```json
{
  "ConnectionStrings": {
    "DBType": "postgres",
    "DefaultConnection": "Server=localhost;Port=5432;Database=roadpatcher;User ID=postgres;Password=proot;"
  },
  "Paging": {
    "PageSize": 5
  },
  "GoogleMaps": {
    "ApiKey":  "AIzaSyD..."
  },
  "Stripe": {
    "SecretKey": "sk_test_51.. .",
    "PublishableKey": "pk_test_51..."
  }
}
```

### 9.2 Program.cs Key Configurations

- **DbContext:** `UseNpgsql` with `UseNetTopologySuite()`
- **Query Tracking:** `NoTracking` by default
- **Identity:** `RequireConfirmedAccount = true`
- **JSON Serialization:** `snake_case` naming strategy
- **Static Files:** Custom MIME type for `.mp4` (video/mp4)
- **Middlewares Order:**
  1.  Routing
  2. **Authentication**
  3. **ProfileCompletionMiddleware** ← Custom
  4. Authorization
  5. ThemeMiddleware
- **GeometryFactory:** Singleton with SRID 4326

---

## 10. Test Case Categories

### 10.1 Authentication & Authorization

- **TC001:** Successful registration with valid data
- **TC002:** Email confirmation required before login
- **TC003:** Login with valid credentials
- **TC004:** Login with invalid credentials (error)
- **TC005:** Unauthenticated user redirected to login
- **TC006:** Authenticated user can access protected pages

### 10.2 Profile Completion Enforcement

- **TC007:** New user without UserData redirected to EditProfile
- **TC008:** EditProfile page accessible without UserData
- **TC009:** Static assets accessible without UserData
- **TC010:** Identity pages accessible without UserData
- **TC011:** Other pages blocked until profile complete
- **TC012:** After profile completion, all pages accessible

### 10.3 Profile Management

- **TC013:** Edit profile with all mandatory fields (success)
- **TC014:** Edit profile missing First Name (validation error)
- **TC015:** Upload profile image (JPG/PNG) (success)
- **TC016:** Upload invalid image format (error)
- **TC017:** Address within Pakistan (success)
- **TC018:** Address outside Pakistan (error)
- **TC019:** Fetch Auto populates all fields correctly
- **TC020:** Fetch by Filled Address geocodes correctly
- **TC021:** Map Autocomplete search finds location
- **TC022:** Manual marker drag updates lat/lng

### 10.4 Post Creation

- **TC023:** Create post with all fields (success)
- **TC024:** Create post without image (validation error)
- **TC025:** Create post without video (validation error)
- **TC026:** Create post with optional description (success)
- **TC027:** Create post without description (success)
- **TC028:** Create post with cost = 0 (validation error)
- **TC029:** Create post with Pakistan location (success)
- **TC030:** Create post with non-Pakistan location (error)
- **TC031:** Post created with InReview status and hidden from public feed

### 10.5 Post Listing & Sorting

- **TC032:** Pending list displays only Pending posts
- **TC033:** InProcess list displays only InProcess posts
- **TC034:** Completed list displays only Completed posts
- **TC035:** Posts sorted by proximity to user location
- **TC036:** Favourites list shows only favourited posts
- **TC037:** Donated list shows only donated posts
- **TC038:** Pagination works correctly

### 10.6 PostGIS & Proximity

- **TC039:** Posts sorted correctly by ST_Distance
- **TC040:** Proximity calculation accurate for 1km distance
- **TC041:** Proximity calculation accurate for 100km distance
- **TC042:** GIST spatial index improves query performance

### 10.7 Donation & Payment

- **TC043:** Successful donation with valid amount
- **TC044:** Donation below Rs. 200 rejected
- **TC045:** Donation above remaining amount rejected
- **TC046:** Donation updates Post.Raised correctly
- **TC047:** Donation increments Post.PeopleDonated
- **TC048:** 100% funding auto-changes status to InProcess
- **TC049:** PaymentIntent verification with Stripe succeeds
- **TC050:** Duplicate PaymentIntentId rejected (unique constraint)
- **TC051:** Donation appears in user's Donated list
- **TC052:** Success message displayed to user

### 10.8 Search & Filter

- **TC053:** Basic search finds posts by description keyword
- **TC054:** Basic search finds posts by address keyword
- **TC055:** Basic search case-insensitive
- **TC056:** Advanced search filters by goal range
- **TC057:** Advanced search filters by raised range
- **TC058:** Advanced search filters by percentage range
- **TC059:** Advanced search filters by donor count
- **TC060:** Advanced search filters by status (multi-select)
- **TC061:** Advanced search filters by users (multi-select)
- **TC062:** Map search loads posts in bounding box
- **TC063:** Map search updates when map dragged
- **TC064:** Map markers color-coded by status

### 10.9 Comments & Threading

- **TC065:** Create top-level comment (success)
- **TC066:** Create reply to comment (success)
- **TC067:** Nested replies display correctly (3+ levels)
- **TC068:** Discussions counter increments correctly
- **TC069:** Comments ordered by CreatedAt ASC
- **TC070:** Comment content required (validation)

### 10.10 Like, Favourite, Share

- **TC071:** Like post increments counter
- **TC072:** Unlike post decrements counter
- **TC073:** Favourite post adds to Favourites list
- **TC074:** Unfavourite removes from list
- **TC075:** Share increments counter
- **TC076:** Social media share opens correct platform

### 10.11 Post Status Management

- **TC077:** New post has Pending status (default)
- **TC078:** 100% funded post auto-transitions to InProcess
- **TC079:** Manual toggle InProcess → Completed
- **TC080:** Manual toggle Completed → InProcess
- **TC081:** Pending cannot be manually toggled

### 10.12 Security

- **TC082:** Antiforgery token required on POST
- **TC083:** UserId from claims (not client input)
- **TC084:** Unauthorized access to [Authorize] pages denied
- **TC085:** HTTPS enforced in production
- **TC086:** Email confirmation prevents login

### 10.13 Address Input System

- **TC087:** Fetch Auto uses device location correctly
- **TC088:** Fetch by Filled Address geocodes manually entered address
- **TC089:** Map Autocomplete finds location by name
- **TC090:** Copy to Full Address syncs fields correctly
- **TC091:** Manual marker drag updates coordinates
- **TC092:** Pakistan boundary validation works on all methods

---

## 11. Diagrams Preparation Guide

### 11.1 Use Case Diagram - Actors

- **Primary Actor:** Citizen (Registered User)
- **Secondary Actors:**
  - Payment Gateway (Stripe)
  - Google Maps API
  - Email Service (for confirmation)

### 11.2 Use Case Diagram - Use Cases

- Register Account
- Confirm Email
- Login
- Complete Profile
- Edit Profile
- Change Password
- Create Post (Report Road Damage)
- View Post Lists (Pending/InProcess/Completed/Favourites/Donated)
- View Single Post
- Search Posts (Basic/Advanced/Map)
- Donate to Post
- Like Post
- Comment on Post
- Reply to Comment
- Favourite Post
- Share Post
- View Map Search
- Filter Map by Status/User
- Toggle Post Status (Temporary Feature)

### 11.3 Class Diagram - Main Classes

- ApplicationUser
- UserData
- Post
- Media
- Donation
- Comment
- Like
- Favourite
- Share
- StateBase (abstract)
- PostStatus (enum)
- DonationStatus (enum)
- MediaType (enum)

### 11.4 Sequence Diagrams - Key Flows

- User Registration & Profile Completion
- Post Creation with Geocoding
- Donation with Stripe Payment
- Map Search with Bounding Box Query
- Comment Creation with Discussion Count Update
- 100% Funding Status Transition

### 11.5 Activity Diagrams

- Address Input Decision Tree (5 methods)
- Donation Payment Flow (Stripe integration)
- Search Decision Flow (Basic vs Advanced vs Map)
- Status Transition Logic (Pending → InProcess → Completed)

### 11.6 ERD (Entity Relationship Diagram)

- All entities with attributes
- Relationships with cardinality
- Primary/Foreign keys
- Indexes (especially GIST spatial)

---

## 12. Conclusion

Road Patchers FYP Phase 1 is a fully functional, production-ready community-driven platform that successfully demonstrates: 

✅ **Modern Tech Stack:** ASP.NET Core 10.0, PostgreSQL, PostGIS  
✅ **Spatial Features:** Proximity-based sorting, bounding box queries, GIST indexes  
✅ **Secure Payments:** Stripe integration with server-side validation  
✅ **User Experience:** Profile completion enforcement, threaded comments, real-time updates  
✅ **Search Capabilities:** Basic, Advanced, and Map search with dynamic loading  
✅ **Clean Architecture:** Separation of concerns (Application layer, Web layer), helper classes, middlewares  
✅ **Security:** Authentication, authorization, antiforgery tokens, HTTPS  
✅ **Scalability:** Efficient DB queries, pagination, spatial indexing

**This documentation provides a comprehensive foundation for:**
- Creating UML diagrams (Use Case, Class, Sequence, Activity, ERD)
- Writing detailed test cases and test plans
- Developing user manuals and system documentation
- Presenting to evaluation committees and stakeholders
- Planning future enhancements and Phase 2 development

**Key Achievements:**
- Community empowerment through transparent crowdfunding
- Real-time funding progress tracking
- Geographic awareness with proximity-based sorting
- Secure payment processing with Stripe
- Scalable spatial queries with PostGIS
- User-friendly interface with multiple address input methods
- Robust middleware for profile completion enforcement

---

**Document Version:** 1.0  
**Last Updated:** December 30, 2025  
**Status:** Final - Ready for Submission

---
```
