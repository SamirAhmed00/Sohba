using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sohba.Domain.Entities.GroupAndPage;
using Sohba.Domain.Entities.PostAggregate;
using Sohba.Domain.Entities.StoryAggregate;
using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Enums;
using Sohba.Infrastructure.Data;
using Sohba.Domain.Entities.StoryAggregate;
namespace Sohba.Infrastructure.DBInitializer
{
    public class DBInitializer : IDBInitializer
    {
        private readonly AppDbContext _context;
        private readonly IServiceProvider _serviceProvider;

        public DBInitializer(AppDbContext context, IServiceProvider serviceProvider)
        {
            _context = context;
            _serviceProvider = serviceProvider;
        }

        public async Task InitializeAsync()
        {
            // Apply migrations
            await _context.Database.MigrateAsync();

            // Seed roles and admin user
            await SeedRolesAsync();
            await SeedAdminUserAsync();
            await SeedTestUsersAsync(); 
            await SeedSampleDataAsync();
            await SeedExtraTestDataAsync();
        }

        private async Task SeedRolesAsync()
        {
            var roleManager = _serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            string[] roleNames = { "Admin", "User" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid> { Name = roleName });
                }
            }
        }

        private async Task SeedAdminUserAsync()
        {
            var userManager = _serviceProvider.GetRequiredService<UserManager<User>>();

            string adminEmail = "admin@sohba.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new User
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    UserName = adminEmail,
                    Email = adminEmail,
                    Name = "Admin User",
                    Bio = "System Administrator",
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true,
                    ProfilePictureUrl = "https://ui-avatars.com/api/?name=Admin&background=345e69&color=fff&size=128"
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123456");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }

        // ============================================================
        // NEW: Seed Test Users with different scenarios
        // ============================================================
        private async Task SeedTestUsersAsync()
        {
            var userManager = _serviceProvider.GetRequiredService<UserManager<User>>();

            // ============================================================
            // USER 1: Mohammed - Has friends, groups, pages, posts
            // ============================================================
            var mohammed = await CreateUserIfNotExists(
                userManager,
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "mohammed@sohba.com",
                "Mohammed",
                "Mohammed123!",
                "Software Engineer & Community Builder | Passionate about connecting people",
                "https://ui-avatars.com/api/?name=Mohammed&background=345e69&color=fff&size=128"
            );

            // ============================================================
            // USER 2: Ahmed - Has friends, groups, pages (no posts yet)
            // ============================================================
            var ahmed = await CreateUserIfNotExists(
                userManager,
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "ahmed@sohba.com",
                "Ahmed",
                "Ahmed123!",
                "UI/UX Designer | Creating beautiful digital experiences",
                "https://ui-avatars.com/api/?name=Ahmed&background=4a8291&color=fff&size=128"
            );

            // ============================================================
            // USER 3: Sara - Has friends only (no groups, no pages)
            // ============================================================
            var sara = await CreateUserIfNotExists(
                userManager,
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                "sara@sohba.com",
                "Sara",
                "Sara123!",
                "Content Creator | Storyteller | Coffee Lover ☕",
                "https://ui-avatars.com/api/?name=Sara&background=8B5CF6&color=fff&size=128"
            );

            // ============================================================
            // USER 4: Khaled - Has no friends, no groups, no pages (NEW USER)
            // ============================================================
            var khaled = await CreateUserIfNotExists(
                userManager,
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                "khaled@sohba.com",
                "Khaled",
                "Khaled123!",
                "New to Sohba! Excited to connect with everyone 🚀",
                "https://ui-avatars.com/api/?name=Khaled&background=10B981&color=fff&size=128"
            );

            // ============================================================
            // USER 5: Layla - Has groups only (no friends, no pages)
            // ============================================================
            var layla = await CreateUserIfNotExists(
                userManager,
                Guid.Parse("66666666-6666-6666-6666-666666666666"),
                "layla@sohba.com",
                "Layla",
                "Layla123!",
                "Group Admin | Community Manager | Event Organizer 🎉",
                "https://ui-avatars.com/api/?name=Layla&background=EC4899&color=fff&size=128"
            );

            // ============================================================
            // USER 6: Omar - Has pages only (no friends, no groups)
            // ============================================================
            var omar = await CreateUserIfNotExists(
                userManager,
                Guid.Parse("77777777-7777-7777-7777-777777777777"),
                "omar@sohba.com",
                "Omar",
                "Omar123!",
                "Page Creator | Business Owner | Tech Enthusiast 💻",
                "https://ui-avatars.com/api/?name=Omar&background=F59E0B&color=fff&size=128"
            );

            // ============================================================
            // USER 7: Nour - Has friends and groups (no pages)
            // ============================================================
            var nour = await CreateUserIfNotExists(
                userManager,
                Guid.Parse("88888888-8888-8888-8888-888888888888"),
                "nour@sohba.com",
                "Nour",
                "Nour123!",
                "Marketing Specialist | Social Media Enthusiast 📱",
                "https://ui-avatars.com/api/?name=Nour&background=8B5CF6&color=fff&size=128"
            );

            // ============================================================
            // USER 8: Youssef - Has friends and pages (no groups)
            // ============================================================
            var youssef = await CreateUserIfNotExists(
                userManager,
                Guid.Parse("99999999-9999-9999-9999-999999999999"),
                "youssef@sohba.com",
                "Youssef",
                "Youssef123!",
                "Digital Artist | Photographer | Traveler 🌍",
                "https://ui-avatars.com/api/?name=Youssef&background=EC4899&color=fff&size=128"
            );

            // Now create relationships (Friends, Groups, Pages, Posts)
            await CreateRelationshipsAsync(mohammed, ahmed, sara, khaled, layla, omar, nour, youssef);
        }

        private async Task<User> CreateUserIfNotExists(
            UserManager<User> userManager,
            Guid id,
            string email,
            string name,
            string password,
            string bio,
            string profilePictureUrl)
        {
            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser != null)
                return existingUser;

            var user = new User
            {
                Id = id,
                UserName = email,
                Email = email,
                Name = name,
                Bio = bio,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true,
                ProfilePictureUrl = profilePictureUrl,
                IsPrivateAccount = false,
                ShowActivityStatus = true,
                EmailNotifications = true,
                PushNotifications = true,
                WeeklyDigest = false
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "User");
                return user;
            }

            throw new Exception($"Failed to create user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        private async Task CreateRelationshipsAsync(
            User mohammed,
            User ahmed,
            User sara,
            User khaled,
            User layla,
            User omar,
            User nour,
            User youssef)
        {
            // ============================================================
            // 1. FRIENDSHIPS
            // ============================================================

            // Mohammed's Friends: Ahmed, Sara, Nour, Youssef (4 friends)
            var mohammedFriends = new[] { ahmed, sara, nour, youssef };
            foreach (var friend in mohammedFriends)
            {
                await AddFriendshipAsync(mohammed.Id, friend.Id, FriendshipStatus.Accepted);
            }

            // Ahmed's Friends: Mohammed, Sara (2 friends)
            await AddFriendshipAsync(ahmed.Id, sara.Id, FriendshipStatus.Accepted);

            // Sara's Friends: Mohammed, Ahmed (2 friends)
            // Already added via Mohammed and Ahmed

            // Nour's Friends: Mohammed, Sara, Youssef (3 friends)
            await AddFriendshipAsync(nour.Id, sara.Id, FriendshipStatus.Accepted);
            await AddFriendshipAsync(nour.Id, youssef.Id, FriendshipStatus.Accepted);

            // Youssef's Friends: Mohammed, Nour (2 friends)
            // Already added

            // Pending Requests:
            // Khaled sent request to Mohammed (pending)
            await AddFriendshipAsync(khaled.Id, mohammed.Id, FriendshipStatus.Pending);

            // Layla sent request to Sara (pending)
            await AddFriendshipAsync(layla.Id, sara.Id, FriendshipStatus.Pending);

            // Omar sent request to Ahmed (pending)
            await AddFriendshipAsync(omar.Id, ahmed.Id, FriendshipStatus.Pending);

            // ============================================================
            // 2. GROUPS
            // ============================================================

            // Group 1: "Sohba Developers" - Admin: Mohammed
            var devGroup = await CreateGroupAsync(
                "Sohba Developers",
                "A community for developers building the future of social media. Share code, ideas, and collaborate! 👨‍💻",
                mohammed.Id,
                "https://ui-avatars.com/api/?name=Dev&background=345e69&color=fff&size=128"
            );

            // Members: Mohammed (Admin), Ahmed, Sara, Khaled, Nour
            await AddGroupMemberAsync(devGroup.Id, ahmed.Id, GroupRole.Member);
            await AddGroupMemberAsync(devGroup.Id, sara.Id, GroupRole.Member);
            await AddGroupMemberAsync(devGroup.Id, khaled.Id, GroupRole.Member);
            await AddGroupMemberAsync(devGroup.Id, nour.Id, GroupRole.Member);

            // Group 2: "Sohba Designers" - Admin: Layla
            var designGroup = await CreateGroupAsync(
                "Sohba Designers",
                "A creative space for designers to share UI/UX tips, design systems, and portfolio feedback! 🎨",
                layla.Id,
                "https://ui-avatars.com/api/?name=Design&background=8B5CF6&color=fff&size=128"
            );

            // Members: Layla (Admin), Sara, Youssef, Omar, Nour
            await AddGroupMemberAsync(designGroup.Id, sara.Id, GroupRole.Member);
            await AddGroupMemberAsync(designGroup.Id, youssef.Id, GroupRole.Member);
            await AddGroupMemberAsync(designGroup.Id, omar.Id, GroupRole.Member);
            await AddGroupMemberAsync(designGroup.Id, nour.Id, GroupRole.Member);

            // Group 3: "Sohba Travelers" - Admin: Youssef
            var travelGroup = await CreateGroupAsync(
                "Sohba Travelers",
                "For wanderlust souls! Share travel photos, tips, and stories from around the world! ✈️",
                youssef.Id,
                "https://ui-avatars.com/api/?name=Travel&background=10B981&color=fff&size=128"
            );

            // Members: Youssef (Admin), Mohammed, Sara, Khaled
            await AddGroupMemberAsync(travelGroup.Id, mohammed.Id, GroupRole.Member);
            await AddGroupMemberAsync(travelGroup.Id, sara.Id, GroupRole.Member);
            await AddGroupMemberAsync(travelGroup.Id, khaled.Id, GroupRole.Member);

            // ============================================================
            // 3. PAGES
            // ============================================================

            // Page 1: "Sohba Tech" - Admin: Mohammed
            var techPage = await CreatePageAsync(
                "Sohba Tech",
                "Your daily dose of technology news, reviews, and insights. Stay updated with the latest in tech! 💻",
                mohammed.Id,
                "https://ui-avatars.com/api/?name=Tech&background=345e69&color=fff&size=128"
            );

            // Followers: Ahmed, Sara, Khaled
            await AddPageFollowerAsync(techPage.Id, ahmed.Id);
            await AddPageFollowerAsync(techPage.Id, sara.Id);
            await AddPageFollowerAsync(techPage.Id, khaled.Id);

            // Page 2: "Sohba Design" - Admin: Omar
            var designPage = await CreatePageAsync(
                "Sohba Design",
                "Everything about design! UI/UX, Graphic Design, Branding, and more. Follow for daily inspiration! 🎨",
                omar.Id,
                "https://ui-avatars.com/api/?name=Design&background=F59E0B&color=fff&size=128"
            );

            // Followers: Ahmed, Sara, Nour, Layla
            await AddPageFollowerAsync(designPage.Id, ahmed.Id);
            await AddPageFollowerAsync(designPage.Id, sara.Id);
            await AddPageFollowerAsync(designPage.Id, nour.Id);
            await AddPageFollowerAsync(designPage.Id, layla.Id);

            // Page 3: "Sohba Food" - Admin: Nour
            var foodPage = await CreatePageAsync(
                "Sohba Food",
                "For food lovers! Recipes, restaurant reviews, and food photography. 🍕🍜🍰",
                nour.Id,
                "https://ui-avatars.com/api/?name=Food&background=EC4899&color=fff&size=128"
            );

            // Followers: Mohammed, Sara, Youssef
            await AddPageFollowerAsync(foodPage.Id, mohammed.Id);
            await AddPageFollowerAsync(foodPage.Id, sara.Id);
            await AddPageFollowerAsync(foodPage.Id, youssef.Id);

            // ============================================================
            // 4. POSTS
            // ============================================================

            // Mohammed's Posts
            await CreatePostAsync(
                "Welcome to Sohba! 🚀",
                "I'm excited to announce the launch of Sohba - a new social media platform built with love and passion. Connect with friends, share your stories, and build communities! #Sohba #Launch",
                mohammed.Id,
                null,
                new[] { "Sohba", "Launch" }
            );

            await CreatePostAsync(
                "5 Tips for Better Code",
                "After years of development, here are my top 5 tips for writing clean, maintainable code:\n\n1. Write readable code\n2. Test everything\n3. Document your work\n4. Refactor regularly\n5. Learn from others\n\nWhat's your #1 coding tip?",
                mohammed.Id,
                null,
                new[] { "Coding", "DeveloperTips" }
            );

            // Ahmed's Posts
            await CreatePostAsync(
                "Design Principles I Love",
                "Design is not just how it looks, but how it works. Here are my favorite design principles:\n\n- Simplicity\n- Consistency\n- Feedback\n- Accessibility\n\nWhat's your favorite design principle? #Design #UX",
                ahmed.Id,
                null,
                new[] { "Design", "UX" }
            );

            // Sara's Post
            await CreatePostAsync(
                "My Travel Story: Paris ✨",
                "Just came back from Paris! The city of love and lights. Here are some of my favorite moments:\n\n- Eiffel Tower at sunset 🌅\n- Croissants and coffee ☕\n- Walking along the Seine 🌊\n\nWho else loves Paris? #Travel #Paris",
                sara.Id,
                null,
                new[] { "Travel", "Paris" }
            );

            // Nour's Post
            await CreatePostAsync(
                "Social Media Marketing Tips 📱",
                "As a marketer, here's what I've learned about building a social media presence:\n\n1. Be authentic\n2. Engage with your audience\n3. Post consistently\n4. Use visuals\n5. Track analytics\n\nWhat's your biggest social media challenge? #Marketing",
                nour.Id,
                null,
                new[] { "Marketing", "SocialMedia" }
            );

            // Youssef's Post
            await CreatePostAsync(
                "My Photography Journey 📸",
                "Photography has been my passion for 5 years. Here's what I've learned:\n\n- Always carry your camera\n- Golden hour is magical\n- Edit with purpose\n- Tell stories through images\n\nShare your favorite photo below! #Photography",
                youssef.Id,
                null,
                new[] { "Photography" }
            );

            // Save all changes
            await _context.SaveChangesAsync();
        }

        // ============================================================
        // HELPER METHODS
        // ============================================================

        private async Task AddFriendshipAsync(Guid userId, Guid friendUserId, FriendshipStatus status)
        {
            var exists = await _context.Friends
                .AnyAsync(f => (f.UserId == userId && f.FriendUserId == friendUserId) ||
                               (f.UserId == friendUserId && f.FriendUserId == userId));

            if (!exists)
            {
                _context.Friends.Add(new Friend
                {
                    UserId = userId,
                    FriendUserId = friendUserId,
                    Status = status,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        private async Task<Group> CreateGroupAsync(string name, string description, Guid adminId, string imageUrl)
        {
            var existing = await _context.Groups
                .Include(g => g.GroupMembers)
                .FirstOrDefaultAsync(g => g.Name == name);
            if (existing != null)
            {
                if (existing.GroupMembers.All(m => m.UserId != adminId))
                {
                    await AddGroupMemberAsync(existing.Id, adminId, GroupRole.Admin);
                }
                return existing;
            }

            var group = new Group
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                AdminId = adminId,
                CreatedAt = DateTime.UtcNow,
                ImageUrl = imageUrl,
                GroupMembers = new List<GroupMember>()
            };

            _context.Groups.Add(group);

            // Save and verify
            var rowsAffected = await _context.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                throw new Exception($"Failed to create group '{name}'");
            }

            // Add admin as member
            await AddGroupMemberAsync(group.Id, adminId, GroupRole.Admin);

            return group;
        }
        private async Task AddGroupMemberAsync(Guid groupId, Guid userId, GroupRole role)
        {
            //  Check if already a member
            var exists = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

            if (!exists)
            {
                //  Verify group exists
                var groupExists = await _context.Groups.AnyAsync(g => g.Id == groupId);
                if (!groupExists)
                {
                    throw new Exception($"Group with ID '{groupId}' does not exist");
                }

                _context.GroupMembers.Add(new GroupMember
                {
                    Id = Guid.NewGuid(), //  Add explicit Id
                    GroupId = groupId,
                    UserId = userId,
                    Role = role,
                    JoinedAt = DateTime.UtcNow,
                    IsBanned = false
                });

                await _context.SaveChangesAsync();
            }
        }

        private async Task<Page> CreatePageAsync(string name, string description, Guid adminId, string imageUrl)
        {
            var existing = await _context.Pages.FirstOrDefaultAsync(p => p.Name == name);
            if (existing != null)
            {
                await AddPageFollowerAsync(existing.Id, adminId);
                return existing;
            }


            var page = new Page
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                AdminId = adminId,
                CreatedAt = DateTime.UtcNow,
                ImageUrl = imageUrl
            };

            _context.Pages.Add(page);

            // ✅ Save and verify the page was created
            var rowsAffected = await _context.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                throw new Exception($"Failed to create page '{name}'");
            }

            // ✅ Verify the page exists in database
            var savedPage = await _context.Pages.FindAsync(page.Id);
            if (savedPage == null)
            {
                throw new Exception($"Page '{name}' was not found after save");
            }

            // ✅ Admin automatically follows their page
            await AddPageFollowerAsync(page.Id, adminId);

            return page;
        }
        private async Task AddPageFollowerAsync(Guid pageId, Guid userId)
        {
            //  Check if already following
            var exists = await _context.PageFollowers
                .AnyAsync(pf => pf.PageId == pageId && pf.UserId == userId);

            if (!exists)
            {
                //  Verify page exists before adding follower
                var pageExists = await _context.Pages.AnyAsync(p => p.Id == pageId);
                if (!pageExists)
                {
                    throw new Exception($"Page with ID '{pageId}' does not exist");
                }

                _context.PageFollowers.Add(new PageFollower
                {
                    Id = Guid.NewGuid(), //  Add explicit Id
                    PageId = pageId,
                    UserId = userId,
                    FollowedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }
        }

        private async Task CreatePostAsync(string title, string content, Guid userId, string? imageUrl, string[] hashtags)
        {
            var post = new Post
            {
                Id = Guid.NewGuid(),
                Title = title,
                Content = content,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                IsHidden = false,
                IsPrivate = false,
                Privacy = PostPrivacy.Public,
                ImageUrl = imageUrl,
                SourceType = PostSourceType.User
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            // Add hashtags
            foreach (var tag in hashtags)
            {
                var hashtag = await _context.Hashtags.FirstOrDefaultAsync(h => h.Tag == tag);
                if (hashtag == null)
                {
                    hashtag = new Hashtag
                    {
                        Id = Guid.NewGuid(),
                        Tag = tag,
                        Count = 1,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Location = "Egypt"
                    };
                    _context.Hashtags.Add(hashtag);
                }
                else
                {
                    hashtag.Count++;
                    hashtag.UpdatedAt = DateTime.UtcNow;
                }

                _context.PostHashtags.Add(new PostHashtag
                {
                    PostId = post.Id,
                    HashtagId = hashtag.Id
                });
            }
        }

        private async Task SeedSampleDataAsync()
        {
            // This is now handled by SeedTestUsersAsync
            await Task.CompletedTask;
        }


        
        private async Task SeedExtraTestDataAsync()
        {
            var mohammed = await _context.Users.FirstAsync(u => u.Email == "mohammed@sohba.com");
            var ahmed = await _context.Users.FirstAsync(u => u.Email == "ahmed@sohba.com");
            var sara = await _context.Users.FirstAsync(u => u.Email == "sara@sohba.com");
            var khaled = await _context.Users.FirstAsync(u => u.Email == "khaled@sohba.com");
            var layla = await _context.Users.FirstAsync(u => u.Email == "layla@sohba.com");
            var omar = await _context.Users.FirstAsync(u => u.Email == "omar@sohba.com");
            var nour = await _context.Users.FirstAsync(u => u.Email == "nour@sohba.com");
            var youssef = await _context.Users.FirstAsync(u => u.Email == "youssef@sohba.com");

            
            if (await _context.Stories.AnyAsync()) return;

            // ================= 1. STORIES =================
            _context.Stories.Add(new Story
            {
                Id = Guid.NewGuid(),
                Content = "Testing a public story!",
                MediaUrl = null,
                MediaType = null,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                Privacy = StoryPrivacy.Public,
                UserId = mohammed.Id
            });
            _context.Stories.Add(new Story
            {
                Id = Guid.NewGuid(),
                Content = "Friends-only story test",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                Privacy = StoryPrivacy.FriendsOnly,
                UserId = sara.Id
            });

            // ================= 2. بوستات خاصة/Friends + جوه Group/Page =================
            var privatePost = new Post
            {
                Id = Guid.NewGuid(),
                Title = "My Private Thoughts",
                Content = "Only I should see this. #Private",
                UserId = khaled.Id,
                CreatedAt = DateTime.UtcNow,
                IsPrivate = true,
                Privacy = PostPrivacy.Private,
                SourceType = PostSourceType.User
            };
            var friendsOnlyPost = new Post
            {
                Id = Guid.NewGuid(),
                Title = "Friends Only Update",
                Content = "Only my friends can see this. #FriendsOnly",
                UserId = mohammed.Id,
                CreatedAt = DateTime.UtcNow,
                IsPrivate = false,
                Privacy = PostPrivacy.Friends,
                SourceType = PostSourceType.User
            };
            _context.Posts.AddRange(privatePost, friendsOnlyPost);
            await _context.SaveChangesAsync();

            // بوست جوه جروب (لازم تجيب GroupId الحقيقي من الداتابيز الأول)
            var devGroup = await _context.Groups.FirstAsync(g => g.Name == "Sohba Developers");
            var groupPost = new Post
            {
                Id = Guid.NewGuid(),
                Title = "Sprint Planning Discussion",
                Content = "Let's discuss our next sprint goals.",
                UserId = ahmed.Id,
                CreatedAt = DateTime.UtcNow,
                Privacy = PostPrivacy.Public,
                SourceType = PostSourceType.Group,
                SourceId = devGroup.Id,
                GroupId = devGroup.Id
            };

            var techPage = await _context.Pages.FirstAsync(p => p.Name == "Sohba Tech");
            var pagePost = new Post
            {
                Id = Guid.NewGuid(),
                Title = "New Product Launch!",
                Content = "Check out our latest tech review.",
                UserId = mohammed.Id,
                CreatedAt = DateTime.UtcNow,
                Privacy = PostPrivacy.Public,
                SourceType = PostSourceType.Page,
                SourceId = techPage.Id,
                PageId = techPage.Id
            };
            _context.Posts.AddRange(groupPost, pagePost);
            await _context.SaveChangesAsync();

            // ================= 3. كومنتات وردود =================
            var firstPublicPost = await _context.Posts.FirstAsync(p => p.Title == "Welcome to Sohba! 🚀");
            var rootComment = new Comment
            {
                Id = Guid.NewGuid(),
                Content = "Congrats on the launch!",
                CreatedAt = DateTime.UtcNow,
                UserId = ahmed.Id,
                PostId = firstPublicPost.Id
            };
            _context.Comments.Add(rootComment);
            await _context.SaveChangesAsync();

            _context.Comments.Add(new Comment
            {
                Id = Guid.NewGuid(),
                Content = "Thanks Ahmed!",
                CreatedAt = DateTime.UtcNow,
                UserId = mohammed.Id,
                PostId = firstPublicPost.Id,
                ParentCommentId = rootComment.Id
            });

            // ================= 4. تفاعلات (Reactions) =================
            _context.Reactions.Add(new Reaction { Id = Guid.NewGuid(), Type = ReactionType.Like, CreatedAt = DateTime.UtcNow, UserId = ahmed.Id, PostId = firstPublicPost.Id });
            _context.Reactions.Add(new Reaction { Id = Guid.NewGuid(), Type = ReactionType.Love, CreatedAt = DateTime.UtcNow, UserId = sara.Id, PostId = firstPublicPost.Id });

            // ================= 5. بلاغات (Reports) — لاختبار Dashboard =================
            var adminTestPost = await _context.Posts.FirstAsync(p => p.Title == "My Travel Story: Paris ✨");
            _context.PostReports.Add(new PostReport
            {
                Id = Guid.NewGuid(),
                Reason = ReportReason.Spam,
                ReportedAt = DateTime.UtcNow,
                IsResolved = false,
                PostId = adminTestPost.Id,
                UserId = khaled.Id
            });

            // ================= 6. بوستات محفوظة (Saved/Favorite) =================
            _context.SavedPost.Add(new SavedPost
            {
                UserId = mohammed.Id,
                PostId = adminTestPost.Id,
                SavedAt = DateTime.UtcNow,
                Tag = SavedTag.Favorite
            });

            await _context.SaveChangesAsync();
        }
    }
}