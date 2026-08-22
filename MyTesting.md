# Sohba Complete Manual Testing Master Plan

## قواعد الاختبار

أنت تختبر المشروع كمستخدم حقيقي + QA + Developer في نفس الوقت.

في كل Test مهم، افحص 3 مستويات:

1. **Frontend**

   * هل الـ UI تصرف بشكل صحيح؟
   * هل الرسالة مناسبة؟
   * هل العناصر تتحدث بدون Refresh عندما يفترض ذلك؟
   * هل الـ JS يعمل بدون Console Errors؟

2. **Backend**

   * هل Request وصل للـ Controller الصحيح؟
   * هل Authorization و Validation شغالين؟
   * هل Business Rules تطبق؟
   * هل Response صحيح؟

3. **Database**

   * هل البيانات اتخزنت كما يجب؟
   * هل اتعدلت أو اتحذفت؟
   * هل العلاقات والـ FK صحيحة؟
   * هل الـ soft delete والـ query filters يعملان؟

بالنسبة للـ Critical flows، لا تعتبر الاختبار Passed إلا بعد التأكد من المستويات الثلاثة.

---

# PHASE 0 - Test Environment & Baseline

## الهدف

التأكد أن البيئة نفسها سليمة قبل اختبار Features.

### 0.1 Clean Start

* [x] أغلق التطبيق.
* [x] شغله من جديد.
* [x] تأكد أن التطبيق يعمل بدون Startup Exception.
* [x] تأكد أن migrations تطبق بدون error.
* [x] تأكد أن Database موجودة.
* [x] تأكد أن Seed Data موجودة.
* [x] تأكد أن Roles موجودة.
* [x] تأكد أن Admin موجود.
* [x] افتح الموقع من Browser جديد.

### Expected

* Application يبدأ طبيعي.
* لا توجد exceptions في Startup.
* لا توجد migration errors.
* لا توجد DI errors.
* لا توجد database connection errors.

### 0.2 Logs

افتح:

`logs/sohba-*.log`

اختبر:

* [x] Startup
* [x] Login
* [x] Create Post
* [x] Error intentionally
* [x] Notification

راجع:

* هل الأخطاء تتسجل؟
* هل Stack Trace موجود في Server فقط؟
* هل المستخدم لا يرى Stack Trace؟

### 0.3 Browser Console

افتح DevTools:

`Console`

ثم مر على:

* Landing 
* Login
* Home
* Profile
* Posts
* Friends
* Stories
* Groups
* Pages
* Notifications
* Search
* Dashboard

سجل أي:

* Error
* Unhandled Promise
* Failed Fetch
* 404 JS/CSS
* SignalR connection error

---

# PHASE 1 - DATABASE & EF CORE

## الهدف

اختبار Infrastructure والـ persistence بشكل فعلي.

## 1.1 Schema Verification

في SQL Server تحقق من وجود:

* [x] AspNetUsers
* [x] AspNetRoles
* [x] AspNetUserRoles
* [x] Posts
* [x] Comments
* [x] Reactions
* [x] Hashtags
* [x] PostHashtags
* [x] PostReports
* [x] SavedPost
* [x] SavedCollections
* [x] Stories
* [x] StoryViewer
* [x] Friends
* [x] Notification
* [x] Groups
* [x] GroupMembers
* [x] Pages
* [x] PageFollowers

## 1.2 Identity

اختبر:

* [x] User موجود
* [x] Admin موجود
* [x] Roles صحيحة
* [x] User مربوط بالـ User role
* [x] Admin مربوط بالـ Admin role

## 1.3 Foreign Keys

اختبر العلاقات التالية عمليًا:

* User → Post
* User → Comment
* User → Reaction
* User → Story
* Post → Comment
* Post → Reaction
* Post → SavedPost
* Post → Report
* Story → StoryViewer
* Group → GroupMember
* Page → PageFollower

### حاول عمل عمليات تسبب FK conflict

مثل:

* حذف User لديه Posts
* حذف Post عليه Comments
* حذف Post محفوظ
* حذف Group لديه Members

### Expected

النظام لا ينهار، ولا ينتج orphaned records.

## 1.4 Soft Delete

اختبر:

1. أنشئ Post.
2. احذفه.
3. افتح Home.
4. افتح Profile.
5. Search.
6. Hashtag.
7. Details مباشرة.

### Expected

Post يختفي من كل الـ normal queries.

