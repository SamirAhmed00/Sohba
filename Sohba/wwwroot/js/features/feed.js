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

    // Populate the dedup Set with already-rendered post IDs
    collectRenderedPostIds();


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


const renderedPostIds = new Set();

function collectRenderedPostIds() {
    document.querySelectorAll('#postsContainer [data-post-id]').forEach(el => {
        renderedPostIds.add(el.dataset.postId);
    });
}

function setupInfiniteScroll() {
    let scrollTicking = false;

    window.addEventListener('scroll', function () {
        if (scrollTicking) return;
        scrollTicking = true;

        requestAnimationFrame(() => {
            scrollTicking = false;
            if (isLoading || !hasMore) return;

            const scrollHeight = document.documentElement.scrollHeight;
            const scrollTop = document.documentElement.scrollTop || document.body.scrollTop;
            const clientHeight = document.documentElement.clientHeight;

            if (scrollTop + clientHeight >= scrollHeight - 300) {
                loadMorePosts();
            }
        });
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
        const response = await fetch(`/Home/GetPostCards?page=${nextPage}&pageSize=${pageSize}`);
        const result = await response.json();

        if (result.success) {
            const container = document.getElementById('postsContainer');
            if (container && result.html) {
                const temp = document.createElement('div');
                temp.innerHTML = result.html;

                const uniqueCards = Array.from(temp.querySelectorAll('[data-post-id]')).filter(card => {
                    const id = card.dataset.postId;
                    if (!id || renderedPostIds.has(id)) return false;
                    renderedPostIds.add(id);
                    return true;
                });

                if (uniqueCards.length > 0) {
                    uniqueCards.forEach(card => container.appendChild(card));
                }
            }
            currentPage = result.currentPage ?? nextPage;
            hasMore = result.hasMore ?? false;

            if (!hasMore) {
                hideLoadMoreButton();
            }
        }
        else {
            console.error('Failed to load more posts:', result.error);
            SohbaApp.toast(result.error || 'Failed to load more posts', 'error');
        }
    } catch (error) {
        console.error('Error loading more posts:', error);
        SohbaApp.toast('Network error', 'error');
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