// feed.js - Infinite scroll / Load more functionality

let currentPage = 1;
const pageSize = 10;
let isLoading = false;
let hasMore = true;

// ============================================================
// INFINITE SCROLL
// ============================================================
document.addEventListener('DOMContentLoaded', function () {
    // Get initial page from URL or default to 1
    const urlParams = new URLSearchParams(window.location.search);
    currentPage = parseInt(urlParams.get('page')) || 1;

    // Check if there's a "Load More" button (for non-infinite scroll)
    const loadMoreBtn = document.getElementById('loadMoreBtn');
    if (loadMoreBtn) {
        loadMoreBtn.addEventListener('click', function (e) {
            e.preventDefault();
            if (!isLoading && hasMore) {
                loadMorePosts();
            }
        });
    }

    //  Setup infinite scroll if no load more button
    if (!loadMoreBtn) {
        setupInfiniteScroll();
    }
});

function setupInfiniteScroll() {
    // Detect when user scrolls near bottom
    window.addEventListener('scroll', function () {
        if (isLoading || !hasMore) return;

        const scrollHeight = document.documentElement.scrollHeight;
        const scrollTop = document.documentElement.scrollTop || document.body.scrollTop;
        const clientHeight = document.documentElement.clientHeight;

        // Load more when user is 200px from bottom
        if (scrollTop + clientHeight >= scrollHeight - 200) {
            loadMorePosts();
        }
    });
}

function setupLoadMoreButton() {
    const loadMoreBtn = document.getElementById('loadMoreBtn');
    if (loadMoreBtn) {
        loadMoreBtn.addEventListener('click', function (e) {
            e.preventDefault();
            if (!isLoading && hasMore) {
                loadMorePosts();
            }
        });
    }
}

async function loadMorePosts() {
    if (isLoading || !hasMore) return;

    isLoading = true;
    const nextPage = currentPage + 1;

    //  Show loading indicator
    showLoadingIndicator();

    try {
        // Get HTML from server(rendered using _PostCard.cshtml)
        const response = await fetch(`/Home/GetPostCards?page=${nextPage}&pageSize=${pageSize}`);
        const data = await response.json();

        if (data.success) {
            // ✅ Append the HTML directly - same style as original!
            const container = document.getElementById('postsContainer');
            if (container) {
                container.insertAdjacentHTML('beforeend', data.html);
            }

            currentPage = data.currentPage;
            hasMore = data.hasMore;

            if (!hasMore) {
                hideLoadMoreButton();
            }
        } else {
            console.error('Failed to load more posts:', data.error);
            if (window.SohbaApp && SohbaApp.toast) {
                SohbaApp.toast('Failed to load more posts', 'error');
            }
        }
    } catch (error) {
        console.error('Error loading more posts:', error);
        if (window.SohbaApp && SohbaApp.toast) {
            SohbaApp.toast('Network error', 'error');
        }
    } finally {
        isLoading = false;
        hideLoadingIndicator();
    }
}


function showLoadingIndicator() {
    const loader = document.getElementById('loadingIndicator');
    if (loader) loader.classList.remove('hidden');
}

function hideLoadingIndicator() {
    const loader = document.getElementById('loadingIndicator');
    if (loader) loader.classList.add('hidden');
}

function hideLoadMoreButton() {
    const btn = document.getElementById('loadMoreBtn');
    if (btn) btn.style.display = 'none';

    const endMessage = document.getElementById('endOfFeedMessage');
    if (endMessage) endMessage.classList.remove('hidden');
}