ثم راجع DB:

* [ ] Record ما زال موجود
* [ ] `IsDeleted = true`

كرر نفس الاختبار مع:

* User
* Story

## 1.5 SavedPost PostId1

راجع DB schema والـ actual rows.

تحقق من:

* [x] PostId
* [x] PostId1
* [x] هل EF يستخدم الاثنين؟
* [x] هل saving/removing post يتعامل مع FK الصحيح؟
* [x] لا توجد orphan records

هذا Test مستقل لأن التقرير حددها كـ risk.

---

# PHASE 2 - AUTHENTICATION

## 2.1 Registration

اختبر:

* [ ] Valid registration
* [ ] Empty name
* [ ] Invalid email
* [ ] Existing email
* [ ] Weak password
* [ ] Password without uppercase
* [ ] Password without lowercase
* [ ] Password without digit
* [ ] Password under minimum length
* [ ] Mismatched password
* [ ] Duplicate submission

### تحقق في DB

بعد successful registration:

* [ ] User موجود
* [ ] Email صحيح
* [ ] UserName صحيح
* [ ] User role موجود
* [ ] Password hash موجود
* [ ] لا يوجد plain password

## 2.2 Login

اختبر:

* [x] Correct email/password
* [x] Wrong password
* [x] Unknown email
* [x] Empty fields
* [x] Remember Me
* [x] Logout
* [x] Access protected page after logout

## 2.3 Lockout

من نفس الحساب:

* [ ] نفذ 5 محاولات Password غلط
* [ ] حاول الدخول بالـ correct password

### Expected

الحساب يدخل Lockout حسب الإعداد.

ثم اختبر بعد انتهاء المدة.

## 2.4 Authorization

بدون Login:

جرب مباشرة:

* `/Home` - Done✅
* `/Posts/Create` - Done✅
* `/Profile` - Done✅
* `/Friends` - Done✅
* `/Groups` - Done✅
* `/Pages` - Done✅
* `/Notifications` - Done✅
* `/Search` - Done✅
* `/Dashboard` - Done✅

### Expected

كل protected endpoint يمنع الوصول.

## 2.5 Admin Authorization

User عادي يحاول:

* Dashboard - Done✅
* Delete User - Done✅
* Delete Post - Done✅
* Resolve Report - Done✅
* Block User from Dashboard - Done✅

### Expected

كلهم Denied.

---

# PHASE 3 - PASSWORD RESET

## 3.1 Forgot Password

اختبر:

* [ ] Existing email
* [ ] Unknown email
* [ ] Invalid email
* [ ] Empty email

### Expected

لا يتم كشف هل الحساب موجود أم لا.

## 3.2 Mailtrap

للحساب الصحيح:

* [ ] Email يصل
* [ ] Reset link صحيح
* [ ] Link opens
* [ ] Reset works

## 3.3 Reset Abuse

اختبر:

* Expired token
* Invalid token
* Modified token
* Reuse old token
* Password mismatch

---

# PHASE 4 - HOME / FEED

## 4.1 Home

بعد Login:

* [ ] Feed يظهر
* [ ] Stories تظهر
* [ ] Trending hashtags تظهر
* [ ] Recommended groups تظهر
* [ ] Posts مرتبة بشكل صحيح

## 4.2 Pagination

اختبر:

* page 1
* page 2
* page 3
* no more posts

راقب:

* [ ] Duplicate posts
* [ ] Missing posts
* [ ] Wrong order
* [ ] Infinite loop

## 4.3 Infinite Scroll

* [ ] Scroll slowly
* [ ] Scroll quickly
* [ ] Reach bottom repeatedly
* [ ] Load More
* [ ] Reload

### Expected

لا يوجد duplicate post.

## 4.4 Privacy Matrix

استخدم أكثر من User.

أنشئ:

| Post    | Owner      | Friend                       | Non-Friend |
| ------- | ---------- | ---------------------------- | ---------- |
| Public  | يجب أن يرى | يجب أن يرى                   | يجب أن يرى |
| Friends | يرى        | يرى                          | لا يرى     |
| Private | يرى        | حسب الـ actual business rule | لا يرى     |

اختبر:

* Feed
* Profile
* Search
* Hashtag
* Direct Details URL

هذه من أهم الاختبارات في المشروع كله.

---

# PHASE 5 - POSTS

## 5.1 Create Post

اختبر:

