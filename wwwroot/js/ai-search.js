/**
 * AI-Powered Search Modal Component
 * Features: AI Natural Language Search, Autocomplete, Recent Searches,
 * Trending Searches, Quick Actions (Add to Cart, Wishlist)
 */

(function() {
    'use strict';

    // Configuration
    const CONFIG = {
        minQueryLength: 2,
        debounceDelay: 300,
        aiDebounceDelay: 500, // Longer delay for AI search
        maxRecentSearches: 5,
        maxTrendingSearches: 8,
        maxSuggestions: 10,
        maxAIResults: 8,
        endpoints: {
            suggestions: '/api/searchapi/suggestions',
            products: '/api/searchapi/products',
            recent: '/api/searchapi/recent',
            trending: '/api/searchapi/trending',
            recommendations: '/api/searchapi/recommendations',
            recordClick: '/api/searchapi/click',
            recordView: '/api/searchapi/view',
            deleteRecent: '/api/searchapi/recent',
            clearRecent: '/api/searchapi/recent',
            aiSearch: '/api/searchapi/ai-search',
            quickAddCart: '/api/searchapi/quick-add-cart',
            quickAddWishlist: '/api/searchapi/quick-add-wishlist'
        }
    };

    // State
    let debounceTimer = null;
    let currentSearchHistoryId = null;
    let activeIndex = -1;
    let isAIMode = true; // Default to AI mode
    let isSearching = false;

    // DOM Elements
    let searchModal = null;
    let searchInput = null;
    let searchBody = null;
    let clearBtn = null;
    let bsModal = null;

    // Initialize search component
    function init() {
        searchModal = document.getElementById('searchModal');
        if (!searchModal) return;

        searchInput = document.getElementById('modalSearchInput');
        searchBody = document.getElementById('searchModalBody');
        clearBtn = document.getElementById('modalClearSearchBtn');

        if (!searchInput || !searchBody) return;

        // Initialize Bootstrap modal
        bsModal = new bootstrap.Modal(searchModal);

        // Event listeners
        searchInput.addEventListener('input', handleInput);
        searchInput.addEventListener('keydown', handleKeydown);

        if (clearBtn) {
            clearBtn.addEventListener('click', clearSearch);
        }

        // Modal events
        searchModal.addEventListener('shown.bs.modal', handleModalShown);
        searchModal.addEventListener('hidden.bs.modal', handleModalHidden);

        // Keyboard shortcut (Ctrl+K or Cmd+K)
        document.addEventListener('keydown', handleGlobalKeydown);

        // Update placeholder for AI mode
        updatePlaceholder();
    }

    // Update placeholder based on mode
    function updatePlaceholder() {
        if (searchInput) {
            searchInput.placeholder = isAIMode
                ? 'Ask AI: "blue shirts under 500 taka", "laptop for gaming"...'
                : 'Search products, categories, brands...';
        }
    }

    // Handle global keyboard shortcuts
    function handleGlobalKeydown(e) {
        // Ctrl+K or Cmd+K to open search
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            bsModal.show();
        }
    }

    // Handle modal shown
    function handleModalShown() {
        searchInput.focus();
        if (searchInput.value.length >= CONFIG.minQueryLength) {
            if (isAIMode) {
                performAISearch(searchInput.value.trim());
            } else {
                fetchSuggestions(searchInput.value.trim());
            }
        } else {
            showDefaultContent();
        }
    }

    // Handle modal hidden
    function handleModalHidden() {
        activeIndex = -1;
    }

    // Handle input changes
    function handleInput(e) {
        const query = e.target.value.trim();

        // Toggle clear button visibility
        toggleClearButton(query.length > 0);

        // Cancel previous debounce
        if (debounceTimer) {
            clearTimeout(debounceTimer);
        }

        if (query.length < CONFIG.minQueryLength) {
            if (query.length === 0) {
                showDefaultContent();
            } else {
                showWelcome();
            }
            return;
        }

        // Debounce search - longer delay for AI mode
        const delay = isAIMode ? CONFIG.aiDebounceDelay : CONFIG.debounceDelay;
        debounceTimer = setTimeout(() => {
            if (isAIMode) {
                performAISearch(query);
            } else {
                fetchSuggestions(query);
            }
        }, delay);
    }

    // Handle keyboard navigation
    function handleKeydown(e) {
        const items = searchBody.querySelectorAll('.search-suggestion-item, .ai-product-card');

        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                activeIndex = activeIndex < items.length - 1 ? activeIndex + 1 : 0;
                setActiveItem(items, activeIndex);
                break;
            case 'ArrowUp':
                e.preventDefault();
                activeIndex = activeIndex > 0 ? activeIndex - 1 : items.length - 1;
                setActiveItem(items, activeIndex);
                break;
            case 'Enter':
                e.preventDefault();
                if (activeIndex >= 0 && items[activeIndex]) {
                    const link = items[activeIndex].querySelector('a') || items[activeIndex];
                    if (link.href) {
                        window.location.href = link.href;
                    } else {
                        items[activeIndex].click();
                    }
                } else if (searchInput.value.trim()) {
                    performSearch(searchInput.value.trim());
                }
                break;
            case 'Escape':
                bsModal.hide();
                break;
            case 'Tab':
                // Toggle AI mode with Tab key
                e.preventDefault();
                toggleAIMode();
                break;
        }
    }

    // Toggle AI mode
    function toggleAIMode() {
        isAIMode = !isAIMode;
        updatePlaceholder();
        updateModeIndicator();

        const query = searchInput.value.trim();
        if (query.length >= CONFIG.minQueryLength) {
            if (isAIMode) {
                performAISearch(query);
            } else {
                fetchSuggestions(query);
            }
        }
    }

    // Update mode indicator in footer
    function updateModeIndicator() {
        const footer = searchModal.querySelector('.search-modal-footer .search-powered');
        if (footer) {
            footer.innerHTML = isAIMode
                ? '<i class="fas fa-robot text-primary me-1"></i>AI Search Mode <kbd class="ms-2">Tab</kbd>'
                : '<i class="fas fa-search text-muted me-1"></i>Standard Search <kbd class="ms-2">Tab</kbd>';
        }
    }

    // Set active item in results
    function setActiveItem(items, index) {
        items.forEach((item, i) => {
            item.classList.toggle('active', i === index);
        });
        if (items[index]) {
            items[index].scrollIntoView({ block: 'nearest', behavior: 'smooth' });
        }
    }

    // Perform AI-powered search
    async function performAISearch(query) {
        if (isSearching) return;
        isSearching = true;

        try {
            showAISearchLoading();
            const response = await fetch(`${CONFIG.endpoints.aiSearch}?q=${encodeURIComponent(query)}&maxResults=${CONFIG.maxAIResults}`);
            const data = await response.json();
            renderAISearchResults(data, query);
        } catch (error) {
            console.error('Error performing AI search:', error);
            // Fallback to standard search
            fetchSuggestions(query);
        } finally {
            isSearching = false;
        }
    }

    // Render AI search results
    function renderAISearchResults(data, query) {
        let html = '';
        activeIndex = -1;

        // AI Response Message
        if (data.message) {
            html += `
                <div class="ai-search-message">
                    <div class="ai-avatar">
                        <i class="fas fa-robot"></i>
                    </div>
                    <div class="ai-message-content">
                        <p>${parseMarkdownLinks(data.message)}</p>
                        ${data.detectedLanguage ? `<span class="ai-language-badge">${getLanguageLabel(data.detectedLanguage)}</span>` : ''}
                    </div>
                </div>
            `;
        }

        // Products Grid
        if (data.products && data.products.length > 0) {
            html += `
                <div class="ai-products-section">
                    <div class="search-section-header">
                        <span><i class="fas fa-magic me-2 text-primary"></i>AI Recommendations (${data.totalFound})</span>
                        <a href="/Customer/Home/Search?q=${encodeURIComponent(query)}" class="view-all-link">View All <i class="fas fa-arrow-right ms-1"></i></a>
                    </div>
                    <div class="ai-products-grid">
                        ${data.products.map(p => renderAIProductCard(p)).join('')}
                    </div>
                </div>
            `;
        }

        // Quick Replies (Suggested Actions)
        if (data.quickReplies && data.quickReplies.length > 0) {
            html += `
                <div class="ai-quick-replies">
                    ${data.quickReplies.map(qr => `
                        <button type="button" class="ai-quick-reply-btn" data-action="${escapeHtml(qr.action || 'send_message')}" data-payload="${escapeHtml(qr.payload || qr.text)}">
                            ${qr.icon ? `<i class="${qr.icon}"></i>` : ''}
                            <span>${escapeHtml(qr.text)}</span>
                        </button>
                    `).join('')}
                </div>
            `;
        }

        // Related Suggestions
        if (data.relatedSuggestions && data.relatedSuggestions.length > 0) {
            html += `
                <div class="search-section">
                    <div class="search-section-header">
                        <span><i class="fas fa-lightbulb me-2"></i>Related Searches</span>
                    </div>
                    <div class="related-suggestions">
                        ${data.relatedSuggestions.map(s => `
                            <span class="related-suggestion-tag" data-term="${escapeHtml(s)}">
                                <i class="fas fa-search"></i>
                                ${escapeHtml(s)}
                            </span>
                        `).join('')}
                    </div>
                </div>
            `;
        }

        // No results
        if (!data.success || (!data.products || data.products.length === 0)) {
            html += `
                <div class="search-empty">
                    <div class="search-empty-icon">
                        <i class="fas fa-search-minus"></i>
                    </div>
                    <h6>No products found</h6>
                    <p>Try different keywords or use natural language like "red dress for party"</p>
                    <div class="ai-suggestions-help">
                        <p class="text-muted small mb-2">Try asking:</p>
                        <div class="ai-example-queries">
                            <button type="button" class="ai-example-btn" data-query="latest mobile phones">📱 Latest mobile phones</button>
                            <button type="button" class="ai-example-btn" data-query="shoes under 2000 taka">👟 Shoes under 2000 taka</button>
                            <button type="button" class="ai-example-btn" data-query="gifts for birthday">🎁 Gifts for birthday</button>
                        </div>
                    </div>
                </div>
            `;
        }

        searchBody.innerHTML = html;
        attachAISearchListeners();
    }

    // Render AI product card with quick actions
    function renderAIProductCard(product) {
        const hasDiscount = product.discountPrice && product.discountPrice < product.price;
        const discountPercent = hasDiscount ? Math.round((1 - product.discountPrice / product.price) * 100) : 0;

        return `
            <div class="ai-product-card ${!product.inStock ? 'out-of-stock' : ''}" data-product-id="${product.id}">
                <a href="${product.productUrl}" class="ai-product-link">
                    <div class="ai-product-image">
                        <img src="/${product.imageUrl}" alt="${escapeHtml(product.name)}" loading="lazy" />
                        ${hasDiscount ? `<span class="discount-badge">-${discountPercent}%</span>` : ''}
                        ${!product.inStock ? `<span class="stock-badge">Out of Stock</span>` : ''}
                    </div>
                    <div class="ai-product-info">
                        <h6 class="ai-product-name" title="${escapeHtml(product.name)}">${escapeHtml(product.name)}</h6>
                        ${product.categoryName ? `<span class="ai-product-category">${escapeHtml(product.categoryName)}</span>` : ''}
                        <div class="ai-product-price">
                            ${hasDiscount ? `
                                <span class="original-price">৳${product.price.toLocaleString()}</span>
                                <span class="sale-price">৳${product.discountPrice.toLocaleString()}</span>
                            ` : `
                                <span class="regular-price">৳${product.price.toLocaleString()}</span>
                            `}
                        </div>
                        ${product.rating ? `
                            <div class="ai-product-rating">
                                <i class="fas fa-star text-warning"></i>
                                <span>${product.rating.toFixed(1)}</span>
                                <span class="text-muted">(${product.reviewCount})</span>
                            </div>
                        ` : ''}
                    </div>
                </a>
                <div class="ai-product-footer">
                    <div class="ai-product-actions">
                        <button type="button" class="ai-action-btn ai-add-cart" data-product-id="${product.id}"
                                ${!product.inStock ? 'disabled' : ''} ${product.hasVariants ? 'data-has-variants="true"' : ''}
                                title="${product.hasVariants ? 'Select Options' : 'Add to Cart'}">
                            <i class="fas ${product.hasVariants ? 'fa-eye' : 'fa-cart-plus'}"></i>
                        </button>
                        <button type="button" class="ai-action-btn ai-add-wishlist" data-product-id="${product.id}" title="Add to Wishlist">
                            <i class="far fa-heart"></i>
                        </button>
                    </div>
                </div>
            </div>
        `;
    }

    // Attach event listeners for AI search results
    function attachAISearchListeners() {
        // Product card clicks
        searchBody.querySelectorAll('.ai-product-card[data-product-id]').forEach(card => {
            card.addEventListener('click', (e) => {
                if (e.target.closest('.ai-product-actions')) return;
                const productId = card.dataset.productId;
                recordProductView(productId, 'ai_search', searchInput.value);
            });
        });

        // Quick add to cart
        searchBody.querySelectorAll('.ai-add-cart').forEach(btn => {
            btn.addEventListener('click', async (e) => {
                e.preventDefault();
                e.stopPropagation();

                const productId = btn.dataset.productId;
                const hasVariants = btn.dataset.hasVariants === 'true';

                if (hasVariants) {
                    // Redirect to product page for variant selection
                    const card = btn.closest('.ai-product-card');
                    const link = card.querySelector('.ai-product-link');
                    if (link) {
                        window.location.href = link.href;
                    }
                    return;
                }

                await quickAddToCart(productId, btn);
            });
        });

        // Quick add to wishlist
        searchBody.querySelectorAll('.ai-add-wishlist').forEach(btn => {
            btn.addEventListener('click', async (e) => {
                e.preventDefault();
                e.stopPropagation();

                const productId = btn.dataset.productId;
                await quickAddToWishlist(productId, btn);
            });
        });

        // Quick reply buttons
        searchBody.querySelectorAll('.ai-quick-reply-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                const action = btn.dataset.action;
                const payload = btn.dataset.payload;

                if (action === 'open_url' && payload) {
                    window.location.href = payload;
                } else if (payload) {
                    searchInput.value = payload;
                    performAISearch(payload);
                }
            });
        });

        // Related suggestion tags
        searchBody.querySelectorAll('.related-suggestion-tag').forEach(tag => {
            tag.addEventListener('click', () => {
                const term = tag.dataset.term;
                searchInput.value = term;
                performAISearch(term);
            });
        });

        // Example query buttons
        searchBody.querySelectorAll('.ai-example-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                const query = btn.dataset.query;
                searchInput.value = query;
                performAISearch(query);
            });
        });
    }

    // Quick add to cart
    async function quickAddToCart(productId, btn) {
        const originalHtml = btn.innerHTML;
        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
        btn.disabled = true;

        try {
            const response = await fetch(CONFIG.endpoints.quickAddCart, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ productId: parseInt(productId), quantity: 1 })
            });

            const result = await response.json();

            if (result.success) {
                btn.innerHTML = '<i class="fas fa-check"></i>';
                btn.classList.add('success');
                showToast('success', result.message || 'Added to cart!');

                // Update cart count if available
                updateCartCount();

                setTimeout(() => {
                    btn.innerHTML = originalHtml;
                    btn.classList.remove('success');
                    btn.disabled = false;
                }, 2000);
            } else {
                btn.innerHTML = originalHtml;
                btn.disabled = false;

                if (result.requiresVariant) {
                    showToast('info', 'Please select product options');
                    const card = btn.closest('.ai-product-card');
                    const link = card.querySelector('.ai-product-link');
                    if (link) {
                        window.location.href = link.href;
                    }
                } else if (result.shopConflict) {
                    showToast('warning', result.conflictMessage || 'Cart conflict detected');
                } else {
                    showToast('error', result.message || 'Failed to add to cart');
                }
            }
        } catch (error) {
            console.error('Error adding to cart:', error);
            btn.innerHTML = originalHtml;
            btn.disabled = false;
            showToast('error', 'Failed to add to cart');
        }
    }

    // Quick add to wishlist
    async function quickAddToWishlist(productId, btn) {
        const originalHtml = btn.innerHTML;
        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
        btn.disabled = true;

        try {
            const response = await fetch(CONFIG.endpoints.quickAddWishlist, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ productId: parseInt(productId) })
            });

            const result = await response.json();

            if (result.success) {
                btn.innerHTML = '<i class="fas fa-heart"></i>';
                btn.classList.add('success', 'in-wishlist');
                showToast('success', result.message || 'Added to wishlist!');

                setTimeout(() => {
                    btn.disabled = false;
                }, 2000);
            } else {
                btn.innerHTML = originalHtml;
                btn.disabled = false;

                if (result.requiresLogin) {
                    showToast('info', 'Please login to add to wishlist');
                } else {
                    showToast('error', result.message || 'Failed to add to wishlist');
                }
            }
        } catch (error) {
            console.error('Error adding to wishlist:', error);
            btn.innerHTML = originalHtml;
            btn.disabled = false;
            showToast('error', 'Failed to add to wishlist');
        }
    }

    // Update cart count in navbar
    function updateCartCount() {
        // Try to update cart count badge if exists
        const cartBadge = document.querySelector('.cart-count, .navbar .fa-shopping-cart + .badge, #cartCount');
        if (cartBadge) {
            const currentCount = parseInt(cartBadge.textContent) || 0;
            cartBadge.textContent = currentCount + 1;
            cartBadge.classList.add('animate-bounce');
            setTimeout(() => cartBadge.classList.remove('animate-bounce'), 500);
        }
    }

    // Show toast notification
    function showToast(type, message) {
        // Use alertify if available
        if (typeof alertify !== 'undefined') {
            alertify.notify(message, type, 3);
            return;
        }

        // Fallback toast
        const toast = document.createElement('div');
        toast.className = `ai-search-toast ${type}`;
        toast.innerHTML = `
            <i class="fas fa-${type === 'success' ? 'check-circle' : type === 'error' ? 'exclamation-circle' : type === 'warning' ? 'exclamation-triangle' : 'info-circle'}"></i>
            <span>${escapeHtml(message)}</span>
        `;
        document.body.appendChild(toast);

        setTimeout(() => {
            toast.classList.add('show');
        }, 10);

        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    }

    // Get language label
    function getLanguageLabel(language) {
        const labels = {
            'Bengali': '🇧🇩 বাংলা',
            'English': '🇬🇧 English',
            'Banglish': '🔤 Banglish'
        };
        return labels[language] || language;
    }

    // Show AI search loading state
    function showAISearchLoading() {
        searchBody.innerHTML = `
            <div class="ai-search-loading">
                <div class="ai-loading-animation">
                    <div class="ai-loading-dot"></div>
                    <div class="ai-loading-dot"></div>
                    <div class="ai-loading-dot"></div>
                </div>
                <span>AI is searching...</span>
            </div>
        `;
    }

    // Fetch suggestions from API (standard mode)
    async function fetchSuggestions(query) {
        try {
            showLoading();
            const response = await fetch(`${CONFIG.endpoints.suggestions}?q=${encodeURIComponent(query)}&maxResults=${CONFIG.maxSuggestions}`);
            const data = await response.json();
            renderSuggestions(data, query);
        } catch (error) {
            console.error('Error fetching suggestions:', error);
            showError();
        }
    }

    // Render suggestions (standard mode)
    function renderSuggestions(data, query) {
        let html = '';
        activeIndex = -1;

        // Recent searches matching query
        if (data.suggestions && data.suggestions.some(s => s.type === 0)) {
            const recentMatches = data.suggestions.filter(s => s.type === 0);
            if (recentMatches.length > 0) {
                html += `
                    <div class="search-section">
                        <div class="search-section-header">
                            <span><i class="fas fa-history me-2"></i>Recent</span>
                        </div>
                        ${recentMatches.map(s => `
                            <div class="search-suggestion-item" data-type="history" data-term="${escapeHtml(s.term)}">
                                <i class="fas fa-history text-muted"></i>
                                <span class="suggestion-text">${s.highlightedTerm || escapeHtml(s.term)}</span>
                                <i class="fas fa-arrow-right text-muted ms-auto" style="font-size: 0.75rem;"></i>
                            </div>
                        `).join('')}
                    </div>
                `;
            }
        }

        // Product suggestions
        if (data.productSuggestions && data.productSuggestions.length > 0) {
            html += `
                <div class="search-section">
                    <div class="search-section-header">
                        <span><i class="fas fa-box me-2"></i>Products</span>
                        <a href="/Customer/Home/Search?q=${encodeURIComponent(query)}" class="view-all-link">View All</a>
                    </div>
                    ${data.productSuggestions.map(p => `
                        <a href="/Customer/Home/Detail/${p.id}" class="search-suggestion-item product-suggestion-item" data-product-id="${p.id}">
                            <img src="/${p.imageUrl || 'images/products/noimage.jpg'}" alt="${escapeHtml(p.name)}" class="product-thumb">
                            <div class="product-info">
                                <div class="product-name">${escapeHtml(p.name)}</div>
                                <div class="product-meta">${p.brand || p.categoryName || ''}</div>
                                <div class="product-price">
                                    ${p.hasDiscount ? `
                                        <span class="original-price text-decoration-line-through">৳${p.price.toLocaleString()}</span>
                                        <span class="sale-price">৳${p.discountedPrice.toLocaleString()}</span>
                                    ` : `
                                        <span class="regular-price">৳${p.price.toLocaleString()}</span>
                                    `}
                                </div>
                            </div>
                        </a>
                    `).join('')}
                </div>
            `;
        }

        // Category suggestions
        if (data.categorySuggestions && data.categorySuggestions.length > 0) {
            html += `
                <div class="search-section">
                    <div class="search-section-header">
                        <span><i class="fas fa-folder me-2"></i>Categories</span>
                    </div>
                    ${data.categorySuggestions.map(c => `
                        <a href="/Customer/Home/Index?productType=${c.id}" class="search-suggestion-item category-suggestion-item">
                            <div class="category-icon">
                                <i class="fas fa-tag"></i>
                            </div>
                            <div class="category-info">
                                <div class="category-name">${escapeHtml(c.name)}</div>
                                <div class="category-count">${c.productCount} products</div>
                            </div>
                        </a>
                    `).join('')}
                </div>
            `;
        }

        // Trending searches matching query
        if (data.suggestions && data.suggestions.some(s => s.type === 1)) {
            const trendingMatches = data.suggestions.filter(s => s.type === 1);
            if (trendingMatches.length > 0) {
                html += `
                    <div class="search-section">
                        <div class="search-section-header">
                            <span><i class="fas fa-fire me-2 text-danger"></i>Trending</span>
                        </div>
                        ${trendingMatches.map(s => `
                            <div class="search-suggestion-item" data-type="trending" data-term="${escapeHtml(s.term)}">
                                <i class="fas fa-fire text-danger"></i>
                                <span class="suggestion-text">${s.highlightedTerm || escapeHtml(s.term)}</span>
                                <i class="fas fa-arrow-right text-muted ms-auto" style="font-size: 0.75rem;"></i>
                            </div>
                        `).join('')}
                    </div>
                `;
            }
        }

        // Autocomplete suggestions
        if (data.suggestions && data.suggestions.some(s => s.type === 5)) {
            const autoCompletes = data.suggestions.filter(s => s.type === 5);
            if (autoCompletes.length > 0) {
                html += `
                    <div class="search-section">
                        <div class="search-section-header">
                            <span><i class="fas fa-lightbulb me-2"></i>Suggestions</span>
                        </div>
                        ${autoCompletes.map(s => `
                            <div class="search-suggestion-item" data-type="autocomplete" data-term="${escapeHtml(s.term)}">
                                <i class="fas fa-search text-muted"></i>
                                <span class="suggestion-text">${s.highlightedTerm || escapeHtml(s.term)}</span>
                                <i class="fas fa-arrow-right text-muted ms-auto" style="font-size: 0.75rem;"></i>
                            </div>
                        `).join('')}
                    </div>
                `;
            }
        }

        if (html === '') {
            html = `
                <div class="search-empty">
                    <div class="search-empty-icon">
                        <i class="fas fa-search-minus"></i>
                    </div>
                    <h6>No results found</h6>
                    <p>We couldn't find anything for "${escapeHtml(query)}". Try AI search for better results!</p>
                    <button type="button" class="btn btn-primary btn-sm mt-2" id="switchToAIBtn">
                        <i class="fas fa-robot me-1"></i>Try AI Search
                    </button>
                </div>
            `;
        }

        searchBody.innerHTML = html;
        attachSuggestionListeners();

        // Attach switch to AI button listener
        const switchBtn = searchBody.querySelector('#switchToAIBtn');
        if (switchBtn) {
            switchBtn.addEventListener('click', () => {
                isAIMode = true;
                updatePlaceholder();
                updateModeIndicator();
                performAISearch(query);
            });
        }
    }

    // Show default content (recent + trending)
    async function showDefaultContent() {
        try {
            showLoading();

            const [recentResponse, trendingResponse] = await Promise.all([
                fetch(`${CONFIG.endpoints.recent}?count=${CONFIG.maxRecentSearches}`),
                fetch(`${CONFIG.endpoints.trending}?count=${CONFIG.maxTrendingSearches}`)
            ]);

            const recentData = await recentResponse.json();
            const trendingData = await trendingResponse.json();

            renderDefaultContent(recentData, trendingData);
        } catch (error) {
            console.error('Error fetching default content:', error);
            showWelcome();
        }
    }

    // Render default content
    function renderDefaultContent(recentSearches, trendingSearches) {
        let html = '';
        activeIndex = -1;

        // AI Mode Toggle Banner
        html += `
            <div class="ai-mode-banner">
                <div class="ai-mode-info">
                    <i class="fas fa-robot text-primary"></i>
                    <div>
                        <strong>AI Search Active</strong>
                        <small>Try natural language queries like "red shoes under 1000 taka"</small>
                    </div>
                </div>
                <button type="button" class="ai-mode-toggle" id="aiModeToggle">
                    <span class="toggle-track ${isAIMode ? 'active' : ''}">
                        <span class="toggle-thumb"></span>
                    </span>
                </button>
            </div>
        `;

        // Recent searches
        if (recentSearches && recentSearches.length > 0) {
            html += `
                <div class="search-section">
                    <div class="search-section-header">
                        <span><i class="fas fa-history me-2"></i>Recent Searches</span>
                        <button type="button" class="btn btn-link btn-sm text-danger p-0" id="clearAllRecentBtn" style="font-size: 0.75rem; text-transform: none; letter-spacing: 0;">
                            Clear All
                        </button>
                    </div>
                    ${recentSearches.map(s => `
                        <div class="search-suggestion-item recent-item" data-type="recent" data-term="${escapeHtml(s.searchTerm)}" data-id="${s.id}">
                            <i class="fas fa-history text-muted"></i>
                            <span class="suggestion-text">${escapeHtml(s.searchTerm)}</span>
                            <button type="button" class="delete-recent-btn ms-auto" data-id="${s.id}" title="Remove">
                                <i class="fas fa-times"></i>
                            </button>
                        </div>
                    `).join('')}
                </div>
            `;
        }

        // Trending searches
        if (trendingSearches && trendingSearches.length > 0) {
            html += `
                <div class="search-section">
                    <div class="search-section-header">
                        <span><i class="fas fa-fire me-2 text-danger"></i>Trending Now</span>
                    </div>
                    <div class="trending-tags">
                        ${trendingSearches.map(t => `
                            <span class="trending-tag" data-term="${escapeHtml(t.searchTerm)}">
                                <i class="fas fa-fire"></i>
                                <span class="trending-text">${escapeHtml(t.searchTerm)}</span>
                            </span>
                        `).join('')}
                    </div>
                </div>
            `;
        }

        // AI Example Queries
        html += `
            <div class="search-section ai-examples-section">
                <div class="search-section-header">
                    <span><i class="fas fa-magic me-2 text-primary"></i>Try AI Search</span>
                </div>
                <div class="ai-example-queries">
                    <button type="button" class="ai-example-btn" data-query="trending fashion items">
                        <i class="fas fa-tshirt"></i> Trending fashion
                    </button>
                    <button type="button" class="ai-example-btn" data-query="best selling electronics">
                        <i class="fas fa-mobile-alt"></i> Best electronics
                    </button>
                    <button type="button" class="ai-example-btn" data-query="gifts under 500 taka">
                        <i class="fas fa-gift"></i> Gifts under ৳500
                    </button>
                    <button type="button" class="ai-example-btn" data-query="home decoration items">
                        <i class="fas fa-home"></i> Home decor
                    </button>
                </div>
            </div>
        `;

        if (html === '') {
            showWelcome();
            return;
        }

        searchBody.innerHTML = html;
        attachDefaultListeners();
    }

    // Show welcome state
    function showWelcome() {
        searchBody.innerHTML = `
            <div class="search-welcome">
                <div class="search-welcome-icon">
                    <i class="fas fa-robot"></i>
                </div>
                <h5>AI-Powered Search</h5>
                <p class="text-muted">Ask in your own words - "blue shirt for office" or "gaming laptop under 50000"</p>
                <div class="ai-example-queries mt-3">
                    <button type="button" class="ai-example-btn" data-query="popular products">
                        <i class="fas fa-star"></i> Popular items
                    </button>
                    <button type="button" class="ai-example-btn" data-query="new arrivals">
                        <i class="fas fa-sparkles"></i> New arrivals
                    </button>
                </div>
            </div>
        `;
        attachWelcomeListeners();
    }

    // Attach welcome listeners
    function attachWelcomeListeners() {
        searchBody.querySelectorAll('.ai-example-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                const query = btn.dataset.query;
                searchInput.value = query;
                performAISearch(query);
            });
        });
    }

    // Show loading state
    function showLoading() {
        searchBody.innerHTML = `
            <div class="search-loading-spinner">
                <div class="spinner"></div>
                <span class="text-muted">Searching...</span>
            </div>
        `;
    }

    // Show error state
    function showError() {
        searchBody.innerHTML = `
            <div class="search-empty">
                <div class="search-empty-icon">
                    <i class="fas fa-exclamation-triangle"></i>
                </div>
                <h6>Something went wrong</h6>
                <p>Please try again later.</p>
            </div>
        `;
    }

    // Attach event listeners to suggestion items
    function attachSuggestionListeners() {
        // Term click handlers
        searchBody.querySelectorAll('.search-suggestion-item[data-term]').forEach(item => {
            item.addEventListener('click', (e) => {
                if (e.target.closest('.delete-recent-btn')) return;
                const term = item.dataset.term;
                searchInput.value = term;
                if (isAIMode) {
                    performAISearch(term);
                } else {
                    performSearch(term);
                }
            });
        });

        // Product click handlers
        searchBody.querySelectorAll('.product-suggestion-item[data-product-id]').forEach(item => {
            item.addEventListener('click', (e) => {
                const productId = item.dataset.productId;
                recordClick(currentSearchHistoryId, productId);
                recordProductView(productId, 'search', searchInput.value);
                bsModal.hide();
            });
        });

        // Category click handlers
        searchBody.querySelectorAll('.category-suggestion-item').forEach(item => {
            item.addEventListener('click', () => {
                bsModal.hide();
            });
        });
    }

    // Attach event listeners for default content
    function attachDefaultListeners() {
        // AI Mode toggle
        const aiToggle = searchBody.querySelector('#aiModeToggle');
        if (aiToggle) {
            aiToggle.addEventListener('click', () => {
                isAIMode = !isAIMode;
                const track = aiToggle.querySelector('.toggle-track');
                track.classList.toggle('active', isAIMode);
                updatePlaceholder();
                updateModeIndicator();
            });
        }

        // Recent search click
        searchBody.querySelectorAll('.recent-item').forEach(item => {
            item.addEventListener('click', (e) => {
                if (e.target.closest('.delete-recent-btn')) return;
                const term = item.dataset.term;
                searchInput.value = term;
                if (isAIMode) {
                    performAISearch(term);
                } else {
                    performSearch(term);
                }
            });
        });

        // Delete recent search
        searchBody.querySelectorAll('.delete-recent-btn').forEach(btn => {
            btn.addEventListener('click', async (e) => {
                e.stopPropagation();
                const id = btn.dataset.id;
                await deleteRecentSearch(id);
                btn.closest('.recent-item').remove();

                // Check if section is empty
                const recentItems = searchBody.querySelectorAll('.recent-item');
                if (recentItems.length === 0) {
                    showDefaultContent();
                }
            });
        });

        // Clear all recent searches
        const clearAllBtn = searchBody.querySelector('#clearAllRecentBtn');
        if (clearAllBtn) {
            clearAllBtn.addEventListener('click', async () => {
                await clearAllRecentSearches();
                showDefaultContent();
            });
        }

        // Trending tag click
        searchBody.querySelectorAll('.trending-tag').forEach(tag => {
            tag.addEventListener('click', () => {
                const term = tag.dataset.term;
                searchInput.value = term;
                if (isAIMode) {
                    performAISearch(term);
                } else {
                    performSearch(term);
                }
            });
        });

        // AI example queries
        searchBody.querySelectorAll('.ai-example-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                const query = btn.dataset.query;
                searchInput.value = query;
                performAISearch(query);
            });
        });
    }

    // Perform search
    function performSearch(query) {
        bsModal.hide();
        window.location.href = `/Customer/Home/Search?q=${encodeURIComponent(query)}`;
    }

    // Record search click
    async function recordClick(searchHistoryId, productId) {
        if (!searchHistoryId) return;
        try {
            await fetch(CONFIG.endpoints.recordClick, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ searchHistoryId, productId: parseInt(productId) })
            });
        } catch (error) {
            console.error('Error recording click:', error);
        }
    }

    // Record product view
    async function recordProductView(productId, source, searchTerm) {
        try {
            await fetch(CONFIG.endpoints.recordView, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    productId: parseInt(productId),
                    source: source,
                    searchTerm: searchTerm || null,
                    viewDuration: 0
                })
            });
        } catch (error) {
            console.error('Error recording view:', error);
        }
    }

    // Delete recent search
    async function deleteRecentSearch(id) {
        try {
            await fetch(`${CONFIG.endpoints.deleteRecent}/${id}`, {
                method: 'DELETE'
            });
        } catch (error) {
            console.error('Error deleting recent search:', error);
        }
    }

    // Clear all recent searches
    async function clearAllRecentSearches() {
        try {
            await fetch(CONFIG.endpoints.clearRecent, {
                method: 'DELETE'
            });
        } catch (error) {
            console.error('Error clearing recent searches:', error);
        }
    }

    // Clear search
    function clearSearch() {
        searchInput.value = '';
        toggleClearButton(false);
        showDefaultContent();
        searchInput.focus();
    }

    // Toggle clear button visibility
    function toggleClearButton(show) {
        if (clearBtn) {
            clearBtn.classList.toggle('show', show);
        }
    }

    // Escape HTML
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Parse markdown-style formatting to HTML
    function parseMarkdownLinks(text) {
        if (!text) return '';

        let result = text;

        // Step 1: Parse markdown links FIRST (before escaping affects brackets)
        // Pattern: [link text](url)
        result = result.replace(/\[([^\]]+)\]\(([^)]+)\)/g, function(match, linkText, url) {
            // Validate URL - only allow relative URLs and http/https
            if (url.startsWith('/') || url.startsWith('http://') || url.startsWith('https://')) {
                // Use placeholder to protect from HTML escaping
                return `{{LINK_START}}${linkText}{{LINK_MID}}${url}{{LINK_END}}`;
            }
            return linkText;
        });

        // Step 2: Parse **bold** - use placeholder
        result = result.replace(/\*\*([^*]+)\*\*/g, '{{BOLD_START}}$1{{BOLD_END}}');

        // Step 3: Parse ~~strikethrough~~ - use placeholder
        result = result.replace(/~~([^~]+)~~/g, '{{STRIKE_START}}$1{{STRIKE_END}}');

        // Step 4: Parse *italic* (single asterisk) - use placeholder
        result = result.replace(/\*([^*]+)\*/g, '{{ITALIC_START}}$1{{ITALIC_END}}');

        // Step 5: Now escape HTML to prevent XSS
        result = escapeHtml(result);

        // Step 6: Replace newlines with <br> for proper formatting
        result = result.replace(/\n/g, '<br>');

        // Step 7: Replace placeholders with actual HTML tags
        result = result.replace(/\{\{BOLD_START\}\}/g, '<strong>');
        result = result.replace(/\{\{BOLD_END\}\}/g, '</strong>');
        result = result.replace(/\{\{STRIKE_START\}\}/g, '<del>');
        result = result.replace(/\{\{STRIKE_END\}\}/g, '</del>');
        result = result.replace(/\{\{ITALIC_START\}\}/g, '<em>');
        result = result.replace(/\{\{ITALIC_END\}\}/g, '</em>');
        result = result.replace(/\{\{LINK_START\}\}([^{]+)\{\{LINK_MID\}\}([^{]+)\{\{LINK_END\}\}/g,
            '<a href="$2" class="ai-message-link">$1</a>');

        return result;
    }

    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // Expose for external use
    window.AISearch = {
        recordProductView: recordProductView,
        open: function() {
            if (bsModal) {
                bsModal.show();
            }
        },
        close: function() {
            if (bsModal) {
                bsModal.hide();
            }
        },
        setAIMode: function(enabled) {
            isAIMode = enabled;
            updatePlaceholder();
            updateModeIndicator();
        },
        search: function(query) {
            if (searchInput && bsModal) {
                bsModal.show();
                searchInput.value = query;
                if (isAIMode) {
                    performAISearch(query);
                } else {
                    fetchSuggestions(query);
                }
            }
        }
    };
})();