* [ ] Text only
* [ ] Image only
* [ ] Text + image
* [ ] Empty post
* [ ] Very long title
* [ ] Very long content
* [ ] Invalid privacy
* [ ] Special characters
* [ ] HTML
* [ ] Script payload
* [ ] Emoji
* [ ] Arabic
* [ ] English
* [ ] Mixed Arabic/English
* [ ] Hashtag
* [ ] Multiple hashtags
* [ ] Duplicate hashtags

## 5.2 Hashtag Creation

أنشئ:

`#dotnet #aspnet #csharp`

ثم تحقق:

* [ ] Hashtags created
* [ ] Correct PostHashtag rows
* [ ] Count updated
* [ ] Trending updated

## 5.3 Post Ownership

User A creates Post.

User B tries:

* Edit
* Delete
* Hide
* Change anything

### Expected

Denied.

## 5.4 Admin Post Control

Admin tries:

* Delete another user's post
* Hide another user's post

هذا Test مهم جدًا بسبب التقرير.

### Expected

Admin behavior يجب أن يعمل فعليًا، وليس فقط أن يكون مسموحًا نظريًا.

---

# PHASE 6 - POST PRIVACY BUG HUNT

## الهدف

اختبار التضارب بين:

`IsPrivate`

و

`Privacy`

نفذ الآتي:

1. Create Post باستخدام Public.
2. راجع DB.
3. راجع `IsPrivate`.
4. راجع `Privacy`.
5. اختبر visibility من Friend.
6. اختبر visibility من Non-Friend.
7. كرر مع Friends.
8. كرر مع Private.
9. اختبر Search.
10. اختبر Profile.
11. اختبر Details direct URL.

### FAIL CONDITIONS

أي حالة:

* Public يظهر لمستخدم غير مسموح
* Private يظهر لمن لا يجب أن يراه
* UI يقول شيء والـ DB يحتوي شيء آخر
* Feed و Search يعطوا نتائج مختلفة بدون سبب

تسجل Critical Security Bug.

---

# PHASE 7 - COMMENTS & REPLIES

## 7.1 Add Comment

اختبر:

* [ ] Normal comment
* [ ] Empty
* [ ] Whitespace
* [ ] Maximum length
* [ ] Over maximum
* [ ] Arabic
* [ ] HTML
* [ ] Script
* [ ] Emoji

## 7.2 Reply Tree

أنشئ:

Comment
→ Reply
→ Reply
→ Reply
→ Reply

ثم حاول إضافة:

→ Reply number 5

### Expected

Depth 4 allowed حسب implementation.

Depth 5 يجب رفضه.

## 7.3 Cross-Post Parent Attack

خذ Comment من Post A.

حاول استخدام `parentCommentId` مع Post B.

### Expected

Rejected.

هذا Test مهم جدًا لأنه Authorization + Domain Rule.

## 7.4 Comment Delete

جرب:

* Comment owner
* Post owner
* Admin
* Random user

### Expected

فقط المسموح لهم يحذفوا.

## 7.5 Comment Edit

بما أن Domain Rule موجود لكن لا يوجد endpoint واضح:

* ابحث هل UI يسمح Edit.
* إذا لا يوجد، سجلها كـ implementation gap.
* لا تعتبر Domain Rule Tested فقط لأنها موجودة.

---

# PHASE 8 - REACTIONS

اختبر كل Reaction:

* Like
* Love
* Haha
* Wow
* Sad
* Angry

لكل واحدة:

* [ ] Add
* [ ] Update
* [ ] Toggle
* [ ] Remove
* [ ] Count update
* [ ] Notification
* [ ] DB row

## Duplicate Reaction

اضغط بسرعة عدة مرات.

### Expected

لا يحدث:

* duplicate reactions
* count inflation

---

# PHASE 9 - SAVED POSTS / FAVORITES / COLLECTIONS

## 9.1 Save

اختبر:

* Save
* Unsave
* Re-save

## 9.2 Favorites

اختبر:

* Add Favorite
* Remove Favorite
* Toggle Favorite
* Auto-created Favorites collection

## 9.3 Collections

أنشئ:

* Collection A
* Collection B
* Collection with same name
* Empty name
* Very long name

## 9.4 Multi-Collection

احفظ نفس Post في:

* Collection A
* Collection B
* Favorites

### Expected

كل علاقة تعمل بدون duplication.

## 9.5 Remove

اختبر:

* Remove from one collection
* Remove from Saved
* Remove from Favorites

وتأكد أن إزالة واحدة لا تحذف العلاقات الأخرى بالخطأ.

## 9.6 Database

لكل حالة راجع:

* SavedPost
* SavedCollection
* CollectionId
* PostId
* Tag

واختبر خصوصًا مشكلة `PostId1`.

---

# PHASE 10 - FRIEND SYSTEM

## 10.1 Request Lifecycle

نفذ:

A → B Request

ثم:

* [ ] Accept
* [ ] Reject
* [ ] Cancel
* [ ] Unfriend

## 10.2 Invalid Requests

اختبر:

* Self request
* Duplicate request
* Request while already friends
* Request while blocked
* Request to nonexistent user

## 10.3 Blocking

A blocks B.

ثم اختبر:

* Profile visibility
* Posts
* Comments
* Reactions
* Friend request
* Existing friendship
* Search
* Suggestions

ثم:

B blocks A

ثم repeat.

### مهم

اختبر **both directions** لأن التقرير أشار أن بعض block queries direction-based.

## 10.4 Unblock

بعد unblock:

* [ ] Profile accessible حسب rules
* [ ] Friend request possible
* [ ] Old friendship is not automatically restored

---

# PHASE 11 - PROFILE & PRIVACY

## Test Matrix

استخدم:

* Owner
* Friend
* Non-friend
* Blocked user

اختبر:

* Profile
* Bio
* Posts
* Friends list
* Contact information
* Private profile

## 11.1 Edit Profile

اختبر:

* Name
* Bio
* Date of birth
* Profile picture
* Invalid image
* Oversized image

## 11.2 Account State

اختبر:

* Deactivate
* Login after deactivate
* Delete account
* Login after delete
* Access old profile URL

### Expected

Soft-deleted user لا يظهر في normal queries.

---

# PHASE 12 - STORIES

هذه Phase عالية الخطورة.

## 12.1 Create Story

اختبر:

* Image
* Invalid extension
* Empty media
* Oversized media
* Text
* Privacy Public
* Privacy FriendsOnly

## 12.2 Daily Limit

أنشئ حتى:

`Story 10`

ثم حاول:

`Story 11`

### Expected

رفض Story 11.

## 12.3 Expiration

لا تعتمد فقط على الانتظار.

راجع DB:

* `CreatedAt`
* `ExpiresAt`

واختبر query behavior.

Story expired يجب ألا يظهر.

## 12.4 Story Privacy

أنشئ FriendsOnly Story.

اختبر:

* Owner
* Friend
* Non-friend
* Blocked user

## 12.5 STORY PRECEDENCE BUG

اختبر سيناريو:

A has non-accepted relationship with B.

B لديه FriendsOnly Story.

A يحاول:

`GetUserStories(B)`

### Expected

A لا يرى Story.

كرر مع:

* Pending
* Rejected
* Blocked

هذا Test لازم يتنفذ يدويًا لأن التقرير حدد SQL precedence bug محتمل.

## 12.6 Story Viewer

اختبر:

* Next
* Previous
* Auto advance
* Keyboard navigation
* Escape
* Progress bar
* Open same story twice
* Mark viewed

DB:

* StoryViewer row ينشأ مرة واحدة فقط.

---

# PHASE 13 - STORY CLEANUP

اختبر:

* Story expired
* Story deleted
* Expired story still exists physically

راجع:

* IsDeleted
* Expired filtering
* `DeleteExpiredStoriesAsync`

### الهدف

معرفة الفرق بين:

"Story غير ظاهر"

و

"Story تم تنظيفه من DB"

لا تعتبر الأولى دليلًا على نجاح الـ cleanup job.

---

# PHASE 14 - GROUPS

## 14.1 Create Group

اختبر:

* Valid name
* Empty
* Short
* Long
* Description
* Image

## 14.2 Membership

اختبر:

* Join
* Leave
* Rejoin
* Duplicate join

## 14.3 Group Permissions

اختبر:

### Member

* View
* Post
* Leave

### Moderator/Admin حسب implementation

* Kick
* Update
* Other permissions

### Non-member

* Post
* Manage
* Access restricted content

## 14.4 Banned User

اختبر مستخدم تم منعه من Group.

يحاول:

* Join
* Post
* Access content

### Expected

Denied.

## 14.5 Sole Admin

Group به Admin واحد.

حاول:

`Leave Group`

### Expected

منع العملية حتى يتم تعيين Admin آخر حسب business rule.

---

# PHASE 15 - PAGES

## 15.1 Create Page

اختبر:

* Name length
* Description
* Image

## 15.2 Follow

اختبر:

* Follow
* Unfollow
* Duplicate follow
* Admin follows own page

### Expected

Admin cannot follow own page.

## 15.3 Page Permissions

User:

* View
* Follow
* Post

Admin:

* Edit
* Delete
* Post

## 15.4 Page Stats

اختبر stats من:

* Admin
* Follower
* Non-follower

خصوصًا بسبب استخدام:

`Guid.Empty`

في `GetPageStats`.

---

# PHASE 16 - SEARCH

## 16.1 Quick Search

اختبر:

* 1 character
* 2 characters
* 3+
* Arabic
* English
* Mixed
* Special characters
* whitespace

## 16.2 Search Categories

ابحث عن:

* User
* Post
* Group
* Page
* Hashtag

## 16.3 Privacy

تأكد أن Search لا يظهر:

* Private post
* Friends-only post
* Blocked user's content

لمستخدم غير مسموح.

## 16.4 QuickSearch vs Full Search

ابحث عن نفس الشيء في:

* Header QuickSearch
* Full Search page

### Expected

لا يوجد contradiction غير مبرر.

---

# PHASE 17 - HASHTAGS

اختبر:

* Create hashtag
* Same hashtag again
* Case differences
* Arabic hashtag
* Multiple hashtags
* Hashtag with punctuation

ثم:

* Trending
* Search hashtag
* Open hashtag page

### DB

تحقق من:

* unique Tag
* PostHashtag
* Count

---

# PHASE 18 - NOTIFICATIONS

## 18.1 Notification Creation

اختبر أحداث:

* Post Like
* Comment
* Friend Request
* Group event
* Page Follow
* System Alert

## 18.2 Self Notification

User يعمل Action على نفسه.

### Expected

No notification.

## 18.3 Real-Time

افتح User B في Browser.

نفذ من User A:

* Like
* Comment
* Friend request

### Expected

User B يحصل:

* SignalR notification
* Toast
* Badge increment
* Notification dropdown update

## 18.4 Polling

انتظر دورة polling.

### Expected

Unread count يظل صحيح.

## 18.5 Read

اختبر:

* Mark one read
* Mark all read
* Delete notification

## 18.6 Ownership Security

User A يرسل request لتحديد Notification تخص User B.

### Expected

Denied.

## 18.7 Bundling

أنشئ عدة notifications متشابهة خلال 15 دقيقة.

راقب هل فعلاً يتم bundling.

التقرير يقول إن rule موجودة ولكن implementation قد لا يستخدمها.

---

# PHASE 19 - SIGNALR SECURITY

اختبر:

* Unauthorized connection
* Expired JWT
* Invalid JWT
* Wrong JWT
* User A attempting to receive User B notifications

راقب:

* Server logs
* WebSocket connection
* Browser console

### Expected

Connection authorization صحيحة.

---

# PHASE 20 - REPORTING & MODERATION

## 20.1 Report Post

اختبر كل reason:

* Spam
* Harassment
* InappropriateContent
* Violence
* Other

## 20.2 Duplicate Report

نفس User يبلغ عن نفس Post مرتين.

### Expected

الثاني يرفض.

## 20.3 Auto Hide

من 5 users:

report same post.

### Expected

عند الوصول للـ threshold:

* Post hides/deletes حسب implementation
* Post disappears from normal feed
* Reports remain available for admin

## 20.4 Admin Review

Admin:

* Open Reports
* Resolve
* Dismiss
* Delete Reported Post

## 20.5 Normal User

يحاول نفس operations.

### Expected

Denied.

---

# PHASE 21 - ADMIN DASHBOARD

## 21.1 Statistics

اختبر:

* Users count
* Posts count
* Groups
* Pages
* Reports

ثم create/delete entity.

### Expected

Stats update.

## 21.2 Users

اختبر:

* Search
* Filter
* Pagination
* Block
* Unblock
* Delete

## 21.3 Posts

اختبر:

* Search
* Source filter
* Pagination
* Hide
* Delete

## 21.4 Dashboard Permission Bug Test

Admin يحاول:

`Hide another user's post`

Admin يحاول:

`Delete another user's post`

### Expected

يعمل.

لو فشل، سجل High/Critical حسب impact.

## 21.5 Dashboard Chart

راجع:

`UsersLast7Days`

وتأكد أن الأرقام فعلية وليست static.

التقرير أشار أنها hardcoded.

---

# PHASE 22 - FILE UPLOAD SECURITY

اختبر كل upload point:

* Posts
* Profile
* Stories
* Groups
* Pages

## Extension

جرب:

* jpg
* jpeg
* png
* gif
* webp
* mp4
* mov
* txt
* pdf
* exe
* svg

## Size

جرب:

* 1KB
* 1MB
* 4MB
* 5MB
* فوق 5MB

### Expected

حسب actual enforced implementation.

لا تعتمد على Domain rules وحدها.

## Filename

جرب filenames تحتوي:

* spaces
* Arabic
* HTML characters
* very long names
* double extension

مثل:

`image.jpg.exe`

## Storage

بعد upload:

* [ ] File physically موجود
* [ ] URL صحيح
* [ ] DB URL صحيح
* [ ] file can be loaded

بعد delete entity:

* [ ] هل الملف حذف؟
* [ ] أم database فقط؟

سجل النتيجة.

---

# PHASE 23 - XSS

هذه من أهم Security Phases.

ضع malicious values في:

* Name
* Bio
* Post title
* Post content
* Comment
* Notification message إن أمكن
* Group name
* Page name
* Search input

اختبر payloads بسيطة مثل HTML/Script content.

افحص:

* Feed
* Profile
* Notifications
* Story UI
* Group
* Page
* Dashboard

خصوصًا الأماكن التي تستخدم:

`innerHTML`

أو

`insertAdjacentHTML`

### Expected

النص يظهر كنص، وليس HTML/JavaScript منفذ.

لو Script اشتغل:

**CRITICAL SECURITY BUG**

---

# PHASE 24 - IDOR / AUTHORIZATION ATTACKS

هذا الاختبار ضروري جدًا.

خذ ID خاص بـ User A.

وأنت User B حاول تمرر ID A إلى endpoints تخص:

* Post
* Comment
* SavedPost
* Collection
* Notification
* Profile
* Group
* Page
* Story

أمثلة:

* Modify A's post
* Delete A's comment
* Access A's private profile
* Read A's notification
* Modify A's collection
* Remove A's saved post
* View A's private story

### Expected

كل unauthorized operation يفشل.

هذا أهم من مجرد وجود `[Authorize]`.

---

# PHASE 25 - CSRF / ANTIFORGERY

لكل POST:

* احذف antiforgery token
* غير token
* استخدم token قديم

اختبر:

* Login-sensitive forms
* Create Post
* Delete Post
* Comment
* Reaction
* Friend request
* Block
* Group
* Page
* Notifications
* Dashboard

### Expected

Request مرفوض.

---

# PHASE 26 - RATE LIMITING

اختبر كل policy.

## Auth

نفذ Requests بسرعة.

Expected:

`429`

## API

Repeat same endpoint rapidly.

## Feed

Rapid pagination.

## FriendRequest

Rapid requests.

## Dashboard

Rapid requests.

### Important

بعد انتهاء الـ window:

* Requests يجب أن تعمل مرة أخرى.

---

# PHASE 27 - ERROR HANDLING

تعمد إرسال:

* Invalid ID
* Random GUID
* Empty GUID
* Non-existing Post
* Non-existing User
* Missing parent comment
* Deleted Post
* Deleted User
* Invalid JSON
* Malformed JSON

### تحقق من:

* HTTP Status
* Response body
* User message
* Log
* Correlation ID

### Expected

No stack trace exposed.

---

# PHASE 28 - CONCURRENCY

نفذ عمليات في نفس الوقت من Browserين.

## Reactions

User clicks same reaction simultaneously.

## Friend Request

Two users send requests simultaneously.

## Save

Same post saved simultaneously.

## Comments

Multiple comments simultaneously.

## Collections

Create same collection name simultaneously.

### Expected

No:

* duplicate rows
* inconsistent counts
* race condition
* broken UI

---

# PHASE 29 - PAGINATION

اختبر كل pagination system:

* Feed
* Saved Posts
* Notifications
* Friends
* Followers
* Dashboard Users
* Dashboard Posts
* Reports

لكل واحدة:

* Page 1
* Middle page
* Last page
* Page beyond last
* Page size boundary
* Empty result

### Expected

Correct:

* TotalCount
* TotalPages
* HasPreviousPage
* HasNextPage

---

# PHASE 30 - UI / FRONTEND INTEGRATION

## Core JS

اختبر:

* `SohbaApp.post`
* `postForm`
* `get`
* toast
* loading state
* error state
* 401/403 handling
* 429 handling
* invalid JSON

## Buttons

كل button:

* Click once
* Double click
* Rapid click

### Expected

No duplicate requests.

## Modals

اختبر:

* Open
* Close
* Escape
* Outside click
* Reopen
* Submit
* Validation
* Reset state

---

# PHASE 31 - JAVASCRIPT / API CONTRACT

لكل AJAX endpoint:

افتح DevTools → Network.

تحقق:

* Request method
* URL
* Status
* Payload
* Headers
* Anti-forgery
* Response JSON
* UI update

اختبر endpoints الموجودة في التقرير واحدة واحدة.

خصوصًا:

`/Home/GetPostCards`

`/Posts/Delete`

`/Posts/Edit`

`/Comments/Delete`

`/Friends/*`

`/Stories/*`

`/Notifications/*`

`/Dashboard/*`

`/Search/QuickSearch`

---

# PHASE 32 - RESPONSIVE TEST

على الأقل:

* Desktop
* Tablet
* Mobile

اختبر:

* Landing
* Login
* Home
* Feed
* Profile
* Friends
* Groups
* Pages
* Stories
* Notifications
* Search
* Dashboard

ركز على:

* Navbar
* Sidebar
* Modals
* Post cards
* Story rail
* Tables
* Buttons
* Forms
* Horizontal overflow

---

# PHASE 33 - BROWSER COMPATIBILITY

اختبر على:

* Chrome
* Edge
* Firefox

ركز على:

* Login
* Fetch
* SignalR
* File upload
* Modals
* Infinite scroll
* Notification UI

---

# PHASE 34 - PERFORMANCE

## Feed

راقب:

* Response time
* Number of SQL queries
* Duplicate queries

خصوصًا Story feed بسبب الـ N+1 concern.

## Stories

راقب query count عند وجود عدد كبير من stories.

## Notifications

راقب pagination.

## Dashboard

راقب loading time.

## Search

راقب query time.

### الهدف

تحديد:

* N+1
* Slow SQL
* Large payloads
* Excessive requests

---

# PHASE 35 - DATA INTEGRITY

بعد كل Feature رئيسية راجع DB.

## Post

تأكد من:

Post
→ User
→ Hashtags
→ PostHashtags
→ Comments
→ Reactions
→ Reports
→ SavedPosts

## Story

Story
→ User
→ StoryViewer

## Group

Group
→ Admin
→ GroupMembers

## Page

Page
→ Admin
→ PageFollowers

## Notification

Notification
→ Receiver
→ Sender
→ Target

أي orphan أو row غير متوقع = Bug.

---

# PHASE 36 - END-TO-END USER JOURNEY

نفذ رحلة كاملة كأنك مستخدم حقيقي.

## Journey A - New User

Register
→ Login
→ Update profile
→ Upload photo
→ Create post
→ Add hashtag
→ React
→ Comment
→ Save
→ Favorite
→ Create collection
→ Friend request
→ Notification
→ Logout

ثم راجع DB.

## Journey B - Two Users

User A
→ Create private post
→ User B tries access
→ Become friends
→ B accesses
→ Unfriend
→ access again

## Journey C - Block

A and B friends
→ A blocks B
→ relationships removed
→ B cannot interact
→ A unblocks
→ test interaction again

## Journey D - Group

Create Group
→ Join
→ Post
→ Leave
→ Rejoin
→ Admin action

## Journey E - Page

Create Page
→ Auto-follow
→ Another user follows
→ Page post
→ Notification
→ Unfollow

## Journey F - Moderation

User creates post
→ 5 reports
→ auto-hide
→ Admin opens report
→ resolves/deletes
→ notification

---

# PHASE 37 - REGRESSION TEST

بعد كل إصلاح Bug:

لا تختبر الـ bug فقط.

ارجع اختبر:

* Authentication
* Home
* Posts
* Comments
* Reactions
* Saved
* Friends
* Stories
* Notifications

خصوصًا إذا التعديل في:

* Domain
* Application Service
* Repository
* DbContext
* JavaScript core
* BaseController
* Program.cs

---

# PHASE 38 - FINAL SECURITY PASS

قبل إطلاق المشروع، تأكد من:

* [ ] Production JWT key ليس placeholder
* [ ] Mailtrap credentials ليست exposed
* [ ] Secrets ليست committed
* [ ] Admin credentials ليست production credentials
* [ ] HTTPS يعمل
* [ ] Secure cookie يعمل
* [ ] Authorization صحيحة
* [ ] IDOR tests passed
* [ ] CSRF tests passed
* [ ] XSS tests passed
* [ ] Rate limiting يعمل
* [ ] File upload secured
* [ ] Sensitive errors غير ظاهرة
* [ ] JWT validation كاملة

---

# PHASE 39 - PRODUCTION READINESS

## Configuration

* [ ] Production connection string
* [ ] Production JWT key
* [ ] Production issuer
* [ ] Production audience
* [ ] Email configuration
* [ ] HTTPS
* [ ] Logging
* [ ] Error handling
* [ ] Static files
* [ ] Upload permissions

## Database

* [ ] Latest migration applied
* [ ] No unexpected migration pending
* [ ] No orphan records
* [ ] Indexes موجودة
* [ ] Seed data لا تسبب production contamination
* [ ] Admin seed credentials changed/disabled

## Application

* [ ] No critical Console errors
* [ ] No startup exceptions
* [ ] No obvious dead functionality
* [ ] No broken links
* [ ] No unauthorized access
* [ ] No critical security findings

---

# CRITICAL TARGETED TESTS FROM THE CODE REVIEW

هذه الاختبارات لا تنتظر نهاية الـ QA. نفذها مبكرًا:

## T-001 Privacy inconsistency

`IsPrivate` vs `Privacy`

Severity target: **Critical**

## T-002 Friends-only Story access

اختبر pending/rejected/non-friend بسبب operator precedence.

Severity target: **High/Critical**

## T-003 Admin Hide Post

Admin hides another user's post.

Severity target: **High**

## T-004 Admin Delete Reported Post

Admin deletes another user's post through Dashboard.

Severity target: **High**

## T-005 Bidirectional Blocking

A blocks B و B blocks A.

Severity target: **High**

## T-006 SavedPost PostId1

Save/remove/move posts بين collections.

Severity target: **High**

## T-007 XSS

User-generated content inside JS-generated HTML.

Severity target: **Critical**

## T-008 IDOR

Change IDs manually in requests.

Severity target: **Critical**

## T-009 Story upload rules

Test image/video/domain-vs-storage mismatch.

Severity target: **Medium/High**

## T-010 Notification bundling

Generate repeated notifications within 15 minutes.

Severity target: **Medium**

## T-011 Story cleanup

Verify expired stories are filtered vs physically deleted.

Severity target: **Medium**

## T-012 Page Stats

Test stats with admin/follower/non-follower.

Severity target: **Medium**

---

# PASS / FAIL RULE

لكل Test سجل:

**Test ID**

**Feature**

**Actor**

**Precondition**

**Action**

**Expected Result**

**Actual Result**

**Frontend Result**

**Backend Result**

**Database Result**

**Status: PASS / FAIL / BLOCKED**

**Severity**

**Evidence**

---

# Severity

## Critical

* Security bypass
* IDOR
* XSS execution
* Privacy leak
* Unauthorized admin action
* Authentication bypass
* Data corruption

## High

* Major business rule violation
* Important feature broken
* Data inconsistency
* Notification/security failure
* Major workflow impossible

## Medium

* Feature partially broken
* Incorrect UI state
* Pagination issue
* Non-critical validation issue

## Low

* Visual issue
* Minor wording
* Small UX issue
* Non-blocking console warning

---

# FINAL RELEASE GATE

Sohba لا يعتبر Ready for Launch إلا عندما:

* [ ] All Critical tests PASS
* [ ] All High tests PASS
* [ ] No unresolved privacy/security issue
* [ ] Authentication fully passes
* [ ] Authorization fully passes
* [ ] CRUD flows fully pass
* [ ] Database integrity passes
* [ ] AJAX/API contracts pass
* [ ] SignalR passes
* [ ] File uploads pass
* [ ] Mobile passes
* [ ] Regression passes
* [ ] Production configuration reviewed
* [ ] No known blocker remains

Final verdict:

**READY**

أو

**READY WITH NON-BLOCKING ISSUES**

أو

**NOT READY FOR RELEASE**
